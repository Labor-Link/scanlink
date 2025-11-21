using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ScanLink
{
    internal enum HidConnectionType
    {
        HidKeyboard,
        HidRaw
    }

    internal sealed class HidDeviceInfo
    {
        public HidDeviceInfo(IntPtr deviceHandle, string devicePath, string pnpDeviceId, string vid, string pid, HidConnectionType connectionType)
        {
            DeviceHandle = deviceHandle;
            DevicePath = devicePath;
            PnpDeviceId = pnpDeviceId;
            Vid = vid;
            Pid = pid;
            ConnectionType = connectionType;
        }

        public IntPtr DeviceHandle { get; }
        public string DevicePath { get; }
        public string PnpDeviceId { get; }
        public string Vid { get; }
        public string Pid { get; }
        public string SerialNumber { get; set; }
        public string FriendlyName { get; set; }
        public string Manufacturer { get; set; }
        public HidConnectionType ConnectionType { get; set; }
    }

    internal sealed class HidScanEventArgs : EventArgs
    {
        public HidScanEventArgs(HidDeviceInfo device, string data, HidConnectionType connectionType)
        {
            Device = device;
            Data = data;
            ConnectionType = connectionType;
            Timestamp = DateTime.Now;
        }

        public HidDeviceInfo Device { get; }
        public string Data { get; }
        public HidConnectionType ConnectionType { get; }
        public DateTime Timestamp { get; }
    }

    internal sealed class RawInputManager : IDisposable
    {
        private sealed class KeyboardBuffer
        {
            public readonly StringBuilder Buffer = new StringBuilder();
            public readonly Stopwatch Stopwatch = Stopwatch.StartNew();
        }

        private const int WM_INPUT = 0x00FF;
        private const int WM_INPUT_DEVICE_CHANGE = 0x00FE;

        private const int RIM_TYPEKEYBOARD = 1;
        private const int RIM_TYPEHID = 2;

        private const int RIDEV_INPUTSINK = 0x00000100;
        private const int RIDEV_DEVNOTIFY = 0x00002000;

        private const uint RID_INPUT = 0x10000003;
        private const uint RIDI_DEVICENAME = 0x20000007;

        private const int RI_KEY_BREAK = 0x0001;
        private const int SCAN_TIMEOUT_MS = 80;

        private const ushort VKEY_RETURN = 0x0D;
        private const ushort VKEY_SHIFT = 0x10;

        private readonly Dictionary<IntPtr, HidDeviceInfo> _deviceCache = new Dictionary<IntPtr, HidDeviceInfo>();
        private readonly Dictionary<IntPtr, KeyboardBuffer> _keyboardBuffers = new Dictionary<IntPtr, KeyboardBuffer>();
        private readonly object _sync = new object();

        public event EventHandler<HidScanEventArgs> HidScanReceived;
        public event EventHandler<string> Diagnostics;

        public bool IsRegistered { get; private set; }
        public IntPtr WindowHandle { get; private set; }

        public void RegisterForDevices(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
            {
                throw new ArgumentException("Window handle must be valid", nameof(hwnd));
            }

            lock (_sync)
            {
                if (IsRegistered && WindowHandle == hwnd)
                {
                    return;
                }

                var devices = new RAWINPUTDEVICE[]
                {
                    new RAWINPUTDEVICE
                    {
                        UsagePage = 0x01,   // Generic Desktop Controls
                        Usage = 0x06,       // Keyboard
                        Flags = RIDEV_INPUTSINK | RIDEV_DEVNOTIFY,
                        Target = hwnd
                    },
                    new RAWINPUTDEVICE
                    {
                        UsagePage = 0x8C,   // Bar Code Scanner page
                        Usage = 0x02,       // Bar Code Scanner
                        Flags = RIDEV_INPUTSINK | RIDEV_DEVNOTIFY,
                        Target = hwnd
                    }
                };

                if (!RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE))))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException($"RegisterRawInputDevices failed with error {error}");
                }

                WindowHandle = hwnd;
                IsRegistered = true;
                EmitDiagnostics("Raw input devices registered for HID keyboard and raw HID scanners.");
            }
        }

        public void ProcessMessage(ref Message m)
        {
            if (!IsRegistered)
            {
                return;
            }

            if (m.Msg == WM_INPUT)
            {
                ProcessRawInput(m.LParam);
            }
            else if (m.Msg == WM_INPUT_DEVICE_CHANGE)
            {
                HandleDeviceChange(m.WParam, m.LParam);
            }
        }

        private void ProcessRawInput(IntPtr rawInputPtr)
        {
            uint dwSize = 0;
            if (GetRawInputData(rawInputPtr, RID_INPUT, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))) != 0 || dwSize == 0)
            {
                return;
            }

            IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);
            try
            {
                if (GetRawInputData(rawInputPtr, RID_INPUT, buffer, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))) != dwSize)
                {
                    return;
                }

                var header = Marshal.PtrToStructure<RAWINPUTHEADER>(buffer);
                if (header.dwType == RIM_TYPEKEYBOARD)
                {
                    ProcessKeyboardInput(buffer, header);
                }
                else if (header.dwType == RIM_TYPEHID)
                {
                    ProcessHidInput(buffer, header);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private void ProcessKeyboardInput(IntPtr buffer, RAWINPUTHEADER header)
        {
            IntPtr dataPtr = IntPtr.Add(buffer, Marshal.SizeOf(typeof(RAWINPUTHEADER)));
            RAWKEYBOARD rawKeyboard = Marshal.PtrToStructure<RAWKEYBOARD>(dataPtr);

            if ((rawKeyboard.Flags & RI_KEY_BREAK) == RI_KEY_BREAK)
            {
                return; // Ignore key releases
            }

            HidDeviceInfo device = GetOrCreateDevice(header.hDevice, HidConnectionType.HidKeyboard);
            KeyboardBuffer keyboardBuffer = GetOrCreateKeyboardBuffer(header.hDevice);

            if (rawKeyboard.VKey == VKEY_SHIFT)
            {
                return; // Ignore raw shift presses
            }

            keyboardBuffer.Stopwatch.Stop();
            long elapsed = keyboardBuffer.Stopwatch.ElapsedMilliseconds;
            keyboardBuffer.Stopwatch.Restart();

            if (elapsed > SCAN_TIMEOUT_MS)
            {
                keyboardBuffer.Buffer.Clear();
            }

            if (rawKeyboard.VKey == VKEY_RETURN)
            {
                if (keyboardBuffer.Buffer.Length > 0)
                {
                    string scannedData = keyboardBuffer.Buffer.ToString();
                    keyboardBuffer.Buffer.Clear();
                    EmitScan(device, scannedData, HidConnectionType.HidKeyboard);
                }
                return;
            }

            char c = ConvertKeyToChar(rawKeyboard.VKey);
            if (c != '\0')
            {
                keyboardBuffer.Buffer.Append(c);
            }
        }

        private void ProcessHidInput(IntPtr buffer, RAWINPUTHEADER header)
        {
            IntPtr dataPtr = IntPtr.Add(buffer, Marshal.SizeOf(typeof(RAWINPUTHEADER)));
            int sizeHid = Marshal.ReadInt32(dataPtr);
            int count = Marshal.ReadInt32(dataPtr, sizeof(int));
            int rawDataOffset = Marshal.SizeOf(typeof(RAWHID));

            if (sizeHid <= 0 || count <= 0)
            {
                return;
            }

            int totalBytes = sizeHid * count;
            byte[] rawBytes = new byte[totalBytes];
            Marshal.Copy(IntPtr.Add(dataPtr, rawDataOffset), rawBytes, 0, rawBytes.Length);

            // Attempt to interpret as ASCII text
            string payload = ExtractAsciiString(rawBytes);
            if (string.IsNullOrWhiteSpace(payload))
            {
                return;
            }

            HidDeviceInfo device = GetOrCreateDevice(header.hDevice, HidConnectionType.HidRaw);
            EmitScan(device, payload.Trim(), HidConnectionType.HidRaw);
        }

        private HidDeviceInfo GetOrCreateDevice(IntPtr deviceHandle, HidConnectionType connectionType)
        {
            lock (_sync)
            {
                if (_deviceCache.TryGetValue(deviceHandle, out HidDeviceInfo existing))
                {
                    existing.ConnectionType = connectionType;
                    return existing;
                }

                string devicePath = GetDevicePath(deviceHandle);
                string pnpId = ExtractPnpId(devicePath);
                string vid = ExtractIdSegment(devicePath, "VID_");
                string pid = ExtractIdSegment(devicePath, "PID_");

                var deviceInfo = new HidDeviceInfo(deviceHandle, devicePath, pnpId, vid, pid, connectionType)
                {
                    FriendlyName = pnpId,
                    Manufacturer = string.Empty,
                    SerialNumber = ExtractSerialFromPath(devicePath)
                };

                _deviceCache[deviceHandle] = deviceInfo;
                EmitDiagnostics($"Registered HID device: {deviceInfo.PnpDeviceId ?? deviceInfo.DevicePath}");
                return deviceInfo;
            }
        }

        private KeyboardBuffer GetOrCreateKeyboardBuffer(IntPtr deviceHandle)
        {
            lock (_sync)
            {
                if (_keyboardBuffers.TryGetValue(deviceHandle, out KeyboardBuffer existing))
                {
                    return existing;
                }

                var buffer = new KeyboardBuffer();
                _keyboardBuffers[deviceHandle] = buffer;
                return buffer;
            }
        }

        private string GetDevicePath(IntPtr deviceHandle)
        {
            uint size = 0;
            if (GetRawInputDeviceInfo(deviceHandle, RIDI_DEVICENAME, IntPtr.Zero, ref size) != 0 || size == 0)
            {
                return string.Empty;
            }

            IntPtr buffer = Marshal.AllocHGlobal((int)size * sizeof(char));
            try
            {
                if (GetRawInputDeviceInfo(deviceHandle, RIDI_DEVICENAME, buffer, ref size) == 0)
                {
                    return Marshal.PtrToStringUni(buffer);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
            return string.Empty;
        }

        private static string ExtractPnpId(string devicePath)
        {
            if (string.IsNullOrEmpty(devicePath))
            {
                return null;
            }

            int start = devicePath.IndexOf('#');
            if (start < 0)
            {
                return devicePath;
            }

            string withoutPrefix = devicePath.Substring(start + 1);
            int end = withoutPrefix.IndexOf('#');
            if (end >= 0)
            {
                withoutPrefix = withoutPrefix.Substring(0, end);
            }

            return withoutPrefix.Replace('#', '\\');
        }

        private static string ExtractSerialFromPath(string devicePath)
        {
            if (string.IsNullOrEmpty(devicePath))
            {
                return string.Empty;
            }

            int lastHash = devicePath.LastIndexOf('#');
            if (lastHash > 0)
            {
                string potentialSerial = devicePath.Substring(lastHash + 1);
                int braceIndex = potentialSerial.IndexOf('{');
                if (braceIndex > 0)
                {
                    potentialSerial = potentialSerial.Substring(0, braceIndex);
                }
                return potentialSerial;
            }
            return string.Empty;
        }

        private static string ExtractIdSegment(string devicePath, string token)
        {
            if (string.IsNullOrEmpty(devicePath))
            {
                return string.Empty;
            }

            var match = Regex.Match(devicePath, $"{token}([0-9A-Fa-f]{{4}})");
            return match.Success ? match.Groups[1].Value.ToUpperInvariant() : string.Empty;
        }

        private void HandleDeviceChange(IntPtr wParam, IntPtr lParam)
        {
            const int GIDC_ARRIVAL = 1;
            const int GIDC_REMOVAL = 2;

            if (wParam == (IntPtr)GIDC_ARRIVAL)
            {
                EmitDiagnostics("HID device arrival detected.");
                // Force device info creation on first data packet.
            }
            else if (wParam == (IntPtr)GIDC_REMOVAL)
            {
                lock (_sync)
                {
                    _deviceCache.Remove(lParam);
                    _keyboardBuffers.Remove(lParam);
                }
                EmitDiagnostics("HID device removal detected.");
            }
        }

        private static string ExtractAsciiString(byte[] rawBytes)
        {
            if (rawBytes == null || rawBytes.Length == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder(rawBytes.Length);
            foreach (byte b in rawBytes)
            {
                if (b == 0)
                {
                    continue;
                }
                if (b == 0x0D || b == 0x0A)
                {
                    builder.Append('\n');
                    continue;
                }
                if (b < 32 || b > 126)
                {
                    // Non printable; include hex bracket for debugging
                    builder.AppendFormat("[0x{0:X2}]", b);
                }
                else
                {
                    builder.Append((char)b);
                }
            }
            return builder.ToString();
        }

        private static char ConvertKeyToChar(ushort vKey)
        {
            Keys key = (Keys)vKey;
            if (key >= Keys.D0 && key <= Keys.D9)
            {
                return (char)('0' + (key - Keys.D0));
            }
            if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
            {
                return (char)('0' + (key - Keys.NumPad0));
            }
            if (key >= Keys.A && key <= Keys.Z)
            {
                return (char)('A' + (key - Keys.A));
            }
            if (key == Keys.OemMinus) return '-';
            if (key == Keys.OemOpenBrackets) return '[';
            if (key == Keys.OemCloseBrackets) return ']';
            if (key == Keys.OemSemicolon) return ';';
            if (key == Keys.OemQuotes) return '\'';
            if (key == Keys.Oemcomma) return ',';
            if (key == Keys.OemPeriod) return '.';
            if (key == Keys.OemQuestion) return '/';
            if (key == Keys.Oemtilde) return '`';
            if (key == Keys.Oem5) return '|';
            if (key == Keys.Space) return ' ';

            switch (key)
            {
                case Keys.D1: return '!';
                case Keys.D2: return '@';
                case Keys.D3: return '#';
                case Keys.D4: return '$';
                case Keys.D5: return '%';
                case Keys.D6: return '^';
                case Keys.D7: return '&';
                case Keys.D8: return '*';
                case Keys.D9: return '(';
                case Keys.D0: return ')';
            }

            return '\0';
        }

        private void EmitScan(HidDeviceInfo device, string data, HidConnectionType connectionType)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return;
            }
            HidScanReceived?.Invoke(this, new HidScanEventArgs(device, data, connectionType));
        }

        private void EmitDiagnostics(string message)
        {
            Diagnostics?.Invoke(this, message);
            Debug.WriteLine("[RAWINPUT] " + message);
        }

        public void Dispose()
        {
            lock (_sync)
            {
                _deviceCache.Clear();
                _keyboardBuffers.Clear();
                IsRegistered = false;
                WindowHandle = IntPtr.Zero;
            }
        }

        #region P/Invoke

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE
        {
            public ushort UsagePage;
            public ushort Usage;
            public int Flags;
            public IntPtr Target;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWKEYBOARD
        {
            public ushort MakeCode;
            public ushort Flags;
            public ushort Reserved;
            public ushort VKey;
            public uint Message;
            public uint ExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWHID
        {
            public int dwSizeHid;
            public int dwCount;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, IntPtr pData, ref uint pcbSize);

        #endregion
    }
}


