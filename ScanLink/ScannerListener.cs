using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace ScanLink
{
    public class ScannerListener : IDisposable
    {
        // Event to notify subscribers when a barcode is scanned
        public event EventHandler<string> BarcodeScanned;

        // Windows constants for hooks
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        // Delegate for the hook callback procedure
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        // Handle for the hook
        private IntPtr _hookId = IntPtr.Zero;
        private readonly LowLevelKeyboardProc _proc;

        // Buffer to store scanned characters and a timer to detect scanner speed
        private readonly StringBuilder _barcode = new StringBuilder();
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private const int SCANNER_INPUT_SPEED_MS = 50; // Keystrokes faster than this are from the scanner

        public ScannerListener()
        {
            _proc = HookCallback;
            _hookId = SetHook(_proc);
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                // Check the time since the last keypress
                if (_stopwatch.IsRunning && _stopwatch.ElapsedMilliseconds > SCANNER_INPUT_SPEED_MS)
                {
                    // Timeout elapsed, it's probably human typing, so reset
                    _barcode.Clear();
                }
                _stopwatch.Restart();

                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;

                // If the key is Enter and we have a barcode, the scan is complete
                if (key == Keys.Enter)
                {
                    if (_barcode.Length > 0)
                    {
                        // Raise the event with the scanned data
                        OnBarcodeScanned(_barcode.ToString());
                        _barcode.Clear();
                    }
                }
                else
                {
                    // Append the character to our buffer
                    // This part might need adjustment based on keyboard layout/shift keys
                    // For simple alphanumeric barcodes, this is usually sufficient.
                    char c = KeyToChar(key);
                    if (c != '\0')
                    {
                        _barcode.Append(c);
                    }
                }
            }
            // Pass the event to the next hook in the chain
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        protected virtual void OnBarcodeScanned(string barcode)
        {
            BarcodeScanned?.Invoke(this, barcode);
        }
        
        // Helper to convert Keys enum to a char (simplified)
        private char KeyToChar(Keys key)
        {
            // This is a simplified conversion. A more robust solution
            // would handle shift states and different keyboard layouts.
            if (key >= Keys.D0 && key <= Keys.D9)
                return (char)('0' + (key - Keys.D0));
            if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
                return (char)('0' + (key - Keys.NumPad0));
            if (key >= Keys.A && key <= Keys.Z)
                return (char)('A' + (key - Keys.A));
            
            // Add other characters as needed for common barcode symbols
            if (key == Keys.OemMinus) return '-';
            if (key == Keys.OemOpenBrackets) return '[';
            if (key == Keys.OemCloseBrackets) return ']';
            if (key == Keys.OemSemicolon) return ';';
            if (key == Keys.OemQuotes) return '\'';
            if (key == Keys.Oemcomma) return ',';
            if (key == Keys.OemPeriod) return '.';
            if (key == Keys.OemQuestion) return '/';
            if (key == Keys.Oemtilde) return '`'; 
            if (key == Keys.Oem5) return '|'; // Handle pipe character
            // Commonly used shifted characters that scanners might send directly
            if (key == Keys.D1) return '!'; // Assuming D1 might send ! directly
            if (key == Keys.D2) return '@';
            if (key == Keys.D3) return '#';
            if (key == Keys.D4) return '$';
            if (key == Keys.D5) return '%';
            if (key == Keys.D6) return '^';
            if (key == Keys.D7) return '&';
            if (key == Keys.D8) return '*';
            if (key == Keys.D9) return '(';
            if (key == Keys.D0) return ')';
            // Handle + and = (often on the same key as OemPlus/OemEquals, removed due to error)
            // For simplicity, scanners often send the character directly as if shift was pressed.
            // If we need to explicitly handle '=' or '+' if not caught by D-keys, we'd use more advanced P/Invoke
            
            return '\0'; // Return null char for unhandled keys
        }

        public void Dispose()
        {
            UnhookWindowsHookEx(_hookId);
        }

        #region P/Invoke Declarations
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        #endregion
    }
}