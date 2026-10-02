using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScanLink.DesignSystem;
using System.Diagnostics;
using System.Management;

namespace ScanLink
{
    /// <summary>
    /// Scanners on this site (the mockup's Devices screen): one row per scanner with its line,
    /// block, supplier and COM settings. Built on SLDialog; also used as a page via
    /// EmbeddedFormHost. See design/screens.md, "Devices".
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal partial class ScannerManagementForm : SLDialog
    {
        private SLTable scannerDataGridView;
        private SLButton saveButton;
        private SLButton refreshButton;
        private SLButton configHelpButton;
        private SLTextBox debugOutputTextBox;
        private SLBanner connectionBanner;
        private SLBanner resultBanner;
        private SLField debugField;
        private SLToggle debugToggle;
        private List<ScannerInfo> detectedScanners;
		public event EventHandler ScannersSaved;

        public class ScannerInfo
        {
            public string SerialNumber { get; set; }
            public string PNPDeviceID { get; set; }
            public string ComPort { get; set; }
            public string ConnectionType { get; set; } = "USB-COM";
            public string LineID { get; set; }
            public string BlockID { get; set; }
            public string Supplier { get; set; }
            public string BaudRate { get; set; } = "9600";
            public string Parity { get; set; } = "None";
            public string DataBits { get; set; } = "8";
            public string StopBits { get; set; } = "One";
            public string Status { get; set; }
            public bool IsCurrentlyConnected { get; set; }

            public string AssignmentKey
            {
                get
                {
                    string conn = string.IsNullOrWhiteSpace(ConnectionType) ? "USB-COM" : ConnectionType;
                    string id = string.IsNullOrWhiteSpace(PNPDeviceID) ? "UNKNOWN" : PNPDeviceID;
                    return $"{conn}::{id}";
                }
            }

            public string ModeDisplay
            {
                get
                {
                    switch ((ConnectionType ?? "USB-COM").ToUpperInvariant())
                    {
                        case "USB-HID-KEYBOARD":
                            return "HID-KBD";
                        case "USB-HID-RAW":
                            return "HID-RAW";
                        default:
                            return "USB-COM";
                    }
                }
            }

            public string GetComPortDisplay()
            {
                if ((ConnectionType ?? string.Empty).StartsWith("USB-HID", StringComparison.OrdinalIgnoreCase))
                {
                    return ModeDisplay;
                }
                return string.IsNullOrWhiteSpace(ComPort) ? "Auto" : ComPort;
            }
        }

        private void LoadHidScanners()
        {
            try
            {
                LogDebug("Scanning for HID-mode scanners...");
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE PNPDeviceID LIKE 'HID\\\\VID_%'");
                var hidDevices = searcher.Get();

                var connectedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (ManagementObject device in hidDevices)
                {
                    string deviceId = device["PNPDeviceID"]?.ToString();
                    if (string.IsNullOrWhiteSpace(deviceId))
                    {
                        continue;
                    }

                    string name = device["Name"]?.ToString();
                    string description = device["Description"]?.ToString();
                    string manufacturer = device["Manufacturer"]?.ToString();
                    string vid = ExtractVid(deviceId);

                    if (!IsLikelyScannerDevice(name, description, manufacturer, vid))
                    {
                        continue;
                    }

                    string connectionType = DetermineHidConnectionType(name, description);

                    var scanner = new ScannerInfo
                    {
                        PNPDeviceID = deviceId,
                        ConnectionType = connectionType,
                        SerialNumber = !string.IsNullOrWhiteSpace(name) ? name : deviceId,
                        LineID = "",
                        BlockID = "",
                        Supplier = "",
                        IsCurrentlyConnected = true,
                        Status = "Connected"
                    };

                    var merged = AddOrUpdateScanner(scanner, updateConnectionState: true);
                    if (merged != null)
                    {
                        merged.ConnectionType = connectionType;
                        merged.IsCurrentlyConnected = true;
                        merged.Status = "Connected";
                        if (string.IsNullOrWhiteSpace(merged.SerialNumber))
                        {
                            merged.SerialNumber = scanner.SerialNumber;
                        }
                        connectedKeys.Add(merged.AssignmentKey);
                        LogDebug($"HID scanner detected: {merged.PNPDeviceID} [{merged.ModeDisplay}]");
                    }
                }

                foreach (var scanner in detectedScanners.Where(s => (s.ConnectionType ?? string.Empty).StartsWith("USB-HID", StringComparison.OrdinalIgnoreCase)))
                {
                    if (!connectedKeys.Contains(scanner.AssignmentKey))
                    {
                        scanner.IsCurrentlyConnected = false;
                    }
                }

                LogDebug($"HID scanner detection complete. Found {connectedKeys.Count} connected HID scanner(s).");
            }
            catch (Exception ex)
            {
                LogDebug($"Failed to enumerate HID scanners: {ex.Message}");
            }
        }

        private void RefreshComPortStatusFromWmi()
        {
            try
            {
                LogDebug("Refreshing COM-port scanners via WMI...");
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'");
                var devices = searcher.Get();

                var connectedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (ManagementObject device in devices)
                {
                    string deviceId = device["PNPDeviceID"]?.ToString();
                    if (string.IsNullOrWhiteSpace(deviceId))
                    {
                        continue;
                    }

                    string name = device["Name"]?.ToString() ?? string.Empty;
                    string description = device["Description"]?.ToString() ?? string.Empty;
                    string manufacturer = device["Manufacturer"]?.ToString() ?? string.Empty;
                    string vid = ExtractVid(deviceId);

                    if (!IsLikelyScannerDevice(name, description, manufacturer, vid))
                    {
                        continue;
                    }

                    string comPort = string.Empty;
                    var nameMatch = Regex.Match(name, @"\((COM\d+)\)", RegexOptions.IgnoreCase);
                    if (nameMatch.Success)
                    {
                        comPort = nameMatch.Groups[1].Value;
                    }
                    if (string.IsNullOrWhiteSpace(comPort))
                    {
                        var match = Regex.Match(description, @"(COM\d+)", RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            comPort = match.Groups[1].Value;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(comPort))
                    {
                        continue;
                    }

                    comPort = comPort.ToUpperInvariant();

                    var scanner = new ScannerInfo
                    {
                        PNPDeviceID = deviceId,
                        ConnectionType = "USB-COM",
                        ComPort = comPort,
                        SerialNumber = !string.IsNullOrWhiteSpace(name) ? name : deviceId,
                        IsCurrentlyConnected = true,
                        Status = "Connected"
                    };

                    var merged = AddOrUpdateScanner(scanner, updateConnectionState: true);
                    if (merged != null)
                    {
                        merged.ConnectionType = "USB-COM";
                        merged.ComPort = comPort;
                        merged.IsCurrentlyConnected = true;
                        merged.Status = "Connected";
                        if (string.IsNullOrWhiteSpace(merged.SerialNumber))
                        {
                            merged.SerialNumber = scanner.SerialNumber;
                        }
                        connectedKeys.Add(merged.AssignmentKey);
                        LogDebug($"COM scanner detected via WMI: {merged.PNPDeviceID} [{merged.ComPort}]");
                    }
                }

                foreach (var scanner in detectedScanners.Where(s => string.Equals(s.ConnectionType, "USB-COM", StringComparison.OrdinalIgnoreCase)))
                {
                    if (!connectedKeys.Contains(scanner.AssignmentKey))
                    {
                        scanner.IsCurrentlyConnected = false;
                    }
                }

                LogDebug($"COM-port scanner refresh complete. Found {connectedKeys.Count} connected COM scanner(s).");
            }
            catch (Exception ex)
            {
                LogDebug($"Failed to enumerate COM scanners via WMI: {ex.Message}");
            }
        }

        private static readonly Dictionary<string, string> KnownScannerVids = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "05F9", "Datalogic" },
            { "0C2E", "Honeywell" },
            { "1504", "Zebra" },
            { "05E0", "Symbol Technologies" },
            { "1A86", "CH34x" },
            { "0403", "FTDI" },
            { "05A9", "Opticon" },
            { "10C4", "Silabs" }
        };

