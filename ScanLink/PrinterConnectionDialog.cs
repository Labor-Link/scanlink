using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScanLink
{
    public partial class PrinterConnectionDialog : Form
    {
        private Form1 _mainForm;

        public PrinterConnectionDialog(Form1 mainForm)
        {
            InitializeComponent();
            _mainForm = mainForm;
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            // Initialize comboBox_port with options
            string[] strPort = { "USB", "File", "COM", "LAN", "Multi-LAN" };
            foreach (string str in strPort) comboBox_port.Items.Add(str);
            comboBox_port.Text = _mainForm.CurrentConnectionType;

            // Update the UI to reflect current state
            UpdateConnectionUI();
        }

        private void comboBox_port_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Update the main form's current connection type
            _mainForm.CurrentConnectionType = comboBox_port.Text;

            // Call the main form's method to update the connection UI
            _mainForm.UpdateConnectionUI(comboBox_port.Text, textBox_port, connectionStatusLabel);
        }

        private void button_setting_Click(object sender, EventArgs e)
        {
            // Call the main form's configure method
            _mainForm.HandleConnectionConfigure(comboBox_port.Text, this);

            // Update the UI after configuration
            UpdateConnectionUI();
        }

        private void UpdateConnectionUI()
        {
            // Update the UI based on current connection type
            _mainForm.UpdateConnectionUI(comboBox_port.Text, textBox_port, connectionStatusLabel);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Hide the dialog instead of closing it
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            base.OnFormClosing(e);
        }
    }
}
