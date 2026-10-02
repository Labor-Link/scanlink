using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ScanLink.DesignSystem;
using ScanLink.Themed;

namespace ScanLink
{
    /// <summary>
    /// The mockup's Login screen (design/mockup/screens/LoginScreen.js): navy brand panel on the
    /// left, the sign-in form centred on the right.
    ///
    /// The email and password boxes are Form1's existing TextBoxes, wrapped in SLTextBox, and
    /// loginButton / passwordToggleButton are replaced by SL controls in their fields, so the
    /// sign-in code in Form1.cs (Text, Enabled, PerformClick, PasswordChar) runs unchanged.
    /// loginStatusLabel and loadingStatusLabel stay as Form1's message channel but are no longer
    /// shown; their text and colour are mirrored into banners.
    ///
    /// The mockup's "Keep me signed in" and "Forgot password?" are left out: the client has no
    /// such features (sessions are restored automatically).
    /// </summary>
    public partial class Form1
    {
        private Panel _loginFormHost;
        private LoginLayout _login;
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

                // Replace the two buttons with SL controls; Form1 keeps using the same fields.
                Button oldLogin = loginButton;
                oldLogin.Click -= loginButton_Click;
                if (oldLogin.Parent != null) oldLogin.Parent.Controls.Remove(oldLogin);
                loginButton = new SLButton { Name = "loginButton", Text = "Sign in" };
                loginButton.Click += loginButton_Click;

                Button oldToggle = passwordToggleButton;
                oldToggle.Click -= passwordToggleButton_Click;
                if (oldToggle.Parent != null) oldToggle.Parent.Controls.Remove(oldToggle);
                passwordToggleButton = new SLIconButton { Name = "passwordToggleButton", ButtonSize = SLSize.Sm, IconName = "eye", Label = "Show password", IconSize = 18 };
                passwordToggleButton.Click += passwordToggleButton_Click;

                _login = LoginLayout.Compose(loginPanel, usernameTextBox, passwordTextBox,
                    (SLButton)loginButton, (SLIconButton)passwordToggleButton, TryLoadSidebarLogo());
                _loginFormHost = _login.FormHost;

                // The old group box, logo and welcome text are superseded.
                if (loginGroupBox != null) loginGroupBox.Visible = false;
                if (loginMainLogoPictureBox != null) loginMainLogoPictureBox.Visible = false;
                if (loginWelcomeLabel != null) loginWelcomeLabel.Visible = false;

                // Form1's status labels become message sources for the banners.
                Detach(loginStatusLabel);
                Detach(loadingStatusLabel);
                Detach(loadingProgressBar);
                loginStatusLabel.TextChanged += (s, e) => SyncLoginMessages();
                loginStatusLabel.ForeColorChanged += (s, e) => SyncLoginMessages();
                loginStatusLabel.VisibleChanged += (s, e) => SyncLoginMessages();
                loadingStatusLabel.TextChanged += (s, e) => SyncLoginMessages();
                loadingStatusLabel.VisibleChanged += (s, e) => SyncLoginMessages();
                loginButton.TextChanged += (s, e) => ((SLButton)loginButton).Loading = loginButton.Text.StartsWith("Signing in", StringComparison.Ordinal);
                SyncLoginMessages();
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

        private static void Detach(Control c)
        {
            if (c != null && c.Parent != null) c.Parent.Controls.Remove(c);
        }

        /// <summary>
        /// Red status text -> error banner; orange ("Authenticating…") -> info banner; the grey
        /// idle prompt -> nothing (the screen's description already says it). Loading messages
        /// ("Loading dashboard…") -> an info banner with a spinner icon.
        /// </summary>
        private void SyncLoginMessages()
        {
            if (_login == null) return;
            string status = IconSet.StripLeadingGlyph(loginStatusLabel.Text ?? "");
            int ink = loginStatusLabel.ForeColor.ToArgb();
            bool error = ink == Color.Red.ToArgb() || ink == Theme.Err500.ToArgb() || ink == Theme.Err700.ToArgb();
            bool info = ink == Color.Orange.ToArgb() || ink == Theme.Info500.ToArgb() || ink == Theme.Warn500.ToArgb();
            bool showStatus = loginStatusLabel.Visible && status.Length > 0 && (error || info);
            _login.ShowStatus(showStatus, error ? SLTone.Error : SLTone.Info, status);

            string loading = loadingStatusLabel.Text ?? "";
            _login.ShowLoading(loadingStatusLabel.Visible && loading.Length > 0, loading);
        }

