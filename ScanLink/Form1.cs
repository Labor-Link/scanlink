using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using BarcodePrinter_API;
using BarcodePrinter_API.Comm;
using BarcodePrinter_API.Emulation.PPLB;
// using BarcodePrinter_API.Emulation.PPLZ;
using System.Text;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Threading.Tasks;
using ScanLink;
using System.Linq;
using System.Web.Script.Serialization;

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

        private Process _scannerProcess;
        private ScannerComPortManager _scannerComPortManager;
        private FileSystemWatcher _scansFileWatcher;
        private System.Windows.Forms.Timer _scanRefreshTimer;
        private System.Windows.Forms.Timer _fileChangeDebounceTimer;
        private ApiAuthService _apiAuthService;
        private ScanLogUploadService _scanLogUploadService;

        // string strGraphicFilter = "All Graphic Type|*.bmp;*.gif;*.exig;*.jpg;*.png;*.tiff|All File|*.*||";
        // string[] strEmulation = { "PPLB", "PPLZ" };
        string[] strEmulation = { "PPLB"};
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
        // FunctionData[] PPLZ_ItemList;
        // string[] PPLZ_BarcodeList = { "QR Code", "Code 128" };

        BarcodePrinter BarcodePrinter;
        string strFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ScanLink", "BarcodePrinter");

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

        // Products and Crops data (from products_and_crops.json)
        private class ProductEntry { public string product_id; public string product_name; }
        private class CropEntry { public string crop_id; public string crop_name; public string variety; public int count; public string grade; }
        private class ProductsCropsRoot
        {
            public List<ProductEntry> products;
            public List<CropEntry> crops;
            public class IdsBucket { public List<string> product_ids; public List<string> crop_ids; }
            public IdsBucket ids;
        }
        private List<ProductEntry> _productsFromJson;
        private List<CropEntry> _cropsFromJson;
        private ComboBox comboBox_ProductName; // displays product_name, sets product_id
        private ComboBox comboBox_CropName;    // crop_name
        private ComboBox comboBox_Variety;     // variety
        private ComboBox comboBox_Count;       // count
        private ComboBox comboBox_Grade;       // grade
        private Label label_SelectedProductId; // shows chosen product_id
        private Label label_SelectedCropId;    // shows chosen crop_id

        public Form1()
        {
            InitializeComponent();
            _apiAuthService = new ApiAuthService();
            
            // Initialize scan log upload service
            _scanLogUploadService = new ScanLogUploadService(_apiAuthService);
            _scanLogUploadService.LogMessage += ScanLogUploadService_LogMessage;
            
            this.DoubleBuffered = true;
            try
            {
                // Set form font to generic sans-serif and apply recursively to all child controls
                this.Font = new Font(FontFamily.GenericSansSerif, this.Font?.SizeInPoints > 0 ? this.Font.SizeInPoints : 9F, this.Font?.Style ?? FontStyle.Regular, GraphicsUnit.Point);
                ApplySansSerifFont(this, null);
            }
            catch { }
            
            // Show login panel first, hide other panels
            loginPanel.Visible = true;
            startPanel.Visible = false;
            printerContentPanel.Visible = false;
            scannerContentPanel.Visible = false;
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
            PPLB_ItemList[0] = new FunctionData { Descration = "🎯 Custom Preset (Advanced Settings)", Function = __testPPLB_customPreset };
            PPLB_ItemList[1] = new FunctionData { Descration = "Calibrate", Function = __testPPLB_calibrate };
            PPLB_ItemList[2] = new FunctionData { Descration = "BarcodeUtil 1 : one barcode", Function = __testPPLB_barcode1 };
            PPLB_ItemList[3] = new FunctionData { Descration = "Reset", Function = __testPPLB_set1 };

            // PPLZ_ItemList = new FunctionData[2];
            // PPLZ_ItemList[0] = new FunctionData { Descration = "Reset", Function = __testPPLZ_set1 };
            // PPLZ_ItemList[1] = new FunctionData { Descration = "Calibrate", Function = __testPPLZ_calibrate };
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Set initial panel visibility - show login panel
            loginPanel.Visible = true; // Show login first
            startPanel.Visible = false;
            printerContentPanel.Visible = false;
            scannerContentPanel.Visible = false;

            // Load ScanLinkLogo.png into loginMainLogoPictureBox and startpanelLogoPictureBox
            LoadLoginLogo();
            
            // Set up colored labels with red asterisks
            CreateColoredLabel(usernameLabel, "Email", "*");
            CreateColoredLabel(passwordLabel, "Password", "*");

            strSelectFolder = strFolder;
            try { Directory.CreateDirectory(strFolder); } catch {}

            // Initialize COM port scanner manager
            _scannerComPortManager = new ScannerComPortManager();
            _scannerComPortManager.ScannerDataReceived += ScannerComPortManager_DataReceived;
            _scannerComPortManager.ScannerError += ScannerComPortManager_Error;
            _scannerComPortManager.ScannerLog += ScannerComPortManager_Log;

            // Removed duplicated printer UI initialization code
            // InitFunctionData();
            
            // Hook up Shown event to auto-start scan script
            this.Shown += Form1_Shown;
            this.Resize += Form1_Resize;
            
            // foreach (string str in strPort) comboBox_port.Items.Add(str);
            // comboBox_port.Text = "USB";
            // foreach (string str in strEmulation) comboBox_emulation.Items.Add(str);
            // comboBox_emulation.Text = "PPLB";
            
            // Initialize advanced settings with defaults and tooltips
            // InitializeAdvancedSettings();
            
            // Initialize placeholder text for login fields
            InitializeLoginPlaceholders();

            // Apply initial layout to ensure new UI is shown from start
            LayoutRootPanels();

            // Apply subtle rounded corners to key UI elements
            // ApplyRoundedCorners(button_send, 10);
            // ApplyRoundedCorners(button_preview, 10);
            // ApplyRoundedCorners(button_setting, 10);

            // ApplyRoundedCorners(barcodeTextPanel, 8);
            // ApplyRoundedCorners(dimensionsPanel, 8);
            // ApplyRoundedCorners(alignmentPanel, 8);
            // ApplyRoundedCorners(qualityPanel, 8);
            // ApplyRoundedCorners(previewPanel, 8);

            // ApplyRoundedCorners(actionPanel, 8);
            // ApplyRoundedCorners(statusPanel, 8);
            
            // Apply modern minimalist styling to key buttons
            ApplyModernStylesToButtons();
            LayoutRootPanels();
            
            InitializeProductAndCropSelectors();
            // Load saved advanced settings
            LoadAdvancedSettings();
        }

        private void LoadLoginLogo()
        {
            try
            {
                // Try to load logo from multiple possible locations and file names
                string[] possiblePaths = new string[]
                {
                    Path.Combine(Application.StartupPath, "ScanLinkLogo.png"),
                    Path.Combine(Application.StartupPath, "Scan Link - final logo.png"),
                    Path.Combine(Application.StartupPath, "logo.png"),
                    Path.Combine(Application.StartupPath, "Resources", "ScanLinkLogo.png"),
                    Path.Combine(Application.StartupPath, "Resources", "Scan Link - final logo.png"),
                    Path.Combine(Application.StartupPath, "..", "..", "ScanLinkLogo.png"),
                    Path.Combine(Application.StartupPath, "..", "..", "Scan Link - final logo.png"),
                    Path.Combine(Application.StartupPath, "..", "..", "logo.png"),
                    Path.Combine(Application.StartupPath, "..", "..", "Resources", "ScanLinkLogo.png")
                };

                foreach (string path in possiblePaths)
                {
                    string fullPath = Path.GetFullPath(path);
                    System.Diagnostics.Debug.WriteLine($"Trying: {fullPath}");
                    
                    if (File.Exists(fullPath))
                    {
                        Image logoImage = Image.FromFile(fullPath);
                        if (loginMainLogoPictureBox != null)
                        {
                            loginMainLogoPictureBox.Image = logoImage;
                            loginMainLogoPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                            loginMainLogoPictureBox.Visible = true;
                            System.Diagnostics.Debug.WriteLine($"✓ Successfully loaded logo from: {fullPath}");
                            System.Diagnostics.Debug.WriteLine($"  PictureBox size: {loginMainLogoPictureBox.Width}x{loginMainLogoPictureBox.Height}");
                            System.Diagnostics.Debug.WriteLine($"  PictureBox location: {loginMainLogoPictureBox.Location}");
                            System.Diagnostics.Debug.WriteLine($"  PictureBox visible: {loginMainLogoPictureBox.Visible}");
                        }
                        if (startpanelLogoPictureBox != null)
                        {
                            startpanelLogoPictureBox.Image = logoImage;
                            startpanelLogoPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                            startpanelLogoPictureBox.Visible = true;
                        }
                        return;
                    }
                }

                System.Diagnostics.Debug.WriteLine("❌ Logo file not found in any expected location");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading logo: {ex.Message}");
            }
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

        private async void loginButton_Click(object sender, EventArgs e)
        {
            // Validate required fields first
            if (!ValidateLoginFields())
            {
                loginStatusLabel.Text = "Please fill in all required fields";
                loginStatusLabel.ForeColor = Color.Red;
                return;
            }

            loginButton.Enabled = false;
            loginButton.Text = "Logging in...";
            loginStatusLabel.Text = "Authenticating...";
            loginStatusLabel.ForeColor = Color.Orange;

            try
            {
                var result = await _apiAuthService.LoginAsync(usernameTextBox.Text, passwordTextBox.Text);

                if (result.Success)
                {
                    //loginStatusLabel.Text = "Login successful! Redirecting...";
                    // Decode JWT token to extract user information
                    var tokenPayload = _apiAuthService.DecodeJwtToken(result.Data.accessToken);
                    
                    // Debug: Check if token decoding worked
                    if (tokenPayload == null || tokenPayload.Count == 0)
                    {
                        loginStatusLabel.Text = "❌ Error: Failed to decode JWT token payload";
                        loginStatusLabel.ForeColor = Color.Red;
                        return;
                    }
                    
                    // Extract key information from token
                    string userId = GetTokenValue(tokenPayload, "userId") ?? GetTokenValue(tokenPayload, "sub") ?? "N/A";
                    string activeSiteId = _apiAuthService.GetActiveSiteIdFromPayload(tokenPayload) ?? "N/A";
                    string authorities = GetTokenValue(tokenPayload, "authorities") ?? GetTokenValue(tokenPayload, "roles") ?? "N/A";
                    string exp = GetTokenValue(tokenPayload, "exp") ?? "N/A";
                    string iat = GetTokenValue(tokenPayload, "iat") ?? "N/A";
                    string iss = GetTokenValue(tokenPayload, "iss") ?? "N/A";
                    
                    // Get employee authority site IDs only
                    var employeeSiteIds = _apiAuthService.GetEmployeeAuthoritySiteIdsFromPayload(tokenPayload);
                    
                    // Format token information for display
                    var tokenInfo = new System.Text.StringBuilder();
                    tokenInfo.AppendLine("✅ Login successful! JWT Token decoded:");
                    string firstName = GetTokenValue(tokenPayload, "first_name") ?? "N/A";
                    string lastName = GetTokenValue(tokenPayload, "last_name") ?? "N/A";
                    string profileFileId = GetTokenValue(tokenPayload, "profile_file_id") ?? "N/A";
                    tokenInfo.AppendLine($"");
                    tokenInfo.AppendLine($"👤 Name: {firstName} {lastName}");
                    tokenInfo.AppendLine($"🆔 Profile ID: {profileFileId}");
                    tokenInfo.AppendLine($"👤 User ID: {userId}");
                    
                    
                    // Add employee authority site information
                    if (employeeSiteIds.Count > 0)
                    {
                        tokenInfo.AppendLine($"");
                        tokenInfo.AppendLine("🏢 Employee Authority Sites:");
                        foreach (var siteId in employeeSiteIds)
                        {
                            tokenInfo.AppendLine($"Site ID: {siteId}");
                        }
                    }
                    else
                    {
                        tokenInfo.AppendLine($"");
                        tokenInfo.AppendLine("⚠️ No employee authority sites found in token");
                        
                        // Debug: Show if authorities exist
                        if (tokenPayload.ContainsKey("authorities"))
                        {
                            tokenInfo.AppendLine($"🔍 Authorities field exists in token payload");
                        }
                        else
                        {
                            tokenInfo.AppendLine($"❌ Authorities field NOT found in token payload");
                            tokenInfo.AppendLine($"📋 Available keys in token: {string.Join(", ", tokenPayload.Keys)}");
                        }
                    }
                    
                    // loginStatusLabel.Text = tokenInfo.ToString();
                    // loginStatusLabel.ForeColor = Color.Green;
                    
                    // Check authentication status immediately after login
                    var authStatus = _apiAuthService.IsTokenValid();
                    System.Diagnostics.Debug.WriteLine($"Authentication check after login: {authStatus}");
                    
                    // Extend token expiry for testing (temporary fix)
                    _apiAuthService.ExtendTokenExpiry();
                    
                    // Check authentication status again after extending
                    authStatus = _apiAuthService.IsTokenValid();
                    System.Diagnostics.Debug.WriteLine($"Authentication check after extending: {authStatus}");
                    
                    // Show token info for a moment before redirecting
                    await Task.Delay(3000);
                    
                    // Enrich queued logs immediately after login, then start periodic uploader
                    _scanLogUploadService?.EnrichLogsFileOnce();
                    _scanLogUploadService?.Start();
                    
                    // Hide login panel and show scanner panel directly
                    loginPanel.Visible = false;
                    startPanel.Visible = false;
                    printerContentPanel.Visible = false;
                    scannerContentPanel.Visible = true;
                    
                    // Initialize the scanner data grid view
                    InitializeScannerDataGridView();
                    LoadScansData();
                    StartScanFileMonitoring();
                    
                    // Initialize COM port scanners after login
                    InitializeComPortScanners();
                    
                    // Apply layout to ensure new UI is shown
                    LayoutRootPanels();
                }
                else
                {
                    // loginStatusLabel.Text = $"{result.ErrorMessage}";
                    loginStatusLabel.Text = "Invalid username or password";
                    loginStatusLabel.ForeColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                loginStatusLabel.Text = $"Error: {ex.Message}";
                loginStatusLabel.ForeColor = Color.Red;
            }
            finally
            {
                loginButton.Enabled = true;
                loginButton.Text = "Login";
            }
        }

        private void InitializeLoginPlaceholders()
        {
            // Initialize username placeholder
            usernameTextBox.Text = "Type your Email...";
            usernameTextBox.ForeColor = Color.Gray;
            
            // Initialize password placeholder
            passwordTextBox.Text = "Type your Password...";
            passwordTextBox.ForeColor = Color.Gray;
            passwordTextBox.PasswordChar = '\0';
        }

        private void passwordToggleButton_Click(object sender, EventArgs e)
        {
            // Don't toggle if placeholder text is showing
            if (passwordTextBox.Text == "Type your Password...")
                return;
                
            if (passwordTextBox.PasswordChar == '●')
            {
                // Show password
                passwordTextBox.PasswordChar = '\0';
                passwordToggleButton.Text = "👁️";
            }
            else
            {
                // Hide password
                passwordTextBox.PasswordChar = '●';
                passwordToggleButton.Text = "👁️";
            }
        }

        private bool ValidateLoginFields()
        {
            bool isValid = true;
            
            // Validate username/email
            if (string.IsNullOrWhiteSpace(usernameTextBox.Text) || usernameTextBox.Text == "Type your Email...")
            {
                isValid = false;
            }
            
            // Validate password
            if (string.IsNullOrWhiteSpace(passwordTextBox.Text) || passwordTextBox.Text == "Type your Password...")
            {
                isValid = false;
            }
            
            return isValid;
        }

        private void usernameTextBox_Enter(object sender, EventArgs e)
        {
            if (usernameTextBox.Text == "Type your Email...")
            {
                usernameTextBox.Text = "";
                usernameTextBox.ForeColor = Color.Black;
            }
        }

        private void usernameTextBox_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(usernameTextBox.Text))
            {
                usernameTextBox.Text = "Type your Email...";
                usernameTextBox.ForeColor = Color.Gray;
            }
        }

        private void usernameTextBox_TextChanged(object sender, EventArgs e)
        {
            if (usernameTextBox.Text != "Type your Email...")
            {
                usernameTextBox.ForeColor = Color.Black;
            }
        }

        private void passwordTextBox_Enter(object sender, EventArgs e)
        {
            if (passwordTextBox.Text == "Type your Password...")
            {
                passwordTextBox.Text = "";
                passwordTextBox.ForeColor = Color.Black;
                passwordTextBox.PasswordChar = '●';
            }
        }

        private void passwordTextBox_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(passwordTextBox.Text))
            {
                passwordTextBox.Text = "Type your Password...";
                passwordTextBox.ForeColor = Color.Gray;
                passwordTextBox.PasswordChar = '\0';
            }
        }

        private void passwordTextBox_TextChanged(object sender, EventArgs e)
        {
            if (passwordTextBox.Text != "Type your Password...")
            {
                passwordTextBox.ForeColor = Color.Black;
                if (passwordTextBox.PasswordChar == '\0')
                {
                    passwordTextBox.PasswordChar = '●';
                }
            }
        }

        private string GetTokenValue(Dictionary<string, object> tokenPayload, string key)
        {
            if (tokenPayload != null && tokenPayload.ContainsKey(key))
            {
                var value = tokenPayload[key];
                if (value != null)
                {
                    // Handle arrays (like authorities/roles)
                    if (value is System.Collections.ArrayList arrayList)
                    {
                        return string.Join(", ", arrayList.ToArray());
                    }
                    // Handle other object types
                    return value.ToString();
                }
            }
            return null;
        }


        public string GetApiToken()
        {
            return _apiAuthService?.GetCurrentToken();
        }

        public bool IsAuthenticated()
        {
            return _apiAuthService?.IsTokenValid() ?? false;
        }

        public Dictionary<string, object> GetTokenPayload()
        {
            return _apiAuthService?.GetCurrentTokenPayload();
        }

        public string GetActiveSiteId()
        {
            return _apiAuthService?.GetActiveSiteId();
        }

        public List<ApiAuthService.SiteInfo> GetAllSiteIds()
        {
            return _apiAuthService?.GetAllSiteIds() ?? new List<ApiAuthService.SiteInfo>();
        }

        public string GetTokenStatus()
        {
            if (_apiAuthService == null) return "No auth service";
            if (!_apiAuthService.IsTokenValid()) return "Token invalid or expired";
            return "Token valid";
        }

        private void button_FetchEmployees_Click(object sender, EventArgs e)
        {
            try
            {
                // Check authentication status first
                if (!_apiAuthService.IsTokenValid())
                {
                    // Get detailed token information for debugging
                    var tokenInfo = $"❌ Authentication failed. Token Status: {GetTokenStatus()}";
                    if (_apiAuthService.GetCurrentToken() == null)
                    {
                        tokenInfo += " | No token stored";
                    }
                    else
                    {
                        tokenInfo += $" | Token present: {_apiAuthService.GetCurrentToken().Substring(0, Math.Min(20, _apiAuthService.GetCurrentToken().Length))}...";
                    }
                    
                    statusLabel.Text = tokenInfo;
                    statusLabel.ForeColor = System.Drawing.Color.FromArgb(231, 76, 60);
                    return;
                }

                // Show employee selection dialog
                using (var employeeDialog = new EmployeeSelectionDialog(_apiAuthService))
                {
                    if (employeeDialog.ShowDialog() == DialogResult.OK)
                    {
                        var selectedEmployee = employeeDialog.SelectedEmployee;
                        if (selectedEmployee != null)
                        {
                            // Update the Employee ID textbox with the last 10 characters of the selected employee's user_id
                            string employeeId = selectedEmployee.user_id;
                            textBox_EmployeeID.Text = employeeId.Length > 10 ? employeeId.Substring(employeeId.Length - 10) : employeeId;
                            
                            // Update status
                            statusLabel.Text = $"Selected employee: {selectedEmployee.first_name} {selectedEmployee.last_name} (User ID: {selectedEmployee.user_id})";
                            statusLabel.ForeColor = System.Drawing.Color.FromArgb(46, 204, 113);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Error fetching employees: {ex.Message}";
                statusLabel.ForeColor = System.Drawing.Color.FromArgb(231, 76, 60);
            }
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
                // case "PPLZ":
                //     comboBox_test.Items.Clear();
                //     foreach (FunctionData item in PPLZ_ItemList) comboBox_test.Items.Add(item.Descration);
                //     comboBox_test.SelectedIndex = 0;
                //     comboBox_barcode.Items.Clear();
                //     foreach (string str in PPLZ_BarcodeList) comboBox_barcode.Items.Add(str);
                //     comboBox_barcode.SelectedIndex = 0;
                //     break;
            }
            
            // Auto-save settings when changed
            SaveAdvancedSettings();
        }

        private void comboBox_test_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox_barcode.Enabled = ("BarcodeUtil 1 : one barcode" == comboBox_test.Text) || ("🎯 Custom Preset (Advanced Settings)" == comboBox_test.Text);
            
            // Show advanced settings automatically when custom preset is selected
            if ("🎯 Custom Preset (Advanced Settings)" == comboBox_test.Text)
            {
                // checkBox_showAdvanced.Checked = true;
                // statusLabel.Text = "Custom Preset selected - Advanced settings enabled automatically for full control.";
                statusLabel.Text = "Custom Preset selected.";
                statusLabel.ForeColor = System.Drawing.Color.FromArgb(52, 152, 219);
            }
            
            // Enable/disable two-up controls based on custom preset selection
            bool enableTwoUpControls = ("🎯 Custom Preset (Advanced Settings)" == comboBox_test.Text) && (comboBox_emulation.Text == "PPLB");
            if (checkBox_twoUp != null) checkBox_twoUp.Enabled = enableTwoUpControls;
            bool x2Enabled = enableTwoUpControls && (checkBox_twoUp?.Checked ?? false);
            if (numericUpDown_x2Coordinate != null) numericUpDown_x2Coordinate.Enabled = x2Enabled;
            if (label_x2Coordinate != null) label_x2Coordinate.Enabled = x2Enabled;

            // Auto-save settings when changed
            SaveAdvancedSettings();
        }

        private void checkBox_showAdvanced_CheckedChanged(object sender, EventArgs e)
        {
            advancedPanel.Visible = checkBox_showAdvanced.Checked;
            if (checkBox_showAdvanced.Checked)
            {
                // Keep the window maximized and don't change window state
                statusLabel.Text = "Advanced settings enabled. Scroll down for all options. Configure barcode dimensions, alignment, and quality.";
            }
            else
            {
                // Keep the window maximized and don't change window state
                statusLabel.Text = "Basic settings mode. Check 'Show Advanced Settings' for more options.";
            }
            statusLabel.ForeColor = System.Drawing.Color.FromArgb(127, 140, 141);
            
            // Ensure the main panel can scroll properly
            mainPanel.AutoScrollMinSize = new Size(0, checkBox_showAdvanced.Checked ? 1100 : 700);
            
            // Update layout to position the advanced panel correctly
            LayoutRootPanels();
        }

        private void trackBar_darkness_Scroll(object sender, EventArgs e)
        {
            label_darknessValue.Text = trackBar_darkness.Value.ToString();
            
            // Auto-save settings when changed
            SaveAdvancedSettings();
        }

        // Event handlers for advanced settings controls
        private void comboBox_barcode_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Auto-save settings when changed
            SaveAdvancedSettings();
        }

        private void comboBox_alignment_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Auto-save settings when changed
            SaveAdvancedSettings();
        }

        private void comboBox_rotation_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Auto-save settings when changed
            SaveAdvancedSettings();
        }

        private void comboBox_speed_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Auto-save settings when changed
            SaveAdvancedSettings();
        }

        private void numericUpDown_width_ValueChanged(object sender, EventArgs e)
        {
            // Auto-save settings when changed
            SaveAdvancedSettings();
        }

        private void numericUpDown_height_ValueChanged(object sender, EventArgs e)
        {
            // Auto-save settings when changed
            SaveAdvancedSettings();
        }

        private void numericUpDown_gap_ValueChanged(object sender, EventArgs e)
        {
            // Auto-save settings when changed
            SaveAdvancedSettings();
        }

        private void numericUpDown_xCoordinate_ValueChanged(object sender, EventArgs e)
        {
            // Auto-save settings when changed
            SaveAdvancedSettings();
        }

        private void numericUpDown_x2Coordinate_ValueChanged(object sender, EventArgs e)
        {
            // Auto-save settings when changed
            SaveAdvancedSettings();
        }

        private void checkBox_twoUp_CheckedChanged(object sender, EventArgs e)
        {
            bool enabled = checkBox_twoUp.Checked;
            if (numericUpDown_x2Coordinate != null) numericUpDown_x2Coordinate.Enabled = enabled;
            if (label_x2Coordinate != null) label_x2Coordinate.Enabled = enabled;
            SaveAdvancedSettings();
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
            toolTip.SetToolTip(textBox_EmployeeID, "Enter the text/data to encode in the barcode");
            toolTip.SetToolTip(comboBox_ProductID, "Select the product ID for the barcode");
            toolTip.SetToolTip(numericUpDown_width, "Width of the barcode in dots/pixels");
            toolTip.SetToolTip(numericUpDown_height, "Height of the barcode in dots/pixels");
            toolTip.SetToolTip(numericUpDown_gap, "Gap between labels in millimeters");
            toolTip.SetToolTip(comboBox_alignment, "Horizontal alignment of the barcode on the label");
            toolTip.SetToolTip(trackBar_darkness, "Print darkness/density (1=lightest, 30=darkest)");
            toolTip.SetToolTip(comboBox_speed, "Print speed setting (1=slowest/highest quality, 9=fastest)");
            toolTip.SetToolTip(button_preview, "Preview the current settings before printing");
            toolTip.SetToolTip(checkBox_showAdvanced, "Show/hide advanced configuration options");
            
            // Set initial form height for basic mode
            this.Height = 750;
            
            // Add real-time preview update when Employee ID changes
            textBox_EmployeeID.TextChanged += textBox_EmployeeID_TextChanged;
            
            // Add real-time preview update when Product ID changes
            comboBox_ProductID.SelectedIndexChanged += comboBox_ProductID_SelectedIndexChanged;
            // Add real-time preview update when Crop ID changes
            comboBox_CropID.SelectedIndexChanged += comboBox_CropID_SelectedIndexChanged;
        }

        private void InitializeProductAndCropSelectors()
        {
            // Populate 000..999 for Product and Crop
            if (comboBox_ProductID != null)
            {
                comboBox_ProductID.Items.Clear();
                for (int i = 0; i <= 999; i++) comboBox_ProductID.Items.Add(i.ToString("D3"));
                if (comboBox_ProductID.Items.Count > 0) comboBox_ProductID.SelectedIndex = 0;
            }
            if (comboBox_CropID != null)
            {
                comboBox_CropID.Items.Clear();
                for (int i = 0; i <= 999; i++) comboBox_CropID.Items.Add(i.ToString("D3"));
                if (comboBox_CropID.Items.Count > 0) comboBox_CropID.SelectedIndex = 0;
            }

            // Enhance with JSON-backed dropdowns (product_name and crop attributes)
            try { InitializeProductAndCropSelectorsFromJson(); } catch { }
        }

        private void InitializeProductAndCropSelectorsFromJson()
        {
            if (!LoadProductsAndCropsJson()) return;
            if (advancedPanel == null) return;

            // Create controls once
            if (comboBox_ProductName == null)
            {
                comboBox_ProductName = new ComboBox();
                comboBox_ProductName.DropDownStyle = ComboBoxStyle.DropDownList;
                comboBox_ProductName.Width = 220;
                comboBox_ProductName.SelectedIndexChanged += (s, e) => OnProductNameChanged();
                advancedPanel.Controls.Add(comboBox_ProductName);
            }
            if (label_SelectedProductId == null)
            {
                label_SelectedProductId = new Label();
                label_SelectedProductId.AutoSize = true;
                advancedPanel.Controls.Add(label_SelectedProductId);
            }

            if (comboBox_CropName == null)
            {
                comboBox_CropName = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
                comboBox_CropName.SelectedIndexChanged += (s, e) => OnCropSelectorsChanged();
                advancedPanel.Controls.Add(comboBox_CropName);
            }
            if (comboBox_Variety == null)
            {
                comboBox_Variety = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
                comboBox_Variety.SelectedIndexChanged += (s, e) => OnCropSelectorsChanged();
                advancedPanel.Controls.Add(comboBox_Variety);
            }
            if (comboBox_Count == null)
            {
                comboBox_Count = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 80 };
                comboBox_Count.SelectedIndexChanged += (s, e) => OnCropSelectorsChanged();
                advancedPanel.Controls.Add(comboBox_Count);
            }
            if (comboBox_Grade == null)
            {
                comboBox_Grade = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 80 };
                comboBox_Grade.SelectedIndexChanged += (s, e) => OnCropSelectorsChanged();
                advancedPanel.Controls.Add(comboBox_Grade);
            }
            if (label_SelectedCropId == null)
            {
                label_SelectedCropId = new Label { AutoSize = true };
                advancedPanel.Controls.Add(label_SelectedCropId);
            }

            // Position controls relative to existing ProductID/CropID controls
            int prodTop = comboBox_ProductID != null ? Math.Max(0, comboBox_ProductID.Top - 30) : 40;
            int prodLeft = comboBox_ProductID != null ? comboBox_ProductID.Left : 20;
            comboBox_ProductName.Location = new Point(prodLeft, prodTop);
            label_SelectedProductId.Location = new Point(comboBox_ProductName.Right + 10, prodTop + 4);

            int cropTop = comboBox_CropID != null ? Math.Max(0, comboBox_CropID.Top - 30) : (prodTop + 40);
            int cropLeft = comboBox_CropID != null ? comboBox_CropID.Left : 20;
            comboBox_CropName.Location = new Point(cropLeft, cropTop);
            comboBox_Variety.Location = new Point(comboBox_CropName.Right + 10, cropTop);
            comboBox_Count.Location = new Point(comboBox_Variety.Right + 10, cropTop);
            comboBox_Grade.Location = new Point(comboBox_Count.Right + 10, cropTop);
            label_SelectedCropId.Location = new Point(comboBox_Grade.Right + 10, cropTop + 4);

            // Bind product_name list
            comboBox_ProductName.DataSource = _productsFromJson.Select(p => new { name = p.product_name, id = p.product_id }).ToList();
            comboBox_ProductName.DisplayMember = "name";
            comboBox_ProductName.ValueMember = "id";
            if (_productsFromJson.Count > 0) comboBox_ProductName.SelectedIndex = 0;

            // Bind crop attribute lists
            var cropNames = _cropsFromJson.Select(c => c.crop_name).Distinct().OrderBy(n => n).ToList();
            comboBox_CropName.Items.Clear();
            foreach (var n in cropNames) comboBox_CropName.Items.Add(n);
            if (comboBox_CropName.Items.Count > 0) comboBox_CropName.SelectedIndex = 0;

            // Initialize dependent lists based on selected crop_name
            PopulateCropDependentLists();
            OnProductNameChanged();
            OnCropSelectorsChanged();
        }

        private bool LoadProductsAndCropsJson()
        {
            try
            {
                string startupPath = Application.StartupPath;
                string jsonPath = Path.Combine(startupPath, "products_and_crops.json");
                if (!File.Exists(jsonPath))
                {
                    // Fallback to project path
                    string projectRoot = Path.GetFullPath(Path.Combine(Application.StartupPath, "..", "..", ".."));
                    string projPath = Path.Combine(projectRoot, "ScanLink", "products_and_crops.json");
                    if (File.Exists(projPath))
                    {
                        try { File.Copy(projPath, jsonPath, true); } catch { jsonPath = projPath; }
                    }
                }
                if (!File.Exists(jsonPath)) return false;

                string json = File.ReadAllText(jsonPath);
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                var data = serializer.Deserialize<ProductsCropsRoot>(json);
                if (data == null) return false;
                _productsFromJson = data.products ?? new List<ProductEntry>();
                _cropsFromJson = data.crops ?? new List<CropEntry>();
                return _productsFromJson.Count > 0 && _cropsFromJson.Count > 0;
            }
            catch { return false; }
        }

        private void OnProductNameChanged()
        {
            if (comboBox_ProductName?.SelectedItem == null || comboBox_ProductID == null) return;
            string id = (comboBox_ProductName.SelectedValue ?? string.Empty).ToString();
            // Update legacy ProductID combo selection to the mapped id
            int idx = -1;
            if (!string.IsNullOrEmpty(id))
            {
                for (int i = 0; i < comboBox_ProductID.Items.Count; i++)
                {
                    if (string.Equals(comboBox_ProductID.Items[i]?.ToString(), id, StringComparison.Ordinal)) { idx = i; break; }
                }
            }
            if (idx >= 0) comboBox_ProductID.SelectedIndex = idx; else comboBox_ProductID.Text = id;
            if (label_SelectedProductId != null) label_SelectedProductId.Text = $"Product ID: {id}";
        }

        private void PopulateCropDependentLists()
        {
            if (comboBox_CropName?.SelectedItem == null) return;
            string name = comboBox_CropName.SelectedItem.ToString();
            var varieties = _cropsFromJson.Where(c => c.crop_name == name).Select(c => c.variety).Distinct().OrderBy(v => v).ToList();
            comboBox_Variety.Items.Clear(); foreach (var v in varieties) comboBox_Variety.Items.Add(v);
            if (comboBox_Variety.Items.Count > 0) comboBox_Variety.SelectedIndex = 0;

            var counts = _cropsFromJson.Where(c => c.crop_name == name).Select(c => c.count).Distinct().OrderBy(x => x).ToList();
            comboBox_Count.Items.Clear(); foreach (var ct in counts) comboBox_Count.Items.Add(ct);
            if (comboBox_Count.Items.Count > 0) comboBox_Count.SelectedIndex = 0;

            var grades = _cropsFromJson.Where(c => c.crop_name == name).Select(c => c.grade).Distinct().OrderBy(g => g).ToList();
            comboBox_Grade.Items.Clear(); foreach (var g in grades) comboBox_Grade.Items.Add(g);
            if (comboBox_Grade.Items.Count > 0) comboBox_Grade.SelectedIndex = 0;
        }

        private void OnCropSelectorsChanged()
        {
            if (comboBox_CropName == null || comboBox_Variety == null || comboBox_Count == null || comboBox_Grade == null) return;
            if (comboBox_Variety.Focused && comboBox_Count.Items.Count == 0) PopulateCropDependentLists();
            if (comboBox_CropName.Focused) PopulateCropDependentLists();

            string name = comboBox_CropName.SelectedItem?.ToString();
            string variety = comboBox_Variety.SelectedItem?.ToString();
            int count = 0; int.TryParse(comboBox_Count.SelectedItem?.ToString() ?? "0", out count);
            string grade = comboBox_Grade.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(variety) || count == 0 || string.IsNullOrEmpty(grade)) return;

            var match = _cropsFromJson.FirstOrDefault(c => c.crop_name == name && c.variety == variety && c.count == count && c.grade == grade);
            string id = match?.crop_id ?? "";

            // Update legacy CropID combo selection to the mapped id
            if (comboBox_CropID != null)
            {
                int idx = -1;
                if (!string.IsNullOrEmpty(id))
                {
                    for (int i = 0; i < comboBox_CropID.Items.Count; i++)
                    {
                        if (string.Equals(comboBox_CropID.Items[i]?.ToString(), id, StringComparison.Ordinal)) { idx = i; break; }
                    }
                }
                if (idx >= 0) comboBox_CropID.SelectedIndex = idx; else comboBox_CropID.Text = id;
            }

            if (label_SelectedCropId != null) label_SelectedCropId.Text = string.IsNullOrEmpty(id) ? "Crop ID: (no match)" : $"Crop ID: {id}";
        }
        
        private void textBox_EmployeeID_TextChanged(object sender, EventArgs e)
        {
            // Update status to show current Employee ID
            string currentText = !string.IsNullOrWhiteSpace(textBox_EmployeeID.Text) ? textBox_EmployeeID.Text : "Default: 23456";
            statusLabel.Text = $"Employee ID updated: '{currentText}' - Click Preview to see visual representation";
            statusLabel.ForeColor = System.Drawing.Color.FromArgb(52, 152, 219);
        }
        private void comboBox_ProductID_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Update status to show current Product ID
            string currentText = comboBox_ProductID.SelectedItem?.ToString() ?? "000";
            statusLabel.Text = $"Product ID updated: '{currentText}' - Click Preview to see visual representation";
            statusLabel.ForeColor = System.Drawing.Color.FromArgb(52, 152, 219);
            SaveAdvancedSettings();
        }

        private void comboBox_CropID_SelectedIndexChanged(object sender, EventArgs e)
        {
            string currentText = comboBox_CropID.SelectedItem?.ToString() ?? "000";
            statusLabel.Text = $"Crop ID updated: '{currentText}' - Click Preview to see visual representation";
            statusLabel.ForeColor = System.Drawing.Color.FromArgb(52, 152, 219);
            SaveAdvancedSettings();
        }

        private void PrintBarcodeWithAdvancedSettings(int printCount)
        {
            if (checkBox_showAdvanced.Checked && !string.IsNullOrWhiteSpace(textBox_EmployeeID.Text))
            {
                // Use custom Employee ID from advanced settings
                string customText = textBox_EmployeeID.Text;
                statusLabel.Text = $"Printing barcode with custom text: '{customText}'";
                statusLabel.ForeColor = System.Drawing.Color.FromArgb(52, 152, 219);
                
                // Apply advanced settings
                ApplyAdvancedSettings();
            }
            if (checkBox_showAdvanced.Checked && comboBox_ProductID.SelectedItem != null)
            {
                // Use custom Product ID from advanced settings
                string customText = comboBox_ProductID.SelectedItem?.ToString() ?? "p1";
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
                ErrorDialog.ShowError("Preview Error", $"Preview failed: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}", this);
                statusLabel.Text = "Preview failed. Please check your settings.";
                statusLabel.ForeColor = System.Drawing.Color.FromArgb(231, 76, 60);
            }
        }
        
        private void ShowVisualBarcodePreview()
        {
            // Get current settings first
            string fullEmployeeID = !string.IsNullOrWhiteSpace(textBox_EmployeeID.Text) ? textBox_EmployeeID.Text : "";
            string EmployeeID = fullEmployeeID.Length > 10 ? fullEmployeeID.Substring(fullEmployeeID.Length - 10) : fullEmployeeID.PadLeft(10, '0');
            string ProductID = (comboBox_ProductID.SelectedItem?.ToString() ?? "000").PadLeft(3, '0');
            string CropID = (comboBox_CropID.SelectedItem?.ToString() ?? "000").PadLeft(3, '0');
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
            
            //BarcodeID = 10-char EmployeeID + 3-char ProductID + 3-char CropID
            string BarcodeID = $"{EmployeeID}{ProductID}{CropID}";

            // Calculate text layout
            var (textLines, textFont, textSize) = CalculateTextLayout(BarcodeID, labelWidth);
            
            // Create visual representation
            CreateBarcodePreviewVisual(previewPanel, BarcodeID, labelWidth, labelHeight, textLines);
            
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
                $"📝 Text: {BarcodeID} (fits in 1 line)";
                
            infoLabel.Text = $"Preview Settings:\n" +
                $"{textLayoutInfo}\n" +
                $"📏 Dimensions: {labelWidth} x {labelHeight} pixels\n" +
                $"🔄 Alignment: {comboBox_alignment.Text} | 🔁 Rotation: 0°\n" +
                $"📊 Barcode Type: {comboBox_barcode.Text} | 🌑 Darkness: {trackBar_darkness.Value}/30\n" +
                $"🔢 Print Count: {numericUpDown_count.Value} | ⚡ Speed: {comboBox_speed.Text}";
            
            infoPanel.Controls.Add(infoLabel);
            
            previewForm.Controls.Add(previewPanel);
            previewForm.Controls.Add(infoPanel);
            
            // Show preview
            previewForm.ShowDialog(this);
            
            statusLabel.Text = $"Visual preview shown for: '{BarcodeID}' ({textLines.Length} lines)";
            statusLabel.ForeColor = System.Drawing.Color.FromArgb(46, 204, 113);
        }
        
        private void CreateBarcodePreviewVisual(Panel panel, string BarcodeID, int width, int height, string[] textLines)
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
            headerLabel.Text = "!! Scan-Link !!";
            headerLabel.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            headerLabel.ForeColor = Color.FromArgb(52, 73, 94);
            headerLabel.Location = new Point(0, 0); // 30-5 for margin
            headerLabel.AutoSize = true;
            marginIndicator.Controls.Add(headerLabel);
            currentY += 25;
            
            // // 2. Show barcode type (always printed)
            // Label typeLabel = new Label();
            // typeLabel.Text = comboBox_barcode.Text;
            // typeLabel.Font = new Font("Segoe UI", 8);
            // typeLabel.ForeColor = Color.FromArgb(127, 140, 141);
            // typeLabel.Location = new Point(45, currentY); // 50-5 for margin
            // typeLabel.AutoSize = true;
            // marginIndicator.Controls.Add(typeLabel);
            // currentY += 25;
            
            // 3. Show text layout info (if advanced settings and wrapped text)
            // if (checkBox_showAdvanced.Checked && textLines.Length > 1)
            // {
            //     Label wrapLabel = new Label();
            //     wrapLabel.Text = $"Text wrapped to {textLines.Length} lines for width {width}:";
            //     wrapLabel.Font = new Font("Courier New", 7);
            //     wrapLabel.ForeColor = Color.FromArgb(155, 89, 182);
            //     wrapLabel.Location = new Point(45, currentY);
            //     wrapLabel.AutoSize = true;
            //     marginIndicator.Controls.Add(wrapLabel);
            //     currentY += 15;
                
            //     // Show each wrapped line
            //     for (int i = 0; i < textLines.Length; i++)
            //     {
            //         Label lineLabel = new Label();
            //         lineLabel.Text = $"Line {i + 1}: {textLines[i]}";
            //         lineLabel.Font = new Font("Courier New", 7);
            //         lineLabel.ForeColor = Color.FromArgb(155, 89, 182);
            //         lineLabel.Location = new Point(45, currentY);
            //         lineLabel.AutoSize = true;
            //         marginIndicator.Controls.Add(lineLabel);
            //         currentY += 12;
            //     }
            //     currentY += 10; // Extra spacing
            // }
            
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
            // Label normalLabel = new Label();
            // normalLabel.Text = "normal";
            // normalLabel.Font = new Font("Segoe UI", 8);
            // normalLabel.ForeColor = Color.FromArgb(231, 76, 60);
            // normalLabel.Location = new Point(barcodeX, currentY - 5);
            // normalLabel.AutoSize = true;
            // marginIndicator.Controls.Add(normalLabel);
            
            // 6. Simulated barcode visual
            Panel barcodeVisual = new Panel();
			// int barcodeHeight = checkBox_showAdvanced.Checked ? (int)numericUpDown_height.Value : 100;
            int barcodeHeight = 50;
            
            // Calculate barcode width using same logic as printer
            int desiredWidth = checkBox_showAdvanced.Checked ? (int)numericUpDown_width.Value : 400;
            int estimatedBarsPerChar = 11; // Average bars per character for Code 128
            int estimatedTotalBars = BarcodeID.Length * estimatedBarsPerChar;
            int narrowBarWidth = Math.Max(1, Math.Min(10, desiredWidth / estimatedTotalBars));
            if (narrowBarWidth < 1) narrowBarWidth = 1;
            if (narrowBarWidth > 10) narrowBarWidth = 10;
            
            // Calculate actual barcode width based on narrow bar width
            int barcodeWidth = Math.Min(width - barcodeX - 10, estimatedTotalBars * narrowBarWidth + 6 * narrowBarWidth); // +6 for quiet zones
            barcodeWidth = Math.Max(barcodeWidth, 150); // ensure visible minimum for scannability
            //Sticker height
            int stickerHeight = (int)numericUpDown_height.Value;

            barcodeVisual.Location = new Point(0, stickerHeight-120);
			barcodeVisual.Size = new Size(Math.Min(Math.Max(barcodeWidth, 220), 260), 50);
            barcodeVisual.BackColor = Color.White;
            // barcodeVisual.BorderStyle = BorderStyle.FixedSingle;
            marginIndicator.Controls.Add(barcodeVisual);
            
            // Create barcode pattern with calculated dimensions
            barcodeVisual.Paint += (s, pe) => DrawBarcodePreview(pe.Graphics, new Rectangle(Point.Empty, barcodeVisual.Size), BarcodeID, comboBox_barcode.Text);
            currentY += barcodeVisual.Height + 15;
            
            // // 7. Show human readable label
            // Label humanLabel = new Label();
            // humanLabel.Text = "human readable";
            // humanLabel.Font = new Font("Segoe UI", 8);
            // humanLabel.ForeColor = Color.FromArgb(231, 76, 60);
            // humanLabel.Location = new Point(barcodeX, currentY);
            // humanLabel.AutoSize = true;
            // marginIndicator.Controls.Add(humanLabel);
            // currentY += 15;
            
            // // 8. Second barcode (human readable version)
            // Panel barcodeVisual2 = new Panel();
            // barcodeVisual2.Location = new Point(barcodeX, currentY + 5);
			// barcodeVisual2.Size = new Size(Math.Min(Math.Max(barcodeWidth, 220), 260), Math.Max(100, barcodeHeight));
            // barcodeVisual2.BackColor = Color.White;
            // // barcodeVisual2.BorderStyle = BorderStyle.FixedSingle;
            // marginIndicator.Controls.Add(barcodeVisual2);
            
            // // Create second barcode pattern
            // barcodeVisual2.Paint += (s, pe) => {
            //     var area = new Rectangle(0, 0, barcodeVisual2.Width, barcodeVisual2.Height - 20);
            //     DrawBarcodePreview(pe.Graphics, area, BarcodeID, comboBox_barcode.Text);
            //     // Add human readable text
            //     using (Font font = new Font("Courier New", 8))
            //     using (Brush brush = new SolidBrush(Color.Black))
            //     {
            //         pe.Graphics.DrawString(BarcodeID, font, brush, 5, barcodeVisual2.Height - 18);
            //     }
            // };

            // 9. EmployeeIDLabel
            Label EmployeeIDLabel = new Label();
            string fullEmployeeID = !string.IsNullOrWhiteSpace(textBox_EmployeeID.Text) ? textBox_EmployeeID.Text : "Tanish";
            string EmployeeID = fullEmployeeID.Length > 10 ? fullEmployeeID.Substring(fullEmployeeID.Length - 10) : fullEmployeeID;
            EmployeeIDLabel.Text = $"EmployeeID: {EmployeeID}";
            EmployeeIDLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            EmployeeIDLabel.ForeColor = Color.FromArgb(52, 73, 94);
            EmployeeIDLabel.Location = new Point(0, stickerHeight-110+barcodeHeight); // 30-5 for margin
            EmployeeIDLabel.AutoSize = true;
            marginIndicator.Controls.Add(EmployeeIDLabel);
            currentY += 25;

            // 10. ProductIDLabel
            Label ProductIDLabel = new Label();
            ProductIDLabel.Text = $"ProductID: {comboBox_ProductID.SelectedItem?.ToString() ?? "p1"}";
            ProductIDLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            ProductIDLabel.ForeColor = Color.FromArgb(52, 73, 94);
            ProductIDLabel.Location = new Point(0, stickerHeight-75+barcodeHeight); // 30-5 for margin
            ProductIDLabel.AutoSize = true;
            marginIndicator.Controls.Add(ProductIDLabel);
            currentY += 25;
            
            // // 9. Add label dimension info
            // Label dimensionsLabel = new Label();
            // dimensionsLabel.Text = $"Label Size: {width} x {height} pixels | Gap: {numericUpDown_gap.Value}mm";
            // dimensionsLabel.Font = new Font("Segoe UI", 8, FontStyle.Italic);
            // dimensionsLabel.ForeColor = Color.FromArgb(127, 140, 141);
            // dimensionsLabel.Location = new Point(5, labelOutline.Height - 15);
            // dimensionsLabel.AutoSize = true;
            // labelOutline.Controls.Add(dimensionsLabel);
            
            // // 10. Add alignment and rotation indicators
            // Panel alignmentIndicator = new Panel();
            // alignmentIndicator.Size = new Size(8, 8);
            // alignmentIndicator.BackColor = Color.FromArgb(231, 76, 60);
            
            // switch (comboBox_alignment.SelectedIndex)
            // {
            //     case 0: // Left
            //         alignmentIndicator.Location = new Point(2, labelOutline.Height / 2);
            //         break;
            //     case 1: // Center
            //         alignmentIndicator.Location = new Point(labelOutline.Width / 2 - 4, labelOutline.Height / 2);
            //         break;
            //     case 2: // Right
            //         alignmentIndicator.Location = new Point(labelOutline.Width - 10, labelOutline.Height / 2);
            //         break;
            // }
            // labelOutline.Controls.Add(alignmentIndicator);
            
            // 11. Add rotation indicator
            // if (false)
            // {
            //     Label rotationLabel = new Label();
            //     rotationLabel.Text = "↻0°";
            //     rotationLabel.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            //     rotationLabel.ForeColor = Color.FromArgb(231, 76, 60);
            //     rotationLabel.Location = new Point(labelOutline.Width - 60, 5);
            //     rotationLabel.AutoSize = true;
            //     labelOutline.Controls.Add(rotationLabel);
            // }
        }
        
        private void CreateBarcodePattern(Graphics g, Size panelSize, string text, string barcodeType)
        {
            // Create a more accurate barcode-like visual pattern that better represents actual barcode output
            using (Brush blackBrush = new SolidBrush(Color.Black))
            using (Brush whiteBrush = new SolidBrush(Color.White))
            {
                // Handle different barcode types
                if (barcodeType == "QR Code")
                {
                    CreateQRCodePattern(g, panelSize, text);
                    return;
                }
                
                // Calculate bar width to match printer's narrow bar width calculation
                int estimatedBarsPerChar = 11; // Average bars per character for Code 128
                int estimatedTotalBars = text.Length * estimatedBarsPerChar;
                int narrowBarWidth = Math.Max(1, Math.Min(10, panelSize.Width / estimatedTotalBars));
                if (narrowBarWidth < 1) narrowBarWidth = 1;
                if (narrowBarWidth > 10) narrowBarWidth = 10;
                
                int x = 0;
                int barHeight = panelSize.Height - 25;
                
                // Create a more realistic barcode pattern
                // Start with quiet zone (white space)
                g.FillRectangle(whiteBrush, x, 0, narrowBarWidth * 3, barHeight);
                x += narrowBarWidth * 3;
                
                // Create barcode pattern based on text characters
                foreach (char c in text)
                {
                    int charValue = (int)c;
                    
                    // Create start pattern (black bar)
                    g.FillRectangle(blackBrush, x, 0, narrowBarWidth, barHeight);
                    x += narrowBarWidth;
                    g.FillRectangle(whiteBrush, x, 0, narrowBarWidth, barHeight);
                    x += narrowBarWidth;
                    
                    // Create character pattern (simplified Code 128-like pattern)
                    for (int i = 0; i < 6; i++)
                    {
                        bool isBlack = (charValue & (1 << (i % 8))) != 0;
                        g.FillRectangle(isBlack ? blackBrush : whiteBrush, x, 0, narrowBarWidth, barHeight);
                        x += narrowBarWidth;
                        if (x >= panelSize.Width - narrowBarWidth) break;
                    }
                    
                    if (x >= panelSize.Width - narrowBarWidth) break;
                }
                
                // End with quiet zone
                g.FillRectangle(whiteBrush, x, 0, narrowBarWidth * 3, barHeight);
                
                // Add human readable text at bottom (centered)
                using (Font font = new Font("Courier New", 8))
                {
                    SizeF textSize = g.MeasureString(text, font);
                    float textX = (panelSize.Width - textSize.Width) / 2;
                    g.DrawString(text, font, blackBrush, textX, panelSize.Height - 20);
                }
            }
        }
        
        private void CreateQRCodePattern(Graphics g, Size panelSize, string text)
        {
            // Create a simplified QR code pattern
            using (Brush blackBrush = new SolidBrush(Color.Black))
            using (Brush whiteBrush = new SolidBrush(Color.White))
            {
                // Fill background
                g.FillRectangle(whiteBrush, 0, 0, panelSize.Width, panelSize.Height);
                
                // Calculate QR code size (simplified)
                int qrSize = Math.Min(panelSize.Width, panelSize.Height - 25) / 25; // 25x25 grid
                if (qrSize < 1) qrSize = 1;
                
                int startX = (panelSize.Width - (qrSize * 25)) / 2;
                int startY = (panelSize.Height - 25 - (qrSize * 25)) / 2;
                
                // Create a simplified QR-like pattern
                Random rand = new Random(text.GetHashCode()); // Use text hash for consistent pattern
                for (int y = 0; y < 25; y++)
                {
                    for (int x = 0; x < 25; x++)
                    {
                        // Create position markers (corners)
                        if ((x < 7 && y < 7) || (x >= 18 && y < 7) || (x < 7 && y >= 18))
                        {
                            // Position markers
                            if ((x < 3 || x >= 4) && (y < 3 || y >= 4))
                            {
                                g.FillRectangle(blackBrush, startX + x * qrSize, startY + y * qrSize, qrSize, qrSize);
                            }
                        }
                        else
                        {
                            // Data area - use pseudo-random pattern based on text
                            bool isBlack = (rand.Next(100) < 50);
                            if (isBlack)
                            {
                                g.FillRectangle(blackBrush, startX + x * qrSize, startY + y * qrSize, qrSize, qrSize);
                            }
                        }
                    }
                }
                
                // Add human readable text at bottom (centered)
                using (Font font = new Font("Courier New", 8))
                {
                    SizeF textSize = g.MeasureString(text, font);
                    float textX = (panelSize.Width - textSize.Width) / 2;
                    g.DrawString(text, font, blackBrush, textX, panelSize.Height - 20);
                }
            }
        }

        // Render a scannable preview using ZXing if available; fallback to simple pattern
        private void DrawBarcodePreview(Graphics graphics, Rectangle area, string text, string barcodeType)
        {
            try
            {
				// Prefer our built-in Code 128-B renderer for Code 128 variants to ensure scannability
				if (IsCode128Type(barcodeType))
				{
					using (Bitmap bmp = RenderCode128B(text, area.Width, Math.Max(40, area.Height - 2)))
					{
						if (bmp != null)
						{
							graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
							graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
							graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

							// Draw 1:1 centered (no scaling) to preserve sharp bars
							int dx = area.X + (area.Width - bmp.Width) / 2;
							int dy = area.Y + (area.Height - bmp.Height) / 2;
							graphics.FillRectangle(Brushes.White, area);
							graphics.DrawImageUnscaled(bmp, dx, dy);
							return;
						}
					}
				}

                using (Bitmap bmp = TryRenderWithZXing(text, barcodeType, area.Width, Math.Max(1, area.Height - 2)))
                {
                    if (bmp != null)
                    {
                        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                        graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

                        // Draw 1:1 centered (no scaling) to preserve sharp bars
                        int dx = area.X + (area.Width - bmp.Width) / 2;
                        int dy = area.Y + (area.Height - bmp.Height) / 2;
                        graphics.FillRectangle(Brushes.White, area);
                        graphics.DrawImageUnscaled(bmp, dx, dy);
                        return;
                    }
                }
            }
            catch
            {
                // Ignore and fallback
            }

            // Fallback: existing approximate pattern
            CreateBarcodePattern(graphics, new Size(area.Width, area.Height), text, barcodeType);
        }

		private bool IsCode128Type(string barcodeType)
		{
			if (string.IsNullOrEmpty(barcodeType)) return true;
			return barcodeType.IndexOf("Code 128", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		// Render Code 128 (Code Set B) to a bitmap with correct checksum and quiet zones.
		// Ensures 10-module quiet zones on both sides and 1:1 module rendering (no scaling) for scannability.
		private Bitmap RenderCode128B(string text, int maxWidth, int height)
		{
			if (maxWidth < 40 || height < 10) return null;
			if (string.IsNullOrEmpty(text)) text = " ";

			// Validate characters for Code Set B (ASCII 32..126 typical). Replace unsupported with space.
			List<int> dataCodes = new List<int>(text.Length);
			foreach (char c in text)
			{
				int ascii = (int)c;
				if (ascii < 32 || ascii > 126) ascii = 32;
				dataCodes.Add(ascii - 32); // Code B mapping
			}

			// Build full code sequence: StartB(104), data, checksum, Stop(106)
			List<int> codes = new List<int>();
			codes.Add(104); // Start Code B
			codes.AddRange(dataCodes);

			int checksum = 104; // start value * 1
			for (int i = 0; i < dataCodes.Count; i++)
			{
				checksum += dataCodes[i] * (i + 1);
			}
			checksum %= 103;
			codes.Add(checksum);
			codes.Add(106); // Stop

			int[][] patterns = CODE128_PATTERNS;

			// Calculate total modules including quiet zones (10 modules each side)
			int modules = 20; // quiet zones
			foreach (int code in codes)
			{
				var w = patterns[code];
				int sum = 0;
				for (int i = 0; i < w.Length; i++) sum += w[i];
				modules += sum;
			}

			// Choose module width so that total fits within maxWidth; prefer moduleWidth >= 2 when possible
			int moduleWidth = Math.Max(1, maxWidth / modules);
			if (moduleWidth <= 0) moduleWidth = 1;
			int totalWidth = modules * moduleWidth;
			while (totalWidth > maxWidth && moduleWidth > 1)
			{
				moduleWidth--;
				totalWidth = modules * moduleWidth;
			}

			// int barHeight = Math.Max(40, height - 2);
			int barHeight = 50;
			Bitmap bmp = new Bitmap(Math.Max(1, totalWidth), barHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			using (Graphics g = Graphics.FromImage(bmp))
			{
				g.Clear(Color.White);
				int x = 10 * moduleWidth; // left quiet zone
				bool drawBar = true; // start with bar
				foreach (int code in codes)
				{
					int[] w = patterns[code];
					for (int i = 0; i < w.Length; i++)
					{
						int run = w[i] * moduleWidth;
						if (drawBar)
						{
							g.FillRectangle(Brushes.Black, x, 0, run, barHeight);
						}
						x += run;
						drawBar = !drawBar;
					}
				}
				// Right quiet zone automatically remains white
			}
			return bmp;
		}

		// Code128 pattern table (module widths). 107 entries (0..106).
		// Stop (index 106) includes 7 widths (13 modules). Others have 6 widths (11 modules).
		private static readonly int[][] CODE128_PATTERNS = new int[][]
		{
			new[]{2,1,2,2,2,2}, new[]{2,2,2,1,2,2}, new[]{2,2,2,2,2,1}, new[]{1,2,1,2,2,3}, new[]{1,2,1,3,2,2},
			new[]{1,3,1,2,2,2}, new[]{1,2,2,2,1,3}, new[]{1,2,2,3,1,2}, new[]{1,3,2,2,1,2}, new[]{2,2,1,2,1,3},
			new[]{2,2,1,3,1,2}, new[]{2,3,1,2,1,2}, new[]{1,1,2,2,3,2}, new[]{1,2,2,1,3,2}, new[]{1,2,2,2,3,1},
			new[]{1,1,3,2,2,2}, new[]{1,2,3,1,2,2}, new[]{1,2,3,2,2,1}, new[]{2,2,3,2,1,1}, new[]{2,2,1,1,3,2},
			new[]{2,2,1,2,3,1}, new[]{2,1,3,2,1,2}, new[]{2,2,3,1,1,2}, new[]{3,1,2,1,3,1}, new[]{3,1,1,2,2,2},
			new[]{3,2,1,1,2,2}, new[]{3,2,1,2,2,1}, new[]{3,1,2,2,1,2}, new[]{3,2,2,1,1,2}, new[]{3,2,2,2,1,1},
			new[]{2,1,2,1,2,3}, new[]{2,1,2,3,2,1}, new[]{2,3,2,1,2,1}, new[]{1,1,1,3,2,3}, new[]{1,3,1,1,2,3},
			new[]{1,3,1,3,2,1}, new[]{1,1,2,3,1,3}, new[]{1,3,2,1,1,3}, new[]{1,3,2,3,1,1}, new[]{2,1,1,3,1,3},
			new[]{2,3,1,1,1,3}, new[]{2,3,1,3,1,1}, new[]{1,1,2,1,3,3}, new[]{1,1,2,3,3,1}, new[]{1,3,2,1,3,1},
			new[]{1,1,3,1,2,3}, new[]{1,1,3,3,2,1}, new[]{1,3,3,1,2,1}, new[]{3,1,3,1,2,1}, new[]{2,1,1,3,3,1},
			new[]{2,3,1,1,3,1}, new[]{2,1,3,1,1,3}, new[]{2,1,3,3,1,1}, new[]{2,1,3,1,3,1}, new[]{3,1,1,1,2,3},
			new[]{3,1,1,3,2,1}, new[]{3,3,1,1,2,1}, new[]{3,1,2,1,1,3}, new[]{3,1,2,3,1,1}, new[]{3,3,2,1,1,1},
			new[]{3,1,4,1,1,1}, new[]{2,2,1,4,1,1}, new[]{4,3,1,1,1,1}, new[]{1,1,1,2,2,4}, new[]{1,1,1,4,2,2},
			new[]{1,2,1,1,2,4}, new[]{1,2,1,4,2,1}, new[]{1,4,1,1,2,2}, new[]{1,4,1,2,2,1}, new[]{1,1,2,2,1,4},
			new[]{1,1,2,4,1,2}, new[]{1,2,2,1,1,4}, new[]{1,2,2,4,1,1}, new[]{1,4,2,1,1,2}, new[]{1,4,2,2,1,1},
			new[]{2,4,1,2,1,1}, new[]{2,2,1,1,1,4}, new[]{4,1,3,1,1,1}, new[]{2,4,1,1,1,2}, new[]{1,3,4,1,1,1},
			new[]{1,1,1,2,4,2}, new[]{1,2,1,1,4,2}, new[]{1,2,1,2,4,1}, new[]{1,1,4,2,1,2}, new[]{1,2,4,1,1,2},
			new[]{1,2,4,2,1,1}, new[]{4,1,1,2,1,2}, new[]{4,2,1,1,1,2}, new[]{4,2,1,2,1,1}, new[]{2,1,2,1,4,1},
			new[]{2,1,4,1,2,1}, new[]{4,1,2,1,2,1}, new[]{1,1,1,1,4,3}, new[]{1,1,1,3,4,1}, new[]{1,3,1,1,4,1},
			new[]{1,1,4,1,1,3}, new[]{1,1,4,3,1,1}, new[]{4,1,1,1,1,3}, new[]{4,1,1,3,1,1}, new[]{1,1,3,1,4,1},
			new[]{1,1,4,1,3,1}, new[]{3,1,1,1,4,1}, new[]{4,1,1,1,3,1}, new[]{2,1,1,4,1,2}, new[]{2,1,1,2,1,4},
			new[]{2,1,1,2,3,2}, new[]{2,3,3,1,1,1,2}
		};

        // Attempts to use ZXing without adding a hard reference. Returns a Bitmap or null.
        private Bitmap TryRenderWithZXing(string text, string barcodeType, int width, int height)
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                System.Reflection.Assembly zxingAsm = null;
                foreach (var asm in assemblies)
                {
                    var name = asm.GetName().Name;
                    if (string.Equals(name, "ZXing", StringComparison.OrdinalIgnoreCase) ||
                        (name != null && name.StartsWith("ZXing", StringComparison.OrdinalIgnoreCase)))
                    {
                        zxingAsm = asm; break;
                    }
                }
                if (zxingAsm == null) return null;

                // Prefer BarcodeWriterPixelData so we can control output regardless of platform
                var writerPixelDataType = zxingAsm.GetType("ZXing.BarcodeWriterPixelData");
                var barcodeFormatType = zxingAsm.GetType("ZXing.BarcodeFormat");
                var encodingOptionsType = zxingAsm.GetType("ZXing.Common.EncodingOptions");
                if (writerPixelDataType == null || barcodeFormatType == null || encodingOptionsType == null) return null;

                object writer = Activator.CreateInstance(writerPixelDataType);

                // Set Format
                string enumName = MapBarcodeTypeToZxingFormatName(barcodeType);
                object formatValue = Enum.Parse(barcodeFormatType, enumName, true);
                var formatProp = writerPixelDataType.GetProperty("Format");
                formatProp.SetValue(writer, formatValue, null);

                // Options
                object options = Activator.CreateInstance(encodingOptionsType);
                // Enforce sensible minimums so bars are resolvable
                int zxWidth = Math.Max(160, width);
                int zxHeight = Math.Max(60, height);
                encodingOptionsType.GetProperty("Width").SetValue(options, zxWidth, null);
                encodingOptionsType.GetProperty("Height").SetValue(options, zxHeight, null);
                var marginProp = encodingOptionsType.GetProperty("Margin");
                if (marginProp != null) marginProp.SetValue(options, 10, null); // quiet zones
                var pureProp = encodingOptionsType.GetProperty("PureBarcode");
                if (pureProp != null) pureProp.SetValue(options, true, null);

                var optionsProp = writerPixelDataType.GetProperty("Options");
                optionsProp.SetValue(writer, options, null);

                // Write
                var writeMethod = writerPixelDataType.GetMethod("Write", new Type[] { typeof(string) });
                object pixelData = writeMethod.Invoke(writer, new object[] { text });
                if (pixelData == null) return null;

                // Extract pixel data (RGBA)
                var pdType = pixelData.GetType();
                int pdWidth = (int)pdType.GetProperty("Width").GetValue(pixelData, null);
                int pdHeight = (int)pdType.GetProperty("Height").GetValue(pixelData, null);
                byte[] pixels = (byte[])pdType.GetProperty("Pixels").GetValue(pixelData, null);

                // Create bitmap
                var bmp = new Bitmap(pdWidth, pdHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                var bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, bmp.PixelFormat);
                try
                {
                    System.Runtime.InteropServices.Marshal.Copy(pixels, 0, bmpData.Scan0, pixels.Length);
                }
                finally
                {
                    bmp.UnlockBits(bmpData);
                }
                return bmp;
            }
            catch
            {
                return null;
            }
        }

        private string MapBarcodeTypeToZxingFormatName(string barcodeType)
        {
            if (string.IsNullOrEmpty(barcodeType)) return "CODE_128";
            switch (barcodeType)
            {
                case "QR Code": return "QR_CODE";
                case "Code 128 UCC Serial Shipping Container Code": return "CODE_128";
                case "Code 128 auto A, B, C modes": return "CODE_128";
                case "Code 128 mode A": return "CODE_128";
                case "Code 128 mode B": return "CODE_128";
                case "Code 128 mode C": return "CODE_128";
                case "EAN-13": return "EAN_13";
                case "EAN-8": return "EAN_8";
                case "UPC-A": return "UPC_A";
                case "UPC-E": return "UPC_E";
                case "Code 39 std. or extended": return "CODE_39";
                case "Code 93": return "CODE_93";
                case "Interleaved 2 of 5": return "ITF";
                case "Codabar": return "CODABAR";
                case "Data Matrix": return "DATA_MATRIX";
                case "Aztec Code": return "AZTEC";
                case "PDF417": return "PDF_417";
                default: return "CODE_128";
            }
        }

        private void button_send_Click(object sender, EventArgs e)
        {
            // Show progress and update UI
            button_send.Enabled = false;
            button_send.Text = "🔄 Processing...";
            progressBar.Visible = true; // keep visible as a thin status strip
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
                    // case "PPLZ":
                    //     PPLZ_ItemList[comboBox_test.SelectedIndex].Function(printcount);
                    //     break;
                }
                
                // Success feedback with advanced settings summary
                string successMessage = "✅ Print job completed successfully!";
                if (checkBox_showAdvanced.Checked)
                {
                    successMessage += $"\n🎯 Advanced settings were applied: {textBox_EmployeeID.Text}, {numericUpDown_width.Value}x{numericUpDown_height.Value}";
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
                progressBar.Style = ProgressBarStyle.Continuous; // remain visible but not animating
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
                    // case "PPLZ":
                    //     PPLZEmulation = new PPLZ();
                    //     BarcodePrinter.AddEmulation(PPLZEmulation);
                    //     break;
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
            byte[] buf;
            byte[] buf2;
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
                        
                        // Validate and auto-correct parameters according to ARGOX SDK constraints
                        string corrections = "";
                        if (labelWidthDots < 2)
                        {
                            corrections += $"Width corrected from {labelWidthDots} to 2 pixels. ";
                            labelWidthDots = 2;
                        }
                        if (labelHeightDots < 1)
                        {
                            corrections += $"Height corrected from {labelHeightDots} to 1 pixel. ";
                            labelHeightDots = 1;
                        }
                        else if (labelHeightDots > 32000)
                        {
                            corrections += $"Height corrected from {labelHeightDots} to 32000 pixels. ";
                            labelHeightDots = 32000;
                        }
                        if (gapMM < 16)
                        {
                            corrections += $"Gap corrected from {gapMM} to 16 pixels (minimum required). ";
                            gapMM = 16;
                        }
                        else if (gapMM > 600)
                        {
                            corrections += $"Gap corrected from {gapMM} to 600 pixels (maximum allowed). ";
                            gapMM = 600;
                        }
                        
                        // Show corrections to user if any were made
                        if (!string.IsNullOrEmpty(corrections))
                        {
                            statusLabel.Text = $"⚠️ Parameter corrections: {corrections}";
                            statusLabel.ForeColor = System.Drawing.Color.FromArgb(255, 193, 7); // Warning color
                        }
                        
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
                    
                    // Use custom Employee ID from textBox_EmployeeID and Product ID from comboBox_ProductID (always check, not just when advanced settings are on)
                    string fullEmployeeID = !string.IsNullOrWhiteSpace(textBox_EmployeeID.Text) ? textBox_EmployeeID.Text : "Tan01";
                    string EmployeeID = fullEmployeeID.Length > 10 ? fullEmployeeID.Substring(fullEmployeeID.Length - 10) : fullEmployeeID;

            string ProductID = (comboBox_ProductID.SelectedItem?.ToString() ?? "000").PadLeft(3, '0');
            string CropID = (comboBox_CropID.SelectedItem?.ToString() ?? "000").PadLeft(3, '0');

                    //BarcodeID = 'EmployeeID'|'ProductID'
            string BarcodeID = $"{EmployeeID}{ProductID}{CropID}";

                    buf = encoder.GetBytes(BarcodeID);
                    
                    // Calculate text layout based on width constraints
                    int labelWidth = checkBox_showAdvanced.Checked ? (int)numericUpDown_width.Value : 200;
                    var (textLines, textFont, textSize) = CalculateTextLayout(BarcodeID, labelWidth);
                    
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
                        
                        // Force default rotation (0°)
                        orientation = PPLBOrient.Clockwise_0_Degrees;
                        
                        // Apply height setting
                        barcodeHeight = (int)numericUpDown_height.Value;
                    }
                    
                    // Calculate narrow bar width based on desired total barcode width
                    // Formula: narrowBarWidth = desiredWidth / (estimatedBarsCount * averageBarRatio)
                    int desiredWidth = checkBox_showAdvanced.Checked ? (int)numericUpDown_width.Value : 400;
                    int estimatedBarsPerChar = 11; // Average bars per character for Code 128
                    int estimatedTotalBars = BarcodeID.Length * estimatedBarsPerChar;
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
                    // Print requested number of copies using printer's copy mechanism
                    PPLBEmulation.SetUtil.SetPrintOut(Math.Max(1, printcount), 1);
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
            byte[] buf;
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
                
                // Validate and auto-correct parameters according to ARGOX SDK constraints
                string corrections = "";
                if (labelWidthDots < 2)
                {
                    corrections += $"Width corrected from {labelWidthDots} to 2 pixels. ";
                    labelWidthDots = 2;
                }
                if (labelHeightDots < 1)
                {
                    corrections += $"Height corrected from {labelHeightDots} to 1 pixel. ";
                    labelHeightDots = 1;
                }
                else if (labelHeightDots > 32000)
                {
                    corrections += $"Height corrected from {labelHeightDots} to 32000 pixels. ";
                    labelHeightDots = 32000;
                }
                if (gapMM < 16)
                {
                    corrections += $"Gap corrected from {gapMM} to 16 pixels (minimum required). ";
                    gapMM = 16;
                }
                else if (gapMM > 600)
                {
                    corrections += $"Gap corrected from {gapMM} to 600 pixels (maximum allowed). ";
                    gapMM = 600;
                }
                
                // Show corrections to user if any were made
                if (!string.IsNullOrEmpty(corrections))
                {
                    statusLabel.Text = $"⚠️ Parameter corrections: {corrections}";
                    statusLabel.ForeColor = System.Drawing.Color.FromArgb(255, 193, 7); // Warning color
                }
                
                PPLBEmulation.SetUtil.SetLabelLength(PPLBMediaTrack.Gap_Mode, labelHeightDots, gapMM);
                PPLBEmulation.SetUtil.SetPrintWidth(labelWidthDots);
                PPLBEmulation.SetUtil.SetHomePosition(5, 5); // 5-dot margin
                
                PPLBEmulation.SetUtil.SetClearImageBuffer();

                
                int stickerWidth = (int)numericUpDown_width.Value;
                int xCoordinate = (int)(numericUpDown_xCoordinate?.Value ?? 0);
                int x2Coordinate = (int)(numericUpDown_x2Coordinate?.Value ?? 0);
                
                // Custom header with settings info
                //If roll applied in center
                buf = encoder.GetBytes("!! Scan-Link !!");
                PPLBEmulation.TextUtil.PrintText(xCoordinate, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_4, 1, 1, false, buf);

                //If roll applied on x=0
                // buf = encoder.GetBytes("|| Scan-Link ||");
                // PPLBEmulation.TextUtil.PrintText(0, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_4, 1, 1, false, buf);


                // buf = encoder.GetBytes($"Custom Preset - {DateTime.Now:HH:mm:ss}");
                // buf = encoder.GetBytes($"{DateTime.Now:HH:mm:ss}");
                // PPLBEmulation.TextUtil.PrintText(540, 35, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_1, 1, 1, false, buf);
                
                // buf = encoder.GetBytes($"Settings: D{trackBar_darkness.Value} S{speedValue}");
                // PPLBEmulation.TextUtil.PrintText(30, 25, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_1, 1, 1, false, buf);
                
                // Use custom Employee ID and product ID
                string fullEmployeeID = !string.IsNullOrWhiteSpace(textBox_EmployeeID.Text) ? textBox_EmployeeID.Text : "";
                string EmployeeID = fullEmployeeID.Length > 10 ? fullEmployeeID.Substring(fullEmployeeID.Length - 10) : fullEmployeeID.PadLeft(10, '0');

                string ProductID = (comboBox_ProductID.SelectedItem?.ToString() ?? "000").PadLeft(3, '0');
                string CropID = (comboBox_CropID.SelectedItem?.ToString() ?? "000").PadLeft(3, '0');

                //BarcodeID = 'EmployeeID'|'ProductID'
                string BarcodeID = $"{EmployeeID}{ProductID}{CropID}";

                buf = encoder.GetBytes(BarcodeID);
                var bufBarcode = buf;
                
                // Calculate optimal text layout for the specified width
                int labelWidth = (int)numericUpDown_width.Value;
                var (textLines, textFont, textSize) = CalculateTextLayout(BarcodeID, labelWidth);
                
                // Print text layout information
                // buf2 = encoder.GetBytes($"Text Layout: {textLines.Length} line(s) for width {labelWidth}px");
                // PPLBEmulation.TextUtil.PrintText(30, 50, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_1, 1, 1, false, buf2);
                
                // Print each line of the optimized text
                // int textStartY = 65;
                // for (int i = 0; i < textLines.Length; i++)
                // {
                //     buf2 = encoder.GetBytes($"L{i + 1}: {textLines[i]}");
                //     PPLBEmulation.TextUtil.PrintText(30, textStartY + (i * 12), PPLBOrient.Clockwise_0_Degrees, textFont, textSize, textSize, false, buf2);
                // }
                
                // Apply rotation; ignore alignment (X is controlled via coordinates)
                int yPos = 80 + (textLines.Length * 12) + 10;
                
                PPLBOrient orientation = PPLBOrient.Clockwise_0_Degrees;
                // Force default rotation (0°)
                orientation = PPLBOrient.Clockwise_0_Degrees;
                
                // int barcodeHeight = (int)numericUpDown_height.Value;
                int barcodeHeight = 50;
                
                // Calculate narrow bar width for custom preset
                int desiredWidth = (int)numericUpDown_width.Value;
                int estimatedBarsPerChar = 11; // Average bars per character for Code 128
                int estimatedTotalBars = BarcodeID.Length * estimatedBarsPerChar;
                int narrowBarWidth = Math.Max(1, Math.Min(10, desiredWidth / estimatedTotalBars));
                if (narrowBarWidth < 1) narrowBarWidth = 1;
                if (narrowBarWidth > 10) narrowBarWidth = 10; // ARGOX SDK typically supports 1-10
                
                // Add width information to the label
                // buf2 = encoder.GetBytes($"BarW{narrowBarWidth} (Target:{desiredWidth}px)");
                // PPLBEmulation.TextUtil.PrintText(200, 25, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_1, 1, 1, false, buf2);

                
                // Print barcode with custom settings
                // buf2 = encoder.GetBytes($"Text: {BarcodeID}");
                // PPLBEmulation.TextUtil.PrintText(xPos, yPos, orientation, PPLBFont.Font_2, 1, 1, false, buf2);
                
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
                
                int stickerHeight = (int)numericUpDown_height.Value;

                bool twoUp = checkBox_twoUp != null && checkBox_twoUp.Checked;
                int remaining = Math.Max(1, printcount);
                int labelWidthTwoUp = (int)numericUpDown_width.Value;
                List<string> warnings = new List<string>();

                if (!twoUp)
                {
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(xCoordinate, stickerHeight-120, orientation, barcodeType, narrowBarWidth, 0, barcodeHeight, false, bufBarcode);
                buf = encoder.GetBytes($"EmployeeID: {EmployeeID}");
                    PPLBEmulation.TextUtil.PrintText(xCoordinate, stickerHeight-110+barcodeHeight, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_3, 1, 1, false, buf);
                buf = encoder.GetBytes($"ProductID:{ProductID} CropID:{CropID}");
                    PPLBEmulation.TextUtil.PrintText(xCoordinate, stickerHeight-75+barcodeHeight, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_3, 1, 1, false, buf);
                    PPLBEmulation.SetUtil.SetPrintOut(remaining, 1);
                    PPLBEmulation.IOUtil.PrintOut();
                }
                else
                {
                    if (xCoordinate < 0 || xCoordinate > labelWidthTwoUp) warnings.Add($"Left X ({xCoordinate}) out of bounds");
                    if (x2Coordinate < 0 || x2Coordinate > labelWidthTwoUp) warnings.Add($"Right X ({x2Coordinate}) out of bounds");

                    while (remaining > 0)
                    {
                        PPLBEmulation.SetUtil.SetClearImageBuffer();

                        // left
                        buf = encoder.GetBytes("!! Scan-Link !!");
                        PPLBEmulation.TextUtil.PrintText(xCoordinate, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_4, 1, 1, false, buf);
                        PPLBEmulation.BarcodeUtil.PrintOneDBarcode(xCoordinate, stickerHeight-120, orientation, barcodeType, narrowBarWidth, 0, barcodeHeight, false, bufBarcode);
                        buf = encoder.GetBytes($"EmployeeID: {EmployeeID}");
                        PPLBEmulation.TextUtil.PrintText(xCoordinate, stickerHeight-110+barcodeHeight, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_3, 1, 1, false, buf);
                        buf = encoder.GetBytes($"ProductID:{ProductID} CropID:{CropID}");
                        PPLBEmulation.TextUtil.PrintText(xCoordinate, stickerHeight-75+barcodeHeight, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_3, 1, 1, false, buf);

                        // right (if any remaining)
                        if (remaining > 1)
                        {
                            buf = encoder.GetBytes("!! Scan-Link !!");
                            PPLBEmulation.TextUtil.PrintText(x2Coordinate, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_4, 1, 1, false, buf);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(x2Coordinate, stickerHeight-120, orientation, barcodeType, narrowBarWidth, 0, barcodeHeight, false, bufBarcode);
                            buf = encoder.GetBytes($"EmployeeID: {EmployeeID}");
                            PPLBEmulation.TextUtil.PrintText(x2Coordinate, stickerHeight-110+barcodeHeight, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_3, 1, 1, false, buf);
                            buf = encoder.GetBytes($"ProductID:{ProductID} CropID:{CropID}");
                            PPLBEmulation.TextUtil.PrintText(x2Coordinate, stickerHeight-75+barcodeHeight, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_3, 1, 1, false, buf);
                        }

                        PPLBEmulation.SetUtil.SetPrintOut(1, 1);
                        PPLBEmulation.IOUtil.PrintOut();
                        remaining -= Math.Min(2, remaining);
                    }

                    if (warnings.Count > 0)
                    {
                        statusLabel.Text = "⚠️ " + string.Join("; ", warnings);
                        statusLabel.ForeColor = System.Drawing.Color.FromArgb(255, 193, 7);
                    }
                }
                
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

        // PPLZ PPLZEmulation;
        // private void __testPPLZ_calibrate(int printcount)
        // {
        //     int index = -1;
        //     if (false == __createPrn("PPLZ_calibrate.txt", ++index)) return;
        //     try 
        //     { 
        //         PPLZEmulation.SetUtil.SetMediaCalibration(); 
        //         PPLZEmulation.IOUtil.PrintOut(); 
        //     }
        //     catch (Exception ex) 
        //     { 
        //         ShowException.Show(this.Name, "__testPPLZ_calibrate", ex); 
        //     }
        //     finally 
        //     { 
        //         BarcodePrinter.Connection.Close(); 
        //     }
        // }

        // private void __testPPLZ_set1(int printcount)
        // {
        //     int index = -1;
        //     if (false == __createPrn("PPLZ_set1.txt", ++index)) return;
        //     try 
        //     { 
        //         PPLZEmulation.SetUtil.SetReset(); 
        //         PPLZEmulation.IOUtil.PrintOut(); 
        //     }
        //     catch (Exception ex) 
        //     { 
        //         ShowException.Show(this.Name, "__testPPLZ_set1", ex); 
        //     }
        //     finally 
        //     { 
        //         BarcodePrinter.Connection.Close(); 
        //     }
        // }

        private void printerButton_Click(object sender, EventArgs e)
        {
            startPanel.Visible = false;
            printerContentPanel.Visible = true;
            scannerContentPanel.Visible = false;
            InitializePrinterUI(); // Call the new initialization method
            
            // Apply layout to ensure new UI is shown
            LayoutRootPanels();
        }

        private void scannerButton_Click(object sender, EventArgs e)
        {
            startPanel.Visible = false;
            printerContentPanel.Visible = false;
            scannerContentPanel.Visible = true;
            
            // Initialize the scanner data grid view
            InitializeScannerDataGridView();
            LoadScansData();
            StartScanFileMonitoring();
            
            // Initialize COM port scanners
            InitializeComPortScanners();
            
            // Ensure layout positions header buttons above outputs and maximize widths
            LayoutRootPanels();
        }

        private void InitializeScannerDataGridView()
        {
            // Style the grid (columns will be set by pagination system)
            scannerDataGridView.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
            scannerDataGridView.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(50, 74, 95);
            scannerDataGridView.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.White;
            
            // Set AutoSizeColumnsMode to prevent empty columns
            scannerDataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            scannerDataGridView.AllowUserToAddRows = false;
            scannerDataGridView.AllowUserToDeleteRows = false;
            scannerDataGridView.ReadOnly = true;
        }

        private void LoadScansData()
        {
            try
            {
                // Prefer ProgramData scans file; fallback to bin root then project file
                string programDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ScanLink");
                string programDataScansFile = Path.Combine(programDataDir, "scans.txt");
                string binScansFile = Path.Combine(Application.StartupPath, "scans.txt");
                string projectRoot = Path.GetFullPath(Path.Combine(Application.StartupPath, "..", "..", ".."));
                string projectScansFile = Path.Combine(projectRoot, "ScanLink", "ScanLinkScanner", "scans.txt");
                string scansFilePath = File.Exists(programDataScansFile)
                    ? programDataScansFile
                    : (File.Exists(binScansFile) ? binScansFile : projectScansFile);

                if (!File.Exists(scansFilePath))
                {
                    InitializePagination(null);
                    return;
                }

                // Read the file
                string jsonContent = File.ReadAllText(scansFilePath);

                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    InitializePagination(null);
                    return;
                }

                // Parse JSON array using JavaScriptSerializer
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                var scansArray = serializer.Deserialize<List<Dictionary<string, object>>>(jsonContent);

                if (scansArray == null || scansArray.Count == 0)
                {
                    InitializePagination(null);
                    return;
                }

                // Create DataTable for all scanner data
                DataTable allData = new DataTable();
                allData.Columns.Add("Date", typeof(string));
                allData.Columns.Add("Time", typeof(string));
                allData.Columns.Add("SerialNumber", typeof(string));
                allData.Columns.Add("LineNumber", typeof(string));
                allData.Columns.Add("BlockNumber", typeof(string));
                allData.Columns.Add("ProductID", typeof(string));
                allData.Columns.Add("CropID", typeof(string));
                allData.Columns.Add("EmployeeID", typeof(string));

                // Get all entries (reverse order for latest first)
                var allScans = scansArray.AsEnumerable().Reverse().ToList();

                // Populate the DataTable
                foreach (var scan in allScans)
                {
                    string date = "";
                    string time = "";
                    string serial = "";
                    string blockId = "";
                    string lineId = "";
                    string productId = "";
                    string cropId = "";
                    string employeeId = "";

                    // Extract device data
                    if (scan.ContainsKey("device") && scan["device"] is Dictionary<string, object> device)
                    {
                        if (device.ContainsKey("serial"))
                            serial = device["serial"]?.ToString() ?? "";
                        if (device.ContainsKey("blockID"))
                            blockId = device["blockID"]?.ToString() ?? "";
                        if (device.ContainsKey("lineID"))
                            lineId = device["lineID"]?.ToString() ?? "";
                    }

                    // Extract product and employee IDs
                    if (scan.ContainsKey("productId"))
                        productId = scan["productId"]?.ToString() ?? "";
                    // If future scans include cropId, capture it; otherwise leave empty
                    if (scan.ContainsKey("cropId"))
                        cropId = scan["cropId"]?.ToString() ?? "";
                    if (scan.ContainsKey("employeeId"))
                        employeeId = scan["employeeId"]?.ToString() ?? "";
                    // Fallback parsing from concatenated barcode if fields are missing
                    if ((string.IsNullOrEmpty(employeeId) || string.IsNullOrEmpty(productId) || string.IsNullOrEmpty(cropId))
                        && scan.ContainsKey("barcode") && scan["barcode"] != null)
                    {
                        string code = scan["barcode"].ToString();
                        if (!string.IsNullOrEmpty(code))
                        {
                            code = code.Trim();
                            if (code.StartsWith("]") && code.Length >= 3)
                            {
                                // Strip leading AIM Symbology Identifier such as "]C1"
                                code = code.Substring(3);
                            }
                        }
                        if (!string.IsNullOrEmpty(code) && code.Length >= 16)
                        {
                            if (string.IsNullOrEmpty(employeeId) && code.Length >= 10)
                            {
                                employeeId = code.Substring(0, 10);
                            }
                            if (string.IsNullOrEmpty(productId) && code.Length >= 13)
                            {
                                productId = code.Substring(10, 3);
                            }
                            if (string.IsNullOrEmpty(cropId) && code.Length >= 16)
                            {
                                cropId = code.Substring(13, 3);
                            }
                        }
                    }
                    if (scan.ContainsKey("date"))
                        date = scan["date"]?.ToString() ?? "";
                    if (scan.ContainsKey("time"))
                        time = scan["time"]?.ToString() ?? "";

                    allData.Rows.Add(date, time, serial, blockId, lineId, productId, cropId, employeeId);
                }

                // Initialize pagination with all data
                InitializePagination(allData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading scans data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                InitializePagination(null);
            }
        }

        private void StartScanFileMonitoring()
        {
            try
            {
                // Prefer ProgramData directory; fallback to bin root then project directory
                string programDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ScanLink");
                string binScansDirectory = Application.StartupPath;
                string projectRoot = Path.GetFullPath(Path.Combine(Application.StartupPath, "..", "..", ".."));
                string projectScansDirectory = Path.Combine(projectRoot, "ScanLink", "ScanLinkScanner");
                string scansDirectory = Directory.Exists(programDataDir)
                    ? programDataDir
                    : (Directory.Exists(binScansDirectory) ? binScansDirectory : projectScansDirectory);

                // Stop existing watcher if any
                if (_scansFileWatcher != null)
                {
                    _scansFileWatcher.EnableRaisingEvents = false;
                    _scansFileWatcher.Dispose();
                }

                // Stop existing timers if any
                if (_scanRefreshTimer != null)
                {
                    _scanRefreshTimer.Stop();
                    _scanRefreshTimer.Dispose();
                }

                if (_fileChangeDebounceTimer != null)
                {
                    _fileChangeDebounceTimer.Stop();
                    _fileChangeDebounceTimer.Dispose();
                }

                // Create debounce timer for file changes
                _fileChangeDebounceTimer = new System.Windows.Forms.Timer();
                _fileChangeDebounceTimer.Interval = 500; // 500ms debounce
                _fileChangeDebounceTimer.Tick += (s, e) => 
                {
                    _fileChangeDebounceTimer.Stop();
                    // Only reload if scanner panel is visible
                    if (scannerContentPanel.Visible)
                    {
                        if (scannerOutputTextBox != null)
                        {
                            string timestamp = DateTime.Now.ToString("HH:mm:ss");
                            scannerOutputTextBox.AppendText($"[{timestamp}] 📄 Scans file updated - Refreshing table...\r\n");
                            scannerOutputTextBox.ScrollToCaret();
                        }
                        LoadScansData();
                    }
                };

                // Only set up file watcher for updates when file actually changes
                if (Directory.Exists(scansDirectory))
                {
                    _scansFileWatcher = new FileSystemWatcher(scansDirectory, "scans.txt");
                    _scansFileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
                    _scansFileWatcher.Changed += (s, e) => 
                    {
                        // Debounce the file change event to prevent multiple rapid updates
                        if (this.InvokeRequired)
                        {
                            this.BeginInvoke(new Action(() => 
                            {
                                _fileChangeDebounceTimer.Stop();
                                _fileChangeDebounceTimer.Start();
                            }));
                        }
                        else
                        {
                            _fileChangeDebounceTimer.Stop();
                            _fileChangeDebounceTimer.Start();
                        }
                    };
                    _scansFileWatcher.EnableRaisingEvents = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting up file monitoring: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // private async void runScannerScriptButton_Click(object sender, EventArgs e)
        // {
        //     scannerOutputTextBox.Clear();
        //     scannerOutputTextBox.AppendText("Starting scanner script...\r\n");
        //     runScannerScriptButton.Enabled = false;
        //     barcodeInputTextBox.Enabled = false;
        //     sendBarcodeButton.Enabled = false;

        //     try
        //     {
        //         // Use relative path from application startup directory
        //         string scriptPath = Path.Combine(Application.StartupPath, "ScanLinkScanner", "scan_capture.ps1");
                
        //         var processInfo = new ProcessStartInfo("powershell.exe")
        //         {
        //             RedirectStandardOutput = true,
        //             RedirectStandardError = true,
        //             RedirectStandardInput = true, // Enable standard input redirection
        //             UseShellExecute = false,
        //             CreateNoWindow = true,
        //             Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\""
        //         };

        //         _scannerProcess = Process.Start(processInfo);

        //         if (_scannerProcess == null) return;
                    
        //         // Enable input controls now that the process is ready for input
        //         barcodeInputTextBox.Enabled = true;
        //         sendBarcodeButton.Enabled = true;

        //         // Handle process exit
        //         _scannerProcess.EnableRaisingEvents = true;
        //         _scannerProcess.Exited += (s, args) =>
        //         {
        //             this.Invoke((MethodInvoker)delegate
        //             {
        //                 scannerOutputTextBox.AppendText("Script execution finished.\r\n");
        //                 runScannerScriptButton.Enabled = true;
        //                 barcodeInputTextBox.Enabled = false;
        //                 sendBarcodeButton.Enabled = false;
        //                 _scannerProcess = null; // Clear the process reference
        //             });
        //         };

        //         // Read output asynchronously
        //         _ = Task.Run(async () =>
        //         {
        //             while (!_scannerProcess.StandardOutput.EndOfStream)
        //             {
        //                 string line = await _scannerProcess.StandardOutput.ReadLineAsync();
        //                 this.Invoke((MethodInvoker)delegate
        //                 {
        //                     scannerOutputTextBox.AppendText(line + "\r\n");
        //                 });
        //             }
        //         });

        //         _ = Task.Run(async () =>
        //         {
        //             while (!_scannerProcess.StandardError.EndOfStream)
        //             {
        //                 string line = await _scannerProcess.StandardError.ReadLineAsync();
        //                 this.Invoke((MethodInvoker)delegate
        //                 {
        //                     scannerOutputTextBox.AppendText("Error: " + line + "\r\n");
        //                     scannerOutputTextBox.ForeColor = Color.Red;
        //                 });
        //             }
        //         });
        //     }
        //     catch (Exception ex)
        //     {
        //         scannerOutputTextBox.AppendText($"Failed to run script: {ex.Message}\r\n");
        //         scannerOutputTextBox.ForeColor = Color.Red;
        //         runScannerScriptButton.Enabled = true;
        //         barcodeInputTextBox.Enabled = false;
        //         sendBarcodeButton.Enabled = false;
        //     }
        // }

        // private void sendBarcodeButton_Click(object sender, EventArgs e)
        // {
        //     if (_scannerProcess != null && !_scannerProcess.HasExited)
        //     {
        //         string barcode = barcodeInputTextBox.Text.Trim();
        //         if (!string.IsNullOrEmpty(barcode))
        //         {
        //             _scannerProcess.StandardInput.WriteLine(barcode);
        //             // scannerOutputTextBox.AppendText($"> Sent: {barcode}\r\n"); // Removed this line
        //             barcodeInputTextBox.Clear();
        //         }
        //     }
        // }

        // private void barcodeInputTextBox_KeyDown(object sender, KeyEventArgs e)
        // {
        //     if (e.KeyCode == Keys.Enter)
        //     {
        //         sendBarcodeButton_Click(sender, e);
        //         e.Handled = true; // Prevent beep sound
        //         e.SuppressKeyPress = true; // Prevent further processing of the key
        //     }
        // }

        private void manageScannersButton_Click(object sender, EventArgs e)
        {
            // Open the scanner management form
			ScannerManagementForm scannerManagementForm = new ScannerManagementForm();
			scannerManagementForm.ScannersSaved += (s, args) =>
			{
				if (scannerOutputTextBox != null)
				{
					string ts = DateTime.Now.ToString("HH:mm:ss");
					scannerOutputTextBox.AppendText($"[{ts}] [C# INFO] Scanner configuration saved — reinitializing scanners...\r\n");
					scannerOutputTextBox.ScrollToCaret();
				}
				_scannerComPortManager?.CloseAllScanners();
				InitializeComPortScanners();
			};
			scannerManagementForm.ShowDialog();
        }

        private async void button_manualUpload_Click(object sender, EventArgs e)
        {
            try
            {
                statusLabel.Text = "API: Uploading...";
                statusLabel.ForeColor = Color.DodgerBlue;

                var result = await _scanLogUploadService.UploadQueuedLogsManually();
                int ok = result.succeeded;
                int bad = result.failed;
                string lastErr = result.lastError;

                if (ok > 0 && bad == 0)
                {
                    statusLabel.Text = $"API: Uploaded {ok} log(s) successfully";
                    statusLabel.ForeColor = Color.Green;
                }
                else if (ok > 0 && bad > 0)
                {
                    statusLabel.Text = $"API: Uploaded {ok}, failed {bad}. Remaining kept.";
                    statusLabel.ForeColor = Color.Orange;
                }
                else
                {
                    statusLabel.Text = $"API: No uploads. {(string.IsNullOrEmpty(lastErr) ? "" : lastErr)}";
                    statusLabel.ForeColor = Color.OrangeRed;
                }

                // Refresh grid after possible changes
                LoadScansData();
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"API: Error - {ex.Message}";
                statusLabel.ForeColor = Color.Red;
            }
        }

        private void backToStartButton_Click(object sender, EventArgs e)
        {
            startPanel.Visible = true;
            printerContentPanel.Visible = false;
            scannerContentPanel.Visible = false;
            
            // Stop monitoring when leaving scanner panel
            if (_scanRefreshTimer != null)
            {
                _scanRefreshTimer.Stop();
            }
            if (_fileChangeDebounceTimer != null)
            {
                _fileChangeDebounceTimer.Stop();
            }
            if (_scansFileWatcher != null)
            {
                _scansFileWatcher.EnableRaisingEvents = false;
            }
        }

        private void Scanner_Click(object sender, EventArgs e)
        {
            startPanel.Visible = false;
            printerContentPanel.Visible = false;
            scannerContentPanel.Visible = true;
            
            // Initialize the scanner data grid view
            InitializeScannerDataGridView();
            LoadScansData();
            StartScanFileMonitoring();
            
            // Initialize COM port scanners
            InitializeComPortScanners();
            
            // Apply layout to ensure new UI is shown
            LayoutRootPanels();
        }

        private void Printer_Click(object sender, EventArgs e)
        {
            startPanel.Visible = false;
            printerContentPanel.Visible = true;
            scannerContentPanel.Visible = false;
            InitializePrinterUI(); // Call the new initialization method
            
            // Apply layout to ensure new UI is shown
            LayoutRootPanels();
        }

        private void logoutButton_Click(object sender, EventArgs e)
        {
            // Stop the scan log upload service
            _scanLogUploadService?.Stop();
            
            // Clear all cache memory (tokens, etc.)
            if (_apiAuthService != null)
            {
                _apiAuthService.ClearToken();
            }
            
            // Clear any other cached data
            // Clear scanner data if needed
            if (allScannerData != null && allScannerData.Rows.Count > 0)
            {
                allScannerData.Clear();
            }
            if (filteredScannerData != null && filteredScannerData.Rows.Count > 0)
            {
                filteredScannerData.Clear();
            }
            if (currentPageData != null && currentPageData.Rows.Count > 0)
            {
                currentPageData.Clear();
            }
            
            // Reset pagination
            currentPage = 1;
            totalPages = 1;
            
            // Clear scanner output
            scannerOutputTextBox.Clear();
            
            // Reset form fields
            usernameTextBox.Clear();
            passwordTextBox.Clear();
            textBox_EmployeeID.Clear();
            comboBox_ProductID.SelectedIndex = 0;
            
            // Hide all panels and show login panel
            startPanel.Visible = false;
            printerContentPanel.Visible = false;
            scannerContentPanel.Visible = false;
            loginPanel.Visible = true;
            
            // Reset login status
            loginStatusLabel.Text = "Please Login to access your account";
            loginStatusLabel.ForeColor = Color.Gray;
            
            // Reset login button
            loginButton.Enabled = true;
            loginButton.Text = "Login";
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            // Automatically start the scan script after the form is fully displayed
            // This ensures all controls are initialized and ready
            StartScanScript();
            LayoutRootPanels();
        }

        private void StartScanScript()
        {
            // Check if script is already running
            if (_scannerProcess != null && !_scannerProcess.HasExited)
            {
                return;
            }

            try
            {
                // Use path from application startup directory (bin root)
                string scannerDir = Application.StartupPath;
                string scriptPath = Path.Combine(scannerDir, "scan_capture.ps1");

                // Fallback: ensure bin script is present and up-to-date (copy from project if newer)
                bool needCopy = !File.Exists(scriptPath);
                string projectScannerDir = Path.GetFullPath(Path.Combine(Application.StartupPath, "..", "..", "..", "ScanLink", "ScanLinkScanner"));
                string sourceScript = Path.Combine(projectScannerDir, "scan_capture.ps1");
                if (!needCopy && File.Exists(sourceScript))
                {
                    try
                    {
                        var srcTime = File.GetLastWriteTimeUtc(sourceScript);
                        var dstTime = File.Exists(scriptPath) ? File.GetLastWriteTimeUtc(scriptPath) : DateTime.MinValue;
                        if (srcTime > dstTime) needCopy = true;
                    }
                    catch { }
                }
                if (needCopy)
                {
                    try
                    {
                        if (File.Exists(sourceScript))
                        {
                            Directory.CreateDirectory(scannerDir);
                            File.Copy(sourceScript, scriptPath, true);
                            // Also copy sibling helper files used by the script
                            string[] helperFiles = new[] { "scanner_detection.ps1", "scans.txt", "scanner_assignments.txt" };
                            foreach (var hf in helperFiles)
                            {
                                string src = Path.Combine(projectScannerDir, hf);
                                string dst = Path.Combine(scannerDir, hf);
                                if (File.Exists(src))
                                {
                                    try
                                    {
                                        bool copyHelper = true;
                                        try
                                        {
                                            var srcT = File.GetLastWriteTimeUtc(src);
                                            var dstT = File.Exists(dst) ? File.GetLastWriteTimeUtc(dst) : DateTime.MinValue;
                                            if (srcT <= dstT) copyHelper = false;
                                        }
                                        catch { }
                                        if (copyHelper) File.Copy(src, dst, true);
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                    catch { }
                }
                
                var processInfo = new ProcessStartInfo("powershell.exe")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\""
                };

                _scannerProcess = Process.Start(processInfo);

                if (_scannerProcess == null) return;

                // Handle process exit
                _scannerProcess.EnableRaisingEvents = true;
                _scannerProcess.Exited += (s, args) =>
                {
                    if (this.InvokeRequired)
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            _scannerProcess = null;
                        });
                    }
                    else
                    {
                        _scannerProcess = null;
                    }
                };

                // Read output asynchronously in background
                Task.Run(async () =>
                {
                    try
                    {
                        while (!_scannerProcess.StandardOutput.EndOfStream)
                        {
                            string line = await _scannerProcess.StandardOutput.ReadLineAsync();
                            if (!string.IsNullOrEmpty(line))
                            {
                                this.Invoke((MethodInvoker)delegate
                                {
                                    if (scannerOutputTextBox.InvokeRequired)
                                    {
                                        scannerOutputTextBox.Invoke((MethodInvoker)delegate
                                        {
                                            scannerOutputTextBox.AppendText(line + "\r\n");
                                        });
                                    }
                                    else
                                    {
                                        scannerOutputTextBox.AppendText(line + "\r\n");
                                    }
                                });
                            }
                        }
                    }
                    catch { }
                });

                Task.Run(async () =>
                {
                    try
                    {
                        while (!_scannerProcess.StandardError.EndOfStream)
                        {
                            string line = await _scannerProcess.StandardError.ReadLineAsync();
                            if (!string.IsNullOrEmpty(line))
                            {
                                this.Invoke((MethodInvoker)delegate
                                {
                                    if (scannerOutputTextBox.InvokeRequired)
                                    {
                                        scannerOutputTextBox.Invoke((MethodInvoker)delegate
                                        {
                                            scannerOutputTextBox.AppendText("Error: " + line + "\r\n");
                                        });
                                    }
                                    else
                                    {
                                        scannerOutputTextBox.AppendText("Error: " + line + "\r\n");
                                    }
                                });
                            }
                        }
                    }
                    catch { }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start scan script: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeComPortScanners()
        {
            // IMMEDIATELY write to textbox to prove this method is being called
            if (scannerOutputTextBox != null)
            {
                scannerOutputTextBox.AppendText($"\r\n");
                scannerOutputTextBox.AppendText($"╔════════════════════════════════════════════╗\r\n");
                scannerOutputTextBox.AppendText($"║  C# SCANNER INITIALIZATION STARTING...    ║\r\n");
                scannerOutputTextBox.AppendText($"╚════════════════════════════════════════════╝\r\n");
                scannerOutputTextBox.ScrollToCaret();
            }
            else
            {
                Debug.WriteLine("[INIT ERROR] scannerOutputTextBox is NULL!");
            }
            
            try
            {
                Debug.WriteLine("[INIT] Starting COM port scanner initialization...");
                
                // Detect all COM port scanners
                var detectedScanners = ComPortScannerDetection.DetectComPortScanners();
                
                Debug.WriteLine($"[INIT] Detected {detectedScanners?.Count ?? 0} scanner(s)");
                
                if (scannerOutputTextBox != null)
                {
                    scannerOutputTextBox.AppendText($"[C# INFO] Detected {detectedScanners?.Count ?? 0} COM port scanner(s) from C# code\r\n");
                }
                
                if (detectedScanners == null || detectedScanners.Count == 0)
                {
                    Debug.WriteLine("[INIT] No COM port scanners detected");
                    if (scannerOutputTextBox != null)
                    {
                        scannerOutputTextBox.AppendText($"[C# WARNING] No COM port scanners detected by C#. Please configure scanners in Scanner Management.\r\n");
                    }
                    return;
                }
                
                // Load scanner assignments from file
                string assignmentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ScanLink", "scanner_assignments.txt");
                var assignments = LoadScannerAssignmentsFromFile(assignmentsPath);
                
                Debug.WriteLine($"[INIT] Loaded {assignments.Count} scanner assignment(s) from file");
                
                if (scannerOutputTextBox != null)
                {
                    scannerOutputTextBox.AppendText($"[C# INFO] Loaded {assignments.Count} scanner assignment(s) from file\r\n");
                    scannerOutputTextBox.AppendText($"[C# INFO] Assignment file: {assignmentsPath}\r\n");
                }
                
                // Open each detected scanner
                int successCount = 0;
                int failCount = 0;
                
                foreach (var scanner in detectedScanners)
                {
                    Debug.WriteLine($"[INIT] Processing scanner: {scanner.DeviceName} on {scanner.ComPort}");
                    
                    // Check if this scanner has saved configuration
                    if (assignments.ContainsKey(scanner.PNPDeviceID))
                    {
                        var assignment = assignments[scanner.PNPDeviceID];
                        scanner.LineID = assignment.LineID;
                        scanner.BlockID = assignment.BlockID;
                        scanner.BaudRate = assignment.BaudRate;
                        scanner.Parity = assignment.Parity;
                        scanner.DataBits = assignment.DataBits;
                        scanner.StopBits = assignment.StopBits;
                        Debug.WriteLine($"[INIT] Applied saved settings: Baud={scanner.BaudRate}, LineID={scanner.LineID}, BlockID={scanner.BlockID}");
                    }
                    else
                    {
                        Debug.WriteLine($"[INIT] No saved configuration found for {scanner.PNPDeviceID}, using defaults");
                    }
                    
                    // Open the scanner
                    bool opened = _scannerComPortManager.OpenScanner(scanner);
                    
                    if (opened)
                    {
                        successCount++;
                        Debug.WriteLine($"[INIT] ✓ Opened scanner: {scanner.DeviceName} on {scanner.ComPort}");
                        if (scannerOutputTextBox != null)
                        {
                            scannerOutputTextBox.AppendText($"✓ Connected: {scanner.DeviceName} ({scanner.ComPort}) - Line {scanner.LineID}, Block {scanner.BlockID}\r\n");
                        }
                    }
                    else
                    {
                        failCount++;
                        Debug.WriteLine($"[INIT] ✗ Failed to open scanner: {scanner.DeviceName} on {scanner.ComPort}");
                        if (scannerOutputTextBox != null)
                        {
                            scannerOutputTextBox.AppendText($"✗ Failed: {scanner.DeviceName} ({scanner.ComPort}) - Check COM port settings\r\n");
                        }
                    }
                }
                
                Debug.WriteLine($"[INIT] Initialization complete: {successCount} success, {failCount} failed");
                
                if (scannerOutputTextBox != null)
                {
                    scannerOutputTextBox.AppendText($"\r\n[INFO] Scanner initialization complete: {successCount} connected, {failCount} failed\r\n");
                    
                    // Check PowerShell process status
                    if (_scannerProcess != null && !_scannerProcess.HasExited)
                    {
                        scannerOutputTextBox.AppendText($"[INFO] ✓ PowerShell script is running and ready\r\n");
                    }
                    else
                    {
                        scannerOutputTextBox.AppendText($"[WARNING] ✗ PowerShell script is NOT running - scans will not be processed!\r\n");
                    }
                    
                    scannerOutputTextBox.AppendText($"[INFO] Ready to scan. Waiting for barcode input...\r\n\r\n");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[INIT ERROR] Error initializing COM port scanners: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Error initializing COM port scanners:\n{ex.Message}", "Scanner Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                
                if (scannerOutputTextBox != null)
                {
                    scannerOutputTextBox.AppendText($"[ERROR] Initialization failed: {ex.Message}\r\n");
                }
            }
        }
        
        private Dictionary<string, ScannerConfig> LoadScannerAssignmentsFromFile(string filePath)
        {
            var assignments = new Dictionary<string, ScannerConfig>();
            
            if (!File.Exists(filePath))
                return assignments;
                
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                ScannerConfig currentScanner = null;
                
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    
                    if (trimmed.StartsWith("PNPDeviceID:"))
                    {
                        currentScanner = new ScannerConfig();
                        currentScanner.PNPDeviceID = trimmed.Substring("PNPDeviceID:".Length).Trim();
                    }
                    else if (currentScanner != null)
                    {
                        if (trimmed.StartsWith("COM Port:"))
                        {
                            currentScanner.ComPort = trimmed.Substring("COM Port:".Length).Trim();
                        }
                        else if (trimmed.StartsWith("Line ID:"))
                        {
                            currentScanner.LineID = trimmed.Substring("Line ID:".Length).Trim();
                        }
                        else if (trimmed.StartsWith("Block ID:"))
                        {
                            currentScanner.BlockID = trimmed.Substring("Block ID:".Length).Trim();
                        }
                        else if (trimmed.StartsWith("Baud Rate:"))
                        {
                            currentScanner.BaudRate = int.Parse(trimmed.Substring("Baud Rate:".Length).Trim());
                        }
                        else if (trimmed.StartsWith("Parity:"))
                        {
                            currentScanner.Parity = ParseParity(trimmed.Substring("Parity:".Length).Trim());
                        }
                        else if (trimmed.StartsWith("Data Bits:"))
                        {
                            currentScanner.DataBits = int.Parse(trimmed.Substring("Data Bits:".Length).Trim());
                        }
                        else if (trimmed.StartsWith("Stop Bits:"))
                        {
                            currentScanner.StopBits = ParseStopBits(trimmed.Substring("Stop Bits:".Length).Trim());
                            
                            // Entry complete, add to dictionary
                            if (!string.IsNullOrEmpty(currentScanner.PNPDeviceID))
                            {
                                assignments[currentScanner.PNPDeviceID] = currentScanner;
                            }
                            currentScanner = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading scanner assignments: {ex.Message}");
            }
            
            return assignments;
        }
        
        private System.IO.Ports.Parity ParseParity(string parity)
        {
            switch (parity?.ToLower())
            {
                case "odd": return System.IO.Ports.Parity.Odd;
                case "even": return System.IO.Ports.Parity.Even;
                case "mark": return System.IO.Ports.Parity.Mark;
                case "space": return System.IO.Ports.Parity.Space;
                default: return System.IO.Ports.Parity.None;
            }
        }
        
        private System.IO.Ports.StopBits ParseStopBits(string stopBits)
        {
            switch (stopBits?.ToLower())
            {
                case "two": return System.IO.Ports.StopBits.Two;
                case "onepointfive": return System.IO.Ports.StopBits.OnePointFive;
                default: return System.IO.Ports.StopBits.One;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Stop and dispose the upload service
            _scanLogUploadService?.Dispose();
            
            // Dispose COM port scanner manager
            _scannerComPortManager?.Dispose();
            if (_scannerProcess != null && !_scannerProcess.HasExited)
            {
                try
                {
                    _scannerProcess.StandardInput.Close(); // Signal PowerShell to exit gracefully
                    _scannerProcess.WaitForExit(5000); // Give it up to 5 seconds to exit
                }
                catch (InvalidOperationException) { /* Process might have already exited */ }
                catch (Exception ex)
                {
                    // Log or handle any other exceptions during graceful shutdown
                    Debug.WriteLine($"Error during graceful scanner process shutdown: {ex.Message}");
                }
                finally
                {
                    if (_scannerProcess != null && !_scannerProcess.HasExited)
                    {
                        _scannerProcess.Kill(); // Force kill if it didn't exit gracefully
                        Debug.WriteLine("Scanner process force-killed.");
                    }
                    _scannerProcess?.Dispose();
                }
            }
            else
            {
                _scannerProcess?.Dispose();
            }
        }

        private void ScannerComPortManager_DataReceived(object sender, ScannerDataReceivedEventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                try
                {
                    string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    Debug.WriteLine($"[SCAN] Data received: {e.Data} from {e.Scanner.DeviceName} ({e.Scanner.ComPort})");
                    
                    // ALWAYS show in scanner output textbox with detailed info (even if panel not visible)
                    if (scannerOutputTextBox != null)
                    {
                        scannerOutputTextBox.AppendText($"\r\n");
                        scannerOutputTextBox.AppendText($"════════════════════════════════════════\r\n");
                        scannerOutputTextBox.AppendText($"[{timestamp}] 📥 SCAN RECEIVED\r\n");
                        scannerOutputTextBox.AppendText($"════════════════════════════════════════\r\n");
                        scannerOutputTextBox.AppendText($"    Port: {e.Scanner.ComPort}\r\n");
                        scannerOutputTextBox.AppendText($"    Data: {e.Data}\r\n");
                        scannerOutputTextBox.AppendText($"    Line ID: {e.Scanner.LineID ?? "Not Set"}\r\n");
                        scannerOutputTextBox.AppendText($"    Block ID: {e.Scanner.BlockID ?? "Not Set"}\r\n");
                    }
                    
                    // Process scanned barcode from COM port scanner
                if (_scannerProcess != null && !_scannerProcess.HasExited)
                {
                        // Send data to PowerShell script with scanner identification
                        // Format: "PNPDeviceID|BarcodeData"
                        string formattedData = $"{e.Scanner.PNPDeviceID}|{e.Data}";
                        
                        try
                        {
                            _scannerProcess.StandardInput.WriteLine(formattedData);
                            _scannerProcess.StandardInput.Flush();
                            
                            Debug.WriteLine($"[SCAN] Sent to PowerShell: {formattedData}");
                            
                            if (scannerContentPanel.Visible && scannerOutputTextBox != null)
                            {
                                scannerOutputTextBox.AppendText($"    ✓ Sent to PowerShell for processing\r\n\r\n");
                                scannerOutputTextBox.ScrollToCaret();
                            }
                        }
                        catch (Exception psEx)
                        {
                            Debug.WriteLine($"[SCAN ERROR] Failed to write to PowerShell: {psEx.Message}");
                            if (scannerContentPanel.Visible && scannerOutputTextBox != null)
                            {
                                scannerOutputTextBox.AppendText($"    ✗ ERROR: Failed to send to PowerShell: {psEx.Message}\r\n\r\n");
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"[SCAN ERROR] PowerShell process not running!");
                        if (scannerContentPanel.Visible && scannerOutputTextBox != null)
                        {
                            scannerOutputTextBox.AppendText($"    ✗ ERROR: PowerShell process not running!\r\n");
                            scannerOutputTextBox.AppendText($"    Action: Click Scanner Management to restart\r\n\r\n");
                            scannerOutputTextBox.ScrollToCaret();
                        }
                        // Attempt auto-recovery: start the script and retry once after a short delay
                        StartScanScript();
                        Task.Run(async () =>
                        {
                            await Task.Delay(750);
                            try
                            {
                                if (_scannerProcess != null && !_scannerProcess.HasExited)
                                {
                                    string formattedData = $"{e.Scanner.PNPDeviceID}|{e.Data}";
                                    _scannerProcess.StandardInput.WriteLine(formattedData);
                                    _scannerProcess.StandardInput.Flush();
                                }
                            }
                            catch { }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SCAN ERROR] Exception in data handler: {ex.Message}");
                    if (scannerContentPanel.Visible && scannerOutputTextBox != null)
                    {
                        scannerOutputTextBox.AppendText($"[ERROR] Exception: {ex.Message}\r\n\r\n");
                        scannerOutputTextBox.ScrollToCaret();
                    }
                }
            });
        }

        private void ScannerComPortManager_Error(object sender, ScannerErrorEventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                Debug.WriteLine($"Scanner error: {e.ErrorMessage} - Scanner: {e.Scanner.DeviceName}");
                
                // Optionally show error to user in scanner output
                if (scannerContentPanel.Visible && scannerOutputTextBox != null)
                {
                    scannerOutputTextBox.AppendText($"[ERROR] {e.Scanner.DeviceName}: {e.ErrorMessage}\r\n");
                }
            });
        }

        private void ScannerComPortManager_Log(object sender, ScannerLogEventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                System.Diagnostics.Debug.WriteLine($"[C# DEBUG] {e.Scanner?.ComPort}: {e.Message}");
                if (scannerOutputTextBox != null)
                {
                    scannerOutputTextBox.AppendText($"[{timestamp}] [C# DEBUG] {e.Scanner?.ComPort}: {e.Message}\r\n");
                    scannerOutputTextBox.ScrollToCaret();
                }
            });
        }

        private void ScanLogUploadService_LogMessage(object sender, string message)
        {
            // Log upload service messages to scanner output (only if scanner panel is visible)
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    if (scannerContentPanel.Visible && scannerOutputTextBox != null)
                    {
                        scannerOutputTextBox.AppendText(message + "\r\n");
                    }
                    System.Diagnostics.Debug.WriteLine(message);
                });
            }
            else
            {
                if (scannerContentPanel.Visible && scannerOutputTextBox != null)
                {
                    scannerOutputTextBox.AppendText(message + "\r\n");
                }
                System.Diagnostics.Debug.WriteLine(message);
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            LayoutRootPanels();
        }


        // Recursively applies a generic sans-serif font to the given control tree
        public static void ApplySansSerifFont(Control root, float? size)
        {
            if (root == null) return;
            try
            {
                var family = FontFamily.GenericSansSerif;
                float newSize = size ?? (root.Font?.Size ?? 9f);
                var style = root.Font?.Style ?? FontStyle.Regular;
                var unit = root.Font?.Unit ?? GraphicsUnit.Point;
                root.Font = new Font(family, newSize, style, unit);

                foreach (Control child in root.Controls)
                {
                    ApplySansSerifFont(child, size);
                }

                if (root is ToolStrip toolStrip)
                {
                    toolStrip.Font = new Font(family, newSize, style, GraphicsUnit.Point);
                    foreach (ToolStripItem item in toolStrip.Items)
                    {
                        item.Font = toolStrip.Font;
                    }
                }
            }
            catch { }
        }

        // Pagination variables
        private int currentPage = 1;
        private int pageSize = 17;
        private int totalPages = 1;
        private DataTable allScannerData = new DataTable();
        private DataTable filteredScannerData = new DataTable();
        private DataTable currentPageData = new DataTable();
        
        // Filter variables
        private DateTime? filterDateFrom = null;
        private DateTime? filterDateTo = null;
        private string filterBlockNumber = "";
        private string filterLineNumber = "";
        private string filterProductId = "";
        private string filterCropId = "";
        
        // Count tracking variables
        private int activeScannersCount = 0;
        private HashSet<string> activeScannerIds = new HashSet<string>();

        private void showScannerOutputCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (scannerOutputTextBox != null)
            {
                scannerOutputTextBox.Visible = showScannerOutputCheckBox.Checked;
            }
            
            // Apply layout to move entire scanner content up/down based on checkbox state
            // This ensures filters, buttons, datagrid, and pagination all move together
            LayoutRootPanels();
        }

        private void previousPageButton_Click(object sender, EventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                LoadCurrentPage();
                UpdatePaginationControls();
            }
        }

        private void nextPageButton_Click(object sender, EventArgs e)
        {
            if (currentPage < totalPages)
            {
                currentPage++;
                LoadCurrentPage();
                UpdatePaginationControls();
            }
        }

        private void LoadCurrentPage()
        {
            // Use filtered data for pagination
            DataTable sourceData = filteredScannerData ?? allScannerData;
            
            if (sourceData == null || sourceData.Rows.Count == 0)
            {
                if (scannerDataGridView != null)
                {
                    scannerDataGridView.DataSource = null;
                    scannerDataGridView.Rows.Clear();
                }
                return;
            }

            // Calculate the range for current page
            int startIndex = (currentPage - 1) * pageSize;
            int endIndex = Math.Min(startIndex + pageSize - 1, sourceData.Rows.Count - 1);

            // Create a new DataTable for current page data
            DataTable newPageData = new DataTable();
            
            // Copy the structure from source data
            foreach (DataColumn column in sourceData.Columns)
            {
                newPageData.Columns.Add(column.ColumnName, column.DataType);
            }

            // Add rows for current page
            for (int i = startIndex; i <= endIndex; i++)
            {
                DataRow newRow = newPageData.NewRow();
                DataRow sourceRow = sourceData.Rows[i];
                
                // Copy each column value
                foreach (DataColumn column in sourceData.Columns)
                {
                    newRow[column.ColumnName] = sourceRow[column.ColumnName];
                }
                
                newPageData.Rows.Add(newRow);
            }

            // Update the current page data reference
            currentPageData = newPageData;

            // Update DataGridView
                    if (scannerDataGridView != null)
                    {
                // Clear any existing columns to prevent duplicates
                scannerDataGridView.Columns.Clear();
                scannerDataGridView.DataSource = currentPageData;
                
                // Set column widths to utilize full available space
                UpdateDataGridViewColumnWidths();
                
                // Force refresh of the DataGridView
                scannerDataGridView.Refresh();
            }
        }

        private void UpdateDataGridViewColumnWidths()
        {
            if (scannerDataGridView != null && scannerDataGridView.Columns.Count >= 8)
            {
                // Calculate available width for columns (subtract some margin for scrollbar)
                int availableWidth = scannerDataGridView.Width - 20; // Reserve 20px for potential scrollbar
                
                // Distribute width proportionally based on content importance and typical length
                scannerDataGridView.Columns["Date"].Width = (int)(availableWidth * 0.12);
                scannerDataGridView.Columns["Time"].Width = (int)(availableWidth * 0.12);
                scannerDataGridView.Columns["SerialNumber"].Width = (int)(availableWidth * 0.20);
                scannerDataGridView.Columns["LineNumber"].Width = (int)(availableWidth * 0.10);
                scannerDataGridView.Columns["BlockNumber"].Width = (int)(availableWidth * 0.10);
                scannerDataGridView.Columns["ProductID"].Width = (int)(availableWidth * 0.10);
                scannerDataGridView.Columns["CropID"].Width = (int)(availableWidth * 0.10);
                scannerDataGridView.Columns["EmployeeID"].Width = (int)(availableWidth * 0.16);
            }
        }

        private void UpdatePaginationControls()
        {
            // Calculate total pages based on filtered data
            DataTable sourceData = filteredScannerData ?? allScannerData;
            int totalRows = sourceData?.Rows.Count ?? 0;
            totalPages = Math.Max(1, (int)Math.Ceiling((double)totalRows / pageSize));
            
            if (pageInfoLabel != null)
            {
                pageInfoLabel.Text = $"Page {currentPage} of {totalPages}";
            }

            if (previousPageButton != null)
            {
                previousPageButton.Enabled = currentPage > 1;
            }

            if (nextPageButton != null)
            {
                nextPageButton.Enabled = currentPage < totalPages;
            }
        }

        private void InitializePagination(DataTable data)
        {
            if (data == null)
            {
                allScannerData = new DataTable();
                filteredScannerData = new DataTable();
                totalPages = 1;
                currentPage = 1;
            }
            else
            {
                allScannerData = data.Copy();
                filteredScannerData = data.Copy(); // Start with all data
                totalPages = Math.Max(1, (int)Math.Ceiling((double)allScannerData.Rows.Count / pageSize));
                currentPage = 1;
            }

            // Populate product and crop ID combo boxes (fixed ranges)
            PopulateProductIdComboBox();
            PopulateCropIdComboBox();

            // Clear the DataGridView completely before setting up pagination
                    if (scannerDataGridView != null)
                    {
                scannerDataGridView.Columns.Clear();
                scannerDataGridView.DataSource = null;
                scannerDataGridView.Rows.Clear();
            }

            LoadCurrentPage();
            UpdatePaginationControls();
            
            // Update count labels
            UpdateActiveScannersCount();
            UpdateCountLabels();
        }

        private void applyFiltersButton_Click(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void clearFiltersButton_Click(object sender, EventArgs e)
        {
            ClearFilters();
        }

        private void ApplyFilters()
        {
            // Get filter values from UI controls
            filterDateFrom = dateFromPicker.Checked ? dateFromPicker.Value.Date : (DateTime?)null;
            filterDateTo = dateToPicker.Checked ? dateToPicker.Value.Date.AddDays(1).AddTicks(-1) : (DateTime?)null; // End of day
            filterBlockNumber = blockNumberTextBox.Text.Trim();
            filterLineNumber = lineNumberTextBox.Text.Trim();
            filterProductId = productIdComboBox.SelectedItem?.ToString() ?? "";
            filterCropId = cropIdComboBox.SelectedItem?.ToString() ?? "";

            // Apply filters to data
            ApplyFiltersToData();
            
            // Reset to first page and reload
            currentPage = 1;
            LoadCurrentPage();
            UpdatePaginationControls();
        }

        private void ClearFilters()
        {
            // Clear filter values
            filterDateFrom = null;
            filterDateTo = null;
            filterBlockNumber = "";
            filterLineNumber = "";
            filterProductId = "";
            filterCropId = "";

            // Clear UI controls
            dateFromPicker.Checked = false;
            dateToPicker.Checked = false;
            blockNumberTextBox.Clear();
            lineNumberTextBox.Clear();
            productIdComboBox.SelectedIndex = -1;
            cropIdComboBox.SelectedIndex = -1;

            // Reset filtered data to all data
            filteredScannerData = allScannerData.Copy();
            
            // Reset to first page and reload
            currentPage = 1;
            LoadCurrentPage();
            UpdatePaginationControls();
        }

        private void ApplyFiltersToData()
        {
            if (allScannerData == null || allScannerData.Rows.Count == 0)
            {
                filteredScannerData = new DataTable();
                return;
            }

            // Create filtered DataTable
            filteredScannerData = allScannerData.Clone(); // Copy structure only

            foreach (DataRow row in allScannerData.Rows)
            {
                bool includeRow = true;

                // Date filter
                if (filterDateFrom.HasValue || filterDateTo.HasValue)
                {
                    if (DateTime.TryParse(row["Date"]?.ToString(), out DateTime rowDate))
                    {
                        if (filterDateFrom.HasValue && rowDate.Date < filterDateFrom.Value.Date)
                            includeRow = false;
                        if (filterDateTo.HasValue && rowDate.Date > filterDateTo.Value.Date)
                            includeRow = false;
                    }
                    else
                    {
                        includeRow = false; // Exclude rows with invalid dates
                    }
                }

                // Block number filter
                if (includeRow && !string.IsNullOrEmpty(filterBlockNumber))
                {
                    string blockId = row["BlockNumber"]?.ToString() ?? "";
                    if (blockId.IndexOf(filterBlockNumber, StringComparison.OrdinalIgnoreCase) < 0)
                        includeRow = false;
                }

                // Line number filter
                if (includeRow && !string.IsNullOrEmpty(filterLineNumber))
                {
                    string lineId = row["LineNumber"]?.ToString() ?? "";
                    if (lineId.IndexOf(filterLineNumber, StringComparison.OrdinalIgnoreCase) < 0)
                        includeRow = false;
                }

                // Product ID filter
                if (includeRow && !string.IsNullOrEmpty(filterProductId))
                {
                    string productId = row["ProductID"]?.ToString() ?? "";
                    if (!productId.Equals(filterProductId, StringComparison.OrdinalIgnoreCase))
                        includeRow = false;
                }

                // Crop ID filter
                if (includeRow && !string.IsNullOrEmpty(filterCropId))
                {
                    string cropId = row["CropID"]?.ToString() ?? "";
                    if (!cropId.Equals(filterCropId, StringComparison.OrdinalIgnoreCase))
                        includeRow = false;
                }

                if (includeRow)
                {
                    filteredScannerData.ImportRow(row);
                }
            }
        }

        private void PopulateProductIdComboBox()
        {
            if (productIdComboBox == null)
                return;
            productIdComboBox.Items.Clear();
            productIdComboBox.Items.Add(""); // Empty option for "all"
            for (int i = 0; i <= 999; i++) productIdComboBox.Items.Add(i.ToString("D3"));
        }

        private void PopulateCropIdComboBox()
        {
            if (cropIdComboBox == null)
                return;
            cropIdComboBox.Items.Clear();
            cropIdComboBox.Items.Add(""); // Empty option for "all"
            for (int i = 0; i <= 999; i++) cropIdComboBox.Items.Add(i.ToString("D3"));
        }

        private void UpdateCountLabels()
        {
            if (allScannerData == null || allScannerData.Rows.Count == 0)
            {
                if (activeScannersLabel != null)
                    activeScannersLabel.Text = "Active Scanners: 0";
                if (todayScansLabel != null)
                    todayScansLabel.Text = "Today's Scans: 0";
                if (lastHourScansLabel != null)
                    lastHourScansLabel.Text = "Last Hour Scans: 0";
                return;
            }

            // Count today's scans
            int todayScans = CountTodaysScans();
            
            // Count last hour scans
            int lastHourScans = CountLastHourScans();
            
            // Update labels
            if (activeScannersLabel != null)
                activeScannersLabel.Text = $"Active Scanners: {activeScannersCount}";
            if (todayScansLabel != null)
                todayScansLabel.Text = $"Today's Scans: {todayScans}";
            if (lastHourScansLabel != null)
                lastHourScansLabel.Text = $"Last Hour Scans: {lastHourScans}";
        }

        private int CountTodaysScans()
        {
            if (allScannerData == null || allScannerData.Rows.Count == 0)
                return 0;

            DateTime today = DateTime.Today;
            int count = 0;

            foreach (DataRow row in allScannerData.Rows)
            {
                if (DateTime.TryParse(row["Date"]?.ToString(), out DateTime scanDate))
                {
                    if (scanDate.Date == today)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private int CountLastHourScans()
        {
            if (allScannerData == null || allScannerData.Rows.Count == 0)
                return 0;

            DateTime oneHourAgo = DateTime.Now.AddHours(-1);
            int count = 0;

            foreach (DataRow row in allScannerData.Rows)
            {
                if (DateTime.TryParse($"{row["Date"]} {row["Time"]}", out DateTime scanDateTime))
                {
                    if (scanDateTime >= oneHourAgo)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private void UpdateActiveScannersCount()
        {
            if (allScannerData == null || allScannerData.Rows.Count == 0)
            {
                activeScannersCount = 0;
                activeScannerIds.Clear();
                return;
            }

            // Clear previous active scanner IDs
            activeScannerIds.Clear();
            
            // Get scans from the last 2 hours to determine active scanners
            DateTime twoHoursAgo = DateTime.Now.AddHours(-2);
            
            foreach (DataRow row in allScannerData.Rows)
            {
                if (DateTime.TryParse($"{row["Date"]} {row["Time"]}", out DateTime scanDateTime))
                {
                    if (scanDateTime >= twoHoursAgo)
                    {
                        string employeeId = row["EmployeeID"]?.ToString()?.Trim();
                        if (!string.IsNullOrEmpty(employeeId))
                        {
                            activeScannerIds.Add(employeeId);
                        }
                    }
                }
            }
            
            activeScannersCount = activeScannerIds.Count;
        }

        #region Advanced Settings Persistence

        private string GetSettingsFilePath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, "ScanLink");
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            return Path.Combine(appFolder, "advanced_settings.txt");
        }

        private void SaveAdvancedSettings()
        {
            try
            {
                string settingsFile = GetSettingsFilePath();
                var settings = new Dictionary<string, string>
                {
                    ["PrinterLanguage"] = comboBox_emulation?.SelectedItem?.ToString() ?? "",
                    ["TestMode"] = comboBox_test?.SelectedItem?.ToString() ?? "",
                    ["BarcodeType"] = comboBox_barcode?.SelectedItem?.ToString() ?? "",
                    ["ProductID"] = comboBox_ProductID?.SelectedItem?.ToString() ?? "000",
                    ["CropID"] = comboBox_CropID?.SelectedItem?.ToString() ?? "000",
                    ["Width"] = numericUpDown_width?.Value.ToString() ?? "400",
                    ["Height"] = numericUpDown_height?.Value.ToString() ?? "180",
                    ["XCoordinate"] = numericUpDown_xCoordinate?.Value.ToString() ?? "250",
                    ["TwoUpEnabled"] = checkBox_twoUp?.Checked.ToString() ?? "False",
                    ["X2Coordinate"] = numericUpDown_x2Coordinate?.Value.ToString() ?? "0",
                    ["Gap"] = numericUpDown_gap?.Value.ToString() ?? "2",
                    ["Alignment"] = comboBox_alignment?.SelectedItem?.ToString() ?? "Left",
                    ["Rotation"] = comboBox_rotation?.SelectedItem?.ToString() ?? "0°",
                    ["Darkness"] = trackBar_darkness?.Value.ToString() ?? "5",
                    ["PrintSpeed"] = comboBox_speed?.SelectedItem?.ToString() ?? "5 - Medium"
                };

                using (var writer = new StreamWriter(settingsFile))
                {
                    foreach (var setting in settings)
                    {
                        writer.WriteLine($"{setting.Key}={setting.Value}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Silently handle errors to avoid disrupting user experience
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        private void LoadAdvancedSettings()
        {
            try
            {
                string settingsFile = GetSettingsFilePath();
                if (!File.Exists(settingsFile))
                {
                    return; // No settings file exists, use defaults
                }

                var settings = new Dictionary<string, string>();
                using (var reader = new StreamReader(settingsFile))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line) || !line.Contains("="))
                            continue;

                        var parts = line.Split(new char[] { '=' }, 2);
                        if (parts.Length == 2)
                        {
                            settings[parts[0].Trim()] = parts[1].Trim();
                        }
                    }
                }

                // Apply loaded settings to controls
                ApplySettingsToControls(settings);
            }
            catch (Exception ex)
            {
                // Silently handle errors to avoid disrupting user experience
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            }
        }

        private void ApplySettingsToControls(Dictionary<string, string> settings)
        {
            try
            {
                // Printer Language
                if (settings.ContainsKey("PrinterLanguage") && comboBox_emulation != null)
                {
                    string value = settings["PrinterLanguage"];
                    for (int i = 0; i < comboBox_emulation.Items.Count; i++)
                    {
                        if (comboBox_emulation.Items[i].ToString() == value)
                        {
                            comboBox_emulation.SelectedIndex = i;
                            break;
                        }
                    }
                }

                // Test Mode
                if (settings.ContainsKey("TestMode") && comboBox_test != null)
                {
                    string value = settings["TestMode"];
                    for (int i = 0; i < comboBox_test.Items.Count; i++)
                    {
                        if (comboBox_test.Items[i].ToString() == value)
                        {
                            comboBox_test.SelectedIndex = i;
                            break;
                        }
                    }
                }

                // Barcode Type
                if (settings.ContainsKey("BarcodeType") && comboBox_barcode != null)
                {
                    string value = settings["BarcodeType"];
                    for (int i = 0; i < comboBox_barcode.Items.Count; i++)
                    {
                        if (comboBox_barcode.Items[i].ToString() == value)
                        {
                            comboBox_barcode.SelectedIndex = i;
                            break;
                        }
                    }
                }

                // ProductID
                if (settings.ContainsKey("ProductID") && comboBox_ProductID != null)
                {
                    string value = settings["ProductID"];
                    for (int i = 0; i < comboBox_ProductID.Items.Count; i++)
                    {
                        if (comboBox_ProductID.Items[i].ToString() == value)
                        {
                            comboBox_ProductID.SelectedIndex = i;
                            break;
                        }
                    }
                }

                // CropID
                if (settings.ContainsKey("CropID") && comboBox_CropID != null)
                {
                    string value = settings["CropID"];
                    for (int i = 0; i < comboBox_CropID.Items.Count; i++)
                    {
                        if (comboBox_CropID.Items[i].ToString() == value)
                        {
                            comboBox_CropID.SelectedIndex = i;
                            break;
                        }
                    }
                }

                // Width
                if (settings.ContainsKey("Width") && numericUpDown_width != null)
                {
                    if (decimal.TryParse(settings["Width"], out decimal width))
                    {
                        numericUpDown_width.Value = Math.Max(numericUpDown_width.Minimum, Math.Min(numericUpDown_width.Maximum, width));
                    }
                }

                // Height
                if (settings.ContainsKey("Height") && numericUpDown_height != null)
                {
                    if (decimal.TryParse(settings["Height"], out decimal height))
                    {
                        numericUpDown_height.Value = Math.Max(numericUpDown_height.Minimum, Math.Min(numericUpDown_height.Maximum, height));
                    }
                }

                // X Coordinate
                if (settings.ContainsKey("XCoordinate") && numericUpDown_xCoordinate != null)
                {
                    if (decimal.TryParse(settings["XCoordinate"], out decimal xcoord))
                    {
                        numericUpDown_xCoordinate.Value = Math.Max(numericUpDown_xCoordinate.Minimum, Math.Min(numericUpDown_xCoordinate.Maximum, xcoord));
                    }
                }

                // Two Up Enabled
                if (settings.ContainsKey("TwoUpEnabled") && checkBox_twoUp != null)
                {
                    if (bool.TryParse(settings["TwoUpEnabled"], out bool twoUp))
                    {
                        checkBox_twoUp.Checked = twoUp;
                    }
                }

                // X2 Coordinate
                if (settings.ContainsKey("X2Coordinate") && numericUpDown_x2Coordinate != null)
                {
                    if (decimal.TryParse(settings["X2Coordinate"], out decimal x2coord))
                    {
                        numericUpDown_x2Coordinate.Value = Math.Max(numericUpDown_x2Coordinate.Minimum, Math.Min(numericUpDown_x2Coordinate.Maximum, x2coord));
                    }
                }

                // Gap
                if (settings.ContainsKey("Gap") && numericUpDown_gap != null)
                {
                    if (decimal.TryParse(settings["Gap"], out decimal gap))
                    {
                        numericUpDown_gap.Value = Math.Max(numericUpDown_gap.Minimum, Math.Min(numericUpDown_gap.Maximum, gap));
                    }
                }

                // Alignment
                if (settings.ContainsKey("Alignment") && comboBox_alignment != null)
                {
                    string value = settings["Alignment"];
                    for (int i = 0; i < comboBox_alignment.Items.Count; i++)
                    {
                        if (comboBox_alignment.Items[i].ToString() == value)
                        {
                            comboBox_alignment.SelectedIndex = i;
                            break;
                        }
                    }
                }

                // Rotation
                if (settings.ContainsKey("Rotation") && comboBox_rotation != null)
                {
                    string value = settings["Rotation"];
                    for (int i = 0; i < comboBox_rotation.Items.Count; i++)
                    {
                        if (comboBox_rotation.Items[i].ToString() == value)
                        {
                            comboBox_rotation.SelectedIndex = i;
                            break;
                        }
                    }
                }

                // Darkness
                if (settings.ContainsKey("Darkness") && trackBar_darkness != null)
                {
                    if (int.TryParse(settings["Darkness"], out int darkness))
                    {
                        trackBar_darkness.Value = Math.Max(trackBar_darkness.Minimum, Math.Min(trackBar_darkness.Maximum, darkness));
                        label_darknessValue.Text = darkness.ToString();
                    }
                }

                // Print Speed
                if (settings.ContainsKey("PrintSpeed") && comboBox_speed != null)
                {
                    string value = settings["PrintSpeed"];
                    for (int i = 0; i < comboBox_speed.Items.Count; i++)
                    {
                        if (comboBox_speed.Items[i].ToString() == value)
                        {
                            comboBox_speed.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying settings: {ex.Message}");
            }
        }

        #endregion
    }
}


