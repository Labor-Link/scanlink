using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScanLink.DesignSystem
{
    /// <summary>
    /// The mockup's Dialog (design/mockup/source/components/layout/Dialog.js). Inherit from it
    /// for every dialog in the app; never lay a dialog out with Location/Size by hand.
    ///
    ///   Header  padding 20/20/0: Title 16px/600 (red for Danger, amber for Warning),
    ///           Description 13px muted 4px below, ✕ close at top right.
    ///   Body    vertical SLStack, padding 16/20, gap 16 — add content to Body.
    ///   Footer  #FCFCFD band, 1px #F1F3F7 top border, padding 16/20, buttons right-aligned,
    ///           gap 8 — add with AddAction(); order them secondary first, primary last.
    ///   Window  white, radius 12 (Windows 11), OS drop shadow, dimmed backdrop over the owner.
    ///   Width   DialogWidth (520 default, as in the mockup); height follows the content.
    ///   Embedded  When hosted as a page (TopLevel = false, see EmbeddedFormHost) the same
    ///           panel is drawn as a card on the app background, 24px from the top-left,
    ///           without the close button or backdrop.
    ///
    ///   &lt;Dialog title="Remove this scanner?" tone="danger" width="440px" description="…"
    ///           actions={&lt;&gt;&lt;Button variant="secondary"&gt;Keep it&lt;/Button&gt;&lt;Button variant="danger"&gt;Remove scanner&lt;/Button&gt;&lt;/&gt;} /&gt;
    ///   var d = new SLDialog { Title = "Remove this scanner?", Tone = SLDialogTone.Danger, DialogWidth = 440, Description = "…" };
    ///   d.AddAction(new SLButton { Text = "Keep it", Variant = SLVariant.Secondary, DialogResult = DialogResult.Cancel });
    ///   d.AddAction(new SLButton { Text = "Remove scanner", Variant = SLVariant.Danger, DialogResult = DialogResult.OK });
    /// </summary>
    [DesignerCategory("Code")]
    internal class SLDialog : Form, ISLSurface
    {
        private string _title, _description;
        private SLDialogTone _tone = SLDialogTone.Default;
        private int _width = 520;
        private int _bodyHeight;
        private bool _showClose = true, _closeHover, _roundedByDwm;
        private Form _overlay;

        public SLDialog()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            MaximizeBox = false;
            MinimizeBox = false;
            KeyPreview = true;
            BackColor = Theme.SurfaceCard;
            ForeColor = Theme.TextBody;
            Font = Theme.FontSm;

            Body = new SLStack(SLOrientation.Vertical, 16) { Padding = new Padding(20, 16, 20, 16) };
            Footer = new SLStack(SLOrientation.Horizontal, 8) { Padding = new Padding(20, 16, 20, 16), Justify = SLJustify.End, Align = SLAlign.Center };
            Controls.Add(Body);
            Controls.Add(Footer);
            Footer.ControlAdded += (s, e) => PerformLayout();
            Footer.ControlRemoved += (s, e) => PerformLayout();
            ClientSize = new Size(_width, 200);
        }

        public string Title { get { return _title; } set { _title = value; Text = value; PerformLayout(); Invalidate(); } }
        public string Description { get { return _description; } set { _description = value; PerformLayout(); Invalidate(); } }
        public SLDialogTone Tone { get { return _tone; } set { _tone = value; Invalidate(); } }

        /// <summary>The mockup's width prop, in px. Default 520.</summary>
        public int DialogWidth { get { return _width; } set { _width = value; PerformLayout(); } }

        /// <summary>Fixed body height (e.g. for a log or a grid that scrolls). 0 = fit content.</summary>
        public int BodyHeight { get { return _bodyHeight; } set { _bodyHeight = value; PerformLayout(); } }

        public bool ShowClose { get { return _showClose; } set { _showClose = value; Invalidate(); } }

        /// <summary>Dialog content: vertical stack, gap 16. Use SLField / SLFieldSet / SLText inside.</summary>
        public SLStack Body { get; private set; }

        /// <summary>Footer action row. Prefer AddAction.</summary>
        public SLStack Footer { get; private set; }

        /// <summary>Adds a footer button. Add the secondary first and the primary last.</summary>
        public void AddAction(Control button)
        {
            Footer.Controls.Add(button);
            SLButton b = button as SLButton;
            if (b != null && b.DialogResult == DialogResult.Cancel && CancelButton == null) CancelButton = b;
        }

        private Color TitleColor
        {
            get { return _tone == SLDialogTone.Danger ? Theme.Err500 : _tone == SLDialogTone.Warning ? Theme.Warn500 : Theme.TextHeading; }
        }

        /// <summary>Hosted inside another control as a page rather than shown as a window.</summary>
        private bool Embedded { get { return !TopLevel; } }

        private bool CloseVisible { get { return _showClose && !Embedded; } }

        /// <summary>The white dialog panel inside the client area.</summary>
        private Rectangle _panel;

        private int PanelWidth
        {
            get { return Embedded ? Math.Max(200, Math.Min(_width, ClientSize.Width - 48)) : _width; }
        }

        private int TextWidth { get { return PanelWidth - 40 - (CloseVisible ? 12 + 24 : 0); } }

        private int HeaderHeight()
        {
            int h = 20;
            if (!string.IsNullOrEmpty(_title)) h += SLLayout.WrappedHeight(_title, SLType.Of(SLTextStyle.Title), TextWidth);
            if (!string.IsNullOrEmpty(_description)) h += 4 + SLLayout.WrappedHeight(_description, SLType.Of(SLTextStyle.Muted), TextWidth);
            return h;
        }

        private bool HasFooter { get { return Footer.Controls.Count > 0; } }

        private Rectangle CloseRect { get { return new Rectangle(_panel.Right - 20 - 24, _panel.Top + 18, 24, 24); } }

        public Color SurfaceFor(Control child) { return child == Footer ? Theme.N25 : Theme.SurfaceCard; }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (Body == null) return;
            int w = PanelWidth;
            int inset = Embedded ? 1 : 0;              // embedded panel has a 1px card border
            int header = HeaderHeight();
            int footer = HasFooter ? 1 + Footer.MeasureHeight(w - 2 * inset) : 0;
            int body = _bodyHeight > 0 ? _bodyHeight + Body.Padding.Vertical : Body.MeasureHeight(w - 2 * inset);
            int total = header + body + footer + 2 * inset;

            if (Embedded)
            {
                _panel = new Rectangle(24, 24, w, total);
                // The page host scrolls when the card is taller than the page.
                Size min = new Size(0, _panel.Bottom + 24);
                if (MinimumSize != min) MinimumSize = min;
            }
            else
            {
                int maxH = (int)(Screen.FromControl(this).WorkingArea.Height * 0.9);
                total = Math.Min(maxH, total);
                if (ClientSize.Width != w || ClientSize.Height != total)
                    ClientSize = new Size(w, total);
                _panel = new Rectangle(0, 0, w, total);
            }

            Body.SetBounds(_panel.X + inset, _panel.Y + inset + header, w - 2 * inset,
                           Math.Max(0, total - 2 * inset - header - footer));
            Footer.Visible = HasFooter;
            if (HasFooter)
                Footer.SetBounds(_panel.X + inset, _panel.Bottom - inset - footer + 1, w - 2 * inset, footer - 1);
            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (SolidBrush b = new SolidBrush(Embedded ? Theme.SurfaceApp : Theme.SurfaceCard))
                e.Graphics.FillRectangle(b, ClientRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            if (Embedded)
                SLPaint.Box(g, _panel, Theme.RadiusLg, Theme.SurfaceCard, Theme.BorderDefault);

            int x = _panel.X + 20, y = _panel.Y + 20;
            if (!string.IsNullOrEmpty(_title))
            {
                SLTypeSpec t = SLType.Of(SLTextStyle.Title); t.Color = TitleColor;
                y += SLLayout.DrawWrapped(g, _title, t, new Rectangle(x, y, TextWidth, 1000), TextFormatFlags.Left);
            }
            if (!string.IsNullOrEmpty(_description))
                SLLayout.DrawWrapped(g, _description, SLType.Of(SLTextStyle.Muted), new Rectangle(x, y + 4, TextWidth, 1000), TextFormatFlags.Left);

            if (CloseVisible)
                SLPaint.Text(g, "\u2715", Theme.FontLg, CloseRect, _closeHover ? Theme.N600 : Theme.N400, TextFormatFlags.HorizontalCenter);

            if (HasFooter && Footer.Visible)
            {
                int top = Footer.Top - 1;
                Rectangle band = new Rectangle(Footer.Left, top, Footer.Width, Footer.Height + 1);
                if (Embedded)
                {
                    using (System.Drawing.Drawing2D.GraphicsPath inner = SLPaint.RoundedRect(
                        new RectangleF(_panel.X + 1, _panel.Y + 1, _panel.Width - 2, _panel.Height - 2), Theme.RadiusLg - 1))
                    using (Region clip = new Region(inner))
                    {
                        clip.Intersect(band);
                        using (SolidBrush b = new SolidBrush(Theme.N25)) g.FillRegion(b, clip);
                    }
                }
                else
                {
                    using (SolidBrush b = new SolidBrush(Theme.N25)) g.FillRectangle(b, band);
                }
                SLPaint.HLine(g, Theme.BorderSubtle, band.Left, band.Right, top);
            }

            if (!Embedded && !_roundedByDwm)
            {
                using (Pen p = new Pen(Theme.BorderDefault))
                    g.DrawRectangle(p, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool over = CloseVisible && CloseRect.Contains(e.Location);
            if (over != _closeHover) { _closeHover = over; Cursor = over ? Cursors.Hand : Cursors.Default; Invalidate(CloseRect); }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_closeHover) { _closeHover = false; Invalidate(CloseRect); }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (CloseVisible && CloseRect.Contains(e.Location)) { DialogResult = DialogResult.Cancel; Close(); }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape && CancelButton == null && !Embedded) { DialogResult = DialogResult.Cancel; Close(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ---- window chrome -------------------------------------------------------------

        private const int WM_NCHITTEST = 0x84, HTCLIENT = 1, HTCAPTION = 2;
        private const int CS_DROPSHADOW = 0x20000;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33, DWMWCP_ROUND = 2;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int pref = DWMWCP_ROUND;
                _roundedByDwm = DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int)) == 0;
            }
            catch (Exception) { _roundedByDwm = false; } // Windows 10: square corners + 1px border
        }

        /// <summary>The header strip drags the window, like a title bar.</summary>
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_NCHITTEST && (int)m.Result == HTCLIENT)
            {
                Point p = PointToClient(new Point((short)((long)m.LParam & 0xFFFF), (short)(((long)m.LParam >> 16) & 0xFFFF)));
                if (!Embedded && p.Y < Body.Top && !(CloseVisible && CloseRect.Contains(p))) m.Result = (IntPtr)HTCAPTION;
            }
        }

        // ---- backdrop ------------------------------------------------------------------

        private sealed class Backdrop : Form
        {
            public Backdrop()
            {
                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.Manual;
                BackColor = Theme.Overlay;
                Opacity = Theme.OverlayOpacity;
                Enabled = false; // clicks cannot activate it above the dialog
            }
            protected override bool ShowWithoutActivation { get { return true; } }
        }

        protected override void OnLoad(EventArgs e)
        {
            PerformLayout();
            base.OnLoad(e);
        }

        /// <summary>
        /// Backdrop and centring for modal use only. Follows visibility rather than load/close,
        /// because some dialogs (PrinterConnectionDialog) hide instead of closing and are
        /// shown again later.
        /// </summary>
        protected override void OnVisibleChanged(EventArgs e)
        {
            if (Visible && TopLevel) ShowBackdrop();
            else HideBackdrop();
            base.OnVisibleChanged(e);
        }

        private void ShowBackdrop()
        {
            Form owner = Owner;
            if (owner == null && ActiveForm != null && ActiveForm != this) owner = ActiveForm;
            bool onScreen = Location.X > -10000; // the design gallery renders off-screen
            if (owner != null && owner.Visible && owner.WindowState != FormWindowState.Minimized)
            {
                if (Modal && _overlay == null)
                {
                    _overlay = new Backdrop { Bounds = owner.Bounds };
                    _overlay.Show(owner);
                    BringToFront();
                }
                if (onScreen) Location = new Point(owner.Left + (owner.Width - Width) / 2, owner.Top + (owner.Height - Height) / 2);
            }
            else if (onScreen)
            {
                Rectangle wa = Screen.FromPoint(Cursor.Position).WorkingArea;
                Location = new Point(wa.Left + (wa.Width - Width) / 2, wa.Top + (wa.Height - Height) / 2);
            }
        }

        private void HideBackdrop()
        {
            if (_overlay == null) return;
            _overlay.Close();
            _overlay.Dispose();
            _overlay = null;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            HideBackdrop();
            base.OnFormClosed(e);
        }
    }
}
