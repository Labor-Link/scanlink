namespace ScanLink
{
    partial class PrinterConnectionDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.connectionGroupBox = new System.Windows.Forms.GroupBox();
            this.connectionStatusLabel = new System.Windows.Forms.Label();
            this.label_port = new System.Windows.Forms.Label();
            this.comboBox_port = new System.Windows.Forms.ComboBox();
            this.button_setting = new System.Windows.Forms.Button();
            this.textBox_port = new System.Windows.Forms.TextBox();
            this.connectionGroupBox.SuspendLayout();
            this.SuspendLayout();
            //
            // connectionGroupBox
            //
            this.connectionGroupBox.Controls.Add(this.connectionStatusLabel);
            this.connectionGroupBox.Controls.Add(this.label_port);
            this.connectionGroupBox.Controls.Add(this.comboBox_port);
            this.connectionGroupBox.Controls.Add(this.button_setting);
            this.connectionGroupBox.Controls.Add(this.textBox_port);
            this.connectionGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.connectionGroupBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
            this.connectionGroupBox.ForeColor = System.Drawing.Color.FromArgb(12, 24, 33);
            this.connectionGroupBox.Location = new System.Drawing.Point(0, 0);
            this.connectionGroupBox.Name = "connectionGroupBox";
            this.connectionGroupBox.Padding = new System.Windows.Forms.Padding(20, 20, 20, 20);
            this.connectionGroupBox.Size = new System.Drawing.Size(600, 180);
            this.connectionGroupBox.TabIndex = 0;
            this.connectionGroupBox.TabStop = false;
            this.connectionGroupBox.Text = "🔌 Printer Connection";
            this.connectionGroupBox.AutoSize = true;
            this.connectionGroupBox.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            //
            // connectionStatusLabel
            //
            this.connectionStatusLabel.AutoSize = true;
            this.connectionStatusLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
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
            this.label_port.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
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
            this.comboBox_port.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
            this.comboBox_port.FormattingEnabled = true;
            this.comboBox_port.Location = new System.Drawing.Point(190, 32);
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
            this.button_setting.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
            this.button_setting.ForeColor = System.Drawing.Color.White;
            this.button_setting.Location = new System.Drawing.Point(460, 32);
            this.button_setting.Name = "button_setting";
            this.button_setting.Size = new System.Drawing.Size(120, 32);
            this.button_setting.TabIndex = 2;
            this.button_setting.Text = "Configure";
            this.button_setting.UseVisualStyleBackColor = false;
            this.button_setting.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(27, 42, 65);
            this.button_setting.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(0, 0, 0);
            this.button_setting.Click += new System.EventHandler(this.button_setting_Click);
            //
            // textBox_port
            //
            this.textBox_port.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
            this.textBox_port.Location = new System.Drawing.Point(190, 70);
            this.textBox_port.Name = "textBox_port";
            this.textBox_port.ReadOnly = true;
            this.textBox_port.Size = new System.Drawing.Size(390, 23);
            this.textBox_port.TabIndex = 3;
            //
            // PrinterConnectionDialog
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(600, 180);
            this.Controls.Add(this.connectionGroupBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PrinterConnectionDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Printer Connection Settings";
            this.connectionGroupBox.ResumeLayout(false);
            this.connectionGroupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox connectionGroupBox;
        private System.Windows.Forms.Label connectionStatusLabel;
        private System.Windows.Forms.Label label_port;
        private System.Windows.Forms.ComboBox comboBox_port;
        private System.Windows.Forms.Button button_setting;
        private System.Windows.Forms.TextBox textBox_port;
    }
}
