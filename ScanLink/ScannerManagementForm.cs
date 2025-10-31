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
        private Label titleLabel;
        private List<ScannerInfo> detectedScanners;

        public class ScannerInfo
        {
            public string SerialNumber { get; set; }
            public string PNPDeviceID { get; set; }
            public string LineID { get; set; }
            public string BlockID { get; set; }
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
            this.titleLabel = new Label();
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
            // ScannerManagementForm - Responsive and centered
            // 
            this.AutoScaleDimensions = new SizeF(96F, 96F); // Use DPI-aware scaling
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.ClientSize = new Size(900, 650); // Increased initial size
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.refreshButton);
            this.Controls.Add(this.scannerDataGridView);
            this.Controls.Add(this.titleLabel);
            this.MinimumSize = new Size(800, 500); // Set minimum size for usability
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

            // Position DataGridView - responsive with margins; reduce height to keep buttons visible
            int gridTop = topMargin + titleHeight + 10; // gap after title
            int bottomReserved = buttonHeight + bottomPadding + 10; // include padding and gap above buttons
            int gridBottom = clientHeight - bottomReserved;
            int gridLeft = horizontalMargin;
            int gridRight = clientWidth - horizontalMargin;

            // Ensure minimum grid size
            int gridWidth = Math.Max(400, gridRight - gridLeft);
            int gridHeight = Math.Max(200, gridBottom - gridTop);

            this.scannerDataGridView.Location = new Point(gridLeft, gridTop);
            this.scannerDataGridView.Size = new Size(gridWidth, gridHeight);

            // Position buttons at bottom with margins
            int buttonY = clientHeight - bottomPadding - buttonHeight;
            this.refreshButton.Location = new Point(horizontalMargin, buttonY);
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

            // Adjust FillWeight a bit based on width tiers to keep balance, no gaps
            bool isWideForm = this.ClientSize.Width > 1000;
            bool isNarrowForm = this.ClientSize.Width < 800;

            float serialW = isWideForm ? 12f : (isNarrowForm ? 18f : 15f);
            float pnpW    = isWideForm ? 45f : (isNarrowForm ? 30f : 35f);
            float lineW   = isWideForm ? 13f : (isNarrowForm ? 16f : 15f);
            float blockW  = isWideForm ? 13f : (isNarrowForm ? 16f : 15f);
            float statusW = isWideForm ? 17f : (isNarrowForm ? 20f : 20f);

            SetColumnFillWeight("SerialNumber", serialW);
            SetColumnFillWeight("PNPDeviceID", pnpW);
            SetColumnFillWeight("LineID", lineW);
            SetColumnFillWeight("BlockID", blockW);
            SetColumnFillWeight("Status", statusW);
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
            
            try
            {
                // First, load historical scanners from assignments file
                LoadHistoricalScanners();
                
                // Then run the scanner detection PowerShell script to get currently connected scanners (bin root)
                string scriptPath = Path.Combine(Application.StartupPath, "scanner_detection.ps1");
                
                if (!File.Exists(scriptPath))
                {
                    MessageBox.Show($"Scanner detection script not found at: {scriptPath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

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

                    if (process.ExitCode != 0)
                    {
                        MessageBox.Show($"Error running scanner detection: {error}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Parse the output to extract currently connected scanner information
                    ParseCurrentScanners(output);
                }
                
                // Update status for all scanners (connected vs not connected)
                UpdateScannerStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading scanners: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                        
                        // Add historical scanner (initially marked as not connected)
                        if (!string.IsNullOrEmpty(currentPNPDeviceID))
                        {
                            detectedScanners.Add(new ScannerInfo
                            {
                                SerialNumber = $"Scanner {detectedScanners.Count + 1}",
                                PNPDeviceID = currentPNPDeviceID,
                                LineID = currentLineID,
                                BlockID = currentBlockID,
                                Status = "Not Connected",
                                IsCurrentlyConnected = false
                            });
                            
                            System.Diagnostics.Debug.WriteLine($"Added historical scanner: {currentPNPDeviceID} - Line: {currentLineID}, Block: {currentBlockID}");
                        }
                        
                        // Reset for next entry
                        currentPNPDeviceID = null;
                        currentLineID = "";
                        currentBlockID = "";
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
            System.Diagnostics.Debug.WriteLine($"Parsing current scanners output: {output}");
            string[] lines = output.Split('\n');
            List<string> currentPNPDeviceIDs = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                
                // Look for scanner entries
                if (line.StartsWith("Scanner #"))
                {
                    // Look for PNPDeviceID in the next few lines
                    for (int j = i + 1; j < Math.Min(i + 10, lines.Length); j++)
                    {
                        string nextLine = lines[j].Trim();
                        if (nextLine.StartsWith("PNPDeviceID:"))
                        {
                            string pnpDeviceID = nextLine.Substring("PNPDeviceID:".Length).Trim();
                            currentPNPDeviceIDs.Add(pnpDeviceID);
                            System.Diagnostics.Debug.WriteLine($"Found currently connected scanner: {pnpDeviceID}");
                            break;
                        }
                    }
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"Total currently connected scanners found: {currentPNPDeviceIDs.Count}");

            // Update the detectedScanners list to mark which ones are currently connected
            foreach (var scanner in detectedScanners)
            {
                if (currentPNPDeviceIDs.Contains(scanner.PNPDeviceID))
                {
                    scanner.IsCurrentlyConnected = true;
                    scanner.Status = "Connected";
                    System.Diagnostics.Debug.WriteLine($"Scanner {scanner.PNPDeviceID} marked as CONNECTED");
                }
                else
                {
                    scanner.IsCurrentlyConnected = false;
                    scanner.Status = "Not Connected";
                    System.Diagnostics.Debug.WriteLine($"Scanner {scanner.PNPDeviceID} marked as NOT CONNECTED");
                }
            }

            // Add any new scanners that are currently connected but not in historical data
            foreach (string currentPNPDeviceID in currentPNPDeviceIDs)
            {
                if (!detectedScanners.Any(s => s.PNPDeviceID == currentPNPDeviceID))
                {
                    detectedScanners.Add(new ScannerInfo
                    {
                        SerialNumber = $"Scanner {detectedScanners.Count + 1}",
                        PNPDeviceID = currentPNPDeviceID,
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

            // Add columns
            DataGridViewTextBoxColumn serialColumn = new DataGridViewTextBoxColumn();
            serialColumn.HeaderText = "Serial Number";
            serialColumn.Name = "SerialNumber";
            serialColumn.FillWeight = 15; // 15%
            serialColumn.ReadOnly = true;
            scannerDataGridView.Columns.Add(serialColumn);

            DataGridViewTextBoxColumn pnpColumn = new DataGridViewTextBoxColumn();
            pnpColumn.HeaderText = "PNPDeviceID";
            pnpColumn.Name = "PNPDeviceID";
            pnpColumn.FillWeight = 35; // 35%
            pnpColumn.ReadOnly = true;
            scannerDataGridView.Columns.Add(pnpColumn);

            DataGridViewTextBoxColumn lineIdColumn = new DataGridViewTextBoxColumn();
            lineIdColumn.HeaderText = "Line ID";
            lineIdColumn.Name = "LineID";
            lineIdColumn.FillWeight = 15; // 15%
            lineIdColumn.ReadOnly = false;
            scannerDataGridView.Columns.Add(lineIdColumn);

            DataGridViewTextBoxColumn blockIdColumn = new DataGridViewTextBoxColumn();
            blockIdColumn.HeaderText = "Block ID";
            blockIdColumn.Name = "BlockID";
            blockIdColumn.FillWeight = 15; // 15%
            blockIdColumn.ReadOnly = false;
            scannerDataGridView.Columns.Add(blockIdColumn);

            DataGridViewTextBoxColumn statusColumn = new DataGridViewTextBoxColumn();
            statusColumn.HeaderText = "Status";
            statusColumn.Name = "Status";
            statusColumn.FillWeight = 20; // 20%
            statusColumn.ReadOnly = true;
            scannerDataGridView.Columns.Add(statusColumn);

            // Populate data
            scannerDataGridView.Rows.Clear();
            foreach (var scanner in detectedScanners)
            {
                scannerDataGridView.Rows.Add(scanner.SerialNumber, scanner.PNPDeviceID, scanner.LineID, scanner.BlockID, scanner.Status);
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
        }

        private void refreshButton_Click(object sender, EventArgs e)
        {
            LoadDetectedScanners();
            PopulateDataGridView();
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
                        // Note: Status is read-only and managed automatically, so we don't update it here
                    }
                }

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
                    writer.WriteLine("Scanner Assignments - Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    writer.WriteLine("=" + new string('=', 60));
                    writer.WriteLine();

                    int scannerNum = 1;
                    foreach (var assignment in existingAssignments.Values)
                    {
                        writer.WriteLine($"Scanner #{scannerNum}:");
                        writer.WriteLine($"  PNPDeviceID: {assignment.PNPDeviceID}");
                        writer.WriteLine($"  Line ID: {assignment.LineID}");
                        writer.WriteLine($"  Block ID: {assignment.BlockID}");
                        writer.WriteLine();
                        scannerNum++;
                    }
                }

                MessageBox.Show($"Scanner assignments saved to:\n{savePath}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving scanner assignments: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
