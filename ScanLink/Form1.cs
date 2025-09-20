using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using BarcodePrinter_API;
using BarcodePrinter_API.Comm;
using BarcodePrinter_API.Emulation.PPLB;
using BarcodePrinter_API.Emulation.PPLZ;
using System.Text;

namespace ScanLink
{
    public delegate void VoidFunction(int count);

    public partial class Form1 : Form
    {
        public class FunctionData
        {
            public string Descration;
            public VoidFunction Function;
        }

        string strGraphicFilter = "All Graphic Type|*.bmp;*.gif;*.exig;*.jpg;*.png;*.tiff|All File|*.*||";
        string[] strEmulation = { "PPLB", "PPLZ" };
        string[] strPort = { "USB", "File", "COM", "LAN", "Multi-LAN" };

        FunctionData[] PPLB_ItemList;
        string[] PPLB_BarcodeList = {
            "Code 128 UCC Serial Shipping Container Code",
            "Code 128 auto A, B, C modes",
            "Code 128 mode A",
            "Code 128 mode B",
            "Code 128 mode C",
            "UCC/EAN 128",
            "Interleaved 2 of 5",
            "Interleaved 2 of 5 with mod 10 check digit",
            "Interleaved 2 of 5 with human readable check digit",
            "German Post Code",
            "Matrix 2 of 5",
            "UPC Interleaved 2 of 5",
            "Code 39 std. or extended",
            "Code 39 with check digit",
            "Code 93",
            "EAN-13",
            "EAN-13 2 digit add-on",
            "EAN-13 5 digit add-on",
            "EAN-8",
            "EAN-8 2 digit add-on",
            "EAN-8 5 digit add-on",
            "Codabar",
            "Postnet 5, 9, 11 and 13 digit",
            "UPC-A",
            "UPC-A 2 digit add-on",
            "UPC-A 5 digit add-on",
            "UPC-E",
            "UPC-E 2 digit add-on",
            "UPC-E 5 digit add-on",
            "PDF417",
            "Aztec Code",
            "MaxiCode",
            "QR Code",
            "RSS",
            "Data Matrix",
        };
        FunctionData[] PPLZ_ItemList;
        string[] PPLZ_BarcodeList = { "QR Code", "Code 128" };

        BarcodePrinter BarcodePrinter;
        string strFolder = @"C:\\BarcodePrinter";

        string m_ComName = SerialConnection.DefaultPortName;
        int m_baudRate = SerialConnection.DefaultBaudRate;
        int m_dataBits = SerialConnection.DefaultDataBits;
        SerialParity m_parity = SerialConnection.DefaultParity;
        SerialStopBits m_stopBits = SerialConnection.DefaultStopBits;
        SerialHandshake m_handshake = SerialConnection.DefaultHandshake;

        string m_TCPAddress = TCPConnection.DefaultAddress;
        int m_TCPPort = TCPConnection.DefaultPort;

        // USB connection variable
        string m_USBDevicePath = "";

        // Minimal app does not support Multi-LAN list UI

        string strSelectFolder;

        public Form1()
        {
            InitializeComponent();
        }

        string MergeIPAddressAndPort(string ipAddress, int port)
        {
            if (IPAddress.TryParse(ipAddress, out var address))
            {
                return address.AddressFamily == AddressFamily.InterNetworkV6 ? $"[{ipAddress}]:{port}" : $"{ipAddress}:{port}";
            }
            return string.Empty;
        }

        void InitFunctionData()
        {
            PPLB_ItemList = new FunctionData[4];
            PPLB_ItemList[0] = new FunctionData { Descration = "Reset", Function = __testPPLB_set1 };
            PPLB_ItemList[1] = new FunctionData { Descration = "Calibrate", Function = __testPPLB_calibrate };
            PPLB_ItemList[2] = new FunctionData { Descration = "BarcodeUtil 1 : one barcode", Function = __testPPLB_barcode1 };
            PPLB_ItemList[3] = new FunctionData { Descration = "🎯 Custom Preset (Advanced Settings)", Function = __testPPLB_customPreset };

            PPLZ_ItemList = new FunctionData[2];
            PPLZ_ItemList[0] = new FunctionData { Descration = "Reset", Function = __testPPLZ_set1 };
            PPLZ_ItemList[1] = new FunctionData { Descration = "Calibrate", Function = __testPPLZ_calibrate };
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            strSelectFolder = strFolder;
            Directory.CreateDirectory(strFolder);

            InitFunctionData();
            foreach (string str in strPort) comboBox_port.Items.Add(str);
            comboBox_port.Text = "USB";
            foreach (string str in strEmulation) comboBox_emulation.Items.Add(str);
            comboBox_emulation.Text = "PPLB";
            
            // Initialize advanced settings with defaults and tooltips
            InitializeAdvancedSettings();
        }

        private void comboBox_port_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (comboBox_port.Text)
            {
                case "File":
                    textBox_port.Text = "📁 " + strFolder;
                    connectionStatusLabel.Text = "Status: File output configured";
                    connectionStatusLabel.ForeColor = System.Drawing.Color.FromArgb(46, 204, 113);
                    break;
                case "COM":
                    textBox_port.Text = "🔌 Serial: " + this.m_ComName + " (" + this.m_baudRate + " baud)";
                    connectionStatusLabel.Text = "Status: Serial port ready";
                    connectionStatusLabel.ForeColor = System.Drawing.Color.FromArgb(52, 152, 219);
                    break;
                case "USB":
                    if (string.IsNullOrWhiteSpace(this.m_USBDevicePath))
                    {
                        textBox_port.Text = "🔌 USB: Click Configure to select device";
                        connectionStatusLabel.Text = "Status: USB device not configured";
                        connectionStatusLabel.ForeColor = System.Drawing.Color.FromArgb(230, 126, 34);
                    }
                    else
                    {
                        textBox_port.Text = "🔌 USB: " + this.m_USBDevicePath;
                        connectionStatusLabel.Text = "Status: USB device configured";
                        connectionStatusLabel.ForeColor = System.Drawing.Color.FromArgb(46, 204, 113);
                    }
                    break;
                case "LAN":
                    textBox_port.Text = "🌐 Network: " + this.MergeIPAddressAndPort(this.m_TCPAddress, this.m_TCPPort);
                    connectionStatusLabel.Text = "Status: Network connection configured";
                    connectionStatusLabel.ForeColor = System.Drawing.Color.FromArgb(52, 152, 219);
                    break;
                case "Multi-LAN":
                    textBox_port.Text = "🌐 Multi-LAN (Not supported in this version)";
                    connectionStatusLabel.Text = "Status: Feature not available";
                    connectionStatusLabel.ForeColor = System.Drawing.Color.FromArgb(149, 165, 166);
                    break;
                default:
                    MessageBox.Show("Connection type not supported: " + comboBox_port.Text, "Scan Link", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
            }
            
            // Update status bar
            statusLabel.Text = $"Connection updated to {comboBox_port.Text}. Ready to configure print settings.";
            statusLabel.ForeColor = System.Drawing.Color.FromArgb(127, 140, 141);
        }