        private static readonly string[] ScannerKeywordMatches = new[]
        {
            "scanner",
            "barcode",
            "imag",
            "qr",
            "datalogic",
            "honeywell",
            "zebra",
            "symbol",
            "opticon",
            "datamax"
        };

        private ScannerInfo AddOrUpdateScanner(ScannerInfo incoming, bool updateConnectionState)
        {
            if (incoming == null || string.IsNullOrWhiteSpace(incoming.PNPDeviceID))
            {
                return null;
            }

            incoming.ConnectionType = string.IsNullOrWhiteSpace(incoming.ConnectionType) ? "USB-COM" : incoming.ConnectionType;

            var existing = detectedScanners.FirstOrDefault(s =>
                string.Equals(s.AssignmentKey, incoming.AssignmentKey, StringComparison.OrdinalIgnoreCase));

            if (existing == null)
            {
                incoming.SerialNumber = !string.IsNullOrWhiteSpace(incoming.SerialNumber)
                    ? incoming.SerialNumber
                    : $"Scanner {detectedScanners.Count + 1}";

                incoming.BaudRate = incoming.BaudRate ?? "9600";
                incoming.Parity = incoming.Parity ?? "None";
                incoming.DataBits = incoming.DataBits ?? "8";
                incoming.StopBits = incoming.StopBits ?? "One";
                incoming.Status = incoming.Status ?? (incoming.IsCurrentlyConnected ? "Connected" : "Not Connected");

                detectedScanners.Add(incoming);
                return incoming;
            }

            if (!string.IsNullOrWhiteSpace(incoming.SerialNumber) &&
                (string.IsNullOrWhiteSpace(existing.SerialNumber) ||
                 existing.SerialNumber.StartsWith("Scanner ", StringComparison.OrdinalIgnoreCase)))
            {
                existing.SerialNumber = incoming.SerialNumber;
            }

            existing.ConnectionType = incoming.ConnectionType;

            if (!string.IsNullOrWhiteSpace(incoming.ComPort))
            {
                existing.ComPort = incoming.ComPort;
            }

            if (!string.IsNullOrWhiteSpace(incoming.LineID))
            {
                existing.LineID = incoming.LineID;
            }

            if (!string.IsNullOrWhiteSpace(incoming.BlockID))
            {
                existing.BlockID = incoming.BlockID;
            }

            if (!string.IsNullOrWhiteSpace(incoming.Supplier))
            {
                existing.Supplier = incoming.Supplier;
            }

            if (!string.IsNullOrWhiteSpace(incoming.BaudRate))
            {
                existing.BaudRate = incoming.BaudRate;
            }

            if (!string.IsNullOrWhiteSpace(incoming.Parity))
            {
                existing.Parity = incoming.Parity;
            }

            if (!string.IsNullOrWhiteSpace(incoming.DataBits))
            {
                existing.DataBits = incoming.DataBits;
            }

            if (!string.IsNullOrWhiteSpace(incoming.StopBits))
            {
                existing.StopBits = incoming.StopBits;
            }

            if (!string.IsNullOrWhiteSpace(incoming.Status))
            {
                existing.Status = incoming.Status;
            }

            if (updateConnectionState)
            {
                existing.IsCurrentlyConnected = incoming.IsCurrentlyConnected;
            }

            return existing;
        }

