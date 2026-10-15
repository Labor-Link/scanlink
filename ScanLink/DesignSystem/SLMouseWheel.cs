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
            Control under = Control.FromChildHandle(target);
            if (under == null) return false;

            // A number box, drop-down or slider under the pointer takes the wheel only when it has
            // focus. Otherwise scrolling the page past it changed its value: the printer
            // settings' DPI, Width and X silently changed while scrolling, breaking printing.
            Control valueControl = ValueControlAt(under);
            if (valueControl != null && !valueControl.ContainsFocus)
            {
                if (valueControl.Parent == null || !valueControl.Parent.IsHandleCreated) return true;   // swallow
                target = valueControl.Parent.Handle;   // the page (or its scrolling parent) scrolls
                if (target == m.HWnd) return false;
            }

            SendMessage(target, m.Msg, m.WParam, m.LParam);
            return true;
        }

        /// <summary>The control, or the one it is part of (a NumericUpDown's edit box), whose value the
        /// wheel changes; null for anything else.</summary>
        private static Control ValueControlAt(Control control)
        {
            for (Control c = control; c != null; c = c.Parent)
            {
                if (c is UpDownBase || c is ComboBox || c is TrackBar || c is DateTimePicker) return c;
                if (c is Form || c is DataGridView) return null;   // a grid scrolls itself; stop at the window
            }
            return null;
        }
    }

    /// <summary>
    /// A panel that may scroll sideways (AutoScroll for content wider than it) but leaves the
    /// vertical wheel to the page. A plain AutoScroll Panel with only a horizontal scrollbar
    /// turns the wheel into sideways scrolling, so with the pointer over it the page under it
    /// could not be scrolled (the printer settings below 600 px wide).
    /// </summary>
    internal class SLHorizontalScrollPanel : Panel
    {
        private const int WM_MOUSEWHEEL = 0x020A;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_MOUSEWHEEL && !VerticalScroll.Visible)
            {
                DefWndProc(ref m);   // DefWindowProc passes the wheel on to the parent
                return;
            }
            base.WndProc(ref m);
        }
    }
}
