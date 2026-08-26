using System;
using System.Drawing;
using System.Windows.Forms;

namespace ScanLink.Themed
{
    /// <summary>
    /// Compact site tile pinned to the sidebar footer — the design system's answer to site
    /// switching living behind a modal. Clicking it re-opens the existing MultiUserDashboard,
    /// so behaviour is unchanged; only the entry point moves.
    /// </summary>
    internal class SiteTileButton : Panel
    {
        private readonly Label _name;
        private readonly Label _meta;
        private bool _hover;

        public event EventHandler Activated;

        public SiteTileButton()
        {
            Dock = DockStyle.Top;
            Height = 52;
            BackColor = Theme.Blend(Color.White, Theme.SurfaceShell, 0.07);
            Cursor = Cursors.Hand;
            Margin = Padding.Empty;

            _name = new Label
            {
                Font = Theme.FontSmBold,
                ForeColor = Theme.TextOnShellStrong,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(Theme.S3, 8),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };

            _meta = new Label
            {
                Font = Theme.FontMonoSm,
                ForeColor = Theme.TextOnShell,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(Theme.S3, 27),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };

            Controls.Add(_name);
            Controls.Add(_meta);

            foreach (Control c in new Control[] { this, _name, _meta })
            {
                c.Click += (s, e) => { EventHandler h = Activated; if (h != null) h(this, EventArgs.Empty); };
                c.MouseEnter += (s, e) => SetHover(true);
                c.MouseLeave += (s, e) => SetHover(false);
            }
        }

        private void SetHover(bool hover)
        {
            if (_hover == hover) return;
            _hover = hover;
            BackColor = Theme.Blend(Color.White, Theme.SurfaceShell, hover ? 0.14 : 0.07);
        }

        public void SetSite(string name, string id)
        {
            _name.Text = string.IsNullOrEmpty(name) ? "No site selected" : name;
            _meta.Text = string.IsNullOrEmpty(id) ? "Tap to choose" : id;
        }
    }
}