        /// <summary>
        /// Sets the reveal-password icon to match the field's state: eye = click to reveal,
        /// eye-off = click to hide.
        /// </summary>
        private void SetPasswordToggleIcon(bool revealed)
        {
            SLIconButton toggle = passwordToggleButton as SLIconButton;
            if (toggle != null)
            {
                toggle.IconName = revealed ? "eye-off" : "eye";
                toggle.Label = revealed ? "Hide password" : "Show password";
                return;
            }
            if (passwordToggleButton == null) return;
            passwordToggleButton.Text = string.Empty;
            if (!IconSet.ApplyTo(passwordToggleButton, revealed ? "eye-off" : "eye", 16, IconSet.Tint.Dark))
            {
                passwordToggleButton.Image = null;
                passwordToggleButton.Text = "👁️";
            }
        }
    }

    /// <summary>
    /// Builds the Login screen into a host panel. Shared by Form1 and the design gallery (which
    /// passes stand-in TextBoxes), so the gallery renders exactly what operators see.
    /// </summary>
    internal sealed class LoginLayout
    {
        private const int FormWidth = 380;

        public Panel FormHost { get; private set; }
        private SLStack _form;
        private SLBanner _status, _loading;
        private LoginBrandPanel _brand;

        public static LoginLayout Compose(Control host, TextBox email, TextBox password,
                                          SLButton signIn, SLIconButton toggle, Image logo)
        {
            LoginLayout l = new LoginLayout();

            l._brand = new LoginBrandPanel { Dock = DockStyle.Left, Logo = logo };
            l.FormHost = new Panel { Dock = DockStyle.Fill, BackColor = Theme.SurfaceApp };

            l._form = new SLStack(SLOrientation.Vertical, 20) { BackColor = Theme.SurfaceApp };

            SLStack heading = new SLStack(SLOrientation.Vertical, 6);
            heading.AddRange(
                new SLText("Sign in", SLTextStyle.Heading),
                new SLText("Use the email and password your manager set up for you.", SLTextStyle.Body) { Color = Theme.TextMuted });

            l._status = new SLBanner { Tone = SLTone.Error, IconName = "circle-alert" };

            SLTextBox emailBox = new SLTextBox(email) { FieldSize = SLSize.Lg };
            SLTextBox passwordBox = new SLTextBox(password) { FieldSize = SLSize.Lg, Suffix = toggle };

            signIn.ButtonSize = SLSize.Lg;
            signIn.AutoSize = false;
            signIn.Height = Theme.HeightLg;

            SLText help = new SLText("Trouble signing in? Ask your packhouse manager.", SLTextStyle.Muted)
            {
                Align = TextFormatFlags.HorizontalCenter,
                LineHeight = 1.6f
            };

            l._loading = new SLBanner { Tone = SLTone.Info, IconName = "loader" };

            l._form.AddRange(heading, l._status,
                new SLField("Email address", emailBox) { Required = true },
                new SLField("Password", passwordBox) { Required = true },
                signIn, help, l._loading);
            SLVisibility.Set(l._status, false);
            SLVisibility.Set(l._loading, false);

            l.FormHost.Controls.Add(l._form);
            host.Controls.Add(l.FormHost);
            host.Controls.Add(l._brand);

            host.Resize += (s, e) => l.Arrange(host);
            l.FormHost.Resize += (s, e) => l.Centre();
            l.Arrange(host);
            return l;
        }

        /// <summary>Grid "1.05fr / .95fr" with a 420px minimum for the form side.</summary>
        private void Arrange(Control host)
        {
            int w = host.ClientSize.Width;
            int brand = (int)Math.Round(w * 1.05 / 2.0);
            if (w - brand < 420) brand = Math.Max(0, w - 420);
            _brand.Width = brand;
            Centre();
        }

        /// <summary>The form column is centred in the space right of the brand panel.</summary>
        private void Centre()
        {
            if (FormHost == null || _form == null) return;
            int hostW = FormHost.ClientSize.Width, hostH = FormHost.ClientSize.Height;
            int width = Math.Min(FormWidth, Math.Max(200, hostW - 48));
            int height = _form.MeasureHeight(width);
            _form.SetBounds(Math.Max(24, (hostW - width) / 2), Math.Max(40, (hostH - height) / 2), width, height);
        }

