using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScanLink.Themed
{
    /// <summary>
    /// Rounded search input with a leading icon and placeholder text.
    ///
    /// The placeholder is hand-rolled: TextBox.PlaceholderText arrived in .NET Core and this
    /// app targets .NET Framework 4.8, so the grey prompt is painted as real text and
    /// cleared on focus. <see cref="Query"/> therefore never returns the prompt string.
    /// </summary>
    internal class SearchField : Panel
    {
        private readonly TextBox _input;
        private readonly Label _icon;
        private readonly string _placeholder;
        private bool _showingPlaceholder;

        /// <summary>Raised as the operator types, debounced by the caller if needed.</summary>
        public event EventHandler QueryChanged;

        public SearchField(string placeholder)
        {
            _placeholder = placeholder ?? string.Empty;

            SetStyle(ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.UserPaint
                     | ControlStyles.ResizeRedraw, true);

            Height = Theme.HeightMd;
            BackColor = Theme.SurfaceCard;

            _icon = new Label
            {
                AutoSize = false,
                Size = new Size(16, 16),
                Location = new Point(Theme.S3, (Theme.HeightMd - 16) / 2),
                BackColor = Color.Transparent,
                Image = IconSet.Get("search", 16, IconSet.Tint.Dark)
            };
            if (_icon.Image == null) { _icon.Text = "⚲"; _icon.ForeColor = Theme.TextMuted; }

            _input = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = Theme.FontSm,
                BackColor = Theme.SurfaceCard,
                ForeColor = Theme.TextMuted,
                Left = Theme.S3 + 16 + Theme.S2,
                Top = (Theme.HeightMd - 17) / 2,
                Height = 17,
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right,
                Text = _placeholder
            };
            _showingPlaceholder = true;

            _input.GotFocus += (s, e) =>
            {
                Invalidate();   // the border is drawn by this panel, not the TextBox
                if (!_showingPlaceholder) return;
                _showingPlaceholder = false;
                _input.Text = string.Empty;
                _input.ForeColor = Theme.TextBody;
            };
            _input.LostFocus += (s, e) =>
            {
                Invalidate();
                if (_input.Text.Length != 0) return;
                _showingPlaceholder = true;
                _input.Text = _placeholder;
                _input.ForeColor = Theme.TextMuted;
            };
            _input.TextChanged += (s, e) =>
            {
                if (_showingPlaceholder) return;
                EventHandler handler = QueryChanged;
                if (handler != null) handler(this, EventArgs.Empty);
            };

            Controls.Add(_icon);
            Controls.Add(_input);
        }

        /// <summary>The typed query, or empty while the placeholder is showing.</summary>
        public string Query
        {
            get { return _showingPlaceholder ? string.Empty : _input.Text.Trim(); }
        }

        public void Clear()
        {
            _showingPlaceholder = true;
            _input.Text = _placeholder;
            _input.ForeColor = Theme.TextMuted;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            _input.Width = Math.Max(20, Width - _input.Left - Theme.S3);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = Theme.RoundedPath(bounds, Theme.RadiusMd))
            using (SolidBrush fill = new SolidBrush(Theme.SurfaceCard))
            using (Pen border = new Pen(_input.Focused ? Theme.FocusRing : Theme.BorderStrong, 1f))
            {
                g.FillPath(fill, path);
                g.DrawPath(border, path);
            }
            base.OnPaint(e);
        }
    }
}
