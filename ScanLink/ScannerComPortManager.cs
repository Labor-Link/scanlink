using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;

namespace ScanLink
{
    /// <summary>
    /// Manages multiple COM port scanner connections simultaneously.
    /// Each scanner is identified by its COM port and PNPDeviceID.
    /// Supports manufacturer-agnostic scanner communication.
    /// </summary>
    public class ScannerComPortManager : IDisposable
    {
        private readonly Dictionary<string, ScannerComPort> _activeScannners = new Dictionary<string, ScannerComPort>();
        private readonly object _lockObj = new object();
        private bool _disposed = false;

        public event EventHandler<ScannerDataReceivedEventArgs> ScannerDataReceived;
        public event EventHandler<ScannerEventArgs> ScannerConnected;
        public event EventHandler<ScannerEventArgs> ScannerDisconnected;
        public event EventHandler<ScannerErrorEventArgs> ScannerError;
        public event EventHandler<ScannerLogEventArgs> ScannerLog;

        /// <summary>
        /// Opens a COM port for the specified scanner configuration.
        /// </summary>
        public bool OpenScanner(ScannerConfig config)
        {
            if (config == null || string.IsNullOrEmpty(config.ComPort))
                return false;

            lock (_lockObj)
            {
                // Check if already open
                if (_activeScannners.ContainsKey(config.PNPDeviceID))
                {
                    return true; // Already open
                }

                try
                {
                    var scannerPort = new ScannerComPort(config);
                    scannerPort.DataReceived += ScannerPort_DataReceived;
                    scannerPort.ErrorOccurred += ScannerPort_ErrorOccurred;
                    scannerPort.LogOccurred += ScannerPort_LogOccurred;

                    if (scannerPort.Open())
                    {
                        _activeScannners[config.PNPDeviceID] = scannerPort;
                        ScannerConnected?.Invoke(this, new ScannerEventArgs(config));
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    IssueLoggingService.LogIssue("Scanner Port Error", $"Failed to open scanner {config.DeviceName} on {config.ComPort}: {ex.Message}\nStack Trace:\n{ex.StackTrace}");
                    ScannerError?.Invoke(this, new ScannerErrorEventArgs(config, $"Failed to open scanner: {ex.Message}"));
                }
            }

            return false;
        }

        /// <summary>
        /// Closes a specific scanner by PNPDeviceID.
        /// </summary>
        public void CloseScanner(string pnpDeviceId)
        {
            if (string.IsNullOrEmpty(pnpDeviceId))
                return;

            lock (_lockObj)
            {
                if (_activeScannners.TryGetValue(pnpDeviceId, out var scanner))
                {
                    scanner.Close();
                    scanner.DataReceived -= ScannerPort_DataReceived;
                    scanner.ErrorOccurred -= ScannerPort_ErrorOccurred;
                    scanner.LogOccurred -= ScannerPort_LogOccurred;
                    _activeScannners.Remove(pnpDeviceId);
                    ScannerDisconnected?.Invoke(this, new ScannerEventArgs(scanner.Config));
                }
            }
        }

        /// <summary>
        /// Closes all active scanner connections.
        /// </summary>
        public void CloseAllScanners()
        {
            lock (_lockObj)
            {
                var pnpIds = _activeScannners.Keys.ToList();
                foreach (var pnpId in pnpIds)
                {
                    CloseScanner(pnpId);
                }
            }
        }

        /// <summary>
        /// Gets all currently active scanner configurations.
        /// </summary>
        public List<ScannerConfig> GetActiveScanners()
        {
            lock (_lockObj)
            {
                return _activeScannners.Values.Select(s => s.Config).ToList();
            }
        }

        /// <summary>
        /// Checks if a scanner is currently open.
        /// </summary>
        public bool IsScannerOpen(string pnpDeviceId)
        {
            lock (_lockObj)
            {
                return _activeScannners.ContainsKey(pnpDeviceId);
            }
        }

        /// <summary>
        /// Re-initializes a scanner (sends COM mode configuration commands).
        /// Useful if scanner was disconnected or needs reconfiguration.
        /// </summary>
        public bool ReinitializeScanner(string pnpDeviceId)
        {
            lock (_lockObj)
            {
                if (_activeScannners.TryGetValue(pnpDeviceId, out var scanner))
                {
                    return scanner.Reinitialize();
                }
            }
            return false;
        }

        /// <summary>
        /// Sends a beep command to a specific scanner for testing/feedback.
        /// </summary>
        public bool BeepScanner(string pnpDeviceId)
        {
            lock (_lockObj)
            {
                if (_activeScannners.TryGetValue(pnpDeviceId, out var scanner))
                {
                    return scanner.Beep();
                }
            }
            return false;
        }

        /// <summary>
        /// Tests the connection to a specific scanner.
        /// </summary>
        public bool TestScannerConnection(string pnpDeviceId)
        {
            lock (_lockObj)
            {
                if (_activeScannners.TryGetValue(pnpDeviceId, out var scanner))
                {
                    return scanner.TestConnection();
                }
            }
            return false;
        }

        private void ScannerPort_DataReceived(object sender, ScannerDataReceivedEventArgs e)
        {
            ScannerDataReceived?.Invoke(this, e);
        }

        private void ScannerPort_ErrorOccurred(object sender, ScannerErrorEventArgs e)
        {
            ScannerError?.Invoke(this, e);
        }

        private void ScannerPort_LogOccurred(object sender, ScannerLogEventArgs e)
        {
            ScannerLog?.Invoke(this, e);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                CloseAllScanners();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Represents a single COM port scanner connection.
    /// </summary>
    internal class ScannerComPort
    {
        private SerialPort _serialPort;
        private StringBuilder _dataBuffer = new StringBuilder();
        private readonly object _bufferLock = new object();
        private System.Timers.Timer _pollTimer;
        private DateTime _lastReceiveUtc = DateTime.MinValue;

        public ScannerConfig Config { get; private set; }
        public event EventHandler<ScannerDataReceivedEventArgs> DataReceived;
        public event EventHandler<ScannerErrorEventArgs> ErrorOccurred;
        public event EventHandler<ScannerLogEventArgs> LogOccurred;

        public ScannerComPort(ScannerConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public bool Open()
        {
            try
            {
                _serialPort = new SerialPort
                {
                    PortName = Config.ComPort,
                    BaudRate = Config.BaudRate,
                    Parity = Config.Parity,
                    DataBits = Config.DataBits,
                    StopBits = Config.StopBits,
                    Handshake = Handshake.None,
                    ReadTimeout = 500,
                    WriteTimeout = 500
                };

                // Many RS-232/USB-COM scanners (including Datalogic) require DTR/RTS asserted
                _serialPort.DtrEnable = true;
                _serialPort.RtsEnable = true;
                _serialPort.NewLine = "\r\n";
                _serialPort.ReceivedBytesThreshold = 1;

                EmitLog($"Opening {Config.ComPort} @ {Config.BaudRate},{Config.DataBits},{Config.Parity},{Config.StopBits} DTR={_serialPort.DtrEnable} RTS={_serialPort.RtsEnable}");

                _serialPort.DataReceived += SerialPort_DataReceived;
                _serialPort.ErrorReceived += SerialPort_ErrorReceived;

                _serialPort.Open();

                EmitLog($"Port opened: {Config.DeviceName} ({Config.ComPort})");
                EmitLog($"Line states CTS={_serialPort.CtsHolding} DSR={_serialPort.DsrHolding} CD={_serialPort.CDHolding}");

                // Start a lightweight poller as a fallback when DataReceived is unreliable
                StartPolling();

                // Initialize scanner to COM mode
                System.Threading.Thread.Sleep(200); // Give scanner time to stabilize
                bool initSuccess = ScannerCommandProtocols.InitializeScanner(_serialPort, Config.Manufacturer);
                
                if (initSuccess)
                {
                    System.Diagnostics.Debug.WriteLine($"Scanner {Config.DeviceName} initialized successfully");
                    EmitLog("Initialization succeeded");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Scanner {Config.DeviceName} opened but initialization commands may have failed");
                    EmitLog("Initialization may have failed");
                }

                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                string msg = $"COM port {Config.ComPort} is already in use by another application. Please close any other scanner applications (like 'winscan') and try again.";
                IssueLoggingService.LogIssue("Scanner Port Locked", $"COM path {Config.DeviceName} on {Config.ComPort} is unauthorized/locked.\n{ex.Message}");
                EmitLog(msg);
                ErrorOccurred?.Invoke(this, new ScannerErrorEventArgs(Config, msg));
                return false;
            }
            catch (Exception ex)
            {
                EmitLog($"Failed to open COM port: {ex.Message}");
                IssueLoggingService.LogIssue("Scanner Port Exception", $"Failed to open {Config.DeviceName} on {Config.ComPort}: {ex.Message}\nStack Trace:\n{ex.StackTrace}");
                ErrorOccurred?.Invoke(this, new ScannerErrorEventArgs(Config, $"Failed to open COM port: {ex.Message}"));
                return false;
            }
        }

        public void Close()
        {
            try
            {
                EmitLog($"Closing port {Config.ComPort}");
                StopPolling();
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.DataReceived -= SerialPort_DataReceived;
                    _serialPort.ErrorReceived -= SerialPort_ErrorReceived;
                    _serialPort.Close();
                    _serialPort.Dispose();
                    EmitLog($"Port closed {Config.ComPort}");
                }
            }
            catch { }
        }

        public bool Reinitialize()
        {
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    EmitLog("Reinitializing scanner");
                    bool ok = ScannerCommandProtocols.InitializeScanner(_serialPort, Config.Manufacturer);
                    EmitLog(ok ? "Reinitialize OK" : "Reinitialize FAILED");
                    return ok;
                }
            }
            catch { }
            return false;
        }

        public bool Beep()
        {
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    EmitLog("Sending beep command");
                    bool ok = ScannerCommandProtocols.BeepScanner(_serialPort, Config.Manufacturer);
                    EmitLog(ok ? "Beep sent" : "Beep send failed");
                    return ok;
                }
            }
            catch { }
            return false;
        }

