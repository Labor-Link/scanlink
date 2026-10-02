using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace ScanLink.DesignSystem
{
    /// <summary>
    /// Renders every reference scene with the SL* controls and saves &lt;id&gt;.png, so the
    /// WinForms output can be diffed against the mockup (design/reference/png/&lt;id&gt;.png).
    ///
    ///   ScanLink.exe --gallery out\actual
    ///   node design/tools/compare.js --actual out\actual
    ///
    /// Scene ids, widths and contents MUST match design/reference/gallery.html. When you add a
    /// component or migrate a screen, add a scene on both sides.
    /// </summary>
    internal static class SLGallery
    {
        private sealed class Scene
        {
            public string Id;
            public int Width;
            public bool AppBackground;
            public Func<Control> Build;
            public Func<Form> BuildDialog;
        }

        public static int Run(string outDir)
        {
            Directory.CreateDirectory(outDir);
            int failures = 0;
            foreach (Scene scene in Scenes())
            {
                try
                {
                    using (Bitmap bmp = scene.BuildDialog != null ? CaptureDialog(scene) : CaptureScene(scene))
                        bmp.Save(Path.Combine(outDir, scene.Id + ".png"), ImageFormat.Png);
                    Console.WriteLine("rendered " + scene.Id);
                }
                catch (Exception ex)
                {
                    failures++;
                    Console.Error.WriteLine("FAILED " + scene.Id + ": " + ex);
                    File.WriteAllText(Path.Combine(outDir, scene.Id + ".error.txt"), ex.ToString());
                }
            }
            return failures == 0 ? 0 : 1;
        }

        private static Bitmap CaptureScene(Scene scene)
        {
            Color bg = scene.AppBackground ? Theme.SurfaceApp : Theme.SurfaceCard;
            using (Form host = OffscreenForm(bg))
            {
                SLStack root = new SLStack(SLOrientation.Vertical, 0) { Padding = new Padding(24), BackColor = bg, Align = SLAlign.Start };
                Control content = scene.Build();
                root.Controls.Add(content);
                host.Controls.Add(root);
                // Block-level content stretches to the scene width like a <div>; inline buttons don't.
                root.Align = (content is SLButton && content.Tag as string != "block") ? SLAlign.Start : SLAlign.Stretch;

                int h = root.MeasureHeight(scene.Width);
                host.ClientSize = new Size(scene.Width, h);
                root.SetBounds(0, 0, scene.Width, h);
                host.Show();
                root.PerformLayout();
                h = root.MeasureHeight(scene.Width);
                host.ClientSize = new Size(scene.Width, h);
                root.SetBounds(0, 0, scene.Width, h);
                host.ActiveControl = null; // references are unfocused; a focused TextBox selects its text
                Application.DoEvents();
                host.Refresh();

                Bitmap bmp = new Bitmap(scene.Width, h, PixelFormat.Format32bppArgb);
                host.DrawToBitmap(bmp, new Rectangle(0, 0, scene.Width, h));
                host.Close();
                return bmp;
            }
        }

        private static Bitmap CaptureDialog(Scene scene)
        {
            using (Form dialog = scene.BuildDialog())
            {
                dialog.StartPosition = FormStartPosition.Manual;
                dialog.Location = new Point(-20000, -20000);
                dialog.Show();
                dialog.PerformLayout();
                dialog.ActiveControl = null;
                Application.DoEvents();
                dialog.Refresh();
                Bitmap bmp = new Bitmap(dialog.Width, dialog.Height, PixelFormat.Format32bppArgb);
                dialog.DrawToBitmap(bmp, new Rectangle(0, 0, dialog.Width, dialog.Height));
                dialog.Close();
                return bmp;
            }
        }

        private static Form OffscreenForm(Color bg)
        {
            return new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                Location = new Point(-20000, -20000),
                ShowInTaskbar = false,
                AutoScaleMode = AutoScaleMode.None,
                BackColor = bg
            };
        }

        private static SLStack Row(int gap, params Control[] children)
        {
            SLStack s = new SLStack(SLOrientation.Horizontal, gap) { Align = SLAlign.Center };
            s.AddRange(children);
            return s;
        }

        private static SLStack Column(int gap, params Control[] children)
        {
            SLStack s = new SLStack(SLOrientation.Vertical, gap);
            s.AddRange(children);
            return s;
        }

        private static SLComboBox Combo(string selected, params string[] options)
        {
            SLComboBox c = new SLComboBox();
            c.Items.AddRange(options);
            c.SelectedItem = selected;
            return c;
        }

        private static IEnumerable<Scene> Scenes()
        {
            yield return new Scene { Id = "button-primary", Width = 240, Build = () => new SLButton { Text = "Print labels", IconName = "printer" } };
            yield return new Scene { Id = "button-secondary", Width = 240, Build = () => new SLButton { Text = "Sync now", Variant = SLVariant.Secondary, IconName = "cloud-upload" } };
            yield return new Scene { Id = "button-ghost-sm", Width = 240, Build = () => new SLButton { Text = "See all scans", Variant = SLVariant.Ghost, ButtonSize = SLSize.Sm, IconEndName = "arrow-right" } };
            yield return new Scene { Id = "button-danger", Width = 240, Build = () => new SLButton { Text = "Remove scanner", Variant = SLVariant.Danger } };
            yield return new Scene { Id = "button-disabled", Width = 240, Build = () => new SLButton { Text = "Next", Enabled = false } };
            yield return new Scene
            {
                Id = "button-variants", Width = 520,
                Build = () => Row(8,
                    new SLButton { Text = "Navy", Variant = SLVariant.Navy },
                    new SLButton { Text = "Success", Variant = SLVariant.Success },
                    new SLButton { Text = "Clean up", Variant = SLVariant.DangerQuiet })
            };
            yield return new Scene
            {
                Id = "button-lg-block", Width = 428, AppBackground = true,
                Build = () => new SLButton { Text = "Sign in", ButtonSize = SLSize.Lg, AutoSize = false, Tag = "block", Height = Theme.HeightLg }
            };
            yield return new Scene
            {
                Id = "iconbuttons", Width = 240,
                Build = () => Row(4,
                    new SLIconButton { IconName = "pencil", ButtonSize = SLSize.Sm, Label = "Edit" },
                    new SLIconButton { IconName = "trash-2", ButtonSize = SLSize.Sm, Variant = SLIconButtonVariant.Danger, Label = "Remove" },
                    new SLIconButton { IconName = "refresh-cw", Variant = SLIconButtonVariant.Solid, Label = "Refresh" })
            };
            yield return new Scene
            {
                Id = "badges", Width = 520,
                Build = () => Row(8,
                    new SLBadge("Working", SLTone.Success, true), new SLBadge("Waiting", SLTone.Warning, true),
                    new SLBadge("Offline", SLTone.Error, true), new SLBadge("Syncing", SLTone.Info, true),
                    new SLBadge("Live", SLTone.Brand, false), new SLBadge("Draft", SLTone.Neutral, false))
            };
            yield return new Scene
            {
                Id = "banner-warning", Width = 720, AppBackground = true,
                Build = () => new SLBanner
                {
                    Tone = SLTone.Warning, IconName = "cloud-off", Title = "42 scans haven't reached the cloud yet",
                    Message = "They're safely saved on this computer. Sync them before you clean up local scans.",
                    Action = new SLButton { Text = "Sync now", Variant = SLVariant.Secondary, ButtonSize = SLSize.Sm }
                }
            };
            yield return new Scene
            {
                Id = "banner-error", Width = 720, AppBackground = true,
                Build = () => new SLBanner
                {
                    Tone = SLTone.Error, IconName = "unplug", Title = "Line 2 scanner isn't answering",
                    Message = "Check that it's plugged in and switched on, then look again. Scans already saved are not affected.",
                    Action = new SLButton { Text = "Look again", Variant = SLVariant.Secondary, ButtonSize = SLSize.Sm }
                }
            };
            yield return new Scene
            {
                Id = "textfield-hint", Width = 420,
                Build = () => new SLField("Printer address", new SLTextBox { Mono = true, Text = "192.168.1.44:9100" }) { Hint = "You'll find this printed on the label on the printer." }
            };
            yield return new Scene
            {
                Id = "textfield-required-lg", Width = 428, AppBackground = true,
                Build = () => new SLField("Email address", new SLTextBox { FieldSize = SLSize.Lg, PlaceholderText = "you@packhouse.co" }) { Required = true }
            };
            yield return new Scene
            {
                Id = "textfield-error", Width = 420,
                Build = () => new SLField("Email address", new SLTextBox { Text = "jmokoena@" }) { Required = true, Error = "Enter the email address you use for ScanLink." }
            };
            yield return new Scene
            {
                Id = "select", Width = 420,
                Build = () => new SLField("Connection", Combo("Network (LAN)", "Network (LAN)", "USB cable", "Serial cable (COM)", "Save to a file")) { Hint = "Network is the usual choice in a packhouse." }
            };
            yield return new Scene
            {
                Id = "numberfield", Width = 420,
                Build = () => new SLField("Avg weight", new SLNumberBox { Unit = "kg", Maximum = 1000, Value = 4 })
            };
            yield return new Scene
            {
                Id = "checks", Width = 420,
                Build = () => Column(16,
                    new SLCheckBox { Text = "Keep me signed in", Checked = true },
                    new SLCheckBox { Text = "Print two labels per row", Description = "Uses the 2-up label roll." },
                    new SLToggle { Text = "Open ScanLink on this page", Description = "Otherwise ScanLink opens on Scans.", Checked = true },
                    new SLToggle { Text = "Advanced settings" })
            };
            yield return new Scene
            {
                Id = "segmented", Width = 520,
                Build = () => Row(0, new SLSegmentedControl("Today", "Last 7 days", "This season", "Custom"))
            };
            yield return new Scene { Id = "card-equipment", Width = 440, AppBackground = true, Build = EquipmentCard };
            yield return new Scene { Id = "card-form", Width = 640, AppBackground = true, Build = FormCard };
            yield return new Scene
            {
                Id = "emptystate", Width = 520,
                Build = () => new SLEmptyState
                {
                    IconName = "search-x", Title = "No scans match that search",
                    Description = "Try a shorter search, or widen the date range to Last 7 days.",
                    Action = new SLButton { Text = "Clear filters", Variant = SLVariant.Secondary, ButtonSize = SLSize.Sm }
                }
            };
            yield return new Scene { Id = "table", Width = 720, Build = ScansTable };
            yield return new Scene { Id = "dialog-remove", BuildDialog = RemoveDialog };
            yield return new Scene { Id = "dialog-printer", BuildDialog = PrinterDialog };
            yield return new Scene { Id = "dialog-scan", BuildDialog = ScanDialog };

            // C#-only scenes (no mockup counterpart): rendered for review, not scored.
            yield return new Scene { Id = "app-add-combination", BuildDialog = () => new AddCombinationDialog(new ProductCombinationsService(new ApiAuthService())) };
            yield return new Scene { Id = "app-setup-dialog", BuildDialog = () => new SetupDialog(new ProductCombinationsService(new ApiAuthService())) };
            yield return new Scene { Id = "app-error-dialog", BuildDialog = () => ErrorDialog.Create("Upload failed", "System.Net.WebException: The remote name could not be resolved: 'api.scanlink.app'\r\n   at ScanLogUploadService.UploadAsync()") };
        }

        private static Control EquipmentCard()
        {
            SLCard card = new SLCard { Title = "Equipment", Subtitle = "Green means it's sending data right now.", BodyPadding = new Padding(8), };
            card.Body.Gap = 0;
            card.Actions.Controls.Add(new SLButton { Text = "Manage", Variant = SLVariant.Ghost, ButtonSize = SLSize.Sm });
            string[][] rows = { new[] { "Line 3 scanner", "SL-HH-02 · COM3", "Working", "s" }, new[] { "Line 2 scanner", "SL-HH-11 · COM7", "Offline", "e" } };
            foreach (string[] r in rows)
            {
                SLStack text = Column(1,
                    new SLText(r[0], SLTextStyle.BodySmMedium) { LineHeight = 1.3f },
                    new SLText(r[1], SLTextStyle.MonoCaption));
                SLStack row = Row(12, text, new SLBadge(r[2], r[3] == "s" ? SLTone.Success : SLTone.Error, true));
                row.Padding = new Padding(12, 10, 12, 10);
                row.SetGrow(text);
                card.Body.Controls.Add(row);
            }
            return card;
        }

        private static Control FormCard()
        {
            SLCard card = new SLCard { Title = "What are you labelling?", Subtitle = "Pick the crop first — the product list narrows to match." };
            SLFieldSet fields = new SLFieldSet { Columns = 2 };
            fields.Add(new SLField("Crop", Combo("Avocado (01)", "Avocado (01)", "Citrus (02)")) { Required = true });
            fields.Add(new SLField("Product", Combo("Hass Loose (118)", "Hass Loose (118)", "Valencia 88 (204)")) { Required = true });
            card.Body.Controls.Add(fields);
            card.Footer.Controls.Add(new SLButton { Text = "Next: how many?", IconEndName = "arrow-right" });
            return card;
        }

        private static Control ScansTable()
        {
            SLTable table = new SLTable { Height = SLTableStyle.HeaderHeight + 4 * SLTableStyle.BadgeRowHeight };
            DataTable data = new DataTable();
            foreach (string c in new[] { "Serial", "Time", "Block", "Picked by", "Status" }) data.Columns.Add(c);
            data.Rows.Add("SC-0093-AA", "07:14", "14", "J. Mokoena", "Synced");
            data.Rows.Add("SC-0093-AB", "07:15", "14", "J. Mokoena", "Synced");
            data.Rows.Add("SC-0094-AC", "07:16", "07", "P. Naidoo", "Waiting");
            data.Rows.Add("SC-0094-AD", "07:17", "07", "P. Naidoo", "Waiting");
            table.DataSource = data;
            table.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            table.DataBindingComplete += (s, e) =>
            {
                int[] widths = { 160, 96, 110, 148, 158 };
                for (int i = 0; i < table.Columns.Count && i < widths.Length; i++) table.Columns[i].Width = widths[i];
                table.ClearSelection();
                if (table.Rows.Count > 1) table.Rows[1].Selected = true;
            };
            table.SetMono("Serial");
            table.SetMuted("Time");
            table.SetBadge("Status", v => Convert.ToString(v) == "Synced" ? SLTone.Success : SLTone.Warning);
            return table;
        }

        private static Form RemoveDialog()
        {
            SLDialog d = new SLDialog
            {
                Title = "Remove this scanner?", Tone = SLDialogTone.Danger, DialogWidth = 440,
                Description = "Line 2 scanner (SL-HH-11) will stop sending scans to this site. You can add it again later."
            };
            d.AddAction(new SLButton { Text = "Keep it", Variant = SLVariant.Secondary, DialogResult = DialogResult.Cancel });
            d.AddAction(new SLButton { Text = "Remove scanner", Variant = SLVariant.Danger, DialogResult = DialogResult.OK });
            return d;
        }

        private static Form PrinterDialog()
        {
            SLDialog d = new SLDialog
            {
                Title = "How is the printer connected?", DialogWidth = 480,
                Description = "Choose how this computer reaches the label printer. If you're not sure, ask whoever set it up."
            };
            SLFieldSet fields = new SLFieldSet { Columns = 1 };
            fields.Add(new SLField("Connection", Combo("Network (LAN)", "Network (LAN)", "USB cable", "Serial cable (COM)", "Save to a file")) { Hint = "Network is the usual choice in a packhouse." });
            fields.Add(new SLField("Printer address", new SLTextBox { Mono = true, Text = "192.168.1.44:9100" }) { Hint = "You'll find this printed on the label on the printer." });
            d.Body.Controls.Add(fields);
            d.AddAction(new SLButton { Text = "Cancel", Variant = SLVariant.Secondary, DialogResult = DialogResult.Cancel });
            d.AddAction(new SLButton { Text = "Save connection" });
            return d;
        }

        private static Form ScanDialog()
        {
            SLDialog d = new SLDialog { Title = "SC-0093-AA", Description = "Everything recorded when this carton was scanned.", DialogWidth = 460 };
            SLKeyValueList kv = new SLKeyValueList();
            kv.Add("Time", "07:14"); kv.Add("Block", "14"); kv.Add("Line", "1");
            kv.Add("Picked by", "J. Mokoena"); kv.Add("Supplier", "Rooidraai"); kv.Add("Product", "Hass Loose");
            d.Body.Controls.Add(kv);
            SLStack badgeRow = Row(0, new SLBadge("Sent to the cloud", SLTone.Success, true));
            d.Body.Controls.Add(badgeRow);
            d.AddAction(new SLButton { Text = "Close", Variant = SLVariant.Secondary, DialogResult = DialogResult.Cancel });
            d.AddAction(new SLButton { Text = "Reprint this label", IconName = "printer" });
            return d;
        }
    }
}
