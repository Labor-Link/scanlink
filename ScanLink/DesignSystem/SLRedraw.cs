using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScanLink.DesignSystem
{
    /// <summary>
    /// Turns a burst of layout and visibility changes into one repaint:
    ///
    ///   using (new SLRedrawBatch(panel)) { a.Visible = true; b.Height = 0; ... }
    ///
    /// Without it every change paints on its own, so switching a tab or opening "Show details"
    /// showed each in-between state (the staged redraw the tester reported). Suspends layout and
    /// stops painting (WM_SETREDRAW) for the block, then lays out once and repaints the whole
    /// subtree once. Nested batches on the same control only repaint when the outermost ends.
    ///
    /// Inside a batch the control counts as invisible to Windows: Focus() fails, so focus after
    /// the block. Never wrap a MessageBox, an await or slow I/O, the screen stays frozen until
    /// Dispose. Top-level windows are only layout-suspended, never frozen.
    /// </summary>
    internal sealed class SLRedrawBatch : IDisposable
    {
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool RedrawWindow(IntPtr hWnd, IntPtr rect, IntPtr region, uint flags);

        private const int WM_SETREDRAW = 0x000B;
        private const uint RDW_INVALIDATE = 0x1, RDW_ERASE = 0x4, RDW_ALLCHILDREN = 0x80, RDW_FRAME = 0x400;

        private sealed class Counter { public int Depth; }
        private static readonly ConditionalWeakTable<Control, Counter> Frozen = new ConditionalWeakTable<Control, Counter>();

        private readonly Control _control;
        private readonly bool _frozen;
        private bool _disposed;

        public SLRedrawBatch(Control control)
        {
            _control = control;
            if (control == null || control.IsDisposed) return;
            control.SuspendLayout();
            Form form = control as Form;
            _frozen = control.IsHandleCreated && control.Visible && (form == null || !form.TopLevel);
            if (_frozen && Frozen.GetOrCreateValue(control).Depth++ == 0)
                SendMessage(control.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
        }

        public void Dispose()
        {
            if (_disposed || _control == null) return;
            _disposed = true;
            if (_control.IsDisposed) return;
            _control.ResumeLayout(true);
            if (_frozen && --Frozen.GetOrCreateValue(_control).Depth == 0 && _control.IsHandleCreated)
            {
                SendMessage(_control.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
                RedrawWindow(_control.Handle, IntPtr.Zero, IntPtr.Zero, RDW_INVALIDATE | RDW_ERASE | RDW_ALLCHILDREN | RDW_FRAME);
            }
        }
    }
}