        private static string ExtractVid(string pnpDeviceId)
        {
            if (string.IsNullOrWhiteSpace(pnpDeviceId))
            {
                return string.Empty;
            }

            var match = Regex.Match(pnpDeviceId, "VID_([0-9A-F]{4})", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.ToUpperInvariant() : string.Empty;
        }

        private static string ExtractPid(string pnpDeviceId)
        {
            if (string.IsNullOrWhiteSpace(pnpDeviceId))
            {
                return string.Empty;
            }

            var match = Regex.Match(pnpDeviceId, "PID_([0-9A-F]{4})", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.ToUpperInvariant() : string.Empty;
        }

        private static bool IsLikelyScannerDevice(string name, string description, string manufacturer, string vid)
        {
            string combined = $"{name} {description} {manufacturer}".ToLowerInvariant();
            if (ScannerKeywordMatches.Any(keyword => combined.Contains(keyword)))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(vid) && KnownScannerVids.ContainsKey(vid);
        }

        private static string DetermineHidConnectionType(string name, string description)
        {
            string combined = $"{name} {description}";
            if (!string.IsNullOrWhiteSpace(combined) &&
                combined.IndexOf("keyboard", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "USB-HID-KEYBOARD";
            }

            return "USB-HID-RAW";
        }

        private Dictionary<string, ScannerInfo> ParseAssignmentsFile(string[] lines)
        {
            var assignments = new Dictionary<string, ScannerInfo>(StringComparer.OrdinalIgnoreCase);
            if (lines == null || lines.Length == 0)
            {
                return assignments;
            }

            ScannerInfo current = null;

            void CommitCurrent()
            {
                if (current == null || string.IsNullOrWhiteSpace(current.PNPDeviceID))
                {
                    current = null;
                    return;
                }

                current.ConnectionType = string.IsNullOrWhiteSpace(current.ConnectionType) ? "USB-COM" : current.ConnectionType;
                current.ComPort = current.ComPort == "Auto-detect" ? string.Empty : current.ComPort;
                current.BaudRate = current.BaudRate ?? "9600";
                current.Parity = current.Parity ?? "None";
                current.DataBits = current.DataBits ?? "8";
                current.StopBits = current.StopBits ?? "One";
                current.LineID = current.LineID ?? string.Empty;
                current.BlockID = current.BlockID ?? string.Empty;
                current.Supplier = current.Supplier ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(current.SerialNumber) &&
                    current.SerialNumber.StartsWith("Scanner", StringComparison.OrdinalIgnoreCase))
                {
                    current.SerialNumber = current.PNPDeviceID;
                }
                current.SerialNumber = current.SerialNumber ?? current.PNPDeviceID;
                current.Status = current.Status ?? "Not Connected";
                current.IsCurrentlyConnected = false;

                assignments[current.AssignmentKey] = current;
                current = null;
            }

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line))
                {
                    CommitCurrent();
                    continue;
                }

                if (line.StartsWith("Scanner #", StringComparison.OrdinalIgnoreCase))
                {
                    CommitCurrent();
                    current = new ScannerInfo
                    {
                        SerialNumber = line
                    };
                    continue;
                }

                if (current == null)
                {
                    current = new ScannerInfo();
                }

                if (line.StartsWith("PNPDeviceID:", StringComparison.OrdinalIgnoreCase))
                {
                    current.PNPDeviceID = line.Substring("PNPDeviceID:".Length).Trim();
                }
                else if (line.StartsWith("Connection Type:", StringComparison.OrdinalIgnoreCase))
                {
                    current.ConnectionType = line.Substring("Connection Type:".Length).Trim();
                }
                else if (line.StartsWith("COM Port:", StringComparison.OrdinalIgnoreCase))
                {
                    current.ComPort = line.Substring("COM Port:".Length).Trim();
                }
                else if (line.StartsWith("Line ID:", StringComparison.OrdinalIgnoreCase))
                {
                    current.LineID = line.Substring("Line ID:".Length).Trim();
                }
                else if (line.StartsWith("Block ID:", StringComparison.OrdinalIgnoreCase))
                {
                    current.BlockID = line.Substring("Block ID:".Length).Trim();
                }
                else if (line.StartsWith("Supplier:", StringComparison.OrdinalIgnoreCase))
                {
                    current.Supplier = line.Substring("Supplier:".Length).Trim();
                }
                else if (line.StartsWith("Baud Rate:", StringComparison.OrdinalIgnoreCase))
                {
                    current.BaudRate = line.Substring("Baud Rate:".Length).Trim();
                }
                else if (line.StartsWith("Parity:", StringComparison.OrdinalIgnoreCase))
                {
                    current.Parity = line.Substring("Parity:".Length).Trim();
                }
                else if (line.StartsWith("Data Bits:", StringComparison.OrdinalIgnoreCase))
                {
                    current.DataBits = line.Substring("Data Bits:".Length).Trim();
                }
                else if (line.StartsWith("Stop Bits:", StringComparison.OrdinalIgnoreCase))
                {
                    current.StopBits = line.Substring("Stop Bits:".Length).Trim();
                }
            }

            CommitCurrent();

