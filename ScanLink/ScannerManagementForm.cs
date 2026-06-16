using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Management;

namespace ScanLink
{
    public partial class ScannerManagementForm : Form
    {
        private DataGridView scannerDataGridView;
        private Button saveButton;
        private Button refreshButton;
        private Button configHelpButton;
        private Label titleLabel;
        private TextBox debugOutputTextBox;
        private Label debugLabel;
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

        private void InitializeComponent()
        {
            this.scannerDataGridView = new DataGridView();
            this.saveButton = new Button();
            this.refreshButton = new Button();
            this.configHelpButton = new Button();
            this.titleLabel = new Label();
            this.debugOutputTextBox = new TextBox();
            this.debugLabel = new Label();
            this.SuspendLayout();

            // 
            // titleLabel - Centered and responsive
            // 
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            this.titleLabel.ForeColor = Color.FromArgb(52, 73, 94);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new Size(250, 20);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "🔧 Scanner Management";
            this.titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            this.titleLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // 
            // scannerDataGridView - Responsive with margins
            // 
            this.scannerDataGridView.AllowUserToAddRows = false;
            this.scannerDataGridView.AllowUserToDeleteRows = false;
            this.scannerDataGridView.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            this.scannerDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.scannerDataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.scannerDataGridView.BackgroundColor = Color.White;
            this.scannerDataGridView.BorderStyle = BorderStyle.Fixed3D;
            this.scannerDataGridView.GridColor = Color.FromArgb(230, 230, 230);
            this.scannerDataGridView.Name = "scannerDataGridView";
            this.scannerDataGridView.RowHeadersVisible = false;
            this.scannerDataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            // Only one row selectable at a time. With MultiSelect (the default) clicking a second row
            // left both it and the previously-selected/connected row highlighted (two blue rows), which
            // made it look like two scanners were selected and confused which one's edit was saved.
            // Same root cause as the printer USB dialog fix.
            this.scannerDataGridView.MultiSelect = false;
            this.scannerDataGridView.TabIndex = 1;

            // 
            // refreshButton - Bottom left with margin
            // 
            this.refreshButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.refreshButton.BackColor = Color.FromArgb(50, 74, 95);
            this.refreshButton.FlatAppearance.BorderSize = 0;
            this.refreshButton.FlatStyle = FlatStyle.Flat;
            this.refreshButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.refreshButton.ForeColor = Color.White;
            this.refreshButton.Name = "refreshButton";
            this.refreshButton.Size = new Size(140, 40);
            this.refreshButton.TabIndex = 2;
            this.refreshButton.Text = "🔄 Refresh";
            this.refreshButton.UseVisualStyleBackColor = false;
            this.refreshButton.Click += new EventHandler(this.refreshButton_Click);

            // 
            // configHelpButton - Bottom middle
            // 
            this.configHelpButton.Anchor = AnchorStyles.Bottom;
            this.configHelpButton.BackColor = Color.FromArgb(41, 128, 185);
            this.configHelpButton.FlatAppearance.BorderSize = 0;
            this.configHelpButton.FlatStyle = FlatStyle.Flat;
            this.configHelpButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.configHelpButton.ForeColor = Color.White;
            this.configHelpButton.Name = "configHelpButton";
            this.configHelpButton.Size = new Size(200, 40);
            this.configHelpButton.TabIndex = 4;
            this.configHelpButton.Text = "📋 COM Mode Setup Help";
            this.configHelpButton.UseVisualStyleBackColor = false;
            this.configHelpButton.Click += new EventHandler(this.configHelpButton_Click);

            // 
            // saveButton - Bottom right with margin
            // 
            this.saveButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.saveButton.BackColor = Color.FromArgb(50, 74, 95);
            this.saveButton.FlatAppearance.BorderSize = 0;
            this.saveButton.FlatStyle = FlatStyle.Flat;
            this.saveButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.saveButton.ForeColor = Color.White;
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new Size(140, 40);
            this.saveButton.TabIndex = 3;
            this.saveButton.Text = "💾 Save";
            this.saveButton.UseVisualStyleBackColor = false;
            this.saveButton.Click += new EventHandler(this.saveButton_Click);

            // 
            // debugLabel
            // 
            this.debugLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.debugLabel.AutoSize = true;
            this.debugLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.debugLabel.ForeColor = Color.FromArgb(52, 73, 94);
            this.debugLabel.Name = "debugLabel";
            this.debugLabel.Text = "🔍 Detection Log:";
            this.debugLabel.TabIndex = 4;

            // 
            // debugOutputTextBox
            // 
            this.debugOutputTextBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.debugOutputTextBox.BackColor = Color.FromArgb(240, 240, 240);
            this.debugOutputTextBox.BorderStyle = BorderStyle.FixedSingle;
            this.debugOutputTextBox.Font = new Font("Consolas", 8F);
            this.debugOutputTextBox.Multiline = true;
            this.debugOutputTextBox.Name = "debugOutputTextBox";
            this.debugOutputTextBox.ReadOnly = true;
            this.debugOutputTextBox.ScrollBars = ScrollBars.Vertical;
            this.debugOutputTextBox.TabIndex = 5;

            // 
            // ScannerManagementForm - Responsive and centered
            // 
            this.AutoScaleDimensions = new SizeF(96F, 96F); // Use DPI-aware scaling
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.ClientSize = new Size(900, 600); // Reduced height for more compact dialog
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.configHelpButton);
            this.Controls.Add(this.refreshButton);
            this.Controls.Add(this.scannerDataGridView);
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.debugLabel);
            this.Controls.Add(this.debugOutputTextBox);
            this.MinimumSize = new Size(800, 600); // Set minimum size for usability
            this.Name = "ScannerManagementForm";
            this.Text = "Scanner Management - ScanLink";
            this.StartPosition = FormStartPosition.CenterParent;
            this.WindowState = FormWindowState.Normal;
            
