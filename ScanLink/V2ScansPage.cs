using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ScanLink.Themed;

namespace ScanLink
{
    /// <summary>
    /// The Scans page, rebuilt against the v2 design: a search-and-period filter bar over a
    /// full-height table, with the diagnostic surfaces folded away.
    ///
    /// Applied programmatically after InitializeComponent for the same reason as the shell:
    /// every original control keeps its field reference and its handlers, and only its
    /// container changes. No filter is removed — the block, line, product and explicit date
    /// controls move behind "More filters" rather than being deleted, and the scanner
    /// console and its Sync / Cleanup buttons move behind "Show details".
    /// </summary>
    public partial class Form1
    {
        private CardPanel _outputCard;
        private CardPanel _filtersCard;
        private CardPanel _gridCard;
        private StatTile _todayTile;
        private StatTile _lastHourTile;
        private StatTile _seasonTile;

        private SearchField _searchField;
        private SegmentedControl _periodSegments;
        private Label _scanCountLabel;
        private Label _detailsToggle;
        private Panel _moreFiltersHost;
        private int _moreFiltersFullHeight;
        private int _outputCardFullHeight;
        private Label _moreFiltersToggle;
        private Timer _searchDebounce;

        private bool _detailsExpanded;
        private bool _moreFiltersExpanded;

        private const string PeriodToday = "today";
        private const string PeriodWeek = "week";
        private const string PeriodSeason = "season";
        private const string PeriodCustom = "custom";

        private const int FilterBarHeight = Theme.HeightMd;
        private const int FilterLinksHeight = 28;
        private const int StatsPanelFullHeight = 116;
        /// <summary>Height of the Daily Stats / Connected Scanners row that
        /// InitDashboardStatusUI adds under the tiles; its RowStyle is Absolute 250.</summary>
        private const int DashboardRowHeight = 250;

        /// <summary>
        /// Free-text search across serial, block, supplier and product. Read by
        /// ApplyFiltersToData alongside the other filter fields.
        /// </summary>
        private string filterSearchText = "";

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
                ApplyDetailsDisclosure();
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

        // ---------------------------------------------------------------- top bar actions

        private Control BuildScansLivePill()
        {
            Pill live = new Pill("Live");
            live.SetTone("info");
            return live;
        }

        private Control BuildScansSyncAction()
        {
            Button sync = new Button { Text = "Sync now", AutoSize = false };
            Image icon = IconSet.Get("cloud-upload", 16, IconSet.Tint.Dark);
            if (icon != null)
            {
                sync.Image = icon;
                sync.ImageAlign = ContentAlignment.MiddleLeft;
                sync.TextAlign = ContentAlignment.MiddleRight;
                sync.Padding = new Padding(Theme.S2, 0, Theme.S3, 0);
            }
            ThemeStyles.Secondary(sync);
            sync.Height = Theme.HeightMd;
            // Same action as the console's "Sync logs to API", promoted to the page header
            // because it is the one thing an operator reaches for when the count looks wrong.
            sync.Click += (s, e) => { if (button_manualUpload != null) button_manualUpload.PerformClick(); };
            return sync;
        }

        private Control BuildScansPrintAction()
        {
            Button print = new Button { Text = "Print labels", AutoSize = false };
            Image icon = IconSet.Get("printer", 16, IconSet.Tint.White);
            if (icon != null)
            {
                print.Image = icon;
                print.ImageAlign = ContentAlignment.MiddleLeft;
                print.TextAlign = ContentAlignment.MiddleRight;
                print.Padding = new Padding(Theme.S2, 0, Theme.S3, 0);
            }
            ThemeStyles.Primary(print);
            print.Click += (s, e) => NavigateTo(NavPrint);
            return print;
        }

        // ---------------------------------------------------------------- cards

        /// <summary>Swaps a control inside the scans TableLayoutPanel for a card, same cell.
        /// Cards in AutoSize rows dock to the top with an explicit height: a Fill child in an
        /// AutoSize row has no height to derive its own from.</summary>
        private CardPanel WrapInCard(Control original, string title, string subtitle, bool bodyPadding, bool fillRow)
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
                Dock = fillRow ? DockStyle.Fill : DockStyle.Top,
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

            int panelHeight = Math.Max(scannerOutputPanel.Height, 120);
            _outputCard = WrapInCard(scannerOutputPanel,
                "Scanner output",
                "Live log from the scanners, and the controls for syncing and clearing it.",
                true, fillRow: false);
            if (_outputCard == null) return;

            _outputCardFullHeight = CardPanel.TitledHeaderHeight + CardPanel.BodyPaddingV + panelHeight;
            _outputCard.Height = _outputCardFullHeight;
        }

