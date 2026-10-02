using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ScanLink.DesignSystem;

namespace ScanLink
{
    [DesignerCategory("Code")]
    internal partial class PrinterConnectionDialog : SLDialog
    {
        /// <summary>Connection keys Form1 understands, with the words people see.</summary>
        private sealed class ConnectionOption
        {
            public string Key;
            public string Label;
            public override string ToString() { return Label; }
        }

        private static readonly ConnectionOption[] Options =
        {
            new ConnectionOption { Key = "LAN", Label = "Network (LAN)" },
            new ConnectionOption { Key = "USB", Label = "USB cable" },
            new ConnectionOption { Key = "COM", Label = "Serial cable (COM)" },
            new ConnectionOption { Key = "File", Label = "Save to a file" },
            new ConnectionOption { Key = "Multi-LAN", Label = "Several network printers (Multi-LAN)" }
        };

        private readonly Form1 _mainForm;

        // Form1.UpdateConnectionUI writes into a TextBox and a Label. The TextBox is the inner
        // box of textBox_port; the Label is never shown — its text and colour are translated
        // into statusBadge.
        private readonly Label _statusProxy = new Label();
        private bool _cleaningAddress;

        public PrinterConnectionDialog(Form1 mainForm)
        {
            InitializeComponent();
            _mainForm = mainForm;
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            _statusProxy.TextChanged += (s, e) => SyncStatus();
            _statusProxy.ForeColorChanged += (s, e) => SyncStatus();
            textBox_port.Inner.TextChanged += (s, e) => CleanAddress();

            comboBox_port.Items.AddRange(Options);
            SelectKey(_mainForm.CurrentConnectionType);

            UpdateConnectionUI();
        }

        private string SelectedKey
        {
            get
            {
                ConnectionOption o = comboBox_port.SelectedItem as ConnectionOption;
                return o == null ? "" : o.Key;
            }
        }

        private void SelectKey(string key)
        {
            foreach (ConnectionOption o in Options)
            {
                if (string.Equals(o.Key, key, StringComparison.OrdinalIgnoreCase)) { comboBox_port.SelectedItem = o; return; }
            }
        }

        private void comboBox_port_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_mainForm == null || comboBox_port.SelectedIndex < 0) return;
            _mainForm.CurrentConnectionType = SelectedKey;
            _mainForm.UpdateConnectionUI(SelectedKey, textBox_port.Inner, _statusProxy);
        }

        private void button_setting_Click(object sender, EventArgs e)
        {
            _mainForm.HandleConnectionConfigure(SelectedKey, this);
            UpdateConnectionUI();
        }

        private void UpdateConnectionUI()
        {
            if (comboBox_port.SelectedIndex < 0) return;
            _mainForm.UpdateConnectionUI(SelectedKey, textBox_port.Inner, _statusProxy);
        }

        /// <summary>"Status: Serial port ready" in Info500 -> an Info badge "Serial port ready".</summary>
        private void SyncStatus()
        {
            string text = _statusProxy.Text ?? "";
            if (text.StartsWith("Status:", StringComparison.OrdinalIgnoreCase)) text = text.Substring(7).Trim();
            if (text.Length > 0) text = char.ToUpper(text[0]) + text.Substring(1);
            statusBadge.Text = text.Length == 0 ? "Not set up" : text;
            statusBadge.Tone = ToneFor(_statusProxy.ForeColor);
            if (statusBadge.Parent != null) statusBadge.Parent.PerformLayout();
        }

        private static SLTone ToneFor(Color c)
        {
            int argb = c.ToArgb();
            if (argb == Theme.Ok500.ToArgb() || argb == Theme.Ok700.ToArgb()) return SLTone.Success;
            if (argb == Theme.Info500.ToArgb()) return SLTone.Info;
            if (argb == Theme.Warn500.ToArgb() || argb == Theme.Warn700.ToArgb()) return SLTone.Warning;
            if (argb == Theme.Err500.ToArgb() || argb == Theme.Err700.ToArgb()) return SLTone.Error;
            return SLTone.Neutral;
        }

        /// <summary>Form1 prefixes the address with an emoji ("🌐 Network: …"); the field shows plain text.</summary>
        private void CleanAddress()
        {
            if (_cleaningAddress) return;
            string clean = Themed.IconSet.StripLeadingGlyph(textBox_port.Inner.Text);
            if (clean == textBox_port.Inner.Text) return;
            _cleaningAddress = true;
            textBox_port.Inner.Text = clean;
            _cleaningAddress = false;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Hide instead of closing: Form1 keeps one instance and shows it again.
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
            base.OnFormClosing(e);
        }
    }
}
