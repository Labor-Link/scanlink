using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScanLink
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) 
            { 
                components.Dispose(); 
                _apiAuthService?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.loginPanel = new System.Windows.Forms.Panel();
            this.loginWelcomeLabel = new System.Windows.Forms.Label();
            this.loginMainLogoPictureBox = new System.Windows.Forms.PictureBox();
            this.startpanelLogoPictureBox = new System.Windows.Forms.PictureBox();
            this.loginGroupBox = new System.Windows.Forms.GroupBox();
            this.loginButton = new System.Windows.Forms.Button();
            this.passwordToggleButton = new System.Windows.Forms.Button();
            this.passwordTextBox = new System.Windows.Forms.TextBox();
            this.passwordLabel = new System.Windows.Forms.Label();
            this.usernameTextBox = new System.Windows.Forms.TextBox();
            this.usernameLabel = new System.Windows.Forms.Label();
            this.loginStatusLabel = new System.Windows.Forms.Label();
            this.startPanel = new System.Windows.Forms.Panel();
            this.welcomeLabel = new System.Windows.Forms.Label();
            this.logoutButtonStart = new System.Windows.Forms.Button();
            this.logoPictureBox = new System.Windows.Forms.PictureBox();
            this.printerButton = new System.Windows.Forms.Button();
            this.scannerButton = new System.Windows.Forms.Button();
            this.printerContentPanel = new System.Windows.Forms.Panel();
			this.scannerContentPanel = new System.Windows.Forms.Panel();
			this.button_manualUpload = new System.Windows.Forms.Button();
            this.scannerDataGridView = new System.Windows.Forms.DataGridView();
            this.scannerOutputTextBox = new System.Windows.Forms.TextBox();
            this.showScannerOutputCheckBox = new System.Windows.Forms.CheckBox();
            this.previousPageButton = new System.Windows.Forms.Button();
            this.nextPageButton = new System.Windows.Forms.Button();
            this.pageInfoLabel = new System.Windows.Forms.Label();
            this.dateFromPicker = new System.Windows.Forms.DateTimePicker();
            this.dateToPicker = new System.Windows.Forms.DateTimePicker();
            this.blockNumberTextBox = new System.Windows.Forms.TextBox();
            this.lineNumberTextBox = new System.Windows.Forms.TextBox();
            this.productIdComboBox = new System.Windows.Forms.ComboBox();
            this.applyFiltersButton = new System.Windows.Forms.Button();
            this.clearFiltersButton = new System.Windows.Forms.Button();
            this.dateFromLabel = new System.Windows.Forms.Label();
            this.dateToLabel = new System.Windows.Forms.Label();
            this.blockNumberLabel = new System.Windows.Forms.Label();
            this.lineNumberLabel = new System.Windows.Forms.Label();
            this.productIdLabel = new System.Windows.Forms.Label();
            this.activeScannersLabel = new System.Windows.Forms.Label();
            this.todayScansLabel = new System.Windows.Forms.Label();
            this.lastHourScansLabel = new System.Windows.Forms.Label();
            // this.runScannerScriptButton = new System.Windows.Forms.Button();
            this.manageScannersButton = new System.Windows.Forms.Button();
            this.mainPanel = new System.Windows.Forms.Panel();
            this.headerPanel = new System.Windows.Forms.Panel();
            this.titleLabel = new System.Windows.Forms.Label();
            this.subtitleLabel = new System.Windows.Forms.Label();
            this.connectionPanel = new System.Windows.Forms.Panel();
            this.connectionGroupBox = new System.Windows.Forms.GroupBox();
            this.connectionStatusLabel = new System.Windows.Forms.Label();
            this.label_port = new System.Windows.Forms.Label();
            this.comboBox_port = new System.Windows.Forms.ComboBox();
            this.button_setting = new System.Windows.Forms.Button();
            this.textBox_port = new System.Windows.Forms.TextBox();
            this.configPanel = new System.Windows.Forms.Panel();
            this.configGroupBox = new System.Windows.Forms.GroupBox();
            this.barcodeTextPanel = new System.Windows.Forms.Panel();
            this.label_EmployeeID = new System.Windows.Forms.Label();
            this.textBox_EmployeeID = new System.Windows.Forms.TextBox();
            this.button_FetchEmployees = new System.Windows.Forms.Button();
            this.label_ProductID = new System.Windows.Forms.Label();
            this.comboBox_ProductID = new System.Windows.Forms.ComboBox();
            this.label_count = new System.Windows.Forms.Label();
            this.numericUpDown_count = new System.Windows.Forms.NumericUpDown();
            this.advancedPanel = new System.Windows.Forms.Panel();
            this.advancedGroupBox = new System.Windows.Forms.GroupBox();
            this.printerConfigPanel = new System.Windows.Forms.Panel();
            this.label_emulation = new System.Windows.Forms.Label();
            this.comboBox_emulation = new System.Windows.Forms.ComboBox();
            this.label_test = new System.Windows.Forms.Label();
            this.comboBox_test = new System.Windows.Forms.ComboBox();
            this.label_barcode = new System.Windows.Forms.Label();
            this.comboBox_barcode = new System.Windows.Forms.ComboBox();
            this.dimensionsPanel = new System.Windows.Forms.Panel();
            this.label_width = new System.Windows.Forms.Label();
            this.numericUpDown_width = new System.Windows.Forms.NumericUpDown();
            this.label_height = new System.Windows.Forms.Label();
            this.numericUpDown_height = new System.Windows.Forms.NumericUpDown();
            this.label_gap = new System.Windows.Forms.Label();
            this.numericUpDown_gap = new System.Windows.Forms.NumericUpDown();
            this.alignmentPanel = new System.Windows.Forms.Panel();
            this.label_alignment = new System.Windows.Forms.Label();
            this.comboBox_alignment = new System.Windows.Forms.ComboBox();
            this.label_rotation = new System.Windows.Forms.Label();
            this.comboBox_rotation = new System.Windows.Forms.ComboBox();
            this.qualityPanel = new System.Windows.Forms.Panel();
            this.label_darkness = new System.Windows.Forms.Label();
            this.trackBar_darkness = new System.Windows.Forms.TrackBar();
            this.label_darknessValue = new System.Windows.Forms.Label();
            this.label_speed = new System.Windows.Forms.Label();
            this.comboBox_speed = new System.Windows.Forms.ComboBox();
            this.previewPanel = new System.Windows.Forms.Panel();
            this.button_preview = new System.Windows.Forms.Button();
            this.checkBox_showAdvanced = new System.Windows.Forms.CheckBox();
            this.actionPanel = new System.Windows.Forms.Panel();
            this.button_send = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.statusPanel = new System.Windows.Forms.Panel();
            this.statusLabel = new System.Windows.Forms.Label();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.loginPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.loginMainLogoPictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.startpanelLogoPictureBox)).BeginInit();
            this.loginGroupBox.SuspendLayout();
            this.startPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).BeginInit();
            this.printerContentPanel.SuspendLayout();
            this.scannerContentPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scannerDataGridView)).BeginInit();
            this.mainPanel.SuspendLayout();
            this.headerPanel.SuspendLayout();
            this.connectionPanel.SuspendLayout();
            this.connectionGroupBox.SuspendLayout();
            this.configPanel.SuspendLayout();
            this.configGroupBox.SuspendLayout();
            this.barcodeTextPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_count)).BeginInit();
            this.advancedPanel.SuspendLayout();
            this.advancedGroupBox.SuspendLayout();
            this.printerConfigPanel.SuspendLayout();
            this.dimensionsPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_width)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_height)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_gap)).BeginInit();
            this.alignmentPanel.SuspendLayout();
            this.qualityPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_darkness)).BeginInit();
            this.previewPanel.SuspendLayout();
            this.actionPanel.SuspendLayout();
            this.statusPanel.SuspendLayout();
            this.SuspendLayout();
            
            // 
            // loginPanel
            // 
            this.loginPanel.AutoScroll = true;
            this.loginPanel.AutoScrollMinSize = new System.Drawing.Size(0, 800);
            this.loginPanel.BackColor = System.Drawing.Color.White;
            this.loginPanel.Controls.Add(this.loginStatusLabel);
            this.loginPanel.Controls.Add(this.loginGroupBox);
            this.loginPanel.Controls.Add(this.loginMainLogoPictureBox);
            // this.loginPanel.Controls.Add(this.loginWelcomeLabel);
            this.loginPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.loginPanel.Location = new System.Drawing.Point(0, 0);
            this.loginPanel.Name = "loginPanel";
            this.loginPanel.Size = new System.Drawing.Size(600, 1120);
            this.loginPanel.TabIndex = 0;
            this.loginPanel.Visible = true;
            
            // 
            // loginWelcomeLabel
            // 
            this.loginWelcomeLabel.AutoSize = true;
            this.loginWelcomeLabel.Font = new System.Drawing.Font("Segoe UI", 28F, System.Drawing.FontStyle.Bold);
            this.loginWelcomeLabel.ForeColor = System.Drawing.Color.FromArgb(27, 42, 65);
            this.loginWelcomeLabel.Location = new System.Drawing.Point(100, 30);
            this.loginWelcomeLabel.Name = "loginWelcomeLabel";
            this.loginWelcomeLabel.Size = new System.Drawing.Size(400, 51);
            this.loginWelcomeLabel.TabIndex = 0;
            this.loginWelcomeLabel.Text = "Welcome to ScanLink";
            this.loginWelcomeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            
            // 
            // loginMainLogoPictureBox
            // 
            // this.loginMainLogoPictureBox.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            // this.loginMainLogoPictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.loginMainLogoPictureBox.Location = new System.Drawing.Point(50, 90);
            this.loginMainLogoPictureBox.Name = "loginMainLogoPictureBox";
            this.loginMainLogoPictureBox.Size = new System.Drawing.Size(500, 120);
            this.loginMainLogoPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.loginMainLogoPictureBox.TabIndex = 1;
            this.loginMainLogoPictureBox.TabStop = false;

            // 
            // startpanelLogoPictureBox
            // 
            this.startpanelLogoPictureBox.Location = new System.Drawing.Point(50, 80);
            this.startpanelLogoPictureBox.Name = "startpanelLogoPictureBox";
            this.startpanelLogoPictureBox.Size = new System.Drawing.Size(400, 100);
            this.startpanelLogoPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.startpanelLogoPictureBox.TabIndex = 1;
            this.startpanelLogoPictureBox.TabStop = false;
            
            
            // 
            // loginGroupBox
            // 
            this.loginGroupBox.BackColor = System.Drawing.Color.White;
            this.loginGroupBox.Controls.Add(this.loginButton);
            this.loginGroupBox.Controls.Add(this.passwordToggleButton);
            this.loginGroupBox.Controls.Add(this.passwordTextBox);
            this.loginGroupBox.Controls.Add(this.passwordLabel);
            this.loginGroupBox.Controls.Add(this.usernameTextBox);
            this.loginGroupBox.Controls.Add(this.usernameLabel);
            this.loginGroupBox.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.loginGroupBox.ForeColor = System.Drawing.Color.FromArgb(27, 42, 65);
            this.loginGroupBox.Location = new System.Drawing.Point(100, 400);
            this.loginGroupBox.Name = "loginGroupBox";
            this.loginGroupBox.Size = new System.Drawing.Size(400, 280);
            this.loginGroupBox.TabIndex = 2;
            this.loginGroupBox.TabStop = false;
            this.loginGroupBox.Text = "Login";
            
            // 
            // usernameLabel
            // 
            this.usernameLabel.AutoSize = true;
            this.usernameLabel.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.usernameLabel.ForeColor = System.Drawing.Color.Black;
            this.usernameLabel.Location = new System.Drawing.Point(30, 50);
            this.usernameLabel.Name = "usernameLabel";
            this.usernameLabel.Size = new System.Drawing.Size(125, 23);
            this.usernameLabel.TabIndex = 0;
            this.usernameLabel.Text = "Email*";
            
            // 
            // usernameTextBox
            // 
            this.usernameTextBox.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.usernameTextBox.Location = new System.Drawing.Point(30, 80);
            this.usernameTextBox.Name = "usernameTextBox";
            this.usernameTextBox.Size = new System.Drawing.Size(340, 32);
            this.usernameTextBox.TabIndex = 1;
            this.usernameTextBox.Text = "";
            this.usernameTextBox.ForeColor = System.Drawing.Color.Gray;
            this.usernameTextBox.Enter += new System.EventHandler(this.usernameTextBox_Enter);
            this.usernameTextBox.Leave += new System.EventHandler(this.usernameTextBox_Leave);
            this.usernameTextBox.TextChanged += new System.EventHandler(this.usernameTextBox_TextChanged);
            
            // 
            // passwordLabel
            // 
            this.passwordLabel.AutoSize = true;
            this.passwordLabel.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.passwordLabel.ForeColor = System.Drawing.Color.Black;
            this.passwordLabel.Location = new System.Drawing.Point(30, 130);
            this.passwordLabel.Name = "passwordLabel";
            this.passwordLabel.Size = new System.Drawing.Size(77, 23);
            this.passwordLabel.TabIndex = 2;
            this.passwordLabel.Text = "Password*";
            
            // 
            // passwordTextBox
            // 
            this.passwordTextBox.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.passwordTextBox.Location = new System.Drawing.Point(30, 160);
            this.passwordTextBox.Name = "passwordTextBox";
            this.passwordTextBox.PasswordChar = '\0';
            this.passwordTextBox.Size = new System.Drawing.Size(340, 32);
            this.passwordTextBox.TabIndex = 3;
            this.passwordTextBox.Text = "";
            this.passwordTextBox.ForeColor = System.Drawing.Color.Gray;
            this.passwordTextBox.Enter += new System.EventHandler(this.passwordTextBox_Enter);
            this.passwordTextBox.Leave += new System.EventHandler(this.passwordTextBox_Leave);
            this.passwordTextBox.TextChanged += new System.EventHandler(this.passwordTextBox_TextChanged);
            
            // 
            // passwordToggleButton
            // 
            this.passwordToggleButton.BackColor = System.Drawing.Color.Transparent;
            this.passwordToggleButton.FlatAppearance.BorderSize = 0;
            this.passwordToggleButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(225, 225, 225);
            this.passwordToggleButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.passwordToggleButton.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.passwordToggleButton.ForeColor = System.Drawing.Color.FromArgb(0, 0, 0);
            this.passwordToggleButton.Location = new System.Drawing.Point(344, 161);
            this.passwordToggleButton.Name = "passwordToggleButton";
            this.passwordToggleButton.Size = new System.Drawing.Size(25, 21);
            this.passwordToggleButton.TabIndex = 4;
            this.passwordToggleButton.Text = "👁️";
            this.passwordToggleButton.UseVisualStyleBackColor = false;
            this.passwordToggleButton.Click += new System.EventHandler(this.passwordToggleButton_Click);
            
            // 
            // loginButton
            // 
            this.loginButton.BackColor = System.Drawing.Color.FromArgb(50, 74, 95);
            this.loginButton.FlatAppearance.BorderSize = 0;
            this.loginButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.loginButton.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.loginButton.ForeColor = System.Drawing.Color.White;
            this.loginButton.Location = new System.Drawing.Point(30, 210);
            this.loginButton.Name = "loginButton";
            this.loginButton.Size = new System.Drawing.Size(340, 36);
            this.loginButton.TabIndex = 4;
            this.loginButton.Text = "Login";
            this.loginButton.UseVisualStyleBackColor = false;
            this.loginButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(27, 42, 65);
            this.loginButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(27, 42, 65);
            this.loginButton.Click += new System.EventHandler(this.loginButton_Click);
            
            // 
            // loginStatusLabel
            // 
            this.loginStatusLabel.AutoSize = true;
            this.loginStatusLabel.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.loginStatusLabel.ForeColor = System.Drawing.Color.Black;
            this.loginStatusLabel.Location = new System.Drawing.Point(100, 690);
            this.loginStatusLabel.Name = "loginStatusLabel";
            this.loginStatusLabel.Size = new System.Drawing.Size(119, 19);
            this.loginStatusLabel.TabIndex = 3;
            this.loginStatusLabel.Text = "Please Login to access your account";
            // 
            // logoutButtonStart
            // 
            this.logoutButtonStart = new System.Windows.Forms.Button();
            this.logoutButtonStart.BackColor = System.Drawing.Color.FromArgb(231, 76, 60);
            this.logoutButtonStart.FlatAppearance.BorderSize = 0;
            this.logoutButtonStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.logoutButtonStart.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.logoutButtonStart.ForeColor = System.Drawing.Color.White;
            this.logoutButtonStart.Location = new System.Drawing.Point(0,0);
            this.logoutButtonStart.Name = "logoutButtonStart";
            this.logoutButtonStart.Size = new System.Drawing.Size(70, 27);
            this.logoutButtonStart.TabIndex = 5;
            this.logoutButtonStart.Text = "Logout";
            this.logoutButtonStart.UseVisualStyleBackColor = false;
            this.logoutButtonStart.Visible = true;
            this.logoutButtonStart.Click += new System.EventHandler(this.logoutButton_Click);
            

            // 
            // startPanel
            // 
            this.startPanel.AutoScroll = true;
            this.startPanel.AutoScrollMinSize = new System.Drawing.Size(0, 800);
            this.startPanel.BackColor = System.Drawing.Color.White;
            this.startPanel.Controls.Add(this.scannerButton);
            this.startPanel.Controls.Add(this.printerButton);
            this.startPanel.Controls.Add(this.startpanelLogoPictureBox);
            this.startPanel.Controls.Add(this.welcomeLabel);
            this.startPanel.Controls.Add(this.logoutButtonStart);
            this.startPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.startPanel.Location = new System.Drawing.Point(0, 0);
            this.startPanel.Name = "startPanel";
            this.startPanel.Size = new System.Drawing.Size(600, 1120);
            this.startPanel.TabIndex = 0;


            
            // 
            // welcomeLabel
            // 
            this.welcomeLabel.AutoSize = true;
            this.welcomeLabel.Font = new System.Drawing.Font("Segoe UI", 28F, System.Drawing.FontStyle.Bold);
            this.welcomeLabel.ForeColor = System.Drawing.Color.FromArgb(27, 42, 65);
            this.welcomeLabel.Location = new System.Drawing.Point(100, 210);
            this.welcomeLabel.Name = "welcomeLabel";
            this.welcomeLabel.Size = new System.Drawing.Size(400, 51);
            this.welcomeLabel.TabIndex = 2;
            this.welcomeLabel.Text = "Welcome to ScanLink";
            this.welcomeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.welcomeLabel.Visible = true;
            
            
            
            // 
            // logoPictureBox
            // 
            this.logoPictureBox.Location = new System.Drawing.Point(175, 100);
            this.logoPictureBox.Name = "logoPictureBox";
            this.logoPictureBox.Size = new System.Drawing.Size(250, 250);
            this.logoPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.logoPictureBox.TabIndex = 3;
            this.logoPictureBox.TabStop = false;
            
            // 
            // printerButton
            // 
            this.printerButton.BackColor = System.Drawing.Color.FromArgb(50, 74, 95);
            this.printerButton.FlatAppearance.BorderSize = 0;
            this.printerButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.printerButton.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.printerButton.ForeColor = System.Drawing.Color.White;
            this.printerButton.Location = new System.Drawing.Point(150, 380);
            this.printerButton.Name = "printerButton";
            this.printerButton.Size = new System.Drawing.Size(300, 48);
            this.printerButton.TabIndex = 0;
            this.printerButton.Text = "🖨️ Printer";
            this.printerButton.UseVisualStyleBackColor = false;
            this.printerButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(27, 42, 65);
            this.printerButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(0, 0, 0);
            this.printerButton.Click += new System.EventHandler(this.printerButton_Click);
            
            // 
            // scannerButton
            // 
            this.scannerButton.BackColor = System.Drawing.Color.FromArgb(50, 74, 95);
            this.scannerButton.FlatAppearance.BorderSize = 0;
            this.scannerButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.scannerButton.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.scannerButton.ForeColor = System.Drawing.Color.White;
            this.scannerButton.Location = new System.Drawing.Point(150, 460);
            this.scannerButton.Name = "scannerButton";
            this.scannerButton.Size = new System.Drawing.Size(300, 48);
            this.scannerButton.TabIndex = 1;
            this.scannerButton.Text = "📷 Scanner";
            this.scannerButton.UseVisualStyleBackColor = false;
            this.scannerButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(27, 42, 65);
            this.scannerButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(0, 0, 0);
            this.scannerButton.Click += new System.EventHandler(this.scannerButton_Click);
            // 
            // Scanner
            //  
            this.Scanner = new System.Windows.Forms.Button();
            this.Scanner.Location = new System.Drawing.Point(475, 50);
            this.Scanner.Name = "Scanner";
            this.Scanner.Size = new System.Drawing.Size(90, 32);
            this.Scanner.TabIndex = 2;
            this.Scanner.Text = "Scanner";
            this.Scanner.UseVisualStyleBackColor = true;
            this.Scanner.Click += new System.EventHandler(this.Scanner_Click);
            
            
            // 
            // Printer
            // 
            this.Printer = new System.Windows.Forms.Button();
            this.Printer.Location = new System.Drawing.Point(475, 20);
            this.Printer.Name = "Printer";
            this.Printer.Size = new System.Drawing.Size(90, 32);
            this.Printer.TabIndex = 2;
            this.Printer.Text = "Printer";
            this.Printer.UseVisualStyleBackColor = true;
            this.Printer.Click += new System.EventHandler(this.Printer_Click);
            
            

            // 
            // logoutButtonPrinter
            // 
            this.logoutButtonPrinter = new System.Windows.Forms.Button();
            this.logoutButtonPrinter.BackColor = System.Drawing.Color.FromArgb(231, 76, 60);
            this.logoutButtonPrinter.FlatAppearance.BorderSize = 0;
            this.logoutButtonPrinter.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.logoutButtonPrinter.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.logoutButtonPrinter.ForeColor = System.Drawing.Color.White;
            this.logoutButtonPrinter.Location = new System.Drawing.Point(390, 35);
            this.logoutButtonPrinter.Name = "logoutButtonPrinter";
            this.logoutButtonPrinter.Size = new System.Drawing.Size(90, 32);
            this.logoutButtonPrinter.TabIndex = 5;
            this.logoutButtonPrinter.Text = "Logout";
            this.logoutButtonPrinter.UseVisualStyleBackColor = false;
            this.logoutButtonPrinter.Click += new System.EventHandler(this.logoutButton_Click);
            
            // 
            // logoutButtonScanner
            // 
            this.logoutButtonScanner = new System.Windows.Forms.Button();
            this.logoutButtonScanner.BackColor = System.Drawing.Color.FromArgb(231, 76, 60);
            this.logoutButtonScanner.FlatAppearance.BorderSize = 0;
            this.logoutButtonScanner.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.logoutButtonScanner.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.logoutButtonScanner.ForeColor = System.Drawing.Color.White;
            this.logoutButtonScanner.Location = new System.Drawing.Point(400, 20);
            this.logoutButtonScanner.Name = "logoutButtonScanner";
            this.logoutButtonScanner.Size = new System.Drawing.Size(90, 32);
            this.logoutButtonScanner.TabIndex = 3;
            this.logoutButtonScanner.Text = "Logout";
            this.logoutButtonScanner.UseVisualStyleBackColor = false;
            this.logoutButtonScanner.Click += new System.EventHandler(this.logoutButton_Click);
            
            // 
            // barcodeInputTextBox
            // 
            // this.barcodeInputTextBox = new System.Windows.Forms.TextBox();
            // this.barcodeInputTextBox.Location = new System.Drawing.Point(50, 520);
            // this.barcodeInputTextBox.Name = "barcodeInputTextBox";
            // this.barcodeInputTextBox.Size = new System.Drawing.Size(250, 23);
            // this.barcodeInputTextBox.TabIndex = 2;
            // this.barcodeInputTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.barcodeInputTextBox_KeyDown);
            
            // 
            // sendBarcodeButton
            // 
            // this.sendBarcodeButton = new System.Windows.Forms.Button();
            // this.sendBarcodeButton.Location = new System.Drawing.Point(310, 520);
            // this.sendBarcodeButton.Name = "sendBarcodeButton";
            // this.sendBarcodeButton.Size = new System.Drawing.Size(120, 23);
            // this.sendBarcodeButton.TabIndex = 3;
            // this.sendBarcodeButton.Text = "Send Barcode";
            // this.sendBarcodeButton.UseVisualStyleBackColor = true;
            // this.sendBarcodeButton.Click += new System.EventHandler(this.sendBarcodeButton_Click);
            
            // 
            // printerContentPanel
            // 
            this.printerContentPanel.AutoScroll = true;
            this.printerContentPanel.AutoScrollMinSize = new System.Drawing.Size(0, 1200);
            this.printerContentPanel.Controls.Add(this.Scanner);
            this.printerContentPanel.Controls.Add(this.mainPanel);
            this.printerContentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.printerContentPanel.Location = new System.Drawing.Point(0, 0);
            this.printerContentPanel.Name = "printerContentPanel";
            this.printerContentPanel.Size = new System.Drawing.Size(600, 1120);
            this.printerContentPanel.TabIndex = 1;
            this.printerContentPanel.Visible = false;
            
            // 
            // scannerContentPanel
            // 
            // this.scannerContentPanel.Controls.Add(this.sendBarcodeButton);
            // this.scannerContentPanel.Controls.Add(this.barcodeInputTextBox);
            this.scannerContentPanel.AutoScroll = true;
            // this.scannerContentPanel.AutoScrollMinSize = new System.Drawing.Size(0, 800);
			this.scannerContentPanel.Controls.Add(this.Printer);
            this.scannerContentPanel.Controls.Add(this.logoutButtonScanner);
            // this.scannerContentPanel.Controls.Add(this.runScannerScriptButton);
			this.scannerContentPanel.Controls.Add(this.button_manualUpload);
            this.scannerContentPanel.Controls.Add(this.scannerDataGridView);
            this.scannerContentPanel.Controls.Add(this.scannerOutputTextBox);
            this.scannerContentPanel.Controls.Add(this.showScannerOutputCheckBox);
            this.scannerContentPanel.Controls.Add(this.dateFromLabel);
            this.scannerContentPanel.Controls.Add(this.dateFromPicker);
            this.scannerContentPanel.Controls.Add(this.dateToLabel);
            this.scannerContentPanel.Controls.Add(this.dateToPicker);
            this.scannerContentPanel.Controls.Add(this.blockNumberLabel);
            this.scannerContentPanel.Controls.Add(this.blockNumberTextBox);
            this.scannerContentPanel.Controls.Add(this.lineNumberLabel);
            this.scannerContentPanel.Controls.Add(this.lineNumberTextBox);
            this.scannerContentPanel.Controls.Add(this.productIdLabel);
            this.scannerContentPanel.Controls.Add(this.productIdComboBox);
            this.scannerContentPanel.Controls.Add(this.applyFiltersButton);
            this.scannerContentPanel.Controls.Add(this.clearFiltersButton);
            this.scannerContentPanel.Controls.Add(this.activeScannersLabel);
            this.scannerContentPanel.Controls.Add(this.todayScansLabel);
            this.scannerContentPanel.Controls.Add(this.lastHourScansLabel);
            this.scannerContentPanel.Controls.Add(this.previousPageButton);
            this.scannerContentPanel.Controls.Add(this.nextPageButton);
            this.scannerContentPanel.Controls.Add(this.pageInfoLabel);
            this.scannerContentPanel.Controls.Add(this.manageScannersButton);
            this.scannerContentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scannerContentPanel.Location = new System.Drawing.Point(0, 0);
            this.scannerContentPanel.Name = "scannerContentPanel";
            this.scannerContentPanel.Size = new System.Drawing.Size(600, 680);
            this.scannerContentPanel.TabIndex = 2;
            this.scannerContentPanel.Visible = false;
			// 
			// button_manualUpload
			// 
			this.button_manualUpload.Location = new System.Drawing.Point(170, 20);
			this.button_manualUpload.Name = "button_manualUpload";
			this.button_manualUpload.Size = new System.Drawing.Size(130, 32);
			this.button_manualUpload.TabIndex = 4;
			this.button_manualUpload.Text = "Sync logs to API";
			this.button_manualUpload.UseVisualStyleBackColor = true;
			this.button_manualUpload.Click += new System.EventHandler(this.button_manualUpload_Click);
			this.button_manualUpload.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
			// 
            
            // 
            // scannerDataGridView
            // 
            // this.scannerDataGridView.AutoScroll = true;
            this.scannerDataGridView.AllowUserToAddRows = false;
            this.scannerDataGridView.AllowUserToDeleteRows = false;
            this.scannerDataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.scannerDataGridView.BackgroundColor = System.Drawing.Color.White;
            this.scannerDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.scannerDataGridView.Location = new System.Drawing.Point(20, 220);
            this.scannerDataGridView.Name = "scannerDataGridView";
            this.scannerDataGridView.ReadOnly = true;
            this.scannerDataGridView.RowHeadersVisible = false;
            this.scannerDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.scannerDataGridView.Size = new System.Drawing.Size(560, 460);
            this.scannerDataGridView.TabIndex = 0;
            
            // 
            // scannerOutputTextBox
            // 
            this.scannerOutputTextBox.Location = new System.Drawing.Point(20, 50);
            this.scannerOutputTextBox.Multiline = true;
            this.scannerOutputTextBox.Name = "scannerOutputTextBox";
            this.scannerOutputTextBox.ReadOnly = true;
            this.scannerOutputTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.scannerOutputTextBox.Size = new System.Drawing.Size(560, 85);
            this.scannerOutputTextBox.TabIndex = 1;
            this.scannerOutputTextBox.Visible = false;
            
            // 
            // showScannerOutputCheckBox
            // 
            this.showScannerOutputCheckBox.AutoSize = true;
            this.showScannerOutputCheckBox.Checked = false;
            this.showScannerOutputCheckBox.CheckState = System.Windows.Forms.CheckState.Unchecked;
            this.showScannerOutputCheckBox.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.showScannerOutputCheckBox.Location = new System.Drawing.Point(20, 20);
            this.showScannerOutputCheckBox.Name = "showScannerOutputCheckBox";
            this.showScannerOutputCheckBox.Size = new System.Drawing.Size(150, 23);
            this.showScannerOutputCheckBox.TabIndex = 0;
            this.showScannerOutputCheckBox.Text = "Show Scanner Output Textbox";
            this.showScannerOutputCheckBox.UseVisualStyleBackColor = true;
            this.showScannerOutputCheckBox.CheckedChanged += new System.EventHandler(this.showScannerOutputCheckBox_CheckedChanged);
            
            // 
            // previousPageButton
            // 
            this.previousPageButton.BackColor = System.Drawing.Color.FromArgb(50, 74, 95);
            this.previousPageButton.FlatAppearance.BorderSize = 0;
            this.previousPageButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.previousPageButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.previousPageButton.ForeColor = System.Drawing.Color.White;
            this.previousPageButton.Location = new System.Drawing.Point(20, 500);
            this.previousPageButton.Name = "previousPageButton";
            this.previousPageButton.Size = new System.Drawing.Size(80, 30);
            this.previousPageButton.TabIndex = 5;
            this.previousPageButton.Text = "Previous";
            this.previousPageButton.UseVisualStyleBackColor = false;
            this.previousPageButton.Click += new System.EventHandler(this.previousPageButton_Click);
            
            // 
            // nextPageButton
            // 
            this.nextPageButton.BackColor = System.Drawing.Color.FromArgb(50, 74, 95);
            this.nextPageButton.FlatAppearance.BorderSize = 0;
            this.nextPageButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.nextPageButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.nextPageButton.ForeColor = System.Drawing.Color.White;
            this.nextPageButton.Location = new System.Drawing.Point(500, 500);
            this.nextPageButton.Name = "nextPageButton";
            this.nextPageButton.Size = new System.Drawing.Size(80, 30);
            this.nextPageButton.TabIndex = 6;
            this.nextPageButton.Text = "Next";
            this.nextPageButton.UseVisualStyleBackColor = false;
            this.nextPageButton.Click += new System.EventHandler(this.nextPageButton_Click);
            
            // 
            // pageInfoLabel
            // 
            this.pageInfoLabel.AutoSize = true;
            this.pageInfoLabel.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.pageInfoLabel.ForeColor = System.Drawing.Color.Black;
            this.pageInfoLabel.Location = new System.Drawing.Point(250, 505);
            this.pageInfoLabel.Name = "pageInfoLabel";
            this.pageInfoLabel.Size = new System.Drawing.Size(100, 19);
            this.pageInfoLabel.TabIndex = 7;
            this.pageInfoLabel.Text = "Page 1 of 1";
            this.pageInfoLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            
            // 
            // dateFromLabel
            // 
            this.dateFromLabel.AutoSize = true;
            this.dateFromLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.dateFromLabel.ForeColor = System.Drawing.Color.Black;
            this.dateFromLabel.Location = new System.Drawing.Point(20, 230);
            this.dateFromLabel.Name = "dateFromLabel";
            this.dateFromLabel.Size = new System.Drawing.Size(35, 15);
            this.dateFromLabel.TabIndex = 8;
            this.dateFromLabel.Text = "From:";
            
            // 
            // dateFromPicker
            // 
            this.dateFromPicker.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.dateFromPicker.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateFromPicker.Location = new System.Drawing.Point(20, 250);
            this.dateFromPicker.Name = "dateFromPicker";
            this.dateFromPicker.Size = new System.Drawing.Size(120, 23);
            this.dateFromPicker.TabIndex = 9;
            
            // 
            // dateToLabel
            // 
            this.dateToLabel.AutoSize = true;
            this.dateToLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.dateToLabel.ForeColor = System.Drawing.Color.Black;
            this.dateToLabel.Location = new System.Drawing.Point(160, 230);
            this.dateToLabel.Name = "dateToLabel";
            this.dateToLabel.Size = new System.Drawing.Size(22, 15);
            this.dateToLabel.TabIndex = 10;
            this.dateToLabel.Text = "To:";
            
            // 
            // dateToPicker
            // 
            this.dateToPicker.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.dateToPicker.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateToPicker.Location = new System.Drawing.Point(160, 250);
            this.dateToPicker.Name = "dateToPicker";
            this.dateToPicker.Size = new System.Drawing.Size(120, 23);
            this.dateToPicker.TabIndex = 11;
            
            // 
            // blockNumberLabel
            // 
            this.blockNumberLabel.AutoSize = true;
            this.blockNumberLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.blockNumberLabel.ForeColor = System.Drawing.Color.Black;
            this.blockNumberLabel.Location = new System.Drawing.Point(300, 230);
            this.blockNumberLabel.Name = "blockNumberLabel";
            this.blockNumberLabel.Size = new System.Drawing.Size(80, 15);
            this.blockNumberLabel.TabIndex = 12;
            this.blockNumberLabel.Text = "Block Number:";
            
            // 
            // blockNumberTextBox
            // 
            this.blockNumberTextBox.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.blockNumberTextBox.Location = new System.Drawing.Point(300, 250);
            this.blockNumberTextBox.Name = "blockNumberTextBox";
            this.blockNumberTextBox.Size = new System.Drawing.Size(100, 23);
            this.blockNumberTextBox.TabIndex = 13;
            
            // 
            // lineNumberLabel
            // 
            this.lineNumberLabel.AutoSize = true;
            this.lineNumberLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lineNumberLabel.ForeColor = System.Drawing.Color.Black;
            this.lineNumberLabel.Location = new System.Drawing.Point(420, 230);
            this.lineNumberLabel.Name = "lineNumberLabel";
            this.lineNumberLabel.Size = new System.Drawing.Size(75, 15);
            this.lineNumberLabel.TabIndex = 14;
            this.lineNumberLabel.Text = "Line Number:";
            
            // 
            // lineNumberTextBox
            // 
            this.lineNumberTextBox.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lineNumberTextBox.Location = new System.Drawing.Point(420, 250);
            this.lineNumberTextBox.Name = "lineNumberTextBox";
            this.lineNumberTextBox.Size = new System.Drawing.Size(100, 23);
            this.lineNumberTextBox.TabIndex = 15;
            
            // 
            // productIdLabel
            // 
            this.productIdLabel.AutoSize = true;
            this.productIdLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.productIdLabel.ForeColor = System.Drawing.Color.Black;
            this.productIdLabel.Location = new System.Drawing.Point(20, 290);
            this.productIdLabel.Name = "productIdLabel";
            this.productIdLabel.Size = new System.Drawing.Size(60, 15);
            this.productIdLabel.TabIndex = 16;
            this.productIdLabel.Text = "Product ID:";
            
            // 
            // activeScannersLabel
            // 
            this.activeScannersLabel.AutoSize = true;
            this.activeScannersLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.activeScannersLabel.ForeColor = System.Drawing.Color.FromArgb(0, 120, 215);
            this.activeScannersLabel.Location = new System.Drawing.Point(20, 350);
            this.activeScannersLabel.Name = "activeScannersLabel";
            this.activeScannersLabel.Size = new System.Drawing.Size(120, 19);
            this.activeScannersLabel.TabIndex = 20;
            this.activeScannersLabel.Text = "Active Scanners: 0";
            this.activeScannersLabel.Visible = true;
            
            // 
            // todayScansLabel
            // 
            this.todayScansLabel.AutoSize = true;
            this.todayScansLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.todayScansLabel.ForeColor = System.Drawing.Color.FromArgb(0, 120, 215);
            this.todayScansLabel.Location = new System.Drawing.Point(200, 350);
            this.todayScansLabel.Name = "todayScansLabel";
            this.todayScansLabel.Size = new System.Drawing.Size(100, 19);
            this.todayScansLabel.TabIndex = 21;
            this.todayScansLabel.Text = "Today's Scans: 0";
            this.todayScansLabel.Visible = true;
            
            // 
            // lastHourScansLabel
            // 
            this.lastHourScansLabel.AutoSize = true;
            this.lastHourScansLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lastHourScansLabel.ForeColor = System.Drawing.Color.FromArgb(0, 120, 215);
            this.lastHourScansLabel.Location = new System.Drawing.Point(380, 350);
            this.lastHourScansLabel.Name = "lastHourScansLabel";
            this.lastHourScansLabel.Size = new System.Drawing.Size(120, 19);
            this.lastHourScansLabel.TabIndex = 22;
            this.lastHourScansLabel.Text = "Last Hour Scans: 0";
            this.lastHourScansLabel.Visible = true;
            
            // 
            // productIdComboBox
            // 
            this.productIdComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.productIdComboBox.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.productIdComboBox.FormattingEnabled = true;
            this.productIdComboBox.Location = new System.Drawing.Point(20, 310);
            this.productIdComboBox.Name = "productIdComboBox";
            this.productIdComboBox.Size = new System.Drawing.Size(150, 23);
            this.productIdComboBox.TabIndex = 17;
            
            // 
            // applyFiltersButton
            // 
            this.applyFiltersButton.BackColor = System.Drawing.Color.FromArgb(50, 74, 95);
            this.applyFiltersButton.FlatAppearance.BorderSize = 0;
            this.applyFiltersButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.applyFiltersButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.applyFiltersButton.ForeColor = System.Drawing.Color.White;
            this.applyFiltersButton.Location = new System.Drawing.Point(200, 310);
            this.applyFiltersButton.Name = "applyFiltersButton";
            this.applyFiltersButton.Size = new System.Drawing.Size(80, 25);
            this.applyFiltersButton.TabIndex = 18;
            this.applyFiltersButton.Text = "Apply";
            this.applyFiltersButton.UseVisualStyleBackColor = false;
            this.applyFiltersButton.Click += new System.EventHandler(this.applyFiltersButton_Click);
            
            // 
            // clearFiltersButton
            // 
            this.clearFiltersButton.BackColor = System.Drawing.Color.FromArgb(108, 117, 125);
            this.clearFiltersButton.FlatAppearance.BorderSize = 0;
            this.clearFiltersButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.clearFiltersButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.clearFiltersButton.ForeColor = System.Drawing.Color.White;
            this.clearFiltersButton.Location = new System.Drawing.Point(300, 310);
            this.clearFiltersButton.Name = "clearFiltersButton";
            this.clearFiltersButton.Size = new System.Drawing.Size(80, 25);
            this.clearFiltersButton.TabIndex = 19;
            this.clearFiltersButton.Text = "Clear";
            this.clearFiltersButton.UseVisualStyleBackColor = false;
            this.clearFiltersButton.Click += new System.EventHandler(this.clearFiltersButton_Click);
            
            // 
            // runScannerScriptButton
            // 
            // this.runScannerScriptButton.Location = new System.Drawing.Point(50, 470);
            // this.runScannerScriptButton.Name = "runScannerScriptButton";
            // this.runScannerScriptButton.Size = new System.Drawing.Size(150, 40);
            // this.runScannerScriptButton.TabIndex = 1;
            // this.runScannerScriptButton.Text = "Run Scan Script";
            // this.runScannerScriptButton.UseVisualStyleBackColor = true;
            // this.runScannerScriptButton.Click += new System.EventHandler(this.runScannerScriptButton_Click);
            
            // 
            // manageScannersButton
            // 
            this.manageScannersButton.Location = new System.Drawing.Point(50, 20);
            this.manageScannersButton.Name = "manageScannersButton";
            this.manageScannersButton.Size = new System.Drawing.Size(130, 32);
            this.manageScannersButton.TabIndex = 2;
            this.manageScannersButton.Text = "Manage Scanners";
            this.manageScannersButton.UseVisualStyleBackColor = true;
            this.manageScannersButton.Click += new System.EventHandler(this.manageScannersButton_Click);
            
            // 
            // mainPanel
            // 
            this.mainPanel.AutoScroll = true;
            this.mainPanel.BackColor = System.Drawing.Color.White;
            this.mainPanel.Controls.Add(this.statusPanel);
            this.mainPanel.Controls.Add(this.actionPanel);
            this.mainPanel.Controls.Add(this.advancedPanel);
            this.mainPanel.Controls.Add(this.configPanel);
            this.mainPanel.Controls.Add(this.connectionPanel);
            this.mainPanel.Controls.Add(this.headerPanel);
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.Location = new System.Drawing.Point(0, 0);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Padding = new System.Windows.Forms.Padding(20);
            this.mainPanel.Size = new System.Drawing.Size(560, 1500);
            this.mainPanel.TabIndex = 0;
            
            // 
            // headerPanel
            // 
            this.headerPanel.BackColor = System.Drawing.Color.FromArgb(27, 42, 65);
            this.headerPanel.Controls.Add(this.titleLabel);
            this.headerPanel.Controls.Add(this.subtitleLabel);
            this.headerPanel.Controls.Add(this.logoutButtonPrinter);
            this.headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.headerPanel.Location = new System.Drawing.Point(20, 20);
            this.headerPanel.Name = "headerPanel";
            this.headerPanel.Padding = new System.Windows.Forms.Padding(30, 20, 30, 20);
            this.headerPanel.Size = new System.Drawing.Size(560, 100);
            this.headerPanel.TabIndex = 0;
            
            // 
            // titleLabel
            // 
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Segoe UI", 26F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.titleLabel.ForeColor = System.Drawing.Color.White;
            this.titleLabel.Location = new System.Drawing.Point(30, 8);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(168, 45);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "Scan Link";
            
            // 
            // subtitleLabel
            // 
            this.subtitleLabel.AutoSize = true;
            this.subtitleLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.subtitleLabel.ForeColor = System.Drawing.Color.FromArgb(204, 201, 220);
            this.subtitleLabel.Location = new System.Drawing.Point(34, 60);
            this.subtitleLabel.Name = "subtitleLabel";
            this.subtitleLabel.Size = new System.Drawing.Size(281, 20);
            this.subtitleLabel.TabIndex = 1;
            this.subtitleLabel.Text = "Professional Barcode Printing Solution";
            
            // 
            // connectionPanel
            // 
            this.connectionPanel.Controls.Add(this.connectionGroupBox);
            this.connectionPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.connectionPanel.Location = new System.Drawing.Point(20, 120);
            this.connectionPanel.Name = "connectionPanel";
            this.connectionPanel.Padding = new System.Windows.Forms.Padding(0, 20, 0, 0);
            this.connectionPanel.Size = new System.Drawing.Size(560, 160);
            this.connectionPanel.TabIndex = 1;
            
            // 
            // connectionGroupBox
            // 
            this.connectionGroupBox.Controls.Add(this.connectionStatusLabel);
            this.connectionGroupBox.Controls.Add(this.label_port);
            this.connectionGroupBox.Controls.Add(this.comboBox_port);
            this.connectionGroupBox.Controls.Add(this.button_setting);
            this.connectionGroupBox.Controls.Add(this.textBox_port);
            this.connectionGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.connectionGroupBox.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.connectionGroupBox.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.connectionGroupBox.Location = new System.Drawing.Point(0, 20);
            this.connectionGroupBox.Name = "connectionGroupBox";
            this.connectionGroupBox.Padding = new System.Windows.Forms.Padding(20);
            this.connectionGroupBox.Size = new System.Drawing.Size(560, 140);
            this.connectionGroupBox.TabIndex = 0;
            this.connectionGroupBox.TabStop = false;
            this.connectionGroupBox.Text = "🔌 Printer Connection";
            
            // 
            // connectionStatusLabel
            // 
            this.connectionStatusLabel.AutoSize = true;
            this.connectionStatusLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.connectionStatusLabel.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.connectionStatusLabel.Location = new System.Drawing.Point(25, 100);
            this.connectionStatusLabel.Name = "connectionStatusLabel";
            this.connectionStatusLabel.Size = new System.Drawing.Size(200, 15);
            this.connectionStatusLabel.TabIndex = 4;
            this.connectionStatusLabel.Text = "Status: Disconnected";
            // 
            // label_port
            // 
            this.label_port.AutoSize = true;
            this.label_port.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.label_port.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.label_port.Location = new System.Drawing.Point(25, 35);
            this.label_port.Name = "label_port";
            this.label_port.Size = new System.Drawing.Size(89, 15);
            this.label_port.TabIndex = 0;
            this.label_port.Text = "Connection Type";
            
            // 
            // comboBox_port
            // 
            this.comboBox_port.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_port.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.comboBox_port.FormattingEnabled = true;
            this.comboBox_port.Location = new System.Drawing.Point(140, 32);
            this.comboBox_port.Name = "comboBox_port";
            this.comboBox_port.Size = new System.Drawing.Size(250, 23);
            this.comboBox_port.TabIndex = 1;
            this.comboBox_port.SelectedIndexChanged += new System.EventHandler(this.comboBox_port_SelectedIndexChanged);
            
            // 
            // button_setting
            // 
            this.button_setting.BackColor = System.Drawing.Color.FromArgb(50, 74, 95);
            this.button_setting.FlatAppearance.BorderSize = 0;
            this.button_setting.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_setting.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.button_setting.ForeColor = System.Drawing.Color.White;
            this.button_setting.Location = new System.Drawing.Point(410, 32);
            this.button_setting.Name = "button_setting";
            this.button_setting.Size = new System.Drawing.Size(120, 32);
            this.button_setting.TabIndex = 2;
            this.button_setting.Text = "⚙️ Configure";
            this.button_setting.UseVisualStyleBackColor = false;
            this.button_setting.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(27, 42, 65);
            this.button_setting.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(0, 0, 0);
            this.button_setting.Click += new System.EventHandler(this.button_setting_Click);
            
            // 
            // textBox_port
            // 
            this.textBox_port.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.textBox_port.Location = new System.Drawing.Point(140, 70);
            this.textBox_port.Name = "textBox_port";
            this.textBox_port.ReadOnly = true;
            this.textBox_port.Size = new System.Drawing.Size(390, 23);
            this.textBox_port.TabIndex = 3;
            
            // 
            // configPanel
            // 
            this.configPanel.Controls.Add(this.configGroupBox);
            this.configPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.configPanel.Location = new System.Drawing.Point(20, 280);
            this.configPanel.Name = "configPanel";
            this.configPanel.Padding = new System.Windows.Forms.Padding(0, 20, 0, 0);
            this.configPanel.Size = new System.Drawing.Size(560, 185);
            this.configPanel.TabIndex = 2;
            
            // 
            // configGroupBox
            // 
            this.configGroupBox.Controls.Add(this.barcodeTextPanel);
            this.configGroupBox.Controls.Add(this.label_count);
            this.configGroupBox.Controls.Add(this.numericUpDown_count);
            this.configGroupBox.Controls.Add(this.checkBox_showAdvanced);
            this.configGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.configGroupBox.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.configGroupBox.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.configGroupBox.Location = new System.Drawing.Point(0, 20);
            this.configGroupBox.Name = "configGroupBox";
            this.configGroupBox.Padding = new System.Windows.Forms.Padding(20);
            this.configGroupBox.Size = new System.Drawing.Size(560, 175);
            this.configGroupBox.TabIndex = 0;
            this.configGroupBox.TabStop = false;
            this.configGroupBox.Text = "⚙️ Print Configuration";
            
            // 
            // barcodeTextPanel
            // 
            this.barcodeTextPanel.Controls.Add(this.label_EmployeeID);
            this.barcodeTextPanel.Controls.Add(this.textBox_EmployeeID);
            this.barcodeTextPanel.Controls.Add(this.button_FetchEmployees);
            this.barcodeTextPanel.Controls.Add(this.label_ProductID);
            this.barcodeTextPanel.Controls.Add(this.comboBox_ProductID);
            this.barcodeTextPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.barcodeTextPanel.Location = new System.Drawing.Point(20, 20);
            this.barcodeTextPanel.Name = "barcodeTextPanel";
            this.barcodeTextPanel.Size = new System.Drawing.Size(600, 60);
            this.barcodeTextPanel.TabIndex = 0;
            
            // 
            // label_EmployeeID
            // 
            this.label_EmployeeID.AutoSize = true;
            this.label_EmployeeID.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.label_EmployeeID.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.label_EmployeeID.Location = new System.Drawing.Point(5, 5);
            this.label_EmployeeID.Name = "label_EmployeeID";
            this.label_EmployeeID.Size = new System.Drawing.Size(77, 15);
            this.label_EmployeeID.TabIndex = 0;
            this.label_EmployeeID.Text = "Employee ID";
            
            // 
            // textBox_EmployeeID
            // 
            this.textBox_EmployeeID.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.textBox_EmployeeID.Location = new System.Drawing.Point(120, 2);
            this.textBox_EmployeeID.Name = "textBox_EmployeeID";
            this.textBox_EmployeeID.Size = new System.Drawing.Size(300, 20);
            this.textBox_EmployeeID.TabIndex = 1;
            this.textBox_EmployeeID.Text = "1234567890-Tanish";
            
            // 
            // button_FetchEmployees
            // 
            this.button_FetchEmployees.BackColor = System.Drawing.Color.FromArgb(52, 152, 219);
            this.button_FetchEmployees.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_FetchEmployees.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.button_FetchEmployees.ForeColor = System.Drawing.Color.White;
            this.button_FetchEmployees.Location = new System.Drawing.Point(426, 2);
            this.button_FetchEmployees.Name = "button_FetchEmployees";
            this.button_FetchEmployees.Size = new System.Drawing.Size(77, 23);
            this.button_FetchEmployees.TabIndex = 2;
            this.button_FetchEmployees.Text = "📋 Fetch";
            this.button_FetchEmployees.UseVisualStyleBackColor = false;
            this.button_FetchEmployees.Click += new System.EventHandler(this.button_FetchEmployees_Click);

            //
            // label_ProductID
            // 
            this.label_ProductID.AutoSize = true;
            this.label_ProductID.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.label_ProductID.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.label_ProductID.Location = new System.Drawing.Point(5, 40);
            this.label_ProductID.Name = "label_ProductID";
            this.label_ProductID.Size = new System.Drawing.Size(77, 15);
            this.label_ProductID.TabIndex = 0;
            this.label_ProductID.Text = "Product ID"; 
            
            // 
            // comboBox_ProductID
            // 
            this.comboBox_ProductID.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_ProductID.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.comboBox_ProductID.FormattingEnabled = true;
            this.comboBox_ProductID.Items.AddRange(new object[] {
            "p1",
            "p2",
            "p3",
            "p4",
            "p5",
            "p6",
            "p7",
            "p8",
            "p9",
            "p10"});
            this.comboBox_ProductID.Location = new System.Drawing.Point(120, 35);
            this.comboBox_ProductID.Name = "comboBox_ProductID";
            this.comboBox_ProductID.Size = new System.Drawing.Size(383, 20);
            this.comboBox_ProductID.TabIndex = 1;
            this.comboBox_ProductID.SelectedIndex = 0; 
            
            // 
            // label_count
            // 
            this.label_count.AutoSize = true;
            this.label_count.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.label_count.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.label_count.Location = new System.Drawing.Point(25, 110);
            this.label_count.Name = "label_count";
            this.label_count.Size = new System.Drawing.Size(65, 15);
            this.label_count.TabIndex = 6;
            this.label_count.Text = "Print Count";
            
            // 
            // numericUpDown_count
            // 
            this.numericUpDown_count.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.numericUpDown_count.Location = new System.Drawing.Point(140, 107);
            this.numericUpDown_count.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numericUpDown_count.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDown_count.Name = "numericUpDown_count";
            this.numericUpDown_count.Size = new System.Drawing.Size(150, 23);
            this.numericUpDown_count.TabIndex = 7;
            this.numericUpDown_count.Value = new decimal(new int[] { 1, 0, 0, 0 });
            
            // 
            // checkBox_showAdvanced
            // 
            this.checkBox_showAdvanced.AutoSize = true;
            this.checkBox_showAdvanced.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.checkBox_showAdvanced.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.checkBox_showAdvanced.Location = new System.Drawing.Point(27, 140);
            this.checkBox_showAdvanced.Name = "checkBox_showAdvanced";
            this.checkBox_showAdvanced.Size = new System.Drawing.Size(189, 20);
            this.checkBox_showAdvanced.TabIndex = 8;
            this.checkBox_showAdvanced.Text = "Show Advance Settings";
            this.checkBox_showAdvanced.UseVisualStyleBackColor = true;
            this.checkBox_showAdvanced.CheckedChanged += new System.EventHandler(this.checkBox_showAdvanced_CheckedChanged);
            
            // 
            // advancedPanel
            // 
            this.advancedPanel.Controls.Add(this.advancedGroupBox);
            this.advancedPanel.Dock = System.Windows.Forms.DockStyle.None;
            this.advancedPanel.Location = new System.Drawing.Point(20, 500);
            this.advancedPanel.Name = "advancedPanel";
            this.advancedPanel.Padding = new System.Windows.Forms.Padding(0, 20, 0, 0);
            this.advancedPanel.Size = new System.Drawing.Size(560, 470);
            this.advancedPanel.TabIndex = 3;
            this.advancedPanel.Visible = false;
            
            // 
            // advancedGroupBox
            // 
            this.advancedGroupBox.Controls.Add(this.previewPanel);
            this.advancedGroupBox.Controls.Add(this.qualityPanel);
            this.advancedGroupBox.Controls.Add(this.alignmentPanel);
            this.advancedGroupBox.Controls.Add(this.dimensionsPanel);
            this.advancedGroupBox.Controls.Add(this.printerConfigPanel);
            this.advancedGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.advancedGroupBox.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.advancedGroupBox.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.advancedGroupBox.Location = new System.Drawing.Point(0, 20);
            this.advancedGroupBox.Name = "advancedGroupBox";
            this.advancedGroupBox.Padding = new System.Windows.Forms.Padding(20);
            this.advancedGroupBox.Size = new System.Drawing.Size(560, 470);
            this.advancedGroupBox.TabIndex = 0;
            this.advancedGroupBox.TabStop = false;
            this.advancedGroupBox.Text = "🔧 Advanced Print Settings";
            
            // 
            // printerConfigPanel
            // 
            this.printerConfigPanel.Controls.Add(this.label_emulation);
            this.printerConfigPanel.Controls.Add(this.comboBox_emulation);
            this.printerConfigPanel.Controls.Add(this.label_test);
            this.printerConfigPanel.Controls.Add(this.comboBox_test);
            this.printerConfigPanel.Controls.Add(this.label_barcode);
            this.printerConfigPanel.Controls.Add(this.comboBox_barcode);
            this.printerConfigPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.printerConfigPanel.Location = new System.Drawing.Point(20, 43);
            this.printerConfigPanel.Name = "printerConfigPanel";
            this.printerConfigPanel.Size = new System.Drawing.Size(520, 135);
            this.printerConfigPanel.TabIndex = 0;

            // 
            // label_emulation
            // 
            this.label_emulation.AutoSize = true;
            this.label_emulation.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.label_emulation.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.label_emulation.Location = new System.Drawing.Point(5, 5);
            this.label_emulation.Name = "label_emulation";
            this.label_emulation.Size = new System.Drawing.Size(89, 15);
            this.label_emulation.TabIndex = 0;
            this.label_emulation.Text = "Printer Language";
            
            // 
            // comboBox_emulation
            // 
            this.comboBox_emulation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_emulation.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.comboBox_emulation.FormattingEnabled = true;
            this.comboBox_emulation.Location = new System.Drawing.Point(120, 2);
            this.comboBox_emulation.Name = "comboBox_emulation";
            this.comboBox_emulation.Size = new System.Drawing.Size(380, 23);
            this.comboBox_emulation.TabIndex = 1;
            this.comboBox_emulation.SelectedIndexChanged += new System.EventHandler(this.comboBox_emulation_SelectedIndexChanged);
            
            // 
            // label_test
            // 
            this.label_test.AutoSize = true;
            this.label_test.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.label_test.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.label_test.Location = new System.Drawing.Point(5, 45);
            this.label_test.Name = "label_test";
            this.label_test.Size = new System.Drawing.Size(58, 15);
            this.label_test.TabIndex = 2;
            this.label_test.Text = "Test Mode";
            
            // 
            // comboBox_test
            // 
            this.comboBox_test.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_test.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.comboBox_test.FormattingEnabled = true;
            this.comboBox_test.Location = new System.Drawing.Point(120, 42);
            this.comboBox_test.Name = "comboBox_test";
            this.comboBox_test.Size = new System.Drawing.Size(380, 23);
            this.comboBox_test.TabIndex = 3;
            this.comboBox_test.SelectedIndexChanged += new System.EventHandler(this.comboBox_test_SelectedIndexChanged);
            
            // 
            // label_barcode
            // 
            this.label_barcode.AutoSize = true;
            this.label_barcode.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.label_barcode.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.label_barcode.Location = new System.Drawing.Point(5, 85);
            this.label_barcode.Name = "label_barcode";
            this.label_barcode.Size = new System.Drawing.Size(76, 15);
            this.label_barcode.TabIndex = 4;
            this.label_barcode.Text = "Barcode Type";
            
            // 
            // comboBox_barcode
            // 
            this.comboBox_barcode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_barcode.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.comboBox_barcode.FormattingEnabled = true;
            this.comboBox_barcode.Location = new System.Drawing.Point(120, 82);
            this.comboBox_barcode.Name = "comboBox_barcode";
            this.comboBox_barcode.Size = new System.Drawing.Size(380, 23);
            this.comboBox_barcode.TabIndex = 5;
            this.comboBox_barcode.SelectedIndexChanged += new System.EventHandler(this.comboBox_barcode_SelectedIndexChanged);

            // 
            // dimensionsPanel
            // 
            this.dimensionsPanel.Controls.Add(this.label_width);
            this.dimensionsPanel.Controls.Add(this.numericUpDown_width);
            this.dimensionsPanel.Controls.Add(this.label_height);
            this.dimensionsPanel.Controls.Add(this.numericUpDown_height);
            this.dimensionsPanel.Controls.Add(this.label_gap);
            this.dimensionsPanel.Controls.Add(this.numericUpDown_gap);
            this.dimensionsPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.dimensionsPanel.Location = new System.Drawing.Point(20, 178);
            this.dimensionsPanel.Name = "dimensionsPanel";
            this.dimensionsPanel.Size = new System.Drawing.Size(520, 90);
            this.dimensionsPanel.TabIndex = 1;
            
            // 
            // label_width
            // 
            this.label_width.AutoSize = true;
            this.label_width.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.label_width.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.label_width.Location = new System.Drawing.Point(5, 15);
            this.label_width.Name = "label_width";
            this.label_width.Size = new System.Drawing.Size(39, 15);
            this.label_width.TabIndex = 0;
            this.label_width.Text = "Width";
            
            // 
            // numericUpDown_width
            // 
            this.numericUpDown_width.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.numericUpDown_width.Location = new System.Drawing.Point(120, 12);
            this.numericUpDown_width.Maximum = new decimal(new int[] { 999, 0, 0, 0 });
            this.numericUpDown_width.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDown_width.Name = "numericUpDown_width";
            this.numericUpDown_width.Size = new System.Drawing.Size(100, 23);
            this.numericUpDown_width.TabIndex = 1;
            this.numericUpDown_width.Value = new decimal(new int[] { 400, 0, 0, 0 });
            this.numericUpDown_width.ValueChanged += new System.EventHandler(this.numericUpDown_width_ValueChanged);
            
            // 
            // label_height
            // 
            this.label_height.AutoSize = true;
            this.label_height.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.label_height.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.label_height.Location = new System.Drawing.Point(300, 15);
            this.label_height.Name = "label_height";
            this.label_height.Size = new System.Drawing.Size(43, 15);
            this.label_height.TabIndex = 2;
            this.label_height.Text = "Height";
            
            // 
            // numericUpDown_height
            // 
            this.numericUpDown_height.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.numericUpDown_height.Location = new System.Drawing.Point(400, 12);
            this.numericUpDown_height.Maximum = new decimal(new int[] { 999, 0, 0, 0 });
            this.numericUpDown_height.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDown_height.Name = "numericUpDown_height";
            this.numericUpDown_height.Size = new System.Drawing.Size(100, 23);
            this.numericUpDown_height.TabIndex = 3;
            this.numericUpDown_height.Value = new decimal(new int[] { 180, 0, 0, 0 });
            this.numericUpDown_height.ValueChanged += new System.EventHandler(this.numericUpDown_height_ValueChanged);
            
            // 
            // label_gap
            // 
            this.label_gap.AutoSize = true;
            this.label_gap.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.label_gap.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.label_gap.Location = new System.Drawing.Point(5, 50);
            this.label_gap.Name = "label_gap";
            this.label_gap.Size = new System.Drawing.Size(28, 15);
            this.label_gap.TabIndex = 4;
            this.label_gap.Text = "Gap";
            
            // 
            // numericUpDown_gap
            // 
            this.numericUpDown_gap.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.numericUpDown_gap.Location = new System.Drawing.Point(120, 47);
            this.numericUpDown_gap.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numericUpDown_gap.Name = "numericUpDown_gap";
            this.numericUpDown_gap.Size = new System.Drawing.Size(100, 23);
            this.numericUpDown_gap.TabIndex = 5;
            this.numericUpDown_gap.Value = new decimal(new int[] { 2, 0, 0, 0 });
            this.numericUpDown_gap.ValueChanged += new System.EventHandler(this.numericUpDown_gap_ValueChanged);
            
            // 
            // alignmentPanel
            // 
            this.alignmentPanel.Controls.Add(this.label_alignment);
            this.alignmentPanel.Controls.Add(this.comboBox_alignment);
            this.alignmentPanel.Controls.Add(this.label_rotation);
            this.alignmentPanel.Controls.Add(this.comboBox_rotation);
            this.alignmentPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.alignmentPanel.Location = new System.Drawing.Point(20, 268);
            this.alignmentPanel.Name = "alignmentPanel";
            this.alignmentPanel.Size = new System.Drawing.Size(520, 50);
            this.alignmentPanel.TabIndex = 2;
            
            // 
            // label_alignment
            // 
            this.label_alignment.AutoSize = true;
            this.label_alignment.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.label_alignment.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.label_alignment.Location = new System.Drawing.Point(5, 15);
            this.label_alignment.Name = "label_alignment";
            this.label_alignment.Size = new System.Drawing.Size(62, 15);
            this.label_alignment.TabIndex = 0;
            this.label_alignment.Text = "Alignment";
            
            // 
            // comboBox_alignment
            // 
            this.comboBox_alignment.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_alignment.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.comboBox_alignment.FormattingEnabled = true;
            this.comboBox_alignment.Items.AddRange(new object[] { "Left", "Center", "Right" });
            this.comboBox_alignment.Location = new System.Drawing.Point(120, 12);
            this.comboBox_alignment.Name = "comboBox_alignment";
            this.comboBox_alignment.Size = new System.Drawing.Size(100, 23);
            this.comboBox_alignment.TabIndex = 1;
            this.comboBox_alignment.SelectedIndex = 0;
            this.comboBox_alignment.SelectedIndexChanged += new System.EventHandler(this.comboBox_alignment_SelectedIndexChanged);
            
            // 
            // label_rotation
            // 
            this.label_rotation.AutoSize = true;
            this.label_rotation.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.label_rotation.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.label_rotation.Location = new System.Drawing.Point(300, 15);
            this.label_rotation.Name = "label_rotation";
            this.label_rotation.Size = new System.Drawing.Size(52, 15);
            this.label_rotation.TabIndex = 2;
            this.label_rotation.Text = "Rotation";
            // this.label_rotation.Visible = false;
            
            // 
            // comboBox_rotation
            // 
            this.comboBox_rotation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_rotation.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.comboBox_rotation.FormattingEnabled = true;
            this.comboBox_rotation.Items.AddRange(new object[] { "0°", "90°", "180°", "270°" });
            this.comboBox_rotation.Location = new System.Drawing.Point(400, 12);
            this.comboBox_rotation.Name = "comboBox_rotation";
            this.comboBox_rotation.Size = new System.Drawing.Size(100, 23);
            this.comboBox_rotation.TabIndex = 3;
            this.comboBox_rotation.SelectedIndex = 0;
            this.comboBox_rotation.SelectedIndexChanged += new System.EventHandler(this.comboBox_rotation_SelectedIndexChanged);
            // this.comboBox_rotation.Visible = false;
            
            // 
            // qualityPanel
            // 
            this.qualityPanel.Controls.Add(this.label_darkness);
            this.qualityPanel.Controls.Add(this.trackBar_darkness);
            this.qualityPanel.Controls.Add(this.label_darknessValue);
            this.qualityPanel.Controls.Add(this.label_speed);
            this.qualityPanel.Controls.Add(this.comboBox_speed);
            this.qualityPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.qualityPanel.Location = new System.Drawing.Point(20, 318);
            this.qualityPanel.Name = "qualityPanel";
            this.qualityPanel.Size = new System.Drawing.Size(520, 90);
            this.qualityPanel.TabIndex = 3;
            
            // 
            // label_darkness
            // 
            this.label_darkness.AutoSize = true;
            this.label_darkness.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.label_darkness.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.label_darkness.Location = new System.Drawing.Point(5, 15);
            this.label_darkness.Name = "label_darkness";
            this.label_darkness.Size = new System.Drawing.Size(56, 15);
            this.label_darkness.TabIndex = 0;
            this.label_darkness.Text = "Darkness";
            
            // 
            // trackBar_darkness
            // 
            this.trackBar_darkness.Location = new System.Drawing.Point(120, 10);
            this.trackBar_darkness.Maximum = 30;
            this.trackBar_darkness.Minimum = 1;
            
            this.trackBar_darkness.Name = "trackBar_darkness";
            this.trackBar_darkness.Size = new System.Drawing.Size(300, 45);
            this.trackBar_darkness.TabIndex = 1;
            this.trackBar_darkness.Value = 15;
            this.trackBar_darkness.Scroll += new System.EventHandler(this.trackBar_darkness_Scroll);
            
            // 
            // label_darknessValue
            // 
            this.label_darknessValue.AutoSize = true;
            this.label_darknessValue.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.label_darknessValue.ForeColor = System.Drawing.Color.FromArgb(27, 42, 65);
            this.label_darknessValue.Location = new System.Drawing.Point(430, 15);
            this.label_darknessValue.Name = "label_darknessValue";
            this.label_darknessValue.Size = new System.Drawing.Size(19, 15);
            this.label_darknessValue.TabIndex = 2;
            this.label_darknessValue.Text = "15";
            
            // 
            // label_speed
            // 
            this.label_speed.AutoSize = true;
            this.label_speed.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.label_speed.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.label_speed.Location = new System.Drawing.Point(5, 59);
            this.label_speed.Name = "label_speed";
            this.label_speed.Size = new System.Drawing.Size(68, 15);
            this.label_speed.TabIndex = 3;
            this.label_speed.Text = "Print Speed";
            
            // 
            // comboBox_speed
            // 
            this.comboBox_speed.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_speed.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.comboBox_speed.FormattingEnabled = true;
            this.comboBox_speed.Items.AddRange(new object[] { "1 - Slowest", "2", "3", "4", "5 - Medium", "6", "7", "8", "9 - Fastest" });
            this.comboBox_speed.Location = new System.Drawing.Point(120, 56);
            this.comboBox_speed.Name = "comboBox_speed";
            this.comboBox_speed.Size = new System.Drawing.Size(200, 23);
            this.comboBox_speed.TabIndex = 4;
            this.comboBox_speed.SelectedIndex = 4;
            this.comboBox_speed.SelectedIndexChanged += new System.EventHandler(this.comboBox_speed_SelectedIndexChanged);
            
            // 
            // previewPanel
            // 
            this.previewPanel.Controls.Add(this.button_preview);
            this.previewPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.previewPanel.Location = new System.Drawing.Point(20, 408);
            this.previewPanel.Name = "previewPanel";
            this.previewPanel.Size = new System.Drawing.Size(520, 40);
            this.previewPanel.TabIndex = 4;
            
            // 
            // button_preview
            // 
            this.button_preview.BackColor = System.Drawing.Color.FromArgb(50, 74, 95);
            this.button_preview.FlatAppearance.BorderSize = 0;
            this.button_preview.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_preview.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.button_preview.ForeColor = System.Drawing.Color.White;
            this.button_preview.Location = new System.Drawing.Point(5, 0);
            this.button_preview.Name = "button_preview";
            this.button_preview.Size = new System.Drawing.Size(150, 35);
            this.button_preview.TabIndex = 0;
            this.button_preview.Text = "👁️ Preview Label";
            this.button_preview.UseVisualStyleBackColor = false;
            this.button_preview.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(27, 42, 65);
            this.button_preview.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(0, 0, 0);
            this.button_preview.Click += new System.EventHandler(this.button_preview_Click);
            
            // 
            // actionPanel
            // 
            this.actionPanel.Controls.Add(this.button_send);
            this.actionPanel.Controls.Add(this.progressBar);
            this.actionPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.actionPanel.Location = new System.Drawing.Point(20, 970);
            this.actionPanel.Name = "actionPanel";
            this.actionPanel.Padding = new System.Windows.Forms.Padding(0, 30, 0, 0);
            this.actionPanel.Size = new System.Drawing.Size(560, 80);
            this.actionPanel.TabIndex = 3;
            
            // 
            // button_send
            // 
            this.button_send.BackColor = System.Drawing.Color.FromArgb(50, 74, 95);
            this.button_send.FlatAppearance.BorderSize = 0;
            this.button_send.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_send.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.button_send.ForeColor = System.Drawing.Color.White;
            this.button_send.Location = new System.Drawing.Point(0, 18);
            this.button_send.Name = "button_send";
            this.button_send.Size = new System.Drawing.Size(560, 50);
            this.button_send.TabIndex = 0;
            this.button_send.Text = "🖨️ Start Printing";
            this.button_send.UseVisualStyleBackColor = false;
            this.button_send.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(27, 42, 65);
            this.button_send.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(0, 0, 0);
            this.button_send.Click += new System.EventHandler(this.button_send_Click);
            
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(0, 70);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(560, 8);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar.MarqueeAnimationSpeed = 30;
            this.progressBar.TabIndex = 1;
            this.progressBar.Visible = true;
            
            // 
            // statusPanel
            // 
            this.statusPanel.Controls.Add(this.statusLabel);
            this.statusPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.statusPanel.Location = new System.Drawing.Point(20, 980);
            this.statusPanel.Name = "statusPanel";
            this.statusPanel.Padding = new System.Windows.Forms.Padding(0, 15, 0, 15);
            this.statusPanel.Size = new System.Drawing.Size(560, 80);
            this.statusPanel.TabIndex = 4;
            
            // 
            // statusLabel
            // 
            this.statusLabel.BackColor = System.Drawing.Color.FromArgb(204, 201, 220);
            this.statusLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.statusLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.statusLabel.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.statusLabel.Location = new System.Drawing.Point(0, 20);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Padding = new System.Windows.Forms.Padding(20, 10, 20, 10);
            this.statusLabel.Size = new System.Drawing.Size(560, 40);
            this.statusLabel.TabIndex = 0;
            this.statusLabel.Text = "Ready. Choose connection and configure settings above.";
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(600, 1120);
            this.Controls.Add(this.loginPanel);
            this.Controls.Add(this.startPanel);
            this.Controls.Add(this.printerContentPanel);
            this.Controls.Add(this.scannerContentPanel);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MinimumSize = new System.Drawing.Size(600, 650);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Scan Link - Professional Barcode Printing";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.Form1_Load);
            this.startPanel.ResumeLayout(false);
            this.startPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).EndInit();
            this.printerContentPanel.ResumeLayout(false);
            this.scannerContentPanel.ResumeLayout(false);
            this.scannerContentPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scannerDataGridView)).EndInit();
            this.mainPanel.ResumeLayout(false);
            this.headerPanel.ResumeLayout(false);
            this.headerPanel.PerformLayout();
            this.connectionPanel.ResumeLayout(false);
            this.connectionGroupBox.ResumeLayout(false);
            this.connectionGroupBox.PerformLayout();
            this.configPanel.ResumeLayout(false);
            this.configGroupBox.ResumeLayout(false);
            this.configGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_count)).EndInit();
            this.advancedPanel.ResumeLayout(false);
            this.advancedGroupBox.ResumeLayout(false);
            this.printerConfigPanel.ResumeLayout(false);
            this.printerConfigPanel.PerformLayout();
            this.barcodeTextPanel.ResumeLayout(false);
            this.barcodeTextPanel.PerformLayout();
            this.dimensionsPanel.ResumeLayout(false);
            this.dimensionsPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_width)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_height)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_gap)).EndInit();
            this.alignmentPanel.ResumeLayout(false);
            this.alignmentPanel.PerformLayout();
            this.qualityPanel.ResumeLayout(false);
            this.qualityPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_darkness)).EndInit();
            this.previewPanel.ResumeLayout(false);
            this.actionPanel.ResumeLayout(false);
            this.actionPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_count)).EndInit();
            this.statusPanel.ResumeLayout(false);
            this.statusPanel.PerformLayout();
            this.loginPanel.ResumeLayout(false);
            this.loginPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.loginMainLogoPictureBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.startpanelLogoPictureBox)).EndInit();
            this.loginGroupBox.ResumeLayout(false);
            this.loginGroupBox.PerformLayout();
            this.ResumeLayout(false);
        }

        // Printer UI Initialization
        private void InitializePrinterUI()
        {
            InitFunctionData();
            foreach (string str in strPort) comboBox_port.Items.Add(str);
            comboBox_port.Text = "USB";
            foreach (string str in strEmulation) comboBox_emulation.Items.Add(str);
            comboBox_emulation.Text = "PPLB";
            
            InitializeAdvancedSettings();

            ApplyRoundedCorners(button_send, 10);
            ApplyRoundedCorners(button_preview, 10);
            ApplyRoundedCorners(button_setting, 10);
        }

        // Custom label class for colored text
        public class ColoredLabel : Label
        {
            public string MainText { get; set; } = "";
            public string AsteriskText { get; set; } = "*";
            public Color MainTextColor { get; set; } = Color.Black;
            public Color AsteriskColor { get; set; } = Color.Red;

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                
                // Clear the background
                e.Graphics.Clear(this.BackColor);

                // Measure text sizes
                var mainTextSize = e.Graphics.MeasureString(MainText, this.Font);
                var asteriskSize = e.Graphics.MeasureString(AsteriskText, this.Font);

                // Draw main text
                using (var mainBrush = new SolidBrush(MainTextColor))
                {
                    e.Graphics.DrawString(MainText, this.Font, mainBrush, 0, 0);
                }

                // Draw asterisk
                using (var asteriskBrush = new SolidBrush(AsteriskColor))
                {
                    e.Graphics.DrawString(AsteriskText, this.Font, asteriskBrush, mainTextSize.Width, 0);
                }
            }
        }

        // Helper method to create labels with colored asterisks
        private void CreateColoredLabel(Label label, string mainText, string asterisk = "*")
        {
            // Convert the existing label to our custom colored label
            var coloredLabel = new ColoredLabel
            {
                MainText = mainText,
                AsteriskText = asterisk,
                MainTextColor = Color.Black,
                AsteriskColor = Color.Red,
                Location = label.Location,
                Size = label.Size,
                Font = label.Font,
                BackColor = label.BackColor,
                Name = label.Name,
                TabIndex = label.TabIndex,
                UseCompatibleTextRendering = true
            };

            // Replace the original label with the colored one
            var parent = label.Parent;
            var index = parent.Controls.IndexOf(label);
            parent.Controls.Remove(label);
            parent.Controls.Add(coloredLabel);
            parent.Controls.SetChildIndex(coloredLabel, index);
        }

        private System.Windows.Forms.Panel loginPanel;
        private System.Windows.Forms.Label loginWelcomeLabel;
        private System.Windows.Forms.PictureBox loginMainLogoPictureBox;
        
        private System.Windows.Forms.PictureBox startpanelLogoPictureBox;
        private System.Windows.Forms.GroupBox loginGroupBox;
        private System.Windows.Forms.Button loginButton;
        private System.Windows.Forms.Button passwordToggleButton;
        private System.Windows.Forms.TextBox passwordTextBox;
        private System.Windows.Forms.Label passwordLabel;
        private System.Windows.Forms.TextBox usernameTextBox;
        private System.Windows.Forms.Label usernameLabel;
        private System.Windows.Forms.Label loginStatusLabel;
        private System.Windows.Forms.Panel mainPanel;
        private System.Windows.Forms.Panel headerPanel;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label subtitleLabel;
        private System.Windows.Forms.Panel connectionPanel;
        private System.Windows.Forms.GroupBox connectionGroupBox;
        private System.Windows.Forms.Label connectionStatusLabel;
        private System.Windows.Forms.Label label_port;
        private System.Windows.Forms.ComboBox comboBox_port;
        private System.Windows.Forms.Button button_setting;
        private System.Windows.Forms.TextBox textBox_port;
        private System.Windows.Forms.Panel configPanel;
        private System.Windows.Forms.GroupBox configGroupBox;
        private System.Windows.Forms.Panel barcodeTextPanel;
        private System.Windows.Forms.Label label_EmployeeID;
        private System.Windows.Forms.TextBox textBox_EmployeeID;
        private System.Windows.Forms.Button button_FetchEmployees;
        private System.Windows.Forms.Label label_ProductID;
        private System.Windows.Forms.ComboBox comboBox_ProductID;
        private System.Windows.Forms.Label label_count;
        private System.Windows.Forms.NumericUpDown numericUpDown_count;
        private System.Windows.Forms.Panel actionPanel;
        private System.Windows.Forms.Button button_send;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Panel statusPanel;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.CheckBox checkBox_showAdvanced;
        private System.Windows.Forms.Panel advancedPanel;
        private System.Windows.Forms.GroupBox advancedGroupBox;
        private System.Windows.Forms.Panel printerConfigPanel;
        private System.Windows.Forms.Label label_emulation;
        private System.Windows.Forms.ComboBox comboBox_emulation;
        private System.Windows.Forms.Label label_test;
        private System.Windows.Forms.ComboBox comboBox_test;
        private System.Windows.Forms.Label label_barcode;
        private System.Windows.Forms.ComboBox comboBox_barcode;

        private System.Windows.Forms.Panel dimensionsPanel;
        private System.Windows.Forms.Label label_width;
        private System.Windows.Forms.NumericUpDown numericUpDown_width;
        private System.Windows.Forms.Label label_height;
        private System.Windows.Forms.NumericUpDown numericUpDown_height;
        private System.Windows.Forms.Label label_gap;
        private System.Windows.Forms.NumericUpDown numericUpDown_gap;
        private System.Windows.Forms.Panel alignmentPanel;
        private System.Windows.Forms.Label label_alignment;
        private System.Windows.Forms.ComboBox comboBox_alignment;
        private System.Windows.Forms.Label label_rotation;
        private System.Windows.Forms.ComboBox comboBox_rotation;
        private System.Windows.Forms.Panel qualityPanel;
        private System.Windows.Forms.Label label_darkness;
        private System.Windows.Forms.TrackBar trackBar_darkness;
        private System.Windows.Forms.Label label_darknessValue;
        private System.Windows.Forms.Label label_speed;
        private System.Windows.Forms.ComboBox comboBox_speed;
        private System.Windows.Forms.Panel previewPanel;
        private System.Windows.Forms.Button button_preview;
        private System.Windows.Forms.Panel startPanel;
        private System.Windows.Forms.Label welcomeLabel;
        private System.Windows.Forms.Button logoutButtonStart;
        private System.Windows.Forms.PictureBox logoPictureBox;
        private System.Windows.Forms.Button printerButton;
        private System.Windows.Forms.Button scannerButton;
        private System.Windows.Forms.Panel printerContentPanel;
        private System.Windows.Forms.Panel scannerContentPanel;
        private System.Windows.Forms.DataGridView scannerDataGridView;
        private System.Windows.Forms.TextBox scannerOutputTextBox;
        private System.Windows.Forms.CheckBox showScannerOutputCheckBox;
        private System.Windows.Forms.Button previousPageButton;
        private System.Windows.Forms.Button nextPageButton;
        private System.Windows.Forms.Label pageInfoLabel;
        private System.Windows.Forms.DateTimePicker dateFromPicker;
        private System.Windows.Forms.DateTimePicker dateToPicker;
        private System.Windows.Forms.TextBox blockNumberTextBox;
        private System.Windows.Forms.TextBox lineNumberTextBox;
        private System.Windows.Forms.ComboBox productIdComboBox;
        private System.Windows.Forms.Button applyFiltersButton;
        private System.Windows.Forms.Button clearFiltersButton;
        private System.Windows.Forms.Label dateFromLabel;
        private System.Windows.Forms.Label dateToLabel;
        private System.Windows.Forms.Label blockNumberLabel;
        private System.Windows.Forms.Label lineNumberLabel;
        private System.Windows.Forms.Label productIdLabel;
        private System.Windows.Forms.Label activeScannersLabel;
        private System.Windows.Forms.Label todayScansLabel;
        private System.Windows.Forms.Label lastHourScansLabel;
        // private System.Windows.Forms.Button runScannerScriptButton;
        private System.Windows.Forms.Button Scanner;
        private System.Windows.Forms.Button Printer;
        private System.Windows.Forms.Button logoutButtonPrinter;
        private System.Windows.Forms.Button logoutButtonScanner;
        // private System.Windows.Forms.TextBox barcodeInputTextBox;
        // private System.Windows.Forms.Button sendBarcodeButton;
        private System.Windows.Forms.Button manageScannersButton;
        private System.Windows.Forms.Button button_manualUpload;

        // UI Styling Methods
        // Rounds the corners of a control by applying a Region built from a rounded rectangle path.
        private void ApplyRoundedCorners(Control control, int radius)
        {
            if (control == null || radius <= 0) return;
            control.Resize -= Control_RoundedResize;
            control.Resize += Control_RoundedResize;
            SetRoundedRegion(control, radius);
        }

        private void Control_RoundedResize(object sender, EventArgs e)
        {
            if (sender is Control c)
            {
                // Default radius of 8 for panels/group boxes, 12 for buttons
                int radius = (c is Button) ? 12 : 8;
                SetRoundedRegion(c, radius);
            }
        }

        private void SetRoundedRegion(Control control, int radius)
        {
            if (control.Width < 2 || control.Height < 2) return;
            int diameter = radius * 2;
            using (GraphicsPath path = new GraphicsPath())
            {
                path.StartFigure();
                path.AddArc(new Rectangle(0, 0, diameter, diameter), 180, 90);
                path.AddArc(new Rectangle(control.Width - diameter, 0, diameter, diameter), 270, 90);
                path.AddArc(new Rectangle(control.Width - diameter, control.Height - diameter, diameter, diameter), 0, 90);
                path.AddArc(new Rectangle(0, control.Height - diameter, diameter, diameter), 90, 90);
                path.CloseFigure();
                control.Region = new Region(path);
            }
        }

        private void ApplyModernStylesToButtons()
        {
            // Colors (2025 minimal UI palette)
            Color primary = Color.FromArgb(50, 74, 95);           // base primary (navy)
            Color primaryHover = Color.FromArgb(27, 42, 65);      // hover primary (navy darker)
            Color danger = Color.FromArgb(231, 76, 60);           // base danger
            Color dangerHover = Color.FromArgb(192, 57, 43);      // hover danger
            Color success = Color.FromArgb(46, 204, 113);         // success green
            Color successHover = Color.FromArgb(39, 174, 96);     // success green (darker)
            Color textOnPrimary = Color.White;

            // Primary buttons
            StylePrimaryButton(loginButton, primary, primaryHover, textOnPrimary);
            StylePrimaryButton(printerButton, primary, primaryHover, textOnPrimary);
            StylePrimaryButton(scannerButton, primary, primaryHover, textOnPrimary);
            StylePrimaryButton(Scanner, primary, primaryHover, textOnPrimary);
            StylePrimaryButton(button_FetchEmployees, primary, primaryHover, textOnPrimary);
            StylePrimaryButton(button_setting, primary, primaryHover, textOnPrimary);
            if (button_FetchEmployees != null)
            {
                // Reduce Fetch button size subtly
                button_FetchEmployees.MinimumSize = new Size(80, 22);
                button_FetchEmployees.Height = 22;
                button_FetchEmployees.Width = 80;
                button_FetchEmployees.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                button_FetchEmployees.Padding = Padding.Empty;
                ApplyRoundedCorners(button_FetchEmployees, 1);
            }
            if (button_setting != null)
            {
                // Reduce Setting button size subtly
                button_setting.MinimumSize = new Size(80, 28);
                button_setting.Height = 28;
                button_setting.Width = 120;
                button_setting.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                button_setting.Padding = Padding.Empty;
                ApplyRoundedCorners(button_setting, 2);
            }
            StylePrimaryButton(button_send, primary, primaryHover, textOnPrimary);
            StylePrimaryButton(button_preview, primary, primaryHover, textOnPrimary);
            StylePrimaryButton(Printer, primary, primaryHover, textOnPrimary);
            StylePrimaryButton(manageScannersButton, primary, primaryHover, textOnPrimary);
            // Manual upload button - success styling
            if (button_manualUpload != null)
            {
                StylePrimaryButton(button_manualUpload, success, successHover, Color.White);
                if (button_manualUpload.Width < 130) button_manualUpload.Width = 130;
                button_manualUpload.Height = 32;
                button_manualUpload.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                ApplyRoundedCorners(button_manualUpload, 12);
            }

            // Danger/Logout buttons
            StyleDangerButton(logoutButtonStart, danger, dangerHover);
            StyleDangerButton(logoutButtonPrinter, danger, dangerHover);
            StyleDangerButton(logoutButtonScanner, danger, dangerHover);

            // Contrast tweaks per-context
            // Login (on white) - brighter blue for stronger contrast
            if (loginButton != null)
            {
                loginButton.BackColor = Color.FromArgb(32, 101, 209);
                loginButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(28, 92, 190);
                loginButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(24, 82, 170);
                loginButton.ForeColor = Color.White;
                if (loginButton.Width < 160) loginButton.Width = 160;
                if (loginButton.Height < 40) loginButton.Height = 40;
            }

            // Start panel buttons - distinct hues for clarity on white
            if (printerButton != null)
            {
                printerButton.BackColor = Color.FromArgb(46, 164, 79);
                printerButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(41, 148, 71);
                printerButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(36, 131, 63);
                printerButton.ForeColor = Color.White;
                if (printerButton.Height < 44) printerButton.Height = 44;
            }
            if (scannerButton != null)
            {
                scannerButton.BackColor = Color.FromArgb(108, 92, 231);
                scannerButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(96, 82, 205);
                scannerButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(83, 71, 178);
                scannerButton.ForeColor = Color.White;
                if (scannerButton.Height < 44) scannerButton.Height = 44;
            }

            // Back buttons - ensure visibility
            if (Scanner != null)
            {
                Scanner.ForeColor = Color.White;
                if (Scanner.Width < 110) Scanner.Width = 110;
                if (Scanner.Height < 36) Scanner.Height = 36;
            }
            if (Printer != null)
            {
                Printer.ForeColor = Color.White;
                if (Printer.Width < 110) Printer.Width = 110;
                Printer.Height = 32;
                Printer.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            }

            // Manage scanners - make prominent
            if (manageScannersButton != null)
            {
                manageScannersButton.Padding = Padding.Empty;
                manageScannersButton.BackColor = Color.FromArgb(32, 101, 209);
                manageScannersButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(28, 92, 190);
                manageScannersButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(24, 82, 170);
                manageScannersButton.ForeColor = Color.White;
                if (manageScannersButton.Width < 130) manageScannersButton.Width = 130;
                manageScannersButton.Height = 32;
                manageScannersButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            }
            
            // Ensure logout buttons are fully visible sizes
            if (logoutButtonStart != null)
            {
                if (logoutButtonStart.Width < 100) logoutButtonStart.Width = 100;
                logoutButtonStart.Height = 36;
                // move away from edges for visibility
                logoutButtonStart.Location = new Point(Math.Max(20, logoutButtonStart.Left), Math.Max(20, logoutButtonStart.Top));
            }
            if (logoutButtonPrinter != null)
            {
                if (logoutButtonPrinter.Width < 100) logoutButtonPrinter.Width = 100;
                if (logoutButtonPrinter.Height < 36) logoutButtonPrinter.Height = 36;
            }
            if (logoutButtonScanner != null)
            {
                if (logoutButtonScanner.Width < 100) logoutButtonScanner.Width = 100;
                logoutButtonScanner.Height = 32;
                logoutButtonScanner.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            }
        }

        private void StylePrimaryButton(Button button, Color bg, Color hoverBg, Color fg)
        {
            if (button == null) return;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = hoverBg;
            button.FlatAppearance.MouseDownBackColor = hoverBg;
            button.BackColor = bg;
            button.ForeColor = fg;
            float desired = Math.Max(button.Font?.Size ?? 9f, 10.5f);
            button.Font = new Font("Segoe UI", desired, FontStyle.Bold);
            button.Padding = new Padding(12, 6, 12, 6);
            // Keep generous defaults, but allow small overrides for specific buttons
            if (button != button_FetchEmployees && button != button_setting)
            {
                button.MinimumSize = new Size(Math.Max(button.MinimumSize.Width, 100), 40);
                button.Height = Math.Max(button.Height, 40);
            }
            button.UseVisualStyleBackColor = false;
            ApplyRoundedCorners(button, 12);
        }

        private void StyleDangerButton(Button button, Color bg, Color hoverBg)
        {
            if (button == null) return;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = hoverBg;
            button.FlatAppearance.MouseDownBackColor = hoverBg;
            button.BackColor = bg;
            button.ForeColor = Color.White;
            button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            button.Padding = new Padding(12, 6, 12, 6);
            button.MinimumSize = new Size(button.MinimumSize.Width, 36);
            button.Height = Math.Max(button.Height, 36);
            button.UseVisualStyleBackColor = false;
            ApplyRoundedCorners(button, 12);
        }

        // UI Layout Methods
        private void LayoutRootPanels()
        {
            try
            {
                int cw = this.ClientSize.Width;
                int ch = this.ClientSize.Height;
                int marginX = 20;
                int topMargin = 30;
                int spacing = 20;

                // Center contents of loginPanel
                if (loginPanel != null && loginPanel.Visible)
                {
                    if (loginWelcomeLabel != null)
                    {
                        loginWelcomeLabel.Left = Math.Max(marginX, (cw - loginWelcomeLabel.Width) / 2);
                        loginWelcomeLabel.Top = topMargin;
                    }
                    if (loginMainLogoPictureBox != null)
                    {
                        loginMainLogoPictureBox.Left = Math.Max(marginX, (cw - loginMainLogoPictureBox.Width) / 2);
                        loginMainLogoPictureBox.Top = (loginWelcomeLabel?.Bottom ?? topMargin) + spacing;
                    }  
                    if (loginGroupBox != null)
                    {
                        loginGroupBox.Left = Math.Max(marginX, (cw - loginGroupBox.Width) / 2);
                        loginGroupBox.Top = (loginMainLogoPictureBox?.Bottom ?? topMargin) + spacing * 2;
                    }
                    if (loginStatusLabel != null)
                    {
                        // Position just below the login group box
                        loginStatusLabel.Left = (loginGroupBox != null) ? loginGroupBox.Left : Math.Max(marginX, (cw - loginStatusLabel.Width) / 2);
                        int desiredTop = (loginGroupBox?.Bottom ?? topMargin) + spacing;
                        loginStatusLabel.Top = Math.Min(ch - loginStatusLabel.Height - spacing, desiredTop);
                    }
                }

                // Center contents of startPanel
                if (startPanel != null && startPanel.Visible)
                {
                    int spw = startPanel.ClientSize.Width;
                    if (logoutButtonStart != null)
                    {
                        logoutButtonStart.Left = Math.Max(marginX, spw - logoutButtonStart.Width - marginX);
                        logoutButtonStart.Top = topMargin;
                        logoutButtonStart.BringToFront();
                    }
                    
                    // Place startpanelLogoPictureBox at the top, below logout button
                    if (startpanelLogoPictureBox != null)
                    {
                        startpanelLogoPictureBox.Left = Math.Max(marginX, (spw - startpanelLogoPictureBox.Width) / 2);
                        int logoTop = topMargin;
                        if (logoutButtonStart != null)
                        {
                            logoTop = logoutButtonStart.Bottom + spacing;
                        }
                        startpanelLogoPictureBox.Top = logoTop;
                    }
                    
                    if (welcomeLabel != null)
                    {
                        welcomeLabel.Left = Math.Max(marginX, (spw - welcomeLabel.Width) / 2);
                        // Place welcome label below the logo
                        int desiredTop = topMargin;
                        if (startpanelLogoPictureBox != null)
                        {
                            desiredTop = startpanelLogoPictureBox.Bottom + spacing;
                        }
                        else if (logoutButtonStart != null)
                        {
                            desiredTop = logoutButtonStart.Bottom + spacing;
                        }
                        welcomeLabel.Top = desiredTop;
                    }
                    
                    if (logoPictureBox != null)
                    {
                        logoPictureBox.Left = Math.Max(marginX, (spw - logoPictureBox.Width) / 2);
                        logoPictureBox.Top = (welcomeLabel?.Bottom ?? topMargin) + spacing;
                    }
                    if (printerButton != null)
                    {
                        printerButton.Left = Math.Max(marginX, (spw - printerButton.Width) / 2);
                        printerButton.Top = (welcomeLabel?.Bottom ?? topMargin) + spacing * 2;
                    }
                    if (scannerButton != null)
                    {
                        scannerButton.Left = Math.Max(marginX, (spw - scannerButton.Width) / 2);
                        scannerButton.Top = (printerButton?.Bottom ?? topMargin) + spacing;
                    }
                }

                // Printer content header buttons
                if (printerContentPanel != null && printerContentPanel.Visible)
                {
                    if (Scanner != null)
                    {
                        Scanner.Left = Math.Max(marginX, cw - Scanner.Width - marginX);
                        Scanner.Top = topMargin + 20;
                    }
                }

                // Scanner content layout
                if (scannerContentPanel != null && scannerContentPanel.Visible)
                {
                    int availableW = Math.Max(320, cw - 2 * marginX);

                    // Header buttons at top
                    if (Printer != null)
                    {
                        Printer.Left = Math.Max(marginX, cw - Printer.Width - marginX);
                        Printer.Top = topMargin;
                    }
                    if (logoutButtonScanner != null)
                    {
                        int rightOfLogout = (Printer?.Width ?? 0) + spacing;
                        logoutButtonScanner.Left = Math.Max(marginX, cw - logoutButtonScanner.Width - marginX - rightOfLogout);
                        logoutButtonScanner.Top = topMargin;
                    }
                    if (manageScannersButton != null)
                    {
                        manageScannersButton.Left = marginX;
                        manageScannersButton.Top = topMargin;
                    }
                    if (button_manualUpload != null)
                    {
                        // Place manual upload next to manageScannersButton and keep aligned on resize
                        button_manualUpload.Left = (manageScannersButton != null) ? manageScannersButton.Right + spacing : marginX;
                        button_manualUpload.Top = topMargin;
                    }

                    int headerBottom = topMargin;
                    if (manageScannersButton != null) headerBottom = Math.Max(headerBottom, manageScannersButton.Bottom);
                    if (button_manualUpload != null) headerBottom = Math.Max(headerBottom, button_manualUpload.Bottom);
                    if (Printer != null) headerBottom = Math.Max(headerBottom, Printer.Bottom);
                    if (logoutButtonScanner != null) headerBottom = Math.Max(headerBottom, logoutButtonScanner.Bottom);

                    // Position checkbox and expand widths to available width
                    if (showScannerOutputCheckBox != null)
                    {
                        showScannerOutputCheckBox.Left = marginX;
                        showScannerOutputCheckBox.Top = headerBottom + spacing;
                    }
                    if (scannerOutputTextBox != null)
                    {
                        scannerOutputTextBox.Left = marginX;
                        scannerOutputTextBox.Width = availableW;
                        scannerOutputTextBox.Top = (showScannerOutputCheckBox?.Bottom ?? headerBottom) + spacing;
                    }
                    // Position filter controls in one horizontal line
                    int filterControlsTop = (showScannerOutputCheckBox?.Bottom ?? headerBottom) + spacing;
                    if (showScannerOutputCheckBox != null && showScannerOutputCheckBox.Checked && scannerOutputTextBox != null)
                    {
                        // When textbox is visible, position filters below it
                        filterControlsTop = scannerOutputTextBox.Bottom + spacing;
                    }
                    else if (showScannerOutputCheckBox != null && !showScannerOutputCheckBox.Checked)
                    {
                        // When textbox is hidden, move filters up to utilize the space
                        // Position filters right below the checkbox to maximize space usage
                        filterControlsTop = (showScannerOutputCheckBox?.Bottom ?? headerBottom) + spacing;
                    }
                    
                    // Calculate positions for one-line layout with labels inline
                    int currentX = marginX;
                    int labelControlSpacing = 8; // Spacing between label and control
                    
                    // Calculate total width needed for all controls
                    int totalControlWidth = 0;
                    if (dateFromLabel != null) totalControlWidth += dateFromLabel.Width + labelControlSpacing;
                    if (dateFromPicker != null) totalControlWidth += dateFromPicker.Width;
                    if (dateToLabel != null) totalControlWidth += dateToLabel.Width + labelControlSpacing;
                    if (dateToPicker != null) totalControlWidth += dateToPicker.Width;
                    if (blockNumberLabel != null) totalControlWidth += blockNumberLabel.Width + labelControlSpacing;
                    if (blockNumberTextBox != null) totalControlWidth += blockNumberTextBox.Width;
                    if (lineNumberLabel != null) totalControlWidth += lineNumberLabel.Width + labelControlSpacing;
                    if (lineNumberTextBox != null) totalControlWidth += lineNumberTextBox.Width;
                    if (productIdLabel != null) totalControlWidth += productIdLabel.Width + labelControlSpacing;
                    if (productIdComboBox != null) totalControlWidth += productIdComboBox.Width;
                    if (applyFiltersButton != null) totalControlWidth += applyFiltersButton.Width;
                    if (clearFiltersButton != null) totalControlWidth += clearFiltersButton.Width;
                    
                    // Calculate optimal spacing to utilize full width
                    int availableWidth = availableW - (marginX * 2);
                    int remainingWidth = availableWidth - totalControlWidth;
                    int numberOfGaps = 6; // Number of gaps between filter groups
                    int controlSpacing = Math.Max(15, remainingWidth / numberOfGaps); // Minimum 15px spacing
                    
                    // Date From
                    if (dateFromLabel != null)
                    {
                        dateFromLabel.Left = currentX;
                        dateFromLabel.Top = filterControlsTop;
                        currentX += dateFromLabel.Width + labelControlSpacing;
                    }
                    if (dateFromPicker != null)
                    {
                        dateFromPicker.Left = currentX;
                        dateFromPicker.Top = filterControlsTop;
                        currentX += dateFromPicker.Width + controlSpacing;
                    }
                    
                    // Date To
                    if (dateToLabel != null)
                    {
                        dateToLabel.Left = currentX;
                        dateToLabel.Top = filterControlsTop;
                        currentX += dateToLabel.Width + labelControlSpacing;
                    }
                    if (dateToPicker != null)
                    {
                        dateToPicker.Left = currentX;
                        dateToPicker.Top = filterControlsTop;
                        currentX += dateToPicker.Width + controlSpacing;
                    }
                    
                    // Block Number
                    if (blockNumberLabel != null)
                    {
                        blockNumberLabel.Left = currentX;
                        blockNumberLabel.Top = filterControlsTop;
                        currentX += blockNumberLabel.Width + labelControlSpacing;
                    }
                    if (blockNumberTextBox != null)
                    {
                        blockNumberTextBox.Left = currentX;
                        blockNumberTextBox.Top = filterControlsTop;
                        currentX += blockNumberTextBox.Width + controlSpacing;
                    }
                    
                    // Line Number
                    if (lineNumberLabel != null)
                    {
                        lineNumberLabel.Left = currentX;
                        lineNumberLabel.Top = filterControlsTop;
                        currentX += lineNumberLabel.Width + labelControlSpacing;
                    }
                    if (lineNumberTextBox != null)
                    {
                        lineNumberTextBox.Left = currentX;
                        lineNumberTextBox.Top = filterControlsTop;
                        currentX += lineNumberTextBox.Width + controlSpacing;
                    }
                    
                    // Product ID
                    if (productIdLabel != null)
                    {
                        productIdLabel.Left = currentX;
                        productIdLabel.Top = filterControlsTop;
                        currentX += productIdLabel.Width + labelControlSpacing;
                    }
                    if (productIdComboBox != null)
                    {
                        productIdComboBox.Left = currentX;
                        productIdComboBox.Top = filterControlsTop;
                        currentX += productIdComboBox.Width + controlSpacing;
                    }
                    
                    // Apply and Clear buttons
                    if (applyFiltersButton != null)
                    {
                        applyFiltersButton.Left = currentX;
                        applyFiltersButton.Top = filterControlsTop;
                        currentX += applyFiltersButton.Width + controlSpacing;
                    }
                    if (clearFiltersButton != null)
                    {
                        clearFiltersButton.Left = currentX;
                        clearFiltersButton.Top = filterControlsTop;
                    }
                    
                    if (scannerDataGridView != null)
                    {
                        scannerDataGridView.Left = marginX;
                        // Ensure DataGridView uses full available width for maximum space utilization
                        scannerDataGridView.Width = Math.Max(560, cw - 2 * marginX);
                        // Position DataGridView below count labels
                        scannerDataGridView.Top = filterControlsTop + 80;
                        
                        // Adjust DataGridView height to leave space for pagination controls
                        int paginationControlsHeight = 50; // Space for pagination controls
                        scannerDataGridView.Height = Math.Max(200, ch - scannerDataGridView.Top - paginationControlsHeight - spacing * 2);
                    }
                    
                    // Position pagination controls below DataGridView
                    if (previousPageButton != null)
                    {
                        previousPageButton.Left = marginX;
                        previousPageButton.Top = (scannerDataGridView?.Bottom ?? headerBottom) + spacing;
                    }
                    if (nextPageButton != null)
                    {
                        nextPageButton.Left = marginX + availableW - nextPageButton.Width;
                        nextPageButton.Top = (scannerDataGridView?.Bottom ?? headerBottom) + spacing;
                    }
                    if (pageInfoLabel != null)
                    {
                        pageInfoLabel.Left = marginX + (availableW - pageInfoLabel.Width) / 2;
                        pageInfoLabel.Top = (scannerDataGridView?.Bottom ?? headerBottom) + spacing + 5;
                    }
                    
                    // Update DataGridView column widths to utilize full available space
                    UpdateDataGridViewColumnWidths();
                    
                    // Update count labels
                    UpdateActiveScannersCount();
                    UpdateCountLabels();
                    
                    // Position count labels below the filter controls
                    int countLabelsTop = filterControlsTop + 50;
                    if (activeScannersLabel != null)
                    {
                        activeScannersLabel.Left = marginX;
                        activeScannersLabel.Top = countLabelsTop;
                        activeScannersLabel.Visible = true;
                    }
                    if (todayScansLabel != null)
                    {
                        todayScansLabel.Left = marginX + 200;
                        todayScansLabel.Top = countLabelsTop;
                        todayScansLabel.Visible = true;
                    }
                    if (lastHourScansLabel != null)
                    {
                        lastHourScansLabel.Left = marginX + 400;
                        lastHourScansLabel.Top = countLabelsTop;
                        lastHourScansLabel.Visible = true;
                    }
                }

                // Printer content widths and centering
                if (printerContentPanel != null && printerContentPanel.Visible)
                {
                    LayoutPrinterContent(cw);
                }
            }
            catch { }
        }

        private void LayoutPrinterContent(int clientWidth)
        {
            try
            {
                int mainInnerW = Math.Max(320, (mainPanel?.ClientSize.Width ?? clientWidth) - (mainPanel?.Padding.Left ?? 0) - (mainPanel?.Padding.Right ?? 0));
                int fixedWidth = 560; // enforce fixed 560px width for core sections
                int desired = Math.Min(fixedWidth, Math.Max(420, mainInnerW));
                int centerX = Math.Max(20, ((mainPanel?.ClientSize.Width ?? clientWidth) - desired) / 2);

                // Fix and center container sections to 560px
                int y = mainPanel?.Padding.Top ?? 20;
                if (headerPanel != null)
                {
                    headerPanel.Dock = DockStyle.None;
                    headerPanel.Anchor = AnchorStyles.Top;
                    headerPanel.Width = fixedWidth;
                    headerPanel.Left = centerX;
                    headerPanel.Top = y;
                    // Ensure printer header buttons are inside headerPanel
                    if (Scanner != null && Scanner.Parent != headerPanel)
                    {
                        headerPanel.Controls.Add(Scanner);
                    }
                    if (logoutButtonPrinter != null && logoutButtonPrinter.Parent != headerPanel)
                    {
                        headerPanel.Controls.Add(logoutButtonPrinter);
                    }

                    // Position header buttons (right-aligned cluster)
                    int headerSpacing = 10;
                    int headerPaddingRight = 30;
                    int headerTop = 20;
                    if (logoutButtonPrinter != null)
                    {
                        if (logoutButtonPrinter.Height != 32) logoutButtonPrinter.Height = 32;
                        if (logoutButtonPrinter.Width < 90) logoutButtonPrinter.Width = 90;
                        logoutButtonPrinter.Top = headerTop;
                        logoutButtonPrinter.Left = headerPanel.Width - logoutButtonPrinter.Width - headerPaddingRight;
                    }
                    if (Scanner != null)
                    {
                        if (Scanner.Height != 32) Scanner.Height = 32;
                        if (Scanner.Width < 90) Scanner.Width = 90;
                        Scanner.Top = headerTop;
                        int rightNeighborLeft = (logoutButtonPrinter != null) ? logoutButtonPrinter.Left : (headerPanel.Width - headerPaddingRight);
                        Scanner.Left = Math.Max(10, rightNeighborLeft - headerSpacing - Scanner.Width);
                    }

                    y = headerPanel.Bottom + 0;
                }
                if (connectionPanel != null)
                {
                    connectionPanel.Dock = DockStyle.None;
                    connectionPanel.Anchor = AnchorStyles.Top;
                    connectionPanel.Width = fixedWidth;
                    connectionPanel.Left = centerX;
                    connectionPanel.Top = y;
                    y = connectionPanel.Bottom + 0;
                }
                if (configPanel != null)
                {
                    configPanel.Dock = DockStyle.None;
                    configPanel.Anchor = AnchorStyles.Top;
                    configPanel.Width = fixedWidth;
                    configPanel.Left = centerX;
                    configPanel.Top = y;
                    y = configPanel.Bottom + 0;
                }
                if (advancedPanel != null && advancedPanel.Visible)
                {
                    // Keep advanced panel stacked below config if visible
                    advancedPanel.Dock = DockStyle.None;
                    advancedPanel.Anchor = AnchorStyles.Top;
                    advancedPanel.Width = fixedWidth;
                    advancedPanel.Left = centerX;
                    advancedPanel.Top = y;
                    y = advancedPanel.Bottom + 0;
                }
                if (actionPanel != null)
                {
                    actionPanel.Dock = DockStyle.None;
                    actionPanel.Anchor = AnchorStyles.Top;
                    actionPanel.Width = fixedWidth;
                    actionPanel.Left = centerX;
                    // Reduce top padding when advanced panel is hidden to eliminate visible gap
                    if (advancedPanel != null && !advancedPanel.Visible)
                    {
                        actionPanel.Padding = new Padding(0, 10, 0, 0);
                    }
                    actionPanel.Top = y;
                    y = actionPanel.Bottom + 0;
                }
                if (statusPanel != null)
                {
                    statusPanel.Dock = DockStyle.None;
                    statusPanel.Anchor = AnchorStyles.Top;
                    statusPanel.Width = fixedWidth;
                    statusPanel.Left = centerX;
                    statusPanel.Top = y;
                    if (statusLabel != null)
                    {
                        // statusLabel is dock fill; ensure panel width is fixed
                        statusLabel.MaximumSize = new Size(fixedWidth, 0);
                    }
                }
            }
            catch { }
        }
    }
}