        public bool TestConnection()
        {
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    EmitLog("Testing scanner connection");
                    bool ok = ScannerCommandProtocols.TestScannerConnection(_serialPort);
                    EmitLog(ok ? "Test OK" : "Test FAILED");
                    return ok;
                }
            }
            catch { }
            return false;
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (_serialPort == null || !_serialPort.IsOpen)
                    return;

                string data = _serialPort.ReadExisting();
                if (string.IsNullOrEmpty(data))
                {
                    // No payload on event; can occur on certain drivers
                    return;
                }

                _lastReceiveUtc = DateTime.UtcNow;
                string preview = data.Length > 64 ? data.Substring(0, 64) + "..." : data;
                EmitLog($"Raw received {data.Length} chars: \"{preview}\"");

                AppendAndMaybeEmit(data);
            }
            catch (Exception ex)
            {
                EmitLog($"Error receiving data: {ex.Message}");
                ErrorOccurred?.Invoke(this, new ScannerErrorEventArgs(Config, $"Error receiving data: {ex.Message}"));
            }
        }

        private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            EmitLog($"Serial port error: {e.EventType}");
            IssueLoggingService.LogIssue("Scanner Hardware Error", $"Serial device error triggered for {Config.DeviceName} on {Config.ComPort}. EventType: {e.EventType}");
            ErrorOccurred?.Invoke(this, new ScannerErrorEventArgs(Config, $"Serial port error: {e.EventType}"));
        }

        private void StartPolling()
        {
            try
            {
                if (_pollTimer != null)
                {
                    _pollTimer.Stop();
                    _pollTimer.Dispose();
                }
                _pollTimer = new System.Timers.Timer(50);
                _pollTimer.AutoReset = true;
                _pollTimer.Elapsed += (s, e) =>
                {
                    try
                    {
                        if (_serialPort == null || !_serialPort.IsOpen) return;
                        int available = _serialPort.BytesToRead;
                        if (available > 0)
                        {
                            string data = _serialPort.ReadExisting();
                            if (!string.IsNullOrEmpty(data))
                            {
                                _lastReceiveUtc = DateTime.UtcNow;
                                string preview = data.Length > 64 ? data.Substring(0, 64) + "..." : data;
                                EmitLog($"[poll] bytes={available}, read={data.Length}, \"{preview}\"");
                                AppendAndMaybeEmit(data);
                            }
                        }

                        // Inactivity flush: if buffer has data but no CR/LF terminator and quiet > 200ms, emit as a line
                        if (_dataBuffer.Length > 0 && (DateTime.UtcNow - _lastReceiveUtc).TotalMilliseconds > 200)
                        {
                            lock (_bufferLock)
                            {
                                if (_dataBuffer.Length > 0)
                                {
                                    string flushed = _dataBuffer.ToString().Trim();
                                    _dataBuffer.Clear();
                                    if (!string.IsNullOrWhiteSpace(flushed))
                                    {
                                        EmitLog($"Inactivity flush {flushed.Length} chars");
                                        DataReceived?.Invoke(this, new ScannerDataReceivedEventArgs(Config, flushed));
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                };
                _pollTimer.Start();
                EmitLog("Polling fallback started (50ms interval)");
            }
            catch { }
        }

        private void StopPolling()
        {
            try
            {
                if (_pollTimer != null)
                {
                    _pollTimer.Stop();
                    _pollTimer.Dispose();
                    _pollTimer = null;
                    EmitLog("Polling fallback stopped");
                }
            }
            catch { }
        }

        private void AppendAndMaybeEmit(string data)
        {
            lock (_bufferLock)
            {
                _dataBuffer.Append(data);

                string bufferContent = _dataBuffer.ToString();
                
                if (bufferContent.Contains("\r") || bufferContent.Contains("\n"))
                {
                    // Split the buffer by terminators
                    var parts = bufferContent.Split(new[] { '\r', '\n' });
                    
                    _dataBuffer.Clear();
                    
                    // All elements EXCEPT the last one are guaranteed to be cleanly terminated by \r or \n.
                    // The LAST element is the remaining un-terminated portion (could be empty if buffer ended in \r or \n).
                    for (int i = 0; i < parts.Length - 1; i++)
                    {
                        var line = parts[i].Trim();
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            EmitLog($"Terminator line delivered: \"{line}\"");
                            DataReceived?.Invoke(this, new ScannerDataReceivedEventArgs(Config, line));
                        }
                    }
                    
                    // Put the last undetermined piece back in the buffer for the next reading
                    string remainder = parts[parts.Length - 1];
                    if (!string.IsNullOrEmpty(remainder))
                    {
                        _dataBuffer.Append(remainder);
                        EmitLog($"Buffered partial line; buffer length now {_dataBuffer.Length}");
                    }
                }
            }
        }

        private void EmitLog(string message)
        {
            try
            {
                LogOccurred?.Invoke(this, new ScannerLogEventArgs(Config, message));
            }
            catch { }
        }
    }

    /// <summary>
    /// Scanner configuration including COM port settings.
    /// </summary>
    public class ScannerConfig
    {
        public string PNPDeviceID { get; set; }
        public string DeviceName { get; set; }
        public string Manufacturer { get; set; }
        public string SerialNumber { get; set; }
        public string ComPort { get; set; }
        public int BaudRate { get; set; } = 9600;
        public Parity Parity { get; set; } = Parity.None;
        public int DataBits { get; set; } = 8;
        public StopBits StopBits { get; set; } = StopBits.One;
        public string LineID { get; set; }
        public string BlockID { get; set; }

        public ScannerConfig()
        {
        }

        public ScannerConfig(string pnpDeviceId, string comPort)
        {
            PNPDeviceID = pnpDeviceId;
            ComPort = comPort;
        }

        public override string ToString()
        {
            return $"{DeviceName} on {ComPort} (Line: {LineID}, Block: {BlockID})";
        }
    }

    /// <summary>
    /// Event arguments for scanner data received events.
    /// </summary>
    public class ScannerDataReceivedEventArgs : EventArgs
    {
        public ScannerConfig Scanner { get; private set; }
        public string Data { get; private set; }
        public DateTime Timestamp { get; private set; }

        public ScannerDataReceivedEventArgs(ScannerConfig scanner, string data)
        {
            Scanner = scanner;
            Data = data;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Event arguments for scanner connection events.
    /// </summary>
    public class ScannerEventArgs : EventArgs
    {
        public ScannerConfig Scanner { get; private set; }

        public ScannerEventArgs(ScannerConfig scanner)
        {
            Scanner = scanner;
        }
    }

    /// <summary>
    /// Event arguments for scanner error events.
    /// </summary>
    public class ScannerErrorEventArgs : EventArgs
    {
        public ScannerConfig Scanner { get; private set; }
        public string ErrorMessage { get; private set; }
        public DateTime Timestamp { get; private set; }

        public ScannerErrorEventArgs(ScannerConfig scanner, string errorMessage)
        {
            Scanner = scanner;
            ErrorMessage = errorMessage;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Event arguments for diagnostic log messages.
    /// </summary>
    public class ScannerLogEventArgs : EventArgs
    {
        public ScannerConfig Scanner { get; private set; }
        public string Message { get; private set; }
        public DateTime Timestamp { get; private set; }

        public ScannerLogEventArgs(ScannerConfig scanner, string message)
        {
            Scanner = scanner;
            Message = message;
            Timestamp = DateTime.Now;
        }
    }
}

