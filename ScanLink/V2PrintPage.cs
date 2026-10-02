using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ScanLink.Themed;

namespace ScanLink
{
    /// <summary>
    /// Print labels as a page rather than a popup: three short steps down the left, a live
    /// label preview down the right.
    ///
    /// The steps are presentation over the existing print controls, not a new print path.
    /// Crop, product, employee, count, Generate, Preview and Start Printing are the very
    /// same controls the popup hosted — they are re-parented out of barcodeTextPanel and
    /// configGroupBox into the step that asks for them, so every handler, every validation
    /// and the whole barcode build still runs unchanged. What the wizard adds is ordering:
    /// the operator cannot reach "how many" before a product is chosen, and cannot reach
    /// "print" before the barcode has been generated.
    /// </summary>
    public partial class Form1
    {
        private const int StepIdentify = 0;
        private const int StepQuantity = 1;
        private const int StepPrint = 2;

        private static readonly string[] StepTitles =
        {
            "What are you labelling?",
            "How many?",
            "Check and print"
        };

        private Panel _printPage;
        private Panel _stepper;
        private readonly Panel[] _stepChips = new Panel[3];
        private readonly CardPanel[] _stepCards = new CardPanel[3];
        private CardPanel _printerSettingsCard;
        private Panel _labelPreview;
        private Label _printSummary;
        private Label _stepValidation;
        private int _activeStep = StepIdentify;

        // ---------------------------------------------------------------- page

        private void BuildV2PrintPage(Panel host)
        {
            _printPage = new Panel { Dock = DockStyle.Fill, BackColor = Theme.SurfaceApp, Padding = new Padding(Theme.S6) };

            Panel previewColumn = new Panel
            {
                Dock = DockStyle.Right,
                Width = 400,
                BackColor = Theme.SurfaceApp,
                Padding = new Padding(Theme.S4, 0, 0, 0),
                AutoScroll = true
            };
            previewColumn.Controls.Add(BuildPreviewCard());

            // A TableLayoutPanel column, not a stack of Top-docked panels inside an
            // AutoScroll host. That stack is what collapsed the active step card to a blank
            // strip once the printer settings card was expanded past the visible height:
            // docked siblings in a scrolling container are laid out against a display
            // rectangle that changes as the scrollbar appears. Rows size themselves from
            // each card's explicit height and collapse to nothing when a card is hidden.
            TableLayoutPanel stepsColumn = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.SurfaceApp,
                ColumnCount = 1,
                RowCount = 6,
                AutoScroll = true
            };
            stepsColumn.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < 5; i++) stepsColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            stepsColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _printerSettingsCard = BuildPrinterSettingsCard();
            BuildStepCards();
            _stepper = BuildStepper();

            stepsColumn.Controls.Add(_stepper, 0, 0);
            for (int i = 0; i < _stepCards.Length; i++) stepsColumn.Controls.Add(_stepCards[i], 0, i + 1);
            stepsColumn.Controls.Add(_printerSettingsCard, 0, 4);

            // Fill first so the docked right column claims its width first.
            _printPage.Controls.Add(stepsColumn);
            _printPage.Controls.Add(previewColumn);

            host.Controls.Add(_printPage);