        public void ShowStatus(bool show, SLTone tone, string message)
        {
            _status.Tone = tone;
            _status.IconName = tone == SLTone.Error ? "circle-alert" : "loader";
            _status.Message = message;
            SLVisibility.Set(_status, show);
            Centre();
        }

        public void ShowLoading(bool show, string message)
        {
            _loading.Message = message;
            SLVisibility.Set(_loading, show);
            Centre();
        }
    }

    /// <summary>
    /// The mockup's navy brand panel, painted in one pass so every measurement matches:
    /// 48px padding; two soft circles; the white logo (168px wide) at the top; a 30px/600
    /// headline over three icon bullets in the middle; a 12px footer at the bottom.
    /// </summary>
    [DesignerCategory("Code")]
    internal sealed class LoginBrandPanel : Control
    {
        private const int Pad = 48;
        private static readonly string Headline = "Every carton scanned, counted and labelled — without leaving this screen.";
        private static readonly string[][] Bullets =
        {
            new[] { "scan-line", "Watch scans land from every line, live" },
            new[] { "printer", "Print barcode and carton labels in a few clicks" },
            new[] { "cloud-upload", "Nothing is lost if the internet drops" }
        };

        public Image Logo { get; set; }

        public LoginBrandPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Navy900;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Theme.Navy900);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Decorative circles: rgba(77,74,234,.20) top right, rgba(255,255,255,.035) bottom.
            using (SolidBrush b = new SolidBrush(Color.FromArgb(51, Theme.Indigo500)))
                g.FillEllipse(b, Width + 140 - 440, -120, 440, 440);
            using (SolidBrush b = new SolidBrush(Color.FromArgb(9, Color.White)))
                g.FillEllipse(b, Width - 60 - 320, Height + 180 - 320, 320, 320);

            // Logo (168px wide, aspect kept).
            int logoH = 0;
            if (Logo != null)
            {
                logoH = (int)Math.Round(168.0 * Logo.Height / Math.Max(1, Logo.Width));
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(Logo, new Rectangle(Pad, Pad, 168, logoH));
            }

            // Footer: 12px, white at 42%.
            SLTypeSpec foot = new SLTypeSpec(Theme.FontXs, Theme.Blend(Color.White, Theme.Navy900, 0.42), 1.4f);
            int footTop = Height - Pad - foot.LineBox;
            SLPaint.Text(g, "ScanLink for packhouses", foot.Font, new Rectangle(Pad, footTop, Width - Pad * 2, foot.LineBox), foot.Color, TextFormatFlags.Left);

            // Middle block: headline (max 460 wide), 20px, three bullets 12px apart.
            int textW = Math.Min(460, Width - Pad * 2);
            SLTypeSpec head = new SLTypeSpec(Theme.Font4XlSemibold, Color.White, 1.25f);
            SLTypeSpec bullet = new SLTypeSpec(Theme.FontMd, Theme.Blend(Color.White, Theme.Navy900, 0.78), 1.5f);
            int headH = SLLayout.WrappedHeight(Headline, head, textW);
            int rowH = Math.Max(30, bullet.LineBox);
            int middleH = headH + 20 + Bullets.Length * rowH + (Bullets.Length - 1) * 12;

            // justify-content: space-between — equal free space above and below the middle block.
            int top = Pad + logoH, bottom = footTop;
            int y = top + Math.Max(0, (bottom - top - middleH) / 2);

            SLLayout.DrawWrapped(g, Headline, head, new Rectangle(Pad, y, textW, headH + 4), TextFormatFlags.Left);
            y += headH + 20;
            Color chip = Theme.Blend(Color.White, Theme.Navy900, 0.10);
            foreach (string[] b in Bullets)
            {
                Rectangle c = new Rectangle(Pad, y + (rowH - 30) / 2, 30, 30);
                SLPaint.Box(g, c, Theme.RadiusSm, chip, Color.Empty);
                SLIcon.Draw(g, b[0], new Rectangle(c.X + 7, c.Y + 7, 16, 16), Color.White);
                SLPaint.Text(g, b[1], bullet.Font, new Rectangle(Pad + 30 + 12, y, textW - 42, rowH), bullet.Color, TextFormatFlags.Left);
                y += rowH + 12;
            }
        }
    }
}
