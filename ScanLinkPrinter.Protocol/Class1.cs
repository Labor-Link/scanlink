using System.Threading;
using System.Threading.Tasks;

namespace ScanLinkPrinter.Protocol
{
    public enum PrinterConnectionType
    {
        File,
        Serial,
        Usb,
        TcpIp
    }

    public sealed class PrinterConnectionSettings
    {
        public PrinterConnectionType ConnectionType { get; set; }

        // File
        public string? OutputFilePath { get; set; }

        // Serial
        public string? SerialPortName { get; set; }
        public int BaudRate { get; set; } = 9600;
        public int DataBits { get; set; } = 8;
        public string Parity { get; set; } = "None";
        public string StopBits { get; set; } = "One";
        public string Handshake { get; set; } = "None";

        // USB
        public string? UsbDevicePath { get; set; }

        // TCP/IP
        public string? TcpAddress { get; set; }
        public int TcpPort { get; set; } = 9100;
    }

    public sealed class PrintTextCommand
    {
        public string Text { get; set; } = string.Empty;
        public int X { get; set; } = 30;
        public int Y { get; set; } = 30;
        public ArgoxSettings? Argox { get; set; }
    }

    public interface IPrinterProvider
    {
        string Id { get; }
        string DisplayName { get; }

        Task PrintAsync(PrinterConnectionSettings connection, PrintTextCommand command, CancellationToken cancellationToken = default);
    }

    public sealed class ArgoxSettings
    {
        public int? Darkness { get; set; }
        public int? PrintRate { get; set; }
        public int? LabelLength { get; set; }
        public int? PrintWidth { get; set; }
        public ArgoxBarcodeSettings? Barcode { get; set; }
        public ArgoxImageSettings? Image { get; set; }
    }

    public sealed class ArgoxBarcodeSettings
    {
        public string Symbology { get; set; } = "Code128"; // Code128 | QR
        public string Data { get; set; } = string.Empty;
        public int X { get; set; } = 50;
        public int Y { get; set; } = 110;
        public int Height { get; set; } = 100; // for 1D
        public int ModuleSize { get; set; } = 3; // for QR
    }

    public sealed class ArgoxImageSettings
    {
        public string Path { get; set; } = string.Empty;
        public int X { get; set; } = 30;
        public int Y { get; set; } = 50;
        public int? Width { get; set; }
        public int? Height { get; set; }
    }
}
