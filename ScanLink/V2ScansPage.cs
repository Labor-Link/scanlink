using System;
using System.Drawing;
using System.Windows.Forms;
using ScanLink.Themed;

namespace ScanLink
{
    /// <summary>
    /// Rehouses the scans page rows into cards. Applied programmatically after
    /// InitializeComponent for the same reason as the shell: every original control keeps
    /// its field reference and its handlers, and only its container changes.
    ///
    /// No filter is added or removed, and the scanner output console keeps its checkbox,
    /// its log box, Sync logs to API and Cleanup local scans.
    /// </summary>
    public partial class Form1
    {
        private CardPanel _outputCard;
        private CardPanel _filtersCard;
        private CardPanel _gridCard;
        private StatTile _todayTile;
        private StatTile _lastHourTile;
        private StatTile _seasonTile;
        private Button _outputToggle;
        private bool _outputExpanded = true;

        private void BuildV2ScansPage()
        {
            if (scannerContentPanel == null) return;

            scannerContentPanel.SuspendLayout();
            try
            {
                BuildOutputCard();
                BuildStatTiles();
                BuildFiltersCard();
                BuildGridCard();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[SCANS] v2 page build failed: " + ex);
            }
            finally
            {
                scannerContentPanel.ResumeLayout(true);
            }
        }

        /// <summary>Swaps a control inside the scans TableLayoutPanel for a card, same cell.</summary>
        private CardPanel WrapInCard(Control original, string title, string subtitle, bool bodyPadding)
        {
            if (original == null) return null;

            TableLayoutPanel host = scannerContentPanel;
            int col = host.GetColumn(original);
            int row = host.GetRow(original);
            if (col < 0 || row < 0) return null;

            CardPanel card = new CardPanel
            {
                Title = title,
                Subtitle = subtitle,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, Theme.S4)
            };
            card.BodyPadding = bodyPadding;

            host.Controls.Remove(original);
            host.Controls.Add(card, col, row);

            original.Dock = DockStyle.Fill;
            original.BackColor = Color.Transparent;
            card.Body.Controls.Add(original);

            return card;
        }

        private void BuildOutputCard()
        {
            if (scannerOutputPanel == null) return;

            _outputCard = WrapInCard(scannerOutputPanel,
                "Scanner output",
                "Live log from the scanners, and the controls for syncing and clearing it.",
                true);
            if (_outputCard == null) return;

            // Starts open because it holds the sync controls. The toggle exists because this
            // console is diagnostic — useful when something is wrong, noise the rest of the time.
            _outputToggle = new Button { Text = "Hide", AutoSize = false, Width = 84 };
            ThemeStyles.Secondary(_outputToggle);
            _outputToggle.Click += (s, e) =>
            {
                _outputExpanded = !_outputExpanded;
                _outputCard.Body.Visible = _outputExpanded;
                _outputToggle.Text = _outputExpanded ? "Hide" : "Show";
                LayoutRootPanels();
            };
            _outputCard.Actions.Controls.Add(_outputToggle);
        }

        private void BuildStatTiles()
        {
            if (statsPanel == null) return;

            _todayTile = AdoptStatLabel(todayScansLabel, 0);
            _lastHourTile = AdoptStatLabel(lastHourScansLabel, 1);
            _seasonTile = AdoptStatLabel(seasonScansLabel, 2);

            // The row was sized for a single line of text.
            statsPanel.Height = 116;
            statsPanel.BackColor = Color.Transparent;
            statsPanel.Margin = new Padding(0, 0, 0, Theme.S4);
        }

        /// <summary>
        /// Wraps one count label in a StatTile without touching UpdateCountLabels.
        ///
        /// The label carries caption and value in one string ("Today's Scans: 42"), which is
        /// the wrong shape for a tile. Rather than rewrite the code that produces it, the
        /// label moves inside the tile, is hidden, and its TextChanged splits the string
        /// across the tile's caption and value.
        /// </summary>
        private StatTile AdoptStatLabel(Label source, int column)
        {
            if (source == null || statsPanel == null) return null;

            StatTile tile = new StatTile
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(column == 0 ? 0 : Theme.S2, 0, column == 2 ? 0 : Theme.S2, 0)
            };

            statsPanel.Controls.Remove(source);
            statsPanel.Controls.Add(tile, column, 0);

            source.Visible = false;
            source.AutoSize = false;
            source.Size = new Size(1, 1);
            tile.Controls.Add(source);

            source.TextChanged += (s, e) => SyncStatTile(tile, source.Text);
            SyncStatTile(tile, source.Text);

            return tile;
        }

        private static void SyncStatTile(StatTile tile, string text)
        {
            if (tile == null) return;
            if (string.IsNullOrEmpty(text)) { tile.Value = null; return; }

            int split = text.LastIndexOf(':');
            if (split > 0 && split < text.Length - 1)
            {
                tile.Caption = text.Substring(0, split).Trim();
                tile.Value = text.Substring(split + 1).Trim();
            }
            else
            {
                tile.Value = text.Trim();
            }
        }

        private void BuildFiltersCard()
        {
            _filtersCard = WrapInCard(filtersPanel, "Filters", null, true);
            if (filtersPanel != null) filtersPanel.BackColor = Color.Transparent;
        }

        private void BuildGridCard()
        {
            if (scannerDataGridView == null) return;

            _gridCard = WrapInCard(scannerDataGridView, null, null, false);
            if (_gridCard == null) return;

            // Pagination becomes the card's footer. Its own row in the scans layout is
            // AutoSize, so vacating it collapses the row to zero height.
            if (paginationPanel != null)
            {
                scannerContentPanel.Controls.Remove(paginationPanel);
                paginationPanel.Dock = DockStyle.Fill;
                paginationPanel.BackColor = Color.Transparent;
                _gridCard.Footer.Controls.Add(paginationPanel);
                _gridCard.ShowFooter(true);
            }

            _gridCard.Margin = new Padding(0);
        }
    }
}