        private void BuildStatTiles()
        {
            if (statsPanel == null) return;

            _todayTile = AdoptStatLabel(todayScansLabel, 0);
            _lastHourTile = AdoptStatLabel(lastHourScansLabel, 1);
            _seasonTile = AdoptStatLabel(seasonScansLabel, 2);

            // The row was sized for a single line of text.
            statsPanel.Dock = DockStyle.Top;
            statsPanel.Height = StatsPanelFullHeight;
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

        // ---------------------------------------------------------------- filter bar

        /// <summary>
        /// The design's one-line filter bar: search, period, crop, count. The original
        /// fourteen-column filter row is not thrown away — it moves into the same card
        /// behind "More filters", so block, line, product and explicit dates are one click
        /// away and every handler on them still runs.
        /// </summary>
        private void BuildFiltersCard()
        {
            if (filtersPanel == null || scannerContentPanel == null) return;

            int col = scannerContentPanel.GetColumn(filtersPanel);
            int row = scannerContentPanel.GetRow(filtersPanel);
            if (col < 0 || row < 0) return;

            _filtersCard = new CardPanel
            {
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, Theme.S4)
            };
            _filtersCard.BodyPadding = true;

            scannerContentPanel.Controls.Remove(filtersPanel);
            scannerContentPanel.Controls.Add(_filtersCard, col, row);

            // --- the advanced row, collapsed by default ---
            // Collapsed by HEIGHT, not by Visible. filtersPanel holds productIdComboBox,
            // which is data-bound; a bound combo inside a container that has never been
            // created reports zero items however many rows its DataSource has, and the
            // SelectedIndex = 0 in PopulateProductIdComboBox then threw out of LoadScansData.
            // Staying visible at zero height keeps the subtree in the created hierarchy.
            _moreFiltersFullHeight = Math.Max(filtersPanel.Height, 34) + Theme.S3;
            _moreFiltersHost = new Panel
            {
                Dock = DockStyle.Top,
                Height = 0,
                BackColor = Color.Transparent,
                Padding = new Padding(0, Theme.S3, 0, 0)
            };
            filtersPanel.Dock = DockStyle.Fill;
            filtersPanel.BackColor = Color.Transparent;
            _moreFiltersHost.Controls.Add(filtersPanel);

            // --- the v2 bar ---
            Panel bar = new Panel { Dock = DockStyle.Top, Height = FilterBarHeight, BackColor = Color.Transparent };

            _searchField = new SearchField("Search serial, block, supplier or product")
            {
                Left = 0,
                Top = 0,
                Width = 320
            };
            _searchField.QueryChanged += (s, e) => RestartSearchDebounce();

            _periodSegments = new SegmentedControl { Top = 0 };
            _periodSegments.AddSegment(PeriodToday, "Today");
            _periodSegments.AddSegment(PeriodWeek, "Last 7 days");
            _periodSegments.AddSegment(PeriodSeason, "This season");
            _periodSegments.AddSegment(PeriodCustom, "Custom");
            _periodSegments.Left = _searchField.Right + Theme.S4;
            _periodSegments.SegmentSelected += (s, key) => ApplyPeriodPreset(key);

            // The crop combo is moved rather than duplicated: a second combo would need its
            // own binding and the two would drift out of step.
            if (cropIdComboBox != null)
            {
                if (filtersPanel.Controls.Contains(cropIdComboBox)) filtersPanel.Controls.Remove(cropIdComboBox);
                if (cropIdLabel != null && filtersPanel.Controls.Contains(cropIdLabel)) cropIdLabel.Visible = false;

                cropIdComboBox.Dock = DockStyle.None;
                cropIdComboBox.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                cropIdComboBox.Width = 200;
                cropIdComboBox.Left = _periodSegments.Right + Theme.S4;
                cropIdComboBox.Top = (FilterBarHeight - cropIdComboBox.Height) / 2;
                bar.Controls.Add(cropIdComboBox);
            }

            _scanCountLabel = new Label
            {
                AutoSize = false,
                Width = 120,
                Height = FilterBarHeight,
                TextAlign = ContentAlignment.MiddleRight,
                Font = Theme.FontSm,
                ForeColor = Theme.TextMuted,
                BackColor = Color.Transparent,
                UseMnemonic = false
            };

            bar.Controls.Add(_searchField);
            bar.Controls.Add(_periodSegments);
            bar.Controls.Add(_scanCountLabel);

            // Positioned in code rather than anchored. Search, period and crop are all
            // fixed-width and the count is right-aligned, so at narrow widths the crop combo
            // has to give up space instead of sliding under the count.
            bar.Resize += (s, e) => LayoutFilterBar(bar);

            // --- second line: the two disclosures ---
            Panel links = new Panel { Dock = DockStyle.Top, Height = 28, BackColor = Color.Transparent };

            _moreFiltersToggle = MakeQuietLink("More filters", () =>
            {
                _moreFiltersExpanded = !_moreFiltersExpanded;
                ApplyMoreFiltersDisclosure();
            });
            _moreFiltersToggle.TextAlign = ContentAlignment.MiddleLeft;
            _moreFiltersToggle.Location = new Point(0, 0);
            _moreFiltersToggle.Height = 28;

            _detailsToggle = MakeQuietLink("Show details", () =>
            {
                _detailsExpanded = !_detailsExpanded;
                ApplyDetailsDisclosure();
            });
            _detailsToggle.TextAlign = ContentAlignment.MiddleLeft;
            _detailsToggle.Location = new Point(_moreFiltersToggle.Width + Theme.S5, 0);
            _detailsToggle.Height = 28;

            links.Controls.Add(_moreFiltersToggle);
            links.Controls.Add(_detailsToggle);

            // Added bottom-up so the bar ends up above the links, and both above the
            // advanced filter row.
            _filtersCard.Body.Controls.Add(_moreFiltersHost);
            _filtersCard.Body.Controls.Add(links);
            _filtersCard.Body.Controls.Add(bar);

            _filtersCard.Height = CardPanel.BodyPaddingV + FilterBarHeight + FilterLinksHeight;

            LayoutFilterBar(bar);
            _periodSegments.SetSelected(PeriodCustom);
        }

