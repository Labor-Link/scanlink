using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScanLink.DesignSystem
{
    internal enum SLOrientation { Vertical, Horizontal }

    /// <summary>Cross-axis alignment (CSS align-items).</summary>
    internal enum SLAlign { Stretch, Start, Center, End }

    /// <summary>Main-axis distribution (CSS justify-content).</summary>
    internal enum SLJustify { Start, Center, End, SpaceBetween }

    /// <summary>
    /// The CSS flexbox of this library. Every mockup <c>display:flex</c> block becomes an SLStack:
    ///   &lt;div style={{display:"flex", flexDirection:"column", gap:"16px"}}&gt;  ->  new SLStack { Gap = 16 }
    ///   &lt;div style={{display:"flex", gap:"8px", justifyContent:"flex-end"}}&gt; ->  new SLStack { Orientation = Horizontal, Gap = 8, Justify = SLJustify.End }
    ///   flex: 1 on a child                                                      ->  stack.SetGrow(child)
    ///
    /// Children are sized from what they report (ISLMeasure, AutoSize) — never from hard-coded
    /// Location/Size — so text changes and new fonts cannot clip or overlap.
    /// Background is transparent: whatever the parent paints shows through.
    /// </summary>
    [DesignerCategory("Code")]
    internal class SLStack : Panel, ISLMeasure
    {
        private SLOrientation _orientation = SLOrientation.Vertical;
        private int _gap;
        private SLAlign _align = SLAlign.Stretch;
        private SLJustify _justify = SLJustify.Start;
        private readonly HashSet<Control> _grow = new HashSet<Control>();
        private int _lastMeasured = -1;
        private bool _inLayout;


        public SLStack()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Color.Transparent;
        }

        public SLStack(SLOrientation orientation, int gap) : this() { _orientation = orientation; _gap = gap; }

        public SLOrientation Orientation { get { return _orientation; } set { _orientation = value; PerformLayout(); } }
        public int Gap { get { return _gap; } set { _gap = value; PerformLayout(); } }
        public SLAlign Align { get { return _align; } set { _align = value; PerformLayout(); } }
        public SLJustify Justify { get { return _justify; } set { _justify = value; PerformLayout(); } }

        /// <summary>CSS flex: 1 — the child takes the remaining main-axis space.</summary>
        public void SetGrow(Control child, bool grow = true)
        {
            if (grow) _grow.Add(child); else _grow.Remove(child);
            PerformLayout();
        }

        /// <summary>Adds children in order (Controls.Add z-order is otherwise reversed for Dock; here order = visual order).</summary>
        public void AddRange(params Control[] children)
        {
            SuspendLayout();
            foreach (Control c in children) Controls.Add(c);
            ResumeLayout(true);
        }

        private List<Control> Shown()
        {
            List<Control> list = new List<Control>();
            foreach (Control c in Controls) if (IsShown(c)) list.Add(c);
            return list;
        }

        /// <summary>Visible ignoring ancestors (Control.Visible is false until the form shows).</summary>
        private static bool IsShown(Control c)
        {
            // GetState(STATE_VISIBLE) is internal; the public proxy is the Visible setter we track.
            return SLVisibility.IsSet(c);
        }

        public int MeasureHeight(int width)
        {
            List<Control> kids = Shown();
            int inner = width - Padding.Horizontal;
            if (_orientation == SLOrientation.Vertical)
            {
                int h = 0;
                foreach (Control c in kids)
                {
                    int cw = _align == SLAlign.Stretch ? inner : Math.Min(inner, SLLayout.WidthOf(c));
                    h += SLLayout.HeightOf(c, cw);
                }
                if (kids.Count > 1) h += _gap * (kids.Count - 1);
                return h + Padding.Vertical;
            }
            else
            {
                int[] widths = MainSizes(kids, inner);
                int h = 0;
                for (int i = 0; i < kids.Count; i++) h = Math.Max(h, SLLayout.HeightOf(kids[i], widths[i]));
                return h + Padding.Vertical;
            }
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            List<Control> kids = Shown();
            if (_orientation == SLOrientation.Horizontal)
            {
                int w = 0;
                foreach (Control c in kids) w += SLLayout.WidthOf(c);
                if (kids.Count > 1) w += _gap * (kids.Count - 1);
                w += Padding.Horizontal;
                return new Size(w, MeasureHeight(w));
            }
            int mw = 0;
            foreach (Control c in kids) mw = Math.Max(mw, SLLayout.WidthOf(c));
            mw += Padding.Horizontal;
            int width = proposedSize.Width > 0 ? proposedSize.Width : mw;
            return new Size(mw, MeasureHeight(width));
        }

        private int[] MainSizes(List<Control> kids, int inner)
        {
            int[] widths = new int[kids.Count];
            int fixedTotal = 0, growCount = 0;
            for (int i = 0; i < kids.Count; i++)
            {
                if (_grow.Contains(kids[i])) { growCount++; continue; }
                widths[i] = SLLayout.WidthOf(kids[i]);
                fixedTotal += widths[i];
            }
            int gaps = kids.Count > 1 ? _gap * (kids.Count - 1) : 0;
            int free = Math.Max(0, inner - fixedTotal - gaps);
            for (int i = 0; i < kids.Count; i++)
                if (_grow.Contains(kids[i])) widths[i] = growCount > 0 ? free / growCount : 0;
            return widths;
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            if (_inLayout) return;
            _inLayout = true;
            try
            {
                LayoutChildren();
            }
            finally { _inLayout = false; }

            SLLayout.NotifyIfChanged(this, MeasureHeight(Width), ref _lastMeasured);
        }

        private void LayoutChildren()
        {
            List<Control> kids = Shown();
            Rectangle area = new Rectangle(Padding.Left, Padding.Top, Width - Padding.Horizontal, Height - Padding.Vertical);
            if (area.Width <= 0) return;

            if (_orientation == SLOrientation.Vertical)
            {
                int[] heights = new int[kids.Count];
                int[] widths = new int[kids.Count];
                int used = 0, growCount = 0;
                for (int i = 0; i < kids.Count; i++)
                {
                    widths[i] = _align == SLAlign.Stretch ? area.Width : Math.Min(area.Width, SLLayout.WidthOf(kids[i]));
                    heights[i] = SLLayout.HeightOf(kids[i], widths[i]);
                    if (_grow.Contains(kids[i])) growCount++;
                    else used += heights[i];
                }
                int gaps = kids.Count > 1 ? _gap * (kids.Count - 1) : 0;
                if (growCount > 0)
                {
                    int free = Math.Max(0, area.Height - used - gaps);
                    for (int i = 0; i < kids.Count; i++)
                        if (_grow.Contains(kids[i])) heights[i] = free / growCount;
                }
                int y = area.Y;
                for (int i = 0; i < kids.Count; i++)
                {
                    int x = area.X;
                    if (_align == SLAlign.Center) x += (area.Width - widths[i]) / 2;
                    else if (_align == SLAlign.End) x += area.Width - widths[i];
                    kids[i].SetBounds(x, y, widths[i], heights[i]);
                    y += heights[i] + _gap;
                }
            }
            else
            {
                int[] widths = MainSizes(kids, area.Width);
                int total = 0;
                foreach (int w in widths) total += w;
                int gaps = kids.Count > 1 ? _gap * (kids.Count - 1) : 0;
                int free = Math.Max(0, area.Width - total - gaps);
                int x = area.X, extraGap = 0;
                bool anyGrow = false;
                foreach (Control c in kids) if (_grow.Contains(c)) anyGrow = true;
                if (!anyGrow)
                {
                    if (_justify == SLJustify.End) x += free;
                    else if (_justify == SLJustify.Center) x += free / 2;
                    else if (_justify == SLJustify.SpaceBetween && kids.Count > 1) extraGap = free / (kids.Count - 1);
                }
                for (int i = 0; i < kids.Count; i++)
                {
                    int h = _align == SLAlign.Stretch ? area.Height : Math.Min(area.Height, SLLayout.HeightOf(kids[i], widths[i]));
                    if (h <= 0) h = SLLayout.HeightOf(kids[i], widths[i]);
                    int y = area.Y;
                    if (_align == SLAlign.Center) y += (area.Height - h) / 2;
                    else if (_align == SLAlign.End) y += area.Height - h;
                    kids[i].SetBounds(x, y, widths[i], h);
                    x += widths[i] + _gap + extraGap;
                }
            }
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            SLVisibility.Track(e.Control);
            e.Control.VisibleChanged += ChildVisibleChanged;
        }

        protected override void OnControlRemoved(ControlEventArgs e)
        {
            e.Control.VisibleChanged -= ChildVisibleChanged;
            _grow.Remove(e.Control);
            base.OnControlRemoved(e);
        }

        private void ChildVisibleChanged(object sender, EventArgs e) { PerformLayout(); }
    }

    /// <summary>
    /// Tracks the visibility a control was *asked* for. Control.Visible reads false for every
    /// child until its form is shown, which would make measuring layouts collapse to zero
    /// before first paint (the AutoSize bug recorded in docs/V2_MIGRATION_STATUS.md).
    /// </summary>
    internal static class SLVisibility
    {
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Control, object> Hidden =
            new System.Runtime.CompilerServices.ConditionalWeakTable<Control, object>();

        public static void Track(Control c)
        {
            c.VisibleChanged -= OnVisibleChanged;
            c.VisibleChanged += OnVisibleChanged;
        }

        private static void OnVisibleChanged(object sender, EventArgs e)
        {
            Control c = (Control)sender;
            // Only trust the flag when the parent chain is visible; otherwise it is the
            // ancestor's state, not the child's.
            if (c.Parent != null && !c.Parent.Visible) return;
            Hidden.Remove(c);
            if (!c.Visible) Hidden.Add(c, Hidden);
        }

        /// <summary>Hide or show a child of an SL layout. Use instead of Visible = x.</summary>
        public static void Set(Control c, bool visible)
        {
            Hidden.Remove(c);
            if (!visible) Hidden.Add(c, Hidden);
            c.Visible = visible;
            if (c.Parent != null) c.Parent.PerformLayout();
        }

        public static bool IsSet(Control c)
        {
            object o;
            return !Hidden.TryGetValue(c, out o);
        }
    }

    /// <summary>
    /// The mockup's Card (Card.js): white, 1px #E4E7EC border, radius 12.
    /// Optional header (Title 16/600 + Subtitle 13 muted, Actions on the right; padding 16x20,
    /// bottom divider), Body (padding 20 by default; set BodyPadding), optional Footer
    /// (padding 12x20, #FCFCFD band, top divider).
    ///
    ///   &lt;Card title="Equipment" subtitle="…" actions={&lt;Button size="sm" variant="ghost"&gt;Manage&lt;/Button&gt;} padding="8px"&gt;
    ///   var card = new SLCard { Title = "Equipment", Subtitle = "…", BodyPadding = new Padding(8) };
    ///   card.Actions.Controls.Add(new SLButton { Text = "Manage", ButtonSize = SLSize.Sm, Variant = SLVariant.Ghost });
    ///   card.Body.Controls.Add(...);
    /// </summary>
    [DesignerCategory("Code")]
    internal class SLCard : Panel, ISLMeasure, ISLSurface
    {
        private string _title, _subtitle;
        private int _lastMeasured = -1;

        public SLCard()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.SurfaceCard;

            Actions = new SLStack(SLOrientation.Horizontal, 8) { Align = SLAlign.Start };
            Body = new SLStack(SLOrientation.Vertical, 16) { Padding = new Padding(20) };
            Footer = new SLStack(SLOrientation.Horizontal, 8) { Padding = new Padding(20, 12, 20, 12), Align = SLAlign.Center };
            Controls.Add(Actions);
            Controls.Add(Body);
            Controls.Add(Footer);
            Actions.ControlAdded += (s, e) => PerformLayout();
            Actions.ControlRemoved += (s, e) => PerformLayout();
            Footer.ControlAdded += (s, e) => PerformLayout();
            Footer.ControlRemoved += (s, e) => PerformLayout();
        }

        public string Title { get { return _title; } set { _title = value; Relayout(); } }
        public string Subtitle { get { return _subtitle; } set { _subtitle = value; Relayout(); } }

        /// <summary>Header buttons (right side). Horizontal stack, gap 8.</summary>
        public SLStack Actions { get; private set; }

        /// <summary>Card content. Vertical stack, gap 16, padding 20.</summary>
        public SLStack Body { get; private set; }

        /// <summary>Footer row (e.g. Pagination or a Next button). Hidden while empty.</summary>
        public SLStack Footer { get; private set; }

        /// <summary>The mockup's padding prop on Card ("0" for edge-to-edge tables).</summary>
        public Padding BodyPadding { get { return Body.Padding; } set { Body.Padding = value; Relayout(); } }

        private bool HasHeader { get { return !string.IsNullOrEmpty(_title) || !string.IsNullOrEmpty(_subtitle) || Actions.Controls.Count > 0; } }
        private bool HasFooter { get { return Footer.Controls.Count > 0; } }

        private void Relayout() { if (Parent != null) Parent.PerformLayout(this, "Bounds"); PerformLayout(); Invalidate(); }

        public Color SurfaceFor(Control child) { return child == Footer ? Theme.N25 : Theme.SurfaceCard; }

        private int ActionsWidth { get { return Actions.Controls.Count > 0 ? Actions.GetPreferredSize(Size.Empty).Width : 0; } }

        private int HeaderTextWidth(int width)
        {
            int aw = ActionsWidth;
            return Math.Max(40, width - 2 - 40 - (aw > 0 ? aw + 16 : 0));
        }

        private int HeaderHeight(int width)
        {
            if (!HasHeader) return 0;
            int tw = HeaderTextWidth(width);
            int h = 0;
            if (!string.IsNullOrEmpty(_title)) h += SLLayout.WrappedHeight(_title, SLType.Of(SLTextStyle.Title), tw);
            if (!string.IsNullOrEmpty(_title) && !string.IsNullOrEmpty(_subtitle)) h += 2;
            if (!string.IsNullOrEmpty(_subtitle)) h += SLLayout.WrappedHeight(_subtitle, SLType.Of(SLTextStyle.Muted), tw);
            if (Actions.Controls.Count > 0) h = Math.Max(h, Actions.MeasureHeight(ActionsWidth));
            return 16 + h + 16 + 1; // padding + divider
        }

        private int FooterHeight(int width) { return HasFooter ? 1 + Footer.MeasureHeight(width - 2) : 0; }

        public int MeasureHeight(int width)
        {
            return 1 + HeaderHeight(width) + Body.MeasureHeight(width - 2) + FooterHeight(width) + 1;
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            int w = Width;
            int header = HeaderHeight(w);
            int footer = FooterHeight(w);

            Actions.Visible = Actions.Controls.Count > 0;
            if (Actions.Visible)
            {
                Size a = Actions.GetPreferredSize(Size.Empty);
                Actions.SetBounds(w - 1 - 20 - a.Width, 1 + 16, a.Width, a.Height);
            }

            int bodyTop = 1 + header;
            int bodyH = Math.Max(Body.MeasureHeight(w - 2), Height - 2 - header - footer);
            Body.SetBounds(1, bodyTop, w - 2, Math.Max(0, bodyH));

            Footer.Visible = HasFooter;
            if (HasFooter) Footer.SetBounds(1, Height - 1 - (footer - 1), w - 2, footer - 1);
            Invalidate();
            SLLayout.NotifyIfChanged(this, MeasureHeight(w), ref _lastMeasured);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (SolidBrush b = new SolidBrush(SLPaint.BackdropOf(this)))
                e.Graphics.FillRectangle(b, ClientRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            SLPaint.Box(g, ClientRectangle, Theme.RadiusLg, Theme.SurfaceCard, Theme.BorderDefault);

            int header = HeaderHeight(Width);
            if (header > 0)
            {
                int tw = HeaderTextWidth(Width);
                int y = 1 + 16;
                if (!string.IsNullOrEmpty(_title))
                    y += SLLayout.DrawWrapped(g, _title, SLType.Of(SLTextStyle.Title), new Rectangle(21, y, tw, 1000), TextFormatFlags.Left) + 2;
                if (!string.IsNullOrEmpty(_subtitle))
                    SLLayout.DrawWrapped(g, _subtitle, SLType.Of(SLTextStyle.Muted), new Rectangle(21, y, tw, 1000), TextFormatFlags.Left);
                SLPaint.HLine(g, Theme.BorderSubtle, 1, Width - 1, header);
            }

            int footer = FooterHeight(Width);
            if (footer > 0)
            {
                int top = Height - 1 - footer;
                // Footer band clipped to the card's rounded inner shape.
                using (GraphicsPath inner = SLPaint.RoundedRect(new RectangleF(1, 1, Width - 2, Height - 2), Theme.RadiusLg - 1))
                using (Region clip = new Region(inner))
                {
                    clip.Intersect(new Rectangle(1, top, Width - 2, footer));
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using (SolidBrush b = new SolidBrush(Theme.N25)) g.FillRegion(b, clip);
                }
                SLPaint.HLine(g, Theme.BorderSubtle, 1, Width - 1, top);
            }
        }
    }

    /// <summary>
    /// A labelled block of fields laid out in a grid (FieldSet.js): optional Title (14/600) and
    /// Description (13 muted), then a grid of Columns (default 2) with 16px gaps.
    ///   &lt;FieldSet columns={2}&gt;…&lt;/FieldSet&gt;  ->  var fs = new SLFieldSet { Columns = 2 }; fs.Add(field1); fs.Add(field2, span: 2);
    /// </summary>
    [DesignerCategory("Code")]
    internal class SLFieldSet : Panel, ISLMeasure
    {
        private string _title, _description;
        private int _columns = 2;
        private int _lastMeasured = -1;
        private readonly Dictionary<Control, int> _spans = new Dictionary<Control, int>();

        public SLFieldSet()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.Transparent;
        }

        public string Title { get { return _title; } set { _title = value; Relayout(); } }
        public string Description { get { return _description; } set { _description = value; Relayout(); } }
        public int Columns { get { return _columns; } set { _columns = Math.Max(1, value); Relayout(); } }

        public void Add(Control field, int span = 1)
        {
            _spans[field] = Math.Max(1, span);
            Controls.Add(field);
            SLVisibility.Track(field);
            Relayout();
        }

        private void Relayout() { if (Parent != null) Parent.PerformLayout(this, "Bounds"); PerformLayout(); Invalidate(); }

        private int HeaderHeight(int width)
        {
            if (string.IsNullOrEmpty(_title)) return 0;
            int h = SLLayout.WrappedHeight(_title, SLType.Of(SLTextStyle.TitleSm), width);
            if (!string.IsNullOrEmpty(_description)) h += 2 + SLLayout.WrappedHeight(_description, SLType.Of(SLTextStyle.Muted), width);
            return h + 12;
        }

        private struct Cell { public Control C; public int Row, Col, Span; }

        private List<Cell> Place()
        {
            List<Cell> cells = new List<Cell>();
            int row = 0, col = 0;
            foreach (Control c in Controls)
            {
                if (!SLVisibility.IsSet(c)) continue;
                int span;
                if (!_spans.TryGetValue(c, out span)) span = 1;
                span = Math.Min(span, _columns);
                if (col + span > _columns) { row++; col = 0; }
                cells.Add(new Cell { C = c, Row = row, Col = col, Span = span });
                col += span;
                if (col >= _columns) { row++; col = 0; }
            }
            return cells;
        }

        private int ColWidth(int width) { return (width - 16 * (_columns - 1)) / _columns; }

        public int MeasureHeight(int width)
        {
            List<Cell> cells = Place();
            int colW = ColWidth(width);
            Dictionary<int, int> rowH = new Dictionary<int, int>();
            foreach (Cell cell in cells)
            {
                int w = colW * cell.Span + 16 * (cell.Span - 1);
                int h = SLLayout.HeightOf(cell.C, w);
                int cur;
                rowH[cell.Row] = rowH.TryGetValue(cell.Row, out cur) ? Math.Max(cur, h) : h;
            }
            int total = HeaderHeight(width);
            foreach (int h in rowH.Values) total += h;
            if (rowH.Count > 1) total += 16 * (rowH.Count - 1);
            return total;
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            List<Cell> cells = Place();
            int colW = ColWidth(Width);
            Dictionary<int, int> rowH = new Dictionary<int, int>();
            foreach (Cell cell in cells)
            {
                int w = colW * cell.Span + 16 * (cell.Span - 1);
                int h = SLLayout.HeightOf(cell.C, w);
                int cur;
                rowH[cell.Row] = rowH.TryGetValue(cell.Row, out cur) ? Math.Max(cur, h) : h;
            }
            int y = HeaderHeight(Width);
            int currentRow = -1, rowTop = y;
            foreach (Cell cell in cells)
            {
                if (cell.Row != currentRow)
                {
                    if (currentRow >= 0) rowTop += rowH[currentRow] + 16;
                    currentRow = cell.Row;
                }
                int w = colW * cell.Span + 16 * (cell.Span - 1);
                int x = cell.Col * (colW + 16);
                // align-items: start — each field keeps its own height.
                cell.C.SetBounds(x, rowTop, w, SLLayout.HeightOf(cell.C, w));
            }
            SLLayout.NotifyIfChanged(this, MeasureHeight(Width), ref _lastMeasured);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (SolidBrush b = new SolidBrush(SLPaint.BackdropOf(this)))
                e.Graphics.FillRectangle(b, ClientRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (string.IsNullOrEmpty(_title)) return;
            int y = SLLayout.DrawWrapped(e.Graphics, _title, SLType.Of(SLTextStyle.TitleSm), new Rectangle(0, 0, Width, 1000), TextFormatFlags.Left);
            if (!string.IsNullOrEmpty(_description))
                SLLayout.DrawWrapped(e.Graphics, _description, SLType.Of(SLTextStyle.Muted), new Rectangle(0, y + 2, Width, 1000), TextFormatFlags.Left);
        }
    }
}
