using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows.Forms;
using BarcodePrinter_API;
using BarcodePrinter_API.Comm;
using BarcodePrinter_API.Emulation.PPLB;
// using BarcodePrinter_API.Emulation.PPLZ;
using System.Text;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ScanLink;
using System.Linq;
using System.Web.Script.Serialization;

namespace ScanLink
{
    public delegate void VoidFunction(int count);

    public partial class Form1 : Form
    {
        private const string AppVersion = "4.1.4";

        public class FunctionData
        {
            public string Descration;
            public VoidFunction Function;
        }

        private volatile Process _scannerProcess; // volatile: read by the background stdin writer thread, reassigned on the UI thread
        // --- Non-blocking scanner stdin pipe (#5) ---
        // Scans used to be written to the PowerShell process's StandardInput directly on the UI
        // thread. When PowerShell fell behind (slow per-scan work as the queue grew), that write
        // blocked on the fixed OS pipe buffer and froze the whole app - the "scanning stalls after
        // ~2,300 scans" symptom. Scans are now handed to a dedicated background writer thread via
        // this queue, so the UI thread never blocks on the pipe no matter how slow PowerShell is.
        private System.Collections.Concurrent.BlockingCollection<string> _scanWriteQueue;
        private System.Threading.Thread _scanWriterThread;
        private volatile bool _scanWriterRunning;
        private DateTime _lastScannerRestartRequestUtc = DateTime.MinValue;
        private ScannerComPortManager _scannerComPortManager;
        private FileSystemWatcher _scansFileWatcher;

        // --- Full-history metrics index ---------------------------------------------------------
        // The on-disk display buffer (scans.txt) is capped at 300 rows for performance, so counting
        // off it (Today's / Last Hour / Total filtered Scans) maxes out at 300 even when the
        // packhouse scanned thousands. The true history lives in scans_backup.csv (append-only,
        // uncapped). We keep a compact in-memory projection of it - just the fields the stats and
        // filters need - so the numbers are accurate while the grid still renders only a page.
        // Refreshed incrementally: we remember the last byte offset read and only parse what PS has
        // appended since, so per-scan refresh stays cheap (no full re-read, no extra API calls).
        private struct ScanMeta
        {
            public DateTime Ts;       // local timestamp (date + time)
            public string Crop;
            public string Product;
            public string Line;
            public string Block;
        }
        private readonly List<ScanMeta> _metricsIndex = new List<ScanMeta>();
        private readonly object _metricsLock = new object();
        private string _metricsCsvPath = null;
        private long _metricsCsvOffset = 0;
        private System.Windows.Forms.Timer _scanRefreshTimer = null;
        private System.Windows.Forms.Timer _fileChangeDebounceTimer;
        // --- COM scanner auto-reconnect ---
        // After a power cut the PC boots cold and USB-serial scanners enumerate slowly, so a scanner
        // can appear a few seconds after the app has already run its one-shot detection. These cover
        // that gap: a bounded startup retry plus a USB device-arrival watcher. Both reuse the existing
        // idempotent OpenScanner path, so scanners that are already connected are never disturbed.
        private System.Windows.Forms.Timer _scannerReconnectTimer;
        private System.Windows.Forms.Timer _deviceChangeDebounceTimer;
        private int _scannerReconnectAttemptsLeft;
        private int _scannerScanInProgress; // 0/1 guard (Interlocked) so scans never overlap
        private bool _scannerInUseDialogShown; // throttle: one "port in use" popup at a time; re-arms on a successful connect
        private const int ScannerReconnectIntervalMs = 2500;
        private const int ScannerReconnectMaxAttempts = 8; // ~20s window after each (re)init
        private ApiAuthService _apiAuthService;
        private ScanLogUploadService _scanLogUploadService;
        private ProductCombinationsService _productCombinationsService;
        private RawInputManager _rawInputManager;
        // HID (keyboard/raw) scanners forward scans straight to PowerShell and are NOT owned by
        // _scannerComPortManager, which only tracks COM ports. Without listing them here, an HID
        // scanner records scans fine but the dashboard shows "0 Scanners Connected" with an empty
        // grid. Keyed by device id. Touched only on the UI thread (the HID scan handler marshals
        // there, and the status timer runs on the UI thread too), so no extra locking is needed.
        private readonly Dictionary<string, ScannerConfig> _activeHidScanners = new Dictionary<string, ScannerConfig>();
        private static readonly JavaScriptSerializer HidMetaSerializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

        // Popup forms
        private ConfigPopupForm _configPopupForm;
        private BoxLabelsPopupForm _boxLabelsPopupForm;

        // Barcode generation state
        private string _generatedBarcode = "";
        private bool _barcodeGenerated = false;
        // Employee display name captured when chosen from the employee dialog, plus the barcode
        // employee-ID it belongs to. We only print the name when it still matches the current
        // ID, so a manually-changed ID never prints a stale name.
        private string _selectedEmployeeName = "";
        private string _selectedEmployeeBarcodeId = "";

        // UI Controls
        private System.Windows.Forms.Button button_generateBarcode;
        private const string HidKeyboardSourceLabel = "USB-HID Keyboard";
        private const string HidRawSourceLabel = "USB-HID Raw";
        private const string HidKeyboardConnectionType = "USB-HID-KEYBOARD";
        private const string HidRawConnectionType = "USB-HID-RAW";

        // string strGraphicFilter = "All Graphic Type|*.bmp;*.gif;*.exig;*.jpg;*.png;*.tiff|All File|*.*||";
        // string[] strEmulation = { "PPLB", "PPLZ" };
        string[] strEmulation = { "PPLB"};
        string[] strPort = { "USB", "File", "COM", "LAN", "Multi-LAN" };
        private string _currentConnectionType = "USB"; // Default connection type

        public string CurrentConnectionType
        {
            get { return _currentConnectionType; }
            set { _currentConnectionType = value; }
        }

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

        private Label lblDashboardPrinterStatus;
        private Label lblDashboardScannerStatus;
        private System.Windows.Forms.Timer statusRefreshTimer;

        private DataGridView dgvActiveScanners;
        private Panel activeScannersPanel;

        private DailyStatsService _dailyStatsService;
        private SeasonInfo _activeSeason; // fetched once at login; null if the site has no season configured
        private Panel dailyStatsPanel;
        private DateTimePicker dtpDailyStats;
        private TextBox txtStatHours, txtStatWage, txtStatActiveEmps, txtStatPackers, txtStatBoxes;
        private TextBox txtStatCell, txtStatAddress, txtStatDeliveryNote, txtStatTruckReg, txtStatTransporter, txtStatDriverName;
        private Button btnSaveDailyStats;
        private Label lblDailyStatsStatus;

        private Task<bool> _cropOptionsLoadTask;
        private List<CropOption> _cropOptionsCache = new List<CropOption>();
        private string _cropOptionsErrorMessage = "";
        private string _pendingPrinterCropSelection;
        private string _pendingPrinterProductSelection;
        private Dictionary<string, List<ProductOption>> _productOptionsByCrop = new Dictionary<string, List<ProductOption>>(StringComparer.OrdinalIgnoreCase);
        private List<ProductComboItem> _scannerProductItems;
        private CancellationTokenSource _productDetailCts;

        private class ProductCombinationResponse
        {
            public List<ProductCombination> combinations = null;
            public int total_count = 0;
        }

        // --- Searchable / grouped product-combination dropdown -----------------------------------
        // The barcode product picker (comboBox_ProductID) is now an editable combo: the user types
        // in the field to filter (matched across variety/grade/count/carton), and the list is shown
        // group-wise (grouped by variety, with non-selectable header rows). These hold the full,
        // unfiltered set for the current crop plus reentrancy guards so our own item-rebuilds don't
        // re-trigger the search handler.
        private List<ProductComboItem> _allProductComboItemsForCrop = new List<ProductComboItem>();
        private bool _suppressProductSearch = false;
        private string _lastValidProductName = "";

        private class ProductCombination
        {
            public string id = null;
            public string crop_id = null;
            public string crop_name = null;
            public string product_id = null;
            public string product_name = null;
            public string variety_id = null;
            public string variety_name = null;
            public string grade_id = null;
            public string grade_name = null;
            public string count_id = null;
            public string count_name = null;
            public string carton_type_id = null;
            public string carton_type_name = null;
            public double avg_weight_kg = 0;
        }

        private class CropOption
        {
            public string CropIdFull;
            public string CropIdShort;
            public string CropName;
        }

        private class ProductOption
        {
            public string ProductIdFull;
            public string ProductIdShort;
            public string ProductName;
            public string VarietyName;
            public string GradeName;
            public string CountName;
            public string CartonTypeId;
            public double AvgWeightKg;
        }

        private class CropComboItem
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        private class ProductComboItem
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public string ProductName { get; set; }
            public string Variety { get; set; }
            public string Grade { get; set; }
            public string Count { get; set; }
            public string Carton { get; set; }
            public string AvgWeightKg { get; set; }
            // The exact source combination this row represents. The printer/preview resolve from this
            // so they use the precise grade the user picked. Resolving by Value (product_id) alone is
            // wrong because product_id is shared across grades (e.g. product_id 132 = Delta/60/E15D
            // carries both Grade A and Grade B).
            public ScanLink.ProductCombination Combination { get; set; }

            // Group-header rows (e.g. "VARIETY · Turkey") are inserted into the list so combinations
            // are listed group-wise. Headers are not selectable: they carry no Value and are skipped
            // on selection / keyboard navigation.
            public bool IsHeader { get; set; }
            public string GroupLabel { get; set; }
        }

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

        // Removed JSON-backed product/crop selectors and labels

        public Form1()
        {
            InitializeComponent();
            try
            {
                this.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { /* icon is cosmetic; fall back to default if extraction fails */ }
            _apiAuthService = new ApiAuthService();
            _dailyStatsService = new DailyStatsService(_apiAuthService);
            
            // Initialize scan log upload service
            _scanLogUploadService = new ScanLogUploadService(_apiAuthService);
            _scanLogUploadService.LogMessage += ScanLogUploadService_LogMessage;

            // Initialize product combinations service
            _productCombinationsService = new ProductCombinationsService(_apiAuthService);
            _scanLogUploadService.SetProductCombinationsService(_productCombinationsService);

            // Initialize raw input manager for HID scanners (keyboard and raw HID)
            try
            {
                _rawInputManager = new RawInputManager();
                _rawInputManager.HidScanReceived += RawInputManager_HidScanReceived;
                _rawInputManager.Diagnostics += RawInputManager_Diagnostics;
            }
            catch (Exception rawInputEx)
            {
                Debug.WriteLine($"[HID] Failed to initialize RawInputManager: {rawInputEx.Message}");
            }
            
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
            // printerContentPanel.Visible = false;
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
            // Set version in window title bar and header
            this.Text = $"Scan Link v{AppVersion}";
            if (titleLabel != null)
                titleLabel.Text = $"ScanLink v{AppVersion}";

            // Initialize rounded corners for buttons
            Button_SizeChanged(setupButton, EventArgs.Empty);
            Button_SizeChanged(scannerSetupButton, EventArgs.Empty);
            Button_SizeChanged(printerConnectionButton, EventArgs.Empty);
            Button_SizeChanged(barCodesButton, EventArgs.Empty);
            Button_SizeChanged(boxLabelsButton, EventArgs.Empty);
            Button_SizeChanged(reportsButton, EventArgs.Empty);
            Button_SizeChanged(logoutButton, EventArgs.Empty);

            // Initialize advanced settings (always enabled now)
            InitializeAdvancedSettings();

            // Set dateFromPicker to the first day of the current month
            dateFromPicker.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            // Set initial panel visibility - show login panel
            //idhr change krna last me
            loginPanel.Visible = true; // Show login first
            // printerContentPanel.Visible = false;
            scannerContentPanel.Visible = false;

            // Load ScanLinkLogo.png into loginMainLogoPictureBox
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

            // Initialize function data before combobox initialization
            InitFunctionData();

            // Initialize printer language combobox
            foreach (string str in strEmulation) comboBox_emulation.Items.Add(str);

            // Set emulation without triggering SaveAdvancedSettings during initialization
            comboBox_emulation.SelectedIndexChanged -= comboBox_emulation_SelectedIndexChanged;
            comboBox_emulation.Text = "PPLB";
            comboBox_emulation.SelectedIndexChanged += comboBox_emulation_SelectedIndexChanged;

            // Initialize advanced settings with defaults and tooltips
            InitializeAdvancedSettings();
            
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
            // ApplyRoundedCorners(qualityPanel, 8);
            // ApplyRoundedCorners(previewPanel, 8);

            // ApplyRoundedCorners(actionPanel, 8);
            // ApplyRoundedCorners(statusPanel, 8);
            
            // Apply modern minimalist styling to key buttons
            ApplyModernStylesToButtons();
            LayoutRootPanels();
            
            InitDashboardStatusUI();
            InitializeProductAndCropSelectors();
        }

        private void InitDashboardStatusUI()
        {
            lblDashboardPrinterStatus = new Label();
            lblDashboardPrinterStatus.AutoSize = true;
            lblDashboardPrinterStatus.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblDashboardPrinterStatus.ForeColor = Color.FromArgb(52, 152, 219); 
            lblDashboardPrinterStatus.Padding = new Padding(15, 0, 15, 0);
            lblDashboardPrinterStatus.Dock = DockStyle.Right;
            lblDashboardPrinterStatus.TextAlign = ContentAlignment.MiddleRight;

            lblDashboardScannerStatus = new Label();
            lblDashboardScannerStatus.AutoSize = true;
            lblDashboardScannerStatus.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblDashboardScannerStatus.ForeColor = Color.FromArgb(46, 204, 113); 
            lblDashboardScannerStatus.Padding = new Padding(15, 0, 15, 0);
            lblDashboardScannerStatus.Dock = DockStyle.Right;
            lblDashboardScannerStatus.TextAlign = ContentAlignment.MiddleRight;
            
            if (this.headerPanel != null)
            {
                FlowLayoutPanel statusFlow = new FlowLayoutPanel();
                statusFlow.FlowDirection = FlowDirection.RightToLeft;
                statusFlow.Dock = DockStyle.Right;
                statusFlow.AutoSize = true;
                statusFlow.WrapContents = false;
                statusFlow.BackColor = Color.Transparent;

                statusFlow.Controls.Add(lblDashboardScannerStatus);
                statusFlow.Controls.Add(lblDashboardPrinterStatus);
                
                if (this.buttonpanel != null)
                {
                    this.buttonpanel.Controls.Add(statusFlow);
                }
                else
                {
                    this.headerPanel.Controls.Add(statusFlow);
                }
            }

            statusRefreshTimer = new System.Windows.Forms.Timer();
            statusRefreshTimer.Interval = 2000;
            statusRefreshTimer.Tick += StatusRefreshTimer_Tick;
            statusRefreshTimer.Start();
            
            InitActiveScannersDashboardGrid();

            // Initial update
            StatusRefreshTimer_Tick(null, null);
        }

        private void InitActiveScannersDashboardGrid()
        {
            activeScannersPanel = new Panel();
            activeScannersPanel.Dock = DockStyle.Fill;
            activeScannersPanel.Height = 150;
            activeScannersPanel.Padding = new Padding(10);
            
            Label lblScanners = new Label();
            lblScanners.Text = "Connected Scanners (Quick Update Line/Block)";
            lblScanners.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblScanners.Dock = DockStyle.Top;
            lblScanners.Padding = new Padding(0, 0, 0, 5);
            activeScannersPanel.Controls.Add(lblScanners);

            dgvActiveScanners = new DataGridView();
            dgvActiveScanners.Dock = DockStyle.Fill;
            dgvActiveScanners.AllowUserToAddRows = false;
            dgvActiveScanners.AllowUserToDeleteRows = false;
            dgvActiveScanners.BackgroundColor = Color.White;
            dgvActiveScanners.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvActiveScanners.RowHeadersVisible = false;

            dgvActiveScanners.Columns.Add("PNPDeviceID", "PNPDeviceID");
            dgvActiveScanners.Columns["PNPDeviceID"].Visible = false;
            
            dgvActiveScanners.Columns.Add("DeviceName", "Scanner");
            dgvActiveScanners.Columns["DeviceName"].ReadOnly = true;
            dgvActiveScanners.Columns["DeviceName"].FillWeight = 30;
            
            dgvActiveScanners.Columns.Add("ComPort", "Port");
            dgvActiveScanners.Columns["ComPort"].ReadOnly = true;
            dgvActiveScanners.Columns["ComPort"].FillWeight = 15;
            
            dgvActiveScanners.Columns.Add("LineID", "Line");
            dgvActiveScanners.Columns["LineID"].FillWeight = 15;
            
            dgvActiveScanners.Columns.Add("BlockID", "Block");
            dgvActiveScanners.Columns["BlockID"].FillWeight = 12;

            dgvActiveScanners.Columns.Add("Supplier", "Supplier");
            dgvActiveScanners.Columns["Supplier"].FillWeight = 15;

            DataGridViewButtonColumn btnSave = new DataGridViewButtonColumn();
            btnSave.Name = "Action";
            btnSave.Text = "Save";
            btnSave.UseColumnTextForButtonValue = true;
            btnSave.FillWeight = 25;
            dgvActiveScanners.Columns.Add(btnSave);

            dgvActiveScanners.CellContentClick += DgvActiveScanners_CellContentClick;
            
            activeScannersPanel.Controls.Add(dgvActiveScanners);
            dgvActiveScanners.BringToFront();

            TableLayoutPanel splitPanel = new TableLayoutPanel();
            splitPanel.RowCount = 2;
            splitPanel.ColumnCount = 2;
            splitPanel.Dock = DockStyle.Fill;
            splitPanel.AutoSize = true;
            splitPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            splitPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 250));
            splitPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            splitPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            
            InitDailyStatsPanel();

