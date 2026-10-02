using System;
using System.Drawing;
using System.Windows.Forms;

namespace ScanLink.Themed
{
    /// <summary>
    /// Hosts an existing Form inside a panel so it can serve as a page in the shell.
    ///
    /// This is what turns the old dialogs into tabs. Every one of them — scanner
    /// management, printer connection, crops and products — is a full Form with its own
    /// hand-positioned layout and its own handlers. Rewriting them as UserControls would
    /// mean re-testing all of that; demoting them to child controls keeps the field
    /// references, the event wiring and the resize logic exactly as they are, and changes
    /// only where the window lives.
    ///
    /// Three things have to be undone for a Form to behave as a child:
    ///   * TopLevel must go first — setting it after the handle exists throws.
    ///   * MinimumSize is enforced even on a child, so an 800x600 minimum would push the
    ///     form past a smaller host instead of letting the host scroll.
    ///   * Closing must be intercepted, or an in-form Close button disposes the page and
    ///     every subsequent navigation lands on a dead control.
    /// </summary>
    internal static class EmbeddedFormHost
    {
        public static void Embed(Form form, Panel host, Action onCloseRequested)
        {
            if (form == null || host == null) return;

            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.ControlBox = false;
            form.ShowInTaskbar = false;
            form.MaximumSize = Size.Empty;
            form.Dock = DockStyle.Fill;
            form.BackColor = Theme.SurfaceApp;

            // The dialogs were laid out against a fixed client size, so the HOST scrolls and
            // the form keeps whatever minimum it declared for itself. Putting AutoScroll on
            // the form instead would let it shrink below that minimum and then relayout
            // against a client area that its own scrollbars had just changed.
            form.AutoScroll = false;
            host.AutoScroll = true;

            form.FormClosing += (s, e) =>
            {
                if (e.CloseReason != CloseReason.UserClosing) return;
                e.Cancel = true;
                // PrinterConnectionDialog hides itself in its own OnFormClosing override,
                // which runs before this handler. As a page that would leave the tab blank
                // for the rest of the session, so undo it.
                form.Visible = true;
                if (onCloseRequested != null) onCloseRequested();
            };

            host.Controls.Add(form);
            form.Show();
        }
    }
}
