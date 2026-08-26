using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScanLink.Themed;

namespace ScanLink
{
    /// <summary>
    /// Builds the navy sidebar + top bar around the existing content, replacing the
    /// seven-tile header.
    ///
    /// Written as a partial of Form1 and applied AFTER InitializeComponent rather than by
    /// editing InitializeComponent itself. That file is 2,500 hand-edited lines and must
    /// never be opened in the Visual Studio designer; re-parenting a handful of controls
    /// here is far less risky than restructuring it in place.
    ///
    /// Navigation behaviour is unchanged: every sidebar item calls PerformClick on the
    /// original header button, so the handlers run exactly as before — including the
    /// enable/BackColor guards that stop a second print popup being opened.
    /// </summary>
    public partial class Form1
    {
        private Panel _shellRoot;
        private SidebarNav _sidebar;
        private TopBar _topBar;
        private Panel _contentColumn;
        private SiteTileButton _siteTile;
        private PrintLabelsPopupForm _printLabelsPopup;

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

            // The old header row is retired. It stays in the tree with all seven buttons
            // intact but hidden, because their Click handlers mutate the buttons themselves
            // and deleting the fields would silently break those guards. Its row in
            // scannerContentPanel is AutoSize, so a hidden child collapses it to zero height
            // and no row restructuring is needed.
            if (headerPanel != null) headerPanel.Visible = false;
            HideLegacyNavButtons();

            // Status strip belongs to the shell now, not to the scans grid.
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
            if (scannerContentPanel != null) _contentColumn.Controls.Add(scannerContentPanel);
            _contentColumn.Controls.Add(_topBar);
            if (statusPanel != null) _contentColumn.Controls.Add(statusPanel);

            _shellRoot.Controls.Add(_contentColumn);
            _shellRoot.Controls.Add(_sidebar);
            this.Controls.Add(_shellRoot);

            BuildSidebarContents();
            _topBar.SetPage("Scans", "Everything scanned on this site. Newest first.");
            _sidebar.SetActive(NavScans);
            _sidebar.ItemSelected += Sidebar_ItemSelected;

            // The shell is the inverse of the login screen. Driving it from loginPanel means
            // none of the existing show/hide call sites have to change: they always toggle
            // loginPanel, and loginPanel is a direct child of the form so its own visibility
            // is never masked by a hidden parent.
            if (loginPanel != null)
            {
                loginPanel.VisibleChanged += (s, e) =>
                {
                    _shellRoot.Visible = !loginPanel.Visible;
                    if (_shellRoot.Visible) RefreshSidebarSite();
                };
                _shellRoot.Visible = !loginPanel.Visible;
            }

            BuildV2ScansPage();
            BuildV2PrintSurfaces();

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
            _sidebar.AddItem(NavScans, "scan-line", "🔍", "Scans");

            _sidebar.AddGroupTitle("Daily work");
            _sidebar.AddItem(NavPrint, "printer", "🖨️", "Print labels");

            _sidebar.AddGroupTitle("Setup");
            _sidebar.AddItem(NavScanners, "usb", "🔧", "Scanners");
            _sidebar.AddItem(NavPrinter, "plug", "🔌", "Printer");
            _sidebar.AddItem(NavProducts, "database", "📋", "Crops && products");
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
        /// The knockout logo for dark surfaces. The standard ScanLinkLogo.png is dark ink on
        /// transparent, so on the navy rail it renders as an unreadable smudge.
        /// </summary>
        private Image TryLoadSidebarLogo()
        {
            Image white = ScanLink.Themed.IconSet.GetImage("ScanLinkLogoWhite.png");
            if (white != null) return white;

            try
            {
                string path = System.IO.Path.Combine(Application.StartupPath, "ScanLinkLogo.png");
                if (System.IO.File.Exists(path))
                {
                    using (var stream = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                    {
                        return Image.FromStream(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[SHELL] sidebar logo unavailable: " + ex.Message);
            }
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

        /// <summary>
        /// Opens the merged Print labels surface. Replaces barCodesButton_Click and
        /// boxLabelsButton_Click as the entry point; both handlers and both original popup
        /// forms are left intact so reverting to two entries is a one-line change here.
        /// </summary>
        private async void OpenPrintLabels()
        {
            try
            {
                if (_printLabelsPopup == null || _printLabelsPopup.IsDisposed)
                {
                    _printLabelsPopup = new PrintLabelsPopupForm(configPanel, actionPanel, advancedPanel, this);

                    // The same combo wiring barCodesButton_Click performed.
                    if (comboBox_CropID != null)
                    {
                        comboBox_CropID.SelectedIndexChanged -= comboBox_CropID_SelectedIndexChanged;
                        comboBox_CropID.SelectedIndexChanged += comboBox_CropID_SelectedIndexChanged;
                    }
                    if (comboBox_ProductID != null)
                    {
                        comboBox_ProductID.SelectedIndexChanged -= comboBox_ProductID_SelectedIndexChanged;
                        comboBox_ProductID.SelectedIndexChanged += comboBox_ProductID_SelectedIndexChanged;
                    }
                }

                _printLabelsPopup.ShowTab(PrintLabelsPopupForm.TabBarcodes);
                await EnsureCropOptionsLoadedAsync(updateStatusLabel: false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[PRINT] could not open Print labels: " + ex);
                statusLabel.Text = "Error: Could not open Print labels - " + ex.Message;
                statusLabel.ForeColor = Theme.Err500;
            }
        }

        private void Sidebar_ItemSelected(object sender, string key)
        {
            switch (key)
            {
                case NavScans:
                    _sidebar.SetActive(NavScans);
                    _topBar.SetPage("Scans", "Everything scanned on this site. Newest first.");
                    break;

                // The rest open the surfaces the header buttons already opened. PerformClick
                // keeps every guard inside those handlers intact. Active state is not moved,
                // because these open a dialog over the scans page rather than navigating.
                case NavPrint:
                    OpenPrintLabels();
                    break;
                case NavScanners:
                    if (scannerSetupButton != null) scannerSetupButton.PerformClick();
                    break;
                case NavPrinter:
                    if (printerConnectionButton != null) printerConnectionButton.PerformClick();
                    break;
                case NavProducts:
                    if (setupButton != null) setupButton.PerformClick();
                    break;
                case NavReports:
                    if (reportsButton != null) reportsButton.PerformClick();
                    break;
            }
        }
    }
}
