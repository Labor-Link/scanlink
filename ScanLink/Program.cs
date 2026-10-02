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

            Application.Run(new Form1());
            return 0;
        }
    }
}
