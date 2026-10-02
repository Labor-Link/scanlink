using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScanLink.DesignSystem
{
    /// <summary>
    /// The mockup's Button (design/mockup/source/components/controls/Button.js).
    ///
    ///   &lt;Button variant="primary" size="sm" icon={&lt;Icon name="printer" size={16}/&gt;}&gt;Print labels&lt;/Button&gt;
    ///   new SLButton { Text = "Print labels", Variant = SLVariant.Primary, ButtonSize = SLSize.Sm, IconName = "printer" }
    ///
    /// Sizes (height / padding-x / font / gap): Sm 32/12/13px/6, Md 38/16/14px/8, Lg 44/20/14px/8.
    /// Radius 6, weight 600. AutoSize is on: never set Width/Height by hand; use Dock or
    /// SLStack stretching for full-width ("block") buttons.
    ///
    /// Subclasses Button, so DialogResult, AcceptButton/CancelButton and PerformClick work.
    /// </summary>
    [DesignerCategory("Code")]
    internal class SLButton : Button
    {
        private struct Look
        {
            public Color Bg, Hover, Active, Fg, Border;
            public Look(Color bg, Color hover, Color active, Color fg, Color border)
            { Bg = bg; Hover = hover; Active = active; Fg = fg; Border = border; }
        }

        private SLVariant _variant = SLVariant.Primary;
        private SLSize _size = SLSize.Md;
        private string _icon, _iconEnd;
        private int _iconSize = 16;
        private bool _loading, _hover, _down;
        private Timer _spin;
        private float _spinAngle;

        public SLButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            UpdateFont();
        }

        [DefaultValue(SLVariant.Primary)]
        public SLVariant Variant { get { return _variant; } set { _variant = value; Invalidate(); } }

        /// <summary>The mockup's size prop. Named ButtonSize because Control.Size is taken.</summary>
        [DefaultValue(SLSize.Md)]
        public SLSize ButtonSize { get { return _size; } set { _size = value; UpdateFont(); Relayout(); } }

        /// <summary>Lucide icon before the label (the mockup's icon prop), e.g. "printer".</summary>
        public string IconName { get { return _icon; } set { _icon = value; Relayout(); } }

        /// <summary>Lucide icon after the label (the mockup's iconEnd prop), e.g. "arrow-right".</summary>
        public string IconEndName { get { return _iconEnd; } set { _iconEnd = value; Relayout(); } }

        /// <summary>Icon size in px; the mockup passes 16 for buttons.</summary>
        [DefaultValue(16)]
        public int IconSize { get { return _iconSize; } set { _iconSize = value; Relayout(); } }

        /// <summary>Shows a spinner in place of the icon and blocks clicks, like loading={true}.</summary>
        [DefaultValue(false)]
        public bool Loading
        {
            get { return _loading; }
            set
            {
                if (_loading == value) return;
                _loading = value;
                if (_loading)
                {
                    if (_spin == null)
                    {
                        _spin = new Timer { Interval = 16 };
                        _spin.Tick += (s, e) => { _spinAngle = (_spinAngle + 8.2f) % 360f; Invalidate(); };
                    }
                    _spin.Start();
                }
                else if (_spin != null) _spin.Stop();
                Relayout();
            }
        }

        public int ControlHeight
        {
            get { return _size == SLSize.Sm ? Theme.HeightSm : _size == SLSize.Lg ? Theme.HeightLg : Theme.HeightMd; }
        }

        private int PadX { get { return _size == SLSize.Sm ? 12 : _size == SLSize.Lg ? 20 : 16; } }
        private int Gap { get { return _size == SLSize.Sm ? 6 : 8; } }

        private void UpdateFont()
        {
            Font = _size == SLSize.Sm ? Theme.FontSmSemibold : Theme.FontMdSemibold;
        }

        private void Relayout()
        {
            if (AutoSize && Parent != null) Parent.PerformLayout(this, "Bounds");
            if (AutoSize) Size = GetPreferredSize(Size.Empty);
            Invalidate();
        }

        private bool HasLeadIcon { get { return _loading || !string.IsNullOrEmpty(_icon); } }

        public override Size GetPreferredSize(Size proposedSize)
        {
            int w = PadX * 2;
            int parts = 0;
            if (HasLeadIcon) { w += _loading ? 14 : _iconSize; parts++; }
            if (!string.IsNullOrEmpty(Text)) { w += SLPaint.Measure(Text, Font).Width; parts++; }
            if (!string.IsNullOrEmpty(_iconEnd)) { w += _iconSize; parts++; }
            if (parts > 1) w += Gap * (parts - 1);
            return new Size(w, ControlHeight);
        }

        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Relayout(); }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { _down = true; Invalidate(); } base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        protected override void OnClick(EventArgs e)
        {
            if (_loading) return;
            base.OnClick(e);
        }

        private Look LookFor(SLVariant v)
        {
            switch (v)
            {
                case SLVariant.Secondary: return new Look(Theme.N0, Theme.N50, Theme.N100, Theme.N800, Theme.BorderStrong);
                case SLVariant.Ghost: return new Look(Color.Empty, Theme.N100, Theme.N200, Theme.N600, Color.Empty);
                case SLVariant.Navy: return new Look(Theme.Navy900, Theme.Navy950, Theme.Navy950, Color.White, Color.Empty);
                case SLVariant.Success: return new Look(Theme.Ok500, Theme.Ok700, Theme.Ok700, Color.White, Color.Empty);
                case SLVariant.Danger: return new Look(Theme.Err500, Theme.Err700, Theme.Err700, Color.White, Color.Empty);
                case SLVariant.DangerQuiet: return new Look(Theme.N0, Theme.Err50, Theme.Err50, Theme.Err700, Theme.N300);
                default: return new Look(Theme.ActionPrimary, Theme.ActionPrimaryHover, Theme.ActionPrimaryActive, Color.White, Color.Empty);
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
            OnPaintBackground(e);

            Look look = LookFor(_variant);
            bool off = !Enabled || _loading;
            Color fill, border, fg;
            if (off && !_loading)
            {
                fill = Theme.N100; border = Theme.BorderDefault; fg = Theme.N400;
            }
            else if (_loading)
            {
                // The mockup renders loading as disabled styling with a spinner.
                fill = Theme.N100; border = Theme.BorderDefault; fg = Theme.N400;
            }
            else
            {
                fill = _down ? look.Active : _hover ? look.Hover : look.Bg;
                border = look.Border;
                fg = look.Fg;
            }

            Rectangle box = new Rectangle(0, 0, Width, Height);
            if (!fill.IsEmpty || !border.IsEmpty)
                SLPaint.Box(g, box, Theme.RadiusSm, fill.IsEmpty ? SLPaint.BackdropOf(this) : fill, border);

            // Keyboard focus only; mouse clicks do not leave a ring behind (WinForms convention).
            if (Focused && ShowFocusCues && !off)
            {
                using (GraphicsPath p = SLPaint.RoundedRect(new RectangleF(1.5f, 1.5f, Width - 3f, Height - 3f), Theme.RadiusSm - 1))
                using (Pen pen = new Pen(Color.FromArgb(110, Theme.Indigo500), 2f))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.DrawPath(pen, p);
                }
            }

            // Content row, centred: [icon] gap [text] gap [iconEnd]
            Size pref = GetPreferredSize(Size.Empty);
            int contentW = pref.Width - PadX * 2;
            int x = (Width - contentW) / 2;
            int cy = Height / 2;

            if (_loading)
            {
                DrawSpinner(g, new Rectangle(x, cy - 7, 14, 14), fg);
                x += 14 + Gap;
            }
            else if (!string.IsNullOrEmpty(_icon))
            {
                SLIcon.Draw(g, _icon, new Rectangle(x, cy - _iconSize / 2, _iconSize, _iconSize), fg);
                x += _iconSize + Gap;
            }
            if (!string.IsNullOrEmpty(Text))
            {
                int tw = SLPaint.Measure(Text, Font).Width;
                SLPaint.Text(g, Text, Font, new Rectangle(x, 0, tw + 1, Height), fg, TextFormatFlags.Left);
                x += tw + Gap;
            }
            if (!string.IsNullOrEmpty(_iconEnd))
                SLIcon.Draw(g, _iconEnd, new Rectangle(x, cy - _iconSize / 2, _iconSize, _iconSize), fg);
        }

        private void DrawSpinner(Graphics g, Rectangle r, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(color, 2f))
            {
                RectangleF rf = new RectangleF(r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2);
                g.DrawArc(pen, rf, _spinAngle, 270);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _spin != null) { _spin.Dispose(); _spin = null; }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// The mockup's IconButton: square, icon-only, for toolbars and table rows.
    /// Sm 30x30, Md 36x36; variants Ghost (default), Solid, Danger. Always set Label (tooltip).
    /// </summary>
    [DesignerCategory("Code")]
    internal class SLIconButton : Button
    {
        private SLIconButtonVariant _variant = SLIconButtonVariant.Ghost;
        private SLSize _size = SLSize.Md;
        private string _icon, _label;
        private bool _hover;

        public SLIconButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Text = "";
            Size = new Size(36, 36);
        }

        public SLIconButtonVariant Variant { get { return _variant; } set { _variant = value; Invalidate(); } }

        public SLSize ButtonSize
        {
            get { return _size; }
            set { _size = value; int d = value == SLSize.Sm ? 30 : 36; Size = new Size(d, d); Invalidate(); }
        }

        public string IconName { get { return _icon; } set { _icon = value; Invalidate(); } }

        /// <summary>Accessible name and tooltip ("Edit Line 3 scanner").</summary>
        public string Label
        {
            get { return _label; }
            set { _label = value; AccessibleName = value; SLToolTip.Set(this, value); }
        }

        /// <summary>Icon px; the mockup uses 16 in table rows and toolbars.</summary>
        public int IconSize { get; set; } = 16;

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (SolidBrush b = new SolidBrush(SLPaint.BackdropOf(this)))
                e.Graphics.FillRectangle(b, ClientRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            OnPaintBackground(e);

            Color bg, hoverBg, fg;
            switch (_variant)
            {
                case SLIconButtonVariant.Solid: bg = Theme.N0; hoverBg = Theme.N50; fg = Theme.N600; break;
                case SLIconButtonVariant.Danger: bg = Color.Empty; hoverBg = Theme.Err50; fg = Theme.Err500; break;
                default: bg = Color.Empty; hoverBg = Theme.N100; fg = Theme.N500; break;
            }
            if (!Enabled) { bg = Color.Empty; fg = Theme.N300; }
            Color fill = Enabled && _hover ? hoverBg : bg;
            Color border = _variant == SLIconButtonVariant.Solid ? Theme.BorderStrong : Color.Empty;

            if (!fill.IsEmpty || !border.IsEmpty)
                SLPaint.Box(g, ClientRectangle, Theme.RadiusSm, fill.IsEmpty ? SLPaint.BackdropOf(this) : fill, border);

            if (!string.IsNullOrEmpty(_icon))
            {
                int s = IconSize;
                SLIcon.Draw(g, _icon, new Rectangle((Width - s) / 2, (Height - s) / 2, s, s), fg);
            }
        }
    }
}