        private void LayoutFilterBar(Panel bar)
        {
            if (bar == null || _scanCountLabel == null) return;

            int right = bar.ClientSize.Width;
            _scanCountLabel.Left = right - _scanCountLabel.Width;

            if (cropIdComboBox != null && cropIdComboBox.Parent == bar)
            {
                // The crop combo is the only elastic control on the row, so it absorbs
                // whatever the fixed-width controls leave. Below its floor it is allowed to
                // run under the count rather than vanish.
                int available = _scanCountLabel.Left - Theme.S4 - cropIdComboBox.Left;
                cropIdComboBox.Width = Math.Max(120, available);
            }
        }

        private Label MakeQuietLink(string text, Action onClick)
        {
            Label link = new Label
            {
                Text = text,
                AutoSize = false,
                Width = Math.Max(90, TextRenderer.MeasureText(text, Theme.FontSmBold).Width + Theme.S2),
                TextAlign = ContentAlignment.MiddleRight,
                Font = Theme.FontSmBold,
                ForeColor = Theme.TextLink,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent,
                UseMnemonic = false
            };
            link.Click += (s, e) => onClick();
            return link;
        }

        private void ApplyMoreFiltersDisclosure()
        {
            if (_moreFiltersHost == null || _filtersCard == null) return;

            _moreFiltersHost.Height = _moreFiltersExpanded ? _moreFiltersFullHeight : 0;
            _moreFiltersToggle.Text = _moreFiltersExpanded ? "Fewer filters" : "More filters";
            _filtersCard.Height = CardPanel.BodyPaddingV + FilterBarHeight + FilterLinksHeight
                                + _moreFiltersHost.Height;
        }

        /// <summary>Whatever control currently sits in the scans layout's stats row.</summary>
        private Control ResolveStatsRowControl()
        {
            if (statsPanel == null || scannerContentPanel == null) return null;

            Control candidate = statsPanel;
            while (candidate != null && candidate.Parent != scannerContentPanel)
            {
                candidate = candidate.Parent;
            }
            return candidate ?? statsPanel;
        }

        /// <summary>The stats row's expanded height: the three tiles alone, or the tiles
        /// plus the Daily Stats and Connected Scanners panels once those have joined it.</summary>
        private int StatsRowFullHeight(Control statsRow)
        {
            return ReferenceEquals(statsRow, statsPanel)
                ? StatsPanelFullHeight
                : StatsPanelFullHeight + DashboardRowHeight;
        }

        private void ApplyDetailsDisclosure()
        {
            // The console and the three totals are diagnostic. The design puts neither on
            // Scans, but removing them would cost the operator the live log and the season
            // count, so they collapse instead.
            //
            // By height rather than by Visible, for the same reason as the advanced filter
            // row: a subtree that is never made visible is never created, and the scanner
            // console has to keep behaving exactly as it did when it was always on screen.
            // The margin goes with the height, or each collapsed row still reserves its gap.
            if (_outputCard != null)
            {
                _outputCard.Height = _detailsExpanded ? _outputCardFullHeight : 0;
                _outputCard.Margin = new Padding(0, 0, 0, _detailsExpanded ? Theme.S4 : 0);
            }

            // Resolved at call time, not captured at build time. InitDashboardStatusUI runs
            // AFTER the shell is built and lifts statsPanel out of this row into a split
            // panel alongside the Daily Stats Logger and the Connected Scanners grid, so the
            // control occupying the row is not the one this page put there. Collapsing
            // statsPanel itself would leave those two behind on a page whose design has
            // neither.
            Control statsRow = ResolveStatsRowControl();
            if (statsRow != null)
            {
                statsRow.Height = _detailsExpanded ? StatsRowFullHeight(statsRow) : 0;
                statsRow.Margin = new Padding(0, 0, 0, _detailsExpanded ? Theme.S4 : 0);
            }
            if (_detailsToggle != null) _detailsToggle.Text = _detailsExpanded ? "Hide details" : "Show details";
        }