            // Add event handlers for responsive layout
            this.Load += ScannerManagementForm_Load;
            this.Resize += ScannerManagementForm_Resize;
            
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void ScannerManagementForm_Load(object sender, EventArgs e)
        {
            // Initial layout setup
            LayoutForm();
            
            // Ensure grid is populated on form load
            if (detectedScanners != null && detectedScanners.Count > 0 && scannerDataGridView.Rows.Count == 0)
            {
                PopulateDataGridView();
            }
        }

        private void ScannerManagementForm_Resize(object sender, EventArgs e)
        {
            // Recalculate layout when form is resized
            LayoutForm();
        }

        private void LayoutForm()
        {
            if (this.Width < 50 || this.Height < 50) return; // Avoid layout during form creation

            // Use client size for precise layout (excludes borders/title bar)
            int clientWidth = this.ClientSize.Width;
            int clientHeight = this.ClientSize.Height;

            // Calculate responsive margins (percentage-based with smooth scaling)
            int horizontalMargin = Math.Max(20, clientWidth / 20); // 5% margin, minimum 20px
            int topMargin = Math.Max(10, clientHeight / 40); // slightly smaller to move content upward
            int bottomPadding = 24; // reserve extra space so buttons are fully visible
            int buttonHeight = Math.Max(35, this.refreshButton.Height);
            int titleHeight = 30; // compact title height to free space

            // Position title label - centered horizontally at top with bounds checking
            int titleWidth = this.titleLabel.PreferredWidth;
            int titleX = (clientWidth - titleWidth) / 2;
            titleX = Math.Max(horizontalMargin, Math.Min(titleX, clientWidth - titleWidth - horizontalMargin));
            this.titleLabel.Location = new Point(titleX, topMargin);
            this.titleLabel.Size = new Size(titleWidth, titleHeight);

            // Position DataGridView - responsive with margins; reduce height to keep buttons and debug panel visible
            int gridTop = topMargin + titleHeight + 10; // gap after title
            int debugPanelHeight = 120; // height for debug output
            int bottomReserved = buttonHeight + bottomPadding + 10 + debugPanelHeight + 30; // include debug panel
            int gridBottom = clientHeight - bottomReserved;
            int gridLeft = horizontalMargin;
            int gridRight = clientWidth - horizontalMargin;

            // Ensure minimum grid size
            int gridWidth = Math.Max(400, gridRight - gridLeft);
            int gridHeight = Math.Max(200, gridBottom - gridTop);

            this.scannerDataGridView.Location = new Point(gridLeft, gridTop);
            this.scannerDataGridView.Size = new Size(gridWidth, gridHeight);

            // Position debug label and textbox above buttons
            int debugLabelY = gridBottom + 15;
            this.debugLabel.Location = new Point(horizontalMargin, debugLabelY);
            
            int debugTextBoxY = debugLabelY + 20;
            this.debugOutputTextBox.Location = new Point(horizontalMargin, debugTextBoxY);
            this.debugOutputTextBox.Size = new Size(gridWidth, debugPanelHeight);

            // Position buttons at bottom with margins
            int buttonY = clientHeight - bottomPadding - buttonHeight;
            this.refreshButton.Location = new Point(horizontalMargin, buttonY);
            
            // Center the config help button
            int configButtonX = (clientWidth - this.configHelpButton.Width) / 2;
            this.configHelpButton.Location = new Point(configButtonX, buttonY);
            
            this.saveButton.Location = new Point(clientWidth - horizontalMargin - this.saveButton.Width, buttonY);

            // Add visual feedback for form state
            UpdateFormVisuals();

            // With Fill mode enabled, columns fill automatically; adjust weights if needed
            UpdateColumnFillWeights();
        }