            Control existingStatsPanel = this.statsPanel;
            if (existingStatsPanel != null && scannerContentPanel.Controls.Contains(existingStatsPanel)) 
            {
                scannerContentPanel.Controls.Remove(existingStatsPanel);
                splitPanel.Controls.Add(existingStatsPanel, 0, 0);
                splitPanel.SetColumnSpan(existingStatsPanel, 2);
                splitPanel.Controls.Add(dailyStatsPanel, 0, 1);
                splitPanel.Controls.Add(activeScannersPanel, 1, 1);
                scannerContentPanel.Controls.Add(splitPanel, 0, 2);
            }
        }

        private void InitDailyStatsPanel()
        {
            dailyStatsPanel = new Panel();
            dailyStatsPanel.Dock = DockStyle.Fill;
            dailyStatsPanel.Padding = new Padding(10);
            dailyStatsPanel.BackColor = Color.White;
            dailyStatsPanel.BorderStyle = BorderStyle.FixedSingle;

            Label lblHeader = new Label();
            lblHeader.Text = "Daily Stats Logger";
            lblHeader.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblHeader.Dock = DockStyle.Top;
            lblHeader.Padding = new Padding(0, 0, 0, 10);
            lblHeader.AutoSize = true;

            FlowLayoutPanel flowPanel = new FlowLayoutPanel();
            flowPanel.Dock = DockStyle.Fill;
            flowPanel.FlowDirection = FlowDirection.LeftToRight;
            flowPanel.WrapContents = true;
            flowPanel.AutoScroll = true;
            flowPanel.Padding = new Padding(0, 15, 0, 0); // push content down to avoid clipping

            dailyStatsPanel.Controls.Add(flowPanel);
            dailyStatsPanel.Controls.Add(lblHeader);
            lblHeader.BringToFront(); // Ensure header remains on top of flow panel in Z-order

            // Date picker
            Panel datePanel = new Panel() { Width = 250, Height = 80 };
            Label lblDate = new Label() { Text = "Date Selected", AutoSize = true, Location = new Point(0, 5), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            dtpDailyStats = new DateTimePicker() { Location = new Point(0, 30), Width = 150, Format = DateTimePickerFormat.Short };
            dtpDailyStats.ValueChanged += async (s, e) => await LoadDailyStatsAsync();
            
            lblDailyStatsStatus = new Label() { Location = new Point(0, 55), AutoSize = true, ForeColor = Color.Blue };
            datePanel.Controls.Add(lblDate);
            datePanel.Controls.Add(dtpDailyStats);
            datePanel.Controls.Add(lblDailyStatsStatus);
            flowPanel.Controls.Add(datePanel);

            // Helpers for textboxes
            TextBox AddStatField(string labelText, string statName)
            {
                Panel pnl = new Panel() { Width = 135, Height = 80 };
                Label lbl = new Label() { Text = labelText, AutoSize = true, Location = new Point(0, 5) };
                TextBox txt = new TextBox() { Name = statName, Width = 120, Location = new Point(0, 30) };
                pnl.Controls.Add(lbl);
                pnl.Controls.Add(txt);
                flowPanel.Controls.Add(pnl);
                return txt;
            }

            txtStatHours = AddStatField("Hours", "hours");
            txtStatWage = AddStatField("Basic Wage", "basic_wage");
            txtStatActiveEmps = AddStatField("Active Employees", "active_employees");
            txtStatPackers = AddStatField("Packers Count", "packers_count");
            txtStatBoxes = AddStatField("Boxes Packed", "boxes_packed");
            txtStatCell = AddStatField("Cell", "cell");
            txtStatAddress = AddStatField("Address", "address");
            txtStatDeliveryNote = AddStatField("Delivery Note", "delivery_note");
            txtStatTruckReg = AddStatField("Truck Registration", "truck_registration");
            txtStatTransporter = AddStatField("Transporter", "transporter");
            txtStatDriverName = AddStatField("Driver Name", "driver_name");

            btnSaveDailyStats = new Button();
            btnSaveDailyStats.Text = "Save Stats";
            btnSaveDailyStats.BackColor = Color.FromArgb(0, 122, 204);
            btnSaveDailyStats.ForeColor = Color.White;
            btnSaveDailyStats.FlatStyle = FlatStyle.Flat;
            btnSaveDailyStats.Width = 120;
            btnSaveDailyStats.Height = 35;
            btnSaveDailyStats.Margin = new Padding(10, 15, 0, 0);
            btnSaveDailyStats.Click += async (s, e) => await SaveDailyStatsWorkerAsync();
            flowPanel.Controls.Add(btnSaveDailyStats);

            Button btnDebugSiteId = new Button();
            btnDebugSiteId.Text = "Debug Token";
            btnDebugSiteId.BackColor = Color.Orange;
            btnDebugSiteId.ForeColor = Color.White;
            btnDebugSiteId.FlatStyle = FlatStyle.Flat;
            btnDebugSiteId.Width = 120;
            btnDebugSiteId.Height = 35;
            btnDebugSiteId.Margin = new Padding(10, 15, 0, 0);
            btnDebugSiteId.Click += (s, e) => {
                string eff = _apiAuthService?.GetEffectiveSiteId();
                var payload = _apiAuthService?.GetCurrentTokenPayload();
                string json = payload != null ? new System.Web.Script.Serialization.JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(payload) : "null";
                MessageBox.Show($"Effective Site ID: {eff}\n\nLast API Error:\n{_lastStatsError}\n\nToken Payload:\n{json}", "Token Debug Logs");
            };
            flowPanel.Controls.Add(btnDebugSiteId);
            
            dailyStatsPanel.BringToFront();
            dtpDailyStats.Value = DateTime.Today; // triggers load initially
        }

        private async Task LoadDailyStatsAsync()
        {
            lblDailyStatsStatus.Text = "Loading...";
            lblDailyStatsStatus.ForeColor = Color.DarkOrange;
            
            string siteId = _apiAuthService?.GetEffectiveSiteId();

            if (string.IsNullOrEmpty(siteId))
            {
                lblDailyStatsStatus.Text = "No Site Selected";
                lblDailyStatsStatus.ForeColor = Color.Red;
                return;
            }

            var stats = await _dailyStatsService.GetDailyStatsAsync(siteId, dtpDailyStats.Value);
            
            txtStatHours.Text = stats.ContainsKey("hours") ? stats["hours"] : "";
            txtStatWage.Text = stats.ContainsKey("basic_wage") ? stats["basic_wage"] : "";
            txtStatActiveEmps.Text = stats.ContainsKey("active_employees") ? stats["active_employees"] : "";
            txtStatPackers.Text = stats.ContainsKey("packers_count") ? stats["packers_count"] : "";
            txtStatBoxes.Text = stats.ContainsKey("boxes_packed") ? stats["boxes_packed"] : "";
            txtStatCell.Text = stats.ContainsKey("cell") ? stats["cell"] : "";
            txtStatAddress.Text = stats.ContainsKey("address") ? stats["address"] : "";
            txtStatDeliveryNote.Text = stats.ContainsKey("delivery_note") ? stats["delivery_note"] : "";
            txtStatTruckReg.Text = stats.ContainsKey("truck_registration") ? stats["truck_registration"] : "";
            txtStatTransporter.Text = stats.ContainsKey("transporter") ? stats["transporter"] : "";
            txtStatDriverName.Text = stats.ContainsKey("driver_name") ? stats["driver_name"] : "";

            bool anySet = stats.Count > 0;
            lblDailyStatsStatus.Text = anySet ? "Data loaded" : "Not set";
            lblDailyStatsStatus.ForeColor = anySet ? Color.Green : Color.Gray;
        }

        private string _lastStatsError = "No errors yet.";

        private async Task SaveDailyStatsWorkerAsync()
        {
            string siteId = _apiAuthService?.GetEffectiveSiteId();

            if (string.IsNullOrEmpty(siteId))
            {
                MessageBox.Show("Cannot save stats: No active site selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            btnSaveDailyStats.Enabled = false;
            btnSaveDailyStats.Text = "Saving...";

            var statsDict = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(txtStatHours.Text)) statsDict["hours"] = txtStatHours.Text.Trim();
            if (!string.IsNullOrWhiteSpace(txtStatWage.Text)) statsDict["basic_wage"] = txtStatWage.Text.Trim();
            if (!string.IsNullOrWhiteSpace(txtStatActiveEmps.Text)) statsDict["active_employees"] = txtStatActiveEmps.Text.Trim();
            if (!string.IsNullOrWhiteSpace(txtStatPackers.Text)) statsDict["packers_count"] = txtStatPackers.Text.Trim();
            if (!string.IsNullOrWhiteSpace(txtStatBoxes.Text)) statsDict["boxes_packed"] = txtStatBoxes.Text.Trim();
            if (!string.IsNullOrWhiteSpace(txtStatCell.Text)) statsDict["cell"] = txtStatCell.Text.Trim();
            if (!string.IsNullOrWhiteSpace(txtStatAddress.Text)) statsDict["address"] = txtStatAddress.Text.Trim();
            if (!string.IsNullOrWhiteSpace(txtStatDeliveryNote.Text)) statsDict["delivery_note"] = txtStatDeliveryNote.Text.Trim();
            if (!string.IsNullOrWhiteSpace(txtStatTruckReg.Text)) statsDict["truck_registration"] = txtStatTruckReg.Text.Trim();
            if (!string.IsNullOrWhiteSpace(txtStatTransporter.Text)) statsDict["transporter"] = txtStatTransporter.Text.Trim();
            if (!string.IsNullOrWhiteSpace(txtStatDriverName.Text)) statsDict["driver_name"] = txtStatDriverName.Text.Trim();

            var result = await _dailyStatsService.SaveDailyStatsAsync(siteId, dtpDailyStats.Value, statsDict);
            
            if (result.Success)
            {
                lblDailyStatsStatus.Text = "Saved successfully";
                lblDailyStatsStatus.ForeColor = Color.Green;
                _lastStatsError = "Last save successful.";
                MessageBox.Show($"Daily Stats for {dtpDailyStats.Value.ToShortDateString()} saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                lblDailyStatsStatus.Text = "Failed to save";
                lblDailyStatsStatus.ForeColor = Color.Red;
                _lastStatsError = $"Error: {result.ErrorMessage}\n\nPayload Sent: {result.Payload}";
                MessageBox.Show("Failed to save daily stats to the cloud. Click 'Debug Token' to see the exact API rejection reason.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            btnSaveDailyStats.Text = "Save Stats";
            btnSaveDailyStats.Enabled = true;
        }

        private void DgvActiveScanners_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvActiveScanners.Columns["Action"].Index)
            {
                string pnp = dgvActiveScanners.Rows[e.RowIndex].Cells["PNPDeviceID"].Value?.ToString();
                string newLine = dgvActiveScanners.Rows[e.RowIndex].Cells["LineID"].Value?.ToString() ?? "";
                string newBlock = dgvActiveScanners.Rows[e.RowIndex].Cells["BlockID"].Value?.ToString() ?? "";
                string newSupplier = dgvActiveScanners.Rows[e.RowIndex].Cells["Supplier"].Value?.ToString() ?? "";

                var activeScanners = _scannerComPortManager.GetActiveScanners();
                var scanner = activeScanners.FirstOrDefault(s => s.PNPDeviceID == pnp);
                // HID scanners live in _activeHidScanners, not the COM manager; update there too so
                // the change sticks (the entry is the same reference the grid rebuilds from).
                if (scanner == null && _activeHidScanners.TryGetValue(pnp, out var hidScanner))
                {
                    scanner = hidScanner;
                }
                if (scanner != null)
                {
                    scanner.LineID = newLine;
                    scanner.BlockID = newBlock;
                    scanner.Supplier = newSupplier;

                    UpdateScannerAssignmentFile(pnp, newLine, newBlock, newSupplier);

                    if (scannerOutputTextBox != null)
                    {
                        scannerOutputTextBox.AppendText($"[C# INFO] Updated Scanner {scanner.DeviceName} to Line {newLine}, Block {newBlock}, Supplier {newSupplier}\r\n");
                        scannerOutputTextBox.ScrollToCaret();
                    }
                    MessageBox.Show($"Updated Scanner {scanner.DeviceName} Line/Block/Supplier successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void UpdateScannerAssignmentFile(string pnpDeviceId, string newLineId, string newBlockId, string newSupplier = "")
        {
            string savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ScanLink", "scanner_assignments.txt");
            if (!File.Exists(savePath)) return;

            try 
            {
                string[] lines = File.ReadAllLines(savePath);
                bool inTargetScanner = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith("Scanner #"))
                    {
                        inTargetScanner = false;
                    }
                    if (lines[i].Contains("PNPDeviceID: ") && lines[i].Contains(pnpDeviceId))
                    {
                        inTargetScanner = true;
                    }
                    if (inTargetScanner)
                    {
                        if (lines[i].TrimStart().StartsWith("Line ID:"))
                        {
                            lines[i] = $"  Line ID: {newLineId}";
                        }
                        else if (lines[i].TrimStart().StartsWith("Block ID:"))
                        {
                            lines[i] = $"  Block ID: {newBlockId}";
                        }
                        else if (lines[i].TrimStart().StartsWith("Supplier:"))
                        {
                            lines[i] = $"  Supplier: {newSupplier}";
                        }
                    }
                }
                File.WriteAllLines(savePath, lines);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to update scanner assignments: {ex.Message}");
            }
        }

        // All scanners to show on the dashboard: COM ports owned by the manager plus any HID
        // (keyboard/raw) scanners that have produced scans this session. Both feed the recorder
        // identically; only COM ports were ever surfaced in the UI, which is why HID scanners read
        // as "0 connected" even while working.
        private List<ScannerConfig> GetAllDisplayScanners()
        {
            var all = new List<ScannerConfig>();
            if (_scannerComPortManager != null)
            {
                all.AddRange(_scannerComPortManager.GetActiveScanners());
            }
            all.AddRange(_activeHidScanners.Values);
            return all;
        }

        private void StatusRefreshTimer_Tick(object sender, EventArgs e)
        {
            int scannerCount = GetAllDisplayScanners().Count;
            lblDashboardScannerStatus.Text = $"{scannerCount} Scanner{(scannerCount != 1 ? "s" : "")} Connected";
            
            bool isPrinterConnected = false;
            if (!string.IsNullOrEmpty(CurrentConnectionType))
            {
                if (CurrentConnectionType == "USB")
                {
                    isPrinterConnected = !string.IsNullOrWhiteSpace(this.m_USBDevicePath);
                }
                else if (CurrentConnectionType != "Multi-LAN")
                {
                    isPrinterConnected = true;
                }
            }
            lblDashboardPrinterStatus.Text = isPrinterConnected ? "1 Printer Connected" : "0 Printer Connected";

            RefreshActiveScannersGrid();
        }

        private void RefreshActiveScannersGrid()
        {
            if (dgvActiveScanners == null) return;

            var activeScanners = GetAllDisplayScanners();
            bool needsRebuild = false;
            
            if (dgvActiveScanners.Rows.Count != activeScanners.Count) 
            {
                needsRebuild = true;
            } 
            else 
            {
                for (int i = 0; i < activeScanners.Count; i++) 
                {
                    if (dgvActiveScanners.Rows[i].Cells["PNPDeviceID"].Value?.ToString() != activeScanners[i].PNPDeviceID) 
                    {
                        needsRebuild = true;
                        break;
                    }
                }
            }
            
            if (needsRebuild) 
            {
                dgvActiveScanners.Rows.Clear();
                foreach (var scanner in activeScanners) 
                {
                    dgvActiveScanners.Rows.Add(
                        scanner.PNPDeviceID,
                        scanner.DeviceName ?? scanner.PNPDeviceID,
                        scanner.ComPort,
                        scanner.LineID,
                        scanner.BlockID,
                        scanner.Supplier
                    );
                }
            }
        }

        private void LoadLoginLogo()
        {
            try
            {
                // Ensure the PictureBox exists and is properly configured
                if (loginMainLogoPictureBox == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ loginMainLogoPictureBox is null");
                    return;
                }

                // Set initial properties
                loginMainLogoPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                loginMainLogoPictureBox.Visible = true;

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

                System.Diagnostics.Debug.WriteLine($"🔍 Searching for logo files. Application.StartupPath: {Application.StartupPath}");

                foreach (string path in possiblePaths)
                {
                    string fullPath = Path.GetFullPath(path);
                    System.Diagnostics.Debug.WriteLine($"Trying: {fullPath}");

                    if (File.Exists(fullPath))
                    {
                        Image logoImage = Image.FromFile(fullPath);
                        loginMainLogoPictureBox.Image = logoImage;
                        loginMainLogoPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                        loginMainLogoPictureBox.Visible = true;
                        System.Diagnostics.Debug.WriteLine($"✓ Successfully loaded logo from: {fullPath}");
                        System.Diagnostics.Debug.WriteLine($"  PictureBox size: {loginMainLogoPictureBox.Width}x{loginMainLogoPictureBox.Height}");
                        System.Diagnostics.Debug.WriteLine($"  PictureBox location: {loginMainLogoPictureBox.Location}");
                        System.Diagnostics.Debug.WriteLine($"  PictureBox visible: {loginMainLogoPictureBox.Visible}");
                        System.Diagnostics.Debug.WriteLine($"  Image size: {logoImage.Width}x{logoImage.Height}");
                        // start panel removed
                        return;
                    }
                }

                System.Diagnostics.Debug.WriteLine("❌ Logo file not found in any expected location");
                System.Diagnostics.Debug.WriteLine("Available files in StartupPath:");
                try
                {
                    foreach (var file in Directory.GetFiles(Application.StartupPath, "*.*"))
                    {
                        System.Diagnostics.Debug.WriteLine($"  - {Path.GetFileName(file)}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error listing files: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading logo: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // Initialize button states
            button_send.Enabled = false;
            button_send.BackColor = System.Drawing.Color.FromArgb(100, 100, 100);

            if (_rawInputManager != null && !_rawInputManager.IsRegistered)
            {
                try
                {
                    _rawInputManager.RegisterForDevices(this.Handle);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[HID] Raw input registration failed: {ex.Message}");
                    if (scannerOutputTextBox != null)
                    {
                        scannerOutputTextBox.AppendText($"[HID] Registration failed: {ex.Message}\r\n");
                    }
                }
            }
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
                    await Task.Delay(1000);

                    // Show Multi-User Dashboard for site selection
                    loadingStatusLabel.Text = "Loading dashboard...";
                    loadingStatusLabel.Visible = true;

                    using (var dashboard = new MultiUserDashboard(_apiAuthService, tokenPayload))
                    {
                        var dashboardResult = dashboard.ShowDialog(this);

                        if (dashboardResult == DialogResult.OK)
                        {
                            var selectedSiteId = dashboard.GetSelectedSiteId();
                            var selectedSiteName = dashboard.GetSelectedSiteName();

                            if (!string.IsNullOrEmpty(selectedSiteId))
                            {
                                // Store the selected site in the API service
                                _apiAuthService.SetSelectedSite(selectedSiteId, selectedSiteName);

                                System.Diagnostics.Debug.WriteLine($"Site selected: {selectedSiteName} (ID: {selectedSiteId})");
                            }
                            else
                            {
                                loginStatusLabel.Text = "No site selected. Please try logging in again.";
                                loginStatusLabel.ForeColor = Color.Red;
                                loginStatusLabel.Visible = true;
                                loadingStatusLabel.Visible = false;
                                loginButton.Enabled = true;
                                return;
                            }
                        }
                        else
                        {
                            // User cancelled dashboard selection
                            loginStatusLabel.Text = "Site selection cancelled. Please try logging in again.";
                            loginStatusLabel.ForeColor = Color.Red;
                            loginStatusLabel.Visible = true;
                            loadingStatusLabel.Visible = false;
                            loginButton.Enabled = true;
                            return;
                        }
                    }

                    // Enrich queued logs immediately after login, then start periodic uploader
                    _scanLogUploadService?.EnrichLogsFileOnce();
                    _scanLogUploadService?.Start();

                    // Show loading UI and disable login button
                    loginStatusLabel.Visible = false;
                    loadingProgressBar.Visible = true;
                    loadingStatusLabel.Visible = true;
                    loginButton.Enabled = false;
                    loginButton.Text = "Login";

                    // Run scanner system initialization asynchronously
                    bool initSuccess = await InitializeScannerSystemAsync();

                    if (initSuccess)
                    {
                        // Fetch product combinations from API and cache them
                        loadingStatusLabel.Text = "Loading product data...";
                        var combinationsResult = await _productCombinationsService.FetchAndCacheProductCombinationsAsync();
                        if (combinationsResult.Success)
                        {
                            // Repopulate the comboBoxes with API data
                            _scannerProductItems = null; // Clear cache to force reload from API
                            _cropOptionsCache = null; // Clear crop cache too
                            PopulateProductIdComboBox();
                            PopulateCropIdComboBox();
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Warning: Failed to fetch product combinations: {combinationsResult.ErrorMessage}");
                            // Continue anyway - fallback logic will be used
                        }

                        // Fetch the site's active season so the "Season Scans" stat can filter
                        // scans_backup.csv by the same season boundaries the web dashboard uses.
                        string activeSeasonSiteId = _apiAuthService?.GetEffectiveSiteId();
                        if (!string.IsNullOrEmpty(activeSeasonSiteId))
                        {
                            _activeSeason = await _dailyStatsService.GetActiveSeasonAsync(activeSeasonSiteId);
                        }

                        // Hide loading UI and switch to scanner panel
                        loadingProgressBar.Visible = false;
                        loadingStatusLabel.Visible = false;
                        loginPanel.Visible = false;
                        // printerContentPanel.Visible = false;
                        scannerContentPanel.Visible = true;

                        // Apply layout to ensure new UI is properly displayed
                        LayoutRootPanels();
                        
                        // Force a refresh of the daily stats panel now that we are authenticated and selected a site
                        _ = LoadDailyStatsAsync();
                    }
                    else
                    {
                        // Hide loading UI and show error message for retry
                        loadingProgressBar.Visible = false;
                        loadingStatusLabel.Visible = false;
                        loginStatusLabel.Text = "Failed to initialize scanner system. Please try logging in again.";
                        loginStatusLabel.ForeColor = Color.Red;
                        loginStatusLabel.Visible = true;
                        loginButton.Enabled = true;
                        return; // Don't continue with success logic
                    }
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

        private void usernameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                loginButton.PerformClick();
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

        private void passwordTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                loginButton.PerformClick();
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

        private void button_AddCombination_Click(object sender, EventArgs e)
        {
            using (var dialog = new AddCombinationDialog(_productCombinationsService))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    // Cache was already refreshed by CreateProductCombinationAsync; just
                    // rebuild the on-screen dropdowns from it (same pattern used after login).
                    _scannerProductItems = null;
                    _cropOptionsCache = null;
                    PopulateProductIdComboBox();
                    PopulateCropIdComboBox();

                    if (statusLabel != null)
                    {
                        statusLabel.Text = $"✅ Combination created (id {dialog.CreatedCombination?.id})";
                        statusLabel.ForeColor = Color.FromArgb(39, 174, 96);
                    }
                }
            }
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
                            // Update the Employee ID textbox with the last 4 digits of the selected employee's scanlink_id
                            // (barcode format: guard(1) + employee(4) + product(3) + crop(3) + check(1) = 12 digits)
                            string fullId = selectedEmployee.scanlink_id ?? "";
                            string digitsOnly = new string(fullId.Where(char.IsDigit).ToArray());
                            string employeeId = digitsOnly.Length >= 4
                                ? digitsOnly.Substring(digitsOnly.Length - 4)
                                : digitsOnly.PadLeft(4, '0');
                            textBox_EmployeeID.Text = employeeId;

                            // Remember the employee's name (and which barcode ID it maps to) so the
                            // label can print it below the barcode.
                            _selectedEmployeeName = $"{selectedEmployee.first_name} {selectedEmployee.last_name}".Trim();
                            _selectedEmployeeBarcodeId = employeeId;

                            // Update status
                            statusLabel.Text = $"Selected employee: {selectedEmployee.first_name} {selectedEmployee.last_name} (ScanLink ID: {selectedEmployee.scanlink_id} → Barcode: {employeeId})";
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
                statusLabel.Text = "Custom Preset selected - Advanced settings enabled automatically for full control.";
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

        // Advanced settings are now always enabled
        private void InitializeAdvancedSettings()
        {
            // Advanced settings are always visible now
            statusLabel.Text = "Advanced settings enabled. Configure barcode dimensions and quality.";
            statusLabel.ForeColor = System.Drawing.Color.FromArgb(52, 152, 219);

            // Set up tooltips for better user experience
            toolTip.SetToolTip(textBox_EmployeeID, "Enter the text/data to encode in the barcode");
            toolTip.SetToolTip(comboBox_ProductID, "Select the product ID for the barcode");
            toolTip.SetToolTip(numericUpDown_width, "Width of the barcode in dots/pixels");
            toolTip.SetToolTip(numericUpDown_height, "Height of the barcode in dots/pixels");
            toolTip.SetToolTip(numericUpDown_gap, "Gap between labels in millimeters");
            toolTip.SetToolTip(trackBar_darkness, "Print darkness/density (1=lightest, 30=darkest)");
            toolTip.SetToolTip(comboBox_speed, "Print speed setting (1=slowest/highest quality, 9=fastest)");
            toolTip.SetToolTip(button_preview, "Preview the current settings before printing");

                // Update picture box visibility based on two-up checkbox state
                UpdatePictureBoxVisibility();

            // Ensure the main panel can scroll properly for advanced settings
            // printerContentPanel.AutoScrollMinSize = new Size(0, 1100);

            // Update layout to position the panels correctly
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


        private void comboBox_speed_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Auto-save settings when changed
            SaveAdvancedSettings();
        }

        private void numericUpDown_width_ValueChanged(object sender, EventArgs e)
        {
            // Update conversion label
            UpdateDimensionLabels();
            // Auto-save settings when changed
            SaveAdvancedSettings();
        }

        private void numericUpDown_height_ValueChanged(object sender, EventArgs e)
        {
            // Update conversion label
            UpdateDimensionLabels();
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
            // Update conversion label
            UpdateDimensionLabels();
            // Auto-save settings when changed
            SaveAdvancedSettings();
        }

        private void numericUpDown_x2Coordinate_ValueChanged(object sender, EventArgs e)
        {
            // Update conversion label
            UpdateDimensionLabels();
            // Auto-save settings when changed
            SaveAdvancedSettings();
        }

        private void numericUpDown_dpi_ValueChanged(object sender, EventArgs e)
        {
            // Update conversion labels when DPI changes
            UpdateDimensionLabels();
            // Auto-save settings when changed
            SaveAdvancedSettings();
        }

        public void UpdatePictureBoxVisibility()
        {
            bool enabled = checkBox_twoUp.Checked;

            // Show/hide dimension visualization based on two-up checkbox
            if (dimensionVisualizationPictureBox != null)
            {
                dimensionVisualizationPictureBox.Visible = !enabled;
                if (dimensionVisualizationPictureBox.Visible)
                {
                    dimensionVisualizationPictureBox.Invalidate(); // Force redraw
                    dimensionVisualizationPictureBox.BringToFront();
                }
            }

            if (dimensionVisualizationPictureBox_TwoUp != null)
            {
                dimensionVisualizationPictureBox_TwoUp.Visible = enabled;
                if (dimensionVisualizationPictureBox_TwoUp.Visible)
                {
                    dimensionVisualizationPictureBox_TwoUp.Invalidate(); // Force redraw
                    dimensionVisualizationPictureBox_TwoUp.BringToFront();
                }
            }
        }

        private void checkBox_twoUp_CheckedChanged(object sender, EventArgs e)
        {
            bool enabled = checkBox_twoUp.Checked;
            if (numericUpDown_x2Coordinate != null) numericUpDown_x2Coordinate.Enabled = enabled;
            if (label_x2Coordinate != null) label_x2Coordinate.Enabled = enabled;

            // Update picture box visibility
            UpdatePictureBoxVisibility();

            SaveAdvancedSettings();
        }

        private void dimensionVisualizationPictureBox_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // PictureBox dimensions
            int pbWidth = dimensionVisualizationPictureBox.Width;
            int pbHeight = dimensionVisualizationPictureBox.Height;

            // Draw vertical boundary lines (full height)
            using (Pen boundaryPen = new Pen(Color.DarkGray, 2))
            {
                // Left vertical line
                g.DrawLine(boundaryPen, 0, 0, 0, pbHeight);
                // Right vertical line
                g.DrawLine(boundaryPen, pbWidth - 1, 0, pbWidth - 1, pbHeight);
            }

            // Define dimensions for square outer container
            int padding = 20; // Padding around the square
            int availableWidth = pbWidth - 2 * padding;
            int availableHeight = pbHeight - 2 * padding;

            // Calculate sticker dimensions (4 stickers, 1:4 height:width ratio, 5px gaps)
            int gap = 5; // 5px gap between stickers
            int numStickers = 4;
            int numGaps = numStickers - 1; // 3 gaps for 4 stickers

            // Calculate maximum sticker dimensions that fit
            int maxStickerWidth = (int)(availableWidth * 0.8); // Leave some margin
            int totalGapHeight = numGaps * gap;
            int availableHeightForStickers = availableHeight - totalGapHeight;
            int maxStickerHeight = availableHeightForStickers / numStickers;

            // Maintain 1:4 height:width ratio
            int stickerHeight = Math.Min(maxStickerHeight, maxStickerWidth / 4);
            int stickerWidth = stickerHeight * 4; // Exact 1:4 ratio

            // Create square outer container that encloses all stickers
            int totalStickersHeight = numStickers * stickerHeight + totalGapHeight;
            int squareSize = Math.Max(stickerWidth + 40, totalStickersHeight + 40); // Add some margin
            int largeRectX = (pbWidth - squareSize) / 2;
            int largeRectY = (pbHeight - squareSize) / 2;
            int largeRectWidth = squareSize;
            int largeRectHeight = squareSize;

            int xCoordinate = largeRectX + (squareSize - stickerWidth) / 2; // Center stickers horizontally

            // Fill areas with background colors
            // Light gray outside the square boundaries
            using (Brush grayBrush = new SolidBrush(Color.LightGray))
            {
                // Left area (from PictureBox left edge to square left edge)
                g.FillRectangle(grayBrush, 0, 0, largeRectX, pbHeight);
                // Right area (from square right edge to PictureBox right edge)
                g.FillRectangle(grayBrush, largeRectX + largeRectWidth, 0, pbWidth - (largeRectX + largeRectWidth), pbHeight);
            }

            // Darker yellow inside square boundaries (between vertical lines)
            using (Brush yellowBrush = new SolidBrush(Color.FromArgb(255, 255, 200))) // Slightly darker yellow
            {
                g.FillRectangle(yellowBrush, largeRectX, largeRectY, largeRectWidth, largeRectHeight);
            }

            // Draw 4 stickers vertically with 5px gaps (white rectangles over yellow background)
            int startY = largeRectY + (largeRectHeight - (4 * stickerHeight + 3 * gap)) / 2; // Center vertically

            for (int i = 0; i < 4; i++)
            {
                int stickerY = startY + i * (stickerHeight + gap);
                DrawRoundedRectangle(g, new Rectangle(xCoordinate, stickerY, stickerWidth, stickerHeight), 8, Color.White);
            }

            // Draw large outer square (paper/label area) - only vertical lines (on top of backgrounds)
            using (Pen blackPen = new Pen(Color.Black, 2))
            {
                // Left vertical line
                g.DrawLine(blackPen, largeRectX, largeRectY, largeRectX, largeRectY + largeRectHeight);
                // Right vertical line
                g.DrawLine(blackPen, largeRectX + largeRectWidth, largeRectY, largeRectX + largeRectWidth, largeRectY + largeRectHeight);
            }

            // Draw sticker names in the center of each rectangle
            using (Font stickerFont = new Font("Microsoft Sans Serif", 8))
            using (Brush stickerTextBrush = new SolidBrush(Color.DarkBlue))
            {
                for (int i = 0; i < 4; i++)
                {
                    int stickerY = startY + i * (stickerHeight + gap);
                    string stickerName = $"Sticker-{i + 1}";
                    SizeF textSize = g.MeasureString(stickerName, stickerFont);
                    int textX = xCoordinate + (stickerWidth - (int)textSize.Width) / 2;
                    int textY = stickerY + (stickerHeight - (int)textSize.Height) / 2;
                    g.DrawString(stickerName, stickerFont, stickerTextBrush, textX, textY);
                }

                // Draw "Sticker Roll" below the 4 stickers with light yellow background
                string stickerRollText = "Sticker Roll";
                SizeF stickerRollSize = g.MeasureString(stickerRollText, stickerFont);
                int stickerRollX = xCoordinate + (stickerWidth - (int)stickerRollSize.Width) / 2;
                int stickerRollY = startY + 4 * (stickerHeight + gap) - gap + 5; // Below last sticker

                // Draw darker yellow background rectangle
                using (Brush yellowBrush = new SolidBrush(Color.FromArgb(255, 255, 200))) // Match the square background color
                {
                    int textPadding = 4;
                    g.FillRectangle(yellowBrush,
                        stickerRollX - textPadding,
                        stickerRollY - textPadding,
                        stickerRollSize.Width + 2 * textPadding,
                        stickerRollSize.Height + 2 * textPadding);
                }
                g.DrawString(stickerRollText, stickerFont, stickerTextBrush, stickerRollX, stickerRollY);

                // Draw "Ink Roll" in the lower left corner of PictureBox with light gray background
                string inkRollText = "Ink Roll";
                SizeF inkRollSize = g.MeasureString(inkRollText, stickerFont);
                int inkRollX = 10; // Left margin
                int inkRollY = pbHeight - (int)inkRollSize.Height - 110; // Bottom margin

                // Draw light gray background rectangle
                using (Brush grayBrush = new SolidBrush(Color.LightGray))
                {
                    int textPadding = 4;
                    g.FillRectangle(grayBrush,
                        inkRollX - textPadding,
                        inkRollY - textPadding,
                        inkRollSize.Width + 2 * textPadding,
                        inkRollSize.Height + 2 * textPadding);
                }
                g.DrawString(inkRollText, stickerFont, stickerTextBrush, inkRollX, inkRollY);
            }

            // Declare variables for arrow positioning
            int xCoordLabelY = 0;

            // Draw dimension labels
            using (Font labelFont = new Font("Microsoft Sans Serif", 9, FontStyle.Regular))
            using (Brush textBrush = new SolidBrush(Color.Black))
            {
                // Gap label (between first two stickers)
                string gapText = "Gap";
                SizeF gapSize = g.MeasureString(gapText, labelFont);
                int gapLabelX = xCoordinate + stickerWidth / 2 - (int)gapSize.Width / 2;
                int gapLabelY = startY + stickerHeight + gap / 2 - (int)gapSize.Height / 2;
                g.DrawString(gapText, labelFont, textBrush, gapLabelX, gapLabelY);

                // Width label
                string widthText = "Width";
                SizeF widthSize = g.MeasureString(widthText, labelFont);
                int widthLabelX = xCoordinate + stickerWidth / 2 - (int)widthSize.Width / 2;
                int widthLabelY = startY - 20;
                g.DrawString(widthText, labelFont, textBrush, widthLabelX, widthLabelY);

                // Height label
                string heightText = "Height";
                SizeF heightSize = g.MeasureString(heightText, labelFont);
                int heightLabelX = xCoordinate - 70;
                int heightLabelY = startY + stickerHeight / 2 - (int)heightSize.Height / 2;
                g.DrawString(heightText, labelFont, textBrush, heightLabelX, heightLabelY);

                // xCoordinate label (distance from left boundary to second sticker)
                string xCoordText = "xCoordinate";
                SizeF xCoordSize = g.MeasureString(xCoordText, labelFont);
                int xCoordLabelX = Math.Max(10, xCoordinate / 2 - (int)xCoordSize.Width / 2); // Ensure it's visible
                xCoordLabelY = Math.Min(pbHeight - 30, largeRectY + largeRectHeight + 10); // Keep within bounds
                g.DrawString(xCoordText, labelFont, textBrush, xCoordLabelX, xCoordLabelY);
            }

            // Draw dimension arrows/lines
            using (Pen arrowPen = new Pen(Color.Gray, 1))
            {
                // Width arrow
                int arrowY = startY - 10;
                g.DrawLine(arrowPen, xCoordinate, arrowY, xCoordinate + stickerWidth, arrowY);
                // Arrow heads
                g.DrawLine(arrowPen, xCoordinate, arrowY, xCoordinate + 3, arrowY - 3);
                g.DrawLine(arrowPen, xCoordinate, arrowY, xCoordinate + 3, arrowY + 3);
                g.DrawLine(arrowPen, xCoordinate + stickerWidth, arrowY, xCoordinate + stickerWidth - 3, arrowY - 3);
                g.DrawLine(arrowPen, xCoordinate + stickerWidth, arrowY, xCoordinate + stickerWidth - 3, arrowY + 3);

                // Height arrow
                int arrowX = xCoordinate - 10;
                g.DrawLine(arrowPen, arrowX, startY, arrowX, startY + stickerHeight);
                // Arrow heads
                g.DrawLine(arrowPen, arrowX, startY, arrowX - 3, startY + 3);
                g.DrawLine(arrowPen, arrowX, startY, arrowX + 3, startY + 3);
                g.DrawLine(arrowPen, arrowX, startY + stickerHeight, arrowX - 3, startY + stickerHeight - 3);
                g.DrawLine(arrowPen, arrowX, startY + stickerHeight, arrowX + 3, startY + stickerHeight - 3);

                // Gap arrow (between first two stickers)
                int gapArrowX = xCoordinate + stickerWidth + 10;
                int firstStickerBottom = startY + stickerHeight;
                int secondStickerTop = startY + stickerHeight + gap;
                g.DrawLine(arrowPen, gapArrowX, firstStickerBottom, gapArrowX, secondStickerTop);
                // Arrow heads
                g.DrawLine(arrowPen, gapArrowX, firstStickerBottom, gapArrowX - 3, firstStickerBottom + 3);
                g.DrawLine(arrowPen, gapArrowX, firstStickerBottom, gapArrowX + 3, firstStickerBottom + 3);
                g.DrawLine(arrowPen, gapArrowX, secondStickerTop, gapArrowX - 3, secondStickerTop - 3);
                g.DrawLine(arrowPen, gapArrowX, secondStickerTop, gapArrowX + 3, secondStickerTop - 3);

                // xCoordinate arrow (from left boundary to second sticker)
                int xCoordArrowY = xCoordLabelY - 5; // Position above the label
                g.DrawLine(arrowPen, 0, xCoordArrowY, xCoordinate, xCoordArrowY);
                // Arrow heads
                g.DrawLine(arrowPen, 0, xCoordArrowY, 3, xCoordArrowY - 3);
                g.DrawLine(arrowPen, 0, xCoordArrowY, 3, xCoordArrowY + 3);
                g.DrawLine(arrowPen, xCoordinate, xCoordArrowY, xCoordinate - 3, xCoordArrowY - 3);
                g.DrawLine(arrowPen, xCoordinate, xCoordArrowY, xCoordinate - 3, xCoordArrowY + 3);
            }
        }

        private void dimensionVisualizationPictureBox_TwoUp_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // PictureBox dimensions
            int pbWidth = dimensionVisualizationPictureBox_TwoUp.Width;
            int pbHeight = dimensionVisualizationPictureBox_TwoUp.Height;

            // Draw vertical boundary lines (full height)
            using (Pen boundaryPen = new Pen(Color.DarkGray, 2))
            {
                // Left vertical line
                g.DrawLine(boundaryPen, 0, 0, 0, pbHeight);
                // Right vertical line
                g.DrawLine(boundaryPen, pbWidth - 1, 0, pbWidth - 1, pbHeight);
            }

            // Define dimensions for square outer container
            int padding = 20; // Padding around the square
            int availableWidth = pbWidth - 2 * padding;
            int availableHeight = pbHeight - 2 * padding;

            // Calculate sticker dimensions (8 stickers, 4 rows × 2 columns, 1:2 height:width ratio)
            int gap = 5; // 5px gap between stickers
            int numRows = 4;
            int numCols = 2;
            int numStickers = numRows * numCols;
            int numRowGaps = numRows - 1; // Gaps between rows
            int numColGaps = numCols - 1; // Gaps between columns

            // Calculate maximum sticker dimensions that fit
            int maxStickerWidth = (int)(availableWidth * 0.8); // Leave some margin
            int availableWidthForStickers = availableWidth - numColGaps * gap;
            int maxStickerWidthPerSticker = availableWidthForStickers / numCols;

            int availableHeightForStickers = availableHeight - numRowGaps * gap;
            int maxStickerHeightPerSticker = availableHeightForStickers / numRows;

            // Maintain 1:2 height:width ratio
            int stickerHeight = Math.Min(maxStickerHeightPerSticker, maxStickerWidthPerSticker / 2);
            int stickerWidth = stickerHeight * 2; // Exact 1:2 ratio

            // Create square outer container that encloses all stickers
            int totalStickersWidth = numCols * stickerWidth + numColGaps * gap;
            int totalStickersHeight = numRows * stickerHeight + numRowGaps * gap;
            int squareSize = Math.Max(totalStickersWidth + 40, totalStickersHeight + 40); // Add some margin
            int largeRectX = (pbWidth - squareSize) / 2;
            int largeRectY = (pbHeight - squareSize) / 2;
            int largeRectWidth = squareSize;
            int largeRectHeight = squareSize;

            // Center the sticker grid within the square
            int gridStartX = largeRectX + (squareSize - totalStickersWidth) / 2;
            int gridStartY = largeRectY + (squareSize - totalStickersHeight) / 2;

            // Fill areas with background colors
            // Light gray outside the square boundaries
            using (Brush grayBrush = new SolidBrush(Color.LightGray))
            {
                // Left area (from PictureBox left edge to square left edge)
                g.FillRectangle(grayBrush, 0, 0, largeRectX, pbHeight);
                // Right area (from square right edge to PictureBox right edge)
                g.FillRectangle(grayBrush, largeRectX + largeRectWidth, 0, pbWidth - (largeRectX + largeRectWidth), pbHeight);
            }

            // Darker yellow inside square boundaries (between vertical lines)
            using (Brush yellowBrush = new SolidBrush(Color.FromArgb(255, 255, 200))) // Slightly darker yellow
            {
                g.FillRectangle(yellowBrush, largeRectX, largeRectY, largeRectWidth, largeRectHeight);
            }

            // Draw 8 stickers in 4 rows × 2 columns grid (white rectangles over yellow background)
            int stickerIndex = 0;
            for (int row = 0; row < numRows; row++)
            {
                for (int col = 0; col < numCols; col++)
                {
                    int stickerX = gridStartX + col * (stickerWidth + gap);
                    int stickerY = gridStartY + row * (stickerHeight + gap);
                    DrawRoundedRectangle(g, new Rectangle(stickerX, stickerY, stickerWidth, stickerHeight), 8, Color.White);
                }
            }

            // Draw large outer square (paper/label area) - only vertical lines (on top of backgrounds)
            using (Pen blackPen = new Pen(Color.Black, 2))
            {
                // Left vertical line
                g.DrawLine(blackPen, largeRectX, largeRectY, largeRectX, largeRectY + largeRectHeight);
                // Right vertical line
                g.DrawLine(blackPen, largeRectX + largeRectWidth, largeRectY, largeRectX + largeRectWidth, largeRectY + largeRectHeight);
            }

            // Draw sticker names in the center of each rectangle
            using (Font stickerFont = new Font("Microsoft Sans Serif", 8))
            using (Brush stickerTextBrush = new SolidBrush(Color.DarkBlue))
            {
                stickerIndex = 0;
                for (int row = 0; row < numRows; row++)
                {
                    for (int col = 0; col < numCols; col++)
                    {
                        int stickerX = gridStartX + col * (stickerWidth + gap);
                        int stickerY = gridStartY + row * (stickerHeight + gap);
                        string stickerName = $"Sticker-{++stickerIndex}";
                        SizeF textSize = g.MeasureString(stickerName, stickerFont);
                        int textX = stickerX + (stickerWidth - (int)textSize.Width) / 2;
                        int textY = stickerY + (stickerHeight - (int)textSize.Height) / 2;
                        g.DrawString(stickerName, stickerFont, stickerTextBrush, textX, textY);
                    }
                }

                // Draw "Sticker Roll" below the 8 stickers with light yellow background
                string stickerRollText = "Sticker Roll";
                SizeF stickerRollSize = g.MeasureString(stickerRollText, stickerFont);
                int stickerRollX = gridStartX + (totalStickersWidth - (int)stickerRollSize.Width) / 2;
                int stickerRollY = gridStartY + totalStickersHeight + 5; // Below last row

                // Draw darker yellow background rectangle
                using (Brush yellowBrush = new SolidBrush(Color.FromArgb(255, 255, 200))) // Match the square background color
                {
                    int textPadding = 4;
                    g.FillRectangle(yellowBrush,
                        stickerRollX - textPadding,
                        stickerRollY - textPadding,
                        stickerRollSize.Width + 2 * textPadding,
                        stickerRollSize.Height + 2 * textPadding);
                }
                g.DrawString(stickerRollText, stickerFont, stickerTextBrush, stickerRollX, stickerRollY);

                // Draw "Ink Roll" in the lower left corner of PictureBox with light gray background
                string inkRollText = "Ink Roll";
                SizeF inkRollSize = g.MeasureString(inkRollText, stickerFont);
                int inkRollX = 10; // Left margin
                int inkRollY = pbHeight - (int)inkRollSize.Height - 110; // Bottom margin

                // Draw light gray background rectangle
                using (Brush grayBrush = new SolidBrush(Color.LightGray))
                {
                    int textPadding = 4;
                    g.FillRectangle(grayBrush,
                        inkRollX - textPadding,
                        inkRollY - textPadding,
                        inkRollSize.Width + 2 * textPadding,
                        inkRollSize.Height + 2 * textPadding);
                }
                g.DrawString(inkRollText, stickerFont, stickerTextBrush, inkRollX, inkRollY);
            }

            // Declare label positions for use in arrow drawing
            int xCoordLabelY = 0;
            int x2CoordLabelY = 0;

            // Draw dimension labels for two-up mode
            using (Font labelFont = new Font("Microsoft Sans Serif", 9, FontStyle.Regular))
            using (Brush textBrush = new SolidBrush(Color.Black))
            {
                // Height label - across the left vertical line of sticker1 (first sticker)
                string heightText = "Height";
                SizeF heightSize = g.MeasureString(heightText, labelFont);
                int sticker1X = gridStartX; // Left edge of first sticker
                int sticker1Y = gridStartY; // Top edge of first sticker
                int heightLabelX = sticker1X - 70; // Left of the sticker
                int heightLabelY = sticker1Y + (stickerHeight - (int)heightSize.Height) / 2; // Vertically centered on first sticker
                g.DrawString(heightText, labelFont, textBrush, heightLabelX, heightLabelY);

                // Width label - across the upper horizontal line of sticker1 (first sticker)
                string widthText = "Width";
                SizeF widthSize = g.MeasureString(widthText, labelFont);
                int widthLabelX = sticker1X + (stickerWidth - (int)widthSize.Width) / 2; // Horizontally centered on first sticker
                int widthLabelY = sticker1Y - 25; // Above the first sticker
                g.DrawString(widthText, labelFont, textBrush, widthLabelX, widthLabelY);

                // Gap label - vertical distance between sticker1 and sticker3
                string gapText = "Gap";
                SizeF gapSize = g.MeasureString(gapText, labelFont);
                int sticker3Y = gridStartY + stickerHeight + gap; // Top of third sticker (second row)
                int gapLabelX = sticker1X + 50; // Left of the stickers
                int gapLabelY = sticker1Y + stickerHeight + (gap - (int)gapSize.Height) / 2; // Vertically centered in the gap
                g.DrawString(gapText, labelFont, textBrush, gapLabelX, gapLabelY);

                // xCoordinate label - horizontal distance from picture box left to left sticker in second column (sticker5)
                string xCoordText = "xCoordinate";
                SizeF xCoordSize = g.MeasureString(xCoordText, labelFont);
                int sticker5X = gridStartX ; // Left edge of sticker5 (row 2, col 0 - left sticker in second column)
                int xCoordLabelX = (sticker5X - (int)xCoordSize.Width) / 2 -10; // Centered in the space before sticker5
                xCoordLabelY = gridStartY - 40+150; // Above the sticker grid
                g.DrawString(xCoordText, labelFont, textBrush, xCoordLabelX, xCoordLabelY);

                // x2Coordinate label - horizontal distance from picture box left to right sticker in second column (sticker6)
                string x2CoordText = "x2Coordinate";
                SizeF x2CoordSize = g.MeasureString(x2CoordText, labelFont);
                int sticker6X = gridStartX +  (stickerWidth + gap); // Left edge of sticker6 (row 2, col 1 - right sticker in second column)
                int x2CoordLabelX = (sticker5X - (int)xCoordSize.Width) / 2 - 15;
                x2CoordLabelY = gridStartY - 25+300; // Above the sticker grid, below xCoordinate
                g.DrawString(x2CoordText, labelFont, textBrush, x2CoordLabelX, x2CoordLabelY);
            }

            // Draw dimension arrows/lines
            using (Pen arrowPen = new Pen(Color.Gray, 1))
            {
                // Width arrow (across the top of first sticker)
                int widthArrowY = gridStartY - 10; // Above the first sticker
                g.DrawLine(arrowPen, gridStartX, widthArrowY, gridStartX + stickerWidth, widthArrowY);
                // Arrow heads
                g.DrawLine(arrowPen, gridStartX, widthArrowY, gridStartX + 3, widthArrowY - 3);
                g.DrawLine(arrowPen, gridStartX, widthArrowY, gridStartX + 3, widthArrowY + 3);
                g.DrawLine(arrowPen, gridStartX + stickerWidth, widthArrowY, gridStartX + stickerWidth - 3, widthArrowY - 3);
                g.DrawLine(arrowPen, gridStartX + stickerWidth, widthArrowY, gridStartX + stickerWidth - 3, widthArrowY + 3);

                // Height arrow (along the left side of first sticker)
                int heightArrowX = gridStartX - 15; // Left of the first sticker
                g.DrawLine(arrowPen, heightArrowX, gridStartY, heightArrowX, gridStartY + stickerHeight);
                // Arrow heads
                g.DrawLine(arrowPen, heightArrowX, gridStartY, heightArrowX - 3, gridStartY + 3);
                g.DrawLine(arrowPen, heightArrowX, gridStartY, heightArrowX + 3, gridStartY + 3);
                g.DrawLine(arrowPen, heightArrowX, gridStartY + stickerHeight, heightArrowX - 3, gridStartY + stickerHeight - 3);
                g.DrawLine(arrowPen, heightArrowX, gridStartY + stickerHeight, heightArrowX + 3, gridStartY + stickerHeight - 3);

                // Gap arrow (vertical distance between sticker1 and sticker3)
                int gapArrowX = gridStartX - 10; // Left of the stickers
                int gapArrowStartY = gridStartY + stickerHeight; // Bottom of first sticker
                int gapArrowEndY = gridStartY + stickerHeight + gap; // Top of third sticker
                g.DrawLine(arrowPen, gapArrowX, gapArrowStartY, gapArrowX, gapArrowEndY);
                // Arrow heads
                g.DrawLine(arrowPen, gapArrowX, gapArrowStartY, gapArrowX - 3, gapArrowStartY + 3);
                g.DrawLine(arrowPen, gapArrowX, gapArrowStartY, gapArrowX + 3, gapArrowStartY + 3);
                g.DrawLine(arrowPen, gapArrowX, gapArrowEndY, gapArrowX - 3, gapArrowEndY - 3);
                g.DrawLine(arrowPen, gapArrowX, gapArrowEndY, gapArrowX + 3, gapArrowEndY - 3);

                // xCoordinate arrow (from picture box left to sticker5 left)
                int xCoordArrowY = xCoordLabelY + 15; // Below the xCoordinate label
                int sticker5X = gridStartX; // Left edge of sticker5 (left sticker in second column)
                g.DrawLine(arrowPen, 0, xCoordArrowY, sticker5X, xCoordArrowY);
                // Arrow heads
                g.DrawLine(arrowPen, 0, xCoordArrowY, 3, xCoordArrowY - 3);
                g.DrawLine(arrowPen, 0, xCoordArrowY, 3, xCoordArrowY + 3);
                g.DrawLine(arrowPen, sticker5X, xCoordArrowY, sticker5X - 3, xCoordArrowY - 3);
                g.DrawLine(arrowPen, sticker5X, xCoordArrowY, sticker5X - 3, xCoordArrowY + 3);

                // x2Coordinate arrow (from picture box left to sticker6 left)
                int x2CoordArrowY = x2CoordLabelY + 15; // Below the x2Coordinate label
                int sticker6X = gridStartX + (stickerWidth + gap); // Left edge of sticker6 (right sticker in second column)
                g.DrawLine(arrowPen, 0, x2CoordArrowY, sticker6X, x2CoordArrowY);
                // Arrow heads
                g.DrawLine(arrowPen, 0, x2CoordArrowY, 3, x2CoordArrowY - 3);
                g.DrawLine(arrowPen, 0, x2CoordArrowY, 3, x2CoordArrowY + 3);
                g.DrawLine(arrowPen, sticker6X, x2CoordArrowY, sticker6X - 3, x2CoordArrowY - 3);
                g.DrawLine(arrowPen, sticker6X, x2CoordArrowY, sticker6X - 3, x2CoordArrowY + 3);
            }
        }

        private void DrawRoundedRectangle(Graphics g, Rectangle rect, int cornerRadius, Color fillColor)
        {
            using (SolidBrush brush = new SolidBrush(fillColor))
            using (Pen pen = new Pen(Color.Black, 1))
            {
                // Create rounded rectangle path
                System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddArc(rect.X, rect.Y, cornerRadius * 2, cornerRadius * 2, 180, 90);
                path.AddArc(rect.X + rect.Width - cornerRadius * 2, rect.Y, cornerRadius * 2, cornerRadius * 2, 270, 90);
                path.AddArc(rect.X + rect.Width - cornerRadius * 2, rect.Y + rect.Height - cornerRadius * 2, cornerRadius * 2, cornerRadius * 2, 0, 90);
                path.AddArc(rect.X, rect.Y + rect.Height - cornerRadius * 2, cornerRadius * 2, cornerRadius * 2, 90, 90);
                path.CloseFigure();

                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }
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
            // Advanced settings are always enabled now
            
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

        // Tooltips are now set in the new InitializeAdvancedSettings method

        private void InitializeProductAndCropSelectors()
        {
            // Prepare manual fallback options by default
            if (_productOptionsByCrop == null || _productOptionsByCrop.Count == 0)
            {
                SetManualFallbackProductOptions();
            }
            _pendingPrinterProductSelection = null;

            // Populate 000..999 for Product and Crop (scanner filters only)
            if (comboBox_ProductID != null)
            {
                ResetProductComboForNoSelection();
            }
            if (comboBox_CropID != null || cropIdComboBox != null)
            {
                if (_cropOptionsCache != null && _cropOptionsCache.Count > 0)
                {
                    ApplyCropOptionsToCombos();
                }
                else
                {
                    ApplyFallbackCropOptions(null, updateStatusLabel: false);
                }
            }

            SetProductDetailFields("N/A", "N/A", "N/A", "N/A");
        }

        private List<CropComboItem> CreateCropComboItemsFromCache(bool includeEmpty)
        {
            // First try to use cached API data
            if (_productCombinationsService != null && _productCombinationsService.HasCachedData())
            {
                var crops = _productCombinationsService.GetUniqueCrops();
                if (crops != null && crops.Count > 0)
                {
                    var items = crops
                        .Select(c => new CropComboItem { Name = c.crop_name, Value = c.crop_id })
                        .ToList();

                    if (includeEmpty)
                    {
                        items.Insert(0, new CropComboItem { Name = "All Crops", Value = "" });
                    }

                    return items;
                }
            }

            // Fallback to old cache if API data is not available
            if (_cropOptionsCache == null || _cropOptionsCache.Count == 0) return null;

            var fallbackItems = _cropOptionsCache
                .Select(c => new CropComboItem { Name = c.CropName, Value = c.CropIdShort })
                .ToList();

            if (includeEmpty)
            {
                fallbackItems.Insert(0, new CropComboItem { Name = "All Crops", Value = "" });
            }

            return fallbackItems;
        }

        private List<CropComboItem> CreateFallbackCropComboItems(bool includeEmpty)
        {
            var items = new List<CropComboItem>
            {
                new CropComboItem { Name = "manual-c1", Value = "c1" },
                new CropComboItem { Name = "manual-c2", Value = "c2" },
                new CropComboItem { Name = "manual-c3", Value = "c3" }
            };

            if (includeEmpty)
            {
                items.Insert(0, new CropComboItem { Name = "All Crops", Value = "" });
            }

            return items;
        }

        private string GetCartonNameFromId(string cartonId, string cartonName = null)
        {
            if (!string.IsNullOrWhiteSpace(cartonName)) return cartonName;
            if (string.IsNullOrWhiteSpace(cartonId)) return "-";
            return $"Unknown ({cartonId})";
        }

        private List<ProductComboItem> CreateProductComboItemsFromCache(string cropCode)
        {
            if (string.IsNullOrEmpty(cropCode)) return null;

            // First try to use cached API data
            if (_productCombinationsService != null && _productCombinationsService.HasCachedData())
            {
                var products = _productCombinationsService.GetProductsForCrop(cropCode);
                if (products != null && products.Count > 0)
                {
                    return products
                        .Select(p => new ProductComboItem {
                            Name = $"{p.product_name}  ({p.variety_name} | {p.grade_name} | {p.count_name} | {GetCartonNameFromId(p.carton_type_id, p.carton_type_name)} | {p.avg_weight_kg}kg)",
                            Value = p.product_id,
                            ProductName = p.product_name,
                            Variety = p.variety_name,
                            Grade = p.grade_name,
                            Count = p.count_name,
                            Carton = GetCartonNameFromId(p.carton_type_id, p.carton_type_name),
                            AvgWeightKg = p.avg_weight_kg.ToString(),
                            Combination = p
                        })
                        .ToList();
                }
            }

            // Fallback to old cache if API data is not available
            if (_productOptionsByCrop == null) return null;
            if (!_productOptionsByCrop.TryGetValue(cropCode, out var fallbackProducts) || fallbackProducts == null || fallbackProducts.Count == 0)
                return null;

            return fallbackProducts
                .Select(p => new ProductComboItem {
                    Name = $"{p.ProductName}  ({p.VarietyName} | {p.GradeName} | {p.CountName} | {GetCartonNameFromId(p.CartonTypeId)})",
                    Value = p.ProductIdShort,
                    ProductName = p.ProductName,
                    Variety = p.VarietyName,
                    Grade = p.GradeName,
                    Count = p.CountName,
                    Carton = GetCartonNameFromId(p.CartonTypeId),
                    AvgWeightKg = p.AvgWeightKg.ToString()
                })  
                .ToList();
        }

        private List<ProductComboItem> CreateScannerProductItemsFromCache(string cropId = null)
        {
            // First try to use cached API data
            if (_productCombinationsService != null && _productCombinationsService.HasCachedData())
            {
                var allCombinations = _productCombinationsService.GetAllCombinations();
                if (allCombinations != null && allCombinations.Count > 0)
                {
                    // If cropId is specified, filter combinations by crop
                    var combinationsToUse = string.IsNullOrEmpty(cropId)
                        ? allCombinations
                        : allCombinations.Where(c => c.crop_id == cropId).ToList();

                    return combinationsToUse
                        .GroupBy(p => p.product_id, StringComparer.OrdinalIgnoreCase)
                        .Select(g => g.First())
                        .OrderBy(p => p.product_name, StringComparer.OrdinalIgnoreCase)
                        .Select(p => new ProductComboItem {
                            Name = $"{p.product_name}",
                            Value = p.product_id
                        })
                        .ToList();
                }
            }

            // Fallback to old cache if API data is not available
            return CreateScannerProductItemsFromFallbackCache(cropId);
        }

        private List<ProductComboItem> CreateScannerProductItemsFromFallbackCache(string cropId = null)
        {
            if (_productOptionsByCrop == null || _productOptionsByCrop.Count == 0) return null;

            // If cropId is specified, filter products by crop
            if (!string.IsNullOrEmpty(cropId))
            {
                if (_productOptionsByCrop.ContainsKey(cropId))
                {
                    return _productOptionsByCrop[cropId]
                        .GroupBy(p => p.ProductIdShort ?? p.ProductIdFull ?? "", StringComparer.OrdinalIgnoreCase)
                        .Select(g => g.First())
                        .OrderBy(p => p.ProductName, StringComparer.OrdinalIgnoreCase)
                        .Select(p => new ProductComboItem {
                            Name = $"{p.ProductName}",
                            Value = p.ProductIdShort
                        })
                        .ToList();
                }
                else
                {
                    // If no products found for the crop, return empty list
                    return new List<ProductComboItem>();
                }
            }

            // If no cropId specified, return all products from fallback
            return _productOptionsByCrop
                .SelectMany(kvp => kvp.Value)
                .GroupBy(p => p.ProductIdShort ?? p.ProductIdFull ?? "", StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(p => p.ProductName, StringComparer.OrdinalIgnoreCase)
                .Select(p => new ProductComboItem {
                    Name = $"{p.ProductName}",
                    Value = p.ProductIdShort
                })
                .ToList();
        }

        private List<ProductOption> CreateFallbackProductOptions()
        {
            return new List<ProductOption>
            {
                new ProductOption { ProductIdFull = "manual-p1", ProductIdShort = "p1", ProductName = "manual p1" },
                new ProductOption { ProductIdFull = "manual-p2", ProductIdShort = "p2", ProductName = "manual p2" },
                new ProductOption { ProductIdFull = "manual-p3", ProductIdShort = "p3", ProductName = "manual p3" }
            };
        }

        private void ResetProductComboForNoSelection()
        {
            if (comboBox_ProductID == null) return;
            _suppressProductSearch = true;
            try
            {
                comboBox_ProductID.DataSource = null;
                comboBox_ProductID.Items.Clear();
                comboBox_ProductID.Text = "";
            }
            finally { _suppressProductSearch = false; }
            comboBox_ProductID.Enabled = false;
            _allProductComboItemsForCrop = new List<ProductComboItem>(); // no crop -> nothing to search
            _lastValidProductName = "";
            _pendingPrinterProductSelection = null;
            SetProductDetailFields("N/A", "N/A", "N/A", "N/A");
        }

        private void SetProductComboItems(List<ProductComboItem> items, string preferredValue)
        {
            if (comboBox_ProductID == null) return;

            string previousValue = null;
            try { previousValue = comboBox_ProductID.SelectedValue?.ToString(); } catch { previousValue = null; }

            // Cache the full, unfiltered set for this crop so live search can always filter from the
            // complete list (and reset to it when the query is cleared).
            _allProductComboItemsForCrop = (items ?? new List<ProductComboItem>())
                .Where(i => i != null && !i.IsHeader)
                .ToList();

            if (_allProductComboItemsForCrop.Count == 0)
            {
                _suppressProductSearch = true;
                try
                {
                    comboBox_ProductID.DataSource = null;
                    comboBox_ProductID.Items.Clear();
                    comboBox_ProductID.Text = "";
                }
                finally { _suppressProductSearch = false; }
                comboBox_ProductID.Enabled = false;
                _lastValidProductName = "";
                return;
            }

            comboBox_ProductID.Enabled = true;

            // Render the full list grouped (no active query) ...
            ApplyProductSearchAndGrouping("");

            // ... then restore the intended selection (skipping header rows).
            string targetValue = !string.IsNullOrEmpty(preferredValue) ? preferredValue : previousValue;
            SelectProductByValue(targetValue);

            try
            {
                _pendingPrinterProductSelection = comboBox_ProductID.SelectedValue?.ToString();
            }
            catch
            {
                _pendingPrinterProductSelection = null;
            }
        }

        // Rebuilds the dropdown items = current crop's combinations, filtered by `query` (matched
        // across all fields) and listed group-wise by variety with non-selectable header rows.
        private void ApplyProductSearchAndGrouping(string query)
        {
            if (comboBox_ProductID == null) return;

            string[] terms = (query ?? "")
                .ToLowerInvariant()
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var filtered = _allProductComboItemsForCrop.Where(p => MatchesProductQuery(p, terms));
            var grouped = BuildGroupedProductItems(filtered);

            _suppressProductSearch = true;
            try
            {
                comboBox_ProductID.DataSource = null;
                comboBox_ProductID.Items.Clear();
                comboBox_ProductID.DisplayMember = nameof(ProductComboItem.Name);
                comboBox_ProductID.ValueMember = nameof(ProductComboItem.Value);
                comboBox_ProductID.DataSource = grouped;
                comboBox_ProductID.SelectedIndex = -1; // don't auto-snap to a row while filtering
            }
            finally { _suppressProductSearch = false; }
        }

        // Groups by variety (what the client asked for), sorting groups alphabetically and the rows
        // inside each by Grade -> Count (natural numeric) -> Carton. A header row is inserted before
        // each group.
        private List<ProductComboItem> BuildGroupedProductItems(IEnumerable<ProductComboItem> source)
        {
            var result = new List<ProductComboItem>();
            var groups = source
                .GroupBy(p => string.IsNullOrWhiteSpace(p.Variety) ? "—" : p.Variety.Trim(), StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var g in groups)
            {
                result.Add(new ProductComboItem
                {
                    IsHeader = true,
                    GroupLabel = $"VARIETY · {g.Key}  ({g.Count()})",
                    Name = $"— {g.Key} —",
                    Value = null
                });

                foreach (var item in g
                    .OrderBy(i => i.Grade ?? "", StringComparer.OrdinalIgnoreCase)
                    .ThenBy(i => CountSortKey(i.Count))
                    .ThenBy(i => i.Carton ?? "", StringComparer.OrdinalIgnoreCase))
                {
                    result.Add(item);
                }
            }
            return result;
        }

        // Sort counts/sizes numerically when possible (40, 65, 100) instead of lexically (100, 40, 65).
        private int CountSortKey(string count)
        {
            string digits = new string((count ?? "").Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out int n) ? n : int.MaxValue;
        }

        // True if every search term is contained somewhere in the row's fields (AND semantics), so
        // "tur 60" matches a Turkey / count-60 combination regardless of column order.
        private bool MatchesProductQuery(ProductComboItem p, string[] terms)
        {
            if (terms == null || terms.Length == 0) return true;
            if (p == null) return false;
            string hay = $"{p.ProductName} {p.Variety} {p.Grade} {p.Count} {p.Carton} {p.AvgWeightKg}".ToLowerInvariant();
            foreach (var t in terms)
                if (hay.IndexOf(t, StringComparison.Ordinal) < 0) return false;
            return true;
        }

        // Selects the row whose Value matches (skips headers). Falls back to the first real row.
        private void SelectProductByValue(string targetValue)
        {
            if (comboBox_ProductID == null) return;

            int firstReal = -1;
            for (int i = 0; i < comboBox_ProductID.Items.Count; i++)
            {
                var it = comboBox_ProductID.Items[i] as ProductComboItem;
                if (it == null || it.IsHeader) continue;
                if (firstReal < 0) firstReal = i;
                if (!string.IsNullOrEmpty(targetValue) &&
                    string.Equals(it.Value, targetValue, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox_ProductID.SelectedIndex = i;
                    _lastValidProductName = it.Name;
                    return;
                }
            }
            if (firstReal >= 0)
            {
                comboBox_ProductID.SelectedIndex = firstReal;
                _lastValidProductName = (comboBox_ProductID.Items[firstReal] as ProductComboItem)?.Name ?? "";
            }
        }

        // User typed in the combo's edit field -> filter the list live and keep it open. Uses
        // TextUpdate (fires only on real keystrokes, not on programmatic selection) to avoid loops.
        private void comboBox_ProductID_TextUpdate(object sender, EventArgs e)
        {
            if (_suppressProductSearch || comboBox_ProductID == null || !comboBox_ProductID.Enabled) return;

            string query = comboBox_ProductID.Text ?? "";
            ApplyProductSearchAndGrouping(query);

            // Setting DataSource wipes the edit text; restore the user's query + caret and keep the
            // list dropped so they see results update as they type. Open the list BEFORE restoring
            // the caret, because toggling DroppedDown resets the text selection.
            _suppressProductSearch = true;
            try
            {
                if (comboBox_ProductID.Items.Count > 0 && !comboBox_ProductID.DroppedDown)
                    comboBox_ProductID.DroppedDown = true;
                comboBox_ProductID.Text = query;
                comboBox_ProductID.SelectionStart = query.Length;
                comboBox_ProductID.SelectionLength = 0;
            }
            finally { _suppressProductSearch = false; }
        }

        // After the list closes on a committed pick, reset it to the full grouped set (keeping the
        // selection). Done while closed so there's no rebinding glitch on an open list; the next time
        // the user opens the dropdown they can browse everything, and typing re-filters. If they
        // closed mid-query without picking, we leave it for Leave to restore.
        private void comboBox_ProductID_DropDownClosed(object sender, EventArgs e)
        {
            if (comboBox_ProductID == null || !comboBox_ProductID.Enabled) return;

            var sel = comboBox_ProductID.SelectedItem as ProductComboItem;
            if (sel == null || sel.IsHeader) return; // mid-query / no pick -> Leave handles it

            string keepValue = sel.Value;
            ApplyProductSearchAndGrouping("");
            SelectProductByValue(keepValue);
        }

        // When the user leaves the combo with a half-typed/invalid query, snap back to the last valid
        // selection so we never carry a non-combination string into barcode generation.
        private void comboBox_ProductID_Leave(object sender, EventArgs e)
        {
            if (comboBox_ProductID == null || !comboBox_ProductID.Enabled) return;

            var sel = comboBox_ProductID.SelectedItem as ProductComboItem;
            if (sel != null && !sel.IsHeader)
            {
                _lastValidProductName = sel.Name;
                return;
            }

            // No valid row selected (query matched nothing or only a header) -> restore.
            _suppressProductSearch = true;
            try
            {
                ApplyProductSearchAndGrouping("");
                if (!string.IsNullOrEmpty(_lastValidProductName))
                {
                    int idx = -1;
                    for (int i = 0; i < comboBox_ProductID.Items.Count; i++)
                    {
                        var it = comboBox_ProductID.Items[i] as ProductComboItem;
                        if (it != null && !it.IsHeader && string.Equals(it.Name, _lastValidProductName, StringComparison.Ordinal))
                        { idx = i; break; }
                    }
                    comboBox_ProductID.SelectedIndex = idx;
                    if (idx < 0) comboBox_ProductID.Text = "";
                }
            }
            finally { _suppressProductSearch = false; }
        }

        private void UpdateProductOptionsForSelectedCrop()
        {
            if (comboBox_ProductID == null) return;

            SetProductDetailFields("N/A", "N/A", "N/A", "N/A");

            string rawValue = comboBox_CropID?.SelectedValue?.ToString();
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                ResetProductComboForNoSelection();
                return;
            }

            string cropCode = NormalizeCode(rawValue);
            if (string.IsNullOrEmpty(cropCode))
            {
                ResetProductComboForNoSelection();
                return;
            }

            var items = CreateProductComboItemsFromCache(cropCode);
            if (items == null || items.Count == 0)
            {
                ResetProductComboForNoSelection();
                return;
            }

            SetProductComboItems(items, _pendingPrinterProductSelection);
        }

        private void SetManualFallbackProductOptions()
        {
            var manualProducts = CreateFallbackProductOptions();
            var manualCropCodes = new[] { "c1", "c2", "c3" };

            _productOptionsByCrop = new Dictionary<string, List<ProductOption>>(StringComparer.OrdinalIgnoreCase);
            _pendingPrinterProductSelection = null;
            foreach (var code in manualCropCodes)
            {
                _productOptionsByCrop[code] = manualProducts
                    .Select(p => new ProductOption
                    {
                        ProductIdFull = p.ProductIdFull,
                        ProductIdShort = p.ProductIdShort,
                        ProductName = p.ProductName
                    })
                    .ToList();
            }

            _scannerProductItems = manualProducts
                .Select(p => new ProductComboItem { Name = p.ProductName, Value = p.ProductIdShort })
                .ToList();

            SetProductDetailFields("N/A", "N/A", "N/A", "N/A");
        }

        private void SetComboItems(ComboBox combo, List<CropComboItem> items, string preferredValue, bool selectFirstOnMissing)
        {
            if (combo == null || items == null)
                return;

            string previousValue = null;
            try { previousValue = combo.SelectedValue?.ToString(); } catch { previousValue = null; }

            combo.DataSource = null;
            combo.Items.Clear();
            combo.DisplayMember = nameof(CropComboItem.Name);
            combo.ValueMember = nameof(CropComboItem.Value);
            combo.DataSource = items;

            string targetValue = !string.IsNullOrEmpty(preferredValue) ? preferredValue : previousValue;

            if (!string.IsNullOrEmpty(targetValue))
            {
                var match = items.FirstOrDefault(i => string.Equals(i.Value, targetValue, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    combo.SelectedValue = match.Value;
                    return;
                }
            }

            if (items.Count > 0)
            {
                if (selectFirstOnMissing)
                {
                    combo.SelectedIndex = 0;
                }
                else
                {
                    combo.SelectedIndex = -1;
                }
            }
        }

        private void ApplyCropOptionsToCombos()
        {
            if ((_cropOptionsCache?.Count ?? 0) == 0)
            {
                ApplyFallbackCropOptions(null, updateStatusLabel: false);
                return;
            }

            var printerItems = CreateCropComboItemsFromCache(includeEmpty: false);
            SetComboItems(comboBox_CropID, printerItems, _pendingPrinterCropSelection, selectFirstOnMissing: false);

            var scannerItems = CreateCropComboItemsFromCache(includeEmpty: true);
            SetComboItems(cropIdComboBox, scannerItems, null, selectFirstOnMissing: true);

            UpdateProductOptionsForSelectedCrop();
        }

        private void ApplyFallbackCropOptions(string message, bool updateStatusLabel)
        {
            SetManualFallbackProductOptions();

            var printerItems = CreateFallbackCropComboItems(includeEmpty: false);
            SetComboItems(comboBox_CropID, printerItems, _pendingPrinterCropSelection, selectFirstOnMissing: false);

            var scannerItems = CreateFallbackCropComboItems(includeEmpty: true);
            SetComboItems(cropIdComboBox, scannerItems, null, selectFirstOnMissing: true);

            if (updateStatusLabel && !string.IsNullOrEmpty(message) && statusLabel != null)
            {
                statusLabel.Text = message;
                statusLabel.ForeColor = Color.FromArgb(231, 76, 60);
            }

            if (!string.IsNullOrEmpty(message) && scannerContentPanel.Visible && scannerOutputTextBox != null)
            {
                scannerOutputTextBox.AppendText(message + "\r\n");
            }

            UpdateProductOptionsForSelectedCrop();
        }

        private async Task<bool> InitializeScannerSystemAsync()
        {
            try
            {
                // Run initialization tasks on UI thread to prevent deadlocks and cross-thread operation exceptions
                InitializeScannerDataGridView();
                LoadScansData();
                StartScanFileMonitoring();
                InitializeComPortScanners();

                // Layout needs to be done on UI thread
                if (InvokeRequired)
                {
                    Invoke(new Action(() => LayoutRootPanels()));
                }
                else
                {
                    LayoutRootPanels();
                }

                // This is already async
                bool cropOptionsSuccess = await EnsureCropOptionsLoadedAsync(updateStatusLabel: false);

                return cropOptionsSuccess;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Scanner system initialization failed: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> EnsureCropOptionsLoadedAsync(bool updateStatusLabel)
        {
            if (_cropOptionsCache != null && _cropOptionsCache.Count > 0)
            {
                UpdateProductOptionsForSelectedCrop();
                return true;
            }

            if (_cropOptionsLoadTask == null
                || _cropOptionsLoadTask.IsFaulted
                || _cropOptionsLoadTask.IsCanceled
                || (_cropOptionsLoadTask.IsCompleted && (_cropOptionsCache == null || _cropOptionsCache.Count == 0)))
            {
                _cropOptionsLoadTask = LoadCropOptionsFromApiAsync();
            }

            bool success = await _cropOptionsLoadTask;

            if (success)
            {
                ApplyCropOptionsToCombos();
                LayoutRootPanels();
                return true;
            }

            if (string.IsNullOrEmpty(_cropOptionsErrorMessage))
            {
                _cropOptionsErrorMessage = "⚠️ Unable to load crop list. Using manual entries.";
            }

            ApplyFallbackCropOptions(_cropOptionsErrorMessage, updateStatusLabel);
            LayoutRootPanels();
            return false;
        }

        private async Task<bool> LoadCropOptionsFromApiAsync()
        {
            string token = _apiAuthService?.GetCurrentToken();
            if (string.IsNullOrEmpty(token))
            {
                _cropOptionsErrorMessage = "⚠️ No authentication token available. Using manual crop entries.";
                _cropOptionsCache = new List<CropOption>();
                SetManualFallbackProductOptions();
                return false;
            }

            try
            {
                using (var client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, "https://backend-stage.labourlinksoftware.co.za/user/v1/scan-link/product-combinations");
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    var response = await client.SendAsync(request);
                    if (!response.IsSuccessStatusCode)
                    {
                        _cropOptionsErrorMessage = $"⚠️ Unable to load crop list (HTTP {(int)response.StatusCode}). Using manual entries.";
                        _cropOptionsCache = new List<CropOption>();
                        SetManualFallbackProductOptions();
                        return false;
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                    var data = serializer.Deserialize<ProductCombinationResponse>(json);

                    if (data?.combinations == null || data.combinations.Count == 0)
                    {
                        _cropOptionsErrorMessage = "⚠️ Crop list response was empty. Using manual entries.";
                        _cropOptionsCache = new List<CropOption>();
                        SetManualFallbackProductOptions();
                        return false;
                    }

                    var deduped = data.combinations
                        .Where(c => !string.IsNullOrWhiteSpace(c.crop_id) && !string.IsNullOrWhiteSpace(c.crop_name))
                        .GroupBy(c => c.crop_id)
                        .Select(g => new CropOption
                        {
                            CropIdFull = g.Key,
                            CropIdShort = ExtractCropShortId(g.Key),
                            CropName = g.First().crop_name
                        })
                        .OrderBy(o => o.CropName)
                        .ToList();

                    if (deduped.Count == 0)
                    {
                        _cropOptionsErrorMessage = "⚠️ No crops returned from API. Using manual entries.";
                        _cropOptionsCache = new List<CropOption>();
                        SetManualFallbackProductOptions();
                        return false;
                    }

                    var productGroups = data.combinations
                        .Where(c =>
                            !string.IsNullOrWhiteSpace(c.crop_id) &&
                            !string.IsNullOrWhiteSpace(c.id) &&
                            !string.IsNullOrWhiteSpace(c.product_name))
                        .GroupBy(c => ExtractCropShortId(c.crop_id))
                        .ToDictionary(
                            g => g.Key,
                            g => g
                                .Select(p => new ProductOption
                                {
                                    ProductIdFull = p.id,
                                    ProductIdShort = ExtractProductShortId(p.id),
                                    ProductName = p.product_name,
                                    VarietyName = p.variety_name,
                                    GradeName = p.grade_name,
                                    CountName = p.count_name,
                                    CartonTypeId = p.carton_type_id
                                })
                                .OrderBy(p => p.ProductName, StringComparer.OrdinalIgnoreCase)
                                .ToList()
                        , StringComparer.OrdinalIgnoreCase);

                    if (productGroups.Count > 0)
                    {
                        _productOptionsByCrop = productGroups;
                        _scannerProductItems = CreateScannerProductItemsFromCache();
                    }
                    else
                    {
                        SetManualFallbackProductOptions();
                    }
                    _cropOptionsCache = deduped;
                    _cropOptionsErrorMessage = "";
                    return true;
                }
            }
            catch (Exception ex)
            {
                _cropOptionsErrorMessage = $"⚠️ Unable to load crop list ({ex.Message}). Using manual entries.";
                _cropOptionsCache = new List<CropOption>();
                SetManualFallbackProductOptions();
                return false;
            }
        }

        private string ExtractCropShortId(string fullId)
        {
            if (string.IsNullOrEmpty(fullId)) return "";
            return fullId.Length >= 3 ? fullId.Substring(fullId.Length - 3) : fullId;
        }

        private string ExtractProductShortId(string fullId)
        {
            if (string.IsNullOrEmpty(fullId)) return "";
            return fullId.Length >= 3 ? fullId.Substring(fullId.Length - 3) : fullId;
        }

        private string GetSelectedCropCode()
        {
            string raw = comboBox_CropID?.SelectedValue?.ToString() ?? "";
            return NormalizeCropCode(raw);
        }

        private string GetSelectedProductCode()
        {
            string raw = comboBox_ProductID?.SelectedValue?.ToString() ?? "";
            return NormalizeProductCode(raw);
        }

        private string NormalizeCropCode(string raw)
        {
            return NormalizeCode(raw);
        }

        private string NormalizeProductCode(string raw)
        {
            return NormalizeCode(raw);
        }

        private string NormalizeCode(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "000";
            raw = raw.Trim();
            if (raw.All(char.IsDigit))
            {
                return raw.Length >= 3 ? raw.Substring(raw.Length - 3) : raw.PadLeft(3, '0');
            }
            return raw;
        }

        private string ExtractDigits(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            return new string(value.Where(char.IsDigit).ToArray());
        }

        private string NormalizeNumericComponent(string raw, int requiredLength)
        {
            string digitsOnly = ExtractDigits(raw);
            if (digitsOnly.Length >= requiredLength)
            {
                return digitsOnly.Substring(digitsOnly.Length - requiredLength, requiredLength);
            }
            return digitsOnly.PadLeft(requiredLength, '0');
        }

        // Guard digit prefix for UPC-A barcodes. This known first digit allows detection
        // of scanners that suppress the UPC-A Number System digit.
        // Barcode layout: Guard(1) + Employee(4) + Product(3) + Crop(3) + Check(1) = 12
        private const string UpcGuardDigit = "1";

        private string BuildUpc11Payload(out string employeeSegment, out string productSegment, out string cropSegment)
        {
            employeeSegment = NormalizeNumericComponent(textBox_EmployeeID?.Text ?? "", 4);
            productSegment = NormalizeNumericComponent(GetSelectedProductCode(), 3);
            cropSegment = NormalizeNumericComponent(GetSelectedCropCode(), 3);
            return $"{UpcGuardDigit}{employeeSegment}{productSegment}{cropSegment}";
        }

        private string BuildUpcBarcodeFromInputs()
        {
            string employeeSegment, productSegment, cropSegment;
            string upc11 = BuildUpc11Payload(out employeeSegment, out productSegment, out cropSegment);
            int checkDigit = GetUpcCheckDigit(upc11);
            return $"{upc11}{checkDigit}";
        }

        private static int GetUpcCheckDigit(string upc11)
        {
            if (string.IsNullOrWhiteSpace(upc11) || upc11.Length != 11 || !upc11.All(char.IsDigit))
            {
                throw new ArgumentException("UPC-11 payload must contain exactly 11 digits.", nameof(upc11));
            }

            int sumOdd = 0;
            int sumEven = 0;
            for (int i = 0; i < 11; i++)
            {
                int digit = upc11[i] - '0';
                if (i % 2 == 0)
                {
                    sumOdd += digit;
                }
                else
                {
                    sumEven += digit;
                }
            }

            int remainder = (sumOdd * 3 + sumEven) % 10;
            return remainder == 0 ? 0 : 10 - remainder;
        }

        private CropItem GetSelectedCropItem()
        {
            string cropCode = comboBox_CropID?.SelectedValue?.ToString() ?? "";
            if (string.IsNullOrEmpty(cropCode)) return null;

            // First try to get from API data
            if (_productCombinationsService != null && _productCombinationsService.HasCachedData())
            {
                var crops = _productCombinationsService.GetUniqueCrops();
                return crops?.FirstOrDefault(c => string.Equals(c.crop_id, cropCode, StringComparison.OrdinalIgnoreCase));
            }

            return null;
        }

        // Keep the old method for backward compatibility but use the new one
        private CropOption GetSelectedCropOption()
        {
            var cropItem = GetSelectedCropItem();
            if (cropItem == null) return null;

            // Convert CropItem back to CropOption for backward compatibility
            return new CropOption
            {
                CropIdFull = cropItem.crop_id,
                CropIdShort = cropItem.crop_id,
                CropName = cropItem.crop_name
            };
        }

        private ScanLink.ProductCombination GetSelectedProductCombination()
        {
            // Prefer the exact combination the user selected. The combo's Value is only product_id,
            // which is shared across grades (e.g. product_id 132/133 = Delta/60 carry both Grade A and
            // Grade B), so resolving by product_id picks whichever grade is stored first -> wrong grade
            // on the preview/printed label. The selected row already carries its own combination.
            if (comboBox_ProductID?.SelectedItem is ProductComboItem selectedItem && selectedItem.Combination != null)
            {
                return selectedItem.Combination;
            }

            string cropCode = comboBox_CropID?.SelectedValue?.ToString() ?? "";
            string productCode = comboBox_ProductID?.SelectedValue?.ToString();
            if (string.IsNullOrEmpty(cropCode) || string.IsNullOrEmpty(productCode)) return null;

            // First try to get from API data
            if (_productCombinationsService != null && _productCombinationsService.HasCachedData())
            {
                var products = _productCombinationsService.GetProductsForCrop(cropCode);
                return products?.FirstOrDefault(p => string.Equals(p.product_id, productCode, StringComparison.OrdinalIgnoreCase));
            }

            // Fallback to old system (should not happen with API data, but just in case)
            if (_productOptionsByCrop != null && _productOptionsByCrop.TryGetValue(cropCode, out var fallbackProducts) && fallbackProducts != null)
            {
                // Convert ProductOption to a compatible format if needed
                var productOption = fallbackProducts.FirstOrDefault(p => string.Equals(p.ProductIdShort, productCode, StringComparison.OrdinalIgnoreCase));
                if (productOption != null)
                {
                    // Create a ProductCombination from ProductOption for compatibility
                    return new ScanLink.ProductCombination
                    {
                        id = productOption.ProductIdShort,
                        crop_id = cropCode,
                        crop_name = "Unknown", // We don't have this info
                        product_id = productOption.ProductIdShort,
                        product_name = productOption.ProductName,
                        variety_id = "unknown",
                        variety_name = productOption.VarietyName ?? "Unknown",
                        grade_id = "unknown",
                        grade_name = productOption.GradeName ?? "Unknown",
                        count_id = "unknown",
                        count_name = productOption.CountName ?? "Unknown",
                        carton_type_id = productOption.CartonTypeId,
                        avg_weight_kg = productOption.AvgWeightKg
                    };
                }
            }

            return null;
        }

        // Keep the old method for backward compatibility but use the new one
        private ProductOption GetSelectedProductOption()
        {
            var combination = GetSelectedProductCombination();
            if (combination == null) return null;

            // Convert ProductCombination back to ProductOption for backward compatibility
            return new ProductOption
            {
                ProductIdFull = combination.product_id,
                ProductIdShort = combination.product_id,
                ProductName = combination.product_name,
                VarietyName = combination.variety_name,
                GradeName = combination.grade_name,
                CountName = combination.count_name,
                CartonTypeId = combination.carton_type_id,
                AvgWeightKg = combination.avg_weight_kg
            };
        }

        private string GetSelectedCropFullId()
        {
            var option = GetSelectedCropOption();
            if (!string.IsNullOrEmpty(option?.CropIdFull)) return option.CropIdFull;

            string code = comboBox_CropID?.SelectedValue?.ToString();
            if (string.IsNullOrWhiteSpace(code)) return null;
            return $"manual-{NormalizeCode(code)}";
        }

        private string GetSelectedProductFullId()
        {
            var option = GetSelectedProductOption();
            if (!string.IsNullOrEmpty(option?.ProductIdFull)) return option.ProductIdFull;

            string code = comboBox_ProductID?.SelectedValue?.ToString();
            if (string.IsNullOrWhiteSpace(code)) return null;
            return $"manual-{NormalizeCode(code)}";
        }

        private bool IsManualIdentifier(string value)
        {
            return string.IsNullOrWhiteSpace(value) || value.StartsWith("manual", StringComparison.OrdinalIgnoreCase);
        }

        private void SetProductDetailFields(string variety, string grade, string count, string weight = null, string cartonId = null, string cartonName = null)
        {
            if (label_ProductDetail == null) return;
            if (label_ProductDetail.IsDisposed) return;

            if (label_ProductDetail.InvokeRequired)
            {
                label_ProductDetail.Invoke(new Action(() => SetProductDetailFields(variety, grade, count, weight, cartonId, cartonName)));
                return;
            }

            string weightText = string.IsNullOrWhiteSpace(weight) ? "N/A" : weight;
            string cartonText = GetCartonNameFromId(cartonId, cartonName);

            label_ProductDetail.Text = $"Variety: {(string.IsNullOrWhiteSpace(variety) ? "N/A" : variety)}      Grade: {(string.IsNullOrWhiteSpace(grade) ? "N/A" : grade)}      Count: {(string.IsNullOrWhiteSpace(count) ? "N/A" : count)}      Carton: {cartonText}      Avg Weight (kg): {weightText}";
        }

        private void UpdateProductDetailFieldsFromSelection()
        {
            var selectedProduct = GetSelectedProductOption();
            if (selectedProduct != null)
            {
                SetProductDetailFields(
                    selectedProduct.VarietyName ?? "N/A",
                    selectedProduct.GradeName ?? "N/A",
                    selectedProduct.CountName ?? "N/A",
                    selectedProduct.AvgWeightKg.ToString(),
                    selectedProduct.CartonTypeId
                );
            }
            else
            {
                SetProductDetailFields("N/A", "N/A", "N/A", "N/A", null);
            }
        }

        private async Task UpdateProductDetailFieldsAsync()
        {
            if (label_ProductDetail == null || comboBox_CropID == null || comboBox_ProductID == null)
            {
                return;
            }

            string cropFull = GetSelectedCropFullId();
            string productFull = GetSelectedProductFullId();

            if (string.IsNullOrEmpty(cropFull) || string.IsNullOrEmpty(productFull) ||
                IsManualIdentifier(cropFull) || IsManualIdentifier(productFull))
            {
                SetProductDetailFields("N/A", "N/A", "N/A", "N/A", null);
                return;
            }

            string token = _apiAuthService?.GetCurrentToken();
            if (string.IsNullOrEmpty(token))
            {
                SetProductDetailFields("N/A", "N/A", "N/A", "N/A", null);
                return;
            }

            SetProductDetailFields("N/A", "N/A", "N/A", "N/A", null);

            _productDetailCts?.Cancel();
            _productDetailCts = new CancellationTokenSource();
            var ct = _productDetailCts.Token;

            try
            {
                using (var client = new HttpClient())
                using (var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"https://backend-stage.labourlinksoftware.co.za/user/v1/scan-link/product-combinations/filter?cropId={Uri.EscapeDataString(cropFull)}&productId={Uri.EscapeDataString(productFull)}"))
                {
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    using (var response = await client.SendAsync(request, ct))
                    {
                        if (ct.IsCancellationRequested) return;

                        if (!response.IsSuccessStatusCode)
                        {
                            SetProductDetailFields("N/A", "N/A", "N/A", "N/A", null);
                            return;
                        }

                        string json = await response.Content.ReadAsStringAsync();
                        if (ct.IsCancellationRequested) return;

                        var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                        var data = serializer.Deserialize<ProductCombinationResponse>(json);
                        var combination = data?.combinations?.FirstOrDefault();

                        if (combination == null)
                        {
                            SetProductDetailFields("N/A", "N/A", "N/A", "N/A", null);
                            return;
                        }

                        SetProductDetailFields(combination.variety_name, combination.grade_name, combination.count_name, combination.avg_weight_kg.ToString(), combination.carton_type_id, combination.carton_type_name);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            catch
            {
                if (!ct.IsCancellationRequested)
                {
                    SetProductDetailFields("N/A", "N/A", "N/A", "N/A", null);
                }
            }
        }
        
        private void textBox_EmployeeID_TextChanged(object sender, EventArgs e)
        {
            // Update status to show current Employee ID
            string currentText = !string.IsNullOrWhiteSpace(textBox_EmployeeID.Text) ? textBox_EmployeeID.Text : "Default: 23456";
            statusLabel.Text = $"Employee ID updated: '{currentText}' - Click Preview to see visual representation";
            statusLabel.ForeColor = System.Drawing.Color.FromArgb(52, 152, 219);
        }
        // Finds the next non-header row from `start` moving in `dir` (+1 down / -1 up). -1 if none.
        private int NextSelectableProductIndex(int start, int dir)
        {
            if (comboBox_ProductID == null) return -1;
            for (int i = start + dir; i >= 0 && i < comboBox_ProductID.Items.Count; i += dir)
            {
                var it = comboBox_ProductID.Items[i] as ProductComboItem;
                if (it != null && !it.IsHeader) return i;
            }
            return -1;
        }

        private void comboBox_ProductID_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_ProductID == null || !comboBox_ProductID.Enabled)
            {
                return;
            }

            // Ignore selection churn caused by our own item rebuilds during search.
            if (_suppressProductSearch) return;

            // Group headers aren't selectable: if the user lands on one (arrow keys), hop to the
            // nearest real row and let that re-entry do the real work.
            var current = comboBox_ProductID.SelectedItem as ProductComboItem;
            if (current != null && current.IsHeader)
            {
                int idx = comboBox_ProductID.SelectedIndex;
                int next = NextSelectableProductIndex(idx, +1);
                if (next < 0) next = NextSelectableProductIndex(idx, -1);
                if (next >= 0 && next != idx) comboBox_ProductID.SelectedIndex = next;
                return;
            }

            if (current != null) _lastValidProductName = current.Name;

            string selectedValue = comboBox_ProductID.SelectedValue?.ToString() ?? "";
            string displayText = comboBox_ProductID.Text ?? "";
            _pendingPrinterProductSelection = selectedValue;

            // Only update statusLabel if it's accessible (not in popup context)
            if (statusLabel != null && !statusLabel.IsDisposed && statusLabel.Parent != null)
            {
                string message = string.IsNullOrEmpty(displayText)
                    ? "Product selection cleared."
                    : $"Product updated: '{displayText}' ({selectedValue}) - Click Preview to see visual representation";
                statusLabel.Text = message;
                statusLabel.ForeColor = System.Drawing.Color.FromArgb(52, 152, 219);
            }

            SaveAdvancedSettings();

            // Update product details directly from selected product option
            UpdateProductDetailFieldsFromSelection();
        }

        private void comboBox_ProductID_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            ComboBox combo = sender as ComboBox;
            var item = combo.Items[e.Index] as ProductComboItem;

            // Handle the collapsed edit box view
            if ((e.State & DrawItemState.ComboBoxEdit) == DrawItemState.ComboBoxEdit)
            {
                e.DrawBackground();
                string text = item?.Name ?? combo.GetItemText(combo.Items[e.Index]);
                using (Brush editBrush = new SolidBrush(e.ForeColor))
                {
                    e.Graphics.DrawString(text, e.Font, editBrush, e.Bounds, new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter });
                }
                return;
            }

            // Group-header row: a tinted band with the variety label, visually distinct and not
            // highlighted as a selectable item.
            if (item != null && item.IsHeader)
            {
                using (var headerBg = new SolidBrush(Color.FromArgb(235, 240, 245)))
                using (var headerLine = new Pen(Color.FromArgb(200, 210, 220)))
                using (var headerText = new SolidBrush(Color.FromArgb(52, 73, 94)))
                using (var headerFont = new Font(e.Font, FontStyle.Bold))
                {
                    e.Graphics.FillRectangle(headerBg, e.Bounds);
                    e.Graphics.DrawLine(headerLine, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                    Rectangle hr = new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height);
                    e.Graphics.DrawString(item.GroupLabel ?? item.Name, headerFont, headerText, hr,
                        new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter });
                }
                return;
            }

            e.DrawBackground();

            Font mainFont = e.Font;
            Font subFont = new Font(e.Font.FontFamily, Math.Max(e.Font.Size - 1, 7), FontStyle.Regular);
            Brush textBrush;
            Brush subBrush;
            Brush customHighlightBrush = new SolidBrush(System.Drawing.Color.FromArgb(52, 152, 219)); // Same blue
            Brush borderBrush = new SolidBrush(Color.FromArgb(220, 220, 220));
            Pen borderPen = new Pen(borderBrush);

            if (e.State.HasFlag(DrawItemState.Selected))
            {
                e.Graphics.FillRectangle(customHighlightBrush, e.Bounds);
                textBrush = new SolidBrush(Color.White);
                subBrush = new SolidBrush(Color.WhiteSmoke);
            }
            else
            {
                textBrush = new SolidBrush(e.ForeColor);
                subBrush = new SolidBrush(Color.FromArgb(80, 80, 80));
            }

            if (item != null && !string.IsNullOrEmpty(item.ProductName))
            {
                // Tabular Columns Layout - Improved widths for better visibility
                int col1Width = 180; // Product Name (increased from 140)
                int col2Width = 140; // Variety (increased from 120)
                int col3Width = 70;  // Grade (increased from 60)
                int col4Width = 70;  // Count (increased from 60)
                int col5Width = 100; // Carton (increased from 90)
                int col6Width = 80;  // Weight (increased from 70)

                int x = e.Bounds.X + 8; // Increased left padding from 4 to 8

                // Col 1: Product Name
                Rectangle rect1 = new Rectangle(x, e.Bounds.Y, col1Width, e.Bounds.Height);
                e.Graphics.DrawString(item.ProductName ?? "-", mainFont, textBrush, rect1, new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter });
                e.Graphics.DrawLine(borderPen, x + col1Width, e.Bounds.Y, x + col1Width, e.Bounds.Bottom);
                x += col1Width + 8; // Increased spacing from 6 to 8

                // Col 2: Variety
                Rectangle rect2 = new Rectangle(x, e.Bounds.Y, col2Width, e.Bounds.Height);
                e.Graphics.DrawString(item.Variety ?? "-", subFont, subBrush, rect2, new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter });
                e.Graphics.DrawLine(borderPen, x + col2Width, e.Bounds.Y, x + col2Width, e.Bounds.Bottom);
                x += col2Width + 8; // Increased spacing from 6 to 8

                // Col 3: Grade
                Rectangle rect3 = new Rectangle(x, e.Bounds.Y, col3Width, e.Bounds.Height);
                e.Graphics.DrawString(item.Grade ?? "-", subFont, subBrush, rect3, new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter });
                e.Graphics.DrawLine(borderPen, x + col3Width, e.Bounds.Y, x + col3Width, e.Bounds.Bottom);
                x += col3Width + 8; // Increased spacing from 6 to 8

                // Col 4: Count
                Rectangle rect4 = new Rectangle(x, e.Bounds.Y, col4Width, e.Bounds.Height);
                e.Graphics.DrawString(item.Count ?? "-", subFont, subBrush, rect4, new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter });
                e.Graphics.DrawLine(borderPen, x + col4Width, e.Bounds.Y, x + col4Width, e.Bounds.Bottom);
                x += col4Width + 8; // Increased spacing from 6 to 8
                
                // Col 5: Carton
                Rectangle rect5 = new Rectangle(x, e.Bounds.Y, col5Width, e.Bounds.Height);
                e.Graphics.DrawString(item.Carton ?? "-", subFont, subBrush, rect5, new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter });
                e.Graphics.DrawLine(borderPen, x + col5Width, e.Bounds.Y, x + col5Width, e.Bounds.Bottom);
                x += col5Width + 8; // Increased spacing from 6 to 8

                // Col 6: Avg Weight
                Rectangle rect6 = new Rectangle(x, e.Bounds.Y, col6Width, e.Bounds.Height);
                string weightStr = !string.IsNullOrEmpty(item.AvgWeightKg) && item.AvgWeightKg != "0" ? $"{item.AvgWeightKg}kg" : "-";
                e.Graphics.DrawString(weightStr, subFont, subBrush, rect6, new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter });
            }
            else
            {
                // Fallback for simple/unstructured items
                string text = item?.Name ?? combo.GetItemText(combo.Items[e.Index]);
                e.Graphics.DrawString(text, e.Font, textBrush, e.Bounds, new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter });
            }

            // Cleanup resources - dispose all created brushes and objects
            textBrush.Dispose();
            subBrush.Dispose();
            customHighlightBrush.Dispose();
            borderBrush.Dispose();
            borderPen.Dispose();
            subFont.Dispose();
        }

        private void comboBox_CropID_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedValue = comboBox_CropID?.SelectedValue?.ToString() ?? "";
            string displayText = comboBox_CropID?.Text ?? "";
            _pendingPrinterCropSelection = selectedValue;
            _pendingPrinterProductSelection = null;
            UpdateProductOptionsForSelectedCrop();

            // Only update statusLabel if it's accessible (not in popup context)
            if (statusLabel != null && !statusLabel.IsDisposed && statusLabel.Parent != null)
            {
                string message = string.IsNullOrEmpty(displayText)
                    ? "Crop selection cleared."
                    : $"Crop updated: '{displayText}' ({selectedValue}) - Click Preview to see visual representation";
                statusLabel.Text = message;
                statusLabel.ForeColor = System.Drawing.Color.FromArgb(52, 152, 219);
            }
            SaveAdvancedSettings();

            // Update product details directly from selected product option
            UpdateProductDetailFieldsFromSelection();
        }

        private void PrintBarcodeWithAdvancedSettings(int printCount)
        {
            if (!string.IsNullOrWhiteSpace(textBox_EmployeeID.Text))
            {
                // Use custom Employee ID from advanced settings
                string customText = textBox_EmployeeID.Text;
                statusLabel.Text = $"Printing barcode with custom text: '{customText}'";
                statusLabel.ForeColor = System.Drawing.Color.FromArgb(52, 152, 219);
                
                // Apply advanced settings
                ApplyAdvancedSettings();
            }
            if (comboBox_ProductID.Enabled && comboBox_ProductID.SelectedValue != null)
            {
                // Use custom Product ID from advanced settings
                string customText = comboBox_ProductID.Text ?? GetSelectedProductCode();
                statusLabel.Text = $"Printing barcode with custom text: '{customText}'";
                statusLabel.ForeColor = System.Drawing.Color.FromArgb(52, 152, 219);
                
                // Apply advanced settings
                ApplyAdvancedSettings();
            }
            if (comboBox_CropID.SelectedValue != null)
            {
                string cropCode = GetSelectedCropCode();
                string cropDisplay = comboBox_CropID.Text ?? "";
                statusLabel.Text = $"Printing barcode with crop: '{cropDisplay}' ({cropCode})";
                statusLabel.ForeColor = System.Drawing.Color.FromArgb(52, 152, 219);
            }
        }

        private void button_generateBarcode_Click(object sender, EventArgs e)
        {
            try
            {
                string employeeSegment, productSegment, cropSegment;
                string upc11 = BuildUpc11Payload(out employeeSegment, out productSegment, out cropSegment);
                int checkDigit = GetUpcCheckDigit(upc11);
                _generatedBarcode = $"{upc11}{checkDigit}";

                // Mark as generated and enable start printing button
                _barcodeGenerated = true;
                button_send.Enabled = true;
                button_send.Text = "🖨️ Start Printing";
                button_send.BackColor = System.Drawing.Color.FromArgb(46, 125, 50); // Green color

                // Update status
                statusLabel.Text = $"Barcode generated successfully: {_generatedBarcode} (Emp:{employeeSegment} Prod:{productSegment} Crop:{cropSegment} ✓{checkDigit})";
                statusLabel.ForeColor = System.Drawing.Color.FromArgb(46, 204, 113);
            }
            catch (Exception ex)
            {
                ErrorDialog.ShowError("Generation Error", $"Barcode generation failed: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}", this);
                statusLabel.Text = "Barcode generation failed. Please check your settings.";
                statusLabel.ForeColor = System.Drawing.Color.FromArgb(231, 76, 60);
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
            string BarcodeID;
            if (_barcodeGenerated && !string.IsNullOrEmpty(_generatedBarcode))
            {
                BarcodeID = _generatedBarcode;
            }
            else
            {
                BarcodeID = BuildUpcBarcodeFromInputs();
            }

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
                $"📝 Generated Barcode: {BarcodeID} (fits in 1 line)";
                
            infoLabel.Text = $"Preview Settings:\n" +
                $"{textLayoutInfo}\n" +
                $"📏 Dimensions: {labelWidth} x {labelHeight} pixels\n" +
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
            
            int xPos = (int)(numericUpDown_xCoordinate?.Value ?? 0);
            
            // 1. Compact header: "Scan-Link" left + barcode number right (same line, smaller font)
            Label headerLabel = new Label();
            headerLabel.Text = "Scan-Link ||";
            headerLabel.Font = new Font("Courier New", 9, FontStyle.Bold);
            headerLabel.ForeColor = Color.Black;
            headerLabel.Location = new Point(xPos, 2);
            headerLabel.AutoSize = true;
            marginIndicator.Controls.Add(headerLabel);

            // Barcode number on the right side of the header line
            Label headerBarcodeLabel = new Label();
            headerBarcodeLabel.Text = BarcodeID;
            headerBarcodeLabel.Font = new Font("Courier New", 9, FontStyle.Bold);
            headerBarcodeLabel.ForeColor = Color.Black;
            headerBarcodeLabel.AutoSize = true;
            marginIndicator.Controls.Add(headerBarcodeLabel);
            // Place right after "Scan-Link ||" with larger gap (matches print positioning)
            headerBarcodeLabel.Location = new Point(xPos + headerLabel.PreferredWidth + 40, 2);
            
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
            // if (textLines.Length > 1)
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
            
            // 6. Simulated barcode visual
            Panel barcodeVisual = new Panel();
            
            // Calculate barcode width using same logic as printer
            int desiredWidth = (int)numericUpDown_width.Value;
            int estimatedBarsPerChar = 11; // Average bars per character for Code 128
            int estimatedTotalBars = BarcodeID.Length * estimatedBarsPerChar;
            int narrowBarWidth = Math.Max(1, Math.Min(10, desiredWidth / estimatedTotalBars));
            if (narrowBarWidth < 1) narrowBarWidth = 1;
            if (narrowBarWidth > 10) narrowBarWidth = 10;
            
            // Calculate actual barcode width based on narrow bar width
            int barcodeWidth = Math.Min(width - xPos - 10, estimatedTotalBars * narrowBarWidth + 6 * narrowBarWidth); // +6 for quiet zones
            barcodeWidth = Math.Max(barcodeWidth, 150); // ensure visible minimum for scannability

            // Match physical hardware printing absolute coordinates (Y=20, Height=110) - compact layout
            int actualBarcodeHeight = 110;
            barcodeVisual.Location = new Point(xPos, 20);
			barcodeVisual.Size = new Size(Math.Min(Math.Max(barcodeWidth, 220), marginIndicator.Width - xPos), actualBarcodeHeight);
            barcodeVisual.BackColor = Color.White;
            marginIndicator.Controls.Add(barcodeVisual);
            
            // Create barcode pattern with calculated dimensions
            barcodeVisual.Paint += (s, pe) => DrawBarcodePreview(pe.Graphics, new Rectangle(Point.Empty, barcodeVisual.Size), BarcodeID, comboBox_barcode.Text);

            // Barcode number is now shown in the header line (no longer below bars)

            // Detail lines below the barcode - mirror of the printed label (employee name; variety | count | grade)
            string previewEmpName = GetEmployeeNameForBarcode(BarcodeID);
            var previewCombo = GetSelectedProductCombination();
            string previewLine2 = string.Join("  |  ",
                new[] { previewCombo?.variety_name, previewCombo?.count_name, previewCombo?.grade_name }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
            // Mirror the print-side height guard: only show lines that fit the label height.
            const int previewDetailTextHeight = 14;
            if (barcodeVisual.Bottom + 4 + previewDetailTextHeight > height) previewEmpName = "";
            if (barcodeVisual.Bottom + 20 + previewDetailTextHeight > height) previewLine2 = "";

            if (!string.IsNullOrWhiteSpace(previewEmpName))
            {
                Label empDetailLabel = new Label();
                empDetailLabel.Text = previewEmpName;
                empDetailLabel.Font = new Font("Courier New", 8, FontStyle.Bold);
                empDetailLabel.ForeColor = Color.Black;
                empDetailLabel.Location = new Point(xPos, barcodeVisual.Bottom + 4);
                empDetailLabel.AutoSize = true;
                marginIndicator.Controls.Add(empDetailLabel);
            }
            if (!string.IsNullOrWhiteSpace(previewLine2))
            {
                Label countGradeLabel = new Label();
                countGradeLabel.Text = previewLine2;
                countGradeLabel.Font = new Font("Courier New", 8, FontStyle.Bold);
                countGradeLabel.ForeColor = Color.Black;
                countGradeLabel.Location = new Point(xPos, barcodeVisual.Bottom + 20);
                countGradeLabel.AutoSize = true;
                marginIndicator.Controls.Add(countGradeLabel);
            }
            
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

            // 9. EmployeeIDLabel - Removed per user request
            // Label EmployeeIDLabel = new Label();
            // string fullEmployeeID = !string.IsNullOrWhiteSpace(textBox_EmployeeID.Text) ? textBox_EmployeeID.Text : "";
            // string EmployeeID = fullEmployeeID.Length > 6 ? fullEmployeeID.Substring(fullEmployeeID.Length - 6) : fullEmployeeID.PadLeft(6, '0');
            // EmployeeIDLabel.Text = $"EmployeeID: {EmployeeID}";
            // EmployeeIDLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            // EmployeeIDLabel.ForeColor = Color.FromArgb(52, 73, 94);
            // EmployeeIDLabel.Location = new Point(0, barcodeVisual.Bottom + 5); // Position after barcode
            // EmployeeIDLabel.AutoSize = true;
            // marginIndicator.Controls.Add(EmployeeIDLabel);

            // 10. ProductIDLabel - Removed per user request
            // Label ProductIDLabel = new Label();
            // string cropId = GetSelectedCropCode();
            // string productId = GetSelectedProductCode();
            // ProductIDLabel.Text = $"CropId: {cropId}   ProductId: {productId}";
            // ProductIDLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            // ProductIDLabel.ForeColor = Color.FromArgb(52, 73, 94);
            // ProductIDLabel.Location = new Point(0, EmployeeIDLabel.Bottom + 5); // Position after Employee ID
            // ProductIDLabel.AutoSize = true;
            // marginIndicator.Controls.Add(ProductIDLabel);
            
            // // 9. Add label dimension info
            // Label dimensionsLabel = new Label();
            // dimensionsLabel.Text = $"Label Size: {width} x {height} pixels | Gap: {numericUpDown_gap.Value}mm";
            // dimensionsLabel.Font = new Font("Segoe UI", 8, FontStyle.Italic);
            // dimensionsLabel.ForeColor = Color.FromArgb(127, 140, 141);
            // dimensionsLabel.Location = new Point(5, labelOutline.Height - 15);
            // dimensionsLabel.AutoSize = true;
            // labelOutline.Controls.Add(dimensionsLabel);
            
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
            // Check if barcode has been generated
            if (!_barcodeGenerated || string.IsNullOrEmpty(_generatedBarcode))
            {
                statusLabel.Text = "❌ Please generate a barcode first before printing.";
                statusLabel.ForeColor = System.Drawing.Color.FromArgb(231, 76, 60);
                return;
            }

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

                // Apply advanced settings (always enabled)
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

                // Success feedback with generated barcode summary
                string successMessage = $"✅ Print job completed successfully!\n🎯 Barcode used: {_generatedBarcode}";
                statusLabel.Text = successMessage;
                statusLabel.ForeColor = System.Drawing.Color.FromArgb(46, 204, 113);

                // Reset barcode generation state after successful print
                _barcodeGenerated = false;
                _generatedBarcode = "";
                button_send.Text = "🖨️ Generate Barcode";
                button_send.BackColor = System.Drawing.Color.FromArgb(100, 100, 100); // Gray out permanently
            }
            catch (Exception ex)
            {
                // Error feedback
                statusLabel.Text = $"❌ Print failed: {ex.Message}";
                statusLabel.ForeColor = System.Drawing.Color.FromArgb(231, 76, 60);
                // Re-enable button on failure so user can try again
                button_send.Enabled = true;
                button_send.Text = "🖨️ Start Printing";
                button_send.BackColor = System.Drawing.Color.FromArgb(46, 125, 50);
            }
            finally
            {
                progressBar.Style = ProgressBarStyle.Continuous; // remain visible but not animating
            }
        }

        private bool __createPrn(string additionalname, int index)
        {
            IPrinterConnection fs = null;
            try
            {
                switch (CurrentConnectionType)
                {
                    case "File":
                        fs = new FileStreamConnection(strFolder + "\\" + additionalname);
                        break;
                    case "COM":
                        fs = new SerialConnection(m_ComName, m_baudRate, m_parity, m_dataBits, m_stopBits, m_handshake);
                        break;
                    case "USB":
                        if (string.IsNullOrWhiteSpace(m_USBDevicePath))
                        {
                            throw new Exception("No USB device selected. Please click 'Printer Setup' or 'Configure' and select your printer from the list first.");
                        }
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
                    // Try Label_With_Gap media type for better gap detection
                    // PPLBEmulation.SetUtil.SetHardwareOption(PPLBMediaType.Label_With_Gap, PPLBPrintMode.Tear_Off, 0);
                    PPLBEmulation.SetUtil.SetHardwareOption(PPLBMediaType.Direct_Thermal_Media, PPLBPrintMode.Tear_Off, 0);
                    
                    // Apply advanced settings (always enabled)
                    {
                        // Apply darkness setting (convert from 1-30 to 0-30 range)
                        PPLBEmulation.SetUtil.SetDarkness(trackBar_darkness.Value - 1);
                        
                        // Apply print speed (convert from 1-9 to actual speed)
                        int speedValue = comboBox_speed.SelectedIndex + 1;
                        PPLBEmulation.SetUtil.SetPrintRate(speedValue);
                        
                        // Apply label dimensions (controls entire print area)
                        int labelWidthDots = (int)numericUpDown_width.Value;
                        int labelHeightDots = (int)numericUpDown_height.Value;
                        decimal gapMM = numericUpDown_gap.Value;

                        // Convert gap from millimeters to dots using DPI
                        decimal dpi = numericUpDown_dpi?.Value ?? 203;
                        int gapDots = (int)Math.Round(gapMM * dpi / 25.4m);

                        // For automatic gap detection, some printers support gapDots = 0 for auto-detection
                        // Uncomment the line below to try automatic gap detection:
                        // gapDots = 0; // Enable automatic gap detection

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
                        // For gap detection, allow smaller values but warn if too small
                        if (gapDots < 16)
                        {
                            corrections += $"Gap corrected from {gapDots} to 16 pixels (recommended minimum for gap detection). ";
                            gapDots = 16;
                        }
                        else if (gapDots > 600)
                        {
                            corrections += $"Gap corrected from {gapDots} to 600 pixels (maximum allowed). ";
                            gapDots = 600;
                        }

                        // Show corrections to user if any were made
                        if (!string.IsNullOrEmpty(corrections))
                        {
                            statusLabel.Text = $"⚠️ Parameter corrections: {corrections}";
                            statusLabel.ForeColor = System.Drawing.Color.FromArgb(255, 193, 7); // Warning color
                        }

                        // Set the actual label dimensions
                        // Note: If gap detection fails, try Continuous mode instead:
                        // PPLBEmulation.SetUtil.SetLabelLength(PPLBMediaTrack.Continuous_Media_Mode, labelHeightDots, gapDots);
                        PPLBEmulation.SetUtil.SetLabelLength(PPLBMediaTrack.Gap_Mode, labelHeightDots, gapDots);
                        PPLBEmulation.SetUtil.SetPrintWidth(labelWidthDots);
                        PPLBEmulation.SetUtil.SetHomePosition(5, 5); // 5-dot margin
                    }
                    
                    PPLBEmulation.SetUtil.SetClearImageBuffer();
                    buf = encoder.GetBytes("Label: one barcode");
                    PPLBEmulation.TextUtil.PrintText(30, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                    buf = encoder.GetBytes(comboBox_barcode.Text);
                    PPLBEmulation.TextUtil.PrintText(50, 50, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                    
                    // Use the generated barcode if available, otherwise fall back to old logic
                    string BarcodeID;
                    if (_barcodeGenerated && !string.IsNullOrEmpty(_generatedBarcode))
                    {
                        BarcodeID = _generatedBarcode;
                    }
                    else
                    {
                        BarcodeID = BuildUpcBarcodeFromInputs();
                    }

                    buf = encoder.GetBytes(BarcodeID);
                    
                    // Calculate text layout based on width constraints
                    int labelWidth = (int)numericUpDown_width.Value;
                    var (textLines, textFont, textSize) = CalculateTextLayout(BarcodeID, labelWidth);
                    
                    // Print text information with width-constrained layout
                    if (textLines.Length > 1)
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
                    int xPos = 50, yPos = 110, barcodeHeight = 110;
                    PPLBOrient orientation = PPLBOrient.Clockwise_0_Degrees;
                    
                    // Advanced settings always enabled
                    {
                        // Set default centered position
                        xPos = 150; // Center
                        
                        // Default rotation (0°)
                        orientation = PPLBOrient.Clockwise_0_Degrees;
                        
                        // Apply height setting
                        barcodeHeight = (int)numericUpDown_height.Value;
                    }
                    
                    // Calculate narrow bar width based on desired total barcode width
                    // Formula: narrowBarWidth = desiredWidth / (estimatedBarsCount * averageBarRatio)
                    int desiredWidth = (int)numericUpDown_width.Value;
                    int estimatedBarsPerChar = 11; // Average bars per character for Code 128
                    int estimatedTotalBars = BarcodeID.Length * estimatedBarsPerChar;
                    int narrowBarWidth = Math.Max(1, Math.Min(10, desiredWidth / estimatedTotalBars));
                    if (narrowBarWidth < 1) narrowBarWidth = 1;
                    if (narrowBarWidth > 10) narrowBarWidth = 10; // ARGOX SDK typically supports 1-10
                    
                    // Update status with calculated width information
                    // Advanced settings always enabled
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
                            int qrSize = Math.Min((int)numericUpDown_width.Value / 50, 10);
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

        // Returns the captured employee name ONLY when the supplied barcode's employee segment
        // matches the employee the name was captured for. Barcode layout is
        // guard(1) + employee(4) + product(3) + crop(3) + check(1), so the employee digits are
        // [1..5). Keying off the actual barcode (not the editable textbox) means a stale cached
        // barcode, a hand-edited ID, or a non-normalized ID can never print a name that disagrees
        // with the bars actually being produced.
        private string GetEmployeeNameForBarcode(string barcodeId)
        {
            if (string.IsNullOrWhiteSpace(_selectedEmployeeName) || string.IsNullOrWhiteSpace(_selectedEmployeeBarcodeId))
                return "";
            if (string.IsNullOrEmpty(barcodeId) || barcodeId.Length < 5) return "";
            string employeeSegment = barcodeId.Substring(1, 4);
            return string.Equals(employeeSegment, _selectedEmployeeBarcodeId, StringComparison.Ordinal)
                ? _selectedEmployeeName
                : "";
        }

        // Custom preset method that fully utilizes advanced settings
        // Print small text with a simulated bold effect (double-strike at x and x+1). Keeps the
        // added detail lines small while staying legible on thermal labels. No-op for empty text.
        // When maxWidthDots > 0 the text is truncated to fit, so a long value can't overrun the
        // label edge or (in two-up) the neighbouring column.
        private void PrintBoldLabelText(int x, int y, PPLBFont font, string text, int maxWidthDots = 0)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            if (maxWidthDots > 0)
            {
                // Conservative Font_1 glyph width on a 203-dpi head; over-estimating means we
                // truncate slightly early rather than risk an overrun.
                const int approxCharWidthDots = 10;
                int maxChars = Math.Max(1, maxWidthDots / approxCharWidthDots);
                if (text.Length > maxChars) text = text.Substring(0, maxChars);
            }
            byte[] b = Encoding.Default.GetBytes(text);
            PPLBEmulation.TextUtil.PrintText(x, y, PPLBOrient.Clockwise_0_Degrees, font, 1, 1, false, b);
            PPLBEmulation.TextUtil.PrintText(x + 1, y, PPLBOrient.Clockwise_0_Degrees, font, 1, 1, false, b);
        }

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
                // Note: If gap detection issues occur, try changing to Continuous mode or different media type
                // PPLBEmulation.SetUtil.SetHardwareOption(PPLBMediaType.Label_With_Gap, PPLBPrintMode.Tear_Off, 0);
                PPLBEmulation.SetUtil.SetHardwareOption(PPLBMediaType.Direct_Thermal_Media, PPLBPrintMode.Tear_Off, 0);
                
                // Apply all advanced settings including label dimensions
                PPLBEmulation.SetUtil.SetDarkness(trackBar_darkness.Value - 1);
                int speedValue = Math.Max(1, comboBox_speed.SelectedIndex + 1);
                PPLBEmulation.SetUtil.SetPrintRate(speedValue);
                
                // Set label dimensions (controls entire print area)
                int labelWidthDots = (int)numericUpDown_width.Value;
                int labelHeightDots = (int)numericUpDown_height.Value;
                decimal gapMM = numericUpDown_gap.Value;

                // Convert gap from millimeters to dots using DPI
                decimal dpi = numericUpDown_dpi?.Value ?? 203;
                int gapDots = (int)Math.Round(gapMM * dpi / 25.4m);

                // For automatic gap detection, some printers support gapDots = 0 for auto-detection
                // Uncomment the line below to try automatic gap detection:
                // gapDots = 0; // Enable automatic gap detection

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
                // For gap detection, allow smaller values but warn if too small
                if (gapDots < 16)
                {
                    corrections += $"Gap corrected from {gapDots} to 16 pixels (recommended minimum for gap detection). ";
                    gapDots = 16;
                }
                else if (gapDots > 600)
                {
                    corrections += $"Gap corrected from {gapDots} to 600 pixels (maximum allowed). ";
                    gapDots = 600;
                }

                // Show corrections to user if any were made
                if (!string.IsNullOrEmpty(corrections))
                {
                    statusLabel.Text = $"⚠️ Parameter corrections: {corrections}";
                    statusLabel.ForeColor = System.Drawing.Color.FromArgb(255, 193, 7); // Warning color
                }

                // Note: If gap detection fails, try Continuous mode instead:
                // PPLBEmulation.SetUtil.SetLabelLength(PPLBMediaTrack.Continuous_Media_Mode, labelHeightDots, gapDots);
                PPLBEmulation.SetUtil.SetLabelLength(PPLBMediaTrack.Gap_Mode, labelHeightDots, gapDots);
                
                PPLBEmulation.SetUtil.SetPrintWidth(labelWidthDots);
                PPLBEmulation.SetUtil.SetHomePosition(5, 5); // 5-dot margin
                
                PPLBEmulation.SetUtil.SetClearImageBuffer();

                
                int stickerWidth = (int)numericUpDown_width.Value;
                int xCoordinate = (int)(numericUpDown_xCoordinate?.Value ?? 0);
                int x2Coordinate = (int)(numericUpDown_x2Coordinate?.Value ?? 0);
                
                // Compact header: "Scan-Link ||" on the left, barcode number on the right (same line, smaller font)
                buf = encoder.GetBytes("Scan-Link ||");
                PPLBEmulation.TextUtil.PrintText(xCoordinate, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);

                //If roll applied on x=0
                // buf = encoder.GetBytes("|| Scan-Link ||");
                // PPLBEmulation.TextUtil.PrintText(0, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_4, 1, 1, false, buf);


                // buf = encoder.GetBytes($"Custom Preset - {DateTime.Now:HH:mm:ss}");
                // buf = encoder.GetBytes($"{DateTime.Now:HH:mm:ss}");
                // PPLBEmulation.TextUtil.PrintText(540, 35, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_1, 1, 1, false, buf);
                
                // buf = encoder.GetBytes($"Settings: D{trackBar_darkness.Value} S{speedValue}");
                // PPLBEmulation.TextUtil.PrintText(30, 25, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_1, 1, 1, false, buf);
                
                // Use custom Employee ID (exactly 6 characters, left-padded with zeros)
                string fullEmployeeID = !string.IsNullOrWhiteSpace(textBox_EmployeeID.Text) ? textBox_EmployeeID.Text : "";
                string EmployeeID = fullEmployeeID;

                string CropID = GetSelectedCropCode();
                string ProductID = GetSelectedProductCode();

                // Use the generated barcode if available, otherwise fall back to new format logic
                string BarcodeID;
                if (_barcodeGenerated && !string.IsNullOrEmpty(_generatedBarcode))
                {
                    BarcodeID = _generatedBarcode;
                }
                else
                {
                    BarcodeID = BuildUpcBarcodeFromInputs();
                }

                buf = encoder.GetBytes(BarcodeID);
                var bufBarcode = buf;

                // Print barcode number right after "Scan-Link ||" text with much more spacing
                byte[] bufBarcodeText = encoder.GetBytes(BarcodeID);
                PPLBEmulation.TextUtil.PrintText(xCoordinate + 150, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, bufBarcodeText);
                
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
                
                // Default rotation (0°)
                int yPos = 80 + (textLines.Length * 12) + 10;

                PPLBOrient orientation = PPLBOrient.Clockwise_0_Degrees;
                
                // int barcodeHeight = (int)numericUpDown_height.Value;
                int barcodeHeight = 110;
                
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
                // Force Code 128 subset C. The payload is always exactly 12 numeric digits
                // (Guard1+Emp4+Prod3+Crop3+Check1), so subset C is the one correct, deterministic
                // encoding (6 digit-pairs). Auto Mode let the printer firmware pick A/B/C per value
                // and mis-encoded certain digit patterns, producing bars that decoded to <12 digits
                // (label showed 12, scan reported "< 11"). Subset C removes that auto-switch step.
                // Also non-GS1, so no FNC1 / ]C1 prefix in scans.
                barcodeType = PPLBBarCodeType.Code_128_Mode_C;
                
                int stickerHeight = (int)numericUpDown_height.Value;

                // Build the small bold detail lines printed below the barcode (values only, no keys):
                //   line 1 = employee name; line 2 = variety | count | grade.
                // Name is keyed off the actual barcode's employee segment, never the editable textbox.
                string detailEmpName = GetEmployeeNameForBarcode(BarcodeID);
                var detailCombo = GetSelectedProductCombination();
                string detailLine2 = string.Join("  |  ",
                    new[] { detailCombo?.variety_name, detailCombo?.count_name, detailCombo?.grade_name }
                        .Where(s => !string.IsNullOrWhiteSpace(s)));
                // Below the bars (bars are Y=20..130); does not affect bar height/width.
                const int detailLine1Y = 134;
                const int detailLine2Y = 150;
                const int detailTextHeight = 14;
                // Drop any line that would fall outside the configured label height.
                int detailLabelHeight = (int)numericUpDown_height.Value;
                if (detailLine1Y + detailTextHeight > detailLabelHeight) detailEmpName = "";
                if (detailLine2Y + detailTextHeight > detailLabelHeight) detailLine2 = "";

                bool twoUp = checkBox_twoUp != null && checkBox_twoUp.Checked;
                int remaining = Math.Max(1, printcount);
                List<string> warnings = new List<string>();

                if (!twoUp)
                {
                    // Print barcode below compact header line (Y=20 since header is now single small line)
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(xCoordinate, 20, orientation, barcodeType, narrowBarWidth, 0, barcodeHeight, false, bufBarcode);
                    // Small bold detail lines below the barcode (employee name; count | grade)
                    PrintBoldLabelText(xCoordinate, detailLine1Y, PPLBFont.Font_1, detailEmpName, Math.Max(1, labelWidthDots - xCoordinate));
                    PrintBoldLabelText(xCoordinate, detailLine2Y, PPLBFont.Font_1, detailLine2, Math.Max(1, labelWidthDots - xCoordinate));
                    // EmployeeID and CropID/ProductID text removed per user request
                    PPLBEmulation.SetUtil.SetPrintOut(remaining, 1);
                    PPLBEmulation.IOUtil.PrintOut();
                }
                else
                {
                    // ---- Two-up geometry fix -------------------------------------------------
                    // Bug: SetPrintWidth(labelWidthDots) earlier sized the image buffer to ONE
                    // label width, and narrowBarWidth was sized to the full label width. In two-up
                    // the right barcode is drawn at x2Coordinate (~one label width across), and a
                    // full 12-digit Code 128 is ~0.77x the label width, so the right barcode's stop
                    // pattern + trailing digit-pairs fell OUTSIDE the buffer and were clipped: the
                    // label showed bars but the scan decoded a short/invalid value ("Scanned value
                    // must be at least 11 digits"). The left and right barcodes are otherwise
                    // byte-identical (same data, type, bar width, height), so the failure was purely
                    // positional — which is why it always hit the right column. Verified by a
                    // render+decode simulation: right column NO-READ before, both columns read after.
                    // Fix: size the bars so a full barcode (101 modules) + a 10-module quiet zone fit
                    // inside the column pitch (x2 - x), and widen the print buffer to cover the right
                    // column's bars + quiet zone. This self-corrects whether the operator set Width as
                    // a single-column width or as the full 2-up media width.
                    const int BARCODE_MODULES = 101; // 12-digit subset-C: start + 6 pairs + checksum + stop
                    const int QUIET_MODULES = 10;    // Code 128 minimum quiet zone (each side)
                    int columnPitch = x2Coordinate - xCoordinate;
                    int twoUpNarrow = narrowBarWidth;
                    if (columnPitch > 0)
                        twoUpNarrow = Math.Max(1, Math.Min(narrowBarWidth, columnPitch / (BARCODE_MODULES + QUIET_MODULES)));
                    int twoUpBarDots = BARCODE_MODULES * twoUpNarrow;
                    int twoUpQuietDots = QUIET_MODULES * twoUpNarrow;
                    int twoUpPrintWidth = Math.Max(labelWidthDots, x2Coordinate + twoUpBarDots + twoUpQuietDots + 5);
                    narrowBarWidth = twoUpNarrow; // both columns draw with this so they stay identical
                    PPLBEmulation.SetUtil.SetPrintWidth(twoUpPrintWidth);

                    // Fit warnings, checked against the real two-up geometry
                    if (xCoordinate < 0) warnings.Add($"Left X ({xCoordinate}) out of bounds");
                    if (x2Coordinate <= xCoordinate) warnings.Add($"Right X ({x2Coordinate}) must be greater than Left X ({xCoordinate})");
                    if (columnPitch > 0 && twoUpBarDots + twoUpQuietDots > columnPitch)
                        warnings.Add($"Right barcode ({twoUpBarDots}+{twoUpQuietDots} dots) wider than column gap ({columnPitch}); increase X2 spacing or Width");
                    if (twoUpNarrow < 2)
                        warnings.Add($"Two-up bars are 1 dot wide (pitch {columnPitch}); may not scan — increase Width or X2 spacing");

                    while (remaining > 0)
                    {
                        PPLBEmulation.SetUtil.SetClearImageBuffer();

                        // left - compact header: "Scan-Link ||" left + barcode number right, same line
                        buf = encoder.GetBytes("Scan-Link ||");
                        PPLBEmulation.TextUtil.PrintText(xCoordinate, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                        PPLBEmulation.TextUtil.PrintText(xCoordinate + 150, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, bufBarcodeText);
                        PPLBEmulation.BarcodeUtil.PrintOneDBarcode(xCoordinate, 20, orientation, barcodeType, narrowBarWidth, 0, barcodeHeight, false, bufBarcode);
                        PrintBoldLabelText(xCoordinate, detailLine1Y, PPLBFont.Font_1, detailEmpName, Math.Max(1, (x2Coordinate > xCoordinate ? x2Coordinate - xCoordinate - 6 : labelWidthDots - xCoordinate)));
                        PrintBoldLabelText(xCoordinate, detailLine2Y, PPLBFont.Font_1, detailLine2, Math.Max(1, (x2Coordinate > xCoordinate ? x2Coordinate - xCoordinate - 6 : labelWidthDots - xCoordinate)));

                        // right (if any remaining)
                        if (remaining > 1)
                        {
                            buf = encoder.GetBytes("Scan-Link ||");
                            PPLBEmulation.TextUtil.PrintText(x2Coordinate, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                            PPLBEmulation.TextUtil.PrintText(x2Coordinate + 150, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, bufBarcodeText);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(x2Coordinate, 20, orientation, barcodeType, narrowBarWidth, 0, barcodeHeight, false, bufBarcode);
                            // Right column gets the same width budget as the left column (the column
                            // pitch). Using labelWidthDots - x2Coordinate would collapse to ~0 here
                            // because x2Coordinate is already ~one label width, truncating text to 1 char.
                            int rightColMaxWidth = x2Coordinate > xCoordinate ? x2Coordinate - xCoordinate - 6 : labelWidthDots - x2Coordinate;
                            PrintBoldLabelText(x2Coordinate, detailLine1Y, PPLBFont.Font_1, detailEmpName, Math.Max(1, rightColMaxWidth));
                            PrintBoldLabelText(x2Coordinate, detailLine2Y, PPLBFont.Font_1, detailLine2, Math.Max(1, rightColMaxWidth));
                        }

                        PPLBEmulation.SetUtil.SetPrintOut(1, 1);
                        PPLBEmulation.IOUtil.PrintOut();
                        remaining -= Math.Min(2, remaining);
                    }
                }
                
                if (warnings.Count > 0)
                {
                    statusLabel.Text = "⚠️ " + string.Join("; ", warnings);
                    statusLabel.ForeColor = System.Drawing.Color.FromArgb(255, 193, 7);
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

        // removed: start panel printer button handler

        // removed: start panel scanner button handler

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
                // Keep the full-history metrics index current before we recompute the stat labels.
                // Cheap: only the bytes appended to scans_backup.csv since last load are parsed.
                RefreshMetricsIndex();

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
                JavaScriptSerializer serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
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
                allData.Columns.Add("BlockNumber", typeof(string));
                allData.Columns.Add("LineNumber", typeof(string));
                allData.Columns.Add("Supplier", typeof(string));
                allData.Columns.Add("CropID", typeof(string));
                allData.Columns.Add("ProductID", typeof(string));
                allData.Columns.Add("ParsedInfo", typeof(string));

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
                    string supplier = "";
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
                        if (device.ContainsKey("supplier"))
                            supplier = device["supplier"]?.ToString() ?? "";
                    }

                    // Also check top-level supplier field
                    if (string.IsNullOrEmpty(supplier) && scan.ContainsKey("supplier"))
                        supplier = scan["supplier"]?.ToString() ?? "";

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
                        // Barcode layout: Guard(1) + Employee(4) + Product(3) + Crop(3) + Check(1) = 12
                        const int encodedDataLength = 12;
                        string digitsOnly = new string(code.Where(char.IsDigit).ToArray());

                        // Guard digit recovery: if scanner dropped the first digit,
                        // we receive 11 digits instead of 12. Prepend the known guard '1'.
                        if (digitsOnly.Length == 11)
                        {
                            digitsOnly = "1" + digitsOnly;
                        }

                        if (!string.IsNullOrEmpty(digitsOnly) && digitsOnly.Length >= encodedDataLength)
                        {
                            string bestUpc = null;

                            // Try all possible 12-digit substrings to find the valid one
                            // This reliably ignores any scanner-prepended EAN zeros or appended check digits
                            for (int i = 0; i <= digitsOnly.Length - encodedDataLength; i++)
                            {
                                string candidate = digitsOnly.Substring(i, encodedDataLength);
                                if (candidate.StartsWith("1"))
                                {
                                    try 
                                    {
                                        string upc11 = candidate.Substring(0, 11);
                                        int expectedCheckDigit = GetUpcCheckDigit(upc11);
                                        int actualCheckDigit = candidate[11] - '0';
                                        
                                        if (expectedCheckDigit == actualCheckDigit)
                                        {
                                            bestUpc = candidate;
                                            break; // Found the valid sequence!
                                        }
                                    } 
                                    catch { } // Ignore if check digit calc fails
                                }
                            }

                            // Fallback if no valid checksum found
                            if (bestUpc == null)
                            {
                                string tempDigits = digitsOnly;
                                while (tempDigits.Length > encodedDataLength && tempDigits.StartsWith("0"))
                                {
                                    tempDigits = tempDigits.Substring(1);
                                }
                                bestUpc = tempDigits.Substring(0, encodedDataLength);
                            }

                            if (string.IsNullOrEmpty(employeeId))
                            {
                                employeeId = bestUpc.Substring(1, 4); // skip guard digit
                            }
                            if (string.IsNullOrEmpty(productId))
                            {
                                productId = bestUpc.Substring(5, 3);
                            }
                            if (string.IsNullOrEmpty(cropId))
                            {
                                cropId = bestUpc.Substring(8, 3);
                            }

                        }
                    }
                    if (scan.ContainsKey("date"))
                        date = scan["date"]?.ToString() ?? "";
                    if (scan.ContainsKey("time"))
                        time = scan["time"]?.ToString() ?? "";

                    allData.Rows.Add(date, time, serial, blockId, lineId, supplier, cropId, productId, employeeId);
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
                        
                        // Fire-and-forget an immediate API upload round so logs 
                        // push up to the cloud instantly when a scan happens
                        _ = _scanLogUploadService?.TryUploadLogsAsync();
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

        private void button_cleanupScans_Click(object sender, EventArgs e)
        {
            try
            {
                // SAFETY GATE: never wipe the local view while scans are still waiting to upload.
                // scans.txt is a disposable display buffer, but if the upload queue isn't empty those
                // scans exist only locally, and clearing now could lose them (the exact failure mode
                // we just hardened against). GetPendingCount() reads the queue under the cross-process
                // lock; it returns -1 if it could not verify (treated as "not safe").
                int pending = _scanLogUploadService?.GetPendingCount() ?? -1;
                if (pending != 0)
                {
                    string msg = pending < 0
                        ? "Couldn't verify the upload queue right now. Please click \"Sync logs to API\", wait until it reports no remaining logs, then try again."
                        : $"You still have {pending} scan(s) that have NOT been synced to the database.\n\n" +
                          "Click \"Sync logs to API\" first and wait until it reports no remaining logs, then try again.";
                    MessageBox.Show(msg, "Sync required before cleanup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    statusLabel.Text = pending < 0 ? "Cleanup blocked: queue not verified" : $"Cleanup blocked: {pending} unsynced scan(s)";
                    statusLabel.ForeColor = Color.OrangeRed;
                    return;
                }

                var confirm = MessageBox.Show(
                    "Do you really want to clean up your local scans?\n\n" +
                    "Make sure you have synced them to the database. This clears the on-screen recent-scans list only " +
                    "— your full backup (scans_backup.csv) is NOT touched.",
                    "Confirm cleanup of local scans",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (confirm != DialogResult.Yes) return;

                int cleared = ClearLocalScansDisplay();

                // Refresh the in-memory grids and reload from the now-empty file.
                allScannerData?.Clear();
                filteredScannerData?.Clear();
                currentPageData?.Clear();
                currentPage = 1;
                totalPages = 1;
                LoadScansData();

                statusLabel.Text = cleared > 0
                    ? $"Local scans display cleared ({cleared} record(s) removed)."
                    : "Local scans display cleared.";
                statusLabel.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Cleanup failed: {ex.Message}";
                statusLabel.ForeColor = Color.Red;
            }
        }

        // Resets the local display buffer (scans.txt) to an empty array. This is the desktop
        // recent-activity view only; full history lives in scans_backup.csv and the database, so
        // clearing it loses nothing the system depends on. Written atomically (temp file + atomic
        // replace) so a concurrent scanner write can never observe a half-written file.
        private int ClearLocalScansDisplay()
        {
            string programDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ScanLink");
            string scansFile = Path.Combine(programDataDir, "scans.txt");
            int cleared = 0;

            try
            {
                if (File.Exists(scansFile))
                {
                    try
                    {
                        string content = File.ReadAllText(scansFile);
                        if (!string.IsNullOrWhiteSpace(content) && content.Trim() != "[]")
                        {
                            var list = new System.Web.Script.Serialization.JavaScriptSerializer { MaxJsonLength = int.MaxValue }
                                .Deserialize<List<object>>(content);
                            cleared = list?.Count ?? 0;
                        }
                    }
                    catch { /* corrupt/unparseable: count unknown, still safe to reset */ }
                }

                try { Directory.CreateDirectory(programDataDir); } catch { }
                string tmp = Path.Combine(programDataDir, "scans.txt." + Guid.NewGuid().ToString("N") + ".tmp");
                File.WriteAllText(tmp, "[]");
                if (File.Exists(scansFile)) File.Replace(tmp, scansFile, null);
                else File.Move(tmp, scansFile);
            }
            catch
            {
                try { File.WriteAllText(scansFile, "[]"); } catch { }
            }

            return cleared;
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

            // Clear product combinations cache
            if (_productCombinationsService != null)
            {
                _productCombinationsService.ClearCache();
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
            ResetProductComboForNoSelection();
            
            // Hide all panels and show login panel
            // printerContentPanel.Visible = false;
            scannerContentPanel.Visible = false;
            loginPanel.Visible = true;

            // Reset loading UI
            loadingProgressBar.Visible = false;
            loadingStatusLabel.Visible = false;
            loginStatusLabel.Visible = true;

            // Reset login status
            loginStatusLabel.Text = "Please Login to access your account";
            loginStatusLabel.ForeColor = Color.Gray;
            
            // Reset login button
            loginButton.Enabled = true;
            loginButton.Text = "Login";
        }

        private async void Form1_Shown(object sender, EventArgs e)
        {
            // Load saved advanced settings after the form is fully displayed
            // This ensures all controls are initialized and ready
            LoadAdvancedSettings();
            UpdateDimensionLabels();
            UpdatePictureBoxVisibility();

            // Automatically start the scan script after the form is fully displayed
            StartScanScript();
            LayoutRootPanels();

            // Try auto-login from saved token
            await TryAutoLoginAsync();
        }

        private async System.Threading.Tasks.Task TryAutoLoginAsync()
        {
            try
            {
                if (!_apiAuthService.LoadTokenFromFile())
                    return;

                if (!_apiAuthService.IsTokenValid())
                    return;

                System.Diagnostics.Debug.WriteLine("Auto-login: valid saved token found");

                // Show loading state
                loginPanel.Visible = true;
                loginButton.Enabled = false;
                loadingStatusLabel.Text = "Restoring session...";
                loadingStatusLabel.Visible = true;
                loadingProgressBar.Visible = true;

                // If site was saved, skip dashboard; otherwise show it
                string savedSiteId = _apiAuthService.GetSelectedSiteId();
                string savedSiteName = _apiAuthService.GetSelectedSiteName();

                if (string.IsNullOrEmpty(savedSiteId))
                {
                    // Need site selection — show dashboard
                    var tokenPayload = _apiAuthService.GetCurrentTokenPayload();
                    if (tokenPayload == null)
                    {
                        ResetToLogin("Session expired. Please login again.");
                        return;
                    }

                    using (var dashboard = new MultiUserDashboard(_apiAuthService, tokenPayload))
                    {
                        var result = dashboard.ShowDialog(this);
                        if (result == DialogResult.OK && !string.IsNullOrEmpty(dashboard.GetSelectedSiteId()))
                        {
                            _apiAuthService.SetSelectedSite(dashboard.GetSelectedSiteId(), dashboard.GetSelectedSiteName());
                        }
                        else
                        {
                            ResetToLogin("Site selection cancelled.");
                            return;
                        }
                    }
                }

                // Enrich queued logs and start uploader
                _scanLogUploadService?.EnrichLogsFileOnce();
                _scanLogUploadService?.Start();

                // Run scanner system initialization
                loadingStatusLabel.Text = "Initializing scanners...";
                bool initSuccess = await InitializeScannerSystemAsync();

                if (initSuccess)
                {
                    // Fetch product combinations
                    loadingStatusLabel.Text = "Loading product data...";
                    var combinationsResult = await _productCombinationsService.FetchAndCacheProductCombinationsAsync();
                    if (combinationsResult.Success)
                    {
                        _scannerProductItems = null;
                        _cropOptionsCache = null;
                        PopulateProductIdComboBox();
                        PopulateCropIdComboBox();
                    }

                    // Switch to scanner panel
                    loadingProgressBar.Visible = false;
                    loadingStatusLabel.Visible = false;
                    loginPanel.Visible = false;
                    scannerContentPanel.Visible = true;
                    LayoutRootPanels();
                    _ = LoadDailyStatsAsync();
                }
                else
                {
                    ResetToLogin("Failed to initialize scanner system. Please login again.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Auto-login failed: {ex.Message}");
                ResetToLogin("Session restore failed. Please login again.");
            }
        }

        private void ResetToLogin(string message)
        {
            loadingProgressBar.Visible = false;
            loadingStatusLabel.Visible = false;
            loginPanel.Visible = true;
            loginButton.Enabled = true;
            loginStatusLabel.Text = message;
            loginStatusLabel.ForeColor = Color.FromArgb(255, 152, 0);
            loginStatusLabel.Visible = true;
            _apiAuthService.ClearToken();
        }

        // ---- Non-blocking scanner stdin writer (#5) ----------------------------------------------
        // Starts the single background thread that owns all writes to the PowerShell StandardInput.
        // Idempotent: safe to call on every StartScanScript().
        private void StartScannerWriter()
        {
            if (_scanWriterRunning && _scanWriterThread != null && _scanWriterThread.IsAlive) return;
            if (_scanWriteQueue == null || _scanWriteQueue.IsAddingCompleted)
                _scanWriteQueue = new System.Collections.Concurrent.BlockingCollection<string>();
            _scanWriterRunning = true;
            _scanWriterThread = new System.Threading.Thread(ScannerWriterLoop)
            {
                IsBackground = true,
                Name = "ScannerStdinWriter"
            };
            _scanWriterThread.Start();
        }

        // Hand a scan to the background writer. Non-blocking: returns immediately, so the UI thread
        // is never stuck on a full pipe even when PowerShell is busy.
        private void EnqueueScannerLine(string formattedData)
        {
            if (string.IsNullOrEmpty(formattedData)) return;
            try
            {
                if (_scanWriteQueue == null || _scanWriteQueue.IsAddingCompleted) StartScannerWriter();
                _scanWriteQueue.Add(formattedData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SCAN] Failed to enqueue scan for PowerShell: {ex.Message}");
            }
        }

        // Drains the queue to the PowerShell pipe. Runs on a background thread, so blocking here can
        // never freeze the UI. A scan is held (not dropped) through a transient PowerShell restart.
        private void ScannerWriterLoop()
        {
            while (_scanWriterRunning)
            {
                string line;
                try { line = _scanWriteQueue.Take(); }
                catch (InvalidOperationException) { break; } // CompleteAdding() called -> shutting down
                catch (Exception) { break; }

                bool delivered = false;
                while (_scanWriterRunning && !delivered)
                {
                    try
                    {
                        var proc = _scannerProcess;
                        if (proc != null && !proc.HasExited)
                        {
                            proc.StandardInput.WriteLine(line);
                            proc.StandardInput.Flush();
                            delivered = true;
                        }
                        else
                        {
                            // Process down: ask the UI thread to (re)start it, then retry the same line.
                            RequestScannerRestart();
                            System.Threading.Thread.Sleep(200);
                        }
                    }
                    catch (Exception)
                    {
                        // Pipe broke mid-write (process died). Request a restart and retry the line.
                        RequestScannerRestart();
                        System.Threading.Thread.Sleep(200);
                    }
                }
            }
        }

        // Ask the UI thread to (re)start the scanner script, throttled so a backlog can't spam restarts.
        private void RequestScannerRestart()
        {
            try
            {
                var now = DateTime.UtcNow;
                if ((now - _lastScannerRestartRequestUtc).TotalSeconds < 3) return;
                _lastScannerRestartRequestUtc = now;
                if (this.IsHandleCreated && !this.IsDisposed)
                    this.BeginInvoke(new Action(() => { try { StartScanScript(); } catch { } }));
            }
            catch { }
        }

        // Stops the background writer (called on form close, before the pipe is torn down). Any scans
        // still queued in memory are flushed straight to the (still-alive) PowerShell process so they
        // reach api_upload_logs.jsonl / scans_backup.csv instead of being lost on a quick close.
        private void StopScannerWriter()
        {
            _scanWriterRunning = false;
            try { _scanWriteQueue?.CompleteAdding(); } catch { }
            bool stopped = false;
            try { stopped = _scanWriterThread == null || _scanWriterThread.Join(2000); } catch { }

            // Only drain ourselves once the writer thread has actually exited, so we never write to
            // the same stdin from two threads at once.
            if (stopped && _scanWriteQueue != null)
            {
                try
                {
                    var proc = _scannerProcess;
                    while (_scanWriteQueue.TryTake(out string line))
                    {
                        try
                        {
                            if (proc != null && !proc.HasExited)
                            {
                                proc.StandardInput.WriteLine(line);
                                proc.StandardInput.Flush();
                            }
                            else break; // process already gone; nothing more we can do
                        }
                        catch { break; }
                    }
                }
                catch { }
            }
        }
        // ------------------------------------------------------------------------------------------

        private void StartScanScript()
        {
            // Ensure the background stdin writer is running before we (re)start the scanner script.
            StartScannerWriter();

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
                        scanner.Supplier = assignment.Supplier;
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
                        _scannerInUseDialogShown = false; // re-arm: a port that connects is no longer locked
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

                // Update count labels
                UpdateCountLabels();

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

            // Keep trying for a short window so a scanner that finishes enumerating after a cold
            // boot still gets connected automatically, without the user reconnecting manually.
            StartScannerAutoReconnect();
        }

        // Starts (or restarts) the bounded post-init retry window. Idempotent and cheap: each tick
        // only opens scanners that are not already connected. Marshals to the UI thread because a
        // WinForms timer must be created/started on the thread that owns the message loop.
        private void StartScannerAutoReconnect()
        {
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(StartScannerAutoReconnect)); } catch { }
                return;
            }

            try
            {
                _scannerReconnectAttemptsLeft = ScannerReconnectMaxAttempts;
                if (_scannerReconnectTimer == null)
                {
                    _scannerReconnectTimer = new System.Windows.Forms.Timer();
                    _scannerReconnectTimer.Interval = ScannerReconnectIntervalMs;
                    _scannerReconnectTimer.Tick += ScannerReconnectTimer_Tick;
                }
                _scannerReconnectTimer.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AUTO-RECONNECT] Failed to start retry timer: {ex.Message}");
            }
        }

        private void ScannerReconnectTimer_Tick(object sender, EventArgs e)
        {
            ConnectDetectedScanners();
            _scannerReconnectAttemptsLeft--;
            if (_scannerReconnectAttemptsLeft <= 0)
            {
                _scannerReconnectTimer?.Stop();
            }
        }

        // Called from WndProc when a USB device-change is seen. Debounces both to coalesce the burst
        // of WM_DEVICECHANGE messages and to give Windows time to actually create the COM port before
        // we re-scan.
        private void OnScannerDeviceChange()
        {
            try
            {
                if (_deviceChangeDebounceTimer == null)
                {
                    _deviceChangeDebounceTimer = new System.Windows.Forms.Timer();
                    _deviceChangeDebounceTimer.Interval = 1500;
                    _deviceChangeDebounceTimer.Tick += (s, e) =>
                    {
                        _deviceChangeDebounceTimer.Stop();
                        ConnectDetectedScanners();
                    };
                }
                _deviceChangeDebounceTimer.Stop();
                _deviceChangeDebounceTimer.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AUTO-RECONNECT] Device-change handling error: {ex.Message}");
            }
        }

        // Idempotent: detect COM scanners and open only those not already connected, applying any
        // saved Line/Block/COM settings exactly like the initial init. Detection + open run on a
        // background thread (OpenScanner briefly blocks on a WMI query and a stabilize sleep) and are
        // guarded so overlapping triggers (retry tick + device-change) can't run two scans at once.
        // Scanners that are already open are skipped entirely, so active scanning is never disturbed.
        private void ConnectDetectedScanners()
        {
            if (_scannerComPortManager == null) return;
            if (Interlocked.CompareExchange(ref _scannerScanInProgress, 1, 0) != 0) return;

            Task.Run(() =>
            {
                try
                {
                    var detected = ComPortScannerDetection.DetectComPortScanners();
                    if (detected == null || detected.Count == 0) return;

                    string assignmentsPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                        "ScanLink", "scanner_assignments.txt");
                    var assignments = LoadScannerAssignmentsFromFile(assignmentsPath);

                    foreach (var scanner in detected)
                    {
                        if (scanner == null || string.IsNullOrEmpty(scanner.PNPDeviceID))
                            continue;

                        // Already connected — leave it completely alone. Check BOTH the PNP id and the
                        // COM-port name: the same physical port can be re-detected under a different
                        // (sometimes synthetic) PNP id, and serial-port exclusivity is per port name,
                        // so matching only on PNP id would let us re-open a port we already hold.
                        if (_scannerComPortManager.IsScannerOpen(scanner.PNPDeviceID) ||
                            _scannerComPortManager.IsComPortOpen(scanner.ComPort))
                            continue;

                        if (assignments.TryGetValue(scanner.PNPDeviceID, out var assignment))
                        {
                            scanner.LineID = assignment.LineID;
                            scanner.BlockID = assignment.BlockID;
                            scanner.Supplier = assignment.Supplier;
                            scanner.BaudRate = assignment.BaudRate;
                            scanner.Parity = assignment.Parity;
                            scanner.DataBits = assignment.DataBits;
                            scanner.StopBits = assignment.StopBits;
                        }

                        if (_scannerComPortManager.OpenScanner(scanner))
                        {
                            _scannerInUseDialogShown = false; // re-arm: a port that connects is no longer locked
                            var s = scanner;
                            SafeAppendScannerOutput($"🔌 Auto-connected scanner: {s.DeviceName} ({s.ComPort}) - Line {s.LineID}, Block {s.BlockID}");
                            if (IsHandleCreated)
                            {
                                try { BeginInvoke(new Action(() => { try { UpdateCountLabels(); } catch { } })); }
                                catch { }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AUTO-RECONNECT] Scan/open error: {ex.Message}");
                }
                finally
                {
                    Interlocked.Exchange(ref _scannerScanInProgress, 0);
                }
            });
        }

        // Thread-safe append to the scanner output box (callable from the background scan task).
        private void SafeAppendScannerOutput(string message)
        {
            try
            {
                var box = scannerOutputTextBox;
                // Skip until the handle exists so we never create it on the background thread.
                if (box == null || !box.IsHandleCreated) return;
                if (box.InvokeRequired)
                {
                    box.BeginInvoke(new Action(() => SafeAppendScannerOutput(message)));
                    return;
                }
                string ts = DateTime.Now.ToString("HH:mm:ss");
                box.AppendText($"[{ts}] {message}\r\n");
                box.ScrollToCaret();
            }
            catch { }
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
                        else if (trimmed.StartsWith("Supplier:"))
                        {
                            currentScanner.Supplier = trimmed.Substring("Supplier:".Length).Trim();
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
            _productDetailCts?.Cancel();
            _productDetailCts?.Dispose();
            _productDetailCts = null;

            // Stop and dispose the upload service
            _scanLogUploadService?.Dispose();
            
            // Stop scanner auto-reconnect timers before tearing down the manager
            try { _scannerReconnectTimer?.Stop(); _scannerReconnectTimer?.Dispose(); } catch { }
            try { _deviceChangeDebounceTimer?.Stop(); _deviceChangeDebounceTimer?.Dispose(); } catch { }

            // Dispose COM port scanner manager
            _scannerComPortManager?.Dispose();
            _rawInputManager?.Dispose();

            // Stop the background stdin writer before tearing down the pipe so it can't write into a
            // closing stream.
            StopScannerWriter();

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

        /// <summary>
        /// Recovers a dropped first digit from a UPC-A barcode.
        /// Many scanners are configured to suppress the UPC-A "Number System" digit,
        /// transmitting only 11 of the 12 digits. This method uses the UPC-A check
        /// digit algorithm to mathematically recover the missing first digit.
        /// </summary>
        private static string RecoverUpcFirstDigitIfNeeded(string rawBarcode)
        {
            if (string.IsNullOrEmpty(rawBarcode))
                return rawBarcode;

            // Strip any AIM Symbology Identifier prefix (e.g. ]C1)
            string cleaned = rawBarcode.Trim();
            if (cleaned.StartsWith("]") && cleaned.Length >= 3)
                cleaned = cleaned.Substring(3);

            // Extract only digit characters
            string digitsOnly = new string(cleaned.Where(char.IsDigit).ToArray());

            // Only apply recovery when we have exactly 11 digits (dropped first digit)
            if (digitsOnly.Length != 11)
                return rawBarcode;

            // We know our barcodes always use '1' as the guard digit.
            // If the scanner dropped the first digit, it was the '1'.
            string recoveredBarcode = "1" + digitsOnly;
            Debug.WriteLine($"[UPC-RECOVERY] Scanner sent 11 digits, prepended guard digit '1': {digitsOnly} → {recoveredBarcode}");

            return recoveredBarcode;
        }

        private void ScannerComPortManager_DataReceived(object sender, ScannerDataReceivedEventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                try
                {
                    string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    Debug.WriteLine($"[SCAN] Data received: {e.Data} from {e.Scanner.DeviceName} ({e.Scanner.ComPort})");

                    // Recover dropped first digit if scanner suppressed UPC-A Number System digit
                    string barcodeData = RecoverUpcFirstDigitIfNeeded(e.Data);
                    if (barcodeData != e.Data)
                    {
                        Debug.WriteLine($"[SCAN] UPC-A first digit recovered: '{e.Data}' → '{barcodeData}'");
                    }
                    
                    // ALWAYS show in scanner output textbox with detailed info (even if panel not visible)
                    if (scannerOutputTextBox != null)
                    {
                        scannerOutputTextBox.AppendText($"\r\n");
                        scannerOutputTextBox.AppendText($"════════════════════════════════════════\r\n");
                        scannerOutputTextBox.AppendText($"[{timestamp}] 📥 SCAN RECEIVED\r\n");
                        scannerOutputTextBox.AppendText($"════════════════════════════════════════\r\n");
                        scannerOutputTextBox.AppendText($"    Port: {e.Scanner.ComPort}\r\n");
                        scannerOutputTextBox.AppendText($"    Raw:  {e.Data}\r\n");
                        scannerOutputTextBox.AppendText($"    Data: {barcodeData}\r\n");
                        if (barcodeData != e.Data)
                            scannerOutputTextBox.AppendText($"    ⚡ UPC-A first digit auto-recovered\r\n");
                        scannerOutputTextBox.AppendText($"    Line ID: {e.Scanner.LineID ?? "Not Set"}\r\n");
                        scannerOutputTextBox.AppendText($"    Block ID: {e.Scanner.BlockID ?? "Not Set"}\r\n");
                        scannerOutputTextBox.AppendText($"    Supplier: {e.Scanner.Supplier ?? "Not Set"}\r\n");
                    }
                    
                    // Hand the scan to the background stdin writer. This never blocks the UI thread,
                    // even when PowerShell is busy - which is what stops the app freezing / scanning
                    // stalling as the on-disk files grow. The writer auto-(re)starts the script and
                    // holds the scan if the process is momentarily down, so no scan is dropped here.
                    string formattedData = $"{e.Scanner.PNPDeviceID}|{barcodeData}";
                    EnqueueScannerLine(formattedData);
                    Debug.WriteLine($"[SCAN] Queued for PowerShell: {formattedData}");
                    if (scannerContentPanel.Visible && scannerOutputTextBox != null)
                    {
                        scannerOutputTextBox.AppendText($"    ✓ Queued for processing\r\n\r\n");
                        scannerOutputTextBox.ScrollToCaret();
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

        private void RawInputManager_HidScanReceived(object sender, HidScanEventArgs e)
        {
            if (this.IsDisposed)
            {
                return;
            }

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new MethodInvoker(() => RawInputManager_HidScanReceived(sender, e)));
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string connectionTypeToken = e.ConnectionType == HidConnectionType.HidKeyboard ? HidKeyboardConnectionType : HidRawConnectionType;
            string sourceLabel = e.ConnectionType == HidConnectionType.HidKeyboard ? HidKeyboardSourceLabel : HidRawSourceLabel;

            string deviceId = e.Device?.PnpDeviceId;
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                deviceId = e.Device?.DevicePath;
            }
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                deviceId = "UNKNOWN-HID";
            }

            string friendlyName = string.IsNullOrWhiteSpace(e.Device?.FriendlyName) ? deviceId : e.Device.FriendlyName;

            if (scannerOutputTextBox != null)
            {
                scannerOutputTextBox.AppendText("\r\n");
                scannerOutputTextBox.AppendText("════════════════════════════════════════\r\n");
                scannerOutputTextBox.AppendText($"[{timestamp}] 📥 {sourceLabel} SCAN\r\n");
                scannerOutputTextBox.AppendText("════════════════════════════════════════\r\n");
                scannerOutputTextBox.AppendText($"    Device: {friendlyName}\r\n");
                scannerOutputTextBox.AppendText($"    Device ID: {deviceId}\r\n");
                if (!string.IsNullOrWhiteSpace(e.Device?.Vid) || !string.IsNullOrWhiteSpace(e.Device?.Pid))
                {
                    scannerOutputTextBox.AppendText($"    VID/PID: {e.Device?.Vid}/{e.Device?.Pid}\r\n");
                }
                if (!string.IsNullOrWhiteSpace(e.Device?.SerialNumber))
                {
                    scannerOutputTextBox.AppendText($"    Serial: {e.Device?.SerialNumber}\r\n");
                }
                scannerOutputTextBox.AppendText($"    Data: {e.Data}\r\n");
                scannerOutputTextBox.ScrollToCaret();
            }

            TrackHidScanner(deviceId, friendlyName, sourceLabel);

            ForwardHidScanToPowerShell(connectionTypeToken, sourceLabel, e.Device, e.Data);
        }

        // Remember an HID (keyboard/raw) scanner so it appears in the dashboard count and the
        // "Connected Scanners" grid. Line/Block/Supplier are read from scanner_assignments.txt —
        // the same source the PowerShell recorder uses to label scans. Runs on the UI thread.
        private void TrackHidScanner(string deviceId, string friendlyName, string sourceLabel)
        {
            if (string.IsNullOrWhiteSpace(deviceId)) return;
            if (_activeHidScanners.ContainsKey(deviceId)) return; // already listed this session

            string line = "", block = "", supplier = "";
            try
            {
                string assignmentsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "ScanLink", "scanner_assignments.txt");
                if (File.Exists(assignmentsPath))
                {
                    var assignments = LoadScannerAssignmentsFromFile(assignmentsPath);
                    if (assignments != null && assignments.TryGetValue(deviceId, out var cfg) && cfg != null)
                    {
                        line = cfg.LineID ?? "";
                        block = cfg.BlockID ?? "";
                        supplier = cfg.Supplier ?? "";
                    }
                }
            }
            catch { /* display-only; never block a scan over a label lookup */ }

            _activeHidScanners[deviceId] = new ScannerConfig
            {
                PNPDeviceID = deviceId,
                DeviceName = friendlyName,
                ComPort = sourceLabel, // surfaces "USB-HID Keyboard" / "USB-HID Raw" in the Port column
                LineID = line,
                BlockID = block,
                Supplier = supplier
            };
        }

        private void RawInputManager_Diagnostics(object sender, string message)
        {
            if (this.IsDisposed)
            {
                return;
            }

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new MethodInvoker(() => RawInputManager_Diagnostics(sender, message)));
                return;
            }

            if (scannerOutputTextBox != null)
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                scannerOutputTextBox.AppendText($"[{timestamp}] [HID DEBUG] {message}\r\n");
                scannerOutputTextBox.ScrollToCaret();
            }
        }

        private void ForwardHidScanToPowerShell(string connectionTypeToken, string sourceLabel, HidDeviceInfo deviceInfo, string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return;
            }

            string deviceId = deviceInfo?.PnpDeviceId;
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                deviceId = deviceInfo?.DevicePath;
            }
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                deviceId = "UNKNOWN-HID";
            }

            string friendlyName = string.IsNullOrWhiteSpace(deviceInfo?.FriendlyName) ? deviceId : deviceInfo.FriendlyName;

            var meta = new
            {
                connectionType = connectionTypeToken,
                deviceId = deviceId,
                devicePath = deviceInfo?.DevicePath,
                friendlyName = friendlyName,
                vid = deviceInfo?.Vid,
                pid = deviceInfo?.Pid,
                serial = deviceInfo?.SerialNumber,
                manufacturer = deviceInfo?.Manufacturer,
                source = sourceLabel
            };

            string metaJson = HidMetaSerializer.Serialize(meta);
            // Recover dropped first digit if HID scanner suppressed UPC-A Number System digit
            string recoveredData = RecoverUpcFirstDigitIfNeeded(data);
            if (recoveredData != data)
            {
                Debug.WriteLine($"[HID] UPC-A first digit recovered: '{data}' → '{recoveredData}'");
            }
            string formattedData = $"{metaJson}|{recoveredData}";

            // Hand off to the background stdin writer (non-blocking; auto-restarts PowerShell and
            // holds the scan if the process is momentarily down).
            EnqueueScannerLine(formattedData);
            Debug.WriteLine($"[HID] Queued for PowerShell: {formattedData}");
            if (scannerOutputTextBox != null)
            {
                scannerOutputTextBox.AppendText($"    ✓ Queued for processing\r\n\r\n");
                scannerOutputTextBox.ScrollToCaret();
            }
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

                if (e.ErrorMessage.Contains("already in use by another application"))
                {
                    // After the self-conflict fix the app never tries to re-open a port it already
                    // owns, so reaching here means another application (e.g. winscan) genuinely holds
                    // the port — exactly when the user SHOULD be told to close it. We only throttle:
                    // show one popup at a time so the retry timer / USB device-arrival watcher can't
                    // stack dialogs and freeze the UI (the original bug). The throttle re-arms as soon
                    // as any scanner connects successfully, so a later lock will alert again.
                    if (!_scannerInUseDialogShown)
                    {
                        _scannerInUseDialogShown = true;
                        MessageBox.Show(e.ErrorMessage, "Scanner Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
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


        protected override void WndProc(ref Message m)
        {
            if (_rawInputManager != null && _rawInputManager.IsRegistered)
            {
                try
                {
                    _rawInputManager.ProcessMessage(ref m);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[HID] Raw input processing error: {ex.Message}");
                }
            }

            // Auto-reconnect COM scanners when a USB device appears (e.g. a scanner that finished
            // enumerating after a cold boot, or one that was re-plugged). DBT_DEVNODES_CHANGED is
            // broadcast to top-level windows without needing RegisterDeviceNotification.
            const int WM_DEVICECHANGE = 0x0219;
            const int DBT_DEVNODES_CHANGED = 0x0007;
            const int DBT_DEVICEARRIVAL = 0x8000;
            if (m.Msg == WM_DEVICECHANGE)
            {
                int evt = m.WParam.ToInt32();
                if (evt == DBT_DEVNODES_CHANGED || evt == DBT_DEVICEARRIVAL)
                {
                    OnScannerDeviceChange();
                }
            }

            base.WndProc(ref m);
        }

        // Print count changed (kept for designer wiring; no-op by default)
        private void numericUpDown_count_ValueChanged(object sender, EventArgs e)
        {
            // Auto-save settings when changed
            SaveAdvancedSettings();
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
        private int pageSize = 13;
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

                // Copy each column value and replace CropID/ProductID with names where possible
                foreach (DataColumn column in sourceData.Columns)
                {
                    if (column.ColumnName == "CropID")
                    {
                        string cropId = sourceRow[column.ColumnName]?.ToString() ?? "";
                        string cropName = GetCropNameFromCache(cropId);
                        newRow[column.ColumnName] = cropName;
                    }
                    else if (column.ColumnName == "ProductID")
                    {
                        string productId = sourceRow[column.ColumnName]?.ToString() ?? "";
                        string productName = GetProductNameFromCache(productId);
                        newRow[column.ColumnName] = productName;
                    }
                    else
                    {
                        newRow[column.ColumnName] = sourceRow[column.ColumnName];
                    }
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

                // Update column headers for better display
                if (scannerDataGridView.Columns.Contains("CropID"))
                {
                    scannerDataGridView.Columns["CropID"].HeaderText = "Crops";
                }
                if (scannerDataGridView.Columns.Contains("ProductID"))
                {
                    scannerDataGridView.Columns["ProductID"].HeaderText = "Products";
                }
                if (scannerDataGridView.Columns.Contains("ParsedInfo"))
                {
                    scannerDataGridView.Columns["ParsedInfo"].HeaderText = "Parsed Info";
                }

                // Set column widths to utilize full available space
                UpdateDataGridViewColumnWidths();

                // Force refresh of the DataGridView
                scannerDataGridView.Refresh();
            }
        }

        private void UpdateDataGridViewColumnWidths()
        {
            if (scannerDataGridView != null && scannerDataGridView.Columns.Count >= 9)
            {
                // Calculate available width for columns (subtract some margin for scrollbar)
                int availableWidth = scannerDataGridView.Width - 20; // Reserve 20px for potential scrollbar

                // Distribute width proportionally based on content importance and typical length
                scannerDataGridView.Columns["Date"].Width = (int)(availableWidth * 0.08);
                scannerDataGridView.Columns["Time"].Width = (int)(availableWidth * 0.06);
                scannerDataGridView.Columns["SerialNumber"].Width = (int)(availableWidth * 0.09);
                scannerDataGridView.Columns["BlockNumber"].Width = (int)(availableWidth * 0.08);
                scannerDataGridView.Columns["LineNumber"].Width = (int)(availableWidth * 0.08);
                scannerDataGridView.Columns["Supplier"].Width = (int)(availableWidth * 0.10);
                scannerDataGridView.Columns["CropID"].Width = (int)(availableWidth * 0.13);
                scannerDataGridView.Columns["ProductID"].Width = (int)(availableWidth * 0.22);
                scannerDataGridView.Columns["ParsedInfo"].Width = (int)(availableWidth * 0.16);
            }
        }

        private string GetCropNameFromCache(string cropId)
        {
            if (string.IsNullOrEmpty(cropId) || _productCombinationsService == null || !_productCombinationsService.HasCachedData())
            {
                return cropId; // Return original ID if cache not available
            }

            var crops = _productCombinationsService.GetUniqueCrops();
            var crop = crops?.FirstOrDefault(c => string.Equals(c.crop_id, cropId, StringComparison.OrdinalIgnoreCase));
            return crop != null ? crop.crop_name : cropId; // Return name if found, otherwise original ID
        }

        private string GetProductNameFromCache(string productId)
        {
            if (string.IsNullOrEmpty(productId) || _productCombinationsService == null || !_productCombinationsService.HasCachedData())
            {
                return productId; // Return original ID if cache not available
            }

            var allCombinations = _productCombinationsService.GetAllCombinations();
            var product = allCombinations?.FirstOrDefault(p => string.Equals(p.product_id, productId, StringComparison.OrdinalIgnoreCase));
            return product != null ? product.product_name : productId; // Return name if found, otherwise original ID
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
            UpdateCountLabels();
        }

        private void applyFiltersButton_Click(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void cropIdComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Get the selected crop ID and repopulate product combo box with filtered products
            string selectedCropId = cropIdComboBox.SelectedValue?.ToString() ?? "";
            PopulateProductIdComboBox(selectedCropId);

            ApplyFilters();
        }

        private void productIdComboBox_SelectedIndexChanged(object sender, EventArgs e)
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
            filterProductId = productIdComboBox.SelectedValue?.ToString() ?? "";
            filterCropId = cropIdComboBox.SelectedValue?.ToString() ?? "";

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
            productIdComboBox.SelectedIndex = productIdComboBox.Items.Count > 0 ? 0 : -1;
            cropIdComboBox.SelectedIndex = cropIdComboBox.Items.Count > 0 ? 0 : -1;

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

        private void PopulateProductIdComboBox(string cropId = null)
        {
            if (productIdComboBox == null)
                return;

            // Always recreate the product items when cropId changes to ensure proper filtering
            _scannerProductItems = CreateScannerProductItemsFromCache(cropId)
                ?? new List<ProductComboItem>
                {
                    new ProductComboItem { Name = "manual p1", Value = "p1" },
                    new ProductComboItem { Name = "manual p2", Value = "p2" },
                    new ProductComboItem { Name = "manual p3", Value = "p3" }
                };

            productIdComboBox.DataSource = null;
            productIdComboBox.Items.Clear();
            productIdComboBox.DisplayMember = nameof(ProductComboItem.Name);
            productIdComboBox.ValueMember = nameof(ProductComboItem.Value);

            var items = new List<ProductComboItem>
            {
                new ProductComboItem { Name = "All Products", Value = "" }
            };
            items.AddRange(_scannerProductItems);

            productIdComboBox.DataSource = items;
            productIdComboBox.SelectedIndex = 0;
            SetProductDetailFields("N/A", "N/A", "N/A", "N/A");
        }

        private void PopulateCropIdComboBox()
        {
            if (cropIdComboBox == null) return;

            var items = CreateCropComboItemsFromCache(includeEmpty: true);
            if (items == null || items.Count == 0)
            {
                items = CreateFallbackCropComboItems(includeEmpty: true);
            }

            SetComboItems(cropIdComboBox, items, null, selectFirstOnMissing: true);
        }

        // Path to the uncapped, append-only history written by scan_capture.ps1.
        private string GetBackupCsvPath()
        {
            string programDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ScanLink");
            return Path.Combine(programDataDir, "scans_backup.csv");
        }

        // Bring the in-memory metrics index up to date with scans_backup.csv. Reads only the bytes
        // appended since the last call (tracked by _metricsCsvOffset), so the cost per refresh is
        // proportional to new scans, not total history. Re-seeds from scratch if the file was
        // truncated/rotated (length shrank below our offset) or the path changed.
        private void RefreshMetricsIndex()
        {
            try
            {
                string path = GetBackupCsvPath();
                lock (_metricsLock)
                {
                    if (!File.Exists(path))
                    {
                        _metricsIndex.Clear();
                        _metricsCsvOffset = 0;
                        _metricsCsvPath = path;
                        return;
                    }

                    long len = new FileInfo(path).Length;
                    if (!string.Equals(_metricsCsvPath, path, StringComparison.OrdinalIgnoreCase) || len < _metricsCsvOffset)
                    {
                        // New file or it shrank -> rebuild from the top.
                        _metricsIndex.Clear();
                        _metricsCsvOffset = 0;
                        _metricsCsvPath = path;
                    }

                    if (len <= _metricsCsvOffset)
                        return; // nothing new appended

                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        fs.Seek(_metricsCsvOffset, SeekOrigin.Begin);
                        int toRead = (int)(len - _metricsCsvOffset);
                        byte[] buf = new byte[toRead];
                        int filled = 0;
                        while (filled < toRead)
                        {
                            int n = fs.Read(buf, filled, toRead - filled);
                            if (n <= 0) break;
                            filled += n;
                        }

                        // Only consume up to the last complete line so a half-written trailing line
                        // (PS mid-append) is picked up on the next refresh instead of being mangled.
                        int lastNl = Array.LastIndexOf(buf, (byte)0x0A, filled - 1);
                        if (lastNl < 0)
                            return; // no complete new line yet

                        string text = Encoding.UTF8.GetString(buf, 0, lastNl + 1);
                        _metricsCsvOffset += (lastNl + 1); // advance by exact bytes consumed

                        foreach (var raw in text.Split('\n'))
                        {
                            string line = raw.TrimEnd('\r');
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            if (line.StartsWith("Timestamp,", StringComparison.OrdinalIgnoreCase)) continue; // header

                            // Columns: Timestamp,EmployeeId,CropId,ProductId,LineNumber,BlockNumber,...
                            // The fields we need are the first six; later device fields may contain
                            // commas, so we never index past column 5.
                            string[] parts = line.Split(',');
                            if (parts.Length < 6) continue;
                            if (!DateTime.TryParse(parts[0], out DateTime ts)) continue;

                            _metricsIndex.Add(new ScanMeta
                            {
                                Ts = ts,
                                Crop = parts[2],
                                Product = parts[3],
                                Line = parts[4],
                                Block = parts[5]
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[METRICS] index refresh failed: {ex.Message}");
            }
        }

        private void UpdateCountLabels()
        {
            // Make sure the index reflects the latest scans before counting. Cheap when nothing new
            // has been appended (returns immediately once the byte offset is caught up).
            RefreshMetricsIndex();

            // Count today's scans
            int todayScans = CountTodaysScans();

            // Count last hour scans
            int lastHourScans = CountLastHourScans();

            // Update labels
            if (todayScansLabel != null)
                todayScansLabel.Text = $"Today's Scans: {todayScans}";
            if (lastHourScansLabel != null)
                lastHourScansLabel.Text = $"Last Hour Scans: {lastHourScans}";
            if (seasonScansLabel != null)
            {
                seasonScansLabel.Text = _activeSeason != null
                    ? $"Season Scans: {CountSeasonScans()}"
                    : "Season Scans: N/A";
            }
        }

        private int CountTodaysScans()
        {
            // The web dashboard's "Today" KPI windows scans by UTC calendar day (00:00-23:59:59
            // UTC), not the local (SAST) calendar day. scans_backup.csv timestamps are local, so
            // convert the UTC day boundary to local time before comparing - that way both UIs
            // agree on which scans count as "today" instead of disagreeing near midnight SAST.
            DateTime utcTodayStart = DateTime.UtcNow.Date;
            DateTime windowStart = utcTodayStart.ToLocalTime();
            DateTime windowEnd = utcTodayStart.AddDays(1).ToLocalTime();
            int count = 0;
            lock (_metricsLock)
            {
                foreach (var m in _metricsIndex)
                    if (m.Ts >= windowStart && m.Ts < windowEnd) count++;
            }
            return count;
        }

        private int CountLastHourScans()
        {
            DateTime oneHourAgo = DateTime.Now.AddHours(-1);
            int count = 0;
            lock (_metricsLock)
            {
                foreach (var m in _metricsIndex)
                    if (m.Ts >= oneHourAgo) count++;
            }
            return count;
        }

        // Total scans within the site's active season (start_date..end_date, or today if the
        // season is still ongoing/has no end_date yet), counted over the full history index -
        // i.e. the same season boundary the web dashboard would use, filtered from the local CSV.
        private int CountSeasonScans()
        {
            if (_activeSeason == null) return 0;

            DateTime start = _activeSeason.StartDate.Date;
            DateTime end = (_activeSeason.EndDate ?? DateTime.Today).Date;
            int count = 0;
            lock (_metricsLock)
            {
                foreach (var m in _metricsIndex)
                    if (m.Ts.Date >= start && m.Ts.Date <= end) count++;
            }
            return count;
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

        private void UpdateDimensionLabels()
        {
            if (numericUpDown_dpi == null || numericUpDown_dpi.Value == 0)
                return;

            decimal dpi = numericUpDown_dpi.Value;

            // Update width label with conversion
            if (numericUpDown_width != null && label_width != null)
            {
                decimal pixels = numericUpDown_width.Value;
                decimal inches = pixels / dpi;
                label_width.Text = $"Width ( {inches:F2}\" )";
            }

            // Update height label with conversion
            if (numericUpDown_height != null && label_height != null)
            {
                decimal pixels = numericUpDown_height.Value;
                decimal inches = pixels / dpi;
                label_height.Text = $"Height ( {inches:F2}\" )";
            }

            // Update xCoordinate label with conversion
            if (numericUpDown_xCoordinate != null && label_xCoordinate != null)
            {
                decimal pixels = numericUpDown_xCoordinate.Value;
                decimal inches = pixels / dpi;
                label_xCoordinate.Text = $"X Coordinate ( {inches:F2}\" )";
            }

            // Update x2Coordinate label with conversion
            if (numericUpDown_x2Coordinate != null && label_x2Coordinate != null)
            {
                decimal pixels = numericUpDown_x2Coordinate.Value;
                decimal inches = pixels / dpi;
                label_x2Coordinate.Text = $"X2 Coordinate ({inches:F2}\")";
            }
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
                     ["EmployeeID"] = textBox_EmployeeID?.Text ?? "123456",
                    ["SelectedEmployeeName"] = _selectedEmployeeName ?? "",
                    ["SelectedEmployeeBarcodeId"] = _selectedEmployeeBarcodeId ?? "",
                    ["ProductID"] = comboBox_ProductID?.SelectedValue?.ToString() ?? "000",
                    ["CropID"] = comboBox_CropID?.SelectedValue?.ToString() ?? "000",
                    ["Width"] = numericUpDown_width?.Value.ToString() ?? "400",
                    ["Height"] = numericUpDown_height?.Value.ToString() ?? "180",
                    ["XCoordinate"] = numericUpDown_xCoordinate?.Value.ToString() ?? "250",
                    ["TwoUpEnabled"] = checkBox_twoUp?.Checked.ToString() ?? "False",
                    ["X2Coordinate"] = numericUpDown_x2Coordinate?.Value.ToString() ?? "0",
                    ["Gap"] = numericUpDown_gap?.Value.ToString() ?? "2",
                    ["Darkness"] = trackBar_darkness?.Value.ToString() ?? "5",
                    ["PrintSpeed"] = comboBox_speed?.SelectedItem?.ToString() ?? "5 - Medium",
                    ["DPI"] = numericUpDown_dpi?.Value.ToString() ?? "203"
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
                // Printer Language - set this first and populate the dependent comboboxes
                if (settings.ContainsKey("PrinterLanguage") && comboBox_emulation != null)
                {
                    string value = settings["PrinterLanguage"];
                    for (int i = 0; i < comboBox_emulation.Items.Count; i++)
                    {
                        if (comboBox_emulation.Items[i].ToString() == value)
                        {
                            // Temporarily remove event handler to prevent it from resetting other comboboxes
                            comboBox_emulation.SelectedIndexChanged -= comboBox_emulation_SelectedIndexChanged;

                            comboBox_emulation.SelectedIndex = i;

                            // Manually populate the dependent comboboxes (same logic as the event handler)
                            switch (value)
                            {
                                case "PPLB":
                                    comboBox_test.Items.Clear();
                                    foreach (FunctionData item in PPLB_ItemList) comboBox_test.Items.Add(item.Descration);
                                    comboBox_barcode.Items.Clear();
                                    foreach (string str in PPLB_BarcodeList) comboBox_barcode.Items.Add(str);
                                    break;
                            }

                            // Re-add event handler
                            comboBox_emulation.SelectedIndexChanged += comboBox_emulation_SelectedIndexChanged;
                            break;
                        }
                    }
                }

                // Now set Test Mode and Barcode Type after the comboboxes are populated
                // Test Mode
                if (settings.ContainsKey("TestMode") && comboBox_test != null && comboBox_test.Items.Count > 0)
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
                if (settings.ContainsKey("BarcodeType") && comboBox_barcode != null && comboBox_barcode.Items.Count > 0)
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

                // EmployeeID
                if (settings.ContainsKey("EmployeeID") && textBox_EmployeeID != null)
                {
                    textBox_EmployeeID.Text = settings["EmployeeID"];
                }

                // Selected employee name + the barcode ID it maps to, so the label can still print
                // the name after an app restart (only the ID was restored before).
                if (settings.ContainsKey("SelectedEmployeeName"))
                    _selectedEmployeeName = settings["SelectedEmployeeName"];
                if (settings.ContainsKey("SelectedEmployeeBarcodeId"))
                    _selectedEmployeeBarcodeId = settings["SelectedEmployeeBarcodeId"];

                // ProductID
                if (settings.ContainsKey("ProductID") && comboBox_ProductID != null)
                {
                    string value = settings["ProductID"];
                    _pendingPrinterProductSelection = value;
                    try
                    {
                        comboBox_ProductID.SelectedValue = value;
                    }
                    catch
                        {
                        // Will be applied once items refresh
                    }
                }

                // CropID
                if (settings.ContainsKey("CropID") && comboBox_CropID != null)
                {
                    string value = settings["CropID"];
                    _pendingPrinterCropSelection = value;
                    try
                    {
                        comboBox_CropID.SelectedValue = value;
                    }
                    catch
                        {
                        // Selection will be restored when combo items refresh
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

                // DPI
                if (settings.ContainsKey("DPI") && numericUpDown_dpi != null)
                {
                    if (decimal.TryParse(settings["DPI"], out decimal dpi))
                    {
                        numericUpDown_dpi.Value = Math.Max(numericUpDown_dpi.Minimum, Math.Min(numericUpDown_dpi.Maximum, dpi));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying settings: {ex.Message}");
            }
        }

        private void setupButton_Click(object sender, EventArgs e)
        {
            // Show the setup dialog with data tables
            SetupDialog setupDialog = new SetupDialog(_productCombinationsService);
            setupDialog.Show(); // Modeless dialog
        }

        private void scannerSetupButton_Click(object sender, EventArgs e)
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

				// Update count labels after reinitialization
				UpdateCountLabels();
			};
			scannerManagementForm.ShowDialog();
        }

        private PrinterConnectionDialog _printerConnectionDialog;

        private void printerConnectionButton_Click(object sender, EventArgs e)
        {
            if (_printerConnectionDialog == null)
            {
                _printerConnectionDialog = new PrinterConnectionDialog(this);
            }

            _printerConnectionDialog.Show();
        }

        private async void barCodesButton_Click(object sender, EventArgs e)
        {
            if (_configPopupForm == null)
            {
                _configPopupForm = new ConfigPopupForm(configPanel, actionPanel);

                // Wire up event handlers for combo boxes in the popup
                if (comboBox_CropID != null)
                {
                    comboBox_CropID.SelectedIndexChanged += comboBox_CropID_SelectedIndexChanged;
                }
                if (comboBox_ProductID != null)
                {
                    comboBox_ProductID.SelectedIndexChanged += comboBox_ProductID_SelectedIndexChanged;
                }
            }

            if (!_configPopupForm.Visible)
            {
                barCodesButton.Enabled = false;
                barCodesButton.BackColor = Color.FromArgb(123, 31, 162); // Darker purple when disabled
                _configPopupForm.Show();
                _configPopupForm.BringToFront();

                // Initialize crop options when popup opens (similar to printer panel)
                await EnsureCropOptionsLoadedAsync(updateStatusLabel: false);
            }
        }

        private void boxLabelsButton_Click(object sender, EventArgs e)
        {
            if (_boxLabelsPopupForm == null)
            {
                _boxLabelsPopupForm = new BoxLabelsPopupForm(advancedPanel, this);
            }

            if (!_boxLabelsPopupForm.Visible)
            {
                boxLabelsButton.Enabled = false;
                boxLabelsButton.BackColor = Color.FromArgb(0, 121, 107); // Darker teal when disabled
                _boxLabelsPopupForm.Show();
                _boxLabelsPopupForm.BringToFront();
            }
        }

        private void reportsButton_Click(object sender, EventArgs e)
        {
            // Log the reports button click
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] Reports button clicked by user");

            // Check if we have a valid token
            if (!_apiAuthService.IsTokenValid())
            {
                string errorMessage = "❌ Unable to access reports\n\n" +
                    "Authentication token is invalid or expired.\n\n" +
                    "Troubleshooting:\n" +
                    "• Please log in again using your credentials\n" +
                    "• Check your internet connection\n" +
                    "• Contact support if the problem persists";

                MessageBox.Show(errorMessage, "Authentication Required", MessageBoxButtons.OK, MessageBoxIcon.Error);
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] Reports access failed - invalid token");
                return;
            }

            try
            {
                // Show loading message
                statusLabel.Text = "🔄 Diverting to the webpage...";

                // Get the current token
                string token = _apiAuthService.GetCurrentToken();

                // Construct the URL with authentication parameters
                string baseUrl = "https://hr.labourlinksoftware.co.za/en/login";
                string authUrl = $"{baseUrl}?auth_token={Uri.EscapeDataString(token)}&fieldType=SCANLINK-DASHBOARD";

                // Try to open in Chrome first, then fallback to default browser
                bool opened = false;
                try
                {
                    // Check if Chrome is available
                    string chromePath = GetChromePath();
                    if (!string.IsNullOrEmpty(chromePath))
                    {
                        // Open in new tab in Chrome
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = chromePath,
                            Arguments = $"--new-tab \"{authUrl}\"",
                            UseShellExecute = true
                        });
                        opened = true;
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] Reports webpage opened in Chrome: {authUrl}");
                    }
                }
                catch (Exception chromeEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] Chrome not available or failed: {chromeEx.Message}");
                }

                // Fallback to default browser if Chrome failed
                if (!opened)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = authUrl,
                        UseShellExecute = true
                    });
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] Reports webpage opened in default browser: {authUrl}");
                }

                // Show success message
                statusLabel.Text = "✅ Successfully opened reports webpage";
                MessageBox.Show("Reports webpage opened successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Clear status after a short delay
                Task.Delay(3000).ContinueWith(_ =>
                {
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() => statusLabel.Text = ""));
                    }
                    else
                    {
                        statusLabel.Text = "";
                    }
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"❌ Failed to open reports webpage\n\nError: {ex.Message}\n\nPlease check your internet connection and try again.";
                MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "❌ Failed to open webpage";
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] Reports webpage opening failed: {ex.Message}");
            }
        }

        private string GetChromePath()
        {
            // Common Chrome installation paths
            string[] chromePaths = new string[]
            {
                @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
                @"C:\Users\" + Environment.UserName + @"\AppData\Local\Google\Chrome\Application\chrome.exe"
            };

            foreach (string path in chromePaths)
            {
                if (System.IO.File.Exists(path))
                {
                    return path;
                }
            }

            return null; // Chrome not found
        }

        private void Button_SizeChanged(object sender, EventArgs e)
        {
            Button button = sender as Button;
            if (button != null && button.Width > 0 && button.Height > 0)
            {
                int radius = 12; // Corner radius
                System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddArc(0, 0, radius, radius, 180, 90);
                path.AddArc(button.Width - radius, 0, radius, radius, 270, 90);
                path.AddArc(button.Width - radius, button.Height - radius, radius, radius, 0, 90);
                path.AddArc(0, button.Height - radius, radius, radius, 90, 90);
                path.CloseFigure();
                button.Region = new Region(path);
            }
        }

        private void Button_Paint(object sender, PaintEventArgs e)
        {
            Button button = sender as Button;
            if (button != null && button.FlatStyle == FlatStyle.Flat)
            {
                int radius = 12;
                int borderWidth = 1;
                Color borderColor = GetButtonBorderColor(button);

                using (System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    path.AddArc(borderWidth, borderWidth, radius, radius, 180, 90);
                    path.AddArc(button.Width - radius - borderWidth, borderWidth, radius, radius, 270, 90);
                    path.AddArc(button.Width - radius - borderWidth, button.Height - radius - borderWidth, radius, radius, 0, 90);
                    path.AddArc(borderWidth, button.Height - radius - borderWidth, radius, radius, 90, 90);
                    path.CloseFigure();

                    using (Pen pen = new Pen(borderColor, borderWidth))
                    {
                        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        e.Graphics.DrawPath(pen, path);
                    }
                }

                // Custom Text Drawing using Tag property ("Title|Description")
                if (button.Tag != null && button.Tag.ToString().Contains("|"))
                {
                    string[] parts = button.Tag.ToString().Split('|');
                    string title = parts[0];
                    string desc = parts[1];

                    e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    // Calculate color - use ForeColor, but if it's somehow transparent or we're hovering/pressing, adapt
                    Color textColor = button.ForeColor;
                    if (textColor == Color.Transparent) textColor = Color.White;

                    using (Font titleFont = new Font("Segoe UI", 10F, FontStyle.Bold))
                    using (Font descFont = new Font("Segoe UI", 8F, FontStyle.Regular))
                    using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    using (Brush textBrush = new SolidBrush(textColor))
                    {
                        // Draw Title slightly above center
                        Rectangle rectTitle = new Rectangle(0, 0, button.Width, button.Height / 2 + 5);
                        sf.LineAlignment = StringAlignment.Far; // Bottom
                        e.Graphics.DrawString(title, titleFont, textBrush, rectTitle, sf);

                        // Draw Description slightly below center
                        Rectangle rectDesc = new Rectangle(0, button.Height / 2 + 5, button.Width, button.Height / 2 - 5);
                        sf.LineAlignment = StringAlignment.Near; // Top
                        
                        // Use a slightly dimmer color for description if text is white
                        Color descColor = textColor == Color.White ? Color.FromArgb(200, 255, 255, 255) : Color.FromArgb(180, textColor);
                        using (Brush descBrush = new SolidBrush(descColor))
                        {
                            e.Graphics.DrawString(desc, descFont, descBrush, rectDesc, sf);
                        }
                    }
                }
            }
        }

        private Color GetButtonBorderColor(Button button)
        {
            // Return the appropriate border color based on button state
            if (!button.Enabled)
                return Color.Gray;
            else if (button.Focused)
                return Color.DodgerBlue;
            else if (button == setupButton)
                return Color.FromArgb(76, 175, 80);
            else if (button == scannerSetupButton)
                return Color.FromArgb(66, 165, 245);
            else if (button == printerConnectionButton)
                return Color.FromArgb(255, 193, 7);
            else if (button == barCodesButton)
                return Color.FromArgb(186, 104, 200);
            else if (button == boxLabelsButton)
                return Color.FromArgb(77, 182, 172);
            else if (button == reportsButton)
                return Color.FromArgb(161, 136, 127);
            else if (button == logoutButton)
                return Color.FromArgb(244, 143, 177);
            else
                return Color.Gray;
        }

        public void EnableBarCodesButton()
        {
            barCodesButton.Enabled = true;
            barCodesButton.BackColor = Color.FromArgb(156, 39, 176);
        }

        public void EnableBoxLabelsButton()
        {
            boxLabelsButton.Enabled = true;
            boxLabelsButton.BackColor = Color.FromArgb(0, 150, 136);
        }

        // Public methods for PrinterConnectionDialog
        public void UpdateConnectionUI(string connectionType, TextBox textBox, Label statusLabel)
        {
            switch (connectionType)
            {
                case "File":
                    textBox.Text = "📁 " + strFolder;
                    statusLabel.Text = "Status: File output configured";
                    statusLabel.ForeColor = System.Drawing.Color.FromArgb(46, 204, 113);
                    // Update main status bar with connection status
                    this.statusLabel.Text = "Status: File output configured";
                    this.statusLabel.ForeColor = System.Drawing.Color.FromArgb(46, 204, 113);
                    break;
                case "COM":
                    textBox.Text = "🔌 Serial: " + this.m_ComName + " (" + this.m_baudRate + " baud)";
                    statusLabel.Text = "Status: Serial port ready";
                    statusLabel.ForeColor = System.Drawing.Color.FromArgb(52, 152, 219);
                    // Update main status bar with connection status
                    this.statusLabel.Text = "Status: Serial port ready";
                    this.statusLabel.ForeColor = System.Drawing.Color.FromArgb(52, 152, 219);
                    break;
                case "USB":
                    if (string.IsNullOrWhiteSpace(this.m_USBDevicePath))
                    {
                        textBox.Text = "🔌 USB: Click Configure to select device";
                        statusLabel.Text = "Status: USB device not configured";
                        statusLabel.ForeColor = System.Drawing.Color.FromArgb(230, 126, 34);
                        // Update main status bar with connection status
                        this.statusLabel.Text = "Status: USB device not configured";
                        this.statusLabel.ForeColor = System.Drawing.Color.FromArgb(230, 126, 34);
                    }
                    else
                    {
                        textBox.Text = "🔌 USB: " + this.m_USBDevicePath;
                        statusLabel.Text = "Status: USB device configured";
                        statusLabel.ForeColor = System.Drawing.Color.FromArgb(46, 204, 113);
                        // Update main status bar with connection status
                        this.statusLabel.Text = "Status: USB device configured";
                        this.statusLabel.ForeColor = System.Drawing.Color.FromArgb(46, 204, 113);
                    }
                    break;
                case "LAN":
                    textBox.Text = "🌐 Network: " + this.MergeIPAddressAndPort(this.m_TCPAddress, this.m_TCPPort);
                    statusLabel.Text = "Status: Network connection configured";
                    statusLabel.ForeColor = System.Drawing.Color.FromArgb(52, 152, 219);
                    // Update main status bar with connection status
                    this.statusLabel.Text = "Status: Network connection configured";
                    this.statusLabel.ForeColor = System.Drawing.Color.FromArgb(52, 152, 219);
                    break;
                case "Multi-LAN":
                    textBox.Text = "🌐 Multi-LAN (Not supported in this version)";
                    statusLabel.Text = "Status: Feature not available";
                    statusLabel.ForeColor = System.Drawing.Color.FromArgb(149, 165, 166);
                    // Update main status bar with connection status
                    this.statusLabel.Text = "Status: Feature not available";
                    this.statusLabel.ForeColor = System.Drawing.Color.FromArgb(149, 165, 166);
                    break;
                default:
                    MessageBox.Show("Connection type not supported: " + connectionType, "Scan Link", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
            }
        }

        public void HandleConnectionConfigure(string connectionType, Form ownerForm)
        {
            switch (connectionType)
            {
                case "File":
                    using (var folderdlg = new FolderBrowserDialog())
                    {
                        folderdlg.SelectedPath = strFolder;
                        if (DialogResult.OK == folderdlg.ShowDialog()) strFolder = folderdlg.SelectedPath;
                    }
                    break;
                case "COM":
                    MessageBox.Show("For simplicity, COM settings are fixed in this minimal app.");
                    break;
                case "USB":
                    // open USBDialog to select USB Device.
                    USBDialog USBsetdlg = new USBDialog();
                    USBsetdlg.DevicePath = this.m_USBDevicePath;
                    // The vendor dialog's device list is multi-select and pre-selects the first
                    // row, so choosing a different printer can leave the first one selected too
                    // and OK returns that first one. Force single-select so the click sticks.
                    ForceUsbDialogSingleSelect(USBsetdlg);
                    if (DialogResult.OK == USBsetdlg.ShowDialog(ownerForm))
                    {
                        // setting USB Device.
                        this.m_USBDevicePath = USBsetdlg.DevicePath;
                    }
                    break;
                case "LAN":
                    var input = Microsoft.VisualBasic.Interaction.InputBox("Host[:Port]", "LAN Target", MergeIPAddressAndPort(m_TCPAddress, m_TCPPort));
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        var host = input; var port = m_TCPPort;
                        if (input.Contains(":")) { var parts = input.Split(':'); host = parts[0]; int.TryParse(parts[1], out port); }
                        m_TCPAddress = host; m_TCPPort = port;
                    }
                    break;
                case "Multi-LAN":
                    MessageBox.Show("Multi-LAN not included in minimal app.");
                    break;
            }
        }

        // The ARGOX SDK's USBDialog (BarcodePrinter_API.dll) hosts a private WinForms form whose
        // device ListView is left at the WinForms default MultiSelect = true, and the form
        // pre-selects the first device on load. With multi-select that initial selection can stick
        // when the user clicks a different printer, so the dialog's OK handler returns
        // SelectedItems[0] (the first printer) instead of the one actually chosen. The vendor
        // dialog is sealed, so reach its inner ListView via reflection and switch it to
        // single-select; then clicking a row always replaces the selection. Fails safe (leaves the
        // dialog unchanged) if a future SDK build renames these private members.
        private static void ForceUsbDialogSingleSelect(USBDialog dialog)
        {
            try
            {
                var formField = typeof(USBDialog).GetField("_USBform",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (!(formField?.GetValue(dialog) is Control innerForm)) return;

                var listViewField = innerForm.GetType().GetField("listView_DeviceInfo",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (listViewField?.GetValue(innerForm) is ListView deviceList)
                {
                    deviceList.MultiSelect = false;
                }
            }
            catch
            {
                // Vendor field names changed - leave the dialog as-is rather than break config.
            }
        }

        #endregion
    }

    public class ConfigPopupForm : Form
    {
        private Panel configPanel;
        private Panel actionPanel;

        public ConfigPopupForm(Panel configPanelRef, Panel actionPanelRef)
        {
            this.configPanel = configPanelRef;
            this.actionPanel = actionPanelRef;
            InitializePopup();
        }

        private void InitializePopup()
        {
            this.Text = "Bar Codes";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(900, 420); // Reduced width and height to fit components
            this.MinimumSize = new Size(900, 420);
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.ShowInTaskbar = false;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.Owner = Application.OpenForms["Form1"];

            // Create container panel (full size since no close button)
            Panel containerPanel = new Panel();
            containerPanel.Dock = DockStyle.Fill;
            containerPanel.Location = new Point(0, 0);

            // Add panels with explicit layout (configPanel at top, actionPanel below)
            configPanel.Dock = DockStyle.None; // Remove docking
            configPanel.AutoSize = false; // Disable auto-sizing for fixed layout
            configPanel.Location = new Point(0, 0);
            configPanel.Size = new Size(containerPanel.Width, 250);
            configPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            configPanel.Visible = true;
            configPanel.Padding = new Padding(20, 10, 20, 10); // Tighter padding
            containerPanel.Controls.Add(configPanel);

            actionPanel.Dock = DockStyle.None; // Remove docking
            actionPanel.AutoSize = false; // Disable auto-sizing for fixed layout
            actionPanel.Location = new Point(0, 250); // Position just below configPanel
            actionPanel.Size = new Size(containerPanel.Width, 110); // Increased height to accommodate content
            actionPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            actionPanel.Visible = true;
            actionPanel.Padding = new Padding(20, 10, 20, 10); // Consistent padding with configPanel
            containerPanel.Controls.Add(actionPanel);

            this.Controls.Add(containerPanel);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // If the owner form is closing, allow this popup to actually close
            if (this.Owner != null && !this.Owner.Visible)
            {
                // Owner is closing, allow this form to close too
                return;
            }

            // Otherwise, hide instead of close to keep in memory
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();

                // Re-enable the button
                if (this.Owner is Form1 mainForm)
                {
                    mainForm.EnableBarCodesButton();
                }
            }
        }
    }

    public class BoxLabelsPopupForm : Form
    {
        private Panel advancedPanel;
        private Form1 mainForm;

        public BoxLabelsPopupForm(Panel advancedPanelRef, Form1 mainFormRef)
        {
            this.advancedPanel = advancedPanelRef;
            this.mainForm = mainFormRef;
            InitializePopup();
        }

        private void InitializePopup()
        {
            this.Text = "Box Labels";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(1400, 550); // Adjusted height to fit only advancedPanel
            this.MinimumSize = new Size(900, 500);
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.ShowInTaskbar = false;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.Owner = Application.OpenForms["Form1"];

            // Add advancedPanel directly (full size)
            advancedPanel.Dock = DockStyle.Fill;
            advancedPanel.Location = new Point(0, 0);
            advancedPanel.Visible = true;
            advancedPanel.Padding = new Padding(50, 20, 50, 20); // Restore original padding with bottom padding
            this.Controls.Add(advancedPanel);

            // Force update picture box visibility after popup is fully set up
            this.Shown += (s, e) => {
                if (mainForm != null) {
                    mainForm.UpdatePictureBoxVisibility();
                }
            };
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // If the owner form is closing, allow this popup to actually close
            if (this.Owner != null && !this.Owner.Visible)
            {
                // Owner is closing, allow this form to close too
                return;
            }

            // Otherwise, hide instead of close to keep in memory
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();

                // Re-enable the button
                if (this.Owner is Form1 mainForm)
                {
                    mainForm.EnableBoxLabelsButton();
                }
            }
        }
    }
}


