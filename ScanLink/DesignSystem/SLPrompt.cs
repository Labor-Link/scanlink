using System.Windows.Forms;

namespace ScanLink.DesignSystem
{
    /// <summary>
    /// One-question dialog on SLDialog. Replaces Microsoft.VisualBasic.Interaction.InputBox,
    /// whose native grey box ignores the design system.
    ///   string name = SLPrompt.Ask(this, "Add new grade", "Grade name", hint: "For example \"Class 3\"");
    ///   if (name == null) { /* cancelled */ }
    /// </summary>
    internal static class SLPrompt
    {
        /// <summary>Returns the entered text, or null if the user cancelled.</summary>
        public static string Ask(IWin32Window owner, string title, string label, string defaultValue = "",
                                 string description = null, string hint = null, string confirmText = "OK")
        {
            using (SLDialog dialog = new SLDialog { Title = title, Description = description, DialogWidth = 440 })
            {
                SLTextBox input = new SLTextBox { Text = defaultValue ?? "" };
                dialog.Body.Controls.Add(new SLField(label, input) { Hint = hint });

                SLButton cancel = new SLButton { Text = "Cancel", Variant = SLVariant.Secondary, DialogResult = DialogResult.Cancel };
                SLButton ok = new SLButton { Text = confirmText, DialogResult = DialogResult.OK };
                dialog.AddAction(cancel);
                dialog.AddAction(ok);
                dialog.AcceptButton = ok;
                dialog.CancelButton = cancel;
                dialog.Shown += (s, e) => { input.Focus(); input.SelectAll(); };

                DialogResult result = owner != null ? dialog.ShowDialog(owner) : dialog.ShowDialog();
                return result == DialogResult.OK ? input.Text : null;
            }
        }
    }
}
