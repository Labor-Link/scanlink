using System;
using System.Drawing;
using System.Windows.Forms;
using ScanLink.Themed;

namespace ScanLink
{
    /// <summary>
    /// Split-screen login: navy brand panel on the left, the existing sign-in form on the
    /// right. Every control is re-parented, not recreated, so fields and handlers survive.
    ///
    /// LAYOUT NOTE, learned the hard way: the form column is a FlowLayoutPanel, NOT a Panel
    /// with AutoSize and Dock=Top children. That combination collapses to zero height in
    /// WinForms — docked children size against the parent while the parent sizes against its
    /// children — and it clipped the entire sign-in form out of existence, leaving the
    /// right-hand side blank.
    /// </summary>
    public partial class Form1
    {
        private Panel _loginAside;
        private Panel _loginFormHost;
        private FlowLayoutPanel _loginFormStack;
        internal bool V2LoginActive { get { return _loginFormHost != null; } }

        private void BuildV2LoginScreen()
        {
            if (loginPanel == null || _loginFormHost != null) return;

            try
            {
                loginPanel.SuspendLayout();

                loginPanel.AutoScroll = false;
                loginPanel.AutoScrollMinSize = Size.Empty;
                loginPanel.Dock = DockStyle.Fill;
                loginPanel.BackColor = Theme.SurfaceApp;

                _loginFormHost = new Panel { Dock = DockStyle.Fill, BackColor = Theme.SurfaceApp };

                _loginAside = new Panel
                {
                    Dock = DockStyle.Left,
                    Width = 520,
                    BackColor = Theme.SurfaceShell,
                    Padding = new Padding(Theme.S12, Theme.S12, Theme.S12, Theme.S12)
                };

                BuildLoginAside();

                // FlowLayoutPanel measures flowed children correctly, so AutoSize gives the
                // real content height. Children must stay undocked for that to hold.
                _loginFormStack = new FlowLayoutPanel
                {
                    FlowDirection = FlowDirection.TopDown,
                    WrapContents = false,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    BackColor = Color.Transparent
                };

                _loginFormStack.Controls.Add(new Label
                {
                    Text = "Sign in",
                    Font = Theme.Font3Xl,
                    ForeColor = Theme.TextHeading,
                    AutoSize = true,
                    Margin = new Padding(0, 0, 0, Theme.S1),
                    UseMnemonic = false
                });
                _loginFormStack.Controls.Add(new Label
                {
                    Text = "Use the email and password your manager set up for you.",
                    Font = Theme.FontMd,
                    ForeColor = Theme.TextMuted,
                    AutoSize = true,
                    Margin = new Padding(0, 0, 0, Theme.S6),
                    UseMnemonic = false
                });

                // Reading order — FlowLayoutPanel lays out in add order.
                AdoptIntoLoginStack(loginGroupBox);
                AdoptIntoLoginStack(loginStatusLabel);
                AdoptIntoLoginStack(loadingProgressBar);
                AdoptIntoLoginStack(loadingStatusLabel);

                if (loginGroupBox != null)
                {
                    loginGroupBox.BackColor = Theme.SurfaceCard;
                    loginGroupBox.ForeColor = Theme.TextHeading;
                    loginGroupBox.Font = Theme.FontMdBold;
                }

                _loginFormHost.Controls.Add(_loginFormStack);

                // The mark moves to the brand panel rather than appearing twice.
                if (loginMainLogoPictureBox != null) loginMainLogoPictureBox.Visible = false;
                if (loginWelcomeLabel != null) loginWelcomeLabel.Visible = false;

                loginPanel.Controls.Add(_loginFormHost);
                loginPanel.Controls.Add(_loginAside);

                _loginFormHost.Resize += (s, e) => CentreLoginStack();
                _loginFormStack.SizeChanged += (s, e) => CentreLoginStack();
                CentreLoginStack();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[LOGIN] v2 login build failed: " + ex);
            }
            finally
            {
                loginPanel.ResumeLayout(true);
            }
        }

