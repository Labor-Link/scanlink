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
    internal class CardPanel : Panel, ScanLink.DesignSystem.ISLSurface
    {
        // Card.js header: padding 16/20, title 16px/600 line 1.3 (21px), 2px gap, subtitle
        // 13px line 1.5 (20px), 1px divider. Painted to match SLCard exactly.
        private const int HeaderPadTop = Theme.S4;
        private const int HeaderPadBottom = Theme.S4;
        private const int TitleHeight = 21;
        private const int TitleGap = 2;
        private const int SubtitleHeight = 20;

        /// <summary>Header height for a card with both a title and a subtitle, and the
        /// vertical body padding. Callers that set an explicit Height have to budget for
        /// both — a card sized without them silently clips its own body, which is how the
        /// step cards lost their buttons.</summary>
        public const int TitledHeaderHeight = HeaderPadTop + TitleHeight + TitleGap + SubtitleHeight + HeaderPadBottom + 1;
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
                Padding = new Padding(Theme.S5, Theme.S3, Theme.S5, Theme.S3),
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

            int height = HeaderPadTop + HeaderPadBottom + 1;
            if (hasTitle) height += TitleHeight;
            if (hasTitle && hasSubtitle) height += TitleGap;
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

            if (!string.IsNullOrEmpty(_title))
            {
                ScanLink.DesignSystem.SLPaint.TextEllipsis(g, _title, Theme.FontLgSemibold,
                    new Rectangle(Theme.S5, top, width, TitleHeight), Theme.TextHeading, TextFormatFlags.Left);
                top += TitleHeight + TitleGap;
            }
            if (!string.IsNullOrEmpty(_subtitle))
            {
                ScanLink.DesignSystem.SLPaint.TextEllipsis(g, _subtitle, Theme.FontSm,
                    new Rectangle(Theme.S5, top, width, SubtitleHeight), Theme.TextMuted, TextFormatFlags.Left);
            }
            ScanLink.DesignSystem.SLPaint.HLine(g, Theme.BorderSubtle, 1, Width - 1, _header.Bottom - 1);
        }

        /// <summary>SL controls inside a card blend their corners into white (body) or the
        /// footer band.</summary>
        public Color SurfaceFor(Control child) { return child == _footer ? Theme.N25 : Theme.SurfaceCard; }

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
            ScanLink.DesignSystem.SLPaint.Box(g, ClientRectangle, Theme.RadiusLg, Theme.SurfaceCard, Theme.BorderDefault);

            if (_footer.Visible && _footer.Height > 0)
            {
                // Footer band (#FCFCFD) clipped to the card's rounded inner shape, top divider.
                Rectangle band = new Rectangle(1, _footer.Top, Width - 2, Height - 1 - _footer.Top);
                using (GraphicsPath inner = ScanLink.DesignSystem.SLPaint.RoundedRect(new RectangleF(1, 1, Width - 2, Height - 2), Theme.RadiusLg - 1))
                using (Region clip = new Region(inner))
                {
                    clip.Intersect(band);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using (SolidBrush b = new SolidBrush(Theme.N25)) g.FillRegion(b, clip);
                }
                ScanLink.DesignSystem.SLPaint.HLine(g, Theme.BorderSubtle, 1, Width - 1, _footer.Top);
            }

            PaintHeaderText(g);
            base.OnPaint(e);
        }
    }
}