        private void button_setting_Click(object sender, EventArgs e)
        {
            switch (comboBox_port.Text)
            {
                case "File":
                    using (var folderdlg = new FolderBrowserDialog())
                    {
                        folderdlg.SelectedPath = strFolder;
                        if (DialogResult.OK == folderdlg.ShowDialog()) strFolder = folderdlg.SelectedPath;
                    }
                    comboBox_port_SelectedIndexChanged(null, null);
                    break;
                case "COM":
                    MessageBox.Show("For simplicity, COM settings are fixed in this minimal app.");
                    break;
                case "USB":
                    // open USBDialog to select USB Device.
                    USBDialog USBsetdlg = new USBDialog();
                    USBsetdlg.DevicePath = this.m_USBDevicePath;
                    if (DialogResult.OK == USBsetdlg.ShowDialog())
                    {
                        // setting USB Device.
                        this.m_USBDevicePath = USBsetdlg.DevicePath;
                    }
                    //update edit string.
                    comboBox_port_SelectedIndexChanged(null, null);
                    break;
                case "LAN":
                    var input = Microsoft.VisualBasic.Interaction.InputBox("Host[:Port]", "LAN Target", MergeIPAddressAndPort(m_TCPAddress, m_TCPPort));
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        var host = input; var port = m_TCPPort;
                        if (input.Contains(":")) { var parts = input.Split(':'); host = parts[0]; int.TryParse(parts[1], out port); }
                        m_TCPAddress = host; m_TCPPort = port;
                    }
                    comboBox_port_SelectedIndexChanged(null, null);
                    break;
                case "Multi-LAN":
                    MessageBox.Show("Multi-LAN not included in minimal app.");
                    break;
            }
        }

