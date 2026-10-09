using System;
using System.Windows.Forms;

namespace ScanLink
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Design-system gallery: renders every reference scene to PNG and exits.
            // See design/README.md ("Checking fidelity").
            int gallery = Array.IndexOf(args, "--gallery");
            if (gallery >= 0)
            {
                string outDir = gallery + 1 < args.Length ? args[gallery + 1] : "gallery";
                return DesignSystem.SLGallery.Run(outDir);
            }

            // Anything not handled where it happens is shown in ScanLink's error dialog (with a
            // Copy button for support) instead of the raw .NET crash dialog, and the app keeps running.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            // Mouse wheel scrolls whatever is under the pointer (WinForms sends it to the focused control).
            Application.AddMessageFilter(new DesignSystem.SLMouseWheel());
            Application.ThreadException += (s, e) =>
            {
                try { ErrorDialog.ShowError("Something went wrong", e.Exception.ToString()); }
                catch (Exception) { /* never let the handler itself crash the app */ }
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                System.Diagnostics.Debug.WriteLine("[FATAL] " + e.ExceptionObject);

            Application.Run(new Form1());
            return 0;
        }
    }
}
