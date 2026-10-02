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

        /// <summary>Header height for a card with both a title and a subtitle, and the
        /// vertical body padding. Callers that set an explicit Height have to budget for
        /// both — a card sized without them silently clips its own body, which is how the
        /// step cards lost their buttons.</summary>
        public const int TitledHeaderHeight = HeaderPadTop + Theme.S3 + TitleHeight + SubtitleHeight;
        public const int BodyPaddingV = Theme.S4 * 2;

        private readonly Panel _header;
        private readonly Panel _body;
        private readonly Panel _footer;
        private readonly FlowLayoutPanel _actions;
        private string _title = string.Empty;
        private string _subtitle = string.Empty;

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

            // The header reserves space and hosts the action buttons; the title and subtitle
            // are painted by this control, not carried as child Labels. Transparent child
            // labels over an owner-painted parent did not render at all here, which left
            // every card headerless while still reserving the header's height.
            _header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 0,
                BackColor = Color.Transparent,
                Visible = false
            };
            _header.Controls.Add(_actions);

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
            get { return _title; }
            set { _title = value ?? string.Empty; UpdateHeader(); Invalidate(); }
        }

        public string Subtitle
        {
            get { return _subtitle; }
            set { _subtitle = value ?? string.Empty; UpdateHeader(); Invalidate(); }
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
            bool hasTitle = !string.IsNullOrEmpty(_title);
            bool hasSubtitle = !string.IsNullOrEmpty(_subtitle);
            bool hasActions = _actions.Controls.Count > 0;

            if (!hasTitle && !hasSubtitle && !hasActions)
            {
                _header.Visible = false;
                _header.Height = 0;
                return;
            }

            int height = HeaderPadTop + Theme.S3;
            if (hasTitle) height += TitleHeight;
            if (hasSubtitle) height += SubtitleHeight;
            if (!hasTitle && !hasSubtitle) height = 52;

            _header.Height = height;
            _header.Visible = true;
        }

        /// <summary>Draws the header text. Called from OnPaint, after the card ground.</summary>
        private void PaintHeaderText(Graphics g)
        {
            if (!_header.Visible) return;

            int right = Width - Theme.S5;
            if (_actions.Controls.Count > 0) right = Math.Min(right, _actions.Left - Theme.S3);
            int width = Math.Max(40, right - Theme.S5);
            int top = HeaderPadTop;

            const TextFormatFlags flags =
                TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;

            if (!string.IsNullOrEmpty(_title))
            {
                TextRenderer.DrawText(g, _title, Theme.FontLgBold,
                    new Rectangle(Theme.S5, top, width, TitleHeight), Theme.TextHeading, flags);
                top += TitleHeight;
            }
            if (!string.IsNullOrEmpty(_subtitle))
            {
                TextRenderer.DrawText(g, _subtitle, Theme.FontSm,
                    new Rectangle(Theme.S5, top, width, SubtitleHeight), Theme.TextMuted, flags);
            }
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            UpdateHeader();
        }

        /// <summary>Re-reserves the header when an action button is added after
        /// construction; Actions is a grandchild, so it does not raise OnControlAdded here.</summary>
        public void RefreshHeader()
        {
            UpdateHeader();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Collapsed cards are sized to zero rather than hidden, so there is nothing to
            // draw and the inset below would produce a negative rectangle.
            if (Width < 2 || Height < 2) return;

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

            PaintHeaderText(g);
            base.OnPaint(e);
        }
    }
}