            return assignments;
        }

        public ScannerManagementForm()
        {
            InitializeComponent();
            
            // Add debug info about file paths
            System.Diagnostics.Debug.WriteLine($"Application.StartupPath: {Application.StartupPath}");
            System.Diagnostics.Debug.WriteLine($"Directory.GetCurrentDirectory(): {Directory.GetCurrentDirectory()}");
            
            LoadDetectedScanners();
            PopulateDataGridView();
        }

        /// <summary>Design-gallery constructor: shows the given scanners without running
        /// hardware detection (which needs PowerShell, WMI and real devices).</summary>
        private ScannerManagementForm(List<ScannerInfo> preview)
        {
            InitializeComponent();
            detectedScanners = preview;
            PopulateDataGridView();
        }

        /// <summary>Sample page for SLGallery: one connected, one offline, one HID scanner.</summary>
        internal static ScannerManagementForm CreatePreview()
        {
            return new ScannerManagementForm(new List<ScannerInfo>
            {
                new ScannerInfo { SerialNumber = "SL-HH-02", PNPDeviceID = @"USB\VID_05F9&PID_4204\S/N:G21L00213", ComPort = "COM3", LineID = "3", BlockID = "14", Supplier = "Rooidraai", Status = "Connected", IsCurrentlyConnected = true },
                new ScannerInfo { SerialNumber = "SL-HH-11", PNPDeviceID = @"USB\VID_05F9&PID_4204\S/N:G21L00488", ComPort = "COM7", LineID = "2", BlockID = "07", Supplier = "Kleinbos", Status = "Not Connected", IsCurrentlyConnected = false },
                new ScannerInfo { SerialNumber = "SL-HH-05", PNPDeviceID = @"HID\VID_05F9&PID_2214\7&1C2A", ConnectionType = "USB-HID-KEYBOARD", LineID = "1", BlockID = "21", Supplier = "Vaalkop", Status = "Connected", IsCurrentlyConnected = true }
            });
        }

        // Layout:
        //   [Error banner: a scanner isn't answering  (Look again)]
        //   [Result banner: saved / removed / failed]
        //   SLCard "Scanners"  actions: COM mode help · Look for scanners
        //     SLTable (editable Line / Block / Supplier / COM settings, trash action)
        //     footer: Show detection log
        //   [Detection log]
        //   Footer: Close · Save assignments
        private void InitializeComponent()
        {
            Name = "ScannerManagementForm";
            Title = "Scanners on this site";
            Description = "Each scanner is tied to a line and a block so scans land in the right place.";
            DialogWidth = 1100;
            BodyHeight = 560;

            connectionBanner = new SLBanner { Tone = SLTone.Error, IconName = "unplug" };
            connectionBanner.Action = new SLButton { Text = "Look again", Variant = SLVariant.Secondary, ButtonSize = SLSize.Sm };
            connectionBanner.Action.Click += refreshButton_Click;
            resultBanner = new SLBanner();
            Body.Controls.Add(connectionBanner);
            Body.Controls.Add(resultBanner);
            SLVisibility.Set(connectionBanner, false);
            SLVisibility.Set(resultBanner, false);

            scannerDataGridView = new SLTable
            {
                Name = "scannerDataGridView",
                ReadOnly = false,            // Line, Block, Supplier and COM settings are edited in place
                EditMode = DataGridViewEditMode.EditOnEnter,
                // Only one row selectable at a time. With MultiSelect clicking a second row left
                // two rows highlighted, which made it unclear which scanner's edit was saved.
                MultiSelect = false,
                Height = 240
            };
            scannerDataGridView.EmptyState = new SLEmptyState
            {
                Compact = true,
                IconName = "usb",
                Title = "No scanners found yet",
                Description = "Plug a scanner into this computer, then choose Look for scanners.",
                Action = new SLButton { Text = "Look for scanners", Variant = SLVariant.Secondary, ButtonSize = SLSize.Sm }
            };
            scannerDataGridView.EmptyState.Action.Click += refreshButton_Click;

            refreshButton = new SLButton { Name = "refreshButton", Text = "Look for scanners", Variant = SLVariant.Secondary, ButtonSize = SLSize.Sm, IconName = "refresh-cw" };
            refreshButton.Click += refreshButton_Click;
            configHelpButton = new SLButton { Name = "configHelpButton", Text = "COM mode help", Variant = SLVariant.Ghost, ButtonSize = SLSize.Sm, IconName = "circle-help" };
            configHelpButton.Click += configHelpButton_Click;

            debugToggle = new SLToggle { Text = "Show detection log" };
            debugToggle.CheckedChanged += (s, e) => SLVisibility.Set(debugField, debugToggle.Checked);

            var card = new SLCard { Title = "Scanners", BodyPadding = Padding.Empty };
            card.Actions.AddRange(configHelpButton, refreshButton);
            card.Body.Controls.Add(scannerDataGridView);
            card.Body.SetGrow(scannerDataGridView);
            card.Footer.Controls.Add(debugToggle);
            Body.Controls.Add(card);
            Body.SetGrow(card);

            debugOutputTextBox = new SLTextBox { Name = "debugOutputTextBox", Multiline = true, ReadOnly = true, Mono = true, Height = 110 };
            debugField = new SLField("Detection log", debugOutputTextBox);
            Body.Controls.Add(debugField);
            SLVisibility.Set(debugField, false);

            var close = new SLButton { Text = "Close", Variant = SLVariant.Secondary, DialogResult = DialogResult.Cancel };
            close.Click += (s, e) => Close();   // also shown modeless, where DialogResult does not close
            saveButton = new SLButton { Name = "saveButton", Text = "Save assignments", IconName = "save" };
            saveButton.Click += saveButton_Click;
            AddAction(close);
            AddAction(saveButton);

            this.Load += ScannerManagementForm_Load;
            // The grid selects its first row when its handle is created, after Populate ran.
            this.Shown += (s, e) => scannerDataGridView.ClearSelection();
            scannerDataGridView.VisibleChanged += (s, e) => { if (scannerDataGridView.Visible) scannerDataGridView.ClearSelection(); };
        }

        private void ScannerManagementForm_Load(object sender, EventArgs e)
        {
            // Ensure grid is populated on form load
            if (detectedScanners != null && detectedScanners.Count > 0 && scannerDataGridView.Rows.Count == 0)
            {
                PopulateDataGridView();
            }
        }

        /// <summary>Owner for confirmation dialogs, whether this is a window or an embedded page.</summary>
        private IWin32Window DialogOwner { get { return TopLevel ? (IWin32Window)this : TopLevelControl; } }

        private void ShowResult(SLTone tone, string title, string message)
        {
            resultBanner.Tone = tone;
            resultBanner.IconName = tone == SLTone.Success ? "circle-check" : tone == SLTone.Error ? "circle-alert" : "info";
            resultBanner.Title = title;
            resultBanner.Message = message;
            SLVisibility.Set(resultBanner, true);
        }

        /// <summary>The mockup's "Line 2 scanner isn't answering" banner, for every scanner that is not connected.</summary>
        private void UpdateConnectionBanner()
        {
            int offline = detectedScanners == null ? 0 : detectedScanners.Count(sc => !sc.IsCurrentlyConnected);
            if (offline == 0) { SLVisibility.Set(connectionBanner, false); return; }
            connectionBanner.Title = offline == 1 ? "1 scanner isn't answering" : offline + " scanners aren't answering";
            connectionBanner.Message = "Check that it's plugged in and switched on, then look again. Scans already saved are not affected.";
            SLVisibility.Set(connectionBanner, true);
        }

        private void UpdateColumnFillWeights()
        {
            if (scannerDataGridView.Columns.Count == 0) return;

            // Short columns fit their header and values; the long Device ID absorbs whatever
            // width is left (and truncates with "…"). Proportional weights clipped "SL-HH-02"
            // and "COM3" on a 1024px screen.
            foreach (DataGridViewColumn col in scannerDataGridView.Columns)
            {
                col.AutoSizeMode = col.Name == "PNPDeviceID"
                    ? DataGridViewAutoSizeColumnMode.Fill
                    : DataGridViewAutoSizeColumnMode.AllCells;
                if (col is DataGridViewComboBoxColumn) col.MinimumWidth = 84;   // value + chevron
            }
            if (scannerDataGridView.Columns.Contains("PNPDeviceID")) scannerDataGridView.Columns["PNPDeviceID"].MinimumWidth = 90;
            if (scannerDataGridView.Columns.Contains("Status")) scannerDataGridView.Columns["Status"].MinimumWidth = 128;
            if (scannerDataGridView.Columns.Contains("Delete")) scannerDataGridView.Columns["Delete"].MinimumWidth = 46;
        }

        private void SetColumnFillWeight(string columnName, float weight)
        {
            if (scannerDataGridView.Columns.Contains(columnName))
            {
                scannerDataGridView.Columns[columnName].FillWeight = weight;
            }
        }

        private void LoadDetectedScanners()
        {
            detectedScanners = new List<ScannerInfo>();
            
            // Clear and log to debug panel
            debugOutputTextBox.Clear();
            LogDebug("=== Scanner Detection Started ===");
            
            // First check COM ports directly
            try
            {
                string[] availablePorts = System.IO.Ports.SerialPort.GetPortNames();
                LogDebug($"Available COM ports on system: {availablePorts.Length}");
                foreach (string port in availablePorts)
                {
                    LogDebug($"  - {port}");
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Failed to enumerate COM ports: {ex.Message}");
            }
            
            try
            {
                // First, load historical scanners from assignments file
                LoadHistoricalScanners();
                LogDebug($"Loaded {detectedScanners.Count} historical scanner(s) from assignments file");
                
                // Then run the scanner detection PowerShell script to get currently connected scanners (bin root)
                string scriptPath = Path.Combine(Application.StartupPath, "scanner_detection.ps1");
                LogDebug($"Looking for script at: {scriptPath}");
                
                if (!File.Exists(scriptPath))
                {
                    LogDebug($"ERROR: Scanner detection script not found at: {scriptPath}");
                    ShowResult(SLTone.Error, "Scanner detection isn't installed", "ScanLink couldn't find its detection script at " + scriptPath + ". Reinstall ScanLink, then look again.");
                    return;
                }

                LogDebug("Executing PowerShell script...");
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\" -Simple",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(error))
                    {
                        LogDebug($"PowerShell Error: {error}");
                    }

                    LogDebug($"PowerShell exit code: {process.ExitCode}");

                    if (process.ExitCode != 0)
                    {
                        LogDebug($"ERROR: Script execution failed");
                        ShowResult(SLTone.Error, "Scanner detection failed", string.IsNullOrWhiteSpace(error) ? "Turn on Show detection log for details." : error.Trim());
                        return;
                    }

                    LogDebug($"PowerShell output received ({output.Length} chars)");
                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        // Log first few lines of output
                        string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        LogDebug($"Output has {lines.Length} line(s)");
                        for (int i = 0; i < Math.Min(5, lines.Length); i++)
                        {
                            LogDebug($"  Line {i + 1}: {lines[i].Substring(0, Math.Min(80, lines[i].Length))}...");
                        }
                    }
                    else
                    {
                        LogDebug("WARNING: PowerShell returned empty output");
                    }

                    // Parse the output to extract currently connected scanner information
                    ParseCurrentScanners(output);
                }

                // Supplement COM-port detection with WMI enumeration
                RefreshComPortStatusFromWmi();
                
                // Detect HID-mode scanners (keyboard/raw)
                LoadHidScanners();
                
                // Update status for all scanners (connected vs not connected)
                UpdateScannerStatus();
                
                int connectedCount = detectedScanners.Count(s => s.IsCurrentlyConnected);
                LogDebug($"=== Detection Complete: {connectedCount} connected, {detectedScanners.Count} total ===");
            }
            catch (Exception ex)
            {
                LogDebug($"EXCEPTION: {ex.Message}");
                LogDebug($"Stack trace: {ex.StackTrace}");
                ShowResult(SLTone.Error, "Couldn't read the scanner list", ex.Message);
            }
        }

        private void LogDebug(string message)
        {
            if (debugOutputTextBox.InvokeRequired)
            {
                debugOutputTextBox.Invoke(new Action(() => LogDebug(message)));
                return;
            }
            
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            debugOutputTextBox.Inner.AppendText($"[{timestamp}] {message}\r\n");
        }

        private void LoadHistoricalScanners()
        {
            try
            {
                // Try multiple possible paths for the scanner assignments file
                string programDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ScanLink");
                string[] possiblePaths = new string[]
                {
                    Path.Combine(programDataDir, "scanner_assignments.txt"),
                    Path.Combine(Application.StartupPath, "scanner_assignments.txt"),
                    Path.Combine(Application.StartupPath, "..", "..", "ScanLinkScanner", "scanner_assignments.txt"),
                    Path.Combine(Directory.GetCurrentDirectory(), "scanner_assignments.txt")
                };

                string assignmentsPath = null;
                foreach (string path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        assignmentsPath = path;
                        break;
                    }
                }
                
                if (assignmentsPath == null)
                {
                    System.Diagnostics.Debug.WriteLine("No scanner assignments file found in any of the expected locations");
                    return; // No existing assignments file, nothing to load
                }

                System.Diagnostics.Debug.WriteLine($"Loading historical scanners from: {assignmentsPath}");
                string[] existingLines = File.ReadAllLines(assignmentsPath);
                var assignments = ParseAssignmentsFile(existingLines);

                foreach (var assignment in assignments.Values)
                {
                    assignment.IsCurrentlyConnected = false;
                    assignment.Status = "Not Connected";
                    AddOrUpdateScanner(assignment, updateConnectionState: false);
                    System.Diagnostics.Debug.WriteLine($"Loaded historical scanner assignment: {assignment.ConnectionType} :: {assignment.PNPDeviceID}");
                }
                
                System.Diagnostics.Debug.WriteLine($"Total historical scanners loaded: {assignments.Count}");
                
                // If no historical scanners found, add a test entry to verify functionality
                if (assignments.Count == 0 && detectedScanners.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No historical scanners found, adding test entry");
                    detectedScanners.Add(new ScannerInfo
                    {
                        SerialNumber = "Test Scanner",
                        PNPDeviceID = "USB\\VID_05F9&PID_2216\\S/N_G24HD1690",
                        LineID = "5",
                        BlockID = "9",
                        Supplier = "",
                        Status = "Not Connected",
                        IsCurrentlyConnected = false
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading historical scanners: {ex.Message}");
                ShowResult(SLTone.Warning, "Saved scanner assignments couldn't be read", ex.Message);
                
                // Add a test entry even if there's an error
                detectedScanners.Add(new ScannerInfo
                {
                    SerialNumber = "Test Scanner (Error Fallback)",
                    PNPDeviceID = "USB\\VID_05F9&PID_2216\\S/N_G24HD1690",
                    LineID = "5",
                    BlockID = "9",
                    Supplier = "",
                    Status = "Not Connected",
                    IsCurrentlyConnected = false
                });
            }
        }

        private void ParseCurrentScanners(string output)
        {
            LogDebug("--- Parsing PowerShell Output ---");
            System.Diagnostics.Debug.WriteLine($"Parsing current scanners output: {output}");
            string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            List<(string pnpId, string comPort, string deviceName)> currentScanners = new List<(string, string, string)>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (!line.StartsWith("Scanner #", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                LogDebug($"Found scanner entry: {line}");
                string pnpDeviceID = null;
                string comPort = null;
                string deviceName = null;

                for (int j = i + 1; j < Math.Min(i + 12, lines.Length); j++)
                {
                    string nextLine = lines[j].Trim();
                    if (string.IsNullOrWhiteSpace(nextLine) || nextLine.StartsWith("Scanner #", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    if (nextLine.StartsWith("COM Port:", StringComparison.OrdinalIgnoreCase))
                    {
                        comPort = nextLine.Substring("COM Port:".Length).Trim();
                        LogDebug($"  Found COM Port: {comPort}");
                    }
                    else if (nextLine.StartsWith("PNPDeviceID:", StringComparison.OrdinalIgnoreCase))
                    {
                        pnpDeviceID = nextLine.Substring("PNPDeviceID:".Length).Trim();
                        LogDebug($"  Found PNPDeviceID: {pnpDeviceID}");
                    }
                    else if (nextLine.StartsWith("DeviceName:", StringComparison.OrdinalIgnoreCase))
                    {
                        deviceName = nextLine.Substring("DeviceName:".Length).Trim();
                    }
                }

                if (!string.IsNullOrEmpty(pnpDeviceID))
                {
                    currentScanners.Add((pnpDeviceID, comPort, deviceName));
                    LogDebug($"✓ Added scanner from detection: {pnpDeviceID} on {comPort}");
                    System.Diagnostics.Debug.WriteLine($"Found currently connected COM scanner: {pnpDeviceID} on {comPort}");
                }
            }

            LogDebug($"Parsed {currentScanners.Count} currently connected COM scanner(s)");
            System.Diagnostics.Debug.WriteLine($"Total currently connected COM scanners found: {currentScanners.Count}");

            var connectedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (pnpId, comPort, deviceName) in currentScanners)
            {
                var scanner = new ScannerInfo
                {
                    PNPDeviceID = pnpId,
                    ComPort = comPort,
                    ConnectionType = "USB-COM",
                    SerialNumber = string.IsNullOrWhiteSpace(deviceName) ? pnpId : deviceName,
                    IsCurrentlyConnected = true,
                    Status = "Connected"
                };

                var merged = AddOrUpdateScanner(scanner, updateConnectionState: true);
                if (merged != null)
                {
                    merged.ConnectionType = "USB-COM";
                    merged.ComPort = comPort;
                    merged.IsCurrentlyConnected = true;
                    merged.Status = "Connected";
                    if (string.IsNullOrWhiteSpace(merged.SerialNumber))
                    {
                        merged.SerialNumber = scanner.SerialNumber;
                    }
                    connectedKeys.Add(merged.AssignmentKey);
                }
            }

            foreach (var scanner in detectedScanners)
            {
                if (scanner.ConnectionType.Equals("USB-COM", StringComparison.OrdinalIgnoreCase))
                {
                    if (!connectedKeys.Contains(scanner.AssignmentKey))
                    {
                        scanner.IsCurrentlyConnected = false;
                    }
                }
            }

            if (!detectedScanners.Any())
            {
                detectedScanners.Add(new ScannerInfo
                {
                    SerialNumber = "No scanners detected",
                    PNPDeviceID = "N/A",
                    ConnectionType = "USB-COM",
                    LineID = "",
                    BlockID = "",
                    Supplier = "",
                    Status = "Not Connected",
                    IsCurrentlyConnected = false
                });
            }
        }

        private void UpdateScannerStatus()
        {
            foreach (var scanner in detectedScanners)
            {
                scanner.Status = scanner.IsCurrentlyConnected ? "Connected" : "Not Connected";
            }
        }

        private void PopulateDataGridView()
        {
            // Clear existing columns
            scannerDataGridView.Columns.Clear();
            
            // Remove old event handler if it exists
            scannerDataGridView.CellContentClick -= ScannerDataGridView_CellContentClick;
            
            // Add columns
            DataGridViewTextBoxColumn serialColumn = new DataGridViewTextBoxColumn();
            serialColumn.HeaderText = "Serial";
            serialColumn.Name = "SerialNumber";
            serialColumn.FillWeight = 10;
            serialColumn.ReadOnly = true;
            scannerDataGridView.Columns.Add(serialColumn);

            DataGridViewTextBoxColumn pnpColumn = new DataGridViewTextBoxColumn();
            pnpColumn.HeaderText = "Device ID";
            pnpColumn.Name = "PNPDeviceID";
            pnpColumn.FillWeight = 25;
            pnpColumn.ReadOnly = true;
            scannerDataGridView.Columns.Add(pnpColumn);

            DataGridViewTextBoxColumn comPortColumn = new DataGridViewTextBoxColumn();
            comPortColumn.HeaderText = "COM port";
            comPortColumn.Name = "ComPort";
            comPortColumn.FillWeight = 8;
            comPortColumn.ReadOnly = true;
            scannerDataGridView.Columns.Add(comPortColumn);

            DataGridViewTextBoxColumn lineIdColumn = new DataGridViewTextBoxColumn();
            lineIdColumn.HeaderText = "Line";
            lineIdColumn.Name = "LineID";
            lineIdColumn.FillWeight = 10;
            lineIdColumn.ReadOnly = false;
            scannerDataGridView.Columns.Add(lineIdColumn);

            DataGridViewTextBoxColumn blockIdColumn = new DataGridViewTextBoxColumn();
            blockIdColumn.HeaderText = "Block";
            blockIdColumn.Name = "BlockID";
            blockIdColumn.FillWeight = 10;
            blockIdColumn.ReadOnly = false;
            scannerDataGridView.Columns.Add(blockIdColumn);

            DataGridViewTextBoxColumn supplierColumn = new DataGridViewTextBoxColumn();
            supplierColumn.HeaderText = "Supplier";
            supplierColumn.Name = "Supplier";
            supplierColumn.FillWeight = 10;
            supplierColumn.ReadOnly = false;
            scannerDataGridView.Columns.Add(supplierColumn);

            // COM Settings columns (editable for configuration)
            DataGridViewComboBoxColumn baudRateColumn = new DataGridViewComboBoxColumn();
            baudRateColumn.HeaderText = "Baud";
            baudRateColumn.Name = "BaudRate";
            baudRateColumn.FillWeight = 8;
            baudRateColumn.Items.AddRange(new object[] { "1200", "2400", "4800", "9600", "19200", "38400", "57600", "115200" });
            scannerDataGridView.Columns.Add(baudRateColumn);

            DataGridViewComboBoxColumn parityColumn = new DataGridViewComboBoxColumn();
            parityColumn.HeaderText = "Parity";
            parityColumn.Name = "Parity";
            parityColumn.FillWeight = 7;
            parityColumn.Items.AddRange(new object[] { "None", "Odd", "Even", "Mark", "Space" });
            scannerDataGridView.Columns.Add(parityColumn);

            DataGridViewComboBoxColumn dataBitsColumn = new DataGridViewComboBoxColumn();
            dataBitsColumn.HeaderText = "Data";
            dataBitsColumn.Name = "DataBits";
            dataBitsColumn.FillWeight = 6;
            dataBitsColumn.Items.AddRange(new object[] { "5", "6", "7", "8" });
            scannerDataGridView.Columns.Add(dataBitsColumn);

            DataGridViewComboBoxColumn stopBitsColumn = new DataGridViewComboBoxColumn();
            stopBitsColumn.HeaderText = "Stop";
            stopBitsColumn.Name = "StopBits";
            stopBitsColumn.FillWeight = 6;
            stopBitsColumn.Items.AddRange(new object[] { "None", "One", "Two", "OnePointFive" });
            scannerDataGridView.Columns.Add(stopBitsColumn);

            DataGridViewTextBoxColumn statusColumn = new DataGridViewTextBoxColumn();
            statusColumn.HeaderText = "Status";
            statusColumn.Name = "Status";
            statusColumn.FillWeight = 10;
            statusColumn.ReadOnly = true;
            scannerDataGridView.Columns.Add(statusColumn);

            // Add Delete button column
            DataGridViewButtonColumn deleteColumn = new DataGridViewButtonColumn();
            deleteColumn.HeaderText = "";
            deleteColumn.Name = "Delete";
            deleteColumn.Text = "Remove";
            deleteColumn.ToolTipText = "Remove this scanner";
            deleteColumn.UseColumnTextForButtonValue = true;
            deleteColumn.FillWeight = 8;
            scannerDataGridView.Columns.Add(deleteColumn);

            // Keep the grid in the SAME order as detectedScanners. Save and the status colour-coding
            // both map grid row index -> detectedScanners[index]; if the user clicked a column header
            // to sort, that mapping would silently desync and edits (Line/Block) would be written to
            // the wrong scanner (e.g. the COM3 row's values saved onto the COM1 entry). Disabling sort
            // guarantees the 1:1 mapping. (Visual layout is unchanged.)
            foreach (DataGridViewColumn col in scannerDataGridView.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            scannerDataGridView.SetMono("SerialNumber");
            scannerDataGridView.SetMono("PNPDeviceID");
            scannerDataGridView.SetMono("ComPort");
            scannerDataGridView.SetMuted("PNPDeviceID");
            scannerDataGridView.SetBadge("Status", v => Convert.ToString(v) == "Connected" ? SLTone.Success : SLTone.Error);
            scannerDataGridView.SetIconAction("Delete", "trash-2", danger: true);
            scannerDataGridView.CellFormatting -= StatusCellFormatting;
            scannerDataGridView.CellFormatting += StatusCellFormatting;

            // Populate data
            scannerDataGridView.Rows.Clear();
            foreach (var scanner in detectedScanners)
            {
                scannerDataGridView.Rows.Add(
                    scanner.SerialNumber,
                    scanner.PNPDeviceID,
                    scanner.GetComPortDisplay(),
                    scanner.LineID,
                    scanner.BlockID,
                    scanner.Supplier,
                    scanner.BaudRate,
                    scanner.Parity,
                    scanner.DataBits,
                    scanner.StopBits,
                    scanner.Status
                );
            }

            // Color-code the rows based on status
            foreach (DataGridViewRow row in scannerDataGridView.Rows)
            {
                if (row.Index >= 0 && row.Index < detectedScanners.Count)
                {
                    var scanner = detectedScanners[row.Index];

                    row.Cells["ComPort"].Value = scanner.GetComPortDisplay();
                    var statusValue = scanner.IsCurrentlyConnected ? "Connected" : "Not Connected";
                    row.Cells["Status"].Value = statusValue;

                    if ((scanner.ConnectionType ?? string.Empty).StartsWith("USB-HID", StringComparison.OrdinalIgnoreCase))
                    {
                        row.Cells["BaudRate"].ReadOnly = true;
                        row.Cells["Parity"].ReadOnly = true;
                        row.Cells["DataBits"].ReadOnly = true;
                        row.Cells["StopBits"].ReadOnly = true;
                        row.Cells["BaudRate"].Style.BackColor = Color.LightGray;
                        row.Cells["Parity"].Style.BackColor = Color.LightGray;
                        row.Cells["DataBits"].Style.BackColor = Color.LightGray;
                        row.Cells["StopBits"].Style.BackColor = Color.LightGray;
                    }
                    else
                    {
                        row.Cells["BaudRate"].ReadOnly = false;
                        row.Cells["Parity"].ReadOnly = false;
                        row.Cells["DataBits"].ReadOnly = false;
                        row.Cells["StopBits"].ReadOnly = false;
                        row.Cells["BaudRate"].Style.BackColor = SystemColors.Window;
                        row.Cells["Parity"].Style.BackColor = SystemColors.Window;
                        row.Cells["DataBits"].Style.BackColor = SystemColors.Window;
                        row.Cells["StopBits"].Style.BackColor = SystemColors.Window;
                    }
                }
                // Connection state is shown by the Status badge, not by tinting the whole row.
            }

            // Update FillWeight proportions after populating data
            UpdateColumnFillWeights();
            UpdateConnectionBanner();
            scannerDataGridView.ClearSelection();   // no row looks "chosen" when the page opens
            
            // Attach event handler for delete button clicks
            scannerDataGridView.CellContentClick += ScannerDataGridView_CellContentClick;
        }

        /// <summary>"Not Connected" is stored as-is (existing data); shown in sentence case.</summary>
        private void StatusCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex < 0 || scannerDataGridView.Columns[e.ColumnIndex].Name != "Status") return;
            if (Convert.ToString(e.Value) == "Not Connected") { e.Value = "Not connected"; e.FormattingApplied = true; }
        }

        private void ScannerDataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // Check if the clicked cell is in the Delete column
            if (e.ColumnIndex == scannerDataGridView.Columns["Delete"].Index && e.RowIndex >= 0)
            {
                // Get the scanner info for this row
                if (e.RowIndex < detectedScanners.Count)
                {
                    var scanner = detectedScanners[e.RowIndex];
                    
                    // Confirm deletion (the mockup's "Remove this scanner?" dialog)
                    bool confirmed;
                    using (var confirm = new SLDialog
                    {
                        Title = "Remove this scanner?",
                        Tone = SLDialogTone.Danger,
                        DialogWidth = 460,
                        Description = "The scanner on " + scanner.GetComPortDisplay() + " will stop sending scans to this site. You can add it again later."
                    })
                    {
                        var details = new SLKeyValueList();
                        details.Add("Device ID", scanner.PNPDeviceID ?? "");
                        details.Add("Line", string.IsNullOrEmpty(scanner.LineID) ? "—" : scanner.LineID);
                        details.Add("Block", string.IsNullOrEmpty(scanner.BlockID) ? "—" : scanner.BlockID);
                        details.Add("Supplier", string.IsNullOrEmpty(scanner.Supplier) ? "—" : scanner.Supplier);
                        confirm.Body.Controls.Add(details);
                        confirm.AddAction(new SLButton { Text = "Keep it", Variant = SLVariant.Secondary, DialogResult = DialogResult.Cancel });
                        confirm.AddAction(new SLButton { Text = "Remove scanner", Variant = SLVariant.Danger, DialogResult = DialogResult.OK });
                        confirmed = confirm.ShowDialog(DialogOwner) == DialogResult.OK;
                    }

                    if (confirmed)
                    {
                        LogDebug($"Deleting scanner: {scanner.PNPDeviceID}");

                        // Remove from list
                        detectedScanners.RemoveAt(e.RowIndex);

                        // Save updated configuration immediately
                        SaveScannersToFile();
                        ScannersSaved?.Invoke(this, EventArgs.Empty);

                        // Refresh the grid
                        PopulateDataGridView();

                        LogDebug($"Scanner deleted successfully");
                        ShowResult(SLTone.Success, "Scanner removed", "It will no longer send scans to this site.");
                    }
                }
            }
        }

        private void refreshButton_Click(object sender, EventArgs e)
        {
            LoadDetectedScanners();
            PopulateDataGridView();
        }

        private void configHelpButton_Click(object sender, EventArgs e)
        {
            using (var help = new SLDialog
            {
                Title = "Put a scanner in COM port mode",
                Description = "ScanLink reads scanners as COM ports. A scanner in keyboard (HID) mode drops back to it when unplugged until it is switched on the scanner itself.",
                DialogWidth = 600
            })
            {
                help.Body.Controls.Add(new SLBanner
                {
                    Tone = SLTone.Info,
                    IconName = "info",
                    Message = "The setting is stored in the scanner, so you only do this once per scanner. ScanLink cannot switch it from the computer."
                });
                AddHelpSection(help, "Option 1 — Datalogic Aladdin (recommended)",
                    "Download Aladdin from datalogic.com and connect the scanner by USB. In Aladdin go to Interface → USB, choose USB COM Port (Virtual COM Port), set the baud rate to 9600 and the suffix to CR+LF, then click Write Configuration.");
                AddHelpSection(help, "Option 2 — programming barcodes",
                    "Open your scanner's programming guide (search for \"<your model> programming guide\") and scan, in order: Enter Programming Mode, USB COM Port Mode, Save Configuration. The scanner beeps after each one.");
                AddHelpSection(help, "Which one fits my scanner?",
                    "Gryphon (GD/GBT): Aladdin. QuickScan (QD/QW/QM): programming barcodes. Magellan and PowerScan: Datalogic's configuration utility.");
                AddHelpSection(help, "Check that it worked",
                    "Open Device Manager (Win + X). The scanner should be under Ports (COM & LPT), not Keyboards. Then choose Look for scanners here.");
                var ok = new SLButton { Text = "Got it", DialogResult = DialogResult.OK };
                help.AddAction(ok);
                help.AcceptButton = ok;
                help.ShowDialog(DialogOwner);
            }
        }

        private static void AddHelpSection(SLDialog dialog, string title, string text)
        {
            var section = new SLStack(SLOrientation.Vertical, 4);
            section.AddRange(new SLText(title, SLTextStyle.TitleSm), new SLText(text, SLTextStyle.BodySm));
            dialog.Body.Controls.Add(section);
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Commit any in-progress cell edit so the latest typed value is read below. Without
                // this, clicking Save while a Line/Block cell was still being edited would save the
                // old value (the edit hadn't been pushed to the cell yet).
                scannerDataGridView.EndEdit();

                // Update the detectedScanners list with current data from the grid. Match each row to
                // its scanner by PNPDeviceID (the read-only identity column) rather than by row index,
                // so edits always land on the correct scanner even if the grid order ever differs.
                for (int i = 0; i < scannerDataGridView.Rows.Count; i++)
                {
                    var row = scannerDataGridView.Rows[i];
                    string pnp = row.Cells["PNPDeviceID"].Value?.ToString();
                    if (string.IsNullOrWhiteSpace(pnp))
                        continue;

                    var scanner = detectedScanners.FirstOrDefault(s =>
                        string.Equals(s.PNPDeviceID, pnp, StringComparison.OrdinalIgnoreCase));
                    if (scanner == null)
                        continue;

                    scanner.LineID = row.Cells["LineID"].Value?.ToString() ?? "";
                    scanner.BlockID = row.Cells["BlockID"].Value?.ToString() ?? "";
                    scanner.Supplier = row.Cells["Supplier"].Value?.ToString() ?? "";
                    scanner.BaudRate = row.Cells["BaudRate"].Value?.ToString() ?? "9600";
                    scanner.Parity = row.Cells["Parity"].Value?.ToString() ?? "None";
                    scanner.DataBits = row.Cells["DataBits"].Value?.ToString() ?? "8";
                    scanner.StopBits = row.Cells["StopBits"].Value?.ToString() ?? "One";
                    // Status and COM Port are read-only and managed automatically.
                }
                
				SaveScannersToFile();
				ScannersSaved?.Invoke(this, EventArgs.Empty);
                ShowResult(SLTone.Success, "Scanner assignments saved", "Scanners are reconnecting with the new settings.");
            }
            catch (Exception ex)
            {
                ShowResult(SLTone.Error, "Couldn't save scanner assignments", ex.Message);
            }
        }

        private void SaveScannersToFile()
        {
            try
            {

                // Save to ProgramData for write permissions
                string savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ScanLink", "scanner_assignments.txt");
                try { Directory.CreateDirectory(Path.GetDirectoryName(savePath)); } catch {}
                
                // Load existing assignments from file
                Dictionary<string, ScannerInfo> existingAssignments = new Dictionary<string, ScannerInfo>(StringComparer.OrdinalIgnoreCase);
                if (File.Exists(savePath))
                {
                    string[] existingLines = File.ReadAllLines(savePath);
                    existingAssignments = ParseAssignmentsFile(existingLines);
                }
                
                // Update or add new scanner assignments
                foreach (var scanner in detectedScanners)
                {
                    if (string.IsNullOrWhiteSpace(scanner.PNPDeviceID) || scanner.PNPDeviceID == "N/A")
                    {
                        continue;
                    }

                    var assignment = new ScannerInfo
                    {
                        SerialNumber = scanner.SerialNumber,
                        PNPDeviceID = scanner.PNPDeviceID,
                        ConnectionType = string.IsNullOrWhiteSpace(scanner.ConnectionType) ? "USB-COM" : scanner.ConnectionType,
                        ComPort = scanner.ConnectionType != null && scanner.ConnectionType.StartsWith("USB-HID", StringComparison.OrdinalIgnoreCase)
                            ? scanner.ModeDisplay
                            : scanner.ComPort,
                        LineID = scanner.LineID,
                        BlockID = scanner.BlockID,
                        Supplier = scanner.Supplier,
                        BaudRate = string.IsNullOrWhiteSpace(scanner.BaudRate) ? "9600" : scanner.BaudRate,
                        Parity = string.IsNullOrWhiteSpace(scanner.Parity) ? "None" : scanner.Parity,
                        DataBits = string.IsNullOrWhiteSpace(scanner.DataBits) ? "8" : scanner.DataBits,
                        StopBits = string.IsNullOrWhiteSpace(scanner.StopBits) ? "One" : scanner.StopBits
                    };

                    existingAssignments[assignment.AssignmentKey] = assignment;
                }
                
                // Write updated assignments to file
                using (StreamWriter writer = new StreamWriter(savePath))
                {
                    writer.WriteLine("Scanner Assignments - Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    writer.WriteLine("=" + new string('=', 70));
                    writer.WriteLine();

                    int scannerNum = 1;
                    foreach (var assignment in existingAssignments.Values
                                 .OrderBy(a => a.ConnectionType ?? "USB-COM")
                                 .ThenBy(a => a.PNPDeviceID, StringComparer.OrdinalIgnoreCase))
                    {
                        string connectionType = assignment.ConnectionType ?? "USB-COM";
                        string comDisplay = connectionType.StartsWith("USB-HID", StringComparison.OrdinalIgnoreCase)
                            ? assignment.ModeDisplay
                            : (string.IsNullOrWhiteSpace(assignment.ComPort) ? "Auto-detect" : assignment.ComPort);

                        writer.WriteLine($"Scanner #{scannerNum}:");
                        writer.WriteLine($"  PNPDeviceID: {assignment.PNPDeviceID}");
                        writer.WriteLine($"  Connection Type: {connectionType}");
                        writer.WriteLine($"  COM Port: {comDisplay}");
                        writer.WriteLine($"  Line ID: {assignment.LineID ?? ""}");
                        writer.WriteLine($"  Block ID: {assignment.BlockID ?? ""}");
                        writer.WriteLine($"  Supplier: {assignment.Supplier ?? ""}");
                        writer.WriteLine($"  Baud Rate: {assignment.BaudRate ?? "9600"}");
                        writer.WriteLine($"  Parity: {assignment.Parity ?? "None"}");
                        writer.WriteLine($"  Data Bits: {assignment.DataBits ?? "8"}");
                        writer.WriteLine($"  Stop Bits: {assignment.StopBits ?? "One"}");
                        writer.WriteLine();
                        scannerNum++;
                    }
                }
                
                LogDebug($"Scanner assignments saved to: {savePath}");
            }
            catch (Exception ex)
            {
                LogDebug($"Error saving scanner assignments: {ex.Message}");
                throw; // Re-throw to be handled by caller
            }
        }
    }
}
