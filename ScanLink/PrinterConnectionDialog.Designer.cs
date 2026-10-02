using System.Windows.Forms;
using ScanLink.DesignSystem;

namespace ScanLink
{
    // Hand-written layout on the design system (no WinForms designer): see design/README.md.
    // Mirrors the mockup's "How is the printer connected?" dialog (scene dialog-printer).
    partial class PrinterConnectionDialog
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            Name = "PrinterConnectionDialog";
            Title = "How is the printer connected?";
            Description = "Choose how this computer reaches the label printer. If you're not sure, ask whoever set it up.";
            DialogWidth = 480;

            comboBox_port = new SLComboBox { Name = "comboBox_port" };
            comboBox_port.SelectedIndexChanged += comboBox_port_SelectedIndexChanged;

            textBox_port = new SLTextBox { Name = "textBox_port", ReadOnly = true, Mono = true };

            statusBadge = new SLBadge { Text = "Not set up", Tone = SLTone.Neutral, Dot = true };
            SLStack statusRow = new SLStack(SLOrientation.Horizontal, 0);
            statusRow.Controls.Add(statusBadge);

            SLFieldSet fields = new SLFieldSet { Columns = 1 };
            fields.Add(new SLField("Connection", comboBox_port) { Hint = "Network is the usual choice in a packhouse." });
            fields.Add(new SLField("Printer address", textBox_port) { Hint = "Filled in when you configure the connection." });
            fields.Add(new SLField("Status", statusRow));
            Body.Controls.Add(fields);

            button_close = new SLButton { Name = "button_close", Text = "Close", Variant = SLVariant.Secondary, DialogResult = DialogResult.Cancel };
            button_close.Click += (s, e) => Close();
            button_setting = new SLButton { Name = "button_setting", Text = "Configure connection", IconName = "settings" };
            button_setting.Click += button_setting_Click;
            AddAction(button_close);
            AddAction(button_setting);
        }

        private SLComboBox comboBox_port;
        private SLTextBox textBox_port;
        private SLBadge statusBadge;
        private SLButton button_setting;
        private SLButton button_close;
    }
}
