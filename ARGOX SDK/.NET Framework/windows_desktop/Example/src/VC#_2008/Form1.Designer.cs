namespace VCSharp_2008
{
    partial class Form1
    {
        /// <summary>
        /// 設計工具所需的變數。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清除任何使用中的資源。
        /// </summary>
        /// <param name="disposing">如果應該處置 Managed 資源則為 true，否則為 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 設計工具產生的程式碼

        /// <summary>
        /// 此為設計工具支援所需的方法 - 請勿使用程式碼編輯器修改這個方法的內容。
        ///
        /// </summary>
        private void InitializeComponent()
        {
            this.button_send = new System.Windows.Forms.Button();
            this.label_port = new System.Windows.Forms.Label();
            this.comboBox_port = new System.Windows.Forms.ComboBox();
            this.button_setting = new System.Windows.Forms.Button();
            this.textBox_port = new System.Windows.Forms.TextBox();
            this.comboBox_test = new System.Windows.Forms.ComboBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label_emulation = new System.Windows.Forms.Label();
            this.comboBox_emulation = new System.Windows.Forms.ComboBox();
            this.comboBox_barcode = new System.Windows.Forms.ComboBox();
            this.label_barcode = new System.Windows.Forms.Label();
            this.label_count = new System.Windows.Forms.Label();
            this.label_test = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.numericUpDown_count = new System.Windows.Forms.NumericUpDown();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_count)).BeginInit();
            this.SuspendLayout();
            // 
            // button_send
            // 
            this.button_send.Location = new System.Drawing.Point(6, 14);
            this.button_send.Name = "button_send";
            this.button_send.Size = new System.Drawing.Size(99, 27);
            this.button_send.TabIndex = 0;
            this.button_send.Text = "Send";
            this.button_send.UseVisualStyleBackColor = true;
            this.button_send.Click += new System.EventHandler(this.button_send_Click);
            // 
            // label_port
            // 
            this.label_port.AutoSize = true;
            this.label_port.Location = new System.Drawing.Point(10, 21);
            this.label_port.Name = "label_port";
            this.label_port.Size = new System.Drawing.Size(24, 12);
            this.label_port.TabIndex = 1;
            this.label_port.Text = "Port";
            // 
            // comboBox_port
            // 
            this.comboBox_port.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_port.FormattingEnabled = true;
            this.comboBox_port.Location = new System.Drawing.Point(75, 18);
            this.comboBox_port.Name = "comboBox_port";
            this.comboBox_port.Size = new System.Drawing.Size(209, 20);
            this.comboBox_port.TabIndex = 1;
            this.comboBox_port.Tag = "";
            this.comboBox_port.SelectedIndexChanged += new System.EventHandler(this.comboBox_port_SelectedIndexChanged);
            // 
            // button_setting
            // 
            this.button_setting.Location = new System.Drawing.Point(6, 41);
            this.button_setting.Name = "button_setting";
            this.button_setting.Size = new System.Drawing.Size(63, 27);
            this.button_setting.TabIndex = 2;
            this.button_setting.Text = "Setting";
            this.button_setting.UseVisualStyleBackColor = true;
            this.button_setting.Click += new System.EventHandler(this.button_setting_Click);
            // 
            // textBox_port
            // 
            this.textBox_port.Location = new System.Drawing.Point(75, 45);
            this.textBox_port.Name = "textBox_port";
            this.textBox_port.ReadOnly = true;
            this.textBox_port.Size = new System.Drawing.Size(209, 22);
            this.textBox_port.TabIndex = 3;
            // 
            // comboBox_test
            // 
            this.comboBox_test.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_test.FormattingEnabled = true;
            this.comboBox_test.Location = new System.Drawing.Point(75, 44);
            this.comboBox_test.Name = "comboBox_test";
            this.comboBox_test.Size = new System.Drawing.Size(209, 20);
            this.comboBox_test.TabIndex = 5;
            this.comboBox_test.SelectedIndexChanged += new System.EventHandler(this.comboBox_test_SelectedIndexChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.textBox_port);
            this.groupBox1.Controls.Add(this.button_setting);
            this.groupBox1.Controls.Add(this.comboBox_port);
            this.groupBox1.Controls.Add(this.label_port);
            this.groupBox1.Location = new System.Drawing.Point(10, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(290, 79);
            this.groupBox1.TabIndex = 8;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Step 1";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.numericUpDown_count);
            this.groupBox2.Controls.Add(this.label_emulation);
            this.groupBox2.Controls.Add(this.comboBox_emulation);
            this.groupBox2.Controls.Add(this.comboBox_barcode);
            this.groupBox2.Controls.Add(this.label_barcode);
            this.groupBox2.Controls.Add(this.label_count);
            this.groupBox2.Controls.Add(this.label_test);
            this.groupBox2.Controls.Add(this.comboBox_test);
            this.groupBox2.Location = new System.Drawing.Point(10, 97);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(290, 134);
            this.groupBox2.TabIndex = 9;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Step 2";
            // 
            // label_emulation
            // 
            this.label_emulation.AutoSize = true;
            this.label_emulation.Location = new System.Drawing.Point(10, 20);
            this.label_emulation.Name = "label_emulation";
            this.label_emulation.Size = new System.Drawing.Size(53, 12);
            this.label_emulation.TabIndex = 14;
            this.label_emulation.Text = "Emulation";
            // 
            // comboBox_emulation
            // 
            this.comboBox_emulation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_emulation.FormattingEnabled = true;
            this.comboBox_emulation.Location = new System.Drawing.Point(75, 17);
            this.comboBox_emulation.Name = "comboBox_emulation";
            this.comboBox_emulation.Size = new System.Drawing.Size(209, 20);
            this.comboBox_emulation.TabIndex = 4;
            this.comboBox_emulation.SelectedIndexChanged += new System.EventHandler(this.comboBox_emulation_SelectedIndexChanged);
            // 
            // comboBox_barcode
            // 
            this.comboBox_barcode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_barcode.FormattingEnabled = true;
            this.comboBox_barcode.Location = new System.Drawing.Point(75, 71);
            this.comboBox_barcode.Name = "comboBox_barcode";
            this.comboBox_barcode.Size = new System.Drawing.Size(209, 20);
            this.comboBox_barcode.TabIndex = 6;
            // 
            // label_barcode
            // 
            this.label_barcode.AutoSize = true;
            this.label_barcode.Location = new System.Drawing.Point(10, 74);
            this.label_barcode.Name = "label_barcode";
            this.label_barcode.Size = new System.Drawing.Size(61, 12);
            this.label_barcode.TabIndex = 11;
            this.label_barcode.Text = "BarcodeUtil";
            // 
            // label_count
            // 
            this.label_count.AutoSize = true;
            this.label_count.Location = new System.Drawing.Point(10, 101);
            this.label_count.Name = "label_count";
            this.label_count.Size = new System.Drawing.Size(59, 12);
            this.label_count.TabIndex = 9;
            this.label_count.Text = "Print Count";
            // 
            // label_test
            // 
            this.label_test.AutoSize = true;
            this.label_test.Location = new System.Drawing.Point(10, 47);
            this.label_test.Name = "label_test";
            this.label_test.Size = new System.Drawing.Size(48, 12);
            this.label_test.TabIndex = 8;
            this.label_test.Text = "Test Item";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.button_send);
            this.groupBox3.Location = new System.Drawing.Point(10, 237);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(290, 47);
            this.groupBox3.TabIndex = 10;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Step 3";
            // 
            // numericUpDown_count
            // 
            this.numericUpDown_count.Location = new System.Drawing.Point(75, 99);
            this.numericUpDown_count.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numericUpDown_count.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown_count.Name = "numericUpDown_count";
            this.numericUpDown_count.Size = new System.Drawing.Size(209, 22);
            this.numericUpDown_count.TabIndex = 7;
            this.numericUpDown_count.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(312, 294);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Name = "Form1";
            this.Text = "Demo BarcodePrinter DLL";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_count)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button button_send;
        private System.Windows.Forms.Label label_port;
        private System.Windows.Forms.ComboBox comboBox_port;
        private System.Windows.Forms.Button button_setting;
        private System.Windows.Forms.TextBox textBox_port;
        private System.Windows.Forms.ComboBox comboBox_test;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label_test;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label_count;
        private System.Windows.Forms.ComboBox comboBox_barcode;
        private System.Windows.Forms.Label label_barcode;
        private System.Windows.Forms.Label label_emulation;
        private System.Windows.Forms.ComboBox comboBox_emulation;
        private System.Windows.Forms.NumericUpDown numericUpDown_count;
    }
}

