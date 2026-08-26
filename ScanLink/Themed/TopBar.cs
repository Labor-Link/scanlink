using System;
using System.Drawing;
using System.Windows.Forms;

namespace ScanLink.Themed
{
    /// <summary>
    /// Page header above every screen: the page title, a plain-language line saying what the
    /// page is for, and at most one primary action.
    /// </summary>
    internal class TopBar : Panel
    {
        private readonly Label _title;
        private readonly Label _subtitle;
        private readonly FlowLayoutPanel _actions;

        public TopBar()
        {
            Dock = DockStyle.Top;
            Height = Theme.TopBarHeight;
            BackColor = Theme.SurfaceCard;
            Padding = new Padding(Theme.S6, 0, Theme.S6, 0);

            _actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Padding = new Padding(0, Theme.S2, 0, Theme.S2)
            };

            // Title and subtitle live in a Fill panel rather than at fixed coordinates. As
            // AutoSize labels pinned left they ran under the docked actions and the subtitle
            // was cut mid-word.
            Panel textColumn = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0, Theme.S2, Theme.S4, Theme.S2)
            };

            _subtitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 18,
                Font = Theme.FontSm,
                ForeColor = Theme.TextMuted,
                AutoSize = false,
                AutoEllipsis = true,
                BackColor = Color.Transparent,
                UseMnemonic = false
            };

            _title = new Label
            {
                Dock = DockStyle.Top,
                Height = 26,
                Font = Theme.Font2Xl,
                ForeColor = Theme.TextHeading,
                AutoSize = false,
                AutoEllipsis = true,
                BackColor = Color.Transparent,
                UseMnemonic = false
            };

            textColumn.Controls.Add(_subtitle);
            textColumn.Controls.Add(_title);

            Controls.Add(textColumn);
            Controls.Add(_actions);
        }

        public FlowLayoutPanel Actions { get { return _actions; } }

        public void SetPage(string title, string subtitle)
        {
            _title.Text = title ?? string.Empty;
            _subtitle.Text = subtitle ?? string.Empty;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen pen = new Pen(Theme.BorderDefault, 1f))
            {
                e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
            }
        }
    }
}
