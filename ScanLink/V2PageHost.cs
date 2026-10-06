using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ScanLink.Themed;

namespace ScanLink
{
    /// <summary>
    /// The shell's page host — the piece that makes every sidebar item a tab.
    ///
    /// Before this, the sidebar called PerformClick on the old hidden header buttons, so
    /// each destination opened a separate top-level window over the scans grid. Nothing
    /// navigated; windows just stacked up. Now each destination is a page docked in one
    /// content area, and selecting a sidebar item swaps which page is in front and what the
    /// top bar says and offers.
    ///
    /// The scans page is special: it is never hidden, only covered. A dozen call sites
    /// across Form1 gate scanner logging and count updates on scannerContentPanel.Visible as
    /// a proxy for "logged in", and Control.Visible reports the *effective* value — so
    /// hiding it to show another tab would silently stop the scanner console from recording
    /// anything while the operator was on the printer page.
    /// </summary>
    public partial class Form1
    {
        private sealed class V2Page
        {
            public string Key;
            public string Title;
            public string Subtitle;
            public Panel Root;
            public readonly List<Control> Actions = new List<Control>();
            /// <summary>Builds the page's content the first time it is shown. Nulled after
            /// it runs, so the dialogs are constructed once and keep their state.</summary>
            public Action Build;
            /// <summary>Runs on every activation, after Build.</summary>
            public Action Activated;
            /// <summary>True for the scans page — covered rather than hidden.</summary>
            public bool CoverOnly;
        }

        private Panel _pageHost;
        private readonly Dictionary<string, V2Page> _pages = new Dictionary<string, V2Page>();
        private string _activePageKey;

        private V2Page AddPage(string key, string title, string subtitle, Panel root, bool coverOnly)
        {
            V2Page page = new V2Page
            {
                Key = key,
                Title = title,
                Subtitle = subtitle,
                Root = root,
                CoverOnly = coverOnly
            };
            _pages[key] = page;

            root.Dock = DockStyle.Fill;
            if (!coverOnly) root.Visible = false;
            _pageHost.Controls.Add(root);
            return page;
        }

