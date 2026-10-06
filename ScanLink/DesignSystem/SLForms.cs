using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScanLink.DesignSystem
{
    /// <summary>Inputs that can show the red invalid border when their SLField has an Error.</summary>
    internal interface ISLInvalid
    {
        bool Invalid { get; set; }
    }

    internal static class SLFieldBox
    {
        public static int HeightFor(SLSize size)
        {
            return size == SLSize.Sm ? Theme.HeightSm : size == SLSize.Lg ? Theme.HeightLg : Theme.HeightMd;
        }

        /// <summary>
        /// The mockup's fieldBox(): white, 1px #D0D5DD border, radius 6; indigo border when
        /// focused, red when invalid, #F7F8FA when disabled. The 3px focus ring is drawn just
        /// inside the border (WinForms controls cannot paint outside their bounds).
        /// </summary>
        public static void Paint(Graphics g, Rectangle r, bool focus, bool invalid, bool disabled, bool hover)
        {
            Color fill = disabled ? Theme.N50 : Theme.N0;
            Color border = invalid ? Theme.Err500 : focus ? Theme.ActionPrimary : Theme.BorderStrong;
            SLPaint.Box(g, r, Theme.RadiusSm, fill, border);
            if (focus && !disabled)
            {
                using (GraphicsPath p = SLPaint.RoundedRect(new RectangleF(r.X + 2f, r.Y + 2f, r.Width - 4f, r.Height - 4f), Theme.RadiusSm - 2))
                using (Pen pen = new Pen(invalid ? Theme.Err50 : Theme.FocusRingSoft, 2f))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.DrawPath(pen, p);
                }
            }
        }
    }

    /// <summary>
    /// Label + input + hint + error, exactly as TextField.js / Select.js / NumberField.js stack
    /// them: label 13px label-ink (red * when Required), optional 12px muted hint under it,
    /// 6px gap, the input, then a 12px red error line 6px below.
    ///
    ///   &lt;TextField label="Printer address" hint="…" mono /&gt;
    ///   new SLField("Printer address", new SLTextBox { Mono = true }) { Hint = "…" }
    /// </summary>
    [DesignerCategory("Code")]
    internal class SLField : Panel, ISLMeasure
    {
        private string _label, _hint, _error;
        private bool _required;
        private Control _input;
        private int _lastMeasured = -1;

        public SLField()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.Transparent;
        }

        public SLField(string label, Control input) : this() { Label = label; Input = input; }

        public string Label { get { return _label; } set { _label = value; Relayout(); } }
        public bool Required { get { return _required; } set { _required = value; Invalidate(); } }
        public string Hint { get { return _hint; } set { _hint = value; Relayout(); } }

        /// <summary>Validation message; also turns the input's border red. Null clears it.</summary>
        public string Error
        {
            get { return _error; }
            set
            {
                _error = value;
                ISLInvalid inv = _input as ISLInvalid;
                if (inv != null) inv.Invalid = !string.IsNullOrEmpty(value);
                Relayout();
            }
        }

        public Control Input
        {
            get { return _input; }
            set
            {
                if (_input != null) Controls.Remove(_input);
                _input = value;
                if (_input != null) Controls.Add(_input);
                Relayout();
            }
        }

        private void Relayout() { if (Parent != null) Parent.PerformLayout(this, "Bounds"); PerformLayout(); Invalidate(); }

        private int LabelBlock(int width)
        {
            if (string.IsNullOrEmpty(_label) && string.IsNullOrEmpty(_hint)) return 0;
            int h = 0;
            if (!string.IsNullOrEmpty(_label)) h += SLType.Of(SLTextStyle.Label).LineBox;
            if (!string.IsNullOrEmpty(_hint)) h += 2 + SLLayout.WrappedHeight(_hint, SLType.Of(SLTextStyle.Caption), width);
            return h + 6;
        }

        private int ErrorBlock(int width)
        {
            if (string.IsNullOrEmpty(_error)) return 0;
            return 6 + SLLayout.WrappedHeight(_error, SLType.Of(SLTextStyle.Caption), width);
        }

        public int MeasureHeight(int width)
        {
            int input = _input == null ? 0 : SLLayout.HeightOf(_input, width);
            return LabelBlock(width) + input + ErrorBlock(width);
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            if (_input != null)
            {
                int top = LabelBlock(Width);
                int h = SLLayout.HeightOf(_input, Width);
                // Fixed-width inputs (NumberField is 160px in the mockup) keep their width.
                int w = (_input is SLNumberBox && ((SLNumberBox)_input).FixedWidth) ? Math.Min(Width, _input.Width) : Width;
                _input.SetBounds(0, top, w, h);
            }
            SLLayout.NotifyIfChanged(this, MeasureHeight(Width), ref _lastMeasured);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (_input != null && e.Y < LabelBlock(Width)) _input.Focus();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (SolidBrush b = new SolidBrush(SLPaint.BackdropOf(this)))
                e.Graphics.FillRectangle(b, ClientRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            int y = 0;
            if (!string.IsNullOrEmpty(_label))
            {
                SLTypeSpec spec = SLType.Of(SLTextStyle.Label);
                SLPaint.Text(g, _label, spec.Font, new Rectangle(0, 0, Width, spec.LineBox), spec.Color, TextFormatFlags.Left);
                if (_required)
                {
                    int lw = SLPaint.Measure(_label, spec.Font).Width;
                    SLPaint.Text(g, "*", spec.Font, new Rectangle(lw + 2, 0, 12, spec.LineBox), Theme.Err500, TextFormatFlags.Left);
                }
                y += spec.LineBox;
            }
            if (!string.IsNullOrEmpty(_hint))
                y += 2 + SLLayout.DrawWrapped(g, _hint, SLType.Of(SLTextStyle.Caption), new Rectangle(0, y + 2, Width, 1000), TextFormatFlags.Left);
            if (!string.IsNullOrEmpty(_error) && _input != null)
            {
                SLTypeSpec err = SLType.Of(SLTextStyle.Caption); err.Color = Theme.Err700;
                SLLayout.DrawWrapped(g, _error, err, new Rectangle(0, _input.Bottom + 6, Width, 1000), TextFormatFlags.Left);
            }
        }
    }

    /// <summary>
    /// Text input (TextField.js without the label — wrap it in SLField for that).
    /// Sm/Md/Lg = 32/38/44px, 14px text, 12px side padding; PrefixIcon (16px, grey) moves the
    /// text to 38px; Suffix (any control, e.g. an SLIconButton) sits 8px from the right.
    /// The real TextBox is in Inner for anything not forwarded here.
    /// </summary>
    [DesignerCategory("Code")]
    internal class SLTextBox : SLControl, ISLInvalid, ISLSurface
    {
        private const int EM_SETCUEBANNER = 0x1501;
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        protected readonly TextBox Box;
        private SLSize _size = SLSize.Md;
        private string _prefixIcon, _placeholder, _unit;
        private Control _suffix;
        private bool _invalid, _mono, _hover;

        public SLTextBox() : this(new TextBox()) { }

        /// <summary>
        /// Wraps an EXISTING TextBox (e.g. a designer field that other code reads and writes):
        /// it is moved into this frame and restyled, but stays the same object, so its handlers
        /// and every reference to it keep working. Legacy colours set on it (Gray placeholder
        /// text, Black input) are mapped to the design system's.
        /// </summary>
        public SLTextBox(TextBox existing)
        {
            Box = existing;
            if (Box.Parent != null) Box.Parent.Controls.Remove(Box);
            Box.BorderStyle = BorderStyle.None;
            Box.Dock = DockStyle.None;
            Box.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            Box.Margin = Padding.Empty;
            Box.Font = Theme.FontMd;
            Box.BackColor = Theme.N0;
            Box.ForeColor = MapInk(Box.ForeColor);
            Box.Visible = true;
            Controls.Add(Box);
            Box.GotFocus += (s, e) => Invalidate();
            Box.LostFocus += (s, e) => Invalidate();
            Box.TextChanged += (s, e) => OnTextChanged(e);
            Box.KeyDown += (s, e) => OnKeyDown(e);
            Box.KeyPress += (s, e) => OnKeyPress(e);
            Box.MouseEnter += (s, e) => { _hover = true; Invalidate(); };
            Box.MouseLeave += (s, e) => { _hover = false; Invalidate(); };
            Box.HandleCreated += (s, e) => ApplyPlaceholder();
            Box.ForeColorChanged += (s, e) =>
            {
                Color mapped = MapInk(Box.ForeColor);
                if (mapped != Box.ForeColor) Box.ForeColor = mapped;
            };
            Cursor = Cursors.IBeam;
            Height = Theme.HeightMd;
        }

        /// <summary>A suffix button (password eye) sits on the field's fill, not the page.</summary>
        public Color SurfaceFor(Control child) { return (!Enabled || Box.ReadOnly) ? Theme.N50 : Theme.N0; }

        /// <summary>Legacy greys/blacks -> placeholder and body ink.</summary>
        private static Color MapInk(Color c)
        {
            int argb = c.ToArgb();
            if (argb == Color.Gray.ToArgb() || argb == Color.DarkGray.ToArgb() || argb == SystemColors.GrayText.ToArgb()) return Theme.N400;
            if (argb == Color.Black.ToArgb() || argb == SystemColors.WindowText.ToArgb()) return Theme.TextBody;
            return c;
        }

        /// <summary>The underlying TextBox, for events and properties not forwarded.</summary>
        public TextBox Inner { get { return Box; } }

        // Guarded: Control's constructor can touch Text before Box exists.
        public override string Text
        {
            get { return Box == null ? base.Text : Box.Text; }
            set { if (Box == null) base.Text = value; else Box.Text = value; }
        }

        public SLSize FieldSize { get { return _size; } set { _size = value; if (!Box.Multiline) Height = SLFieldBox.HeightFor(value); PerformLayout(); } }
        public string PlaceholderText { get { return _placeholder; } set { _placeholder = value; ApplyPlaceholder(); } }
        public string PrefixIcon { get { return _prefixIcon; } set { _prefixIcon = value; PerformLayout(); Invalidate(); } }

        /// <summary>Unit shown at the right in muted text (NumberField's unit prop, e.g. "kg").</summary>
        public string Unit { get { return _unit; } set { _unit = value; PerformLayout(); Invalidate(); } }

        public Control Suffix
        {
            get { return _suffix; }
            set
            {
                if (_suffix != null) Controls.Remove(_suffix);
                _suffix = value;
                if (_suffix != null) Controls.Add(_suffix);
                PerformLayout();
            }
        }

        /// <summary>Consolas text (the mockup's mono prop) for serials, IPs and ports.</summary>
        public bool Mono { get { return _mono; } set { _mono = value; Box.Font = value ? Theme.FontMonoMd : Theme.FontMd; PerformLayout(); } }

        public bool Invalid { get { return _invalid; } set { _invalid = value; Invalidate(); } }
        public bool ReadOnly { get { return Box.ReadOnly; } set { Box.ReadOnly = value; SyncColors(); } }
        public bool UseSystemPasswordChar { get { return Box.UseSystemPasswordChar; } set { Box.UseSystemPasswordChar = value; } }
        public int MaxLength { get { return Box.MaxLength; } set { Box.MaxLength = value; } }

        /// <summary>Multi-line (set Height yourself, or let SLStack.SetGrow fill it).</summary>
        public bool Multiline
        {
            get { return Box.Multiline; }
            set { Box.Multiline = value; Box.ScrollBars = value ? ScrollBars.Vertical : ScrollBars.None; PerformLayout(); }
        }

        public ScrollBars ScrollBars { get { return Box.ScrollBars; } set { Box.ScrollBars = value; } }
        public bool WordWrap { get { return Box.WordWrap; } set { Box.WordWrap = value; } }

        public void SelectAll() { Box.SelectAll(); }
        public void Clear() { Box.Clear(); }
        public void AppendText(string text) { Box.AppendText(text); }
        public new bool Focus() { return Box.Focus(); }
        public override bool Focused { get { return Box.Focused; } }

        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); SyncColors(); }

        private void SyncColors()
        {
            bool dim = !Enabled || Box.ReadOnly;
            Box.BackColor = dim ? Theme.N50 : Theme.N0;
            Box.ForeColor = Enabled ? Theme.TextBody : Theme.N400;
            Invalidate();
        }

        private void ApplyPlaceholder()
        {
            if (!Box.IsHandleCreated || Box.Multiline) return;
            SendMessage(Box.Handle, EM_SETCUEBANNER, (IntPtr)1, _placeholder ?? "");
        }

        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); Box.Focus(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Box.Focus(); }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            int left = string.IsNullOrEmpty(_prefixIcon) ? 12 : 38;
            int right = _suffix != null ? 40 : !string.IsNullOrEmpty(_unit) ? 44 : 12;
            if (Box.Multiline)
                Box.SetBounds(left, 9, Math.Max(10, Width - left - right), Math.Max(10, Height - 18));
            else
            {
                int h = Box.PreferredHeight;
                Box.SetBounds(left, (Height - h) / 2, Math.Max(10, Width - left - right), h);
            }
            if (_suffix != null)
            {
                Size s = _suffix.AutoSize ? _suffix.GetPreferredSize(Size.Empty) : _suffix.Size;
                _suffix.SetBounds(Width - 8 - s.Width, (Height - s.Height) / 2, s.Width, s.Height);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            SLFieldBox.Paint(g, ClientRectangle, Box.Focused, _invalid, !Enabled || Box.ReadOnly, _hover);
            if (!string.IsNullOrEmpty(_prefixIcon))
                SLIcon.Draw(g, _prefixIcon, new Rectangle(12, (Height - 16) / 2, 16, 16), Theme.N400);
            if (!string.IsNullOrEmpty(_unit) && _suffix == null)
            {
                int uw = SLPaint.Measure(_unit, Theme.FontSm).Width;
                SLPaint.Text(g, _unit, Theme.FontSm, new Rectangle(Width - 12 - uw, 0, uw + 1, Height), Theme.TextMuted, TextFormatFlags.Left);
            }
        }
    }

    /// <summary>
    /// NumberField.js: a text field that holds a decimal. Up/Down keys and the mouse wheel step
    /// by Increment. Same members as NumericUpDown (Value, Minimum, Maximum, Increment,
    /// DecimalPlaces, ValueChanged) so it drops in where one was used. 160px wide by default.
    /// </summary>
    [DesignerCategory("Code")]
    internal class SLNumberBox : SLTextBox
    {
        private decimal _value, _min, _max = 100, _increment = 1;
        private int _decimals;
        private bool _syncing;

        public event EventHandler ValueChanged;

        public SLNumberBox()
        {
            Width = 160;
            FixedWidth = true;
            Box.TextAlign = HorizontalAlignment.Left;
            Box.Leave += (s, e) => Commit();
            Box.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Up) { Step(1); e.Handled = true; }
                else if (e.KeyCode == Keys.Down) { Step(-1); e.Handled = true; }
                else if (e.KeyCode == Keys.Enter) Commit();
            };
            Box.MouseWheel += (s, e) => { if (Box.Focused) Step(e.Delta > 0 ? 1 : -1); };
            Render();
        }

        /// <summary>Keeps the 160px width inside an SLField instead of stretching.</summary>
        public bool FixedWidth { get; set; }

        public decimal Minimum { get { return _min; } set { _min = value; Value = _value; } }
        public decimal Maximum { get { return _max; } set { _max = value; Value = _value; } }
        public decimal Increment { get { return _increment; } set { _increment = value; } }
        public int DecimalPlaces { get { return _decimals; } set { _decimals = value; Render(); } }

        public decimal Value
        {
            get { return _value; }
            set
            {
                decimal v = Math.Max(_min, Math.Min(_max, value));
                bool changed = v != _value;
                _value = v;
                Render();
                if (changed && ValueChanged != null) ValueChanged(this, EventArgs.Empty);
            }
        }

        private void Step(int dir) { Value = _value + dir * _increment; Box.SelectAll(); }

        private void Commit()
        {
            decimal parsed;
            if (decimal.TryParse(Box.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out parsed)) Value = parsed;
            else Render();
        }

        private void Render()
        {
            if (_syncing) return;
            _syncing = true;
            Box.Text = _value.ToString("F" + _decimals, CultureInfo.CurrentCulture);
            _syncing = false;
        }
    }

    /// <summary>
    /// Select.js as a ComboBox: same 38px white box, #D0D5DD border, radius 6, 14px text and a
    /// chevron. Subclasses ComboBox, so DataSource / DisplayMember / ValueMember / SelectedValue /
    /// SelectedIndexChanged behave exactly as before — only the painting changes.
    /// Always a drop-down list (no free typing). Set PlaceholderText for the empty state.
    /// </summary>
    [DesignerCategory("Code")]
    internal class SLComboBox : ComboBox, ISLInvalid
    {
        private SLSize _size = SLSize.Md;
        private bool _invalid, _hover;
        private string _placeholder;

        public SLComboBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            DropDownStyle = ComboBoxStyle.DropDownList;
            DrawMode = DrawMode.OwnerDrawFixed;
            FlatStyle = FlatStyle.Flat;
            Font = Theme.FontMd;
            IntegralHeight = false;
            MaxDropDownItems = 10;
            ApplySize();
        }

        public SLSize FieldSize { get { return _size; } set { _size = value; ApplySize(); } }
        public bool Invalid { get { return _invalid; } set { _invalid = value; Invalidate(); } }
        public string PlaceholderText { get { return _placeholder; } set { _placeholder = value; Invalidate(); } }

        private void ApplySize()
        {
            // An owner-drawn drop-down list is ItemHeight + 6 tall.
            ItemHeight = SLFieldBox.HeightFor(_size) - 6;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override void OnSelectedIndexChanged(EventArgs e) { Invalidate(); base.OnSelectedIndexChanged(e); }
        protected override void OnDropDownClosed(EventArgs e) { Invalidate(); base.OnDropDownClosed(e); }
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
            SLFieldBox.Paint(g, ClientRectangle, Focused || DroppedDown, _invalid, !Enabled, _hover);

            bool empty = SelectedIndex < 0;
            string text = empty ? (_placeholder ?? "") : GetItemText(SelectedItem);
            Color ink = !Enabled ? Theme.N400 : empty ? Theme.N400 : Theme.TextBody;
            // Chrome's native <select> adds ~4px inside the 12px padding and draws a small dark
            // arrow close to the right edge (measured from the CI fidelity report).
            SLPaint.TextEllipsis(g, text, Font, new Rectangle(16, 0, Width - 16 - 28, Height), ink, TextFormatFlags.Left);
            SLIcon.Draw(g, "chevron-down", new Rectangle(Width - 15, (Height - 14) / 2, 14, 14), Enabled ? Theme.N800 : Theme.N300, 3.4f);
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            if ((e.State & DrawItemState.ComboBoxEdit) == DrawItemState.ComboBoxEdit) return; // painted in OnPaint
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            using (SolidBrush b = new SolidBrush(selected ? Theme.Indigo50 : Theme.N0))
                e.Graphics.FillRectangle(b, e.Bounds);
            string text = GetItemText(Items[e.Index]);
            Rectangle r = new Rectangle(e.Bounds.X + 16, e.Bounds.Y, e.Bounds.Width - 28, e.Bounds.Height);
            SLPaint.TextEllipsis(e.Graphics, text, Font, r, selected ? Theme.TextHeading : Theme.TextBody, TextFormatFlags.Left);
        }
    }

    /// <summary>
    /// Checkbox.js: 16px box (indigo when checked), 14px/500 label, optional 12px muted
    /// Description below. The box keeps the browser's default checkbox margin (4px left, 3px
    /// right/bottom), so the text starts 33px in. Subclasses CheckBox (Checked / CheckedChanged).
    /// </summary>
    [DesignerCategory("Code")]
    internal class SLCheckBox : CheckBox, ISLMeasure
    {
        private string _description;
        private bool _hover;

        public SLCheckBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Font = Theme.FontMd;
            AutoSize = true;
            Cursor = Cursors.Hand;
        }

        public string Description { get { return _description; } set { _description = value; if (Parent != null) Parent.PerformLayout(this, "Bounds"); Invalidate(); } }

        private static readonly SLTypeSpec LabelSpec = new SLTypeSpec(Theme.FontMdMedium, Theme.TextBody, 1.3f);
        private const int BoxX = 4, TextX = 33; // 4px margin + 16px box + 3px margin + 10px gap

        public override Size GetPreferredSize(Size proposedSize)
        {
            int w = TextX + Math.Max(SLPaint.Measure(Text, LabelSpec.Font).Width,
                                     string.IsNullOrEmpty(_description) ? 0 : SLPaint.Measure(_description, Theme.FontXs).Width) + 1;
            return new Size(w, MeasureHeight(w));
        }

        public int MeasureHeight(int width)
        {
            int h = LabelSpec.LineBox;
            if (!string.IsNullOrEmpty(_description)) h += 2 + SLLayout.WrappedHeight(_description, SLType.Of(SLTextStyle.Caption), Math.Max(10, width - TextX));
            return Math.Max(h, 20); // 1px top margin + 16px box + 3px bottom margin
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (SolidBrush b = new SolidBrush(SLPaint.BackdropOf(this)))
                e.Graphics.FillRectangle(b, ClientRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            OnPaintBackground(e);
            Rectangle box = new Rectangle(BoxX, 1, 16, 16);
            if (Checked)
            {
                Color fill = Enabled ? (_hover ? Theme.ActionPrimaryHover : Theme.ActionPrimary) : Theme.N300;
                SLPaint.Box(g, box, 3, fill, Color.Empty);
                SLIcon.Draw(g, "check", new Rectangle(box.X + 2, box.Y + 2, 12, 12), Color.White, 3f);
            }
            else
            {
                // Chrome's unchecked box: white with a mid-grey 1px border.
                SLPaint.Box(g, box, 3, Enabled ? Theme.N0 : Theme.N50, Enabled ? (_hover ? Theme.N600 : Theme.N500) : Theme.N200);
            }
            if (Focused && ShowFocusCues) SLPaint.FocusRing(g, Rectangle.Inflate(box, -1, -1), 3);

            SLTypeSpec label = LabelSpec;
            if (!Enabled) label.Color = Theme.N400;
            SLPaint.Text(g, Text, label.Font, new Rectangle(TextX, 0, Width - TextX, label.LineBox), label.Color, TextFormatFlags.Left);
            if (!string.IsNullOrEmpty(_description))
                SLLayout.DrawWrapped(g, _description, SLType.Of(SLTextStyle.Caption), new Rectangle(TextX, label.LineBox + 2, Width - TextX, 1000), TextFormatFlags.Left);
        }
    }

    /// <summary>
    /// Toggle.js: 40x22 switch (indigo on, #D0D5DD off) for settings that apply immediately;
    /// 12px gap, 14px label, optional Description. Subclasses CheckBox (Checked / CheckedChanged).
    /// </summary>
    [DesignerCategory("Code")]
    internal class SLToggle : CheckBox, ISLMeasure
    {
        private string _description;

        public SLToggle()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Font = Theme.FontMd;
            AutoSize = true;
            Cursor = Cursors.Hand;
        }

        public string Description { get { return _description; } set { _description = value; if (Parent != null) Parent.PerformLayout(this, "Bounds"); Invalidate(); } }

        private static readonly SLTypeSpec LabelSpec = new SLTypeSpec(Theme.FontMdMedium, Theme.TextBody, 1.3f);

        private int TextHeight(int width)
        {
            int h = LabelSpec.LineBox;
            if (!string.IsNullOrEmpty(_description)) h += 2 + SLLayout.WrappedHeight(_description, SLType.Of(SLTextStyle.Caption), Math.Max(10, width - 52));
            return h;
        }

        public int MeasureHeight(int width) { return Math.Max(22, TextHeight(width)); }

        public override Size GetPreferredSize(Size proposedSize)
        {
            int w = 52 + Math.Max(SLPaint.Measure(Text, LabelSpec.Font).Width,
                                  string.IsNullOrEmpty(_description) ? 0 : SLPaint.Measure(_description, Theme.FontXs).Width) + 1;
            return new Size(w, MeasureHeight(w));
        }

        protected override void OnCheckedChanged(EventArgs e) { Invalidate(); base.OnCheckedChanged(e); }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (SolidBrush b = new SolidBrush(SLPaint.BackdropOf(this)))
                e.Graphics.FillRectangle(b, ClientRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            OnPaintBackground(e);
            int top = (Height - 22) / 2;
            Rectangle track = new Rectangle(0, top, 40, 22);
            Color fill = !Enabled ? Theme.N200 : Checked ? Theme.ActionPrimary : Theme.N300;
            SLPaint.Box(g, track, 11, fill, Color.Empty);
            Rectangle knob = new Rectangle(Checked ? 21 : 3, top + 3, 16, 16);
            SLPaint.Shadow(g, knob, 8, 1);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush b = new SolidBrush(Color.White)) g.FillEllipse(b, knob);
            if (Focused && ShowFocusCues) SLPaint.FocusRing(g, Rectangle.Inflate(track, -1, -1), 10);

            int th = TextHeight(Width);
            int y = (Height - th) / 2;
            SLTypeSpec label = LabelSpec;
            if (!Enabled) label.Color = Theme.N400;
            SLPaint.Text(g, Text, label.Font, new Rectangle(52, y, Width - 52, label.LineBox), label.Color, TextFormatFlags.Left);
            if (!string.IsNullOrEmpty(_description))
                SLLayout.DrawWrapped(g, _description, SLType.Of(SLTextStyle.Caption), new Rectangle(52, y + label.LineBox + 2, Width - 52, 1000), TextFormatFlags.Left);
        }
    }

    /// <summary>
    /// Field box around a control that has no SL version, so it reads like the other fields:
    /// same white box, #D0D5DD border, radius 6, indigo when focused.
    ///   ListBox / CheckedListBox / ListView: border removed, inset 4px (set Height, e.g. 96).
    ///   Editable ComboBox (DropDown style), NumericUpDown, single-line TextBox: 38px high,
    ///   vertically centred, 12px side padding; a stock combo's own flat border is clipped away.
    ///   new SLFrame(productCombo)       new SLFrame(new CheckedListBox()) { Height = 96 }
    /// </summary>
    [DesignerCategory("Code")]
    internal class SLFrame : SLControl, ISLSurface
    {
        private readonly Control _child;
        private readonly Panel _clip;   // hides a stock ComboBox's / DateTimePicker's own border
        private readonly int _clipInset;

        public SLFrame(Control child)
        {
            _child = child;
            if (child.Parent != null) child.Parent.Controls.Remove(child);
            child.Dock = DockStyle.None;
            child.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            child.Margin = Padding.Empty;
            child.Visible = true;
            child.BackColor = Theme.N0;
            child.ForeColor = Theme.TextBody;

            ListBox list = child as ListBox;
            if (list != null) { list.BorderStyle = BorderStyle.None; list.IntegralHeight = false; list.Font = Theme.FontSm; }
            ListView view = child as ListView;
            if (view != null) view.BorderStyle = BorderStyle.None;
            NumericUpDown number = child as NumericUpDown;
            if (number != null) { number.BorderStyle = BorderStyle.None; number.Font = Theme.FontMd; }
            TextBox text = child as TextBox;
            if (text != null && !text.Multiline) { text.BorderStyle = BorderStyle.None; text.Font = Theme.FontMd; }

            ComboBox combo = child as ComboBox;
            DateTimePicker date = child as DateTimePicker;
            if ((combo != null && !(combo is SLComboBox)) || date != null)
            {
                if (combo != null) { combo.FlatStyle = FlatStyle.Flat; combo.Font = Theme.FontMd; _clipInset = 1; }
                if (date != null) { date.Font = Theme.FontMd; date.CalendarFont = Theme.FontSm; _clipInset = 2; }
                _clip = new Panel { BackColor = Theme.N0 };
                _clip.Controls.Add(child);
                Controls.Add(_clip);
            }
            else Controls.Add(child);

            child.GotFocus += (s, e) => Invalidate();
            child.LostFocus += (s, e) => Invalidate();
            Height = IsSingleLine ? Theme.HeightMd : 96;
        }

        public Control Child { get { return _child; } }

        public Color SurfaceFor(Control child) { return Enabled ? Theme.N0 : Theme.N50; }

        private bool IsSingleLine
        {
            get
            {
                TextBox t = _child as TextBox;
                return _child is ComboBox || _child is NumericUpDown || _child is DateTimePicker || (t != null && !t.Multiline);
            }
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (_clip != null)
            {
                int h = _child.Height;
                int inner = Math.Max(4, h - 2 * _clipInset);
                _clip.SetBounds(11, (Height - inner) / 2, Math.Max(10, Width - 14), inner);
                _child.SetBounds(-_clipInset, -_clipInset, _clip.Width + 2 * _clipInset, h);
            }
            else if (IsSingleLine)
            {
                // Global restyling (ThemeStyles.Inputs) gives spinners a border back.
                NumericUpDown n = _child as NumericUpDown;
                if (n != null && n.BorderStyle != BorderStyle.None) n.BorderStyle = BorderStyle.None;
                int h = _child.Height;
                _child.SetBounds(12, (Height - h) / 2, Math.Max(10, Width - 16), h);
            }
            else _child.SetBounds(5, 5, Math.Max(0, Width - 10), Math.Max(0, Height - 10));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            SLFieldBox.Paint(e.Graphics, ClientRectangle, _child.ContainsFocus, false, !Enabled, false);
        }
    }
}
