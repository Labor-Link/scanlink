using System;
using System.Drawing;
using System.Windows.Forms;
using ScanLink.Themed;

namespace ScanLink
{
    /// <summary>
    /// Builds the navy sidebar + top bar around a page host, replacing the seven-tile
    /// header.
    ///
    /// Written as a partial of Form1 and applied AFTER InitializeComponent rather than by
    /// editing InitializeComponent itself. That file is 2,500 hand-edited lines and must
    /// never be opened in the Visual Studio designer; re-parenting a handful of controls
    /// here is far less risky than restructuring it in place.
    ///
    /// Navigation is real navigation. Every sidebar item selects a page in the content area
    /// — see V2PageHost — instead of opening a window over the scans grid, which is what the
    /// first pass did and what made the app feel like a launcher rather than a product.
    /// </summary>
    public partial class Form1
    {
        private Panel _shellRoot;
        private SidebarNav _sidebar;
        private TopBar _topBar;
        private Panel _contentColumn;
        private SiteTileButton _siteTile;

        private const string NavScans = "scans";
        private const string NavPrint = "print";
        private const string NavScanners = "scanners";
        private const string NavPrinter = "printer";
        private const string NavProducts = "products";
        private const string NavReports = "reports";

        private void BuildV2Shell()
        {
            if (_shellRoot != null) return;

            _shellRoot = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.SurfaceApp,
                Visible = false
            };

            _sidebar = new SidebarNav();
            _topBar = new TopBar();

            _contentColumn = new Panel { Dock = DockStyle.Fill, BackColor = Theme.SurfaceApp };
            _pageHost = new Panel { Dock = DockStyle.Fill, BackColor = Theme.SurfaceApp };

            // The old header row is retired. It stays in the tree with all seven buttons
            // intact but hidden, because their Click handlers mutate the buttons themselves
            // and deleting the fields would silently break those guards. Its row in
            // scannerContentPanel is AutoSize, so a hidden child collapses it to zero height
            // and no row restructuring is needed.
            if (headerPanel != null) headerPanel.Visible = false;
            HideLegacyNavButtons();

            // Status strip belongs to the shell now, not to the scans grid — it reports on
            // whatever page is showing.
            if (statusPanel != null)
            {
                if (scannerContentPanel != null) scannerContentPanel.Controls.Remove(statusPanel);
                statusPanel.Dock = DockStyle.Bottom;
                statusPanel.Height = Theme.StatusBarHeight;
            }

            if (scannerContentPanel != null)
            {
                this.Controls.Remove(scannerContentPanel);
                scannerContentPanel.Dock = DockStyle.Fill;
                scannerContentPanel.BackColor = Theme.SurfaceApp;
                scannerContentPanel.Padding = new Padding(Theme.S6, Theme.S5, Theme.S6, Theme.S5);
            }

            // Fill first, then Top, then Bottom: WinForms resolves docking from the last
            // added control backwards, so the Fill added first claims what the edges leave.
            _contentColumn.Controls.Add(_pageHost);
            _contentColumn.Controls.Add(_topBar);
            if (statusPanel != null) _contentColumn.Controls.Add(statusPanel);

            _shellRoot.Controls.Add(_contentColumn);
            _shellRoot.Controls.Add(_sidebar);
            this.Controls.Add(_shellRoot);

            BuildSidebarContents();
            _sidebar.ItemSelected += Sidebar_ItemSelected;

            // Pages are registered before the scans page is restyled: registering re-parents
            // scannerContentPanel into the host, and the restyle then works inside it.
            BuildPages();
            BuildV2ScansPage();
            BuildV2PrintSurfaces();

            NavigateTo(NavScans);

            // The shell is the inverse of the login screen. Driving it from loginPanel means
            // none of the existing show/hide call sites have to change: they always toggle
            // loginPanel, and loginPanel is a direct child of the form so its own visibility
            // is never masked by a hidden parent.
            if (loginPanel != null)
            {
                loginPanel.VisibleChanged += (s, e) =>
                {
                    _shellRoot.Visible = !loginPanel.Visible;
                    if (!_shellRoot.Visible) return;
                    RefreshSidebarSite();
                    // A new session always starts on Scans, never on whichever tab the
                    // previous operator left open.
                    NavigateTo(NavScans);
                };
                _shellRoot.Visible = !loginPanel.Visible;
            }

            // Any button nobody re-themed (Daily Stats Logger's Save/Debug, and anything
            // buried in the print panels) picks up the secondary variant here.
            ThemeStyles.StyleRemainingButtons(this);

            _shellRoot.SendToBack();
        }

        private void HideLegacyNavButtons()
        {
            Button[] legacy = new Button[]
            {
                setupButton, scannerSetupButton, printerConnectionButton,
                barCodesButton, boxLabelsButton, reportsButton, logoutButton
            };
            foreach (Button b in legacy) { if (b != null) b.Visible = false; }
        }

