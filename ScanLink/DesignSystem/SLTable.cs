using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace ScanLink.DesignSystem
{
    /// <summary>
    /// DataTable.js for a DataGridView. New code: use SLTable. Existing grids: call
    /// SLTableStyle.Apply(grid) (ThemeStyles.Grid does this) — it paints every cell itself.
    ///
    ///   Header  37px, #F7F8FA, 12px/600 UPPERCASE muted ink, 16px side padding, 1px #E4E7EC bottom.
    ///   Rows    43px (49px when there is a badge column), white; hover #F7F8FA; selected #EFEFFD; 13px body ink; 16px side padding;
    ///           1px #F1F3F7 divider. No zebra striping.
    ///   Columns SetMono (Consolas — serials), SetMuted (grey — times), SetBadge (status pill),
    ///           SetIconAction (a DataGridViewButtonColumn drawn as an SLIconButton: pencil, trash-2).
    ///   Empty   set EmptyState; it replaces the rows when the grid has none.
    /// </summary>
    [DesignerCategory("Code")]
    internal class SLTable : DataGridView
    {
        public SLTable()
        {
            SLTableStyle.Apply(this);
            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AllowUserToResizeRows = false;
            ReadOnly = true;
            SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            MultiSelect = false;
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        /// <summary>Shown instead of the rows while the grid is empty (the mockup's empty prop).</summary>
        public SLEmptyState EmptyState
        {
            get { return SLTableStyle.GetEmpty(this); }
            set { SLTableStyle.SetEmpty(this, value); }
        }

        public void SetMono(string column) { SLTableStyle.SetMono(this, column); }
        public void SetMuted(string column) { SLTableStyle.SetMuted(this, column); }
        public void SetBadge(string column, Func<object, SLTone> toneFor, bool dot = true) { SLTableStyle.SetBadge(this, column, toneFor, dot); }

        /// <summary>Draws a button column as an icon button (the mockup's row actions). Clicks
        /// still arrive through CellContentClick.</summary>
        public void SetIconAction(string column, string iconName, bool danger = false) { SLTableStyle.SetIconAction(this, column, iconName, danger); }
    }

    internal static class SLTableStyle
    {
        public const int HeaderHeight = 37;
        public const int RowHeight = 43;
        /// <summary>Rows in a table with a badge column: the 24px pill sets the height.</summary>
        public const int BadgeRowHeight = 49;
        public const int CellPadX = 16;

        private sealed class State
        {
            public int HoverRow = -1;
            public int HoverCol = -1;
            public readonly Dictionary<string, KeyValuePair<string, bool>> IconActions = new Dictionary<string, KeyValuePair<string, bool>>(StringComparer.OrdinalIgnoreCase);
            public readonly HashSet<string> Mono = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public readonly HashSet<string> Muted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<string, Func<object, SLTone>> Badges = new Dictionary<string, Func<object, SLTone>>(StringComparer.OrdinalIgnoreCase);
            public readonly HashSet<string> BadgeNoDot = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public SLEmptyState Empty;
        }

        private static readonly ConditionalWeakTable<DataGridView, State> States = new ConditionalWeakTable<DataGridView, State>();

        private static State S(DataGridView g) { return States.GetOrCreateValue(g); }

        public static void Apply(DataGridView grid)
        {
            if (grid == null) return;
            // Combo columns show plain text until edited (painted with a chevron above).
            foreach (DataGridViewColumn c in grid.Columns)
            {
                c.HeaderText = DisplayHeader(c.HeaderText);
                DataGridViewComboBoxColumn cc = c as DataGridViewComboBoxColumn;
                if (cc != null) { cc.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing; cc.FlatStyle = FlatStyle.Flat; }
            }
            State st;
            bool already = States.TryGetValue(grid, out st);
            S(grid);

            grid.BackgroundColor = Theme.SurfaceCard;
            grid.BackColor = Theme.SurfaceCard;   // children (the empty state) sit on this
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = Theme.BorderSubtle;
            grid.EnableHeadersVisualStyles = false;
            grid.RowHeadersVisible = false;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = HeaderHeight;
            grid.RowTemplate.Height = RowHeightOf(grid);
            grid.AllowUserToResizeRows = false;

            grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.N50;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.TextMuted;
            grid.ColumnHeadersDefaultCellStyle.Font = Theme.FontXsSemibold;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.N50;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(CellPadX, 0, CellPadX, 0);

            grid.DefaultCellStyle.BackColor = Theme.SurfaceCard;
            grid.DefaultCellStyle.ForeColor = Theme.TextBody;
            grid.DefaultCellStyle.Font = Theme.FontSm;
            grid.DefaultCellStyle.SelectionBackColor = Theme.Indigo50;
            grid.DefaultCellStyle.SelectionForeColor = Theme.TextBody;
            grid.DefaultCellStyle.Padding = new Padding(CellPadX, 0, CellPadX, 0);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Theme.SurfaceCard;
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = Theme.Indigo50;
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = Theme.TextBody;

            foreach (DataGridViewRow row in grid.Rows) row.Height = RowHeightOf(grid);

            if (already) return;
            grid.ColumnAdded += (s, e) =>
            {
                // Headers are painted UPPERCASE; store them that way so auto-sized columns
                // measure the text that is actually drawn.
                e.Column.HeaderText = DisplayHeader(e.Column.HeaderText);
                DataGridViewComboBoxColumn cc = e.Column as DataGridViewComboBoxColumn;
                if (cc != null) { cc.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing; cc.FlatStyle = FlatStyle.Flat; }
            };
            grid.CellPainting += OnCellPainting;
            // Drop-down cells read as text until edited, so one click must also open the list
            // (otherwise the first click only starts editing and a second opens it).
            grid.EditingControlShowing += (s, e) =>
            {
                ComboBox editor = e.Control as ComboBox;
                if (editor == null || !grid.IsHandleCreated) return;
                if ((Control.MouseButtons & MouseButtons.Left) == 0) return;   // keyboard entry: leave it closed
                grid.BeginInvoke((Action)(() => { if (!editor.IsDisposed && editor.Visible) editor.DroppedDown = true; }));
            };
            grid.CellMouseEnter += OnCellMouseEnter;
            grid.MouseLeave += OnMouseLeave;
            grid.RowsAdded += (s, e) => { SyncEmpty(grid); };
            grid.RowsRemoved += (s, e) => SyncEmpty(grid);
            grid.DataBindingComplete += (s, e) => { ApplyRowHeight(grid); SyncEmpty(grid); };
            grid.Resize += (s, e) => SyncEmpty(grid);
            EnableDoubleBuffering(grid);
        }

        private static void EnableDoubleBuffering(DataGridView grid)
        {
            try
            {
                typeof(DataGridView).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(grid, true, null);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Header text as painted: words split ("SerialNumber" -> "Serial Number") and then
        /// uppercased. Uppercasing first lost the word boundaries ("SERIALNUMBER").
        /// </summary>
        public static string DisplayHeader(string header)
        {
            if (string.IsNullOrEmpty(header)) return header;
            System.Text.StringBuilder sb = new System.Text.StringBuilder(header.Length + 8);
            for (int i = 0; i < header.Length; i++)
            {
                char ch = header[i];
                bool boundary = i > 0 && char.IsUpper(ch) && header[i - 1] != ' '
                    && (char.IsLower(header[i - 1]) || (i + 1 < header.Length && char.IsLower(header[i + 1])));
                if (boundary) sb.Append(' ');
                sb.Append(ch == '_' ? ' ' : ch);
            }
            return sb.ToString().ToUpperInvariant();
        }

        public static void SetMono(DataGridView g, string column) { S(g).Mono.Add(column); g.Invalidate(); }
        public static void SetMuted(DataGridView g, string column) { S(g).Muted.Add(column); g.Invalidate(); }

        public static void SetBadge(DataGridView g, string column, Func<object, SLTone> toneFor, bool dot)
        {
            State st = S(g);
            st.Badges[column] = toneFor ?? (o => SLTone.Neutral);
            if (!dot) st.BadgeNoDot.Add(column); else st.BadgeNoDot.Remove(column);
            ApplyRowHeight(g);
            g.Invalidate();
        }

        /// <summary>43px, or 49px when a badge column is present (as DataTable.js lays out).</summary>
        public static int RowHeightOf(DataGridView g)
        {
            State st;
            return States.TryGetValue(g, out st) && st.Badges.Count > 0 ? BadgeRowHeight : RowHeight;
        }

        private static void ApplyRowHeight(DataGridView g)
        {
            int h = RowHeightOf(g);
            g.RowTemplate.Height = h;
            foreach (DataGridViewRow row in g.Rows) row.Height = h;
        }

        public static void SetIconAction(DataGridView g, string column, string iconName, bool danger)
        {
            S(g).IconActions[column] = new KeyValuePair<string, bool>(iconName, danger);
            g.Invalidate();
        }

        public static SLEmptyState GetEmpty(DataGridView g) { return S(g).Empty; }

        public static void SetEmpty(DataGridView g, SLEmptyState empty)
        {
            State st = S(g);
            if (st.Empty != null) g.Controls.Remove(st.Empty);
            st.Empty = empty;
            if (empty != null)
            {
                empty.BackColor = Theme.SurfaceCard;
                g.Controls.Add(empty);
            }
            SyncEmpty(g);
        }

        private static void SyncEmpty(DataGridView g)
        {
            State st;
            if (!States.TryGetValue(g, out st) || st.Empty == null) return;
            bool show = g.Rows.Count == 0;
            st.Empty.Visible = show;
            if (show)
            {
                int top = g.ColumnHeadersVisible ? g.ColumnHeadersHeight : 0;
                st.Empty.SetBounds(0, top, g.ClientSize.Width, Math.Max(0, g.ClientSize.Height - top));
                st.Empty.BringToFront();
            }
        }

        private static void OnCellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView g = (DataGridView)sender;
            State st = S(g);
            if (st.HoverRow == e.RowIndex && st.HoverCol == e.ColumnIndex) return;
            st.HoverCol = e.ColumnIndex;
            int old = st.HoverRow;
            st.HoverRow = e.RowIndex;
            if (old >= 0 && old < g.Rows.Count) g.InvalidateRow(old);
            if (e.RowIndex >= 0) g.InvalidateRow(e.RowIndex);
        }

        private static void OnMouseLeave(object sender, EventArgs e)
        {
            DataGridView g = (DataGridView)sender;
            State st = S(g);
            int old = st.HoverRow;
            st.HoverRow = -1;
            st.HoverCol = -1;
            if (old >= 0 && old < g.Rows.Count) g.InvalidateRow(old);
        }

        private static void OnCellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            DataGridView g = (DataGridView)sender;
            State st = S(g);
            Graphics gr = e.Graphics;
            Rectangle r = e.CellBounds;

            if (e.RowIndex == -1)
            {
                // Header cell.
                if (e.ColumnIndex < 0) return;
                using (SolidBrush b = new SolidBrush(Theme.N50)) gr.FillRectangle(b, r);
                using (Pen p = new Pen(Theme.BorderDefault)) gr.DrawLine(p, r.Left, r.Bottom - 1, r.Right, r.Bottom - 1);
                string text = Convert.ToString(e.FormattedValue ?? e.Value);
                DataGridViewColumn col = g.Columns[e.ColumnIndex];
                TextFormatFlags align = AlignOf(col);
                Rectangle tr = new Rectangle(r.X + CellPadX, r.Y, r.Width - CellPadX * 2, r.Height - 1);
                SLPaint.TextEllipsis(gr, (text ?? "").ToUpperInvariant(), Theme.FontXsSemibold, tr, Theme.TextMuted, align);
                e.Handled = true;
                return;
            }
            if (e.ColumnIndex < 0) return;

            DataGridViewColumn column = g.Columns[e.ColumnIndex];
            bool selected = (e.State & DataGridViewElementStates.Selected) != 0;
            Color bg = selected ? Theme.Indigo50 : st.HoverRow == e.RowIndex ? Theme.N50 : Theme.SurfaceCard;
            // Existing code marks rows/cells with a background (status rows, disabled cells).
            // Keep that meaning, mapped onto the v2 palette.
            Color explicitBg = ExplicitBackColor(g, e.RowIndex, e.ColumnIndex);
            if (!explicitBg.IsEmpty && !selected) bg = explicitBg;
            using (SolidBrush b = new SolidBrush(bg)) gr.FillRectangle(b, r);
            using (Pen p = new Pen(Theme.BorderSubtle)) gr.DrawLine(p, r.Left, r.Bottom - 1, r.Right, r.Bottom - 1);

            Rectangle content = new Rectangle(r.X + CellPadX, r.Y, Math.Max(0, r.Width - CellPadX * 2), r.Height - 1);
            string name = column.Name ?? "";
            string dataName = column.DataPropertyName ?? "";

            Func<object, SLTone> toneFor;
            if (st.Badges.TryGetValue(name, out toneFor) || st.Badges.TryGetValue(dataName, out toneFor))
            {
                string text = Convert.ToString(e.FormattedValue);
                if (!string.IsNullOrEmpty(text))
                {
                    bool dot = !st.BadgeNoDot.Contains(name) && !st.BadgeNoDot.Contains(dataName);
                    Size bs = SLBadge.Measure(text, dot);
                    int bx = content.X;
                    if ((AlignOf(column) & TextFormatFlags.Right) != 0) bx = content.Right - bs.Width;
                    SLBadge.PaintBadge(gr, new Rectangle(bx, content.Y + (content.Height - bs.Height) / 2, bs.Width, bs.Height), text, toneFor(e.Value), dot);
                }
                e.Handled = true;
                return;
            }

            KeyValuePair<string, bool> action;
            if (st.IconActions.TryGetValue(name, out action) && g.Rows[e.RowIndex].Cells[e.ColumnIndex] is DataGridViewButtonCell)
            {
                // IconButton.js, size sm: 30x30, radius 6; ghost = grey icon, danger = red icon,
                // hover wash grey / red-50.
                Rectangle btn = new Rectangle(content.X + (content.Width - 30) / 2, content.Y + (content.Height - 30) / 2, 30, 30);
                if ((AlignOf(column) & TextFormatFlags.HorizontalCenter) == 0 && (AlignOf(column) & TextFormatFlags.Right) != 0)
                    btn.X = content.Right - 30;
                bool hot = st.HoverRow == e.RowIndex && st.HoverCol == e.ColumnIndex;
                if (hot) SLPaint.Box(gr, btn, Theme.RadiusSm, action.Value ? Theme.Err50 : Theme.N100, Color.Empty);
                SLIcon.Draw(gr, action.Key, new Rectangle(btn.X + 7, btn.Y + 7, 16, 16), action.Value ? Theme.Err500 : Theme.N500);
                e.Handled = true;
                return;
            }

            DataGridViewComboBoxCell combo = g.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewComboBoxCell;
            if (combo != null)
            {
                // A drop-down cell reads as text with a small chevron, like the rest of the
                // table; the stock drop-down editor still opens when the cell is clicked.
                bool locked = combo.ReadOnly || g.ReadOnly || column.ReadOnly;
                Rectangle textRect = new Rectangle(content.X, content.Y, Math.Max(0, content.Width - (locked ? 0 : 18)), content.Height);
                SLPaint.TextEllipsis(gr, Convert.ToString(e.FormattedValue), Theme.FontSm, textRect, locked ? Theme.TextMuted : Theme.TextBody, TextFormatFlags.Left);
                if (!locked)
                    SLIcon.Draw(gr, "chevron-down", new Rectangle(content.Right - 14, content.Y + (content.Height - 14) / 2, 14, 14), Theme.N400);
                e.Handled = true;
                return;
            }

            if (!(g.Rows[e.RowIndex].Cells[e.ColumnIndex] is DataGridViewTextBoxCell))
            {
                // Combo, check, image, button and link cells: our row background, the cell's
                // own content (drop-down arrow, tick box…), so editing still works.
                e.Paint(r, DataGridViewPaintParts.ContentBackground | DataGridViewPaintParts.ContentForeground |
                           DataGridViewPaintParts.ErrorIcon);
                e.Handled = true;
                return;
            }

            bool mono = st.Mono.Contains(name) || st.Mono.Contains(dataName);
            bool muted = st.Muted.Contains(name) || st.Muted.Contains(dataName);
            Font font = mono ? Theme.FontMonoBody : Theme.FontSm;
            Color ink = muted ? Theme.TextMuted : Theme.TextBody;
            if (e.CellStyle != null && e.CellStyle.ForeColor != g.DefaultCellStyle.ForeColor && !e.CellStyle.ForeColor.IsEmpty && !muted)
                ink = e.CellStyle.ForeColor; // honour per-cell status colours set by existing code
            SLPaint.TextEllipsis(gr, Convert.ToString(e.FormattedValue), font, content, ink, AlignOf(column));
            e.Handled = true;
        }

        /// <summary>A background set by code on the cell or row (not our defaults), mapped to v2 tones.</summary>
        private static Color ExplicitBackColor(DataGridView g, int row, int col)
        {
            DataGridViewRow r = g.Rows[row];
            Color c = r.Cells[col].HasStyle ? r.Cells[col].Style.BackColor : Color.Empty;
            if (c.IsEmpty && r.HasDefaultCellStyle) c = r.DefaultCellStyle.BackColor;
            if (c.IsEmpty) return Color.Empty;
            int argb = c.ToArgb();
            if (argb == Theme.SurfaceCard.ToArgb() || argb == SystemColors.Window.ToArgb() || argb == Color.White.ToArgb()) return Color.Empty;
            if (argb == Color.LightGreen.ToArgb() || argb == Color.PaleGreen.ToArgb()) return Theme.Ok50;
            if (argb == Color.LightCoral.ToArgb() || argb == Color.MistyRose.ToArgb() || argb == Color.Salmon.ToArgb()) return Theme.Err50;
            if (argb == Color.LightYellow.ToArgb() || argb == Color.Khaki.ToArgb()) return Theme.Warn50;
            if (argb == Color.LightGray.ToArgb() || argb == Color.Gainsboro.ToArgb()) return Theme.N100;
            return c;
        }

        private static TextFormatFlags AlignOf(DataGridViewColumn col)
        {
            DataGridViewContentAlignment a = col.DefaultCellStyle.Alignment;
            if (a == DataGridViewContentAlignment.NotSet) a = col.InheritedStyle != null ? col.InheritedStyle.Alignment : a;
            switch (a)
            {
                case DataGridViewContentAlignment.TopRight:
                case DataGridViewContentAlignment.MiddleRight:
                case DataGridViewContentAlignment.BottomRight:
                    return TextFormatFlags.Right;
                case DataGridViewContentAlignment.TopCenter:
                case DataGridViewContentAlignment.MiddleCenter:
                case DataGridViewContentAlignment.BottomCenter:
                    return TextFormatFlags.HorizontalCenter;
                default:
                    return TextFormatFlags.Left;
            }
        }
    }
}