        // ---------------------------------------------------------------- search & period

        private void RestartSearchDebounce()
        {
            if (_searchDebounce == null)
            {
                // Filtering walks every loaded scan, so a keystroke-per-filter is visibly
                // slow on a full season. One pass after the operator stops typing.
                _searchDebounce = new Timer { Interval = 300 };
                _searchDebounce.Tick += (s, e) =>
                {
                    _searchDebounce.Stop();
                    ApplyFilters();
                };
            }
            _searchDebounce.Stop();
            _searchDebounce.Start();
        }

        private void ApplyPeriodPreset(string key)
        {
            try
            {
                DateTime today = DateTime.Today;

                switch (key)
                {
                    case PeriodToday:
                        SetDateRange(today, today);
                        break;

                    case PeriodWeek:
                        SetDateRange(today.AddDays(-6), today);
                        break;

                    case PeriodSeason:
                        // Falls back to the whole range when the site has no season
                        // configured, which is the honest reading of "this season" there.
                        if (_activeSeason != null)
                        {
                            DateTime end = _activeSeason.EndDate.HasValue ? _activeSeason.EndDate.Value : today;
                            SetDateRange(_activeSeason.StartDate.Date, end.Date);
                        }
                        else
                        {
                            SetDateRange(DateTime.MinValue, today);
                        }
                        break;

                    case PeriodCustom:
                        _moreFiltersExpanded = true;
                        ApplyMoreFiltersDisclosure();
                        return;
                }

                ApplyFilters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[SCANS] period preset '" + key + "' failed: " + ex);
            }
        }

        private void SetDateRange(DateTime from, DateTime to)
        {
            if (dateFromPicker != null)
            {
                // MinValue is outside the picker's own range, so "everything" is expressed
                // as its minimum date rather than by unchecking, which this picker cannot do
                // — ShowCheckBox is false, so Checked is always true.
                DateTime lower = from < dateFromPicker.MinDate ? dateFromPicker.MinDate : from;
                dateFromPicker.Value = lower;
                dateFromPicker.Checked = true;
            }
            if (dateToPicker != null)
            {
                DateTime upper = to > dateToPicker.MaxDate ? dateToPicker.MaxDate : to;
                dateToPicker.Value = upper;
                dateToPicker.Checked = true;
            }
        }

        /// <summary>Reads the search box. Called by ApplyFilters alongside the other
        /// filter fields so there is one place that snapshots the UI.</summary>
        private void CaptureSearchFilter()
        {
            filterSearchText = (_searchField != null) ? _searchField.Query : "";
        }

        /// <summary>The "46 scans" readout — the size of the filtered set, not the page.</summary>
        private void UpdateScanCountLabel()
        {
            if (_scanCountLabel == null) return;
            int count = (filteredScannerData != null) ? filteredScannerData.Rows.Count : 0;
            _scanCountLabel.Text = count.ToString("N0") + (count == 1 ? " scan" : " scans");
        }

        // ---------------------------------------------------------------- grid

        private void BuildGridCard()
        {
            if (scannerDataGridView == null) return;

            _gridCard = WrapInCard(scannerDataGridView, null, null, false, fillRow: true);
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

            scannerDataGridView.DataSourceChanged += (s, e) => UpdateScanCountLabel();
            scannerDataGridView.DataBindingComplete += (s, e) => StyleScanColumns();
        }

        /// <summary>
        /// Per-column treatment the generic grid styling cannot know about: the serial is an
        /// identifier and reads as one in a monospaced face, and the numeric columns are
        /// narrow enough to be worth pinning so the wide text columns take the slack.
        /// </summary>
        private void StyleScanColumns()
        {
            try
            {
                SetColumnStyle("SerialNumber", Theme.FontMono, 150);
                SetColumnStyle("Time", null, 90);
                SetColumnStyle("Date", null, 110);
                SetColumnStyle("BlockNumber", null, 80);
                SetColumnStyle("LineNumber", null, 70);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[SCANS] column styling failed: " + ex);
            }
        }

        private void SetColumnStyle(string name, Font font, int fillWeight)
        {
            if (scannerDataGridView == null || !scannerDataGridView.Columns.Contains(name)) return;
            DataGridViewColumn column = scannerDataGridView.Columns[name];
            if (font != null) column.DefaultCellStyle.Font = font;
            column.FillWeight = fillWeight;
        }
    }
}
