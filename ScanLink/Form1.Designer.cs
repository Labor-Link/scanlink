namespace ScanLink
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) { components.Dispose(); }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
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
            this.label_emulation = new System.Windows.Forms.Label();
            this.comboBox_emulation = new System.Windows.Forms.ComboBox();
            this.label_test = new System.Windows.Forms.Label();
            this.comboBox_test = new System.Windows.Forms.ComboBox();
            this.label_barcode = new System.Windows.Forms.Label();
            this.comboBox_barcode = new System.Windows.Forms.ComboBox();
            this.label_count = new System.Windows.Forms.Label();
            this.numericUpDown_count = new System.Windows.Forms.NumericUpDown();
            this.advancedPanel = new System.Windows.Forms.Panel();
            this.advancedGroupBox = new System.Windows.Forms.GroupBox();
            this.barcodeTextPanel = new System.Windows.Forms.Panel();
            this.label_barcodeText = new System.Windows.Forms.Label();
            this.textBox_barcodeText = new System.Windows.Forms.TextBox();
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
            this.mainPanel.SuspendLayout();
            this.headerPanel.SuspendLayout();
            this.connectionPanel.SuspendLayout();
            this.connectionGroupBox.SuspendLayout();
            this.configPanel.SuspendLayout();
            this.configGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_count)).BeginInit();
            this.advancedPanel.SuspendLayout();
            this.advancedGroupBox.SuspendLayout();
            this.barcodeTextPanel.SuspendLayout();
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
            // mainPanel
            // 
            this.mainPanel.AutoScroll = true;
            this.mainPanel.BackColor = System.Drawing.Color.White;
            this.mainPanel.Controls.Add(this.headerPanel);
            this.mainPanel.Controls.Add(this.connectionPanel);
            this.mainPanel.Controls.Add(this.configPanel);
            this.mainPanel.Controls.Add(this.advancedPanel);
            this.mainPanel.Controls.Add(this.actionPanel);
            this.mainPanel.Controls.Add(this.statusPanel);
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.Location = new System.Drawing.Point(0, 0);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Padding = new System.Windows.Forms.Padding(20);
            this.mainPanel.Size = new System.Drawing.Size(600, 1000);
            this.mainPanel.TabIndex = 0;
            
            // 
            // headerPanel
            // 
            this.headerPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(41)))), ((int)(((byte)(128)))), ((int)(((byte)(185)))));
            this.headerPanel.Controls.Add(this.titleLabel);
            this.headerPanel.Controls.Add(this.subtitleLabel);
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
            this.titleLabel.Font = new System.Drawing.Font("Segoe UI", 24F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.titleLabel.ForeColor = System.Drawing.Color.White;
            this.titleLabel.Location = new System.Drawing.Point(30, 20);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(168, 45);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "Scan Link";
            
            // 
            // subtitleLabel
            // 
            this.subtitleLabel.AutoSize = true;
            this.subtitleLabel.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.subtitleLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(240)))), ((int)(((byte)(241)))));
            this.subtitleLabel.Location = new System.Drawing.Point(35, 65);
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
            this.connectionPanel.Size = new System.Drawing.Size(560, 150);
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
            this.connectionGroupBox.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.connectionGroupBox.Location = new System.Drawing.Point(0, 20);
            this.connectionGroupBox.Name = "connectionGroupBox";
            this.connectionGroupBox.Padding = new System.Windows.Forms.Padding(20);
            this.connectionGroupBox.Size = new System.Drawing.Size(560, 130);
            this.connectionGroupBox.TabIndex = 0;
            this.connectionGroupBox.TabStop = false;
            this.connectionGroupBox.Text = "🔌 Printer Connection";
            
            // 
            // connectionStatusLabel
            // 
            this.connectionStatusLabel.AutoSize = true;
            this.connectionStatusLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.connectionStatusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(149)))), ((int)(((byte)(165)))), ((int)(((byte)(166)))));
            this.connectionStatusLabel.Location = new System.Drawing.Point(25, 100);
            this.connectionStatusLabel.Name = "connectionStatusLabel";
            this.connectionStatusLabel.Size = new System.Drawing.Size(104, 15);
            this.connectionStatusLabel.TabIndex = 4;
            this.connectionStatusLabel.Text = "Status: Disconnected";
            
            // 
            // label_port
            // 
            this.label_port.AutoSize = true;
            this.label_port.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.label_port.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
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
            this.button_setting.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(152)))), ((int)(((byte)(219)))));
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
            this.configPanel.Location = new System.Drawing.Point(20, 270);
            this.configPanel.Name = "configPanel";
            this.configPanel.Padding = new System.Windows.Forms.Padding(0, 20, 0, 0);
            this.configPanel.Size = new System.Drawing.Size(560, 220);
            this.configPanel.TabIndex = 2;
            
            // 
            // configGroupBox
            // 
            this.configGroupBox.Controls.Add(this.label_emulation);
            this.configGroupBox.Controls.Add(this.comboBox_emulation);
            this.configGroupBox.Controls.Add(this.label_test);
            this.configGroupBox.Controls.Add(this.comboBox_test);
            this.configGroupBox.Controls.Add(this.label_barcode);
            this.configGroupBox.Controls.Add(this.comboBox_barcode);
            this.configGroupBox.Controls.Add(this.label_count);
            this.configGroupBox.Controls.Add(this.numericUpDown_count);
            this.configGroupBox.Controls.Add(this.checkBox_showAdvanced);
            this.configGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.configGroupBox.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.configGroupBox.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.configGroupBox.Location = new System.Drawing.Point(0, 20);
            this.configGroupBox.Name = "configGroupBox";
            this.configGroupBox.Padding = new System.Windows.Forms.Padding(20);
            this.configGroupBox.Size = new System.Drawing.Size(560, 200);
            this.configGroupBox.TabIndex = 0;
            this.configGroupBox.TabStop = false;
            this.configGroupBox.Text = "⚙️ Print Configuration";
            
            // 
            // label_emulation
            // 
            this.label_emulation.AutoSize = true;
            this.label_emulation.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.label_emulation.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.label_emulation.Location = new System.Drawing.Point(25, 35);
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
            this.comboBox_emulation.Location = new System.Drawing.Point(140, 32);
            this.comboBox_emulation.Name = "comboBox_emulation";
            this.comboBox_emulation.Size = new System.Drawing.Size(390, 23);
            this.comboBox_emulation.TabIndex = 1;
            this.comboBox_emulation.SelectedIndexChanged += new System.EventHandler(this.comboBox_emulation_SelectedIndexChanged);
            
            // 
            // label_test
            // 
            this.label_test.AutoSize = true;
            this.label_test.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.label_test.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.label_test.Location = new System.Drawing.Point(25, 70);
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
            this.comboBox_test.Location = new System.Drawing.Point(140, 67);
            this.comboBox_test.Name = "comboBox_test";
            this.comboBox_test.Size = new System.Drawing.Size(390, 23);
            this.comboBox_test.TabIndex = 3;
            this.comboBox_test.SelectedIndexChanged += new System.EventHandler(this.comboBox_test_SelectedIndexChanged);
            
            // 
            // label_barcode
            // 
            this.label_barcode.AutoSize = true;
            this.label_barcode.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.label_barcode.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.label_barcode.Location = new System.Drawing.Point(25, 105);
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
            this.comboBox_barcode.Location = new System.Drawing.Point(140, 102);
            this.comboBox_barcode.Name = "comboBox_barcode";
            this.comboBox_barcode.Size = new System.Drawing.Size(390, 23);
            this.comboBox_barcode.TabIndex = 5;
            
            // 
            // label_count
            // 
            this.label_count.AutoSize = true;
            this.label_count.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.label_count.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.label_count.Location = new System.Drawing.Point(25, 140);
            this.label_count.Name = "label_count";
            this.label_count.Size = new System.Drawing.Size(65, 15);
            this.label_count.TabIndex = 6;
            this.label_count.Text = "Print Count";
            
            // 
            // numericUpDown_count
            // 
            this.numericUpDown_count.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.numericUpDown_count.Location = new System.Drawing.Point(140, 137);
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
            this.checkBox_showAdvanced.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.checkBox_showAdvanced.Location = new System.Drawing.Point(25, 175);
            this.checkBox_showAdvanced.Name = "checkBox_showAdvanced";
            this.checkBox_showAdvanced.Size = new System.Drawing.Size(189, 19);
            this.checkBox_showAdvanced.TabIndex = 8;
            this.checkBox_showAdvanced.Text = "🔧 Show Advanced Settings";
            this.checkBox_showAdvanced.UseVisualStyleBackColor = true;
            this.checkBox_showAdvanced.CheckedChanged += new System.EventHandler(this.checkBox_showAdvanced_CheckedChanged);
            
            // 
            // advancedPanel
            // 
            this.advancedPanel.Controls.Add(this.advancedGroupBox);
            this.advancedPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.advancedPanel.Location = new System.Drawing.Point(20, 490);
            this.advancedPanel.Name = "advancedPanel";
            this.advancedPanel.Padding = new System.Windows.Forms.Padding(0, 20, 0, 0);
            this.advancedPanel.Size = new System.Drawing.Size(560, 400);
            this.advancedPanel.TabIndex = 3;
            this.advancedPanel.Visible = false;
            
            // 
            // advancedGroupBox
            // 
            this.advancedGroupBox.Controls.Add(this.barcodeTextPanel);
            this.advancedGroupBox.Controls.Add(this.dimensionsPanel);
            this.advancedGroupBox.Controls.Add(this.alignmentPanel);
            this.advancedGroupBox.Controls.Add(this.qualityPanel);
            this.advancedGroupBox.Controls.Add(this.previewPanel);
            this.advancedGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.advancedGroupBox.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.advancedGroupBox.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.advancedGroupBox.Location = new System.Drawing.Point(0, 20);
            this.advancedGroupBox.Name = "advancedGroupBox";
            this.advancedGroupBox.Padding = new System.Windows.Forms.Padding(20);
            this.advancedGroupBox.Size = new System.Drawing.Size(560, 380);
            this.advancedGroupBox.TabIndex = 0;
            this.advancedGroupBox.TabStop = false;
            this.advancedGroupBox.Text = "🔧 Advanced Print Settings";
            
            // 
            // barcodeTextPanel
            // 
            this.barcodeTextPanel.Controls.Add(this.label_barcodeText);
            this.barcodeTextPanel.Controls.Add(this.textBox_barcodeText);
            this.barcodeTextPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.barcodeTextPanel.Location = new System.Drawing.Point(20, 35);
            this.barcodeTextPanel.Name = "barcodeTextPanel";
            this.barcodeTextPanel.Size = new System.Drawing.Size(520, 50);
            this.barcodeTextPanel.TabIndex = 0;
            
            // 
            // label_barcodeText
            // 
            this.label_barcodeText.AutoSize = true;
            this.label_barcodeText.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.label_barcodeText.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.label_barcodeText.Location = new System.Drawing.Point(5, 15);
            this.label_barcodeText.Name = "label_barcodeText";
            this.label_barcodeText.Size = new System.Drawing.Size(77, 15);
            this.label_barcodeText.TabIndex = 0;
            this.label_barcodeText.Text = "Barcode Text";
            
            // 
            // textBox_barcodeText
            // 
            this.textBox_barcodeText.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.textBox_barcodeText.Location = new System.Drawing.Point(120, 12);
            this.textBox_barcodeText.Name = "textBox_barcodeText";
            this.textBox_barcodeText.Size = new System.Drawing.Size(390, 23);
            this.textBox_barcodeText.TabIndex = 1;
            this.textBox_barcodeText.Text = "1234567890";
            
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
            this.dimensionsPanel.Location = new System.Drawing.Point(20, 85);
            this.dimensionsPanel.Name = "dimensionsPanel";
            this.dimensionsPanel.Size = new System.Drawing.Size(520, 80);
            this.dimensionsPanel.TabIndex = 1;
            
            // 
            // label_width
            // 
            this.label_width.AutoSize = true;
            this.label_width.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.label_width.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
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
            this.numericUpDown_width.Value = new decimal(new int[] { 200, 0, 0, 0 });
            
            // 
            // label_height
            // 
            this.label_height.AutoSize = true;
            this.label_height.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.label_height.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.label_height.Location = new System.Drawing.Point(240, 15);
            this.label_height.Name = "label_height";
            this.label_height.Size = new System.Drawing.Size(43, 15);
            this.label_height.TabIndex = 2;
            this.label_height.Text = "Height";
            
            // 
            // numericUpDown_height
            // 
            this.numericUpDown_height.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.numericUpDown_height.Location = new System.Drawing.Point(300, 12);
            this.numericUpDown_height.Maximum = new decimal(new int[] { 999, 0, 0, 0 });
            this.numericUpDown_height.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDown_height.Name = "numericUpDown_height";
            this.numericUpDown_height.Size = new System.Drawing.Size(100, 23);
            this.numericUpDown_height.TabIndex = 3;
            this.numericUpDown_height.Value = new decimal(new int[] { 100, 0, 0, 0 });
            
            // 
            // label_gap
            // 
            this.label_gap.AutoSize = true;
            this.label_gap.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.label_gap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
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
            
            // 
            // alignmentPanel
            // 
            this.alignmentPanel.Controls.Add(this.label_alignment);
            this.alignmentPanel.Controls.Add(this.comboBox_alignment);
            this.alignmentPanel.Controls.Add(this.label_rotation);
            this.alignmentPanel.Controls.Add(this.comboBox_rotation);
            this.alignmentPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.alignmentPanel.Location = new System.Drawing.Point(20, 165);
            this.alignmentPanel.Name = "alignmentPanel";
            this.alignmentPanel.Size = new System.Drawing.Size(520, 50);
            this.alignmentPanel.TabIndex = 2;
            
            // 
            // label_alignment
            // 
            this.label_alignment.AutoSize = true;
            this.label_alignment.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.label_alignment.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
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
            this.comboBox_alignment.Size = new System.Drawing.Size(150, 23);
            this.comboBox_alignment.TabIndex = 1;
            this.comboBox_alignment.SelectedIndex = 0;
            
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
            
            // 
            // comboBox_rotation
            // 
            this.comboBox_rotation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_rotation.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.comboBox_rotation.FormattingEnabled = true;
            this.comboBox_rotation.Items.AddRange(new object[] { "0°", "90°", "180°", "270°" });
            this.comboBox_rotation.Location = new System.Drawing.Point(360, 12);
            this.comboBox_rotation.Name = "comboBox_rotation";
            this.comboBox_rotation.Size = new System.Drawing.Size(150, 23);
            this.comboBox_rotation.TabIndex = 3;
            this.comboBox_rotation.SelectedIndex = 0;
            
            // 
            // qualityPanel
            // 
            this.qualityPanel.Controls.Add(this.label_darkness);
            this.qualityPanel.Controls.Add(this.trackBar_darkness);
            this.qualityPanel.Controls.Add(this.label_darknessValue);
            this.qualityPanel.Controls.Add(this.label_speed);
            this.qualityPanel.Controls.Add(this.comboBox_speed);
            this.qualityPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.qualityPanel.Location = new System.Drawing.Point(20, 215);
            this.qualityPanel.Name = "qualityPanel";
            this.qualityPanel.Size = new System.Drawing.Size(520, 80);
            this.qualityPanel.TabIndex = 3;
            
            // 
            // label_darkness
            // 
            this.label_darkness.AutoSize = true;
            this.label_darkness.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.label_darkness.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
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
            this.label_darknessValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(152)))), ((int)(((byte)(219)))));
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
            this.label_speed.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.label_speed.Location = new System.Drawing.Point(5, 50);
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
            this.comboBox_speed.Location = new System.Drawing.Point(120, 47);
            this.comboBox_speed.Name = "comboBox_speed";
            this.comboBox_speed.Size = new System.Drawing.Size(200, 23);
            this.comboBox_speed.TabIndex = 4;
            this.comboBox_speed.SelectedIndex = 4;
            
            // 
            // previewPanel
            // 
            this.previewPanel.Controls.Add(this.button_preview);
            this.previewPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.previewPanel.Location = new System.Drawing.Point(20, 295);
            this.previewPanel.Name = "previewPanel";
            this.previewPanel.Size = new System.Drawing.Size(520, 60);
            this.previewPanel.TabIndex = 4;
            
            // 
            // button_preview
            // 
            this.button_preview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(142)))), ((int)(((byte)(68)))), ((int)(((byte)(173)))));
            this.button_preview.FlatAppearance.BorderSize = 0;
            this.button_preview.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_preview.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.button_preview.ForeColor = System.Drawing.Color.White;
            this.button_preview.Location = new System.Drawing.Point(5, 15);
            this.button_preview.Name = "button_preview";
            this.button_preview.Size = new System.Drawing.Size(200, 35);
            this.button_preview.TabIndex = 0;
            this.button_preview.Text = "👁️ Preview Label";
            this.button_preview.UseVisualStyleBackColor = false;
            this.button_preview.Click += new System.EventHandler(this.button_preview_Click);
            
            // 
            // actionPanel
            // 
            this.actionPanel.Controls.Add(this.button_send);
            this.actionPanel.Controls.Add(this.progressBar);
            this.actionPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.actionPanel.Location = new System.Drawing.Point(20, 890);
            this.actionPanel.Name = "actionPanel";
            this.actionPanel.Padding = new System.Windows.Forms.Padding(0, 30, 0, 0);
            this.actionPanel.Size = new System.Drawing.Size(560, 120);
            this.actionPanel.TabIndex = 3;
            
            // 
            // button_send
            // 
            this.button_send.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(204)))), ((int)(((byte)(113)))));
            this.button_send.FlatAppearance.BorderSize = 0;
            this.button_send.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_send.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.button_send.ForeColor = System.Drawing.Color.White;
            this.button_send.Location = new System.Drawing.Point(0, 30);
            this.button_send.Name = "button_send";
            this.button_send.Size = new System.Drawing.Size(560, 50);
            this.button_send.TabIndex = 0;
            this.button_send.Text = "🖨️ Start Printing";
            this.button_send.UseVisualStyleBackColor = false;
            this.button_send.Click += new System.EventHandler(this.button_send_Click);
            
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(0, 90);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(560, 20);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar.TabIndex = 1;
            this.progressBar.Visible = false;
            
            // 
            // statusPanel
            // 
            this.statusPanel.Controls.Add(this.statusLabel);
            this.statusPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.statusPanel.Location = new System.Drawing.Point(20, 1010);
            this.statusPanel.Name = "statusPanel";
            this.statusPanel.Padding = new System.Windows.Forms.Padding(0, 20, 0, 20);
            this.statusPanel.Size = new System.Drawing.Size(560, 70);
            this.statusPanel.TabIndex = 4;
            
            // 
            // statusLabel
            // 
            this.statusLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(249)))), ((int)(((byte)(249)))));
            this.statusLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.statusLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.statusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(127)))), ((int)(((byte)(140)))), ((int)(((byte)(141)))));
            this.statusLabel.Location = new System.Drawing.Point(0, 20);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Padding = new System.Windows.Forms.Padding(20, 10, 20, 10);
            this.statusLabel.Size = new System.Drawing.Size(560, 30);
            this.statusLabel.TabIndex = 0;
            this.statusLabel.Text = "Ready to print. Select your printer connection and configure settings above.";
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(600, 1100);
            this.Controls.Add(this.mainPanel);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MinimumSize = new System.Drawing.Size(600, 650);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Scan Link - Professional Barcode Printing";
            this.Load += new System.EventHandler(this.Form1_Load);
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
            this.statusPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }

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
        private System.Windows.Forms.Label label_emulation;
        private System.Windows.Forms.ComboBox comboBox_emulation;
        private System.Windows.Forms.Label label_test;
        private System.Windows.Forms.ComboBox comboBox_test;
        private System.Windows.Forms.Label label_barcode;
        private System.Windows.Forms.ComboBox comboBox_barcode;
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
        private System.Windows.Forms.Panel barcodeTextPanel;
        private System.Windows.Forms.Label label_barcodeText;
        private System.Windows.Forms.TextBox textBox_barcodeText;
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
    }
}


