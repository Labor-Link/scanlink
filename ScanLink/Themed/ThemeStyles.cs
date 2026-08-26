using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace ScanLink.Themed
{
    /// <summary>
    /// Shared control styling, promoted out of Form1 so the dialogs can use the same
    /// treatment instead of each re-implementing it.
    ///
    /// Deliberately NOT included: setting Form.Font. A form-level font cascades to every
    /// child that has not set its own, which resizes labels inside dialogs whose control
    /// positions are hard-coded, clipping text. Type is applied per control type instead.
    /// </summary>
    internal static class ThemeStyles
    {
        private sealed class RadiusHolder { public int Value; }

        /// <summary>Radius per control. A ConditionalWeakTable, not a Dictionary: dialogs
        /// are constructed and destroyed repeatedly and a static dictionary would hold every
        /// disposed control alive for the life of the process.</summary>
        private static readonly ConditionalWeakTable<Control, RadiusHolder> Radii =
            new ConditionalWeakTable<Control, RadiusHolder>();

        /// <summary>Buttons already given a variant, so the sweep below skips them.</summary>
        private static readonly ConditionalWeakTable<Button, object> Styled =
            new ConditionalWeakTable<Button, object>();

        public static void RoundedCorners(Control control, int radius)
        {
            if (control == null || radius <= 0) return;
            RadiusHolder holder = Radii.GetOrCreateValue(control);
            holder.Value = radius;
            // Subscribing a static method group is idempotent: the delegates compare equal.
            control.Resize -= OnRoundedResize;
            control.Resize += OnRoundedResize;
            SetRoundedRegion(control, radius);
        }

        private static void OnRoundedResize(object sender, EventArgs e)
        {
            Control c = sender as Control;
            if (c == null) return;
            RadiusHolder holder;
            int radius = Radii.TryGetValue(c, out holder)
                ? holder.Value
                : ((c is Button) ? Theme.RadiusSm : Theme.RadiusLg);
            SetRoundedRegion(c, radius);
        }

        public static void SetRoundedRegion(Control control, int radius)
        {
            if (control == null || control.Width < 2 || control.Height < 2) return;
            using (GraphicsPath path = Theme.RoundedPath(new Rectangle(0, 0, control.Width, control.Height), radius))
            {
                control.Region = new Region(path);
            }
        }

        private static void Base(Button button, int minHeight)
        {
            Styled.Remove(button);
            Styled.Add(button, Styled);

            button.FlatStyle = FlatStyle.Flat;
            button.Font = Theme.FontSmBold;
            button.Padding = new Padding(Theme.S2, 0, Theme.S2, 0);
            button.UseVisualStyleBackColor = false;

            // No minimum WIDTH. Forcing one pushed buttons past the fixed columns of the
            // filter TableLayoutPanel, clipping "Apply"/"Clear" to "Ap"/"Clea".
            button.MinimumSize = new Size(button.MinimumSize.Width, minHeight);
            button.Height = Math.Max(button.Height, minHeight);

            FitToText(button);
            RoundedCorners(button, Theme.RadiusSm);
        }

        /// <summary>
        /// Widens a button so its caption cannot clip. Buttons here are laid out at
        /// hard-coded widths measured against the old font; a new font, an icon or a longer
        /// word silently truncates unless the width is recomputed.
        /// </summary>
        public static void FitToText(Button button)
        {
            if (button == null || button.AutoSize) return;
            if (string.IsNullOrEmpty(button.Text)) return;
            try
            {
                Size text = TextRenderer.MeasureText(button.Text, button.Font);
                int icon = (button.Image != null) ? button.Image.Width + Theme.S2 : 0;
                int needed = text.Width + icon + button.Padding.Horizontal + Theme.S4;
                if (button.Width < needed) button.Width = needed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[THEME] could not measure button: " + ex.Message);
            }
        }

        /// <summary>Indigo. One per view.</summary>
        public static void Primary(Button button)
        {
            if (button == null) return;
            Base(button, Theme.HeightMd);
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = Theme.ActionPrimary;
            button.ForeColor = Theme.TextOnAccent;
            button.FlatAppearance.MouseOverBackColor = Theme.ActionPrimaryHover;
            button.FlatAppearance.MouseDownBackColor = Theme.ActionPrimaryActive;
        }

        /// <summary>White with a 1px border — the design system's "secondary".</summary>
        public static void Secondary(Button button)
        {
            if (button == null) return;
            Base(button, Theme.HeightSm);
            button.BackColor = Theme.SurfaceCard;
            button.ForeColor = Theme.TextBody;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Theme.BorderStrong;
            button.FlatAppearance.MouseOverBackColor = Theme.SurfaceSunken;
            button.FlatAppearance.MouseDownBackColor = Theme.N200;
        }

        /// <summary>Navy fill — heavier than secondary.</summary>
        public static void Navy(Button button)
        {
            if (button == null) return;
            Base(button, Theme.HeightSm);
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = Theme.ActionSecondary;
            button.ForeColor = Theme.TextOnAccent;
            button.FlatAppearance.MouseOverBackColor = Theme.ActionSecondaryHover;
            button.FlatAppearance.MouseDownBackColor = Theme.ActionSecondaryHover;
        }

        /// <summary>Destructive but secondary — danger ink on white.</summary>
        public static void QuietDanger(Button button)
        {
            if (button == null) return;
            Base(button, Theme.HeightSm);
            button.BackColor = Theme.SurfaceCard;
            button.ForeColor = Theme.Err500;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Theme.Err500;
            button.FlatAppearance.MouseOverBackColor = Theme.Err50;
            button.FlatAppearance.MouseDownBackColor = Theme.Err50;
        }

        /// <summary>Solid danger — the confirming action in a destructive dialog.</summary>
        public static void Danger(Button button)
        {
            if (button == null) return;
            Base(button, Theme.HeightSm);
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = Theme.Err500;
            button.ForeColor = Theme.TextOnAccent;
            button.FlatAppearance.MouseOverBackColor = Theme.Err700;
            button.FlatAppearance.MouseDownBackColor = Theme.Err700;
        }

        /// <summary>
        /// Right-aligns a dialog's action buttons along the bottom, primary first.
        /// The variants enforce minimum heights and dialogs anchor buttons bottom-right at
        /// hard-coded coordinates; a button that grows under an anchor grows outward, so
        /// without re-laying-out the row the primary slides off the form.
        /// </summary>
        public static void ActionRow(Control container, int margin, params Button[] rightToLeft)
        {
            if (container == null || rightToLeft == null) return;
            int right = container.ClientSize.Width - margin;
            int bottom = container.ClientSize.Height - margin;
            foreach (Button button in rightToLeft)
            {
                if (button == null) continue;
                button.Height = Theme.HeightMd;
                button.MinimumSize = new Size(button.MinimumSize.Width, Theme.HeightMd);
                button.Top = bottom - button.Height;
                button.Left = right - button.Width;
                right = button.Left - Theme.S2;
            }
        }

        /// <summary>
        /// Applies the secondary variant to every button not given one explicitly, so
        /// buttons buried in panels nobody re-themed do not keep the old palette.
        /// </summary>
        public static void StyleRemainingButtons(Control root)
        {
            if (root == null) return;
            foreach (Control child in root.Controls)
            {
                Button button = child as Button;
                if (button != null)
                {
                    object marker;
                    if (!Styled.TryGetValue(button, out marker)) Secondary(button);
                }
                if (child.HasChildren) StyleRemainingButtons(child);
            }
        }

        /// <summary>
        /// Walks the tree and applies input tokens by control type. Walking rather than
        /// listing fields is deliberate: the print panels are reparented into popups at
        /// runtime, so a hard-coded list goes stale.
        ///
        /// Type size is deliberately NOT changed. Several panels place fixed-width boxes
        /// against fixed-width labels, and enlarging the text truncated captions.
        /// </summary>
        public static void Inputs(Control root)
        {
            if (root == null) return;
            foreach (Control child in root.Controls)
            {
                if (child is DataGridView) continue;   // grids paint their own cells

                TextBox textBox = child as TextBox;
                if (textBox != null)
                {
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    textBox.BackColor = Theme.SurfaceCard;
                    textBox.ForeColor = Theme.TextBody;
                    if (textBox.Multiline) textBox.Font = Theme.FontMono;  // log readouts
                }

                ComboBox comboBox = child as ComboBox;
                if (comboBox != null)
                {
                    comboBox.FlatStyle = FlatStyle.Flat;   // BackColor is ignored otherwise
                    comboBox.BackColor = Theme.SurfaceCard;
                    comboBox.ForeColor = Theme.TextBody;
                }

                DateTimePicker picker = child as DateTimePicker;
                if (picker != null)
                {
                    picker.CalendarMonthBackground = Theme.SurfaceCard;
                    picker.CalendarForeColor = Theme.TextBody;
                    picker.CalendarTitleBackColor = Theme.ActionPrimary;
                    picker.CalendarTitleForeColor = Theme.TextOnAccent;
                }

                NumericUpDown spinner = child as NumericUpDown;
                if (spinner != null)
                {
                    spinner.BorderStyle = BorderStyle.FixedSingle;
                    spinner.BackColor = Theme.SurfaceCard;
                    spinner.ForeColor = Theme.TextBody;
                }

                CheckBox checkBox = child as CheckBox;
                if (checkBox != null) checkBox.ForeColor = Theme.TextBody;

                if (child.HasChildren) Inputs(child);
            }
        }

        /// <summary>Applies the v2 table treatment. Safe on any grid in the app.</summary>
        public static void Grid(DataGridView grid)
        {
            if (grid == null) return;

            grid.BackgroundColor = Theme.SurfaceCard;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = Theme.BorderSubtle;
            grid.EnableHeadersVisualStyles = false;
            grid.RowHeadersVisible = false;
            grid.BackColor = Theme.SurfaceCard;

            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.SurfaceSunken;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.TextLabel;
            grid.ColumnHeadersDefaultCellStyle.Font = Theme.FontTableHeader;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(Theme.S2, 0, Theme.S2, 0);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.SurfaceSunken;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Theme.TextLabel;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = Theme.GridHeaderHeight;

            grid.DefaultCellStyle.BackColor = Theme.SurfaceCard;
            grid.DefaultCellStyle.ForeColor = Theme.TextBody;
            grid.DefaultCellStyle.Font = Theme.FontSm;
            grid.DefaultCellStyle.SelectionBackColor = Theme.GridSelection;
            grid.DefaultCellStyle.SelectionForeColor = Theme.TextHeading;
            grid.DefaultCellStyle.Padding = new Padding(Theme.S2, 0, Theme.S2, 0);

            grid.AlternatingRowsDefaultCellStyle.BackColor = Theme.SurfaceAltRow;
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = Theme.GridSelection;
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = Theme.TextHeading;

            grid.RowTemplate.Height = Theme.GridRowHeight;

            grid.ColumnAdded -= OnGridColumnAdded;
            grid.ColumnAdded += OnGridColumnAdded;

            // Designer-declared columns do not raise ColumnAdded again.
            foreach (DataGridViewColumn column in grid.Columns)
            {
                column.HeaderText = ToDisplayHeader(column.HeaderText);
            }
        }

        private static void OnGridColumnAdded(object sender, DataGridViewColumnEventArgs e)
        {
            if (e == null || e.Column == null) return;
            e.Column.HeaderText = ToDisplayHeader(e.Column.HeaderText);
        }

        /// <summary>
        /// Table headers are uppercase in v2, but columns are auto-generated from a
        /// DataTable and several carry raw field names ("SerialNumber"). Uppercasing those
        /// directly yields "SERIALNUMBER", so word boundaries are restored first.
        /// Presentation only — not v2's vocabulary change.
        /// </summary>
        public static string ToDisplayHeader(string header)
        {
            if (string.IsNullOrEmpty(header)) return header;
            System.Text.StringBuilder spaced = new System.Text.StringBuilder(header.Length + 8);
            for (int i = 0; i < header.Length; i++)
            {
                char ch = header[i];
                bool boundary = i > 0
                    && char.IsUpper(ch)
                    && (!char.IsUpper(header[i - 1]) || (i + 1 < header.Length && char.IsLower(header[i + 1])));
                if (boundary && spaced.Length > 0 && spaced[spaced.Length - 1] != ' ') spaced.Append(' ');
                spaced.Append(ch);
            }
            return spaced.ToString().ToUpperInvariant();
        }

        /// <summary>Shared dialog treatment. Buttons are left to the caller, because which
        /// one is primary is a per-dialog decision and only one is allowed.</summary>
        public static void DialogChrome(Form dialog)
        {
            if (dialog == null) return;
            dialog.BackColor = Theme.SurfaceApp;
            dialog.ForeColor = Theme.TextBody;
            Inputs(dialog);
            ApplyGridsRecursively(dialog);
        }

        private static void ApplyGridsRecursively(Control root)
        {
            foreach (Control child in root.Controls)
            {
                DataGridView grid = child as DataGridView;
                if (grid != null) { Grid(grid); continue; }
                if (child.HasChildren) ApplyGridsRecursively(child);
            }
        }
    }
}
