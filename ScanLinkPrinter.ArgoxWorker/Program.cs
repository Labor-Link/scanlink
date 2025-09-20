using System;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using BarcodePrinter_API;
using BarcodePrinter_API.Comm;
using BarcodePrinter_API.Emulation.PPLB;

namespace ScanLinkPrinter.ArgoxWorker
{
    internal static class Logger
    {
        private static string LogDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScanLinkPrinter", "logs");
        public static void Log(string message)
        {
            try
            {
                if (!Directory.Exists(LogDir)) Directory.CreateDirectory(LogDir);
                var path = Path.Combine(LogDir, DateTime.Now.ToString("yyyyMMdd") + ".log");
                File.AppendAllText(path, DateTime.Now.ToString("HH:mm:ss.fff ") + message + Environment.NewLine);
            }
            catch { }
        }
    }

    internal class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                string json;
                if (args.Length == 0)
                {
                    json = Console.In.ReadToEnd();
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Console.Error.WriteLine("No input");
                        return 2;
                    }
                }
                else
                {
                    json = args[0];
                    if (json.StartsWith("@"))
                    {
                        var path = json.Substring(1).Trim('"');
                        json = File.ReadAllText(path);
                    }
                }
                var serializer = new JavaScriptSerializer();
                var request = serializer.Deserialize<PrintRequest>(json);
                if (request == null)
                {
                    Console.Error.WriteLine("Invalid request");
                    return 3;
                }

                Logger.Log($"Op={request.Op} Conn={request.ConnectionType} Target={request.Target}");
                using (var printer = new ArgoxPrintSession(request))
                {
                    printer.Print();
                }
                Console.Out.WriteLine("OK");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
        }
    }

    internal enum ConnType { File, Serial, Usb, TcpIp }

    internal sealed class PrintRequest
    {
        public ConnType ConnectionType { get; set; }
        public string Target { get; set; } = string.Empty; // file path, COM port, USB path, or host[:port]
        public string Text { get; set; } = string.Empty;
        public string Op { get; set; } = "PrintText"; // PrintText | Calibrate
        public ArgoxSettings Argox { get; set; }
    }

    internal sealed class ArgoxSettings
    {
        public int? Darkness { get; set; }
        public int? PrintRate { get; set; }
        public int? LabelLength { get; set; }
        public int? PrintWidth { get; set; }
        public ArgoxBarcodeSettings Barcode { get; set; }
        public ArgoxImageSettings Image { get; set; }
    }

    internal sealed class ArgoxBarcodeSettings
    {
        public string Symbology { get; set; } = "Code128";
        public string Data { get; set; } = string.Empty;
        public int X { get; set; } = 50;
        public int Y { get; set; } = 110;
        public int Height { get; set; } = 100;
        public int ModuleSize { get; set; } = 3;
    }

    internal sealed class ArgoxImageSettings
    {
        public string Path { get; set; } = string.Empty;
        public int X { get; set; } = 30;
        public int Y { get; set; } = 50;
        public int? Width { get; set; }
        public int? Height { get; set; }
    }

    internal sealed class ArgoxPrintSession : IDisposable
    {
        private readonly PrintRequest _req;
        private BarcodePrinter _printer = new BarcodePrinter();
        private PPLB? _pplb;

        public ArgoxPrintSession(PrintRequest req)
        {
            _req = req;
        }

        public void Print()
        {
            Logger.Log($"Begin Print pipeline (Op={_req.Op})");
            IPrinterConnection conn = null;
            try
            {
                conn = CreateConnection(_req);
                _printer.AddConnection(conn);
                _printer.Connection.Open();
                Logger.Log("Connection opened");
            }
            catch (Exception ex)
            {
                Logger.Log($"Connection open failed: {ex.Message}");
                throw;
            }

            _pplb = new PPLB();
            _printer.AddEmulation(_pplb);
            Logger.Log("PPLB emulation added");

            if (string.Equals(_req.Op, "Calibrate", StringComparison.OrdinalIgnoreCase))
            {
                Logger.Log("Calibrate");
                try
                {
                    _pplb.SetUtil.SetMediaCalibration();
                    _pplb.IOUtil.PrintOut();
                    Logger.Log("Calibrate PrintOut");
                }
                catch (Exception ex)
                {
                    Logger.Log($"Calibrate failed: {ex.Message}");
                    throw;
                }
                return;
            }

            // Reset + base setup then apply user settings
            try
            {
                _pplb.SetUtil.SetReset();
                _pplb.SetUtil.SetOrientation(false);
                _pplb.SetUtil.SetHomePosition(0, 0);
                _pplb.SetUtil.SetHardwareOption(PPLBMediaType.Direct_Thermal_Media, PPLBPrintMode.Tear_Off, 0);
                _pplb.SetUtil.SetStorage(PPLBStorage.Dram);
                _pplb.SetUtil.SetClearImageBuffer();
                Logger.Log("Base setup applied (reset/orientation/home/hw/storage/clear)");
            }
            catch (Exception ex)
            {
                Logger.Log($"Base setup failed: {ex.Message}");
                throw;
            }

            if (_req.Argox != null)
            {
                Logger.Log($"Applying Argox settings: PrintWidth={_req.Argox.PrintWidth}, LabelLength={_req.Argox.LabelLength}, PrintRate={_req.Argox.PrintRate}, Darkness={_req.Argox.Darkness}");
                if (_req.Argox.PrintWidth.HasValue) _pplb.SetUtil.SetPrintWidth(_req.Argox.PrintWidth.Value);
                if (_req.Argox.LabelLength.HasValue) _pplb.SetUtil.SetLabelLength(PPLBMediaTrack.Gap_Mode, _req.Argox.LabelLength.Value, 25);
                if (_req.Argox.PrintRate.HasValue)
                {
                    var rate = _req.Argox.PrintRate.Value;
                    if (rate < 1) rate = 1; if (rate > 6) rate = 6; // safe clamp
                    _pplb.SetUtil.SetPrintRate(rate);
                    Logger.Log($"PrintRate set to {rate}");
                }
                if (_req.Argox.Darkness.HasValue)
                {
                    var d = _req.Argox.Darkness.Value;
                    if (d < 0) d = 0; if (d > 15) d = 15; // SDK range
                    try { _pplb.SetUtil.SetDarkness(d); Logger.Log($"Darkness set to {d}"); }
                    catch (Exception ex) { Logger.Log($"SetDarkness skipped: {ex.Message}"); }
                }
            }
            if (_req.Argox != null && _req.Argox.Barcode != null && !string.IsNullOrEmpty(_req.Argox.Barcode.Data))
            {
                var b = _req.Argox.Barcode;
                Logger.Log($"Printing barcode: Symbology={b.Symbology}, X={b.X}, Y={b.Y}, H={b.Height}, Module={b.ModuleSize}, DataLen={b.Data.Length}");
                var data = Encoding.Default.GetBytes(b.Data);
                if (string.Equals(b.Symbology, "QR", StringComparison.OrdinalIgnoreCase))
                {
                    _pplb.BarcodeUtil.PrintQRCode(b.X, b.Y, PPLBQRCodeModel.Model_2, b.ModuleSize, PPLBQRCodeErrCorrect.Standard, data);
                }
                else if (string.Equals(b.Symbology, "DataMatrix", StringComparison.OrdinalIgnoreCase))
                {
                    _pplb.BarcodeUtil.PrintDataMatrix(b.X, b.Y, 0, 0, b.ModuleSize, false, data);
                }
                else if (string.Equals(b.Symbology, "PDF417", StringComparison.OrdinalIgnoreCase))
                {
                    _pplb.BarcodeUtil.PrintPDF417(b.X, b.Y, PPLBOrient.Clockwise_0_Degrees, 400, 300, 1,
                        PPLBPDF417CompressionMode.Auto_Encoding, 4, 10, 0, 0, false, data);
                }
                else
                {
                    _pplb.BarcodeUtil.PrintOneDBarcode(b.X, b.Y, PPLBOrient.Clockwise_0_Degrees, PPLBBarCodeType.Code_128_Auto_Mode, 3, 0, b.Height, true, data);
                }
            }
            else
            {
                var encoder = Encoding.Default;
                var buf = encoder.GetBytes(_req.Text ?? string.Empty);
                Logger.Log($"Printing text at (30,30): Bytes={buf.Length}");
                _pplb.TextUtil.PrintText(30, 30, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
            }
            // Optional image
            if (_req.Argox != null && _req.Argox.Image != null && !string.IsNullOrWhiteSpace(_req.Argox.Image.Path) && File.Exists(_req.Argox.Image.Path))
            {
                Logger.Log($"Printing image: Path={_req.Argox.Image.Path}, X={_req.Argox.Image.X}, Y={_req.Argox.Image.Y}, W={_req.Argox.Image.Width}, H={_req.Argox.Image.Height}");
                if (_req.Argox.Image.Width.HasValue && _req.Argox.Image.Height.HasValue)
                {
                    _pplb.GraphicsUtil.StoreGraphic(_req.Argox.Image.Path, "_tmpimg", _req.Argox.Image.Width.Value, _req.Argox.Image.Height.Value);
                    _pplb.GraphicsUtil.PrintStoreGraphic(_req.Argox.Image.X, _req.Argox.Image.Y, "_tmpimg");
                }
                else
                {
                    _pplb.GraphicsUtil.PrintGraphic(_req.Argox.Image.X, _req.Argox.Image.Y, _req.Argox.Image.Path);
                }
            }
            // Ensure one label is requested
            try
            {
                _pplb.SetUtil.SetPrintOut(1, 1);
                Logger.Log("SetPrintOut 1,1 and PrintOut");
                _pplb.IOUtil.PrintOut();
                Logger.Log("PrintOut issued");
                try { System.Threading.Thread.Sleep(150); Logger.Log("Post-PrintOut wait 150ms"); } catch { }
            }
            catch (Exception ex)
            {
                Logger.Log($"PrintOut failed: {ex.Message}");
                throw;
            }
        }

        private static IPrinterConnection CreateConnection(PrintRequest req)
        {
            switch (req.ConnectionType)
            {
                case ConnType.File:
                    var path = string.IsNullOrWhiteSpace(req.Target) ? Path.Combine(Path.GetTempPath(), "argox.out.txt") : req.Target;
                    return new FileStreamConnection(path);
                case ConnType.Serial:
                    var port = string.IsNullOrWhiteSpace(req.Target) ? "COM1" : req.Target;
                    return new SerialConnection(port, 9600, SerialParity.None, 8, SerialStopBits.One, SerialHandshake.None);
                case ConnType.Usb:
                    var usbPath = ResolveUsbDevicePath(req.Target);
                    return new USBConnection(usbPath);
                case ConnType.TcpIp:
                    string host = req.Target;
                    int port9100 = 9100;
                    if (!string.IsNullOrWhiteSpace(host) && host.Contains(":"))
                    {
                        var parts = host.Split(':');
                        host = parts[0];
                        if (parts.Length > 1 && int.TryParse(parts[1], out var p)) port9100 = p;
                    }
                    return new TCPConnection(host, port9100);
                default:
                    throw new InvalidOperationException("Unsupported connection type");
            }
        }

        private static string ResolveUsbDevicePath(string target)
        {
            if (string.IsNullOrWhiteSpace(target)) throw new ArgumentException("USB target is empty");
            // If input is in format "Friendly | PNPID", take the part after '|'
            try
            {
                var t = target.Trim().Trim('"');
                int bar = t.LastIndexOf('|');
                if (bar >= 0 && bar + 1 < t.Length)
                {
                    target = t.Substring(bar + 1).Trim();
                }
            }
            catch { }
            // If already an interface path, normalize and return
            if (target.StartsWith("\\\\?\\")) { Logger.Log($"USB path (iface): {target}"); return target; }
            if (target.StartsWith("\\??\\")) { var t2 = target.Replace("\\??\\", "\\\\?\\"); Logger.Log($"USB path (normalized): {t2}"); return t2; }

            // If PNPDeviceID like USB\\VID_XXXX&PID_YYYY\\SERIAL, read SymbolicName from registry
            if (target.StartsWith("USB\\", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var regPath = $"SYSTEM\\CurrentControlSet\\Enum\\{target}\\Device Parameters";
                    using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(regPath))
                    {
                        if (key != null)
                        {
                            var sym = key.GetValue("SymbolicName") as string;
                            if (!string.IsNullOrWhiteSpace(sym))
                            {
                                if (sym.StartsWith("\\??\\")) sym = sym.Replace("\\??\\", "\\\\?\\");
                                Logger.Log($"USB SymbolicName: {sym}");
                                return sym;
                            }
                        }
                    }
                }
                catch { }
                // Fallback: construct common interface paths
                var body = target.Replace('\\', '#');
                var guess = "\\\\?\\" + body + "#{a5dcbf10-6530-11d2-901f-00c04fb951ed}";
                Logger.Log($"USB fallback iface: {guess}") ;
                return guess;
            }
            return target;
        }

        public void Dispose()
        {
            try { _printer?.Connection?.Close(); Logger.Log("Connection closed"); } catch (Exception ex) { Logger.Log($"Connection close failed: {ex.Message}"); }
            _pplb = null;
        }
    }
}