        private void BuildSidebarContents()
        {
            // Items are added top-down; each AddItem docks to the top and is brought to the
            // front, so the visual order matches the call order.
            _sidebar.AddGroupTitle("Daily work");
            _sidebar.AddItem(NavScans, "scan-line", "🔍", "Scans");
            _sidebar.AddItem(NavPrint, "printer", "🖨️", "Print labels");

            _sidebar.AddGroupTitle("Setup");
            _sidebar.AddItem(NavScanners, "usb", "🔧", "Scanners");
            _sidebar.AddItem(NavPrinter, "plug", "🔌", "Printer");
            // Single ampersand: the nav labels set UseMnemonic = false, so an escaped "&&"
            // is rendered literally rather than collapsed.
            _sidebar.AddItem(NavProducts, "database", "📋", "Crops & products");
            // Reports has no counterpart in the prototype — the design system says it had no
            // design source, not that it was dropped. Omitting it would lose a feature.
            _sidebar.AddItem(NavReports, "bar-chart-3", "🌐", "Reports");

            _sidebar.AddBrand(TryLoadSidebarLogo(), "ScanLink");


            _siteTile = new SiteTileButton();
            _siteTile.Activated += (s, e) => SwitchSiteFromSidebar();

            Label signOut = new Label
            {
                Text = "Sign out",
                Font = Theme.FontSm,
                ForeColor = Theme.Blend(Color.White, Theme.SurfaceShell, 0.55),
                Dock = DockStyle.Top,
                Height = 34,
                Padding = new Padding(Theme.S3, 0, 0, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent,
                UseMnemonic = false
            };
            signOut.Click += (s, e) => { if (logoutButton != null) logoutButton.PerformClick(); };

            _sidebar.Footer.Controls.Add(signOut);
            _sidebar.Footer.Controls.Add(_siteTile);

            RefreshSidebarSite();
        }

        /// <summary>
        /// The brand mark on the navy rail — currently the wordmark as text.
        ///
        /// Neither shipped asset works here. ScanLinkLogo.png is opaque with no alpha, and
        /// Assets/ScanLinkLogoWhite.png is a white wordmark knocked out onto an opaque white
        /// ground, so on the navy rail it rendered as a plain white block with the tick
        /// floating in it. A knockout cannot be derived from either one automatically.
        ///
        /// Returning null makes SidebarNav fall back to the text wordmark, which is legible
        /// and correct. Drop a real transparent-background white PNG in as
        /// Assets/ScanLinkLogoWhite.png and load it here to restore the image mark.
        /// </summary>
        private Image TryLoadSidebarLogo()
        {
            return null;
        }

        private void RefreshSidebarSite()
        {
            if (_siteTile == null) return;
            try
            {
                string name = (_apiAuthService != null) ? _apiAuthService.GetSelectedSiteName() : null;
                string id = (_apiAuthService != null) ? _apiAuthService.GetSelectedSiteId() : null;
                _siteTile.SetSite(name, id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[SHELL] could not read selected site: " + ex.Message);
            }
        }

        /// <summary>
        /// Site switching from the sidebar footer. The design system moves this out of a
        /// modal between login and the app and into a control that is always one click away.
        /// Same MultiUserDashboard, same SetSelectedSite call — only the entry point moved,
        /// so a cancelled switch leaves the current site in place rather than dropping the
        /// operator back to login the way the startup path does.
        /// </summary>
        private void SwitchSiteFromSidebar()
        {
            try
            {
                if (_apiAuthService == null || !_apiAuthService.IsTokenValid())
                {
                    ResetToLogin("Session expired. Please login again.");
                    return;
                }

                var tokenPayload = _apiAuthService.GetCurrentTokenPayload();
                if (tokenPayload == null)
                {
                    ResetToLogin("Session expired. Please login again.");
                    return;
                }

                using (var dashboard = new MultiUserDashboard(_apiAuthService, tokenPayload))
                {
                    var result = dashboard.ShowDialog(this);
                    if (result == DialogResult.OK && !string.IsNullOrEmpty(dashboard.GetSelectedSiteId()))
                    {
                        _apiAuthService.SetSelectedSite(dashboard.GetSelectedSiteId(), dashboard.GetSelectedSiteName());
                        RefreshSidebarSite();
                        statusLabel.Text = "Status: Switched to " + dashboard.GetSelectedSiteName();
                        statusLabel.ForeColor = Theme.Ok500;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[SHELL] site switch failed: " + ex);
                statusLabel.Text = "Error: Could not switch site - " + ex.Message;
                statusLabel.ForeColor = Theme.Err500;
            }
        }

        private void Sidebar_ItemSelected(object sender, string key)
        {
            NavigateTo(key);
        }
    }
}
