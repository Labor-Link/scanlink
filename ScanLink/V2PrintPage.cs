using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ScanLink.DesignSystem;
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
        private SLStack _stepper;
        private readonly PrintStepChip[] _stepChips = new PrintStepChip[3];
        private readonly CardPanel[] _stepCards = new CardPanel[3];
        private readonly int[] _stepCardHeights = new int[3];
        private CardPanel _printerSettingsCard;
        private Panel _labelPreview;
        private SLKeyValueList _printSummary;
        private SLBanner _stepValidation;
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

        /// <summary>
        /// Fills Test Mode and Barcode Type for the selected printer language when they are
        /// empty, and selects the first entry when nothing is selected. Their change handlers
        /// are held off while doing so: they save settings, and saving defaults here would
        /// overwrite the operator's saved settings before they are loaded.
        /// </summary>
        private void EnsurePrinterFunctionLists()
        {
            if (comboBox_test == null || comboBox_barcode == null) return;
            if (comboBox_emulation != null && comboBox_emulation.Text != "PPLB") return; // the only language wired up
            comboBox_test.SelectedIndexChanged -= comboBox_test_SelectedIndexChanged;
            comboBox_barcode.SelectedIndexChanged -= comboBox_barcode_SelectedIndexChanged;
            try
            {
                if (comboBox_test.Items.Count == 0)
                    foreach (FunctionData item in PPLB_ItemList) comboBox_test.Items.Add(item.Descration);
                if (comboBox_barcode.Items.Count == 0)
                    foreach (string str in PPLB_BarcodeList) comboBox_barcode.Items.Add(str);
                if (comboBox_test.SelectedIndex < 0 && comboBox_test.Items.Count > 0) comboBox_test.SelectedIndex = 0;
                if (comboBox_barcode.SelectedIndex < 0 && comboBox_barcode.Items.Count > 0) comboBox_barcode.SelectedIndex = 0;
            }
            finally
            {
                comboBox_test.SelectedIndexChanged += comboBox_test_SelectedIndexChanged;
                comboBox_barcode.SelectedIndexChanged += comboBox_barcode_SelectedIndexChanged;
            }
        }

        /// <summary>Runs on every navigation to the page.</summary>
        private void OnPrintPageActivated()
        {
            try
            {
                EnsurePrinterFunctionLists();
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

        /// <summary>The mockup's three step buttons, side by side (LabelsScreen.js).</summary>
        private SLStack BuildStepper()
        {
            SLStack stepper = new SLStack(SLOrientation.Horizontal, Theme.S2)
            {
                Dock = DockStyle.Top,
                Height = PrintStepChip.ChipHeight + Theme.S4,
                Padding = new Padding(0, 0, 0, Theme.S4),
                Align = SLAlign.Stretch,
                Margin = new Padding(0)
            };
            for (int i = 0; i < _stepChips.Length; i++)
            {
                int index = i;
                _stepChips[i] = new PrintStepChip { Number = i + 1, Caption = StepTitles[i] };
                _stepChips[i].Click += (s, e) => TryGoToStep(index);
                stepper.Controls.Add(_stepChips[i]);
                stepper.SetGrow(_stepChips[i]);
            }
            return stepper;
        }

        private void RestyleStepChips()
        {
            int furthest = FurthestReachableStep();
            for (int i = 0; i < _stepChips.Length; i++)
            {
                PrintStepChip chip = _stepChips[i];
                if (chip == null) continue;
                chip.Active = i == _activeStep;
                chip.Done = i < _activeStep;
                chip.Reachable = i <= furthest;
                chip.Cursor = chip.Reachable ? Cursors.Hand : Cursors.Default;
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
            // A real list entry, not half-typed search text (that encodes product "000").
            ProductComboItem chosen = comboBox_ProductID != null ? comboBox_ProductID.SelectedItem as ProductComboItem : null;
            bool product = chosen != null && !chosen.IsHeader;
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
            bool show = !string.IsNullOrEmpty(message);
            _stepValidation.Message = message ?? string.Empty;
            SLVisibility.Set(_stepValidation, show);
            // The banner sits in the identify card; its row needs the extra height.
            CardPanel card = _stepCards[StepIdentify];
            if (card != null)
                card.Height = _stepCardHeights[StepIdentify] + (show ? Theme.S4 + _stepValidation.MeasureHeight(Math.Max(300, card.Width - 42)) : 0);
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

        // Step card = CardPanel (header, body, footer with the step's buttons) holding one
        // SLStack laid out from LabelsScreen.js. Card heights are fixed per step because the
        // steps column is a TableLayoutPanel whose rows size from each card's Height.
        private const int StepFooterHeight = 57;   // 1px divider + 12px + 32px + 12px

        private CardPanel NewStepCard(int step, string subtitle, int bodyHeight, SLStack content, params Control[] footer)
        {
            CardPanel card = new CardPanel
            {
                Title = StepTitles[step],
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

            content.Dock = DockStyle.Fill;
            content.BackColor = Theme.SurfaceCard;
            card.Body.Controls.Add(content);

            SLStack actions = new SLStack(SLOrientation.Horizontal, Theme.S2) { Dock = DockStyle.Fill, Align = SLAlign.Center };
            actions.AddRange(footer);
            card.Footer.Controls.Add(actions);
            card.ShowFooter(true);

            _stepCardHeights[step] = CardPanel.TitledHeaderHeight + CardPanel.BodyPaddingV + bodyHeight + StepFooterHeight;
            card.Height = _stepCardHeights[step];
            return card;
        }

        private SLButton BackButton(int toStep)
        {
            SLButton back = new SLButton { Text = "Back", Variant = SLVariant.Secondary, ButtonSize = SLSize.Sm, IconName = "arrow-left" };
            back.Click += (s, e) => GoToStep(toStep);
            return back;
        }

        private CardPanel BuildIdentifyCard()
        {
            // Crop: a drop-down list, swapped for an SLComboBox before any data is bound to it
            // (SetComboItems binds through the ComboBox API).
            if (comboBox_CropID != null && !(comboBox_CropID is SLComboBox))
            {
                ComboBox old = comboBox_CropID;
                if (old.Parent != null) old.Parent.Controls.Remove(old);
                SLComboBox crop = new SLComboBox { Name = old.Name, PlaceholderText = "Choose a crop" };
                if (old.DataSource != null)
                {
                    crop.DisplayMember = old.DisplayMember;
                    crop.ValueMember = old.ValueMember;
                    crop.DataSource = old.DataSource;
                }
                else
                {
                    foreach (object item in old.Items) crop.Items.Add(item);
                    crop.SelectedIndex = old.SelectedIndex;
                }
                comboBox_CropID = crop;
            }

            // Product stays the stock editable combo (owner-drawn, type-ahead handlers) in a frame.
            SLFrame product = new SLFrame(comboBox_ProductID);

            button_FetchEmployees = ReplaceButton(button_FetchEmployees, button_FetchEmployees_Click,
                new SLButton { Text = "Find picker", Variant = SLVariant.Secondary, IconName = "search" });
            SLStack picker = new SLStack(SLOrientation.Horizontal, Theme.S2) { Align = SLAlign.Center };
            SLTextBox employee = new SLTextBox(textBox_EmployeeID);
            picker.AddRange(employee, button_FetchEmployees);
            picker.SetGrow(employee);

            if (label_ProductDetail != null)
            {
                if (label_ProductDetail.Parent != null) label_ProductDetail.Parent.Controls.Remove(label_ProductDetail);
                label_ProductDetail.AutoSize = false;
                label_ProductDetail.Height = 56;   // up to three lines (grade, count, carton, weight)
                label_ProductDetail.Font = Theme.FontSmSemibold;
                label_ProductDetail.ForeColor = Theme.Indigo700;
                label_ProductDetail.BackColor = Color.Transparent;
                label_ProductDetail.Visible = true;
            }
            button_AddCombination = ReplaceButton(button_AddCombination, button_AddCombination_Click,
                new SLButton { Text = "Add combination", Variant = SLVariant.Secondary, ButtonSize = SLSize.Sm, IconName = "plus" });
            SLStack printsAs = new SLStack(SLOrientation.Vertical, Theme.S2) { Align = SLAlign.Start };
            if (label_ProductDetail != null) printsAs.Controls.Add(label_ProductDetail);
            printsAs.Controls.Add(button_AddCombination);
            if (label_ProductDetail != null) label_ProductDetail.Width = 360;

            SLFieldSet fields = new SLFieldSet { Columns = 2 };
            fields.Add(new SLField("Crop", comboBox_CropID) { Required = true });
            fields.Add(new SLField("Product", product) { Required = true });
            fields.Add(new SLField("Who is picking?", picker) { Required = true, Hint = "Employee number, or use Find picker." });
            fields.Add(new SLField("This combination prints as", printsAs));

            _stepValidation = new SLBanner { Tone = SLTone.Error, IconName = "circle-alert" };
            SLStack content = new SLStack(SLOrientation.Vertical, Theme.S4);
            content.AddRange(fields, _stepValidation);
            SLVisibility.Set(_stepValidation, false);

            SLButton next = new SLButton { Text = "Next: how many?", IconEndName = "arrow-right" };
            next.Click += (s, e) =>
            {
                if (!IsIdentifyStepComplete())
                {
                    ShowStepValidation("Choose a crop, a product and who is picking before moving on.");
                    return;
                }
                GoToStep(StepQuantity);
            };

            // Rows: Crop/Product (16 + 6 + 38) + 16 + Picker (16 + 2 + 17 + 6 + 38) / prints-as.
            return NewStepCard(StepIdentify, "Pick the crop first — the product list narrows to match.", 182, content, next);
        }

        private CardPanel BuildQuantityCard()
        {
            SLFrame count = new SLFrame(numericUpDown_count) { Width = 160 };
            SLStack row = new SLStack(SLOrientation.Horizontal, 0) { Align = SLAlign.Start };
            row.Controls.Add(count);

            SLStack content = new SLStack(SLOrientation.Vertical, Theme.S4);
            content.Controls.Add(new SLField("Number of labels", row) { Required = true, Hint = "One label per carton. The printer runs this many identical labels." });

            SLButton next = new SLButton { Text = "Next: check and print", IconEndName = "arrow-right" };
            next.Click += (s, e) => GoToStep(StepPrint);

            return NewStepCard(StepQuantity, "You can change this before printing.", 80, content, BackButton(StepIdentify), next);
        }

        private CardPanel BuildPrintCard()
        {
            _printSummary = new SLKeyValueList();

            button_generateBarcode = ReplaceButton(button_generateBarcode, button_generateBarcode_Click,
                new SLButton { Text = "Generate barcode", Variant = SLVariant.Navy, IconName = "barcode" });
            button_generateBarcode.Click += (s, e) => { SnapshotBarcodeInputs(); UpdatePrintSummary(); RefreshLabelPreview(); };

            button_preview = ReplaceButton(button_preview, button_preview_Click,
                new SLButton { Text = "Open full preview", Variant = SLVariant.Secondary, IconName = "eye" });

            // Start printing: its text and enabled state are driven by the print handlers, which
            // also colour it green when ready. That green maps to the Success variant.
            SLButton send = new SLButton { Text = button_send != null ? button_send.Text : "Start printing", IconName = "printer" };
            bool wasEnabled = button_send == null || button_send.Enabled;
            button_send = ReplaceButton(button_send, button_send_Click, send);
            send.Enabled = wasEnabled;
            send.Click += (s, e) => { UpdatePrintSummary(); RefreshLabelPreview(); };   // after the print handler
            bool mapping = false;
            send.BackColorChanged += (s, e) =>
            {
                if (mapping || send.BackColor.A != 255) return;   // ignore our own reset below
                mapping = true;
                send.Variant = send.BackColor.ToArgb() == Color.FromArgb(46, 125, 50).ToArgb() ? SLVariant.Success : SLVariant.Primary;
                send.BackColor = Color.Transparent;               // the colour comes from the variant
                mapping = false;
            };

            SLStack generateRow = new SLStack(SLOrientation.Horizontal, Theme.S2) { Align = SLAlign.Center };
            generateRow.AddRange(button_generateBarcode, button_preview);

            SLStack printRow = new SLStack(SLOrientation.Horizontal, Theme.S3) { Align = SLAlign.Center };
            printRow.Controls.Add(send);
            if (progressBar != null)
            {
                if (progressBar.Parent != null) progressBar.Parent.Controls.Remove(progressBar);
                progressBar.Dock = DockStyle.None;
                progressBar.Height = 8;
                progressBar.Width = 240;
                printRow.Controls.Add(progressBar);
            }

            SLStack content = new SLStack(SLOrientation.Vertical, Theme.S4);
            content.AddRange(_printSummary, generateRow, printRow);

            // Summary: 5 rows x 18 + 4 x 10 gaps; then two 38px rows, 16px gaps.
            return NewStepCard(StepPrint, "Generate the barcode, check the preview, then print.", 130 + 16 + 38 + 16 + 38, content, BackButton(StepQuantity));
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
                    : "Not generated yet";

                _printSummary.Clear();
                _printSummary.Add("Crop", string.IsNullOrWhiteSpace(crop) ? "—" : crop);
                _printSummary.Add("Product", string.IsNullOrWhiteSpace(product) ? "—" : product);
                _printSummary.Add("Picker", string.IsNullOrWhiteSpace(picker) ? "—" : picker);
                _printSummary.Add("Labels", count);
                _printSummary.Add("Barcode", barcode);
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
        /// <summary>
        /// The advanced panel was laid out for a wide popup: three settings sections down the
        /// left and the sticker layout diagram (550x350) at x=650. In the steps column that
        /// diagram fell off the right edge, so the sticker layout could not be seen. Here the
        /// sections keep their place and the diagrams move underneath them.
        /// </summary>
        private int ArrangeAdvancedPanel()
        {
            if (advancedPanel == null) return 470;

            // The three settings sections dock to the top of the panel (their designer heights).
            int sectionsBottom = advancedPanel.Padding.Top;
            foreach (Control c in new Control[] { printerConfigPanel, dimensionsPanel, qualityPanel })
                if (c != null) sectionsBottom += c.Height;

            int diagramTop = sectionsBottom + Theme.S4;
            int diagramH = 0;
            foreach (PictureBox diagram in new[] { dimensionVisualizationPictureBox, dimensionVisualizationPictureBox_TwoUp })
            {
                if (diagram == null) continue;
                diagram.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                diagram.Location = new Point(0, diagramTop);
                diagramH = Math.Max(diagramH, diagram.Height);
            }

            int height = diagramTop + diagramH + Theme.S2;
            advancedPanel.AutoSize = false;     // AutoSize + Dock collapses (docs/V2_MIGRATION_STATUS.md)
            advancedPanel.Height = height;
            advancedPanel.MinimumSize = new Size(600, height);
            return height;
        }

        /// <summary>
        /// Takes the three settings sections and the diagrams out of the "Advanced Print
        /// Settings" group box (the card already titles them) and restyles their contents:
        /// field-label type, muted units, SL drop-downs (small, to keep the designer's row
        /// spacing) and an SL checkbox. Handlers and selections move with each swapped control.
        /// </summary>
        private void RestyleAdvancedSettings()
        {
            if (advancedPanel == null || advancedGroupBox == null) return;

            // Same relative order, so the docked sections stack as before.
            System.Collections.Generic.List<Control> children = new System.Collections.Generic.List<Control>();
            foreach (Control c in advancedGroupBox.Controls) children.Add(c);
            foreach (Control c in children) advancedPanel.Controls.Add(c);
            advancedGroupBox.Visible = false;
            advancedPanel.Padding = Padding.Empty;
            advancedPanel.BackColor = Theme.SurfaceCard;

            foreach (Panel section in new[] { printerConfigPanel, dimensionsPanel, qualityPanel })
            {
                if (section == null) continue;
                section.BackColor = Theme.SurfaceCard;
                foreach (Control c in section.Controls)
                {
                    Label label = c as Label;
                    if (label != null)
                    {
                        bool unit = label.Name.StartsWith("label_px", StringComparison.Ordinal) ||
                                    label.Name.StartsWith("label_mm", StringComparison.Ordinal) ||
                                    label.Name == "label_darknessValue";
                        label.Font = unit ? Theme.FontSm : Theme.FontSmMedium;
                        label.ForeColor = unit ? Theme.TextMuted : Theme.TextLabel;
                        label.BackColor = Color.Transparent;
                        label.UseMnemonic = false;
                        continue;
                    }
                    NumericUpDown number = c as NumericUpDown;
                    if (number != null) { number.Font = Theme.FontMd; number.BorderStyle = BorderStyle.FixedSingle; continue; }
                    TrackBar track = c as TrackBar;
                    if (track != null) track.BackColor = Theme.SurfaceCard;
                }
            }

            comboBox_emulation = ReplaceCombo(comboBox_emulation, comboBox_emulation_SelectedIndexChanged);
            comboBox_test = ReplaceCombo(comboBox_test, comboBox_test_SelectedIndexChanged);
            comboBox_barcode = ReplaceCombo(comboBox_barcode, comboBox_barcode_SelectedIndexChanged);
            comboBox_speed = ReplaceCombo(comboBox_speed, comboBox_speed_SelectedIndexChanged);

            if (checkBox_twoUp != null && !(checkBox_twoUp is SLCheckBox))
            {
                CheckBox old = checkBox_twoUp;
                SLCheckBox twoUp = new SLCheckBox
                {
                    Name = old.Name,
                    Text = old.Text,
                    Checked = old.Checked,
                    Enabled = old.Enabled,
                    Location = old.Location
                };
                old.CheckedChanged -= checkBox_twoUp_CheckedChanged;
                Control parent = old.Parent;
                if (parent != null) { parent.Controls.Remove(old); parent.Controls.Add(twoUp); }
                twoUp.CheckedChanged += checkBox_twoUp_CheckedChanged;
                checkBox_twoUp = twoUp;
            }
        }

        /// <summary>Swaps a designer drop-down list for a small SLComboBox in place: items,
        /// selection, position and handler move across; the new one is centred on the old row.</summary>
        private static ComboBox ReplaceCombo(ComboBox old, EventHandler handler)
        {
            if (old == null || old is SLComboBox) return old;
            SLComboBox combo = new SLComboBox { Name = old.Name, FieldSize = SLSize.Sm, Enabled = old.Enabled };
            foreach (object item in old.Items) combo.Items.Add(item);
            combo.SelectedIndex = old.SelectedIndex;
            combo.Width = old.Width;
            combo.Location = new Point(old.Left, Math.Max(0, old.Top + (old.Height - combo.Height) / 2));

            old.SelectedIndexChanged -= handler;
            Control parent = old.Parent;
            if (parent != null) { parent.Controls.Remove(old); parent.Controls.Add(combo); }
            combo.SelectedIndexChanged += handler;
            return combo;
        }

        private CardPanel BuildPrinterSettingsCard()
        {
            CardPanel card = new CardPanel
            {
                Title = "Printer settings",
                Subtitle = "Label size, position on the sticker, darkness and speed. Most people never need to change these.",
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, Theme.S4)
            };
            card.BodyPadding = true;
            card.Height = CardPanel.TitledHeaderHeight;

            SLButton toggle = new SLButton { Text = "Show", Variant = SLVariant.Secondary, ButtonSize = SLSize.Sm, IconName = "chevron-down" };

            Panel settingsHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.SurfaceCard,
                AutoScroll = true,   // horizontal scroll only when the column is narrower than the diagram
                Visible = false
            };

            int panelHeight = 470;
            if (advancedPanel != null)
            {
                if (advancedPanel.Parent != null) advancedPanel.Parent.Controls.Remove(advancedPanel);
                advancedPanel.Dock = DockStyle.Top;
                advancedPanel.Visible = true;
                RestyleAdvancedSettings();
                panelHeight = ArrangeAdvancedPanel();
                settingsHost.Controls.Add(advancedPanel);
            }

            bool expanded = false;
            toggle.Click += (s, e) =>
            {
                expanded = !expanded;
                settingsHost.Visible = expanded;
                toggle.Text = expanded ? "Hide" : "Show";
                toggle.IconName = expanded ? "chevron-up" : "chevron-down";
                card.Height = expanded
                    ? CardPanel.TitledHeaderHeight + CardPanel.BodyPaddingV + panelHeight + SystemInformation.HorizontalScrollBarHeight
                    : CardPanel.TitledHeaderHeight;
                if (expanded)
                {
                    EnsurePrinterFunctionLists();
                    UpdatePictureBoxVisibility();
                    // Bring the opened settings into view; they sit below the step card.
                    ScrollableControl column = card.Parent as ScrollableControl;
                    if (column != null) column.ScrollControlIntoView(card);
                }
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
            EventHandler refresh = (s, e) => { InvalidateBarcodeIfInputsChanged(); RefreshLabelPreview(); UpdateStepAvailability(); UpdatePrintSummary(); };

            if (comboBox_CropID != null) comboBox_CropID.SelectedIndexChanged += refresh;
            if (comboBox_ProductID != null)
            {
                comboBox_ProductID.SelectedIndexChanged += refresh;
                comboBox_ProductID.TextChanged += refresh;
            }
            if (textBox_EmployeeID != null) textBox_EmployeeID.TextChanged += refresh;
            if (numericUpDown_count != null) numericUpDown_count.ValueChanged += refresh;
        }

        // ---------------------------------------------------------------- barcode freshness

        /// <summary>Crop / product / picker the current barcode was generated from.</summary>
        private string _barcodeInputs;

        private string CurrentBarcodeInputs()
        {
            return string.Join("|",
                comboBox_CropID != null ? Convert.ToString(comboBox_CropID.SelectedValue) : "",
                comboBox_ProductID != null ? Convert.ToString(comboBox_ProductID.SelectedValue) : "",
                textBox_EmployeeID != null ? textBox_EmployeeID.Text.Trim() : "");
        }

        private void SnapshotBarcodeInputs()
        {
            if (_barcodeGenerated) _barcodeInputs = CurrentBarcodeInputs();
        }

        /// <summary>
        /// The wizard invites going back to change crop, product or picker after generating.
        /// The old barcode digits must not then print with the new product's text: when those
        /// inputs change, the barcode is dropped and Start printing waits for a new one.
        /// (The label count is not part of the barcode and does not invalidate it.)
        /// </summary>
        private void InvalidateBarcodeIfInputsChanged()
        {
            if (!_barcodeGenerated || _barcodeInputs == null) return;
            if (CurrentBarcodeInputs() == _barcodeInputs) return;
            _barcodeGenerated = false;
            _generatedBarcode = "";
            _barcodeInputs = null;
            if (button_send != null)
            {
                button_send.Enabled = false;
                button_send.Text = "Start printing";
                button_send.BackColor = Color.FromArgb(100, 100, 100);
            }
            statusLabel.Text = "Crop, product or picker changed — generate the barcode again before printing.";
            statusLabel.ForeColor = Theme.Warn500;
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

            // LabelsScreen.js sticker: a #F1F3F7 tray (radius 8) holding a white label with a
            // #D0D5DD border (radius 4).
            using (SolidBrush card = new SolidBrush(Theme.SurfaceCard))
                g.FillRectangle(card, surface.ClientRectangle);
            SLPaint.Box(g, new Rectangle(0, 0, surface.ClientSize.Width, surface.ClientSize.Height), Theme.RadiusMd, Theme.N100, Color.Empty);
            SLPaint.Box(g, label, Theme.RadiusXs, Color.White, Theme.BorderStrong);

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

            using (Font mono = new Font("Consolas", 8.25f, FontStyle.Bold))   // 11px / 700, as the mockup's sticker
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

    /// <summary>
    /// One step button from LabelsScreen.js: white, radius 8, padding 12x14, a 24px circle
    /// (green tick when done, indigo number when current, grey otherwise) and a 13px caption
    /// (600 heading ink when current, 500 muted otherwise). The current step gets an indigo
    /// border and soft ring.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal sealed class PrintStepChip : SLControl
    {
        public const int ChipHeight = 50;   // 12 + 24 + 12 + 2px border

        public int Number { get; set; }
        public string Caption { get; set; }
        public bool Active { get; set; }
        public bool Done { get; set; }
        public bool Reachable { get; set; } = true;

        public PrintStepChip()
        {
            Height = ChipHeight;
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle box = new Rectangle(0, 0, Width, Height);
            SLPaint.Box(g, box, Theme.RadiusMd, Theme.SurfaceCard, Active ? Theme.ActionPrimary : Theme.BorderDefault);
            if (Active)
            {
                using (GraphicsPath p = SLPaint.RoundedRect(new RectangleF(2f, 2f, Width - 4f, Height - 4f), Theme.RadiusMd - 2))
                using (Pen pen = new Pen(Theme.FocusRingSoft, 2f))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.DrawPath(pen, p);
                }
            }

            Rectangle circle = new Rectangle(15, (Height - 24) / 2, 24, 24);
            Color fill = Done ? Theme.Ok500 : Active ? Theme.ActionPrimary : Theme.N100;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush b = new SolidBrush(fill)) g.FillEllipse(b, circle);
            if (Done)
                SLIcon.Draw(g, "check", new Rectangle(circle.X + 5, circle.Y + 5, 14, 14), Color.White, 2.4f);
            else
                SLPaint.Text(g, Number.ToString(), Theme.FontXsSemibold, circle, Active ? Color.White : Theme.N500, TextFormatFlags.HorizontalCenter);

            Font font = Active ? Theme.FontSmSemibold : Theme.FontSmMedium;
            Color ink = Active ? Theme.TextHeading : Theme.TextMuted;
            if (!Reachable && !Active) ink = Theme.N400;
            int x = circle.Right + 10;
            SLPaint.TextEllipsis(g, Caption, font, new Rectangle(x, 0, Math.Max(10, Width - x - 14), Height), ink, TextFormatFlags.Left);
        }
    }
}
