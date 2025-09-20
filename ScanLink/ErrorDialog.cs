using System;
using System.Drawing;
using System.Windows.Forms;

namespace ScanLink
{
    public partial class ErrorDialog : Form
    {
        private TextBox textBoxError;
        private Button buttonOK;
        private Button buttonCopy;
        private Label labelTitle;

        public ErrorDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.textBoxError = new TextBox();
            this.buttonOK = new Button();
            this.buttonCopy = new Button();
            this.labelTitle = new Label();
            this.SuspendLayout();

            // 
            // labelTitle
            // 
            this.labelTitle.AutoSize = true;
            this.labelTitle.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));
            this.labelTitle.ForeColor = Color.FromArgb(231, 76, 60);
            this.labelTitle.Location = new Point(12, 9);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new Size(48, 20);
            this.labelTitle.TabIndex = 0;
            this.labelTitle.Text = "Error";

            // 
            // textBoxError
            // 
            this.textBoxError.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom)
                        | AnchorStyles.Left)
                        | AnchorStyles.Right)));
            this.textBoxError.BackColor = Color.White;
            this.textBoxError.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.textBoxError.Location = new Point(12, 40);
            this.textBoxError.Multiline = true;
            this.textBoxError.Name = "textBoxError";
            this.textBoxError.ReadOnly = true;
            this.textBoxError.ScrollBars = ScrollBars.Both;
            this.textBoxError.Size = new Size(560, 300);
            this.textBoxError.TabIndex = 1;
            this.textBoxError.WordWrap = false;

            // 
            // buttonCopy
            // 
            this.buttonCopy.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Right)));
            this.buttonCopy.BackColor = Color.FromArgb(52, 152, 219);
            this.buttonCopy.FlatStyle = FlatStyle.Flat;
            this.buttonCopy.ForeColor = Color.White;
            this.buttonCopy.Location = new Point(377, 350);
            this.buttonCopy.Name = "buttonCopy";
            this.buttonCopy.Size = new Size(90, 30);
            this.buttonCopy.TabIndex = 2;
            this.buttonCopy.Text = "📋 Copy";
            this.buttonCopy.UseVisualStyleBackColor = false;
            this.buttonCopy.Click += new EventHandler(this.ButtonCopy_Click);

            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Right)));
            this.buttonOK.BackColor = Color.FromArgb(95, 39, 205);
            this.buttonOK.DialogResult = DialogResult.OK;
            this.buttonOK.FlatStyle = FlatStyle.Flat;
            this.buttonOK.ForeColor = Color.White;
            this.buttonOK.Location = new Point(482, 350);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new Size(90, 30);
            this.buttonOK.TabIndex = 3;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = false;

            // 
            // ErrorDialog
            // 
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = Color.White;
            this.ClientSize = new Size(584, 391);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.buttonCopy);
            this.Controls.Add(this.textBoxError);
            this.Controls.Add(this.labelTitle);
            this.Icon = SystemIcons.Error;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new Size(400, 300);
            this.Name = "ErrorDialog";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Error Details";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void ButtonCopy_Click(object sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(textBoxError.Text))
                {
                    Clipboard.SetText(textBoxError.Text);
                    
                    // Temporarily change button text to show success
                    string originalText = buttonCopy.Text;
                    buttonCopy.Text = "✅ Copied!";
                    buttonCopy.BackColor = Color.FromArgb(46, 204, 113);
                    
                    // Reset button after 1.5 seconds
                    Timer timer = new Timer();
                    timer.Interval = 1500;
                    timer.Tick += (s, args) =>
                    {
                        buttonCopy.Text = originalText;
                        buttonCopy.BackColor = Color.FromArgb(52, 152, 219);
                        timer.Stop();
                        timer.Dispose();
                    };
                    timer.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy to clipboard: {ex.Message}", "Copy Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public static void ShowError(string title, string errorMessage)
        {
            using (ErrorDialog dialog = new ErrorDialog())
            {
                dialog.labelTitle.Text = title;
                dialog.textBoxError.Text = errorMessage;
                dialog.Text = title;
                dialog.ShowDialog();
            }
        }

        public static void ShowError(string title, string errorMessage, Form parent)
        {
            using (ErrorDialog dialog = new ErrorDialog())
            {
                dialog.labelTitle.Text = title;
                dialog.textBoxError.Text = errorMessage;
                dialog.Text = title;
                dialog.ShowDialog(parent);
            }
        }
    }
}