        /// <summary>
        /// Moves an existing login control into the flowed column. Docking is explicitly
        /// cleared: a docked child inside an AutoSize container is what collapsed this
        /// layout the first time.
        /// </summary>
        private void AdoptIntoLoginStack(Control control)
        {
            if (control == null || _loginFormStack == null) return;

            if (control.Parent != null) control.Parent.Controls.Remove(control);
            control.Dock = DockStyle.None;
            control.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            control.Margin = new Padding(0, 0, 0, Theme.S3);
            control.Visible = true;
            _loginFormStack.Controls.Add(control);
        }

        /// <summary>Centres the form column in whatever space is left of the brand panel.</summary>
        private void CentreLoginStack()
        {
            if (_loginFormHost == null || _loginFormStack == null) return;

            int hostW = _loginFormHost.ClientSize.Width;
            int hostH = _loginFormHost.ClientSize.Height;
            if (hostW <= 0 || hostH <= 0) return;

            _loginFormStack.Left = Math.Max(Theme.S8, (hostW - _loginFormStack.Width) / 2);
            _loginFormStack.Top = Math.Max(Theme.S8, (hostH - _loginFormStack.Height) / 2);
        }

        private void BuildLoginAside()
        {
            FlowLayoutPanel stack = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = false,
                BackColor = Color.Transparent
            };

            Image logo = TryLoadSidebarLogo();
            if (logo != null)
            {
                stack.Controls.Add(new PictureBox
                {
                    Image = logo,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Size = new Size(190, 44),
                    Margin = new Padding(0, 0, 0, Theme.S16),
                    BackColor = Color.Transparent
                });
            }

            stack.Controls.Add(new Label
            {
                Text = "Every carton scanned, counted and labelled\r\n— without leaving this screen.",
                Font = Theme.Font4Xl,
                ForeColor = Theme.TextOnShellStrong,
                AutoSize = false,
                Size = new Size(400, 120),
                Margin = new Padding(0, 0, 0, Theme.S10),
                BackColor = Color.Transparent,
                UseMnemonic = false
            });

            // Order matters and reads top-down.
            string[][] bullets =
            {
                new[] { "scan-line",    "Watch scans land from every line, live" },
                new[] { "printer",      "Print barcode and carton labels in a few clicks" },
                new[] { "cloud-upload", "Nothing is lost if the internet drops" }
            };
            foreach (string[] bullet in bullets)
            {
                stack.Controls.Add(BuildLoginBullet(bullet[0], bullet[1]));
            }

            Label footer = new Label
            {
                Text = "ScanLink for packhouses",
                Font = Theme.FontXs,
                ForeColor = Theme.Blend(Color.White, Theme.SurfaceShell, 0.42),
                Dock = DockStyle.Bottom,
                Height = 24,
                BackColor = Color.Transparent,
                UseMnemonic = false
            };

            _loginAside.Controls.Add(stack);
            _loginAside.Controls.Add(footer);
        }

        /// <summary>One brand-panel bullet: an icon in a soft square, then the line of text.</summary>
        private Panel BuildLoginBullet(string iconName, string text)
        {
            Panel row = new Panel
            {
                Size = new Size(420, 44),
                Margin = new Padding(0, 0, 0, Theme.S3),
                BackColor = Color.Transparent
            };

            Panel chip = new Panel
            {
                Size = new Size(32, 32),
                Location = new Point(0, 6),
                BackColor = Theme.Blend(Color.White, Theme.SurfaceShell, 0.10)
            };
            ThemeStyles.RoundedCorners(chip, Theme.RadiusSm);

            Image icon = IconSet.Get(iconName, 16, IconSet.Tint.White);
            if (icon != null)
            {
                chip.Controls.Add(new PictureBox
                {
                    Image = icon,
                    SizeMode = PictureBoxSizeMode.CenterImage,
                    Dock = DockStyle.Fill,
                    BackColor = Color.Transparent
                });
            }

            row.Controls.Add(chip);
            row.Controls.Add(new Label
            {
                Text = text,
                Font = Theme.FontMd,
                ForeColor = Theme.TextOnShell,
                AutoSize = false,
                Size = new Size(370, 32),
                Location = new Point(46, 6),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                UseMnemonic = false
            });
            return row;
        }
    }
}
