using System;
using System.Windows.Forms;
using ScanLink.DesignSystem;

namespace ScanLink
{
    /// <summary>
    /// Error details with a copy button. Built on SLDialog: danger-tone title, a read-only
    /// mono log in the body, Copy (secondary) and OK (primary) in the footer.
    /// </summary>
    internal partial class ErrorDialog : SLDialog
    {
        private SLTextBox textBoxError;
        private SLButton buttonOK;
        private SLButton buttonCopy;

        public ErrorDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Title = "Error";
            Tone = SLDialogTone.Danger;
            Description = "Copy the details if you need to send them to support.";
            DialogWidth = 600;
            BodyHeight = 300;
            Icon = System.Drawing.SystemIcons.Error;
            Name = "ErrorDialog";

            textBoxError = new SLTextBox
            {
                Name = "textBoxError",
                Multiline = true,
                ReadOnly = true,
                Mono = true,
                WordWrap = false,
                ScrollBars = ScrollBars.Both
            };
            Body.Controls.Add(textBoxError);
            Body.SetGrow(textBoxError);

            buttonCopy = new SLButton { Name = "buttonCopy", Text = "Copy", Variant = SLVariant.Secondary, IconName = "copy" };
            buttonCopy.Click += ButtonCopy_Click;
            buttonOK = new SLButton { Name = "buttonOK", Text = "OK", DialogResult = DialogResult.OK };
            AddAction(buttonCopy);
            AddAction(buttonOK);
            AcceptButton = buttonOK;
            CancelButton = buttonOK;
            // Focus OK, not the log: a focused TextBox selects all its text on open.
            ActiveControl = buttonOK;
        }

        private void ButtonCopy_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(textBoxError.Text)) return;
                Clipboard.SetText(textBoxError.Text);

                buttonCopy.Text = "Copied";
                buttonCopy.IconName = "check";
                Timer timer = new Timer { Interval = 1500 };
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    timer.Dispose();
                    if (buttonCopy.IsDisposed) return;
                    buttonCopy.Text = "Copy";
                    buttonCopy.IconName = "copy";
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy to clipboard: {ex.Message}", "Copy Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>Builds the dialog without showing it (used by the design gallery).</summary>
        internal static ErrorDialog Create(string title, string errorMessage)
        {
            ErrorDialog dialog = new ErrorDialog();
            dialog.Title = title;
            dialog.textBoxError.Text = errorMessage;
            return dialog;
        }

        public static void ShowError(string title, string errorMessage)
        {
            using (ErrorDialog dialog = Create(title, errorMessage))
            {
                dialog.ShowDialog();
            }
        }

        public static void ShowError(string title, string errorMessage, Form parent)
        {
            using (ErrorDialog dialog = Create(title, errorMessage))
            {
                dialog.ShowDialog(parent);
            }
        }
    }
}
