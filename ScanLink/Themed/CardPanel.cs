using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScanLink.Themed
{
    /// <summary>
    /// The redesign's primary container: white ground, 1px border, 12px radius, with an
    /// optional title / subtitle / actions header and a footer.
    ///
    /// Owner-drawn rather than Region-clipped: a Region shows whatever is behind the card at
    /// the corners and stair-steps the border, defeating the point of the radius.
    ///
    /// LAYOUT RULE, learned from a broken build: the header and footer use EXPLICIT heights,
    /// never AutoSize. A container with AutoSize=true whose children are docked collapses to
    /// zero in WinForms — the children size against the parent while the parent sizes
    /// against the children. That rendered every card as an empty white rectangle.
    /// </summary>
    internal class CardPanel : Panel
    {
        private const int HeaderPadTop = Theme.S4;
        private const int TitleHeight = 24;
        private const int SubtitleHeight = 20;

        private readonly Panel _header;
        private readonly Panel _body;
        private readonly Panel _footer;
        private readonly Label _titleLabel;
        private readonly Label _subtitleLabel;
        private readonly FlowLayoutPanel _actions;

        public CardPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.UserPaint
                     | ControlStyles.ResizeRedraw, true);

            BackColor = Color.Transparent;

            _body = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(Theme.S5, Theme.S4, Theme.S5, Theme.S4)
            };

            _titleLabel = new Label
            {
                AutoSize = true,
                Font = Theme.FontLgBold,
                ForeColor = Theme.TextHeading,
                Location = new Point(Theme.S5, HeaderPadTop),
                BackColor = Color.Transparent,
                UseMnemonic = false,
                Visible = false
            };

            _subtitleLabel = new Label
            {
                AutoSize = true,
                Font = Theme.FontSm,
                ForeColor = Theme.TextMuted,
                Location = new Point(Theme.S5, HeaderPadTop + TitleHeight),
                BackColor = Color.Transparent,
                UseMnemonic = false,
                Visible = false
            };

            _actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, Theme.S3, Theme.S5, 0)
            };

            _header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 0,
                BackColor = Color.Transparent,
                Visible = false
            };
            _header.Controls.Add(_actions);
            _header.Controls.Add(_titleLabel);
            _header.Controls.Add(_subtitleLabel);

            _footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Color.Transparent,
                Padding = new Padding(Theme.S5, Theme.S2, Theme.S5, Theme.S2),
                Visible = false
            };

            // Fill first: WinForms resolves docking from the last added control backwards,
            // so the Fill added first claims what the edges leave.
            Controls.Add(_body);
            Controls.Add(_header);
            Controls.Add(_footer);
        }

        [Browsable(false)] public Panel Body { get { return _body; } }
        [Browsable(false)] public FlowLayoutPanel Actions { get { return _actions; } }
        [Browsable(false)] public Panel Footer { get { return _footer; } }

        public string Title
        {
            get { return _titleLabel.Text; }
            set
            {
                _titleLabel.Text = value ?? string.Empty;
                _titleLabel.Visible = !string.IsNullOrEmpty(value);
                UpdateHeader();
            }
        }

        public string Subtitle
        {
            get { return _subtitleLabel.Text; }
            set
            {
                _subtitleLabel.Text = value ?? string.Empty;
                _subtitleLabel.Visible = !string.IsNullOrEmpty(value);
                UpdateHeader();
            }
        }

        /// <summary>Set false for a card holding a full-bleed table.</summary>
        public bool BodyPadding
        {
            get { return _body.Padding != Padding.Empty; }
            set
            {
                _body.Padding = value
                    ? new Padding(Theme.S5, Theme.S4, Theme.S5, Theme.S4)
                    : Padding.Empty;
            }
        }

        public void ShowFooter(bool visible) { _footer.Visible = visible; }

        /// <summary>Header height is computed from content rather than left to AutoSize.</summary>
        private void UpdateHeader()
        {
            bool hasText = _titleLabel.Visible || _subtitleLabel.Visible;
            bool hasActions = _actions.Controls.Count > 0;

            if (!hasText && !hasActions)
            {
                _header.Visible = false;
                _header.Height = 0;
                return;
            }

            int height = HeaderPadTop + Theme.S3;
            if (_titleLabel.Visible) height += TitleHeight;
            if (_subtitleLabel.Visible) height += SubtitleHeight;
            if (!hasText) height = 52;

            // A subtitle with no title sits where the title would have been.
            _subtitleLabel.Top = HeaderPadTop + (_titleLabel.Visible ? TitleHeight : 0);

            _header.Height = height;
            _header.Visible = true;
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            UpdateHeader();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Inset by 1px so the border draws inside the bounds rather than being clipped.
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