        private void UpdateFormVisuals()
        {
            // Update form appearance based on size for better UX
            if (this.WindowState == FormWindowState.Maximized)
            {
                this.BackColor = Color.FromArgb(245, 248, 250); // Slightly lighter for maximized state
            }
            else
            {
                this.BackColor = Color.FromArgb(248, 249, 250); // Standard color for normal state
            }

            // Add subtle border effect for better visual separation
            if (this.Width > 1000)
            {
                // Larger form - add more visual elements
                this.scannerDataGridView.BorderStyle = BorderStyle.Fixed3D;
            }
            else
            {
                // Smaller form - use simpler border
                this.scannerDataGridView.BorderStyle = BorderStyle.FixedSingle;
            }
        }

        private void UpdateColumnFillWeights()
        {
            if (scannerDataGridView.Columns.Count == 0) return;

            // Adjust FillWeight based on form width - more columns now with COM settings
            bool isWideForm = this.ClientSize.Width > 1200;
            bool isNarrowForm = this.ClientSize.Width < 900;

            if (isWideForm)
            {
                SetColumnFillWeight("SerialNumber", 10f);
                SetColumnFillWeight("PNPDeviceID", 23f);
                SetColumnFillWeight("ComPort", 8f);
                SetColumnFillWeight("LineID", 8f);
                SetColumnFillWeight("BlockID", 8f);
                SetColumnFillWeight("Supplier", 9f);
                SetColumnFillWeight("BaudRate", 7f);
                SetColumnFillWeight("Parity", 6f);
                SetColumnFillWeight("DataBits", 5f);
                SetColumnFillWeight("StopBits", 5f);
                SetColumnFillWeight("Status", 10f);
                SetColumnFillWeight("Delete", 8f);
            }
            else if (isNarrowForm)
            {
                SetColumnFillWeight("SerialNumber", 8f);
                SetColumnFillWeight("PNPDeviceID", 18f);
                SetColumnFillWeight("ComPort", 7f);
                SetColumnFillWeight("LineID", 9f);
                SetColumnFillWeight("BlockID", 9f);
                SetColumnFillWeight("Supplier", 10f);
                SetColumnFillWeight("BaudRate", 8f);
                SetColumnFillWeight("Parity", 7f);
                SetColumnFillWeight("DataBits", 6f);
                SetColumnFillWeight("StopBits", 6f);
                SetColumnFillWeight("Status", 7f);
                SetColumnFillWeight("Delete", 9f);
            }
            else
            {
                // Medium size - balanced
                SetColumnFillWeight("SerialNumber", 9f);
                SetColumnFillWeight("PNPDeviceID", 20f);
                SetColumnFillWeight("ComPort", 8f);
                SetColumnFillWeight("LineID", 9f);
                SetColumnFillWeight("BlockID", 9f);
                SetColumnFillWeight("Supplier", 9f);
                SetColumnFillWeight("BaudRate", 7f);
                SetColumnFillWeight("Parity", 6f);
                SetColumnFillWeight("DataBits", 6f);
                SetColumnFillWeight("StopBits", 5f);
                SetColumnFillWeight("Status", 9f);
                SetColumnFillWeight("Delete", 8f);
            }
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
                    MessageBox.Show($"Scanner detection script not found at: {scriptPath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        MessageBox.Show($"Error running scanner detection: {error}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show($"Error loading scanners: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            debugOutputTextBox.AppendText($"[{timestamp}] {message}\r\n");
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
                MessageBox.Show($"Error loading historical scanners: {ex.Message}", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                
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
            pnpColumn.HeaderText = "PNPDeviceID";
            pnpColumn.Name = "PNPDeviceID";
            pnpColumn.FillWeight = 25;
            pnpColumn.ReadOnly = true;
            scannerDataGridView.Columns.Add(pnpColumn);

            DataGridViewTextBoxColumn comPortColumn = new DataGridViewTextBoxColumn();
            comPortColumn.HeaderText = "COM Port";
            comPortColumn.Name = "ComPort";
            comPortColumn.FillWeight = 8;
            comPortColumn.ReadOnly = true;
            scannerDataGridView.Columns.Add(comPortColumn);

            DataGridViewTextBoxColumn lineIdColumn = new DataGridViewTextBoxColumn();
            lineIdColumn.HeaderText = "Line ID";
            lineIdColumn.Name = "LineID";
            lineIdColumn.FillWeight = 10;
            lineIdColumn.ReadOnly = false;
            scannerDataGridView.Columns.Add(lineIdColumn);

            DataGridViewTextBoxColumn blockIdColumn = new DataGridViewTextBoxColumn();
            blockIdColumn.HeaderText = "Block ID";
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
            deleteColumn.HeaderText = "Action";
            deleteColumn.Name = "Delete";
            deleteColumn.Text = "🗑 Delete";
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

                if (row.Cells["Status"].Value?.ToString() == "Connected")
                {
                    row.DefaultCellStyle.BackColor = Color.LightGreen;
                }
                else if (row.Cells["Status"].Value?.ToString() == "Not Connected")
                {
                    row.DefaultCellStyle.BackColor = Color.LightCoral;
                }
            }

            // Update FillWeight proportions after populating data
            UpdateColumnFillWeights();
            
            // Attach event handler for delete button clicks
            scannerDataGridView.CellContentClick += ScannerDataGridView_CellContentClick;
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
                    
                    // Confirm deletion
                    var result = MessageBox.Show(
                        $"Are you sure you want to delete this scanner configuration?\n\n" +
                        $"PNPDeviceID: {scanner.PNPDeviceID}\n" +
                        $"COM Port: {scanner.GetComPortDisplay()}\n" +
                        $"Line ID: {scanner.LineID}\n" +
                        $"Block ID: {scanner.BlockID}\n" +
                        $"Supplier: {scanner.Supplier}",
                        "Confirm Delete",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning
                    );
                    
                    if (result == DialogResult.Yes)
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
                        MessageBox.Show("Scanner configuration deleted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            string helpMessage = @"🔧 How to Configure Scanner for COM Port Mode

PROBLEM: Scanner keeps reverting to HID Keyboard mode when unplugged.

SOLUTION: Permanently configure scanner using one of these methods:

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

METHOD 1: Use Datalogic Configuration Software (RECOMMENDED)

1. Download 'Datalogic Aladdin' from:
   https://www.datalogic.com

2. Connect scanner via USB

3. Open Aladdin software

4. Go to: Interface → USB

5. Select: 'USB COM Port (Virtual COM Port)'

6. Set Baud Rate: 9600

7. Enable Suffix: CR+LF

8. Click 'Write Configuration' to save permanently

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

METHOD 2: Scan Programming Barcodes

1. Find your scanner's programming guide (PDF):
   - Search: '[Your Scanner Model] programming guide'
   - Example: 'Datalogic Gryphon programming guide'

2. In the PDF, find and scan these barcodes IN ORDER:
   a. 'Enter Programming Mode'
   b. 'USB COM Port Mode' or 'USB Virtual COM Port'
   c. 'Save Configuration' or 'Exit Programming'

3. The scanner will beep to confirm each scan

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

METHOD 3: Check Scanner Model Documentation

Common Datalogic models:
• Gryphon (GD/GBT series) → Use Aladdin software
• QuickScan (QD/QW/QM series) → Use programming barcodes
• Magellan series → Use configuration utility
• PowerScan series → Use Datalogic Scan Config

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

VERIFY CONFIGURATION:

1. Open Device Manager (Win + X → Device Manager)

2. Check under 'Ports (COM & LPT)':
   ✓ Should see: 'Datalogic USB-COM Port (COMx)'
   ✗ If under 'Keyboards': Still in HID mode

3. In ScanLink, click 'Refresh' to detect the COM port

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

IMPORTANT:
• Configuration is saved IN THE SCANNER
• Will persist even after unplugging
• Only needs to be done ONCE per scanner
• ScanLink cannot switch mode via software (scanner hardware limitation)

Need more help? Contact Datalogic support or check their website.";

            MessageBox.Show(helpMessage, "Scanner COM Port Configuration Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                MessageBox.Show("Scanner assignments saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving scanner assignments: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