            WirePreviewRefresh();
            GoToStep(StepIdentify);
        }

        /// <summary>Runs on every navigation to the page.</summary>
        private void OnPrintPageActivated()
        {
            try
            {
                // The two-up visualisation is drawn by the advanced panel and only knows to
                // show itself once its host is on screen.
                UpdatePictureBoxVisibility();
                RefreshLabelPreview();
                UpdateStepAvailability();

                // Same call barCodesButton_Click made when its popup opened.
                var ignored = EnsureCropOptionsLoadedAsync(updateStatusLabel: false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[PRINT] page activation failed: " + ex);
            }
        }

        // ---------------------------------------------------------------- stepper

        private Panel BuildStepper()
        {
            Panel stepper = new Panel
            {
                Dock = DockStyle.Top,
                // Two rows of chips plus the gap under them.
                Height = ChipHeight * 2 + Theme.S3 + Theme.S4,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };

            for (int i = 0; i < _stepChips.Length; i++)
            {
                _stepChips[i] = BuildStepChip(i);
                stepper.Controls.Add(_stepChips[i]);
            }

            // Placed by hand rather than flowed. A FlowLayoutPanel wrapped to one chip per
            // row because the chips' right margins were not in the width budget, and the
            // third chip then fell outside the stepper's fixed height and was covered by the
            // step card below it.
            stepper.Resize += (s, e) => LayoutStepChips(stepper);
            LayoutStepChips(stepper);
            return stepper;
        }

        private const int ChipHeight = 62;
        private const int ChipMinWidth = 230;
        private const int ChipMaxWidth = 380;
        private const int ChipNumberLeft = Theme.S5;
        private const int ChipCaptionLeft = Theme.S5 + 26 + Theme.S3;

        /// <summary>Two chips on the first row, the third below, at any column width.</summary>
        private void LayoutStepChips(Panel stepper)
        {
            if (stepper == null) return;

            int available = stepper.ClientSize.Width;
            if (available <= 0) return;

            int width = (available - Theme.S3) / 2;
            if (width < ChipMinWidth) width = ChipMinWidth;
            if (width > ChipMaxWidth) width = ChipMaxWidth;

            for (int i = 0; i < _stepChips.Length; i++)
            {
                Panel chip = _stepChips[i];
                if (chip == null) continue;

                chip.Width = width;
                chip.Left = (i % 2 == 0) ? 0 : width + Theme.S3;
                chip.Top = (i / 2) * (ChipHeight + Theme.S3);
                if (chip.Controls.Count > 1)
                {
                    chip.Controls[1].Width = Math.Max(60, width - ChipCaptionLeft - Theme.S4);
                }
            }
        }

        private Panel BuildStepChip(int index)
        {
            Panel chip = new Panel
            {
                Width = ChipMaxWidth,
                Height = ChipHeight,
                BackColor = Theme.SurfaceCard,
                Cursor = Cursors.Hand,
                Tag = index
            };

            Label number = new Label
            {
                Text = (index + 1).ToString(),
                AutoSize = false,
                Size = new Size(26, 26),
                Location = new Point(ChipNumberLeft, 18),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = Theme.FontSmBold,
                BackColor = Theme.SurfaceSunken,
                ForeColor = Theme.TextMuted,
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            ThemeStyles.RoundedCorners(number, 13);

            Label caption = new Label
            {
                Text = StepTitles[index],
                AutoSize = false,
                Location = new Point(ChipCaptionLeft, 18),
                Size = new Size(chip.Width - ChipCaptionLeft - Theme.S4, 26),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = Theme.FontMd,
                ForeColor = Theme.TextMuted,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };

            chip.Controls.Add(number);
            chip.Controls.Add(caption);
            chip.Paint += (s, e) => PaintStepChip((Panel)s, e);

            // Labels sit on top of the panel and swallow its clicks, so each forwards.
            foreach (Control c in new Control[] { chip, number, caption })
            {
                c.Click += (s, e) => TryGoToStep(index);
            }
            return chip;
        }

        private void PaintStepChip(Panel chip, PaintEventArgs e)
        {
            int index = (int)chip.Tag;
            bool active = index == _activeStep;
            bool reachable = index <= FurthestReachableStep();

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = new Rectangle(0, 0, chip.Width - 1, chip.Height - 1);

            using (GraphicsPath path = Theme.RoundedPath(bounds, Theme.RadiusLg))
            using (SolidBrush fill = new SolidBrush(reachable ? Theme.SurfaceCard : Theme.SurfaceApp))
            using (Pen border = new Pen(active ? Theme.ActionPrimary : Theme.BorderDefault, active ? 2f : 1f))
            {
                g.FillPath(fill, path);
                g.DrawPath(border, path);
            }
        }

        private void RestyleStepChips()
        {
            int furthest = FurthestReachableStep();
            for (int i = 0; i < _stepChips.Length; i++)
            {
                Panel chip = _stepChips[i];
                if (chip == null) continue;

                bool active = i == _activeStep;
                bool reachable = i <= furthest;
                bool done = i < _activeStep;

                Label number = (Label)chip.Controls[0];
                Label caption = (Label)chip.Controls[1];

                number.BackColor = active ? Theme.ActionPrimary : (done ? Theme.Ok50 : Theme.SurfaceSunken);
                number.ForeColor = active ? Theme.TextOnAccent : (done ? Theme.Ok700 : Theme.TextMuted);
                number.Text = done ? "✓" : (i + 1).ToString();

                caption.Font = active ? Theme.FontMdBold : Theme.FontMd;
                caption.ForeColor = active ? Theme.TextHeading : (reachable ? Theme.TextBody : Theme.TextMuted);
                chip.Cursor = reachable ? Cursors.Hand : Cursors.Default;

                chip.Invalidate();
            }
        }

        /// <summary>
        /// How far the operator has legitimately got. Steps are gated on the same state the
        /// print path itself requires, so the wizard cannot be walked past a missing product
        /// or an ungenerated barcode.
        /// </summary>
        private int FurthestReachableStep()
        {
            if (!IsIdentifyStepComplete()) return StepIdentify;
            return StepPrint;
        }

        private bool IsIdentifyStepComplete()
        {
            bool crop = comboBox_CropID != null && comboBox_CropID.SelectedIndex >= 0;
            bool product = comboBox_ProductID != null && !string.IsNullOrWhiteSpace(comboBox_ProductID.Text);
            bool picker = textBox_EmployeeID != null && !string.IsNullOrWhiteSpace(textBox_EmployeeID.Text);
            return crop && product && picker;
        }

        private void TryGoToStep(int index)
        {
            if (index > FurthestReachableStep())
            {
                ShowStepValidation("Choose a crop, a product and who is picking before moving on.");
                return;
            }
            GoToStep(index);
        }

        private void GoToStep(int index)
        {
            _activeStep = index;
            for (int i = 0; i < _stepCards.Length; i++)
            {
                if (_stepCards[i] != null) _stepCards[i].Visible = (i == index);
            }
            ShowStepValidation(null);
            RestyleStepChips();
            UpdatePrintSummary();
            RefreshLabelPreview();
        }

        private void ShowStepValidation(string message)
        {
            if (_stepValidation == null) return;
            _stepValidation.Text = message ?? string.Empty;
            _stepValidation.Visible = !string.IsNullOrEmpty(message);
        }

        private void UpdateStepAvailability()
        {
            RestyleStepChips();
        }

        // ---------------------------------------------------------------- step cards

        private void BuildStepCards()
        {
            _stepCards[StepIdentify] = BuildIdentifyCard();
            _stepCards[StepQuantity] = BuildQuantityCard();
            _stepCards[StepPrint] = BuildPrintCard();
        }

        /// <summary>Moves a designer-built control out of its original parent and places it
        /// at an explicit position in the step that now asks for it.</summary>
        private static void Adopt(Control control, Control newParent, int left, int top, int width)
        {
            if (control == null || newParent == null) return;
            if (control.Parent != null) control.Parent.Controls.Remove(control);
            control.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            control.Dock = DockStyle.None;
            control.Location = new Point(left, top);
            if (width > 0) control.Width = width;
            control.Visible = true;
            newParent.Controls.Add(control);
        }

        private static Label FieldLabel(string text, int left, int top, bool required)
        {
            Label label = new Label
            {
                Text = text,
                AutoSize = false,
                Size = new Size(260, 20),
                Location = new Point(left, top),
                Font = Theme.FontSmBold,
                ForeColor = Theme.TextLabel,
                BackColor = Color.Transparent,
                UseMnemonic = false
            };
            if (required) label.Text = text + " *";
            return label;
        }

        private static Label HintLabel(string text, int left, int top, int width)
        {
            return new Label
            {
                Text = text,
                AutoSize = false,
                Size = new Size(width, 18),
                Location = new Point(left, top),
                Font = Theme.FontXs,
                ForeColor = Theme.TextMuted,
                BackColor = Color.Transparent,
                UseMnemonic = false
            };
        }

        private CardPanel NewStepCard(string title, string subtitle, int bodyHeight)
        {
            CardPanel card = new CardPanel
            {
                Title = title,
                Subtitle = subtitle,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, Theme.S4),
                Visible = false
            };
            // Hidden cards must not reserve their row's margin, or the column keeps a gap
            // for each of the two steps that are not showing.
            card.VisibleChanged += (s, e) =>
                card.Margin = card.Visible ? new Padding(0, 0, 0, Theme.S4) : Padding.Empty;
            card.BodyPadding = true;
            // bodyHeight is the CONTENT height; the header and the body's own padding are
            // added on top. Sizing the card to the content alone clips whatever sits at the
            // bottom of the step, which is always its Next button.
            card.Height = CardPanel.TitledHeaderHeight + CardPanel.BodyPaddingV + bodyHeight;
            return card;
        }

        private CardPanel BuildIdentifyCard()
        {
            CardPanel card = NewStepCard(StepTitles[StepIdentify],
                "Pick the crop first — the product list narrows to match.", 262);
            // Opaque, not transparent: a transparent panel over the owner-painted card
            // left a stale 1px line above every button it contained. The card ground is
            // the same white, and the body is inset by the card's padding so it never
            // reaches the rounded corners.
            Panel body = new Panel { Dock = DockStyle.Fill, BackColor = Theme.SurfaceCard };

            const int colA = 0;
            const int colB = 320;

            body.Controls.Add(FieldLabel("Crop", colA, 0, true));
            Adopt(comboBox_CropID, body, colA, 22, 280);

            body.Controls.Add(FieldLabel("Product", colB, 0, true));
            Adopt(comboBox_ProductID, body, colB, 22, 320);

            body.Controls.Add(FieldLabel("Who is picking?", colA, 68, true));
            body.Controls.Add(HintLabel("Start typing a number or a name.", colA, 88, 280));
            Adopt(textBox_EmployeeID, body, colA, 110, 200);
            Adopt(button_FetchEmployees, body, colA + 210, 108, 0);
            if (button_FetchEmployees != null)
            {
                button_FetchEmployees.Text = "Find picker";
                ThemeStyles.Secondary(button_FetchEmployees);
            }

            Label printsAs = FieldLabel("This combination prints as", colB, 68, false);
            printsAs.Width = 360;
            body.Controls.Add(printsAs);
            Adopt(label_ProductDetail, body, colB, 90, 360);
            if (label_ProductDetail != null)
            {
                label_ProductDetail.AutoSize = false;
                label_ProductDetail.Height = 56;
                label_ProductDetail.Font = Theme.FontSm;
                label_ProductDetail.ForeColor = Theme.Indigo700;
                label_ProductDetail.BackColor = Color.Transparent;
            }

            Adopt(button_AddCombination, body, colB, 150, 0);
            if (button_AddCombination != null) ThemeStyles.Secondary(button_AddCombination);

            _stepValidation = new Label
            {
                AutoSize = false,
                Size = new Size(600, 20),
                Location = new Point(colA, 186),
                Font = Theme.FontSm,
                ForeColor = Theme.Err500,
                BackColor = Color.Transparent,
                Visible = false,
                UseMnemonic = false
            };
            body.Controls.Add(_stepValidation);

            Button next = new Button { Text = "Next: how many?  →", Location = new Point(colA, 210), Width = 200 };
            ThemeStyles.Primary(next);
            next.Click += (s, e) =>
            {
                if (!IsIdentifyStepComplete())
                {
                    ShowStepValidation("Choose a crop, a product and who is picking before moving on.");
                    return;
                }
                GoToStep(StepQuantity);
            };
            body.Controls.Add(next);

            card.Body.Controls.Add(body);
            return card;
        }

        private CardPanel BuildQuantityCard()
        {
            CardPanel card = NewStepCard(StepTitles[StepQuantity],
                "One label per carton. You can change this before printing.", 146);
            // Opaque, not transparent: a transparent panel over the owner-painted card
            // left a stale 1px line above every button it contained. The card ground is
            // the same white, and the body is inset by the card's padding so it never
            // reaches the rounded corners.
            Panel body = new Panel { Dock = DockStyle.Fill, BackColor = Theme.SurfaceCard };

            body.Controls.Add(FieldLabel("Number of labels", 0, 0, true));
            Adopt(numericUpDown_count, body, 0, 22, 140);
            if (numericUpDown_count != null) numericUpDown_count.Font = Theme.FontLg;

            body.Controls.Add(HintLabel("The printer will run this many identical labels.", 0, 58, 400));

            Button back = new Button { Text = "←  Back", Location = new Point(0, 96), Width = 110 };
            ThemeStyles.Secondary(back);
            back.Height = Theme.HeightMd;
            back.Click += (s, e) => GoToStep(StepIdentify);

            Button next = new Button { Text = "Next: check and print  →", Location = new Point(120, 96), Width = 220 };
            ThemeStyles.Primary(next);
            next.Click += (s, e) => GoToStep(StepPrint);

            body.Controls.Add(back);
            body.Controls.Add(next);

            card.Body.Controls.Add(body);
            return card;
        }

        private CardPanel BuildPrintCard()
        {
            CardPanel card = NewStepCard(StepTitles[StepPrint],
                "Generate the barcode, check the preview, then print.", 226);
            // Opaque, not transparent: a transparent panel over the owner-painted card
            // left a stale 1px line above every button it contained. The card ground is
            // the same white, and the body is inset by the card's padding so it never
            // reaches the rounded corners.
            Panel body = new Panel { Dock = DockStyle.Fill, BackColor = Theme.SurfaceCard };

            _printSummary = new Label
            {
                AutoSize = false,
                Size = new Size(560, 76),
                Location = new Point(0, 0),
                Font = Theme.FontSm,
                ForeColor = Theme.TextBody,
                BackColor = Color.Transparent,
                UseMnemonic = false
            };
            body.Controls.Add(_printSummary);

            Adopt(button_generateBarcode, body, 0, 88, 180);
            if (button_generateBarcode != null)
            {
                button_generateBarcode.Text = "Generate barcode";
                ThemeStyles.Navy(button_generateBarcode);
                button_generateBarcode.Height = Theme.HeightMd;
                button_generateBarcode.Click += (s, e) => { UpdatePrintSummary(); RefreshLabelPreview(); };
            }

            Adopt(button_preview, body, 190, 88, 170);
            if (button_preview != null)
            {
                button_preview.Text = "Open full preview";
                ThemeStyles.Secondary(button_preview);
                button_preview.Height = Theme.HeightMd;
            }

            Adopt(button_send, body, 0, 138, 200);
            if (button_send != null)
            {
                // Left as the handler manages it: button_send_Click and
                // button_generateBarcode_Click both drive its text, colour and enabled state
                // as the print job progresses, so the variant is not applied here.
                button_send.Height = Theme.HeightMd;
                button_send.FlatStyle = FlatStyle.Flat;
                button_send.FlatAppearance.BorderSize = 0;
                button_send.Font = Theme.FontSmBold;
                button_send.ForeColor = Theme.TextOnAccent;
                ThemeStyles.RoundedCorners(button_send, Theme.RadiusSm);
            }

            Adopt(progressBar, body, 210, 144, 240);

            Button back = new Button { Text = "←  Back", Location = new Point(0, 182), Width = 110 };
            ThemeStyles.Secondary(back);
            back.Click += (s, e) => GoToStep(StepQuantity);
            body.Controls.Add(back);

            card.Body.Controls.Add(body);
            return card;
        }

        private void UpdatePrintSummary()
        {
            if (_printSummary == null) return;
            try
            {
                string crop = (comboBox_CropID != null) ? comboBox_CropID.Text : "";
                string product = (comboBox_ProductID != null) ? comboBox_ProductID.Text : "";
                string picker = (textBox_EmployeeID != null) ? textBox_EmployeeID.Text : "";
                string count = (numericUpDown_count != null) ? numericUpDown_count.Value.ToString("0") : "0";

                string barcode = _barcodeGenerated && !string.IsNullOrEmpty(_generatedBarcode)
                    ? _generatedBarcode
                    : "not generated yet";

                _printSummary.Text =
                    "Crop:  " + crop + "\r\n" +
                    "Product:  " + product + "\r\n" +
                    "Picker:  " + picker + "     Labels:  " + count + "\r\n" +
                    "Barcode:  " + barcode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[PRINT] summary failed: " + ex);
            }
        }

        // ---------------------------------------------------------------- printer settings

        /// <summary>
        /// "Most people never need to change these." Label size, quality and darkness are
        /// the most intimidating part of the print screen and the most damaging to change by
        /// accident, so the whole advanced panel starts collapsed. Nothing is removed —
        /// every control inside is still constructed, wired and applied when a job prints.
        /// </summary>
        /// <summary>The designer height of advancedPanel; its contents are hand-placed to
        /// this size, so it neither grows nor shrinks usefully.</summary>
        private const int AdvancedPanelHeight = 470;

        private CardPanel BuildPrinterSettingsCard()
        {
            CardPanel card = new CardPanel
            {
                Title = "Printer settings",
                Subtitle = "Most people never need to change these.",
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, Theme.S4)
            };
            card.BodyPadding = true;
            card.Height = CardPanel.TitledHeaderHeight;

            Button toggle = new Button { Text = "Show", Width = 96 };
            ThemeStyles.Secondary(toggle);

            Panel settingsHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoScroll = true,
                Visible = false
            };

            if (advancedPanel != null)
            {
                if (advancedPanel.Parent != null) advancedPanel.Parent.Controls.Remove(advancedPanel);
                // AutoSize off, explicit height, docked to the top of a scrolling host. The
                // panel ships as AutoSize=GrowAndShrink around a Top-docked group box, which
                // in a Fill dock is the WinForms collapse-to-zero case that rendered the
                // print surfaces as empty boxes in the first pass.
                advancedPanel.AutoSize = false;
                advancedPanel.Dock = DockStyle.Top;
                advancedPanel.Height = AdvancedPanelHeight;
                advancedPanel.Visible = true;
                advancedPanel.BackColor = Color.Transparent;
                settingsHost.Controls.Add(advancedPanel);
            }

            bool expanded = false;
            toggle.Click += (s, e) =>
            {
                expanded = !expanded;
                settingsHost.Visible = expanded;
                toggle.Text = expanded ? "Hide" : "Show";
                card.Height = expanded
                    ? CardPanel.TitledHeaderHeight + CardPanel.BodyPaddingV + AdvancedPanelHeight
                    : CardPanel.TitledHeaderHeight;
                if (expanded) UpdatePictureBoxVisibility();
            };

            card.Actions.Controls.Add(toggle);
            card.RefreshHeader();
            card.Body.Controls.Add(settingsHost);
            return card;
        }

        // ---------------------------------------------------------------- preview

        private const int PreviewHeight = 210;
        private const int PreviewNoteHeight = 48;

        private CardPanel BuildPreviewCard()
        {
            CardPanel card = new CardPanel
            {
                Title = "Preview",
                Subtitle = "This is what will come out of the printer.",
                Dock = DockStyle.Top
            };
            card.BodyPadding = true;
            // Sized to its content. Filling the column left most of a full-height white
            // card empty under a small label.
            card.Height = CardPanel.TitledHeaderHeight + CardPanel.BodyPaddingV + PreviewHeight + PreviewNoteHeight;

            _labelPreview = new Panel { Dock = DockStyle.Top, Height = PreviewHeight, BackColor = Color.Transparent };
            _labelPreview.Paint += PaintLabelPreview;

            Label note = new Label
            {
                Dock = DockStyle.Top,
                Height = PreviewNoteHeight,
                Text = "Shown at roughly actual size. Use \"Open full preview\" on step 3 for the exact layout.",
                Font = Theme.FontXs,
                ForeColor = Theme.TextMuted,
                BackColor = Color.Transparent,
                Padding = new Padding(0, Theme.S3, 0, 0),
                UseMnemonic = false
            };

            card.Body.Controls.Add(note);
            card.Body.Controls.Add(_labelPreview);
            return card;
        }

        /// <summary>
        /// Wires the crop and product combos, then the preview.
        ///
        /// The two app handlers are the important half: comboBox_CropID_SelectedIndexChanged
        /// is what narrows the product list to the chosen crop, and
        /// comboBox_ProductID_SelectedIndexChanged is what fills in "this combination prints
        /// as". Both used to be attached by barCodesButton_Click when it opened its popup,
        /// so with the popup gone they have no other home — without this the crop dropdown
        /// would select nothing and the combination line would stay blank.
        ///
        /// Detached before attaching: the handlers are idempotent but their side effects are
        /// not, and a repeat build would otherwise repopulate the product list twice per
        /// selection.
        /// </summary>
        private void WirePreviewRefresh()
        {
            if (comboBox_CropID != null)
            {
                comboBox_CropID.SelectedIndexChanged -= comboBox_CropID_SelectedIndexChanged;
                comboBox_CropID.SelectedIndexChanged += comboBox_CropID_SelectedIndexChanged;
            }
            if (comboBox_ProductID != null)
            {
                comboBox_ProductID.SelectedIndexChanged -= comboBox_ProductID_SelectedIndexChanged;
                comboBox_ProductID.SelectedIndexChanged += comboBox_ProductID_SelectedIndexChanged;
            }

            // Attached after the app handlers so the preview repaints against the list and
            // the detail line they have just updated, not the previous ones.
            EventHandler refresh = (s, e) => { RefreshLabelPreview(); UpdateStepAvailability(); UpdatePrintSummary(); };

            if (comboBox_CropID != null) comboBox_CropID.SelectedIndexChanged += refresh;
            if (comboBox_ProductID != null)
            {
                comboBox_ProductID.SelectedIndexChanged += refresh;
                comboBox_ProductID.TextChanged += refresh;
            }
            if (textBox_EmployeeID != null) textBox_EmployeeID.TextChanged += refresh;
            if (numericUpDown_count != null) numericUpDown_count.ValueChanged += refresh;
        }

        private void RefreshLabelPreview()
        {
            if (_labelPreview != null) _labelPreview.Invalidate();
        }

        /// <summary>
        /// A compact rendering of the printed label. It deliberately reuses
        /// DrawBarcodePreview — the same routine the full-size preview dialog uses — so the
        /// two cannot drift apart, and it is a representation rather than a scannable
        /// barcode, which is why the note under it points at the full preview.
        /// </summary>
        private void PaintLabelPreview(object sender, PaintEventArgs e)
        {
            Panel surface = (Panel)sender;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int labelWidth = Math.Min(surface.ClientSize.Width - Theme.S4 * 2, 300);
            int labelHeight = 170;
            Rectangle label = new Rectangle(
                (surface.ClientSize.Width - labelWidth) / 2, Theme.S2, labelWidth, labelHeight);

            using (SolidBrush ground = new SolidBrush(Theme.SurfaceSunken))
            {
                g.FillRectangle(ground, new Rectangle(0, 0, surface.ClientSize.Width, surface.ClientSize.Height));
            }
            using (SolidBrush paper = new SolidBrush(Color.White))
            using (Pen edge = new Pen(Theme.BorderStrong, 1f))
            {
                g.FillRectangle(paper, label);
                g.DrawRectangle(edge, label);
            }

            string barcodeId;
            string productLine;
            string pickerLine;
            try
            {
                barcodeId = (_barcodeGenerated && !string.IsNullOrEmpty(_generatedBarcode))
                    ? _generatedBarcode
                    : BuildUpcBarcodeFromInputs();

                ScanLink.ProductCombination combo = GetSelectedProductCombination();
                productLine = (combo != null)
                    ? string.Join("  ", new[] { combo.variety_name, combo.count_name, combo.grade_name })
                    : ((comboBox_ProductID != null) ? comboBox_ProductID.Text : "");

                string employee = GetEmployeeNameForBarcode(barcodeId);
                pickerLine = string.IsNullOrWhiteSpace(employee)
                    ? ("PICKER " + ((textBox_EmployeeID != null) ? textBox_EmployeeID.Text : ""))
                    : employee;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[PRINT] preview data unavailable: " + ex.Message);
                TextRenderer.DrawText(g, "Choose a crop, product and picker to see the label.",
                    Theme.FontSm, label, Theme.TextMuted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
                return;
            }

            using (Font mono = new Font("Courier New", 8.5f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, productLine ?? "", mono,
                    new Rectangle(label.X + Theme.S3, label.Y + Theme.S3, label.Width - Theme.S3 * 2, 18),
                    Color.Black, TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

                TextRenderer.DrawText(g, pickerLine ?? "", mono,
                    new Rectangle(label.X + Theme.S3, label.Y + Theme.S3 + 18, label.Width - Theme.S3 * 2, 18),
                    Color.Black, TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

                Rectangle bars = new Rectangle(
                    label.X + Theme.S3, label.Y + 56, label.Width - Theme.S3 * 2, 78);
                try
                {
                    DrawBarcodePreview(g, bars, barcodeId,
                        (comboBox_barcode != null) ? comboBox_barcode.Text : "Code128");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("[PRINT] barcode preview failed: " + ex.Message);
                }

                TextRenderer.DrawText(g, barcodeId ?? "", mono,
                    new Rectangle(label.X + Theme.S3, label.Bottom - 26, label.Width - Theme.S3 * 2, 18),
                    Color.Black, TextFormatFlags.Left | TextFormatFlags.NoPrefix);
            }
        }
    }
}