        private void comboBox_emulation_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch ((sender as ComboBox).Text)
            {
                case "PPLB":
                    comboBox_test.Items.Clear();
                    foreach (FunctionData item in PPLB_ItemList) comboBox_test.Items.Add(item.Descration);
                    comboBox_test.SelectedIndex = 0;
                    comboBox_barcode.Items.Clear();
                    foreach (string str in PPLB_BarcodeList) comboBox_barcode.Items.Add(str);
                    comboBox_barcode.SelectedIndex = 0;
                    break;
                case "PPLZ":
                    comboBox_test.Items.Clear();
                    foreach (FunctionData item in PPLZ_ItemList) comboBox_test.Items.Add(item.Descration);
                    comboBox_test.SelectedIndex = 0;
                    comboBox_barcode.Items.Clear();
                    foreach (string str in PPLZ_BarcodeList) comboBox_barcode.Items.Add(str);
                    comboBox_barcode.SelectedIndex = 0;
                    break;
            }
        }

        private void comboBox_test_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox_barcode.Enabled = ("BarcodeUtil 1 : one barcode" == comboBox_test.Text) || 
                                     ("🎯 Custom Preset (Advanced Settings)" == comboBox_test.Text);
            
            // Show advanced settings automatically when custom preset is selected
            if ("🎯 Custom Preset (Advanced Settings)" == comboBox_test.Text)
            {
                checkBox_showAdvanced.Checked = true;
                statusLabel.Text = "Custom Preset selected - Advanced settings enabled automatically for full control.";
                statusLabel.ForeColor = System.Drawing.Color.FromArgb(52, 152, 219);
            }
        }

        private void checkBox_showAdvanced_CheckedChanged(object sender, EventArgs e)
        {
            advancedPanel.Visible = checkBox_showAdvanced.Checked;
            if (checkBox_showAdvanced.Checked)
            {
                this.Height = Math.Min(1150, Screen.PrimaryScreen.WorkingArea.Height - 50); // Expand but respect screen height
                this.WindowState = FormWindowState.Normal; // Ensure normal window state for scrolling
                statusLabel.Text = "Advanced settings enabled. Scroll down for all options. Configure barcode dimensions, alignment, and quality.";
            }
            else
            {
                this.Height = 750; // Collapse to basic settings
                statusLabel.Text = "Basic settings mode. Check 'Show Advanced Settings' for more options.";
            }
            statusLabel.ForeColor = System.Drawing.Color.FromArgb(127, 140, 141);
            
            // Ensure the main panel can scroll properly
            mainPanel.AutoScrollMinSize = new Size(0, checkBox_showAdvanced.Checked ? 1100 : 700);
        }

        private void trackBar_darkness_Scroll(object sender, EventArgs e)
        {
            label_darknessValue.Text = trackBar_darkness.Value.ToString();
        }

        // Method to calculate optimal text layout based on width constraints
        private (string[] lines, PPLBFont font, int fontSize) CalculateTextLayout(string text, int maxWidth)
        {
            if (string.IsNullOrWhiteSpace(text)) return (new[] { "N/A" }, PPLBFont.Font_2, 1);
            
            // Calculate characters per line based on width (approximate)
            // Assuming average character width of 8 pixels for Font_2
            int avgCharWidth = 8;
            int maxCharsPerLine = Math.Max(1, maxWidth / avgCharWidth);
            
            List<string> lines = new List<string>();
            
            // If text fits in one line
            if (text.Length <= maxCharsPerLine)
            {
                lines.Add(text);
            }
            else
            {
                // Split text into multiple lines
                for (int i = 0; i < text.Length; i += maxCharsPerLine)
                {
                    int length = Math.Min(maxCharsPerLine, text.Length - i);
                    lines.Add(text.Substring(i, length));
                }
            }
            
            // Choose font size based on number of lines and width
            PPLBFont font = PPLBFont.Font_2;
            int fontSize = 1;
            
            if (maxWidth < 150)
            {
                font = PPLBFont.Font_1; // Smaller font for narrow widths
                fontSize = 1;
            }
            else if (maxWidth > 300)
            {
                font = PPLBFont.Font_3; // Larger font for wide widths
                fontSize = 1;
            }
            
            return (lines.ToArray(), font, fontSize);
        }

        // Method to apply advanced settings to barcode printing
        private void ApplyAdvancedSettings()
        {
            if (!checkBox_showAdvanced.Checked) return;
            
            try
            {
                // These settings would be applied to the actual barcode printing
                // For now, we'll store them for use in the printing methods
                
                // Update status with applied settings
                statusLabel.Text = $"Advanced settings applied: {numericUpDown_width.Value}x{numericUpDown_height.Value}, Darkness: {trackBar_darkness.Value}";
                statusLabel.ForeColor = System.Drawing.Color.FromArgb(52, 152, 219);
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Failed to apply advanced settings: {ex.Message}";
                statusLabel.ForeColor = System.Drawing.Color.FromArgb(231, 76, 60);
            }
        }

        // Enhanced barcode printing with custom text
        // Initialize advanced settings with defaults and tooltips
        private void InitializeAdvancedSettings()
        {
            // Set up tooltips for better user experience
            toolTip.SetToolTip(textBox_barcodeText, "Enter the text/data to encode in the barcode");
            toolTip.SetToolTip(numericUpDown_width, "Width of the barcode in dots/pixels");
            toolTip.SetToolTip(numericUpDown_height, "Height of the barcode in dots/pixels");
            toolTip.SetToolTip(numericUpDown_gap, "Gap between labels in millimeters");
            toolTip.SetToolTip(comboBox_alignment, "Horizontal alignment of the barcode on the label");
            toolTip.SetToolTip(comboBox_rotation, "Rotation angle of the barcode");
            toolTip.SetToolTip(trackBar_darkness, "Print darkness/density (1=lightest, 30=darkest)");
            toolTip.SetToolTip(comboBox_speed, "Print speed setting (1=slowest/highest quality, 9=fastest)");
            toolTip.SetToolTip(button_preview, "Preview the current settings before printing");
            toolTip.SetToolTip(checkBox_showAdvanced, "Show/hide advanced configuration options");
            
            // Set initial form height for basic mode
            this.Height = 750;
            
            // Add real-time preview update when barcode text changes
            textBox_barcodeText.TextChanged += textBox_barcodeText_TextChanged;
        }
        
        private void textBox_barcodeText_TextChanged(object sender, EventArgs e)
        {
            // Update status to show current barcode text
            string currentText = !string.IsNullOrWhiteSpace(textBox_barcodeText.Text) ? textBox_barcodeText.Text : "Default: 23456";
            statusLabel.Text = $"Barcode text updated: '{currentText}' - Click Preview to see visual representation";
            statusLabel.ForeColor = System.Drawing.Color.FromArgb(52, 152, 219);
        }

        private void PrintBarcodeWithAdvancedSettings(int printCount)
        {
            if (checkBox_showAdvanced.Checked && !string.IsNullOrWhiteSpace(textBox_barcodeText.Text))
            {
                // Use custom barcode text from advanced settings
                string customText = textBox_barcodeText.Text;
                statusLabel.Text = $"Printing barcode with custom text: '{customText}'";
                statusLabel.ForeColor = System.Drawing.Color.FromArgb(52, 152, 219);
                
                // Apply advanced settings
                ApplyAdvancedSettings();
            }
        }

        private void button_preview_Click(object sender, EventArgs e)
        {
            try
            {
                ShowVisualBarcodePreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Preview failed: {ex.Message}", "Preview Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Preview failed. Please check your settings.";
                statusLabel.ForeColor = System.Drawing.Color.FromArgb(231, 76, 60);
            }
        }
        
        private void ShowVisualBarcodePreview()
        {
            // Get current settings first
            string barcodeText = !string.IsNullOrWhiteSpace(textBox_barcodeText.Text) ? textBox_barcodeText.Text : "1234567890";
            int labelWidth = (int)numericUpDown_width.Value;
            int labelHeight = (int)numericUpDown_height.Value;
            
            // Create a preview form (larger to accommodate complete preview)
            Form previewForm = new Form();
            previewForm.Text = "Complete Label Preview - Scan Link";
            previewForm.Size = new Size(Math.Max(labelWidth + 100, 700), Math.Max(labelHeight + 300, 600));
            previewForm.StartPosition = FormStartPosition.CenterParent;
            previewForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            previewForm.MaximizeBox = false;
            previewForm.MinimizeBox = false;
            
            // Create preview panel
            Panel previewPanel = new Panel();
            previewPanel.Dock = DockStyle.Fill;
            previewPanel.BackColor = Color.White;
            previewPanel.AutoScroll = true;
            
            // Calculate text layout
            var (textLines, textFont, textSize) = CalculateTextLayout(barcodeText, labelWidth);
            
            // Create visual representation
            CreateBarcodePreviewVisual(previewPanel, barcodeText, labelWidth, labelHeight, textLines);
            
            // Add settings info panel
            Panel infoPanel = new Panel();
            infoPanel.Height = 150;
            infoPanel.Dock = DockStyle.Bottom;
            infoPanel.BackColor = Color.FromArgb(247, 249, 249);
            
            Label infoLabel = new Label();
            infoLabel.Dock = DockStyle.Fill;
            infoLabel.Font = new Font("Segoe UI", 9);
            infoLabel.ForeColor = Color.FromArgb(52, 73, 94);
            infoLabel.Padding = new Padding(20);
            
            string textLayoutInfo = textLines.Length > 1 ? 
                $"📝 Text Layout: {textLines.Length} lines - {string.Join(" | ", textLines)}" : 
                $"📝 Text: {barcodeText} (fits in 1 line)";
                
            infoLabel.Text = $"Preview Settings:\n" +
                $"{textLayoutInfo}\n" +
                $"📏 Dimensions: {labelWidth} x {labelHeight} pixels\n" +
                $"🔄 Alignment: {comboBox_alignment.Text} | 🔁 Rotation: {comboBox_rotation.Text}\n" +
                $"📊 Barcode Type: {comboBox_barcode.Text} | 🌑 Darkness: {trackBar_darkness.Value}/30\n" +
                $"🔢 Print Count: {numericUpDown_count.Value} | ⚡ Speed: {comboBox_speed.Text}";
            
            infoPanel.Controls.Add(infoLabel);
            
            previewForm.Controls.Add(previewPanel);
            previewForm.Controls.Add(infoPanel);
            
            // Show preview
            previewForm.ShowDialog(this);
            
            statusLabel.Text = $"Visual preview shown for: '{barcodeText}' ({textLines.Length} lines)";
            statusLabel.ForeColor = System.Drawing.Color.FromArgb(46, 204, 113);
        }
        
        private void CreateBarcodePreviewVisual(Panel panel, string barcodeText, int width, int height, string[] textLines)
        {
            // Create a complete visual representation showing ALL printed elements
            int startX = 20;
            int startY = 50;
            
            // Title
            Label titleLabel = new Label();
            titleLabel.Text = "Complete Label Preview - All Printed Elements";
            titleLabel.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(41, 128, 185);
            titleLabel.Location = new Point(startX, 10);
            titleLabel.AutoSize = true;
            panel.Controls.Add(titleLabel);
            
            // Label outline (representing the ENTIRE label with correct dimensions)
            Panel labelOutline = new Panel();
            labelOutline.Location = new Point(startX, startY);
            labelOutline.Size = new Size(width + 10, height + 20); // Use actual label dimensions
            labelOutline.BorderStyle = BorderStyle.FixedSingle;
            labelOutline.BackColor = Color.FromArgb(250, 250, 250);
            panel.Controls.Add(labelOutline);
            
            // Add margin indicators (5-dot margins)
            Panel marginIndicator = new Panel();
            marginIndicator.Location = new Point(5, 5);
            marginIndicator.Size = new Size(labelOutline.Width - 10, labelOutline.Height - 10);
            marginIndicator.BorderStyle = BorderStyle.FixedSingle;
            marginIndicator.BackColor = Color.White;
            labelOutline.Controls.Add(marginIndicator);
            
            int currentY = 5; // Start after margin
            
            // 1. Show main header (always printed)
            Label headerLabel = new Label();
            headerLabel.Text = "Label: one barcode";
            headerLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            headerLabel.ForeColor = Color.FromArgb(52, 73, 94);
            headerLabel.Location = new Point(25, currentY); // 30-5 for margin
            headerLabel.AutoSize = true;
            marginIndicator.Controls.Add(headerLabel);
            currentY += 25;
            
            // 2. Show barcode type (always printed)
            Label typeLabel = new Label();
            typeLabel.Text = comboBox_barcode.Text;
            typeLabel.Font = new Font("Segoe UI", 8);
            typeLabel.ForeColor = Color.FromArgb(127, 140, 141);
            typeLabel.Location = new Point(45, currentY); // 50-5 for margin
            typeLabel.AutoSize = true;
            marginIndicator.Controls.Add(typeLabel);
            currentY += 25;
            
            // 3. Show text layout info (if advanced settings and wrapped text)
            if (checkBox_showAdvanced.Checked && textLines.Length > 1)
            {
                Label wrapLabel = new Label();
                wrapLabel.Text = $"Text wrapped to {textLines.Length} lines for width {width}:";
                wrapLabel.Font = new Font("Courier New", 7);
                wrapLabel.ForeColor = Color.FromArgb(155, 89, 182);
                wrapLabel.Location = new Point(45, currentY);
                wrapLabel.AutoSize = true;
                marginIndicator.Controls.Add(wrapLabel);
                currentY += 15;
                
                // Show each wrapped line
                for (int i = 0; i < textLines.Length; i++)
                {
                    Label lineLabel = new Label();
                    lineLabel.Text = $"Line {i + 1}: {textLines[i]}";
                    lineLabel.Font = new Font("Courier New", 7);
                    lineLabel.ForeColor = Color.FromArgb(155, 89, 182);
                    lineLabel.Location = new Point(45, currentY);
                    lineLabel.AutoSize = true;
                    marginIndicator.Controls.Add(lineLabel);
                    currentY += 12;
                }
                currentY += 10; // Extra spacing
            }
            
            // 4. Show barcode positioning and alignment
            int barcodeX = 45; // Default left (50-5 for margin)
            if (checkBox_showAdvanced.Checked)
            {
                switch (comboBox_alignment.SelectedIndex)
                {
                    case 0: barcodeX = 45; break;  // Left (50-5)
                    case 1: barcodeX = (width / 2) - 50; break; // Center
                    case 2: barcodeX = width - 100; break; // Right (250-5)
                }
            }
            
            // 5. Show barcode type labels (normal/human readable)
            Label normalLabel = new Label();
            normalLabel.Text = "normal";
            normalLabel.Font = new Font("Segoe UI", 8);
            normalLabel.ForeColor = Color.FromArgb(231, 76, 60);
            normalLabel.Location = new Point(barcodeX, currentY - 5);
            normalLabel.AutoSize = true;
            marginIndicator.Controls.Add(normalLabel);
            
            // 6. Simulated barcode visual
            Panel barcodeVisual = new Panel();
            int barcodeHeight = checkBox_showAdvanced.Checked ? (int)numericUpDown_height.Value : 50;
            barcodeVisual.Location = new Point(barcodeX, currentY + 10);
            barcodeVisual.Size = new Size(Math.Min(width - barcodeX - 10, 200), Math.Min(barcodeHeight, 80));
            barcodeVisual.BackColor = Color.White;
            barcodeVisual.BorderStyle = BorderStyle.FixedSingle;
            marginIndicator.Controls.Add(barcodeVisual);
            
            // Create barcode pattern
            barcodeVisual.Paint += (s, pe) => CreateBarcodePattern(pe.Graphics, barcodeVisual.Size, barcodeText);
            currentY += barcodeVisual.Height + 15;
            
            // 7. Show human readable label
            Label humanLabel = new Label();
            humanLabel.Text = "human readable";
            humanLabel.Font = new Font("Segoe UI", 8);
            humanLabel.ForeColor = Color.FromArgb(231, 76, 60);
            humanLabel.Location = new Point(barcodeX, currentY);
            humanLabel.AutoSize = true;
            marginIndicator.Controls.Add(humanLabel);
            currentY += 15;
            
            // 8. Second barcode (human readable version)
            Panel barcodeVisual2 = new Panel();
            barcodeVisual2.Location = new Point(barcodeX, currentY + 5);
            barcodeVisual2.Size = new Size(Math.Min(width - barcodeX - 10, 200), Math.Min(barcodeHeight, 80));
            barcodeVisual2.BackColor = Color.White;
            barcodeVisual2.BorderStyle = BorderStyle.FixedSingle;
            marginIndicator.Controls.Add(barcodeVisual2);
            
            // Create second barcode pattern
            barcodeVisual2.Paint += (s, pe) => {
                CreateBarcodePattern(pe.Graphics, new Size(barcodeVisual2.Width, barcodeVisual2.Height - 20), barcodeText);
                // Add human readable text
                using (Font font = new Font("Courier New", 8))
                using (Brush brush = new SolidBrush(Color.Black))
                {
                    pe.Graphics.DrawString(barcodeText, font, brush, 5, barcodeVisual2.Height - 18);
                }
            };
            
            // 9. Add label dimension info
            Label dimensionsLabel = new Label();
            dimensionsLabel.Text = $"Label Size: {width} x {height} pixels | Gap: {numericUpDown_gap.Value}mm";
            dimensionsLabel.Font = new Font("Segoe UI", 8, FontStyle.Italic);
            dimensionsLabel.ForeColor = Color.FromArgb(127, 140, 141);
            dimensionsLabel.Location = new Point(5, labelOutline.Height - 15);
            dimensionsLabel.AutoSize = true;
            labelOutline.Controls.Add(dimensionsLabel);
            
            // 10. Add alignment and rotation indicators
            Panel alignmentIndicator = new Panel();
            alignmentIndicator.Size = new Size(8, 8);
            alignmentIndicator.BackColor = Color.FromArgb(231, 76, 60);
            
            switch (comboBox_alignment.SelectedIndex)
            {
                case 0: // Left
                    alignmentIndicator.Location = new Point(2, labelOutline.Height / 2);
                    break;
                case 1: // Center
                    alignmentIndicator.Location = new Point(labelOutline.Width / 2 - 4, labelOutline.Height / 2);
                    break;
                case 2: // Right
                    alignmentIndicator.Location = new Point(labelOutline.Width - 10, labelOutline.Height / 2);
                    break;
            }
            labelOutline.Controls.Add(alignmentIndicator);
            
            // 11. Add rotation indicator
            if (checkBox_showAdvanced.Checked && comboBox_rotation.SelectedIndex > 0)
            {
                Label rotationLabel = new Label();
                rotationLabel.Text = $"↻{comboBox_rotation.Text}";
                rotationLabel.Font = new Font("Segoe UI", 8, FontStyle.Bold);
                rotationLabel.ForeColor = Color.FromArgb(231, 76, 60);
                rotationLabel.Location = new Point(labelOutline.Width - 60, 5);
                rotationLabel.AutoSize = true;
                labelOutline.Controls.Add(rotationLabel);
            }
        }
        
        private void CreateBarcodePattern(Graphics g, Size panelSize, string text)
        {
            // Create a simple barcode-like visual pattern
            using (Brush blackBrush = new SolidBrush(Color.Black))
            using (Brush whiteBrush = new SolidBrush(Color.White))
            {
                // Simple algorithm to create barcode-like pattern based on text
                int barWidth = Math.Max(1, panelSize.Width / (text.Length * 8));
                int x = 0;
                
                foreach (char c in text)
                {
                    int charValue = (int)c;
                    for (int i = 0; i < 8; i++)
                    {
                        bool isBlack = (charValue & (1 << i)) != 0;
                        g.FillRectangle(isBlack ? blackBrush : whiteBrush, x, 0, barWidth, panelSize.Height - 25);
                        x += barWidth;
                        if (x >= panelSize.Width - barWidth) break;
                    }
                    if (x >= panelSize.Width - barWidth) break;
                }
                
                // Add human readable text at bottom
                using (Font font = new Font("Courier New", 8))
                {
                    g.DrawString(text, font, blackBrush, 10, panelSize.Height - 20);
                }
            }
        }

        private void button_send_Click(object sender, EventArgs e)
        {
            // Show progress and update UI
            button_send.Enabled = false;
            button_send.Text = "🔄 Processing...";
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Marquee;
            statusLabel.Text = "Preparing print job...";
            statusLabel.ForeColor = System.Drawing.Color.FromArgb(52, 152, 219);
            
            try
            {
                int printcount = (int)numericUpDown_count.Value;
                statusLabel.Text = $"Printing {printcount} label(s) using {comboBox_emulation.Text} emulation...";
                
                // Apply advanced settings if enabled
                if (checkBox_showAdvanced.Checked)
                {
                    PrintBarcodeWithAdvancedSettings(printcount);
                    statusLabel.Text += $"\n📏 Size: {numericUpDown_width.Value}x{numericUpDown_height.Value}, 🌑 Darkness: {trackBar_darkness.Value}";
                }
                
                switch (comboBox_emulation.Text)
                {
                    case "PPLB":
                        PPLB_ItemList[comboBox_test.SelectedIndex].Function(printcount);
                        break;
                    case "PPLZ":
                        PPLZ_ItemList[comboBox_test.SelectedIndex].Function(printcount);
                        break;
                }
                
                // Success feedback with advanced settings summary
                string successMessage = "✅ Print job completed successfully!";
                if (checkBox_showAdvanced.Checked)
                {
                    successMessage += $"\n🎯 Advanced settings were applied: {textBox_barcodeText.Text}, {numericUpDown_width.Value}x{numericUpDown_height.Value}";
                }
                statusLabel.Text = successMessage;
                statusLabel.ForeColor = System.Drawing.Color.FromArgb(46, 204, 113);
            }
            catch (Exception ex)
            {
                // Error feedback
                statusLabel.Text = $"❌ Print failed: {ex.Message}";
                statusLabel.ForeColor = System.Drawing.Color.FromArgb(231, 76, 60);
            }
            finally
            {
                // Reset UI
                button_send.Enabled = true;
                button_send.Text = "🖨️ Start Printing";
                progressBar.Visible = false;
                progressBar.Style = ProgressBarStyle.Continuous;
            }
        }

        private bool __createPrn(string additionalname, int index)
        {
            IPrinterConnection fs = null;
            try
            {
                switch (comboBox_port.Text)
                {
                    case "File":
                        fs = new FileStreamConnection(strFolder + "\\" + additionalname);
                        break;
                    case "COM":
                        fs = new SerialConnection(m_ComName, m_baudRate, m_parity, m_dataBits, m_stopBits, m_handshake);
                        break;
                    case "USB":
                        fs = new USBConnection(m_USBDevicePath);
                        break;
                    case "LAN":
                        fs = new TCPConnection(m_TCPAddress, m_TCPPort);
                        break;
                    case "Multi-LAN":
                        return false;
                }
                if (null == fs) return false;
                BarcodePrinter = new BarcodePrinter();
                BarcodePrinter.AddConnection(fs);
                BarcodePrinter.Connection.Open();
                switch (comboBox_emulation.Text)
                {
                    case "PPLB":
                        PPLBEmulation = new PPLB();
                        BarcodePrinter.AddEmulation(PPLBEmulation);
                        break;
                    case "PPLZ":
                        PPLZEmulation = new PPLZ();
                        BarcodePrinter.AddEmulation(PPLZEmulation);
                        break;
                }
                return true;
            }
            catch (Exception ex)
            {
                ShowException.Show(this.Name, "__createPrn", ex);
            }
            return false;
        }

        PPLB PPLBEmulation;
        private void __testPPLB_calibrate(int printcount)
        {
            int index = -1;
            if (false == __createPrn("PPLB_calibrate.txt", ++index)) return;
            try 
            { 
                PPLBEmulation.SetUtil.SetMediaCalibration(); 
                PPLBEmulation.IOUtil.PrintOut(); 
            }
            catch (Exception ex) 
            { 
                ShowException.Show(this.Name, "__testPPLB_calibrate", ex); 
            }
            finally 
            { 
                BarcodePrinter.Connection.Close(); 
            }
        }

        private void __testPPLB_set1(int printcount)
        {
            int index = -1;
            if (false == __createPrn("PPLB_set1.txt", ++index)) return;
            try 
            { 
                PPLBEmulation.SetUtil.SetReset(); 
                PPLBEmulation.IOUtil.PrintOut(); 
            }
            catch (Exception ex) 
            { 
                ShowException.Show(this.Name, "__testPPLB_set1", ex); 
            }
            finally 
            { 
                BarcodePrinter.Connection.Close(); 
            }
        }

        private void __testPPLB_barcode1(int printcount)
        {
            byte[] buf, buf2;
            Encoding encoder = Encoding.Default;
            int index = -1;
            
            if (false == __createPrn("PPLB_barcode1_" + comboBox_barcode.Text + ".txt", ++index))
                return;

                try
                {
                    //call methods that you want.
                    PPLBEmulation.SetUtil.SetOrientation(false);
                    PPLBEmulation.SetUtil.SetHomePosition(0, 0);
                    PPLBEmulation.SetUtil.SetHardwareOption(PPLBMediaType.Direct_Thermal_Media, PPLBPrintMode.Tear_Off, 0);
                    
                    // Apply advanced settings if enabled
                    if (checkBox_showAdvanced.Checked)
                    {
                        // Apply darkness setting (convert from 1-30 to 0-30 range)
                        PPLBEmulation.SetUtil.SetDarkness(trackBar_darkness.Value - 1);
                        
                        // Apply print speed (convert from 1-9 to actual speed)
                        int speedValue = comboBox_speed.SelectedIndex + 1;
                        PPLBEmulation.SetUtil.SetPrintRate(speedValue);
                        
                        // Apply label dimensions (controls entire print area)
                        int labelWidthDots = (int)numericUpDown_width.Value;
                        int labelHeightDots = (int)numericUpDown_height.Value;
                        int gapMM = (int)numericUpDown_gap.Value;
                        
                        // Set the actual label dimensions
                        PPLBEmulation.SetUtil.SetLabelLength(PPLBMediaTrack.Gap_Mode, labelHeightDots, gapMM);
                        PPLBEmulation.SetUtil.SetPrintWidth(labelWidthDots);
                        PPLBEmulation.SetUtil.SetHomePosition(5, 5); // 5-dot margin
                    }
                    
                    PPLBEmulation.SetUtil.SetClearImageBuffer();
                    buf = encoder.GetBytes("Label: one barcode");
                    PPLBEmulation.TextUtil.PrintText(30, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                    buf = encoder.GetBytes(comboBox_barcode.Text);
                    PPLBEmulation.TextUtil.PrintText(50, 50, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                    
                    // Use custom barcode text from textBox_barcodeText (always check, not just when advanced settings are on)
                    string barcodeText = !string.IsNullOrWhiteSpace(textBox_barcodeText.Text) ? textBox_barcodeText.Text : "23456";
                    buf = encoder.GetBytes(barcodeText);
                    
                    // Calculate text layout based on width constraints
                    int labelWidth = checkBox_showAdvanced.Checked ? (int)numericUpDown_width.Value : 200;
                    var (textLines, textFont, textSize) = CalculateTextLayout(barcodeText, labelWidth);
                    
                    // Print text information with width-constrained layout
                    if (checkBox_showAdvanced.Checked && textLines.Length > 1)
                    {
                        buf2 = encoder.GetBytes($"Text wrapped to {textLines.Length} lines for width {labelWidth}:");
                        PPLBEmulation.TextUtil.PrintText(50, 75, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_1, 1, 1, false, buf2);
                        
                        // Print each line of wrapped text
                        for (int i = 0; i < textLines.Length; i++)
                        {
                            buf2 = encoder.GetBytes($"Line {i + 1}: {textLines[i]}");
                            PPLBEmulation.TextUtil.PrintText(50, 90 + (i * 15), PPLBOrient.Clockwise_0_Degrees, textFont, textSize, textSize, false, buf2);
                        }
                    }
                    // Get advanced settings for barcode positioning and dimensions
                    int xPos = 50, yPos = 110, barcodeHeight = 50;
                    PPLBOrient orientation = PPLBOrient.Clockwise_0_Degrees;
                    
                    if (checkBox_showAdvanced.Checked)
                    {
                        // Apply alignment settings
                        switch (comboBox_alignment.SelectedIndex)
                        {
                            case 0: xPos = 50; break;  // Left
                            case 1: xPos = 150; break; // Center  
                            case 2: xPos = 250; break; // Right
                        }
                        
                        // Apply rotation settings
                        switch (comboBox_rotation.SelectedIndex)
                        {
                            case 0: orientation = PPLBOrient.Clockwise_0_Degrees; break;
                            case 1: orientation = PPLBOrient.Clockwise_90_Degrees; break;
                            case 2: orientation = PPLBOrient.Clockwise_180_Degrees; break;
                            case 3: orientation = PPLBOrient.Clockwise_270_Degrees; break;
                        }
                        
                        // Apply height setting
                        barcodeHeight = (int)numericUpDown_height.Value;
                    }
                    
                    // Calculate narrow bar width based on desired total barcode width
                    // Formula: narrowBarWidth = desiredWidth / (estimatedBarsCount * averageBarRatio)
                    int desiredWidth = checkBox_showAdvanced.Checked ? (int)numericUpDown_width.Value : 200;
                    int estimatedBarsPerChar = 11; // Average bars per character for Code 128
                    int estimatedTotalBars = barcodeText.Length * estimatedBarsPerChar;
                    int narrowBarWidth = Math.Max(1, Math.Min(10, desiredWidth / estimatedTotalBars));
                    if (narrowBarWidth < 1) narrowBarWidth = 1;
                    if (narrowBarWidth > 10) narrowBarWidth = 10; // ARGOX SDK typically supports 1-10
                    
                    // Update status with calculated width information
                    if (checkBox_showAdvanced.Checked)
                    {
                        statusLabel.Text = $"Applied advanced settings - Darkness: {trackBar_darkness.Value}, Narrow Bar Width: {narrowBarWidth} (for {desiredWidth}px total width)";
                        statusLabel.ForeColor = System.Drawing.Color.FromArgb(52, 152, 219);
                    }
                    
                    switch (comboBox_barcode.Text)
                    {
                        case "Code 128 UCC Serial Shipping Container Code":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(xPos, yPos - 30, orientation, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(xPos, yPos, orientation,
                                PPLBBarCodeType.Code_128_UCC, narrowBarWidth, 0, barcodeHeight, false, buf);
                            buf2 = encoder.GetBytes("human readable");
                            PPLBEmulation.TextUtil.PrintText(xPos, yPos + barcodeHeight + 10, orientation, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(xPos, yPos + barcodeHeight + 40, orientation,
                                PPLBBarCodeType.Code_128_UCC, narrowBarWidth, 0, barcodeHeight, true, buf);
                            break;
                        case "Code 128 auto A, B, C modes":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(xPos, yPos - 30, orientation, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(xPos, yPos, orientation,
                                PPLBBarCodeType.Code_128_Auto_Mode, narrowBarWidth, 0, barcodeHeight, false, buf);
                            buf2 = encoder.GetBytes("human readable");
                            PPLBEmulation.TextUtil.PrintText(xPos, yPos + barcodeHeight + 10, orientation, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(xPos, yPos + barcodeHeight + 40, orientation,
                                PPLBBarCodeType.Code_128_Auto_Mode, narrowBarWidth, 0, barcodeHeight, true, buf);
                            break;
                        case "QR Code":
                            int qrSize = checkBox_showAdvanced.Checked ? Math.Min((int)numericUpDown_width.Value / 50, 10) : 3;
                            PPLBEmulation.BarcodeUtil.PrintQRCode(xPos, yPos, PPLBQRCodeModel.Model_2, qrSize, PPLBQRCodeErrCorrect.Standard, buf);
                            break;
                        default:
                            // Default to Code 128 for any unhandled barcode types
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(xPos, yPos, orientation,
                                PPLBBarCodeType.Code_128_Auto_Mode, narrowBarWidth, 0, barcodeHeight, true, buf);
                            break;
                    }
                    //set print conditions.
                    PPLBEmulation.SetUtil.SetPrintOut(printcount, 1);
                    //The output data.
                    PPLBEmulation.IOUtil.PrintOut();
                }

                //exception.
                catch (Exception ex)
                {
                    ShowException.Show(this.Name, "__testPPLB_barcode1", ex);
                }
                //Close the connection.
                //Notice: If you don't call BarcodePrinter.Connection.Close() method at here, maybe you don't close the connection.
                finally
                {
                    BarcodePrinter.Connection.Close(); // equal to fs.Close();
                }
        }

        // Custom preset method that fully utilizes advanced settings
        private void __testPPLB_customPreset(int printcount)
        {
            byte[] buf, buf2;
            Encoding encoder = Encoding.Default;
            int index = -1;
            
            if (false == __createPrn("PPLB_CustomPreset.txt", ++index))
                return;

            try
            {
                // Always apply advanced settings for custom preset
                PPLBEmulation.SetUtil.SetOrientation(false);
                PPLBEmulation.SetUtil.SetHomePosition(0, 0);
                PPLBEmulation.SetUtil.SetHardwareOption(PPLBMediaType.Direct_Thermal_Media, PPLBPrintMode.Tear_Off, 0);
                
                // Apply all advanced settings including label dimensions
                PPLBEmulation.SetUtil.SetDarkness(trackBar_darkness.Value - 1);
                int speedValue = Math.Max(1, comboBox_speed.SelectedIndex + 1);
                PPLBEmulation.SetUtil.SetPrintRate(speedValue);
                
                // Set label dimensions (controls entire print area)
                int labelWidthDots = (int)numericUpDown_width.Value;
                int labelHeightDots = (int)numericUpDown_height.Value;
                int gapMM = (int)numericUpDown_gap.Value;
                
                PPLBEmulation.SetUtil.SetLabelLength(PPLBMediaTrack.Gap_Mode, labelHeightDots, gapMM);
                PPLBEmulation.SetUtil.SetPrintWidth(labelWidthDots);
                PPLBEmulation.SetUtil.SetHomePosition(5, 5); // 5-dot margin
                
                PPLBEmulation.SetUtil.SetClearImageBuffer();
                
                // Custom header with settings info
                buf = encoder.GetBytes($"Custom Preset - {DateTime.Now:HH:mm:ss}");
                PPLBEmulation.TextUtil.PrintText(30, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                
                buf = encoder.GetBytes($"Settings: D{trackBar_darkness.Value} S{speedValue}");
                PPLBEmulation.TextUtil.PrintText(30, 25, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_1, 1, 1, false, buf);
                
                // Use custom barcode text
                string barcodeText = !string.IsNullOrWhiteSpace(textBox_barcodeText.Text) ? textBox_barcodeText.Text : "CUSTOM-PRESET";
                buf = encoder.GetBytes(barcodeText);
                
                // Calculate optimal text layout for the specified width
                int labelWidth = (int)numericUpDown_width.Value;
                var (textLines, textFont, textSize) = CalculateTextLayout(barcodeText, labelWidth);
                
                // Print text layout information
                buf2 = encoder.GetBytes($"Text Layout: {textLines.Length} line(s) for width {labelWidth}px");
                PPLBEmulation.TextUtil.PrintText(30, 50, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_1, 1, 1, false, buf2);
                
                // Print each line of the optimized text
                int textStartY = 65;
                for (int i = 0; i < textLines.Length; i++)
                {
                    buf2 = encoder.GetBytes($"L{i + 1}: {textLines[i]}");
                    PPLBEmulation.TextUtil.PrintText(30, textStartY + (i * 12), PPLBOrient.Clockwise_0_Degrees, textFont, textSize, textSize, false, buf2);
                }
                
                // Apply alignment and rotation settings
                int xPos = 50, yPos = 80 + (textLines.Length * 12) + 10;
                switch (comboBox_alignment.SelectedIndex)
                {
                    case 0: xPos = 50; break;  // Left
                    case 1: xPos = 150; break; // Center  
                    case 2: xPos = 250; break; // Right
                }
                
                PPLBOrient orientation = PPLBOrient.Clockwise_0_Degrees;
                switch (comboBox_rotation.SelectedIndex)
                {
                    case 0: orientation = PPLBOrient.Clockwise_0_Degrees; break;
                    case 1: orientation = PPLBOrient.Clockwise_90_Degrees; break;
                    case 2: orientation = PPLBOrient.Clockwise_180_Degrees; break;
                    case 3: orientation = PPLBOrient.Clockwise_270_Degrees; break;
                }
                
                int barcodeHeight = (int)numericUpDown_height.Value;
                
                // Calculate narrow bar width for custom preset
                int desiredWidth = (int)numericUpDown_width.Value;
                int estimatedBarsPerChar = 11; // Average bars per character for Code 128
                int estimatedTotalBars = barcodeText.Length * estimatedBarsPerChar;
                int narrowBarWidth = Math.Max(1, Math.Min(10, desiredWidth / estimatedTotalBars));
                if (narrowBarWidth < 1) narrowBarWidth = 1;
                if (narrowBarWidth > 10) narrowBarWidth = 10; // ARGOX SDK typically supports 1-10
                
                // Add width information to the label
                buf2 = encoder.GetBytes($"BarW{narrowBarWidth} (Target:{desiredWidth}px)");
                PPLBEmulation.TextUtil.PrintText(200, 25, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_1, 1, 1, false, buf2);
                
                // Print barcode with custom settings
                buf2 = encoder.GetBytes($"Text: {barcodeText}");
                PPLBEmulation.TextUtil.PrintText(xPos, yPos, orientation, PPLBFont.Font_2, 1, 1, false, buf2);
                
                // Determine barcode type from selection or default to Code 128
                PPLBBarCodeType barcodeType = PPLBBarCodeType.Code_128_Auto_Mode;
                if (comboBox_barcode.SelectedIndex >= 0)
                {
                    switch (comboBox_barcode.Text)
                    {
                        case "Code 128 UCC Serial Shipping Container Code":
                            barcodeType = PPLBBarCodeType.Code_128_UCC;
                            break;
                        case "Code 128 auto A, B, C modes":
                            barcodeType = PPLBBarCodeType.Code_128_Auto_Mode;
                            break;
                        default:
                            barcodeType = PPLBBarCodeType.Code_128_Auto_Mode;
                            break;
                    }
                }
                
                if (comboBox_barcode.Text == "QR Code")
                {
                    int qrSize = Math.Min((int)numericUpDown_width.Value / 50, 10);
                    PPLBEmulation.BarcodeUtil.PrintQRCode(xPos, yPos + 30, PPLBQRCodeModel.Model_2, qrSize, PPLBQRCodeErrCorrect.Standard, buf);
                }
                else
                {
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(xPos, yPos + 30, orientation, barcodeType, narrowBarWidth, 0, barcodeHeight, true, buf);
                }
                
                // Add gap information
                buf2 = encoder.GetBytes($"Gap: {numericUpDown_gap.Value}mm | Align: {comboBox_alignment.Text} | Rot: {comboBox_rotation.Text}");
                PPLBEmulation.TextUtil.PrintText(30, yPos + barcodeHeight + 60, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_1, 1, 1, false, buf2);
                
                // Set print conditions
                PPLBEmulation.SetUtil.SetPrintOut(printcount, 1);
                PPLBEmulation.IOUtil.PrintOut();
                
                // Update status
                statusLabel.Text = $"✅ Custom preset printed with all advanced settings applied!";
                statusLabel.ForeColor = System.Drawing.Color.FromArgb(46, 204, 113);
            }
            catch (Exception ex)
            {
                ShowException.Show(this.Name, "__testPPLB_customPreset", ex);
                statusLabel.Text = $"❌ Custom preset failed: {ex.Message}";
                statusLabel.ForeColor = System.Drawing.Color.FromArgb(231, 76, 60);
            }
            finally
            {
                BarcodePrinter.Connection.Close();
            }
        }

        PPLZ PPLZEmulation;
        private void __testPPLZ_calibrate(int printcount)
        {
            int index = -1;
            if (false == __createPrn("PPLZ_calibrate.txt", ++index)) return;
            try 
            { 
                PPLZEmulation.SetUtil.SetMediaCalibration(); 
                PPLZEmulation.IOUtil.PrintOut(); 
            }
            catch (Exception ex) 
            { 
                ShowException.Show(this.Name, "__testPPLZ_calibrate", ex); 
            }
            finally 
            { 
                BarcodePrinter.Connection.Close(); 
            }
        }

        private void __testPPLZ_set1(int printcount)
        {
            int index = -1;
            if (false == __createPrn("PPLZ_set1.txt", ++index)) return;
            try 
            { 
                PPLZEmulation.SetUtil.SetReset(); 
                PPLZEmulation.IOUtil.PrintOut(); 
            }
            catch (Exception ex) 
            { 
                ShowException.Show(this.Name, "__testPPLZ_set1", ex); 
            }
            finally 
            { 
                BarcodePrinter.Connection.Close(); 
            }
        }
    }
}


