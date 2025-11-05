using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

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
            public string LineID { get; set; }
            public string BlockID { get; set; }
            public string BaudRate { get; set; } = "9600";
            public string Parity { get; set; } = "None";
            public string DataBits { get; set; } = "8";
            public string StopBits { get; set; } = "One";
            public string Status { get; set; }
            public bool IsCurrentlyConnected { get; set; }
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
            this.ClientSize = new Size(900, 800); // Increased initial size for debug panel
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
                SetColumnFillWeight("LineID", 9f);
                SetColumnFillWeight("BlockID", 9f);
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
                SetColumnFillWeight("PNPDeviceID", 20f);
                SetColumnFillWeight("ComPort", 7f);
                SetColumnFillWeight("LineID", 11f);
                SetColumnFillWeight("BlockID", 11f);
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
                SetColumnFillWeight("PNPDeviceID", 22f);
                SetColumnFillWeight("ComPort", 8f);
                SetColumnFillWeight("LineID", 10f);
                SetColumnFillWeight("BlockID", 10f);
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
                string currentPNPDeviceID = null;
                string currentComPort = "";
                string currentLineID = "";
                string currentBlockID = "";
                string currentBaudRate = "9600";
                string currentParity = "None";
                string currentDataBits = "8";
                string currentStopBits = "One";
                
                foreach (string line in existingLines)
                {
                    string trimmedLine = line.Trim();
                    
                    if (trimmedLine.StartsWith("PNPDeviceID:"))
                    {
                        currentPNPDeviceID = trimmedLine.Substring("PNPDeviceID:".Length).Trim();
                    }
                    else if (trimmedLine.StartsWith("COM Port:"))
                    {
                        currentComPort = trimmedLine.Substring("COM Port:".Length).Trim();
                        if (currentComPort == "Auto-detect") currentComPort = "";
                    }
                    else if (trimmedLine.StartsWith("Line ID:"))
                    {
                        currentLineID = trimmedLine.Substring("Line ID:".Length).Trim();
                    }
                    else if (trimmedLine.StartsWith("Block ID:"))
                    {
                        currentBlockID = trimmedLine.Substring("Block ID:".Length).Trim();
                    }
                    else if (trimmedLine.StartsWith("Baud Rate:"))
                    {
                        currentBaudRate = trimmedLine.Substring("Baud Rate:".Length).Trim();
                    }
                    else if (trimmedLine.StartsWith("Parity:"))
                    {
                        currentParity = trimmedLine.Substring("Parity:".Length).Trim();
                    }
                    else if (trimmedLine.StartsWith("Data Bits:"))
                    {
                        currentDataBits = trimmedLine.Substring("Data Bits:".Length).Trim();
                    }
                    else if (trimmedLine.StartsWith("Stop Bits:"))
                    {
                        currentStopBits = trimmedLine.Substring("Stop Bits:".Length).Trim();
                        
                        // Add historical scanner (initially marked as not connected) - complete entry
                        if (!string.IsNullOrEmpty(currentPNPDeviceID))
                        {
                            detectedScanners.Add(new ScannerInfo
                            {
                                SerialNumber = $"Scanner {detectedScanners.Count + 1}",
                                PNPDeviceID = currentPNPDeviceID,
                                ComPort = currentComPort,
                                LineID = currentLineID,
                                BlockID = currentBlockID,
                                BaudRate = currentBaudRate,
                                Parity = currentParity,
                                DataBits = currentDataBits,
                                StopBits = currentStopBits,
                                Status = "Not Connected",
                                IsCurrentlyConnected = false
                            });
                            
                            System.Diagnostics.Debug.WriteLine($"Added historical scanner: {currentPNPDeviceID} - COM: {currentComPort}, Line: {currentLineID}, Block: {currentBlockID}");
                        }
                        
                        // Reset for next entry
                        currentPNPDeviceID = null;
                        currentComPort = "";
                        currentLineID = "";
                        currentBlockID = "";
                        currentBaudRate = "9600";
                        currentParity = "None";
                        currentDataBits = "8";
                        currentStopBits = "One";
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"Total historical scanners loaded: {detectedScanners.Count}");
                
                // If no historical scanners found, add a test entry to verify functionality
                if (detectedScanners.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No historical scanners found, adding test entry");
                    detectedScanners.Add(new ScannerInfo
                    {
                        SerialNumber = "Test Scanner",
                        PNPDeviceID = "USB\\VID_05F9&PID_2216\\S/N_G24HD1690",
                        LineID = "5",
                        BlockID = "9",
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
                    Status = "Not Connected",
                    IsCurrentlyConnected = false
                });
            }
        }

        private void LoadExistingAssignments()
        {
            try
            {
                // Prefer ProgramData; fallback to project source path
                string assignmentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ScanLink", "scanner_assignments.txt");
                if (!File.Exists(assignmentsPath))
                {
                    assignmentsPath = Path.Combine(Application.StartupPath, "..", "..", "ScanLinkScanner", "scanner_assignments.txt");
                }
                
                if (!File.Exists(assignmentsPath))
                {
                    return; // No existing assignments file, nothing to load
                }

                string[] existingLines = File.ReadAllLines(assignmentsPath);
                string currentPNPDeviceID = null;
                string currentLineID = "";
                string currentBlockID = "";
                
                Dictionary<string, ScannerInfo> savedAssignments = new Dictionary<string, ScannerInfo>();
                
                foreach (string line in existingLines)
                {
                    string trimmedLine = line.Trim();
                    
                    if (trimmedLine.StartsWith("PNPDeviceID:"))
                    {
                        currentPNPDeviceID = trimmedLine.Substring("PNPDeviceID:".Length).Trim();
                    }
                    else if (trimmedLine.StartsWith("Line ID:"))
                    {
                        currentLineID = trimmedLine.Substring("Line ID:".Length).Trim();
                    }
                    else if (trimmedLine.StartsWith("Block ID:"))
                    {
                        currentBlockID = trimmedLine.Substring("Block ID:".Length).Trim();
                        
                        // Save the complete entry
                        if (!string.IsNullOrEmpty(currentPNPDeviceID))
                        {
                            savedAssignments[currentPNPDeviceID] = new ScannerInfo
                            {
                                PNPDeviceID = currentPNPDeviceID,
                                LineID = currentLineID,
                                BlockID = currentBlockID
                            };
                        }
                        
                        // Reset for next entry
                        currentPNPDeviceID = null;
                        currentLineID = "";
                        currentBlockID = "";
                    }
                }
                
                // Match detected scanners with saved assignments
                foreach (var scanner in detectedScanners)
                {
                    if (!string.IsNullOrEmpty(scanner.PNPDeviceID) && 
                        savedAssignments.ContainsKey(scanner.PNPDeviceID))
                    {
                        var savedAssignment = savedAssignments[scanner.PNPDeviceID];
                        scanner.LineID = savedAssignment.LineID;
                        scanner.BlockID = savedAssignment.BlockID;
                    }
                }
            }
            catch (Exception ex)
            {
                // Silently fail if we can't load existing assignments
                // The form will still work, just without pre-populated values
                System.Diagnostics.Debug.WriteLine($"Error loading existing assignments: {ex.Message}");
            }
        }

        private void ParseCurrentScanners(string output)
        {
            LogDebug("--- Parsing PowerShell Output ---");
            System.Diagnostics.Debug.WriteLine($"Parsing current scanners output: {output}");
            string[] lines = output.Split('\n');
            List<(string pnpId, string comPort)> currentScanners = new List<(string, string)>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                
                // Look for scanner entries
                if (line.StartsWith("Scanner #"))
                {
                    LogDebug($"Found scanner entry: {line}");
                    string pnpDeviceID = null;
                    string comPort = null;
                    
                    // Look for PNPDeviceID and COM Port in the next few lines
                    for (int j = i + 1; j < Math.Min(i + 10, lines.Length); j++)
                    {
                        string nextLine = lines[j].Trim();
                        if (nextLine.StartsWith("COM Port:"))
                        {
                            comPort = nextLine.Substring("COM Port:".Length).Trim();
                            LogDebug($"  Found COM Port: {comPort}");
                        }
                        else if (nextLine.StartsWith("PNPDeviceID:"))
                        {
                            pnpDeviceID = nextLine.Substring("PNPDeviceID:".Length).Trim();
                            LogDebug($"  Found PNPDeviceID: {pnpDeviceID}");
                        }
                        
                        // If we found both, add to list
                        if (!string.IsNullOrEmpty(pnpDeviceID) && !string.IsNullOrEmpty(comPort))
                        {
                            currentScanners.Add((pnpDeviceID, comPort));
                            LogDebug($"✓ Added scanner: {pnpDeviceID} on {comPort}");
                            System.Diagnostics.Debug.WriteLine($"Found currently connected scanner: {pnpDeviceID} on {comPort}");
                            break;
                        }
                    }
                }
            }
            
            LogDebug($"Parsed {currentScanners.Count} currently connected scanner(s)");
            System.Diagnostics.Debug.WriteLine($"Total currently connected scanners found: {currentScanners.Count}");

            // Update the detectedScanners list to mark which ones are currently connected
            foreach (var scanner in detectedScanners)
            {
                var match = currentScanners.FirstOrDefault(s => s.pnpId == scanner.PNPDeviceID);
                if (!string.IsNullOrEmpty(match.pnpId))
                {
                    scanner.IsCurrentlyConnected = true;
                    scanner.Status = "Connected";
                    scanner.ComPort = match.comPort;
                    System.Diagnostics.Debug.WriteLine($"Scanner {scanner.PNPDeviceID} marked as CONNECTED on {scanner.ComPort}");
                }
                else
                {
                    scanner.IsCurrentlyConnected = false;
                    scanner.Status = "Not Connected";
                    System.Diagnostics.Debug.WriteLine($"Scanner {scanner.PNPDeviceID} marked as NOT CONNECTED");
                }
            }

            // Add any new scanners that are currently connected but not in historical data
            foreach (var (pnpId, comPort) in currentScanners)
            {
                if (!detectedScanners.Any(s => s.PNPDeviceID == pnpId))
                {
                    detectedScanners.Add(new ScannerInfo
                    {
                        SerialNumber = $"Scanner {detectedScanners.Count + 1}",
                        PNPDeviceID = pnpId,
                        ComPort = comPort,
                        LineID = "",
                        BlockID = "",
                        Status = "Connected",
                        IsCurrentlyConnected = true
                    });
                }
            }

            // If no scanners found in the output and no historical data, create a default entry
            if (detectedScanners.Count == 0)
            {
                detectedScanners.Add(new ScannerInfo
                {
                    SerialNumber = "No scanners detected",
                    PNPDeviceID = "N/A",
                    LineID = "",
                    BlockID = "",
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

            // Populate data
            scannerDataGridView.Rows.Clear();
            foreach (var scanner in detectedScanners)
            {
                scannerDataGridView.Rows.Add(
                    scanner.SerialNumber,
                    scanner.PNPDeviceID,
                    scanner.ComPort ?? "N/A",
                    scanner.LineID,
                    scanner.BlockID,
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
                        $"COM Port: {scanner.ComPort}\n" +
                        $"Line ID: {scanner.LineID}\n" +
                        $"Block ID: {scanner.BlockID}",
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
                // Update the detectedScanners list with current data from DataGridView
                for (int i = 0; i < scannerDataGridView.Rows.Count; i++)
                {
                    if (i < detectedScanners.Count)
                    {
                        detectedScanners[i].LineID = scannerDataGridView.Rows[i].Cells["LineID"].Value?.ToString() ?? "";
                        detectedScanners[i].BlockID = scannerDataGridView.Rows[i].Cells["BlockID"].Value?.ToString() ?? "";
                        detectedScanners[i].BaudRate = scannerDataGridView.Rows[i].Cells["BaudRate"].Value?.ToString() ?? "9600";
                        detectedScanners[i].Parity = scannerDataGridView.Rows[i].Cells["Parity"].Value?.ToString() ?? "None";
                        detectedScanners[i].DataBits = scannerDataGridView.Rows[i].Cells["DataBits"].Value?.ToString() ?? "8";
                        detectedScanners[i].StopBits = scannerDataGridView.Rows[i].Cells["StopBits"].Value?.ToString() ?? "One";
                        // Note: Status and ComPort are read-only and managed automatically
                    }
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
                Dictionary<string, ScannerInfo> existingAssignments = new Dictionary<string, ScannerInfo>();
                if (File.Exists(savePath))
                {
                    string[] existingLines = File.ReadAllLines(savePath);
                    string currentPNPDeviceID = null;
                    string currentLineID = "";
                    string currentBlockID = "";
                    
                    foreach (string line in existingLines)
                    {
                        string trimmedLine = line.Trim();
                        
                        if (trimmedLine.StartsWith("PNPDeviceID:"))
                        {
                            currentPNPDeviceID = trimmedLine.Substring("PNPDeviceID:".Length).Trim();
                        }
                        else if (trimmedLine.StartsWith("Line ID:"))
                        {
                            currentLineID = trimmedLine.Substring("Line ID:".Length).Trim();
                        }
                        else if (trimmedLine.StartsWith("Block ID:"))
                        {
                            currentBlockID = trimmedLine.Substring("Block ID:".Length).Trim();
                            
                            // Save the complete entry
                            if (!string.IsNullOrEmpty(currentPNPDeviceID))
                            {
                                existingAssignments[currentPNPDeviceID] = new ScannerInfo
                                {
                                    PNPDeviceID = currentPNPDeviceID,
                                    LineID = currentLineID,
                                    BlockID = currentBlockID
                                };
                            }
                            
                            // Reset for next entry
                            currentPNPDeviceID = null;
                            currentLineID = "";
                            currentBlockID = "";
                        }
                    }
                }
                
                // Update or add new scanner assignments
                foreach (var scanner in detectedScanners)
                {
                    if (!string.IsNullOrEmpty(scanner.PNPDeviceID) && scanner.PNPDeviceID != "N/A")
                    {
                        existingAssignments[scanner.PNPDeviceID] = new ScannerInfo
                        {
                            PNPDeviceID = scanner.PNPDeviceID,
                            LineID = scanner.LineID,
                            BlockID = scanner.BlockID
                        };
                    }
                }
                
                // Write updated assignments to file
                using (StreamWriter writer = new StreamWriter(savePath))
                {
                    writer.WriteLine("Scanner Assignments - COM Port Mode - Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    writer.WriteLine("=" + new string('=', 70));
                    writer.WriteLine();

                    int scannerNum = 1;
                    foreach (var assignment in existingAssignments.Values)
                    {
                        writer.WriteLine($"Scanner #{scannerNum}:");
                        writer.WriteLine($"  PNPDeviceID: {assignment.PNPDeviceID}");
                        writer.WriteLine($"  COM Port: {assignment.ComPort ?? "Auto-detect"}");
                        writer.WriteLine($"  Line ID: {assignment.LineID}");
                        writer.WriteLine($"  Block ID: {assignment.BlockID}");
                        writer.WriteLine($"  Baud Rate: {assignment.BaudRate}");
                        writer.WriteLine($"  Parity: {assignment.Parity}");
                        writer.WriteLine($"  Data Bits: {assignment.DataBits}");
                        writer.WriteLine($"  Stop Bits: {assignment.StopBits}");
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
