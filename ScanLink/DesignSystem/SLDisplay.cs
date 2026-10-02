using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ScanLink.DesignSystem
{
    /// <summary>
    /// Text in one of the mockup's type roles (SLTextStyle). Wraps to its width and keeps the
    /// CSS line-height, so stacked text has the same rhythm as the mockup. Use instead of Label.
    ///   &lt;p style={{font:"400 var(--f-sm)/1.5", color:"var(--text-muted)"}}&gt;  ->  new SLText { Style = SLTextStyle.Muted }
    /// </summary>
    [DesignerCategory("Code")]
    internal class SLText : SLControl, ISLMeasure
    {
        private SLTextStyle _style = SLTextStyle.BodySm;
        private Color _color = Color.Empty;
        private TextFormatFlags _align = TextFormatFlags.Left;

        // AutoSize so horizontal stacks size it to its text; vertical stacks measure the wrap.
        public SLText() { AutoSize = true; Height = 20; }

        public SLText(string text, SLTextStyle style) : this() { Text = text; Style = style; }

        public SLTextStyle Style { get { return _style; } set { _style = value; Relayout(); } }

        /// <summary>Override ink (e.g. Theme.Err700 for an error line). Empty = role colour.</summary>
        public Color Color { get { return _color; } set { _color = value; Invalidate(); } }

        /// <summary>TextFormatFlags.Left / HorizontalCenter / Right.</summary>
        public TextFormatFlags Align { get { return _align; } set { _align = value; Invalidate(); } }

        /// <summary>Single line with ellipsis instead of wrapping.</summary>
        public bool SingleLine { get; set; }

        /// <summary>CSS line-height override (e.g. 1.3); 0 keeps the role's own.</summary>
        public float LineHeight { get; set; }

        private SLTypeSpec Spec
        {
            get
            {
                SLTypeSpec spec = SLType.Of(_style);
                if (LineHeight > 0) spec.LineHeight = LineHeight;
                if (!_color.IsEmpty) spec.Color = _color;
                return spec;
            }
        }

        private void Relayout() { if (Parent != null) Parent.PerformLayout(this, "Text"); Invalidate(); }

        public int MeasureHeight(int width)
        {
            SLTypeSpec spec = Spec;
            if (string.IsNullOrEmpty(Text)) return 0;
            if (SingleLine) return spec.LineBox;
            return SLLayout.WrappedHeight(Text, spec, width);
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            SLTypeSpec spec = Spec;
            return new Size(SLPaint.Measure(Text, spec.Font).Width + 1, spec.LineBox);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            SLTypeSpec spec = Spec;
            if (SingleLine)
                SLPaint.TextEllipsis(e.Graphics, Text, spec.Font, new Rectangle(0, 0, Width, spec.LineBox), spec.Color, _align);
            else
                SLLayout.DrawWrapped(e.Graphics, Text, spec, ClientRectangle, _align);
        }
    }

    /// <summary>
    /// Status pill (Badge.js): 12px/600 text, padding 3x10, pill radius, optional 6px dot.
    /// The label is a plain word ("Working", "Offline"), never a code.
    /// </summary>
    [DesignerCategory("Code")]
    internal class SLBadge : SLControl
    {
        private SLTone _tone = SLTone.Neutral;
        private bool _dot;

        public SLBadge()
        {
            AutoSize = true;
            Font = Theme.FontXsSemibold;
            Size = GetPreferredSize(Size.Empty);
        }

        public SLBadge(string text, SLTone tone, bool dot) : this() { Text = text; Tone = tone; Dot = dot; }

        public SLTone Tone { get { return _tone; } set { _tone = value; Invalidate(); } }
        public bool Dot { get { return _dot; } set { _dot = value; Size = GetPreferredSize(Size.Empty); Invalidate(); } }

        public static void Colors(SLTone tone, out Color bg, out Color fg, out Color dot)
        {
            switch (tone)
            {
                case SLTone.Success: bg = Theme.Ok50; fg = Theme.Ok700; dot = Theme.Ok500; break;
                case SLTone.Warning: bg = Theme.Warn50; fg = Theme.Warn700; dot = Theme.Warn500; break;
                case SLTone.Error: bg = Theme.Err50; fg = Theme.Err700; dot = Theme.Err500; break;
                case SLTone.Info: bg = Theme.Info50; fg = Theme.Info500; dot = Theme.Info500; break;
                case SLTone.Brand: bg = Theme.Indigo50; fg = Theme.Indigo700; dot = Theme.Indigo500; break;
                default: bg = Theme.N100; fg = Theme.N600; dot = Theme.N400; break;
            }
        }

        /// <summary>Badge size for text: also used by SLTable to paint badges in cells.</summary>
        public static Size Measure(string text, bool dot)
        {
            int w = 20 + SLPaint.Measure(text, Theme.FontXsSemibold).Width + (dot ? 12 : 0);
            return new Size(w, 24); // 12px * 1.5 line box + 3px top/bottom padding
        }

        /// <summary>Paints a badge at rect's top-left. Shared with table cells.</summary>
        public static void PaintBadge(Graphics g, Rectangle rect, string text, SLTone tone, bool dot)
        {
            Color bg, fg, dc;
            Colors(tone, out bg, out fg, out dc);
            SLPaint.Box(g, rect, rect.Height / 2, bg, Color.Empty);
            int x = rect.X + 10;
            if (dot)
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (SolidBrush b = new SolidBrush(dc))
                    g.FillEllipse(b, x, rect.Y + (rect.Height - 6) / 2f, 6, 6);
                x += 12;
            }
            SLPaint.Text(g, text, Theme.FontXsSemibold, new Rectangle(x, rect.Y, rect.Right - x, rect.Height), fg, TextFormatFlags.Left);
        }

        public override Size GetPreferredSize(Size proposedSize) { return Measure(Text, _dot); }

        protected override void OnPaint(PaintEventArgs e)
        {
            PaintBadge(e.Graphics, new Rectangle(0, 0, Width, Height), Text, _tone, _dot);
        }
    }

    /// <summary>
    /// Inline message attached to the thing it is about (Banner.js).
    /// Padding 12x14, radius 8, gap 12; icon 18px in tone ink; title 13/600; body 13/400.
    /// Put an action button in Action (e.g. a Secondary Sm SLButton); it sits at the right.
    /// </summary>
    [DesignerCategory("Code")]
    internal class SLBanner : Panel, ISLMeasure
    {
        private SLTone _tone = SLTone.Info;
        private string _icon, _title, _message;
        private Control _action;

        public SLBanner()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Height = 46;
        }

        /// <summary>Info, Success, Warning or Error (Neutral/Brand fall back to Info).</summary>
        public SLTone Tone { get { return _tone; } set { _tone = value; UpdateBackColor(); Invalidate(); } }
        public string IconName { get { return _icon; } set { _icon = value; Relayout(); } }
        public string Title { get { return _title; } set { _title = value; Relayout(); } }
        public string Message { get { return _message; } set { _message = value; Relayout(); } }

        public Control Action
        {
            get { return _action; }
            set
            {
                if (_action != null) Controls.Remove(_action);
                _action = value;
                if (_action != null) Controls.Add(_action);
                Relayout();
            }
        }

        private void UpdateBackColor() { Color bg, fg, border; Colors(out bg, out fg, out border); BackColor = bg; }

        private void Colors(out Color bg, out Color fg, out Color border)
        {
            switch (_tone)
            {
                case SLTone.Success: bg = Theme.Ok50; fg = Theme.Ok700; border = Theme.BannerBorderSuccess; break;
                case SLTone.Warning: bg = Theme.Warn50; fg = Theme.Warn700; border = Theme.BannerBorderWarning; break;
                case SLTone.Error: bg = Theme.Err50; fg = Theme.Err700; border = Theme.BannerBorderError; break;
                default: bg = Theme.Info50; fg = Theme.Info500; border = Theme.BannerBorderInfo; break;
            }
        }

        private void Relayout() { if (Parent != null) Parent.PerformLayout(this, "Bounds"); PerformLayout(); Invalidate(); }

        private int TextLeft { get { return 14 + (string.IsNullOrEmpty(_icon) ? 0 : 18 + 12); } }

        private int TextWidth(int width)
        {
            int right = 14 + (_action != null ? SLLayout.WidthOf(_action) + 12 : 0);
            return Math.Max(40, width - TextLeft - right);
        }

        public int MeasureHeight(int width)
        {
            int tw = TextWidth(width);
            int h = 0;
            if (!string.IsNullOrEmpty(_title)) h += SLLayout.WrappedHeight(_title, SLType.Of(SLTextStyle.BodySmStrong), tw);
            if (!string.IsNullOrEmpty(_title) && !string.IsNullOrEmpty(_message)) h += 2;
            if (!string.IsNullOrEmpty(_message)) h += SLLayout.WrappedHeight(_message, SLType.Of(SLTextStyle.BodySm), tw);
            h = Math.Max(h, 18);
            if (_action != null) h = Math.Max(h, SLLayout.HeightOf(_action, 0));
            return h + 24 + 2; // 12px padding top/bottom + 1px border each side
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (_action != null)
            {
                Size s = _action.AutoSize ? _action.GetPreferredSize(Size.Empty) : _action.Size;
                _action.Bounds = new Rectangle(Width - 1 - 14 - s.Width, 13, s.Width, s.Height);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (SolidBrush b = new SolidBrush(SLPaint.BackdropOf(this)))
                e.Graphics.FillRectangle(b, ClientRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Color bg, fg, border;
            Colors(out bg, out fg, out border);
            SLPaint.Box(g, ClientRectangle, Theme.RadiusMd, bg, border);

            int top = 13;
            if (!string.IsNullOrEmpty(_icon))
                SLIcon.Draw(g, _icon, new Rectangle(15, top + 1, 18, 18), fg);

            int tw = TextWidth(Width);
            int y = top;
            if (!string.IsNullOrEmpty(_title))
            {
                SLTypeSpec t = SLType.Of(SLTextStyle.BodySmStrong); t.Color = fg;
                y += SLLayout.DrawWrapped(g, _title, t, new Rectangle(TextLeft + 1, y, tw, 1000), TextFormatFlags.Left);
                if (!string.IsNullOrEmpty(_message)) y += 2;
            }
            if (!string.IsNullOrEmpty(_message))
                SLLayout.DrawWrapped(g, _message, SLType.Of(SLTextStyle.BodySm), new Rectangle(TextLeft + 1, y, tw, 1000), TextFormatFlags.Left);
        }
    }

    /// <summary>
    /// Shown instead of an empty table (EmptyState.js): 44px indigo icon tile, 14/600 title,
    /// 13px muted description (max 380px), optional action. Always say what happened and what
    /// to do next.
    /// </summary>
    [DesignerCategory("Code")]
    internal class SLEmptyState : Panel, ISLMeasure
    {
        private string _icon, _title, _description;
        private Control _action;
        private bool _compact;

        public SLEmptyState()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        public string IconName { get { return _icon; } set { _icon = value; Relayout(); } }
        public string Title { get { return _title; } set { _title = value; Relayout(); } }
        public string Description { get { return _description; } set { _description = value; Relayout(); } }
        public bool Compact { get { return _compact; } set { _compact = value; Relayout(); } }

        public Control Action
        {
            get { return _action; }
            set
            {
                if (_action != null) Controls.Remove(_action);
                _action = value;
                if (_action != null) Controls.Add(_action);
                Relayout();
            }
        }

        private int PadY { get { return _compact ? 32 : 56; } }
        private int PadX { get { return _compact ? 20 : 24; } }

        private void Relayout() { if (Parent != null) Parent.PerformLayout(this, "Bounds"); PerformLayout(); Invalidate(); }

        private int DescWidth(int width) { return Math.Min(380, width - PadX * 2); }

        public int MeasureHeight(int width)
        {
            int h = PadY;
            if (!string.IsNullOrEmpty(_icon)) h += 44 + 4 + 8;
            h += SLLayout.WrappedHeight(_title, SLType.Of(SLTextStyle.TitleSm), width - PadX * 2);
            if (!string.IsNullOrEmpty(_description))
                h += 8 + SLLayout.WrappedHeight(_description, new SLTypeSpec(Theme.FontSm, Theme.TextMuted, 1.6f), DescWidth(width));
            if (_action != null) h += 8 + 8 + SLLayout.HeightOf(_action, 0);
            return h + PadY;
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (_action == null) return;
            Size s = _action.AutoSize ? _action.GetPreferredSize(Size.Empty) : _action.Size;
            int total = MeasureHeight(Width);
            int offset = Math.Max(0, (Height - total) / 2);
            _action.Bounds = new Rectangle((Width - s.Width) / 2, offset + total - PadY - s.Height, s.Width, s.Height);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Inside a table the grid's own BackColor is the Windows default grey, so an empty
            // state given an opaque BackColor (SLTableStyle sets white) paints that instead.
            Color bg = BackColor.A == 255 ? BackColor : SLPaint.BackdropOf(this);
            using (SolidBrush b = new SolidBrush(bg))
                e.Graphics.FillRectangle(b, ClientRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            int contentH = MeasureHeight(Width);
            int y = Math.Max(0, (Height - contentH) / 2) + PadY;
            TextFormatFlags center = TextFormatFlags.HorizontalCenter;

            if (!string.IsNullOrEmpty(_icon))
            {
                Rectangle tile = new Rectangle((Width - 44) / 2, y, 44, 44);
                SLPaint.Box(g, tile, Theme.RadiusLg, Theme.Indigo50, Color.Empty);
                SLIcon.Draw(g, _icon, new Rectangle(tile.X + 11, tile.Y + 11, 22, 22), Theme.Indigo500);
                y += 44 + 4 + 8;
            }
            y += SLLayout.DrawWrapped(g, _title, SLType.Of(SLTextStyle.TitleSm), new Rectangle(PadX, y, Width - PadX * 2, 1000), center);
            if (!string.IsNullOrEmpty(_description))
            {
                y += 8;
                int dw = DescWidth(Width);
                SLLayout.DrawWrapped(g, _description, new SLTypeSpec(Theme.FontSm, Theme.TextMuted, 1.6f),
                    new Rectangle((Width - dw) / 2, y, dw, 1000), center);
            }
        }
    }

    /// <summary>
    /// Two-column label/value list (the scan-detail grid: "auto 1fr", gap 10px 20px).
    /// Keys 13px muted, values 13px/500 body ink, line-height 1.4.
    ///   var kv = new SLKeyValueList(); kv.Add("Time", "07:14"); kv.Add("Block", "14");
    /// </summary>
    [DesignerCategory("Code")]
    internal class SLKeyValueList : SLControl, ISLMeasure
    {
        private readonly System.Collections.Generic.List<string[]> _rows = new System.Collections.Generic.List<string[]>();
        private static readonly SLTypeSpec KeySpec = new SLTypeSpec(Theme.FontSm, Theme.TextMuted, 1.4f);
        private static readonly SLTypeSpec ValueSpec = new SLTypeSpec(Theme.FontSmMedium, Theme.TextBody, 1.4f);

        public void Add(string key, string value)
        {
            _rows.Add(new[] { key, value });
            if (Parent != null) Parent.PerformLayout(this, "Bounds");
            Invalidate();
        }

        public void Clear() { _rows.Clear(); if (Parent != null) Parent.PerformLayout(this, "Bounds"); Invalidate(); }

        private int KeyWidth()
        {
            int w = 0;
            foreach (string[] r in _rows) w = Math.Max(w, SLPaint.Measure(r[0], KeySpec.Font).Width);
            return w + 1;
        }

        public int MeasureHeight(int width)
        {
            if (_rows.Count == 0) return 0;
            int vw = Math.Max(10, width - KeyWidth() - 20);
            int h = 0;
            foreach (string[] r in _rows) h += Math.Max(KeySpec.LineBox, SLLayout.WrappedHeight(r[1], ValueSpec, vw));
            return h + 10 * (_rows.Count - 1);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            int kw = KeyWidth();
            int vx = kw + 20, vw = Math.Max(10, Width - vx);
            int y = 0;
            foreach (string[] r in _rows)
            {
                SLPaint.Text(e.Graphics, r[0], KeySpec.Font, new Rectangle(0, y, kw, KeySpec.LineBox), KeySpec.Color, TextFormatFlags.Left);
                int h = SLLayout.DrawWrapped(e.Graphics, r[1], ValueSpec, new Rectangle(vx, y, vw, 1000), TextFormatFlags.Left);
                y += Math.Max(KeySpec.LineBox, h) + 10;
            }
        }
    }
}
