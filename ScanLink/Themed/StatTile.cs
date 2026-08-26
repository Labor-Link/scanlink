using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScanLink.Themed
{
    /// <summary>
    /// Headline metric card — a plain-language caption above a large number.
    ///
    /// Built to wrap the existing count labels rather than replace them: Form1's
    /// UpdateCountLabels writes into todayScansLabel / lastHourScansLabel / seasonScansLabel
    /// and that code path is untouched.
    /// </summary>
    internal class StatTile : Panel
    {
        private readonly Label _caption;
        private readonly Label _value;
        private readonly Label _footnote;

        public StatTile()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.UserPaint
                     | ControlStyles.ResizeRedraw, true);

            BackColor = Color.Transparent;
            Padding = new Padding(Theme.S5, Theme.S4, Theme.S5, Theme.S4);
            Size = new Size(220, 104);

            _footnote = new Label
            {
                Dock = DockStyle.Bottom,
                AutoSize = false,
                Height = 16,
                Font = Theme.FontXs,
                ForeColor = Theme.TextMuted,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false,
                Visible = false
            };

            _value = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                Font = Theme.Font3Xl,
                ForeColor = Theme.TextHeading,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "—",
                UseMnemonic = false
            };

            _caption = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 22,
                Font = Theme.FontSm,
                ForeColor = Theme.TextMuted,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false,
                AutoEllipsis = true
            };

            Controls.Add(_value);
            Controls.Add(_caption);
            Controls.Add(_footnote);
        }

        /// <summary>Plain-language caption, e.g. "Scanned today".</summary>
        public string Caption
        {
            get { return _caption.Text; }
            set { _caption.Text = value ?? string.Empty; }
        }

        /// <summary>The metric. An em dash is the app's existing "no value yet" convention.</summary>
        public string Value
        {
            get { return _value.Text; }
            set { _value.Text = string.IsNullOrEmpty(value) ? "—" : value; }
        }

        public string Footnote
        {
            get { return _footnote.Text; }
            set
            {
                _footnote.Text = value ?? string.Empty;
                _footnote.Visible = !string.IsNullOrEmpty(value);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = Theme.RoundedPath(bounds, Theme.RadiusLg))
            using (SolidBrush fill = new SolidBrush(Theme.SurfaceCard))
            using (Pen border = new Pen(Theme.BorderDefault, 1f))
            {
                g.FillPath(fill, path);
                g.DrawPath(border, path);
            }
            base.OnPaint(e);
        }
    }
}