        /// <summary>An empty page whose content is built on first navigation.</summary>
        private Panel NewPagePanel()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.SurfaceApp,
                AutoScroll = false
            };
        }

        private void NavigateTo(string key)
        {
            V2Page page;
            if (!_pages.TryGetValue(key, out page)) return;

            try
            {
                if (page.Build != null)
                {
                    Action build = page.Build;
                    // Cleared before the call, not after: a build that throws must not be
                    // retried on every navigation, or a broken page makes the tab unusable.
                    page.Build = null;
                    build();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[NAV] could not build page '" + key + "': " + ex);
                ShowPageBuildFailure(page, ex);
            }

            _activePageKey = key;

            foreach (V2Page other in _pages.Values)
            {
                if (other == page || other.CoverOnly) continue;
                other.Root.Visible = false;
            }

            page.Root.Visible = true;
            page.Root.BringToFront();

            // A keyboard-mode scanner types into the focused control and ends with Enter: keep
            // focus off the search box and the header buttons on the Scans page.
            if (key == NavScans && scannerDataGridView != null && scannerDataGridView.CanFocus) scannerDataGridView.Focus();

            _sidebar.SetActive(key);
            _topBar.SetPage(page.Title, page.Subtitle);
            ApplyTopBarActions(page);

            try
            {
                if (page.Activated != null) page.Activated();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[NAV] activation of '" + key + "' failed: " + ex);
            }
        }

        /// <summary>
        /// A page whose content could not be built shows why, rather than a blank panel that
        /// looks like the app hung.
        /// </summary>
        private void ShowPageBuildFailure(V2Page page, Exception ex)
        {
            page.Root.Controls.Clear();
            CardPanel card = new CardPanel
            {
                Title = "This screen could not be opened",
                Subtitle = "The details are below, and in the debug log.",
                Dock = DockStyle.Top,
                Height = CardPanel.TitledHeaderHeight + CardPanel.BodyPaddingV + 80
            };
            card.Body.Controls.Add(new Label
            {
                Text = ex.Message,
                Dock = DockStyle.Fill,
                Font = Theme.FontSm,
                ForeColor = Theme.Err700,
                BackColor = Color.Transparent,
                UseMnemonic = false
            });
            Panel pad = new Panel { Dock = DockStyle.Fill, BackColor = Theme.SurfaceApp, Padding = new Padding(Theme.S6) };
            pad.Controls.Add(card);
            page.Root.Controls.Add(pad);
        }

        /// <summary>
        /// Swaps the top bar's action row for the active page's. The row flows
        /// right-to-left, so the first control added sits furthest right — pages list their
        /// primary action first.
        /// </summary>
        private void ApplyTopBarActions(V2Page page)
        {
            _topBar.Actions.SuspendLayout();
            try
            {
                _topBar.Actions.Controls.Clear();
                foreach (Control action in page.Actions)
                {
                    // The row mixes 38px buttons with a 26px pill; without a computed top
                    // margin the flow top-aligns them and the pill rides high.
                    int lift = Math.Max(0, (Theme.HeightMd - action.Height) / 2);
                    action.Margin = new Padding(Theme.S2, lift, 0, 0);
                    _topBar.Actions.Controls.Add(action);
                }
            }
            finally
            {
                _topBar.Actions.ResumeLayout(true);
            }
        }

        /// <summary>
        /// Registers every destination. Pages that wrap one of the old dialogs build lazily:
        /// ScannerManagementForm enumerates COM ports in its constructor and SetupDialog
        /// pulls product combinations, and neither should run before the operator asks for
        /// that tab.
        /// </summary>
        private void BuildPages()
        {
            // --- Scans: the existing grid, never hidden (see the class comment) ---
            V2Page scans = AddPage(NavScans, "Scans", "Everything scanned on this site. Newest first.",
                scannerContentPanel, coverOnly: true);
            scans.Actions.Add(BuildScansPrintAction());
            scans.Actions.Add(BuildScansSyncAction());
            scans.Actions.Add(BuildScansLivePill());
            // Re-applied on every activation, not just at build time: InitDashboardStatusUI
            // runs after the shell is built and rearranges the stats row, so the collapse
            // has to be asserted again once that has happened.
            scans.Activated = () => { ApplyDetailsDisclosure(); UpdateScanCountLabel(); };

            // --- Print labels: the three-step wizard ---
            Panel printRoot = NewPagePanel();
            V2Page print = AddPage(NavPrint, "Print labels", "Three short steps and the labels come out.",
                printRoot, coverOnly: false);
            print.Build = () => BuildV2PrintPage(printRoot);
            print.Activated = () => OnPrintPageActivated();

            // --- Scanners ---
            Panel scannersRoot = NewPagePanel();
            V2Page scanners = AddPage(NavScanners, "Scanners", "Detect, name and assign the scanners on this site.",
                scannersRoot, coverOnly: false);
            scanners.Build = () => EmbedScannerManagement(scannersRoot);
            // The old Scanner Setup window re-detected on every open; the page does the same on
            // every visit after the first (the first build already detected).
            bool scannersFirstVisit = true;
            scanners.Activated = () =>
            {
                if (scannersFirstVisit) { scannersFirstVisit = false; return; }
                if (_scannerManagementPage != null && !_scannerManagementPage.IsDisposed) _scannerManagementPage.RefreshScanners();
            };

            // --- Printer ---
            Panel printerRoot = NewPagePanel();
            V2Page printer = AddPage(NavPrinter, "Printer", "How ScanLink reaches the label printer.",
                printerRoot, coverOnly: false);
            printer.Build = () => EmbedPrinterConnection(printerRoot);

            // --- Crops & products ---
            Panel productsRoot = NewPagePanel();
            V2Page products = AddPage(NavProducts, "Crops & products", "The combinations available when printing labels.",
                productsRoot, coverOnly: false);
            products.Build = () => EmbedProductSetup(productsRoot);

            // --- Reports ---
            Panel reportsRoot = NewPagePanel();
            V2Page reports = AddPage(NavReports, "Reports", "Season and site reporting, on the web dashboard.",
                reportsRoot, coverOnly: false);
            reports.Build = () => BuildReportsPage(reportsRoot);
        }

        private ScannerManagementForm _scannerManagementPage;

        private void EmbedScannerManagement(Panel host)
        {
            ScannerManagementForm form = new ScannerManagementForm();
            _scannerManagementPage = form;

            // The same wiring scannerSetupButton_Click performed. It used ShowDialog, so the
            // reinitialise ran after the window closed; as a page it runs on Save, which is
            // when the operator actually expects it.
            form.ScannersSaved += (s, args) =>
            {
                if (scannerOutputTextBox != null)
                {
                    string ts = DateTime.Now.ToString("HH:mm:ss");
                    scannerOutputTextBox.AppendText("[" + ts + "] [C# INFO] Scanner configuration saved — reinitializing scanners...\r\n");
                    scannerOutputTextBox.ScrollToCaret();
                }
                if (_scannerComPortManager != null) _scannerComPortManager.CloseAllScanners();
                InitializeComPortScanners();
                UpdateCountLabels();
            };

            EmbeddedFormHost.Embed(form, host, () => NavigateTo(NavScans));
        }

        private void EmbedPrinterConnection(Panel host)
        {
            if (_printerConnectionDialog == null || _printerConnectionDialog.IsDisposed)
            {
                _printerConnectionDialog = new PrinterConnectionDialog(this);
            }
            EmbeddedFormHost.Embed(_printerConnectionDialog, host, () => NavigateTo(NavScans));
        }

        private void EmbedProductSetup(Panel host)
        {
            SetupDialog setup = new SetupDialog(_productCombinationsService);
            EmbeddedFormHost.Embed(setup, host, () => NavigateTo(NavScans));
        }

        /// <summary>
        /// Reports lives on the web dashboard, so this page explains that and hands over,
        /// rather than pretending to render reports the client does not have locally. The
        /// original handler still does the work — it carries the token check and the
        /// Chrome-then-default-browser fallback.
        /// </summary>
        private void BuildReportsPage(Panel host)
        {
            Panel pad = new Panel { Dock = DockStyle.Fill, BackColor = Theme.SurfaceApp, Padding = new Padding(Theme.S6) };

            CardPanel card = new CardPanel
            {
                Title = "Reports open in your browser",
                Subtitle = "On the Labour Link dashboard, not in ScanLink.",
                Dock = DockStyle.Top,
                Height = CardPanel.TitledHeaderHeight + CardPanel.BodyPaddingV + 110
            };

            Panel body = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            Label detail = new Label
            {
                Text = "Season totals and per-picker and per-block reporting live on the web dashboard.\r\n"
                     + "ScanLink signs you in automatically — you will not need your password again.",
                AutoSize = false,
                Size = new Size(620, 44),
                Location = new Point(0, 0),
                Font = Theme.FontSm,
                ForeColor = Theme.TextBody,
                BackColor = Color.Transparent,
                UseMnemonic = false
            };

            Button open = new Button { Text = "Open reports", Width = 180 };
            ThemeStyles.Primary(open);
            open.Location = new Point(0, 58);
            // Called directly: reportsButton is hidden, and PerformClick on a hidden button does nothing.
            open.Click += (s, e) => reportsButton_Click(reportsButton, EventArgs.Empty);

            body.Controls.Add(detail);
            body.Controls.Add(open);
            card.Body.Controls.Add(body);

            pad.Controls.Add(card);
            host.Controls.Add(pad);
        }
    }
}
