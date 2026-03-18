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
            this.loginGroupBox = new System.Windows.Forms.GroupBox();
            this.loginButton = new System.Windows.Forms.Button();
            this.passwordToggleButton = new System.Windows.Forms.Button();
            this.passwordTextBox = new System.Windows.Forms.TextBox();
            this.passwordLabel = new System.Windows.Forms.Label();
            this.usernameTextBox = new System.Windows.Forms.TextBox();
            this.usernameLabel = new System.Windows.Forms.Label();
            this.loginStatusLabel = new System.Windows.Forms.Label();
            this.loadingProgressBar = new System.Windows.Forms.ProgressBar();
            this.loadingStatusLabel = new System.Windows.Forms.Label();
            this.printerLoadingSpinner = new System.Windows.Forms.ProgressBar();
            this.printerLoadingLabel = new System.Windows.Forms.Label();
            this.logoPictureBox = new System.Windows.Forms.PictureBox();
            this.dimensionVisualizationPictureBox = new System.Windows.Forms.PictureBox();
            this.dimensionVisualizationPictureBox_TwoUp = new System.Windows.Forms.PictureBox();
            this.button_generateBarcode = new System.Windows.Forms.Button();
            // this.printerContentPanel = new System.Windows.Forms.Panel();
			this.scannerContentPanel = new System.Windows.Forms.TableLayoutPanel();
			this.scannerOutputPanel = new System.Windows.Forms.Panel();
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
            this.cropIdLabel = new System.Windows.Forms.Label();
            this.cropIdComboBox = new System.Windows.Forms.ComboBox();
            this.todayScansLabel = new System.Windows.Forms.Label();
            this.lastHourScansLabel = new System.Windows.Forms.Label();
            this.totalFilteredScansLabel = new System.Windows.Forms.Label();
            this.statsPanel = new System.Windows.Forms.TableLayoutPanel();
            this.paginationPanel = new System.Windows.Forms.TableLayoutPanel();
            this.filtersPanel = new System.Windows.Forms.TableLayoutPanel();
            // this.runScannerScriptButton = new System.Windows.Forms.Button();
            this.headerPanel = new System.Windows.Forms.Panel();
            this.headerTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.titlepanel = new System.Windows.Forms.Panel();
            this.buttonpanel = new System.Windows.Forms.Panel();
            this.buttonTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.setupButton = new System.Windows.Forms.Button();
            this.scannerSetupButton = new System.Windows.Forms.Button();
            this.printerConnectionButton = new System.Windows.Forms.Button();
            this.barCodesButton = new System.Windows.Forms.Button();
            this.boxLabelsButton = new System.Windows.Forms.Button();
            this.reportsButton = new System.Windows.Forms.Button();
            this.logoutButton = new System.Windows.Forms.Button();
            this.titleLabel = new System.Windows.Forms.Label();
            this.subtitleLabel = new System.Windows.Forms.Label();
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
            this.label_xCoordinate = new System.Windows.Forms.Label();
            this.numericUpDown_xCoordinate = new System.Windows.Forms.NumericUpDown();
            this.label_x2Coordinate = new System.Windows.Forms.Label();
            this.numericUpDown_x2Coordinate = new System.Windows.Forms.NumericUpDown();
            this.label_px_width = new System.Windows.Forms.Label();
            this.label_px_height = new System.Windows.Forms.Label();
            this.label_px_xCoordinate = new System.Windows.Forms.Label();
            this.label_px_x2Coordinate = new System.Windows.Forms.Label();
            this.label_mm_gap = new System.Windows.Forms.Label();
            this.checkBox_twoUp = new System.Windows.Forms.CheckBox();
            this.label_gap = new System.Windows.Forms.Label();
            this.numericUpDown_gap = new System.Windows.Forms.NumericUpDown();
            this.qualityPanel = new System.Windows.Forms.Panel();
            this.label_darkness = new System.Windows.Forms.Label();
            this.trackBar_darkness = new System.Windows.Forms.TrackBar();
            this.label_darknessValue = new System.Windows.Forms.Label();
            this.label_speed = new System.Windows.Forms.Label();
            this.comboBox_speed = new System.Windows.Forms.ComboBox();
            this.label_dpi = new System.Windows.Forms.Label();
            this.numericUpDown_dpi = new System.Windows.Forms.NumericUpDown();
            this.previewPanel = new System.Windows.Forms.Panel();
            this.button_preview = new System.Windows.Forms.Button();
            // checkBox_showAdvanced instantiation removed
            this.actionPanel = new System.Windows.Forms.Panel();
            this.button_send = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.statusPanel = new System.Windows.Forms.Panel();
            this.statusLabel = new System.Windows.Forms.Label();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.label_CropID = new System.Windows.Forms.Label();
            this.comboBox_CropID = new System.Windows.Forms.ComboBox();
            this.cropIdLabel = new System.Windows.Forms.Label();
            this.cropIdComboBox = new System.Windows.Forms.ComboBox();
            this.label_ProductDetail = new System.Windows.Forms.Label();
            this.label_count = new System.Windows.Forms.Label();
            this.numericUpDown_count = new System.Windows.Forms.NumericUpDown();
            this.loginPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.loginMainLogoPictureBox)).BeginInit();
            this.loginGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).BeginInit();
            // this.printerContentPanel.SuspendLayout();
            this.scannerContentPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scannerDataGridView)).BeginInit();
            this.headerPanel.SuspendLayout();
            this.headerTableLayoutPanel.SuspendLayout();
            this.titlepanel.SuspendLayout();
            this.buttonpanel.SuspendLayout();
            this.buttonTableLayoutPanel.SuspendLayout();
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
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_xCoordinate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_gap)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_dpi)).BeginInit();
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
            this.loginPanel.BackColor = System.Drawing.Color.FromArgb(248, 249, 250); // Light gray enterprise bg
            this.loginPanel.Controls.Add(this.loginStatusLabel);
            this.loginPanel.Controls.Add(this.loadingProgressBar);
            this.loginPanel.Controls.Add(this.loadingStatusLabel);
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
            this.usernameTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.usernameTextBox_KeyDown);
            
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
            this.passwordTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.passwordTextBox_KeyDown);
            
            // 
            // passwordToggleButton
            // 
            this.passwordToggleButton.BackColor = System.Drawing.Color.Transparent;
            this.passwordToggleButton.FlatAppearance.BorderSize = 0;
            this.passwordToggleButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(225, 225, 225);
            this.passwordToggleButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.passwordToggleButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
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
            this.loginButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
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
            // loadingProgressBar
            //
            this.loadingProgressBar.ForeColor = System.Drawing.Color.FromArgb(0, 120, 215); // Blue color for moving bar
            this.loadingProgressBar.Location = new System.Drawing.Point(100, 720);
            this.loadingProgressBar.Name = "loadingProgressBar";
            this.loadingProgressBar.Size = new System.Drawing.Size(400, 13);
            this.loadingProgressBar.TabIndex = 4;
            this.loadingProgressBar.Visible = false;
            this.loadingProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            //
            // loadingStatusLabel
            //
            this.loadingStatusLabel.AutoSize = true;
            this.loadingStatusLabel.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.loadingStatusLabel.ForeColor = System.Drawing.Color.FromArgb(27, 42, 65);
            this.loadingStatusLabel.Location = new System.Drawing.Point(100, 750);
            this.loadingStatusLabel.Name = "loadingStatusLabel";
            this.loadingStatusLabel.Size = new System.Drawing.Size(119, 19);
            this.loadingStatusLabel.TabIndex = 5;
            this.loadingStatusLabel.Text = "Loading scanner system...";
            this.loadingStatusLabel.Visible = false;
            //
            // printerLoadingSpinner
            //
            this.printerLoadingSpinner.Location = new System.Drawing.Point(1065, 62);
            this.printerLoadingSpinner.Name = "printerLoadingSpinner";
            this.printerLoadingSpinner.Size = new System.Drawing.Size(100, 10);
            this.printerLoadingSpinner.TabIndex = 10;
            this.printerLoadingSpinner.Visible = false;
            this.printerLoadingSpinner.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.printerLoadingSpinner.MarqueeAnimationSpeed = 30;
            this.printerLoadingSpinner.ForeColor = System.Drawing.Color.FromArgb(0, 120, 215);
            //
            // printerLoadingLabel
            //
            this.printerLoadingLabel.AutoSize = true;
            this.printerLoadingLabel.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.printerLoadingLabel.ForeColor = System.Drawing.Color.White;
            this.printerLoadingLabel.Location = new System.Drawing.Point(1065, 75);
            this.printerLoadingLabel.Name = "printerLoadingLabel";
            this.printerLoadingLabel.Size = new System.Drawing.Size(119, 19);
            this.printerLoadingLabel.TabIndex = 11;
            this.printerLoadingLabel.Text = "Switching to scanner...";
            this.printerLoadingLabel.Visible = false;
            //

            
            
            
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
            // this.printerContentPanel.AutoScroll = true;
            // this.printerContentPanel.AutoScrollMinSize = new System.Drawing.Size(0, 1200);
            // this.printerContentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            // this.printerContentPanel.Location = new System.Drawing.Point(0, 0);
            // this.printerContentPanel.Name = "printerContentPanel";
            // this.printerContentPanel.Size = new System.Drawing.Size(600, 1120);
            // this.printerContentPanel.TabIndex = 1;
            // this.printerContentPanel.Visible = false;
            
            //
            // scannerContentPanel
            //
            this.scannerContentPanel.AutoScroll = true;
            this.scannerContentPanel.ColumnCount = 1;
            this.scannerContentPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.scannerContentPanel.Controls.Add(this.headerPanel, 0, 0);
            this.scannerContentPanel.Controls.Add(this.scannerOutputPanel, 0, 1);
            this.scannerContentPanel.Controls.Add(this.statsPanel, 0, 2);
            this.scannerContentPanel.Controls.Add(this.filtersPanel, 0, 3);
            this.scannerContentPanel.Controls.Add(this.scannerDataGridView, 0, 4);
            this.scannerContentPanel.Controls.Add(this.paginationPanel, 0, 5);
            this.scannerContentPanel.Controls.Add(this.statusPanel, 0, 6);
            this.scannerContentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scannerContentPanel.Location = new System.Drawing.Point(0, 0);
            this.scannerContentPanel.Name = "scannerContentPanel";
            this.scannerContentPanel.RowCount = 7;
            this.scannerContentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.scannerContentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.scannerContentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.scannerContentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.scannerContentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.scannerContentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.scannerContentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.scannerContentPanel.Size = new System.Drawing.Size(600, 680);
            this.scannerContentPanel.TabIndex = 2;
            this.scannerContentPanel.Visible = false;

            //
            // scannerOutputPanel
            //
            this.scannerOutputPanel.Controls.Add(this.showScannerOutputCheckBox);
            this.scannerOutputPanel.Controls.Add(this.scannerOutputTextBox);
            this.scannerOutputPanel.Controls.Add(this.button_manualUpload);
            this.scannerOutputPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scannerOutputPanel.Name = "scannerOutputPanel";

            // Absolute positioned controls (on top of the table layout)
            // button_manualUpload moved to scannerOutputPanel

            //
            // statsPanel
            //
            this.statsPanel.ColumnCount = 3;
            this.statsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.statsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.statsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.statsPanel.Controls.Add(this.todayScansLabel, 0, 0);
            this.statsPanel.Controls.Add(this.lastHourScansLabel, 1, 0);
            this.statsPanel.Controls.Add(this.totalFilteredScansLabel, 2, 0);
            this.statsPanel.Location = new System.Drawing.Point(20, 350);
            this.statsPanel.Name = "statsPanel";
            this.statsPanel.RowCount = 1;
            this.statsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.statsPanel.Size = new System.Drawing.Size(560, 30);
            this.statsPanel.TabIndex = 20;
            this.statsPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));

            //
            // paginationPanel
            //
            this.paginationPanel.ColumnCount = 3;
            this.paginationPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.3333F));
            this.paginationPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.3334F));
            this.paginationPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.3333F));
            this.paginationPanel.Controls.Add(this.previousPageButton, 0, 0);
            this.paginationPanel.Controls.Add(this.pageInfoLabel, 1, 0);
            this.paginationPanel.Controls.Add(this.nextPageButton, 2, 0);
            this.paginationPanel.Location = new System.Drawing.Point(20, 500);
            this.paginationPanel.Name = "paginationPanel";
            this.paginationPanel.RowCount = 1;
            this.paginationPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.paginationPanel.Size = new System.Drawing.Size(560, 30);
            this.paginationPanel.TabIndex = 21;
            this.paginationPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));

            //
            // filtersPanel
            //
            this.filtersPanel.ColumnCount = 14;
            this.filtersPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 4F));    // dateFromLabel
            this.filtersPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));    // dateFromPicker (increased)
            this.filtersPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 3F));    // dateToLabel
            this.filtersPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));    // dateToPicker (increased)
            this.filtersPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.75F));    // blockNumberLabel
            this.filtersPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 4F));   // blockNumberTextBox
            this.filtersPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.25F));    // lineNumberLabel
            this.filtersPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 4F));   // lineNumberTextBox
            this.filtersPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 4.5F));    // cropIdLabel
            this.filtersPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 11F));    // cropIdComboBox (increased)
            this.filtersPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 6F));    // productIdLabel
            this.filtersPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16F));    // productIdComboBox (increased)
            this.filtersPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 6.25F));   // applyFiltersButton
            this.filtersPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 6.25F));   // clearFiltersButton
            this.filtersPanel.Controls.Add(this.dateFromLabel, 0, 0);
            this.filtersPanel.Controls.Add(this.dateFromPicker, 1, 0);
            this.filtersPanel.Controls.Add(this.dateToLabel, 2, 0);
            this.filtersPanel.Controls.Add(this.dateToPicker, 3, 0);
            this.filtersPanel.Controls.Add(this.blockNumberLabel, 4, 0);
            this.filtersPanel.Controls.Add(this.blockNumberTextBox, 5, 0);
            this.filtersPanel.Controls.Add(this.lineNumberLabel, 6, 0);
            this.filtersPanel.Controls.Add(this.lineNumberTextBox, 7, 0);
            this.filtersPanel.Controls.Add(this.cropIdLabel, 8, 0);
            this.filtersPanel.Controls.Add(this.cropIdComboBox, 9, 0);
            this.filtersPanel.Controls.Add(this.productIdLabel, 10, 0);
            this.filtersPanel.Controls.Add(this.productIdComboBox, 11, 0);
            this.filtersPanel.Controls.Add(this.applyFiltersButton, 12, 0);
            this.filtersPanel.Controls.Add(this.clearFiltersButton, 13, 0);
            this.filtersPanel.Location = new System.Drawing.Point(20, 200);
            this.filtersPanel.Name = "filtersPanel";
            this.filtersPanel.RowCount = 1;
            this.filtersPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.filtersPanel.Size = new System.Drawing.Size(560, 30);
            this.filtersPanel.TabIndex = 22;
            this.filtersPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));

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
			this.button_manualUpload.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			// 
            
            // 
            // scannerDataGridView
            // 
            // this.scannerDataGridView.AutoScroll = true;
            this.scannerDataGridView.AllowUserToAddRows = false;
            this.scannerDataGridView.AllowUserToDeleteRows = false;
            this.scannerDataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.scannerDataGridView.BackgroundColor = System.Drawing.Color.White;
            this.scannerDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.scannerDataGridView.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.scannerDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.scannerDataGridView.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(248, 249, 250);
            this.scannerDataGridView.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(33, 37, 41);
            this.scannerDataGridView.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.scannerDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.scannerDataGridView.ColumnHeadersHeight = 30;
            this.scannerDataGridView.EnableHeadersVisualStyles = false;
            this.scannerDataGridView.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(232, 240, 254);
            this.scannerDataGridView.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.FromArgb(33, 37, 41);
            this.scannerDataGridView.DefaultCellStyle.Padding = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.scannerDataGridView.RowTemplate.Height = 28;
            this.scannerDataGridView.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(250, 250, 251);
            this.scannerDataGridView.GridColor = System.Drawing.Color.FromArgb(233, 236, 239);
            this.scannerDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scannerDataGridView.Location = new System.Drawing.Point(0, 220); // Removed padding offset
            this.scannerDataGridView.Name = "scannerDataGridView";
            this.scannerDataGridView.ReadOnly = true;
            this.scannerDataGridView.RowHeadersVisible = false;
            this.scannerDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.scannerDataGridView.Size = new System.Drawing.Size(600, 300); // Full width
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
            this.showScannerOutputCheckBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
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
            this.previousPageButton.BackColor = System.Drawing.Color.White;
            this.previousPageButton.FlatAppearance.BorderSize = 1;
            this.previousPageButton.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(206, 212, 218);
            this.previousPageButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.previousPageButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.previousPageButton.ForeColor = System.Drawing.Color.FromArgb(33, 37, 41);
            this.previousPageButton.Location = new System.Drawing.Point(20, 500);
            this.previousPageButton.Name = "previousPageButton";
            this.previousPageButton.Size = new System.Drawing.Size(80, 26);
            this.previousPageButton.TabIndex = 5;
            this.previousPageButton.Text = "Previous";
            this.previousPageButton.UseVisualStyleBackColor = false;
            this.previousPageButton.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.previousPageButton.Click += new System.EventHandler(this.previousPageButton_Click);
            
            // 
            // nextPageButton
            // 
            this.nextPageButton.BackColor = System.Drawing.Color.White;
            this.nextPageButton.FlatAppearance.BorderSize = 1;
            this.nextPageButton.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(206, 212, 218);
            this.nextPageButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.nextPageButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.nextPageButton.ForeColor = System.Drawing.Color.FromArgb(33, 37, 41);
            this.nextPageButton.Location = new System.Drawing.Point(500, 500);
            this.nextPageButton.Name = "nextPageButton";
            this.nextPageButton.Size = new System.Drawing.Size(80, 26);
            this.nextPageButton.TabIndex = 6;
            this.nextPageButton.Text = "Next";
            this.nextPageButton.UseVisualStyleBackColor = false;
            this.nextPageButton.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.nextPageButton.Click += new System.EventHandler(this.nextPageButton_Click);
            
            // 
            // pageInfoLabel
            // 
            this.pageInfoLabel.AutoSize = true;
            this.pageInfoLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.pageInfoLabel.ForeColor = System.Drawing.Color.FromArgb(108, 117, 125); // Muted text
            this.pageInfoLabel.Location = new System.Drawing.Point(250, 505);
            this.pageInfoLabel.Name = "pageInfoLabel";
            this.pageInfoLabel.Size = new System.Drawing.Size(100, 19);
            this.pageInfoLabel.TabIndex = 7;
            this.pageInfoLabel.Text = "Page 1 of 1";
            this.pageInfoLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.pageInfoLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            
            // 
            // dateFromLabel
            // 
            this.dateFromLabel.AutoSize = true;
            this.dateFromLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
            this.dateFromLabel.ForeColor = System.Drawing.Color.Black;
            this.dateFromLabel.Location = new System.Drawing.Point(20, 230);
            this.dateFromLabel.Name = "dateFromLabel";
            this.dateFromLabel.Size = new System.Drawing.Size(35, 15);
            this.dateFromLabel.TabIndex = 8;
            this.dateFromLabel.Text = "From:";
            this.dateFromLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.dateFromLabel.Anchor = System.Windows.Forms.AnchorStyles.Right;
            
            // 
            // dateFromPicker
            // 
            this.dateFromPicker.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
            this.dateFromPicker.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateFromPicker.Location = new System.Drawing.Point(20, 250);
            this.dateFromPicker.Name = "dateFromPicker";
            this.dateFromPicker.Size = new System.Drawing.Size(120, 23);
            this.dateFromPicker.TabIndex = 9;
            this.dateFromPicker.Anchor = System.Windows.Forms.AnchorStyles.Right;
            
            // 
            // dateToLabel
            // 
            this.dateToLabel.AutoSize = true;
            this.dateToLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
            this.dateToLabel.ForeColor = System.Drawing.Color.Black;
            this.dateToLabel.Location = new System.Drawing.Point(160, 230);
            this.dateToLabel.Name = "dateToLabel";
            this.dateToLabel.Size = new System.Drawing.Size(22, 15);
            this.dateToLabel.TabIndex = 10;
            this.dateToLabel.Text = "To:";
            this.dateToLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.dateToLabel.Anchor = System.Windows.Forms.AnchorStyles.Right;
            
            // 
            // dateToPicker
            // 
            this.dateToPicker.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
            this.dateToPicker.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateToPicker.Location = new System.Drawing.Point(160, 250);
            this.dateToPicker.Name = "dateToPicker";
            this.dateToPicker.Size = new System.Drawing.Size(120, 23);
            this.dateToPicker.TabIndex = 11;
            this.dateToPicker.Anchor = System.Windows.Forms.AnchorStyles.Right;
            
            // 
            // blockNumberLabel
            // 
            this.blockNumberLabel.AutoSize = true;
            this.blockNumberLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
            this.blockNumberLabel.ForeColor = System.Drawing.Color.Black;
            this.blockNumberLabel.Location = new System.Drawing.Point(300, 230);
            this.blockNumberLabel.Name = "blockNumberLabel";
            this.blockNumberLabel.Size = new System.Drawing.Size(80, 15);
            this.blockNumberLabel.TabIndex = 12;
            this.blockNumberLabel.Text = "BlockNumber:";
            this.blockNumberLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.blockNumberLabel.Anchor = System.Windows.Forms.AnchorStyles.Right;
            
            // 
            // blockNumberTextBox
            // 
            this.blockNumberTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
            this.blockNumberTextBox.Location = new System.Drawing.Point(300, 250);
            this.blockNumberTextBox.Name = "blockNumberTextBox";
            this.blockNumberTextBox.Size = new System.Drawing.Size(50, 23);
            this.blockNumberTextBox.TabIndex = 13;
            this.blockNumberTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.blockNumberTextBox.Anchor = System.Windows.Forms.AnchorStyles.Right;
            
            // 
            // lineNumberLabel
            // 
            this.lineNumberLabel.AutoSize = true;
            this.lineNumberLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
            this.lineNumberLabel.ForeColor = System.Drawing.Color.Black;
            this.lineNumberLabel.Location = new System.Drawing.Point(420, 230);
            this.lineNumberLabel.Name = "lineNumberLabel";
            this.lineNumberLabel.Size = new System.Drawing.Size(150, 15);
            this.lineNumberLabel.TabIndex = 14;
            this.lineNumberLabel.Text = "LineNumber:";
            this.lineNumberLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lineNumberLabel.Anchor = System.Windows.Forms.AnchorStyles.Right;
            
            // 
            // lineNumberTextBox
            // 
            this.lineNumberTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
            this.lineNumberTextBox.Location = new System.Drawing.Point(420, 250);
            this.lineNumberTextBox.Name = "lineNumberTextBox";
            this.lineNumberTextBox.Size = new System.Drawing.Size(50, 23);
            this.lineNumberTextBox.TabIndex = 15;
            this.lineNumberTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.lineNumberTextBox.Anchor = System.Windows.Forms.AnchorStyles.Right;
            
            // 
            // productIdLabel
            //
            this.productIdLabel.AutoSize = true;
            this.productIdLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
            this.productIdLabel.ForeColor = System.Drawing.Color.Black;
            this.productIdLabel.Location = new System.Drawing.Point(150, 290);
            this.productIdLabel.Name = "productIdLabel";
            this.productIdLabel.Size = new System.Drawing.Size(60, 15);
            this.productIdLabel.TabIndex = 16;
            this.productIdLabel.Text = "Products:";
            this.productIdLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.productIdLabel.Anchor = System.Windows.Forms.AnchorStyles.Right;
            
            
            // 
            // productIdComboBox
            //
            this.productIdComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.productIdComboBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
            this.productIdComboBox.FormattingEnabled = true;
            this.productIdComboBox.Location = new System.Drawing.Point(150, 310);
            this.productIdComboBox.Name = "productIdComboBox";
            this.productIdComboBox.Size = new System.Drawing.Size(200, 23);
            this.productIdComboBox.TabIndex = 17;
            this.productIdComboBox.Anchor = System.Windows.Forms.AnchorStyles.Right;

            // 
            // cropIdLabel
            //
            this.cropIdLabel.AutoSize = true;
            this.cropIdLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
            this.cropIdLabel.ForeColor = System.Drawing.Color.Black;
            this.cropIdLabel.Location = new System.Drawing.Point(20, 290);
            this.cropIdLabel.Name = "cropIdLabel";
            this.cropIdLabel.Size = new System.Drawing.Size(55, 15);
            this.cropIdLabel.TabIndex = 18;
            this.cropIdLabel.Text = "Crops:";
            this.cropIdLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.cropIdLabel.Anchor = System.Windows.Forms.AnchorStyles.Right;

            // 
            // cropIdComboBox
            //
            this.cropIdComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cropIdComboBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
            this.cropIdComboBox.FormattingEnabled = true;
            this.cropIdComboBox.Location = new System.Drawing.Point(20, 310);
            this.cropIdComboBox.Name = "cropIdComboBox";
            this.cropIdComboBox.Size = new System.Drawing.Size(120, 23);
            this.cropIdComboBox.TabIndex = 19;
            this.cropIdComboBox.Anchor = System.Windows.Forms.AnchorStyles.Right;
            
            // 
            // applyFiltersButton
            // 
            this.applyFiltersButton.BackColor = System.Drawing.Color.FromArgb(13, 110, 253);
            this.applyFiltersButton.FlatAppearance.BorderSize = 0;
            this.applyFiltersButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.applyFiltersButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.applyFiltersButton.ForeColor = System.Drawing.Color.White;
            this.applyFiltersButton.Location = new System.Drawing.Point(280, 310);
            this.applyFiltersButton.Name = "applyFiltersButton";
            this.applyFiltersButton.Size = new System.Drawing.Size(80, 24);
            this.applyFiltersButton.TabIndex = 18;
            this.applyFiltersButton.Text = "Apply";
            this.applyFiltersButton.TextAlign = System.Drawing.ContentAlignment.MiddleCenter; // Centered
            this.applyFiltersButton.UseVisualStyleBackColor = false;
            this.applyFiltersButton.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.applyFiltersButton.Click += new System.EventHandler(this.applyFiltersButton_Click);
            this.cropIdComboBox.SelectedIndexChanged += new System.EventHandler(this.cropIdComboBox_SelectedIndexChanged);
            this.productIdComboBox.SelectedIndexChanged += new System.EventHandler(this.productIdComboBox_SelectedIndexChanged);

            //
            // clearFiltersButton
            // 
            this.clearFiltersButton.BackColor = System.Drawing.Color.White;
            this.clearFiltersButton.FlatAppearance.BorderSize = 1;
            this.clearFiltersButton.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(206, 212, 218);
            this.clearFiltersButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.clearFiltersButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.clearFiltersButton.ForeColor = System.Drawing.Color.FromArgb(33, 37, 41);
            this.clearFiltersButton.Location = new System.Drawing.Point(370, 310);
            this.clearFiltersButton.Name = "clearFiltersButton";
            this.clearFiltersButton.Size = new System.Drawing.Size(80, 24);
            this.clearFiltersButton.TabIndex = 19;
            this.clearFiltersButton.Text = "Clear";
            this.clearFiltersButton.TextAlign = System.Drawing.ContentAlignment.MiddleCenter; // Centered
            this.clearFiltersButton.UseVisualStyleBackColor = false;
            this.clearFiltersButton.Click += new System.EventHandler(this.clearFiltersButton_Click);
            
            
            
            // 
            // todayScansLabel
            // 
            this.todayScansLabel.AutoSize = true;
            this.todayScansLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
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
            this.lastHourScansLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
            this.lastHourScansLabel.ForeColor = System.Drawing.Color.FromArgb(0, 120, 215);
            this.lastHourScansLabel.Location = new System.Drawing.Point(380, 350);
            this.lastHourScansLabel.Name = "lastHourScansLabel";
            this.lastHourScansLabel.Size = new System.Drawing.Size(120, 19);
            this.lastHourScansLabel.TabIndex = 22;
            this.lastHourScansLabel.Text = "Last Hour Scans: 0";
            this.lastHourScansLabel.Visible = true;

            //
            // totalFilteredScansLabel
            //
            this.totalFilteredScansLabel.AutoSize = true;
            this.totalFilteredScansLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
            this.totalFilteredScansLabel.ForeColor = System.Drawing.Color.FromArgb(0, 120, 215);
            this.totalFilteredScansLabel.Location = new System.Drawing.Point(560, 350);
            this.totalFilteredScansLabel.Name = "totalFilteredScansLabel";
            this.totalFilteredScansLabel.Size = new System.Drawing.Size(140, 19);
            this.totalFilteredScansLabel.TabIndex = 23;
            this.totalFilteredScansLabel.Text = "Total filtered Scans: 0";
            this.totalFilteredScansLabel.Visible = true;

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
            // headerPanel
            // 
            this.headerPanel.BackColor = System.Drawing.Color.FromArgb(248, 249, 250);
            this.headerPanel.Controls.Add(this.headerTableLayoutPanel);
            this.headerPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headerPanel.Location = new System.Drawing.Point(0, 0); // Removed padding gap
            this.headerPanel.Name = "headerPanel";
            this.headerPanel.Padding = new System.Windows.Forms.Padding(10, 5, 10, 5); // Tighter padding
            this.headerPanel.Size = new System.Drawing.Size(560, 60); // Smaller height
            this.headerPanel.TabIndex = 0;

            //
            // headerTableLayoutPanel
            //
            this.headerTableLayoutPanel.ColumnCount = 2;
            this.headerTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.headerTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 80F));
            this.headerTableLayoutPanel.Controls.Add(this.titlepanel, 0, 0);
            this.headerTableLayoutPanel.Controls.Add(this.buttonpanel, 1, 0);
            this.headerTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headerTableLayoutPanel.Location = new System.Drawing.Point(30, 20);
            this.headerTableLayoutPanel.Name = "headerTableLayoutPanel";
            this.headerTableLayoutPanel.RowCount = 1;
            this.headerTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.headerTableLayoutPanel.Size = new System.Drawing.Size(500, 75); // Increased height to fit descriptions
            this.headerTableLayoutPanel.TabIndex = 0;

            //
            // titlepanel
            //
            this.titlepanel.BackColor = System.Drawing.Color.FromArgb(248, 249, 250);
            this.titlepanel.Controls.Add(this.titleLabel);
            // Subtitle label removed for compactness
            this.titlepanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.titlepanel.Location = new System.Drawing.Point(3, 3);
            this.titlepanel.Name = "titlepanel";
            this.titlepanel.Size = new System.Drawing.Size(94, 54);
            this.titlepanel.TabIndex = 0;

            //
            // buttonpanel
            //
            this.buttonpanel.BackColor = System.Drawing.Color.FromArgb(248, 249, 250);
            this.buttonpanel.Controls.Add(this.buttonTableLayoutPanel);
            this.buttonpanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonpanel.Location = new System.Drawing.Point(103, 3);
            this.buttonpanel.Name = "buttonpanel";
            this.buttonpanel.Size = new System.Drawing.Size(394, 54);
            this.buttonpanel.TabIndex = 1;

            //
            // buttonTableLayoutPanel
            //
            this.buttonTableLayoutPanel.ColumnCount = 7;
            this.buttonTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.2857F));
            this.buttonTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.2857F));
            this.buttonTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.2857F));
            this.buttonTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.2857F));
            this.buttonTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.2857F));
            this.buttonTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.2857F));
            this.buttonTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.2857F));
            this.buttonTableLayoutPanel.Controls.Add(this.setupButton, 0, 0);
            this.buttonTableLayoutPanel.Controls.Add(this.scannerSetupButton, 1, 0);
            this.buttonTableLayoutPanel.Controls.Add(this.printerConnectionButton, 2, 0);
            this.buttonTableLayoutPanel.Controls.Add(this.barCodesButton, 3, 0);
            this.buttonTableLayoutPanel.Controls.Add(this.boxLabelsButton, 4, 0);
            this.buttonTableLayoutPanel.Controls.Add(this.reportsButton, 5, 0);
            this.buttonTableLayoutPanel.Controls.Add(this.logoutButton, 6, 0);
            this.buttonTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.buttonTableLayoutPanel.Name = "buttonTableLayoutPanel";
            this.buttonTableLayoutPanel.RowCount = 1;
            this.buttonTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.buttonTableLayoutPanel.Size = new System.Drawing.Size(394, 69); // Increased height
            this.buttonTableLayoutPanel.TabIndex = 0;

            //
            // setupButton
            //
            this.setupButton.BackColor = System.Drawing.Color.FromArgb(13, 110, 253); // Corporate primary blue
            this.setupButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.setupButton.FlatAppearance.BorderSize = 0;
            this.setupButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(10, 88, 202);
            this.setupButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(11, 94, 215);
            this.setupButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.setupButton.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.setupButton.ForeColor = System.Drawing.Color.White;
            this.setupButton.Margin = new System.Windows.Forms.Padding(2);
            this.setupButton.Name = "setupButton";
            this.setupButton.TabIndex = 0;
            this.setupButton.Text = "";
            this.setupButton.Tag = "Setup|System settings";
            this.setupButton.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.setupButton.UseVisualStyleBackColor = false;
            this.setupButton.SizeChanged += new System.EventHandler(this.Button_SizeChanged);
            this.setupButton.Paint += new System.Windows.Forms.PaintEventHandler(this.Button_Paint);
            this.setupButton.Click += new System.EventHandler(this.setupButton_Click);

            //
            // scannerSetupButton
            //
            this.scannerSetupButton.BackColor = System.Drawing.Color.FromArgb(13, 110, 253);
            this.scannerSetupButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scannerSetupButton.FlatAppearance.BorderSize = 0;
            this.scannerSetupButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(10, 88, 202);
            this.scannerSetupButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(11, 94, 215);
            this.scannerSetupButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.scannerSetupButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.scannerSetupButton.ForeColor = System.Drawing.Color.White;
            this.scannerSetupButton.Margin = new System.Windows.Forms.Padding(2);
            this.scannerSetupButton.Name = "scannerSetupButton";
            this.scannerSetupButton.TabIndex = 1;
            this.scannerSetupButton.Text = ""; 
            this.scannerSetupButton.Tag = "Scanner|Manage devices";
            this.scannerSetupButton.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.scannerSetupButton.UseVisualStyleBackColor = false;
            this.scannerSetupButton.SizeChanged += new System.EventHandler(this.Button_SizeChanged);
            this.scannerSetupButton.Paint += new System.Windows.Forms.PaintEventHandler(this.Button_Paint);
            this.scannerSetupButton.Click += new System.EventHandler(this.scannerSetupButton_Click);

            //
            // printerConnectionButton
            //
            this.printerConnectionButton.BackColor = System.Drawing.Color.FromArgb(13, 110, 253);
            this.printerConnectionButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.printerConnectionButton.FlatAppearance.BorderSize = 0;
            this.printerConnectionButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(10, 88, 202);
            this.printerConnectionButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(11, 94, 215);
            this.printerConnectionButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.printerConnectionButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.printerConnectionButton.ForeColor = System.Drawing.Color.White;
            this.printerConnectionButton.Margin = new System.Windows.Forms.Padding(2);
            this.printerConnectionButton.Name = "printerConnectionButton";
            this.printerConnectionButton.TabIndex = 2;
            this.printerConnectionButton.Text = ""; 
            this.printerConnectionButton.Tag = "Printer|Setup connection";
            this.printerConnectionButton.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.printerConnectionButton.UseVisualStyleBackColor = false;
            this.printerConnectionButton.SizeChanged += new System.EventHandler(this.Button_SizeChanged);
            this.printerConnectionButton.Paint += new System.Windows.Forms.PaintEventHandler(this.Button_Paint);
            this.printerConnectionButton.Click += new System.EventHandler(this.printerConnectionButton_Click);

            //
            // barCodesButton
            //
            this.barCodesButton.BackColor = System.Drawing.Color.FromArgb(13, 110, 253);
            this.barCodesButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.barCodesButton.FlatAppearance.BorderSize = 0;
            this.barCodesButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(10, 88, 202);
            this.barCodesButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(11, 94, 215);
            this.barCodesButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.barCodesButton.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.barCodesButton.ForeColor = System.Drawing.Color.White;
            this.barCodesButton.Margin = new System.Windows.Forms.Padding(2);
            this.barCodesButton.Name = "barCodesButton";
            this.barCodesButton.TabIndex = 3;
            this.barCodesButton.Text = "";
            this.barCodesButton.Tag = "Bar Codes|Generate labels";
            this.barCodesButton.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.barCodesButton.UseVisualStyleBackColor = false;
            this.barCodesButton.SizeChanged += new System.EventHandler(this.Button_SizeChanged);
            this.barCodesButton.Paint += new System.Windows.Forms.PaintEventHandler(this.Button_Paint);
            this.barCodesButton.Click += new System.EventHandler(this.barCodesButton_Click);

            //
            // boxLabelsButton
            //
            this.boxLabelsButton.BackColor = System.Drawing.Color.FromArgb(13, 110, 253);
            this.boxLabelsButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.boxLabelsButton.FlatAppearance.BorderSize = 0;
            this.boxLabelsButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(10, 88, 202);
            this.boxLabelsButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(11, 94, 215);
            this.boxLabelsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.boxLabelsButton.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.boxLabelsButton.ForeColor = System.Drawing.Color.White;
            this.boxLabelsButton.Margin = new System.Windows.Forms.Padding(2);
            this.boxLabelsButton.Name = "boxLabelsButton";
            this.boxLabelsButton.TabIndex = 4;
            this.boxLabelsButton.Text = "";
            this.boxLabelsButton.Tag = "Box Labels|Print dispatch";
            this.boxLabelsButton.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.boxLabelsButton.UseVisualStyleBackColor = false;
            this.boxLabelsButton.SizeChanged += new System.EventHandler(this.Button_SizeChanged);
            this.boxLabelsButton.Paint += new System.Windows.Forms.PaintEventHandler(this.Button_Paint);
            this.boxLabelsButton.Click += new System.EventHandler(this.boxLabelsButton_Click);

            //
            // reportsButton
            //
            this.reportsButton.BackColor = System.Drawing.Color.FromArgb(13, 110, 253);
            this.reportsButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.reportsButton.FlatAppearance.BorderSize = 0;
            this.reportsButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(10, 88, 202);
            this.reportsButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(11, 94, 215);
            this.reportsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.reportsButton.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.reportsButton.ForeColor = System.Drawing.Color.White;
            this.reportsButton.Margin = new System.Windows.Forms.Padding(2);
            this.reportsButton.Name = "reportsButton";
            this.reportsButton.TabIndex = 5;
            this.reportsButton.Text = "";
            this.reportsButton.Tag = "Reports|View web stats";
            this.reportsButton.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.reportsButton.UseVisualStyleBackColor = false;
            this.reportsButton.SizeChanged += new System.EventHandler(this.Button_SizeChanged);
            this.reportsButton.Paint += new System.Windows.Forms.PaintEventHandler(this.Button_Paint);
            this.reportsButton.Click += new System.EventHandler(this.reportsButton_Click);

            //
            // logoutButton
            //
            this.logoutButton.BackColor = System.Drawing.Color.FromArgb(248, 249, 250); // Blends in normally
            this.logoutButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.logoutButton.FlatAppearance.BorderSize = 1;
            this.logoutButton.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(206, 212, 218); // Light gray border
            this.logoutButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(220, 53, 69); // Danger red
            this.logoutButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(220, 53, 69); // Danger red
            this.logoutButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.logoutButton.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.logoutButton.ForeColor = System.Drawing.Color.FromArgb(33, 37, 41);
            this.logoutButton.Margin = new System.Windows.Forms.Padding(2);
            this.logoutButton.Name = "logoutButton";
            this.logoutButton.TabIndex = 6;
            this.logoutButton.Text = "";
            this.logoutButton.Tag = "Logout|End session";
            this.logoutButton.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.logoutButton.UseVisualStyleBackColor = false;
            this.logoutButton.SizeChanged += new System.EventHandler(this.Button_SizeChanged);
            this.logoutButton.Paint += new System.Windows.Forms.PaintEventHandler(this.Button_Paint);
            this.logoutButton.Click += new System.EventHandler(this.logoutButton_Click);

            //
            // titleLabel
            // 
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.titleLabel.ForeColor = System.Drawing.Color.FromArgb(33, 37, 41);
            this.titleLabel.Location = new System.Drawing.Point(0, 5); // Vertically centered
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(168, 45);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "ScanLink";
            
            // 
            // subtitleLabel
            // 
            this.subtitleLabel.AutoSize = true;
            this.subtitleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.subtitleLabel.ForeColor = System.Drawing.Color.FromArgb(204, 201, 220);
            this.subtitleLabel.Location = new System.Drawing.Point(4, 45);
            this.subtitleLabel.Name = "subtitleLabel";
            this.subtitleLabel.Size = new System.Drawing.Size(281, 20);
            this.subtitleLabel.TabIndex = 1;
            this.subtitleLabel.Text = "Professional Barcode Printing Solution";
            
            // configPanel
            // 
            this.configPanel.Controls.Add(this.configGroupBox);
            this.configPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.configPanel.Location = new System.Drawing.Point(0, 280); // Removed left padding
            this.configPanel.Name = "configPanel";
            this.configPanel.Padding = new System.Windows.Forms.Padding(20, 10, 20, 0); // Tighter padding
            this.configPanel.Size = new System.Drawing.Size(600, 250); // Adjusted height
            this.configPanel.TabIndex = 2;
            
            // configGroupBox
            // 
            this.configGroupBox.Controls.Add(this.barcodeTextPanel);
            this.configGroupBox.Controls.Add(this.label_count);
            this.configGroupBox.Controls.Add(this.numericUpDown_count);
            this.configGroupBox.Controls.Add(this.button_generateBarcode);
            this.configGroupBox.Controls.Add(this.button_preview);
            this.configGroupBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.configGroupBox.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.configGroupBox.ForeColor = System.Drawing.Color.FromArgb(33, 37, 41);
            this.configGroupBox.Location = new System.Drawing.Point(20, 10);
            this.configGroupBox.Name = "configGroupBox";
            this.configGroupBox.Padding = new System.Windows.Forms.Padding(10, 10, 10, 10); // Tighter padding
            this.configGroupBox.Size = new System.Drawing.Size(560, 215); // Wider
            this.configGroupBox.TabIndex = 0;
            this.configGroupBox.TabStop = false;
            this.configGroupBox.Text = "Print Configuration"; // Removed emoji for cleaner look
            
            // 
            // barcodeTextPanel
            // 
            this.barcodeTextPanel.Controls.Add(this.label_ProductDetail);
            this.barcodeTextPanel.Controls.Add(this.label_EmployeeID);
            this.barcodeTextPanel.Controls.Add(this.textBox_EmployeeID);
            this.barcodeTextPanel.Controls.Add(this.button_FetchEmployees);
            this.barcodeTextPanel.Controls.Add(this.label_ProductID);
            this.barcodeTextPanel.Controls.Add(this.comboBox_ProductID);
            this.barcodeTextPanel.Controls.Add(this.label_CropID);
            this.barcodeTextPanel.Controls.Add(this.comboBox_CropID);
            this.barcodeTextPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.barcodeTextPanel.Location = new System.Drawing.Point(20, 20);
            this.barcodeTextPanel.Name = "barcodeTextPanel";
            this.barcodeTextPanel.Size = new System.Drawing.Size(420, 125);
            this.barcodeTextPanel.TabIndex = 0;
            
            // 
            // label_EmployeeID
            // 
            this.label_EmployeeID.AutoSize = true;
            this.label_EmployeeID.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Regular);
            this.label_EmployeeID.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.label_EmployeeID.Location = new System.Drawing.Point(5, 5);
            this.label_EmployeeID.Name = "label_EmployeeID";
            this.label_EmployeeID.Size = new System.Drawing.Size(77, 15);
            this.label_EmployeeID.TabIndex = 0;
            this.label_EmployeeID.Text = "Employee ID";
            
            // 
            // textBox_EmployeeID
            // 
            this.textBox_EmployeeID.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Regular);
            this.textBox_EmployeeID.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_EmployeeID.Location = new System.Drawing.Point(170, 2);
            this.textBox_EmployeeID.Name = "textBox_EmployeeID";
            this.textBox_EmployeeID.Size = new System.Drawing.Size(200, 24); // Shorter width to look cleaner
            this.textBox_EmployeeID.TabIndex = 1;
            this.textBox_EmployeeID.Text = "123456";
            
            // 
            // button_FetchEmployees
            // 
            this.button_FetchEmployees.BackColor = System.Drawing.Color.FromArgb(13, 110, 253);
            this.button_FetchEmployees.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_FetchEmployees.FlatAppearance.BorderSize = 0;
            this.button_FetchEmployees.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.button_FetchEmployees.ForeColor = System.Drawing.Color.White;
            this.button_FetchEmployees.Location = new System.Drawing.Point(380, 2);
            this.button_FetchEmployees.Name = "button_FetchEmployees";
            this.button_FetchEmployees.Size = new System.Drawing.Size(80, 24);
            this.button_FetchEmployees.TabIndex = 2;
            this.button_FetchEmployees.Text = "Fetch";
            this.button_FetchEmployees.UseVisualStyleBackColor = false;
            this.button_FetchEmployees.Click += new System.EventHandler(this.button_FetchEmployees_Click);

            
            // 
            // label_CropID
            // 
            this.label_CropID.AutoSize = true;
            this.label_CropID.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Regular);
            this.label_CropID.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.label_CropID.Location = new System.Drawing.Point(5, 40);
            this.label_CropID.Name = "label_CropID";
            this.label_CropID.Size = new System.Drawing.Size(52, 15);
            this.label_CropID.TabIndex = 0;
            this.label_CropID.Text = "Crop"; 
            
            // 
            // comboBox_CropID
            // 
            this.comboBox_CropID.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_CropID.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.comboBox_CropID.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Regular);
            this.comboBox_CropID.FormattingEnabled = true;
            this.comboBox_CropID.Location = new System.Drawing.Point(170, 35);
            this.comboBox_CropID.Name = "comboBox_CropID";
            this.comboBox_CropID.Size = new System.Drawing.Size(200, 24); // Match Employee ID width
            this.comboBox_CropID.MaxDropDownItems = 20;
            this.comboBox_CropID.DropDownHeight = 300;
            this.comboBox_CropID.TabIndex = 2;

            //
            // label_ProductID
            // 
            this.label_ProductID.AutoSize = true;
            this.label_ProductID.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Regular);
            this.label_ProductID.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.label_ProductID.Location = new System.Drawing.Point(5, 75);
            this.label_ProductID.Name = "label_ProductID";
            this.label_ProductID.Size = new System.Drawing.Size(130, 15);
            this.label_ProductID.TabIndex = 0;
            this.label_ProductID.Text = "Product Combination"; 
            
            // 
            // comboBox_ProductID
            // 
            this.comboBox_ProductID.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_ProductID.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.comboBox_ProductID.ItemHeight = 22;
            this.comboBox_ProductID.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.comboBox_ProductID.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Regular);
            this.comboBox_ProductID.FormattingEnabled = true;
            this.comboBox_ProductID.Location = new System.Drawing.Point(170, 70);
            this.comboBox_ProductID.Name = "comboBox_ProductID";
            this.comboBox_ProductID.Size = new System.Drawing.Size(430, 24);
            this.comboBox_ProductID.DropDownWidth = 700; // Increased width for better column visibility
            this.comboBox_ProductID.MaxDropDownItems = 15;
            this.comboBox_ProductID.DropDownHeight = 350;
            this.comboBox_ProductID.TabIndex = 1;
            this.comboBox_ProductID.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.comboBox_ProductID_DrawItem);

            
            //
            // label_ProductDetail
            //
            this.label_ProductDetail.AutoSize = true;
            this.label_ProductDetail.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Regular);
            this.label_ProductDetail.ForeColor = System.Drawing.Color.FromArgb(230, 126, 34); // Keep orange but lighter font
            this.label_ProductDetail.Location = new System.Drawing.Point(170, 100); // Align with combobox
            this.label_ProductDetail.Name = "label_ProductDetail";
            this.label_ProductDetail.Size = new System.Drawing.Size(100, 15);
            this.label_ProductDetail.TabIndex = 8;
            this.label_ProductDetail.Text = "Variety: N/A      Grade: N/A      Count: N/A      Avg Weight (kg): N/A";
            // 
            
            // 
            // label_count
            // 
            this.label_count.AutoSize = true;
            this.label_count.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Regular);
            this.label_count.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.label_count.Location = new System.Drawing.Point(25, 153);
            this.label_count.Name = "label_count";
            this.label_count.Size = new System.Drawing.Size(65, 20);
            this.label_count.TabIndex = 6;
            this.label_count.Text = "Print Count";
            
            // 
            // numericUpDown_count
            // 
            this.numericUpDown_count.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Regular);
            this.numericUpDown_count.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.numericUpDown_count.Location = new System.Drawing.Point(190, 150);
            this.numericUpDown_count.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numericUpDown_count.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDown_count.Name = "numericUpDown_count";
            this.numericUpDown_count.Size = new System.Drawing.Size(140, 24); // Matched height and proportion
            this.numericUpDown_count.TabIndex = 7;
            this.numericUpDown_count.Value = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDown_count.ValueChanged += new System.EventHandler(this.numericUpDown_count_ValueChanged);
            
            // checkBox_showAdvanced definition removed - advanced settings always enabled
            
            // 
            // advancedPanel
            // 
            this.advancedPanel.Controls.Add(this.advancedGroupBox);
            this.advancedPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.advancedPanel.Location = new System.Drawing.Point(0, 530); // Adjusted location based on previous panel changes
            this.advancedPanel.Name = "advancedPanel";
            this.advancedPanel.Padding = new System.Windows.Forms.Padding(20, 10, 20, 0); // Tighter padding
            this.advancedPanel.Size = new System.Drawing.Size(600, 470);
            this.advancedPanel.TabIndex = 3;
            this.advancedPanel.Visible = false;
            this.advancedPanel.AutoSize = true;
            this.advancedPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            
            // 
            // advancedGroupBox
            // 
            this.advancedGroupBox.Controls.Add(this.qualityPanel);
            this.advancedGroupBox.Controls.Add(this.dimensionsPanel);
            this.advancedGroupBox.Controls.Add(this.printerConfigPanel);
            this.advancedGroupBox.Controls.Add(this.dimensionVisualizationPictureBox);
            this.advancedGroupBox.Controls.Add(this.dimensionVisualizationPictureBox_TwoUp);
            this.advancedGroupBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.advancedGroupBox.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.advancedGroupBox.ForeColor = System.Drawing.Color.FromArgb(33, 37, 41);
            this.advancedGroupBox.Location = new System.Drawing.Point(20, 10);
            this.advancedGroupBox.Name = "advancedGroupBox";
            this.advancedGroupBox.Padding = new System.Windows.Forms.Padding(10, 10, 10, 0); // Tighter padding
            this.advancedGroupBox.Size = new System.Drawing.Size(560, 470);
            this.advancedGroupBox.TabIndex = 0;
            this.advancedGroupBox.TabStop = false;
            this.advancedGroupBox.Text = "Advanced Print Settings"; // Removed emoji
            this.advancedGroupBox.AutoSize = true;
            this.advancedGroupBox.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;

            //
            // dimensionVisualizationPictureBox
            //
            this.dimensionVisualizationPictureBox.BackColor = System.Drawing.Color.White;
            this.dimensionVisualizationPictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.dimensionVisualizationPictureBox.Location = new System.Drawing.Point(650, 40);
            this.dimensionVisualizationPictureBox.Name = "dimensionVisualizationPictureBox";
            this.dimensionVisualizationPictureBox.Size = new System.Drawing.Size(550, 350);
            this.dimensionVisualizationPictureBox.TabIndex = 1;
            this.dimensionVisualizationPictureBox.TabStop = false;
            this.dimensionVisualizationPictureBox.Visible = false;
            this.dimensionVisualizationPictureBox.Paint += new System.Windows.Forms.PaintEventHandler(this.dimensionVisualizationPictureBox_Paint);
            //
            // dimensionVisualizationPictureBox_TwoUp
            //
            this.dimensionVisualizationPictureBox_TwoUp.BackColor = System.Drawing.Color.White;
            this.dimensionVisualizationPictureBox_TwoUp.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.dimensionVisualizationPictureBox_TwoUp.Location = new System.Drawing.Point(650, 40);
            this.dimensionVisualizationPictureBox_TwoUp.Name = "dimensionVisualizationPictureBox_TwoUp";
            this.dimensionVisualizationPictureBox_TwoUp.Size = new System.Drawing.Size(550, 350);
            this.dimensionVisualizationPictureBox_TwoUp.TabIndex = 2;
            this.dimensionVisualizationPictureBox_TwoUp.TabStop = false;
            this.dimensionVisualizationPictureBox_TwoUp.Visible = false;
            this.dimensionVisualizationPictureBox_TwoUp.Paint += new System.Windows.Forms.PaintEventHandler(this.dimensionVisualizationPictureBox_TwoUp_Paint);
            //
            //

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
            this.label_emulation.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
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
            this.comboBox_emulation.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.comboBox_emulation.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.comboBox_emulation.FormattingEnabled = true;
            this.comboBox_emulation.Location = new System.Drawing.Point(170, 2);
            this.comboBox_emulation.Name = "comboBox_emulation";
            this.comboBox_emulation.Size = new System.Drawing.Size(380, 23);
            this.comboBox_emulation.TabIndex = 1;
            this.comboBox_emulation.SelectedIndexChanged += new System.EventHandler(this.comboBox_emulation_SelectedIndexChanged);
            
            // 
            // label_test
            // 
            this.label_test.AutoSize = true;
            this.label_test.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
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
            this.comboBox_test.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.comboBox_test.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.comboBox_test.FormattingEnabled = true;
            this.comboBox_test.Location = new System.Drawing.Point(170, 42);
            this.comboBox_test.Name = "comboBox_test";
            this.comboBox_test.Size = new System.Drawing.Size(380, 23);
            this.comboBox_test.TabIndex = 3;
            this.comboBox_test.SelectedIndexChanged += new System.EventHandler(this.comboBox_test_SelectedIndexChanged);
            
            // 
            // label_barcode
            // 
            this.label_barcode.AutoSize = true;
            this.label_barcode.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
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
            this.comboBox_barcode.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.comboBox_barcode.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.comboBox_barcode.FormattingEnabled = true;
            this.comboBox_barcode.Location = new System.Drawing.Point(170, 82);
            this.comboBox_barcode.Name = "comboBox_barcode";
            this.comboBox_barcode.Size = new System.Drawing.Size(380, 23);
            this.comboBox_barcode.TabIndex = 5;
            this.comboBox_barcode.SelectedIndexChanged += new System.EventHandler(this.comboBox_barcode_SelectedIndexChanged);

            // 
            // dimensionsPanel
            // 
            this.dimensionsPanel.Controls.Add(this.label_width);
            this.dimensionsPanel.Controls.Add(this.numericUpDown_width);
            this.dimensionsPanel.Controls.Add(this.label_px_width);
            this.dimensionsPanel.Controls.Add(this.label_height);
            this.dimensionsPanel.Controls.Add(this.numericUpDown_height);
            this.dimensionsPanel.Controls.Add(this.label_px_height);
            this.dimensionsPanel.Controls.Add(this.label_xCoordinate);
            this.dimensionsPanel.Controls.Add(this.numericUpDown_xCoordinate);
            this.dimensionsPanel.Controls.Add(this.label_px_xCoordinate);
            this.dimensionsPanel.Controls.Add(this.label_x2Coordinate);
            this.dimensionsPanel.Controls.Add(this.numericUpDown_x2Coordinate);
            this.dimensionsPanel.Controls.Add(this.label_px_x2Coordinate);
            this.dimensionsPanel.Controls.Add(this.label_mm_gap);
            this.dimensionsPanel.Controls.Add(this.label_gap);
            this.dimensionsPanel.Controls.Add(this.numericUpDown_gap);
            this.dimensionsPanel.Controls.Add(this.checkBox_twoUp);
            this.dimensionsPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.dimensionsPanel.Location = new System.Drawing.Point(20, 178);
            this.dimensionsPanel.Name = "dimensionsPanel";
            this.dimensionsPanel.Size = new System.Drawing.Size(520, 125);
            this.dimensionsPanel.TabIndex = 1;
            
            // 
            // label_width
            // 
            this.label_width.AutoSize = true;
            this.label_width.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
            this.label_width.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.label_width.Location = new System.Drawing.Point(5, 15);
            this.label_width.Name = "label_width";
            this.label_width.Size = new System.Drawing.Size(39, 15);
            this.label_width.TabIndex = 0;
            this.label_width.Text = "Width";
            
            // 
            // numericUpDown_width
            // 
            this.numericUpDown_width.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.numericUpDown_width.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.numericUpDown_width.Location = new System.Drawing.Point(170, 12);
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
            this.label_height.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
            this.label_height.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.label_height.Location = new System.Drawing.Point(350, 15);
            this.label_height.Name = "label_height";
            this.label_height.Size = new System.Drawing.Size(43, 15);
            this.label_height.TabIndex = 2;
            this.label_height.Text = "Height";
            
            // 
            // numericUpDown_height
            // 
            this.numericUpDown_height.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.numericUpDown_height.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.numericUpDown_height.Location = new System.Drawing.Point(500, 12);
            this.numericUpDown_height.Maximum = new decimal(new int[] { 999, 0, 0, 0 });
            this.numericUpDown_height.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDown_height.Name = "numericUpDown_height";
            this.numericUpDown_height.Size = new System.Drawing.Size(100, 23);
            this.numericUpDown_height.TabIndex = 3;
            this.numericUpDown_height.Value = new decimal(new int[] { 180, 0, 0, 0 });
            this.numericUpDown_height.ValueChanged += new System.EventHandler(this.numericUpDown_height_ValueChanged);
            
            // 
            // label_xCoordinate
            // 
            this.label_xCoordinate.AutoSize = true;
            this.label_xCoordinate.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
            this.label_xCoordinate.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.label_xCoordinate.Location = new System.Drawing.Point(5, 50);
            this.label_xCoordinate.Name = "label_xCoordinate";
            this.label_xCoordinate.Size = new System.Drawing.Size(73, 15);
            this.label_xCoordinate.TabIndex = 4;
            this.label_xCoordinate.Text = "X Coordinate";
            
            // 
            // numericUpDown_xCoordinate
            // 
            this.numericUpDown_xCoordinate.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.numericUpDown_xCoordinate.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.numericUpDown_xCoordinate.Location = new System.Drawing.Point(170, 47);
            this.numericUpDown_xCoordinate.Maximum = new decimal(new int[] { 999, 0, 0, 0 });
            this.numericUpDown_xCoordinate.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.numericUpDown_xCoordinate.Name = "numericUpDown_xCoordinate";
            this.numericUpDown_xCoordinate.Size = new System.Drawing.Size(100, 23);
            this.numericUpDown_xCoordinate.TabIndex = 5;
            this.numericUpDown_xCoordinate.ValueChanged += new System.EventHandler(this.numericUpDown_xCoordinate_ValueChanged);
            
            // 
            // label_x2Coordinate
            // 
            this.label_x2Coordinate.AutoSize = true;
            this.label_x2Coordinate.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
            this.label_x2Coordinate.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.label_x2Coordinate.Location = new System.Drawing.Point(350, 50);
            this.label_x2Coordinate.Name = "label_x2Coordinate";
            this.label_x2Coordinate.Size = new System.Drawing.Size(80, 15);
            this.label_x2Coordinate.TabIndex = 8;
            this.label_x2Coordinate.Text = "X2 Coordinate";
            
            // 
            // numericUpDown_x2Coordinate
            // 
            this.numericUpDown_x2Coordinate.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.numericUpDown_x2Coordinate.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.numericUpDown_x2Coordinate.Location = new System.Drawing.Point(500, 47);
            this.numericUpDown_x2Coordinate.Maximum = new decimal(new int[] { 999, 0, 0, 0 });
            this.numericUpDown_x2Coordinate.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.numericUpDown_x2Coordinate.Name = "numericUpDown_x2Coordinate";
            this.numericUpDown_x2Coordinate.Size = new System.Drawing.Size(100, 23);
            this.numericUpDown_x2Coordinate.TabIndex = 9;
            this.numericUpDown_x2Coordinate.Enabled = false;
            this.numericUpDown_x2Coordinate.ValueChanged += new System.EventHandler(this.numericUpDown_x2Coordinate_ValueChanged);

            //
            // label_px_width
            //
            this.label_px_width.AutoSize = true;
            this.label_px_width.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular);
            this.label_px_width.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.label_px_width.Location = new System.Drawing.Point(275, 15);
            this.label_px_width.Name = "label_px_width";
            this.label_px_width.Size = new System.Drawing.Size(18, 13);
            this.label_px_width.TabIndex = 10;
            this.label_px_width.Text = "px";

            //
            // label_px_height
            //
            this.label_px_height.AutoSize = true;
            this.label_px_height.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular);
            this.label_px_height.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.label_px_height.Location = new System.Drawing.Point(605, 15);
            this.label_px_height.Name = "label_px_height";
            this.label_px_height.Size = new System.Drawing.Size(18, 13);
            this.label_px_height.TabIndex = 11;
            this.label_px_height.Text = "px";

            //
            // label_px_xCoordinate
            //
            this.label_px_xCoordinate.AutoSize = true;
            this.label_px_xCoordinate.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular);
            this.label_px_xCoordinate.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.label_px_xCoordinate.Location = new System.Drawing.Point(275, 50);
            this.label_px_xCoordinate.Name = "label_px_xCoordinate";
            this.label_px_xCoordinate.Size = new System.Drawing.Size(18, 13);
            this.label_px_xCoordinate.TabIndex = 12;
            this.label_px_xCoordinate.Text = "px";

            //
            // label_px_x2Coordinate
            //
            this.label_px_x2Coordinate.AutoSize = true;
            this.label_px_x2Coordinate.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular);
            this.label_px_x2Coordinate.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.label_px_x2Coordinate.Location = new System.Drawing.Point(605, 50);
            this.label_px_x2Coordinate.Name = "label_px_x2Coordinate";
            this.label_px_x2Coordinate.Size = new System.Drawing.Size(18, 13);
            this.label_px_x2Coordinate.TabIndex = 13;
            this.label_px_x2Coordinate.Text = "px";

            //
            // label_gap
            // 
            this.label_gap.AutoSize = true;
            this.label_gap.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
            this.label_gap.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.label_gap.Location = new System.Drawing.Point(5, 85);
            this.label_gap.Name = "label_gap";
            this.label_gap.Size = new System.Drawing.Size(28, 15);
            this.label_gap.TabIndex = 6;
            this.label_gap.Text = "Gap";
            
            // 
            // numericUpDown_gap
            // 
            this.numericUpDown_gap.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.numericUpDown_gap.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.numericUpDown_gap.Location = new System.Drawing.Point(170, 82);
            this.numericUpDown_gap.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numericUpDown_gap.Name = "numericUpDown_gap";
            this.numericUpDown_gap.Size = new System.Drawing.Size(100, 23);
            this.numericUpDown_gap.TabIndex = 7;
            this.numericUpDown_gap.Value = new decimal(new int[] { 3, 0, 0, 0 });
            this.numericUpDown_gap.ValueChanged += new System.EventHandler(this.numericUpDown_gap_ValueChanged);
            
            //
            // label_mm_gap
            //
            this.label_mm_gap.AutoSize = true;
            this.label_mm_gap.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular);
            this.label_mm_gap.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.label_mm_gap.Location = new System.Drawing.Point(275, 85);
            this.label_mm_gap.Name = "label_mm_gap";
            this.label_mm_gap.Size = new System.Drawing.Size(18, 13);
            this.label_mm_gap.TabIndex = 10;
            this.label_mm_gap.Text = "mm";

            

            // 
            // checkBox_twoUp
            // 
            this.checkBox_twoUp.AutoSize = true;
            this.checkBox_twoUp.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
            this.checkBox_twoUp.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.checkBox_twoUp.Location = new System.Drawing.Point(350, 84);
            this.checkBox_twoUp.Name = "checkBox_twoUp";
            this.checkBox_twoUp.Size = new System.Drawing.Size(185, 19);
            this.checkBox_twoUp.TabIndex = 10;
            this.checkBox_twoUp.Text = "Print two stickers horizontally";
            this.checkBox_twoUp.UseVisualStyleBackColor = true;
            this.checkBox_twoUp.CheckedChanged += new System.EventHandler(this.checkBox_twoUp_CheckedChanged);
            
            
            // 
            // qualityPanel
            // 
            this.qualityPanel.Controls.Add(this.label_darkness);
            this.qualityPanel.Controls.Add(this.trackBar_darkness);
            this.qualityPanel.Controls.Add(this.label_darknessValue);
            this.qualityPanel.Controls.Add(this.label_speed);
            this.qualityPanel.Controls.Add(this.comboBox_speed);
            this.qualityPanel.Controls.Add(this.label_dpi);
            this.qualityPanel.Controls.Add(this.numericUpDown_dpi);
            this.qualityPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.qualityPanel.Location = new System.Drawing.Point(20, 318);
            this.qualityPanel.Name = "qualityPanel";
            this.qualityPanel.Size = new System.Drawing.Size(520, 80);
            this.qualityPanel.TabIndex = 3;
            
            // 
            // label_darkness
            // 
            this.label_darkness.AutoSize = true;
            this.label_darkness.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
            this.label_darkness.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.label_darkness.Location = new System.Drawing.Point(5, 15);
            this.label_darkness.Name = "label_darkness";
            this.label_darkness.Size = new System.Drawing.Size(56, 15);
            this.label_darkness.TabIndex = 0;
            this.label_darkness.Text = "Darkness";
            
            // 
            // trackBar_darkness
            // 
            this.trackBar_darkness.Location = new System.Drawing.Point(170, 10);
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
            this.label_darknessValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
            this.label_darknessValue.ForeColor = System.Drawing.Color.FromArgb(27, 42, 65);
            this.label_darknessValue.Location = new System.Drawing.Point(480, 15);
            this.label_darknessValue.Name = "label_darknessValue";
            this.label_darknessValue.Size = new System.Drawing.Size(19, 15);
            this.label_darknessValue.TabIndex = 2;
            this.label_darknessValue.Text = "15";
            
            // 
            // label_speed
            // 
            this.label_speed.AutoSize = true;
            this.label_speed.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
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
            this.comboBox_speed.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.comboBox_speed.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.comboBox_speed.FormattingEnabled = true;
            this.comboBox_speed.Items.AddRange(new object[] { "1 - Slowest", "2", "3", "4", "5 - Medium", "6", "7", "8", "9 - Fastest" });
            this.comboBox_speed.Location = new System.Drawing.Point(170, 56);
            this.comboBox_speed.Name = "comboBox_speed";
            this.comboBox_speed.Size = new System.Drawing.Size(100, 23);
            this.comboBox_speed.TabIndex = 4;
            this.comboBox_speed.SelectedIndex = 4;
            this.comboBox_speed.SelectedIndexChanged += new System.EventHandler(this.comboBox_speed_SelectedIndexChanged);

            //
            // label_dpi
            //
            this.label_dpi.AutoSize = true;
            this.label_dpi.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
            this.label_dpi.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.label_dpi.Location = new System.Drawing.Point(350, 59);
            this.label_dpi.Name = "label_dpi";
            this.label_dpi.Size = new System.Drawing.Size(130, 15);
            this.label_dpi.TabIndex = 5;
            this.label_dpi.Text = "Dots per inches (DPI)";

            //
            // numericUpDown_dpi
            //
            this.numericUpDown_dpi.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.numericUpDown_dpi.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.numericUpDown_dpi.Location = new System.Drawing.Point(500, 56);
            this.numericUpDown_dpi.Maximum = new decimal(new int[] { 999, 0, 0, 0 });
            this.numericUpDown_dpi.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.numericUpDown_dpi.Name = "numericUpDown_dpi";
            this.numericUpDown_dpi.Size = new System.Drawing.Size(100, 23);
            this.numericUpDown_dpi.TabIndex = 6;
            this.numericUpDown_dpi.Value = new decimal(new int[] { 203, 0, 0, 0 });
            this.numericUpDown_dpi.ValueChanged += new System.EventHandler(this.numericUpDown_dpi_ValueChanged);

            //
            // previewPanel
            //
            this.previewPanel.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            this.previewPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.previewPanel.Location = new System.Drawing.Point(0, 200);
            this.previewPanel.Name = "previewPanel";
            this.previewPanel.Size = new System.Drawing.Size(420, 50);
            this.previewPanel.TabIndex = 4;
            
            //
            // button_generateBarcode
            //
            this.button_generateBarcode.BackColor = System.Drawing.Color.FromArgb(25, 135, 84); // Success green
            this.button_generateBarcode.FlatAppearance.BorderSize = 0;
            this.button_generateBarcode.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_generateBarcode.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.button_generateBarcode.ForeColor = System.Drawing.Color.White;
            this.button_generateBarcode.Location = new System.Drawing.Point(360, 146);
            this.button_generateBarcode.Name = "button_generateBarcode";
            this.button_generateBarcode.Size = new System.Drawing.Size(140, 32);
            this.button_generateBarcode.TabIndex = 0;
            this.button_generateBarcode.Text = "Generate Barcode";
            this.button_generateBarcode.UseVisualStyleBackColor = false;
            this.button_generateBarcode.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(21, 115, 71);
            this.button_generateBarcode.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(20, 108, 67);
            this.button_generateBarcode.Click += new System.EventHandler(this.button_generateBarcode_Click);

            //
            // button_preview
            //
            this.button_preview.BackColor = System.Drawing.Color.White;
            this.button_preview.FlatAppearance.BorderSize = 1;
            this.button_preview.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(206, 212, 218);
            this.button_preview.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_preview.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Regular);
            this.button_preview.ForeColor = System.Drawing.Color.FromArgb(33, 37, 41);
            this.button_preview.Location = new System.Drawing.Point(520, 146);
            this.button_preview.Name = "button_preview";
            this.button_preview.Size = new System.Drawing.Size(120, 32);
            this.button_preview.TabIndex = 1;
            this.button_preview.Text = "Preview Label";
            this.button_preview.UseVisualStyleBackColor = false;
            this.button_preview.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(248, 249, 250);
            this.button_preview.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(226, 230, 234);
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
            this.button_send.BackColor = System.Drawing.Color.FromArgb(13, 110, 253); // Primary blue
            this.button_send.FlatAppearance.BorderSize = 0;
            this.button_send.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_send.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.button_send.ForeColor = System.Drawing.Color.White;
            this.button_send.Location = new System.Drawing.Point(20, 10); // Account for 20px padding
            this.button_send.Name = "button_send";
            this.button_send.Size = new System.Drawing.Size(520, 45); // Taller, friendlier button (560 - 40)
            this.button_send.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.button_send.TabIndex = 0;
            this.button_send.Text = "Print Barcode";
            this.button_send.UseVisualStyleBackColor = false;
            this.button_send.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(11, 94, 215);
            this.button_send.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(10, 88, 202);
            this.button_send.Click += new System.EventHandler(this.button_send_Click);
            
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(20, 65);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(520, 8);
            this.progressBar.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar.MarqueeAnimationSpeed = 30;
            this.progressBar.TabIndex = 1;
            this.progressBar.Visible = true;
            
            // 
            // statusPanel
            // 
            this.statusPanel.Controls.Add(this.statusLabel);
            this.statusPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.statusPanel.Location = new System.Drawing.Point(20, 980);
            this.statusPanel.Name = "statusPanel";
            this.statusPanel.Padding = new System.Windows.Forms.Padding(0);
            this.statusPanel.Size = new System.Drawing.Size(560, 40);
            this.statusPanel.TabIndex = 4;
            
            // 
            // statusLabel
            // 
            this.statusLabel.BackColor = System.Drawing.Color.FromArgb(248, 249, 250);
            this.statusLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.statusLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular); 
            this.statusLabel.ForeColor = System.Drawing.Color.FromArgb(108, 117, 125);
            this.statusLabel.Location = new System.Drawing.Point(0, 0);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0); // Slight indent
            this.statusLabel.Size = new System.Drawing.Size(560, 30);
            this.statusLabel.TabIndex = 0;
            this.statusLabel.Text = "Ready.";
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(600, 1120);
            this.Controls.Add(this.loginPanel);
            // startPanel removed
            // this.Controls.Add(this.printerContentPanel);
            this.Controls.Add(this.scannerContentPanel);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MinimumSize = new System.Drawing.Size(600, 650);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Scan Link - Professional Barcode Printing";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.Form1_Load);
            // startPanel removed
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).EndInit();
            // this.printerContentPanel.ResumeLayout(false);
            this.scannerContentPanel.ResumeLayout(false);
            this.scannerContentPanel.PerformLayout();
            this.headerPanel.ResumeLayout(false);
            this.headerPanel.PerformLayout();
            this.statusPanel.ResumeLayout(false);
            this.statusPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scannerDataGridView)).EndInit();
            this.headerTableLayoutPanel.ResumeLayout(false);
            this.titlepanel.ResumeLayout(false);
            this.buttonpanel.ResumeLayout(false);
            this.buttonTableLayoutPanel.ResumeLayout(false);
            this.headerPanel.PerformLayout();
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
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_xCoordinate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_x2Coordinate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_gap)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_dpi)).EndInit();
            this.qualityPanel.ResumeLayout(false);
            this.qualityPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_darkness)).EndInit();
            this.previewPanel.ResumeLayout(false);
            this.actionPanel.ResumeLayout(false);
            this.actionPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_count)).EndInit();
            this.loginPanel.ResumeLayout(false);
            this.loginPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.loginMainLogoPictureBox)).EndInit();
            this.loginGroupBox.ResumeLayout(false);
            this.loginGroupBox.PerformLayout();
            this.ResumeLayout(false);
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
        private System.Windows.Forms.GroupBox loginGroupBox;
        private System.Windows.Forms.Button loginButton;
        private System.Windows.Forms.Button passwordToggleButton;
        private System.Windows.Forms.TextBox passwordTextBox;
        private System.Windows.Forms.Label passwordLabel;
        private System.Windows.Forms.TextBox usernameTextBox;
        private System.Windows.Forms.Label usernameLabel;
        private System.Windows.Forms.Label loginStatusLabel;
        private System.Windows.Forms.ProgressBar loadingProgressBar;
        private System.Windows.Forms.Label loadingStatusLabel;
        private System.Windows.Forms.ProgressBar printerLoadingSpinner;
        private System.Windows.Forms.Label printerLoadingLabel;
        private System.Windows.Forms.Panel headerPanel;
        private System.Windows.Forms.TableLayoutPanel headerTableLayoutPanel;
        private System.Windows.Forms.Panel titlepanel;
        private System.Windows.Forms.Panel buttonpanel;
        private System.Windows.Forms.TableLayoutPanel buttonTableLayoutPanel;
        private System.Windows.Forms.Button setupButton;
        private System.Windows.Forms.Button scannerSetupButton;
        private System.Windows.Forms.Button printerConnectionButton;
        private System.Windows.Forms.Button barCodesButton;
        private System.Windows.Forms.Button boxLabelsButton;
        private System.Windows.Forms.Button reportsButton;
        private System.Windows.Forms.Button logoutButton;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label subtitleLabel;
        private System.Windows.Forms.Panel configPanel;
        private System.Windows.Forms.GroupBox configGroupBox;
        private System.Windows.Forms.Panel barcodeTextPanel;
        private System.Windows.Forms.Label label_EmployeeID;
        private System.Windows.Forms.TextBox textBox_EmployeeID;
        private System.Windows.Forms.Button button_FetchEmployees;
        private System.Windows.Forms.Label label_ProductID;
        private System.Windows.Forms.ComboBox comboBox_ProductID;
        private System.Windows.Forms.Label label_CropID;
        private System.Windows.Forms.ComboBox comboBox_CropID;
        private System.Windows.Forms.Label label_ProductDetail;
        private System.Windows.Forms.Label label_count;
        private System.Windows.Forms.NumericUpDown numericUpDown_count;
        private System.Windows.Forms.Panel actionPanel;
        private System.Windows.Forms.Button button_send;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Panel statusPanel;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.ToolTip toolTip;
        // checkBox_showAdvanced removed - advanced settings always enabled
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
        private System.Windows.Forms.Label label_xCoordinate;
        private System.Windows.Forms.NumericUpDown numericUpDown_xCoordinate;
        private System.Windows.Forms.Label label_x2Coordinate;
        private System.Windows.Forms.Label label_px_width;
        private System.Windows.Forms.Label label_px_height;
        private System.Windows.Forms.Label label_px_xCoordinate;
        private System.Windows.Forms.Label label_px_x2Coordinate;
        private System.Windows.Forms.Label label_mm_gap;
        private System.Windows.Forms.NumericUpDown numericUpDown_x2Coordinate;
        private System.Windows.Forms.CheckBox checkBox_twoUp;
        private System.Windows.Forms.Label label_gap;
        private System.Windows.Forms.NumericUpDown numericUpDown_gap;
        private System.Windows.Forms.Panel qualityPanel;
        private System.Windows.Forms.Label label_darkness;
        private System.Windows.Forms.TrackBar trackBar_darkness;
        private System.Windows.Forms.Label label_darknessValue;
        private System.Windows.Forms.Label label_speed;
        private System.Windows.Forms.ComboBox comboBox_speed;
        private System.Windows.Forms.Label label_dpi;
        private System.Windows.Forms.NumericUpDown numericUpDown_dpi;
        private System.Windows.Forms.Panel previewPanel;
        private System.Windows.Forms.Button button_preview;
        private System.Windows.Forms.PictureBox logoPictureBox;
        private System.Windows.Forms.PictureBox dimensionVisualizationPictureBox;
        private System.Windows.Forms.PictureBox dimensionVisualizationPictureBox_TwoUp;
        // private System.Windows.Forms.Panel printerContentPanel;
        private System.Windows.Forms.TableLayoutPanel scannerContentPanel;
        private System.Windows.Forms.Panel scannerOutputPanel;
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
        private System.Windows.Forms.Label todayScansLabel;
        private System.Windows.Forms.Label lastHourScansLabel;
        private System.Windows.Forms.Label totalFilteredScansLabel;
        private System.Windows.Forms.TableLayoutPanel statsPanel;
        private System.Windows.Forms.TableLayoutPanel paginationPanel;
        private System.Windows.Forms.TableLayoutPanel filtersPanel;
        // private System.Windows.Forms.Button runScannerScriptButton;
        // private System.Windows.Forms.TextBox barcodeInputTextBox;
        private System.Windows.Forms.Label cropIdLabel;
        private System.Windows.Forms.ComboBox cropIdComboBox;
        // private System.Windows.Forms.Button sendBarcodeButton;
        // private System.Windows.Forms.Button manageScannersButton;
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
            StylePrimaryButton(button_FetchEmployees, primary, primaryHover, textOnPrimary);
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
            StylePrimaryButton(button_send, primary, primaryHover, textOnPrimary);
            // StylePrimaryButton(button_preview, primary, primaryHover, textOnPrimary);
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




            // // Preview button - same styling as manage scanners
            // if (button_preview != null)
            // {
            //     button_preview.Padding = Padding.Empty;
            //     if (button_preview.Width < 130) button_preview.Width = 130;
            //     button_preview.Height = 30;
            // }
            
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
            float desired = Math.Max(button.Font?.Size ?? 10f, 10.5f);
            button.Font = new Font("Microsoft Sans Serif", desired, FontStyle.Bold);
            button.Padding = new Padding(6, 3, 6, 3);
            // Keep generous defaults, but allow small overrides for specific buttons
            if (button != button_FetchEmployees)
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
            button.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Bold);
            button.Padding = new Padding(6, 3, 6, 3);
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

                    if (loadingProgressBar != null)
                    {
                        loadingProgressBar.Left = (loginGroupBox != null) ? loginGroupBox.Left : Math.Max(marginX, (cw - loadingProgressBar.Width) / 2);
                        int desiredTop = (loginStatusLabel?.Bottom ?? topMargin) + spacing;
                        loadingProgressBar.Top = Math.Min(ch - loadingProgressBar.Height - spacing, desiredTop);
                    }

                    if (loadingStatusLabel != null)
                    {
                        loadingStatusLabel.Left = (loginGroupBox != null) ? loginGroupBox.Left : Math.Max(marginX, (cw - loadingStatusLabel.Width) / 2);
                        int desiredTop = (loadingProgressBar?.Bottom ?? topMargin) + spacing;
                        loadingStatusLabel.Top = Math.Min(ch - loadingStatusLabel.Height - spacing, desiredTop);
                    }
                }


                // Printer content header buttons
                // if (printerContentPanel != null && printerContentPanel.Visible)
                // {
                //     if (Scanner != null)
                //     {
                //         Scanner.Left = Math.Max(marginX, cw - Scanner.Width - marginX);
                //         Scanner.Top = topMargin + 20;
                //     }
                // }

                // Scanner content layout
                if (scannerContentPanel != null && scannerContentPanel.Visible)
                {
                    int availableW = Math.Max(320, cw - 2 * marginX);


                    int headerBottom = topMargin;

                    // Position checkbox and textbox within the scannerOutputPanel
                    if (scannerOutputPanel != null)
                    {
                        int panelMarginX = 10;
                        int panelSpacing = 50;
                        int panelTopMargin = 5;

                        if (showScannerOutputCheckBox != null)
                        {
                            showScannerOutputCheckBox.Left = panelMarginX;
                            showScannerOutputCheckBox.Top = panelTopMargin;
                        }
                        if (button_manualUpload != null)
                        {
                            button_manualUpload.Top = panelTopMargin;
                            button_manualUpload.Left = scannerOutputPanel.Width - button_manualUpload.Width - panelMarginX-20;
                        }
                        if (scannerOutputTextBox != null)
                        {
                            int leftMargin = (showScannerOutputCheckBox?.Right ?? panelMarginX) + panelSpacing;
                            int rightMargin = (button_manualUpload?.Left ?? scannerOutputPanel.Width - panelMarginX) - panelSpacing;
                            scannerOutputTextBox.Left = leftMargin;
                            scannerOutputTextBox.Width = Math.Max(100, rightMargin - leftMargin-50);
                            scannerOutputTextBox.Top = panelTopMargin;
                        }
                    }
                    // TableLayoutPanel handles positioning of filtersPanel, scannerDataGridView, and paginationPanel automatically
                    // Only need to ensure proper sizing within table cells

                    // Row 1 now uses AutoSize, so scannerOutputPanel will automatically adjust its height

                    // Update DataGridView column widths to utilize full available space
                    UpdateDataGridViewColumnWidths();
                    
                    // Update count labels
                    UpdateCountLabels();

                    // TableLayoutPanel handles positioning of statsPanel automatically
                }

                // Printer content widths and centering
                // if (printerContentPanel != null && printerContentPanel.Visible)
                // {
                //     LayoutPrinterContent(cw);
                // }
            }
            catch { }
        }

        // private void LayoutPrinterContent(int clientWidth)
        // {
        //     // Dock-based layout now handles stacking; no manual positioning needed.
        //     return;
        // }
    }
}