using System;
using System.Drawing;
using System.Windows.Forms;
using ScanLink.Themed;

namespace ScanLink
{
    /// <summary>
    /// A single "Print labels" destination replacing the separate Bar Codes and Box Labels
    /// popups. The two originals did nothing but host panels — ConfigPopupForm held
    /// configPanel + actionPanel, BoxLabelsPopupForm held advancedPanel. This hosts all
    /// three and switches with the segmented control the app already uses in SetupDialog, so
    /// no new interaction pattern is introduced.
    ///
    /// The reparenting contract is unchanged: panels are taken by reference and moved whole.
    /// ConfigPopupForm and BoxLabelsPopupForm are deliberately left in place and still
    /// compile, so reverting to two sidebar entries is a one-line change in
    /// Sidebar_ItemSelected.
    /// </summary>
    internal class PrintLabelsPopupForm : Form
    {
        public const string TabBarcodes = "barcodes";
        public const string TabBoxLabels = "boxlabels";

        private readonly Panel _configPanel;
        private readonly Panel _actionPanel;
        private readonly Panel _advancedPanel;
        private readonly Form1 _mainForm;

        private Panel _barcodesHost;
        private Panel _boxLabelsHost;
        private Button _barcodesTab;
        private Button _boxLabelsTab;

        public PrintLabelsPopupForm(Panel configPanel, Panel actionPanel, Panel advancedPanel, Form1 mainForm)
        {
            _configPanel = configPanel;
            _actionPanel = actionPanel;
            _advancedPanel = advancedPanel;
            _mainForm = mainForm;
            InitializePopup();
        }

        private void InitializePopup()
        {
            Text = "Print labels";
            StartPosition = FormStartPosition.CenterScreen;
            // Sized for the larger of the two surfaces so switching tabs never resizes the
            // window under the operator.
            Size = new Size(1400, 620);
            MinimumSize = new Size(940, 560);
            MaximizeBox = true;
            MinimizeBox = true;
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.Sizable;
            BackColor = Theme.SurfaceApp;
            Owner = Application.OpenForms["Form1"];

            Panel content = new Panel { Dock = DockStyle.Fill, BackColor = Theme.SurfaceApp };

            // --- Bar codes surface: config above, actions pinned below ---
            _barcodesHost = new Panel { Dock = DockStyle.Fill, BackColor = Theme.SurfaceApp };

            if (_actionPanel != null)
            {
                _actionPanel.Dock = DockStyle.Bottom;
                _actionPanel.AutoSize = false;
                _actionPanel.Height = 110;
                _actionPanel.Padding = new Padding(Theme.S5, Theme.S3, Theme.S5, Theme.S3);
                _actionPanel.Visible = true;
            }
            if (_configPanel != null)
            {
                _configPanel.Dock = DockStyle.Fill;
                _configPanel.AutoSize = false;
                _configPanel.Padding = new Padding(Theme.S5, Theme.S3, Theme.S5, Theme.S3);
                _configPanel.Visible = true;
            }

            // Fill added first so the docked bottom strip claims its height first.
            if (_configPanel != null) _barcodesHost.Controls.Add(_configPanel);
            if (_actionPanel != null) _barcodesHost.Controls.Add(_actionPanel);

            // --- Box labels surface ---
            _boxLabelsHost = new Panel { Dock = DockStyle.Fill, BackColor = Theme.SurfaceApp, Visible = false };
            if (_advancedPanel != null)
            {
                _advancedPanel.Dock = DockStyle.Fill;
                _advancedPanel.Padding = new Padding(Theme.S8, Theme.S5, Theme.S8, Theme.S5);
                _advancedPanel.Visible = true;
                _boxLabelsHost.Controls.Add(_advancedPanel);
            }

            content.Controls.Add(_barcodesHost);
            content.Controls.Add(_boxLabelsHost);

            Controls.Add(content);
            Controls.Add(BuildTabStrip());

            SetTab(TabBarcodes);

            Shown += (s, e) => { if (_mainForm != null) _mainForm.UpdatePictureBoxVisibility(); };
        }

        private Panel BuildTabStrip()
        {
            Panel strip = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Theme.SurfaceSunken,
                Padding = new Padding(Theme.S3, Theme.S2, Theme.S3, 0)
            };

            _barcodesTab = MakeTab("Bar codes", TabBarcodes, 0);
            _boxLabelsTab = MakeTab("Box labels", TabBoxLabels, 1);

            strip.Controls.Add(_barcodesTab);
            strip.Controls.Add(_boxLabelsTab);
            return strip;
        }

        private Button MakeTab(string text, string key, int index)
        {
            Button tab = new Button
            {
                Text = text,
                Size = new Size(140, 36),
                Location = new Point(Theme.S3 + index * 148, Theme.S2),
                FlatStyle = FlatStyle.Flat,
                Tag = key,
                UseMnemonic = false
            };
            tab.FlatAppearance.BorderSize = 0;
            tab.Click += (s, e) => SetTab(key);
            return tab;
        }

        private void SetTab(string key)
        {
            bool barcodes = (key == TabBarcodes);
            if (_barcodesHost != null) _barcodesHost.Visible = barcodes;
            if (_boxLabelsHost != null) _boxLabelsHost.Visible = !barcodes;

            StyleTab(_barcodesTab, barcodes);
            StyleTab(_boxLabelsTab, !barcodes);
        }

        /// <summary>Segmented control: the selected tab lifts to the card surface.</summary>
        private static void StyleTab(Button tab, bool selected)
        {
            if (tab == null) return;
            tab.BackColor = selected ? Theme.SurfaceCard : Theme.SurfaceSunken;
            tab.ForeColor = selected ? Theme.TextHeading : Theme.TextMuted;
            tab.Font = selected ? Theme.FontSmBold : Theme.FontSm;
            tab.FlatAppearance.MouseOverBackColor = selected ? Theme.SurfaceCard : Theme.N200;
            tab.FlatAppearance.MouseDownBackColor = Theme.N200;
            tab.UseVisualStyleBackColor = false;
            ThemeStyles.RoundedCorners(tab, Theme.RadiusSm);
        }

        public void ShowTab(string key)
        {
            SetTab(key);
            if (!Visible) Show();
            BringToFront();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Same contract as the popups this replaces: hide rather than dispose, so the
            // reparented panels stay alive and keep their state between openings.
            if (Owner != null && !Owner.Visible) return;

            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
            base.OnFormClosing(e);
        }
    }
}
