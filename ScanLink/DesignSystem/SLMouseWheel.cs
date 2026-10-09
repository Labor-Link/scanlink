using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScanLink.DesignSystem
{
    /// <summary>
    /// Sends the mouse wheel to the control under the pointer instead of the focused control
    /// (WinForms' default), so a page scrolls when the pointer is over it — as in a browser and
    /// in the mockup. A control that does not scroll passes the wheel on to its parent, so the
    /// nearest scrollable container (page, card body, table) scrolls.
    /// Installed once in Program.Main: Application.AddMessageFilter(new SLMouseWheel()).
    /// </summary>
    internal sealed class SLMouseWheel : IMessageFilter
    {
        private const int WM_MOUSEWHEEL = 0x020A, WM_MOUSEHWHEEL = 0x020E;

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(Point pt);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg != WM_MOUSEWHEEL && m.Msg != WM_MOUSEHWHEEL) return false;

            // lParam carries the pointer position in screen coordinates.
            long lp = m.LParam.ToInt64();
            Point screen = new Point((short)(lp & 0xFFFF), (short)((lp >> 16) & 0xFFFF));
            IntPtr target = WindowFromPoint(screen);
            if (target == IntPtr.Zero || target == m.HWnd) return false;

            // Only redirect within this application's own windows.
            if (Control.FromChildHandle(target) == null) return false;

            SendMessage(target, m.Msg, m.WParam, m.LParam);
            return true;
        }
    }
}
