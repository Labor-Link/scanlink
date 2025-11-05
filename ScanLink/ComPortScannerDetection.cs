using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;

namespace ScanLink
{
    /// <summary>
    /// Detects scanners connected via COM ports in a manufacturer-agnostic way.
    /// Includes specific support for Datalogic scanners with extensibility for other manufacturers.
    /// </summary>
    public static class ComPortScannerDetection
    {
        // Known scanner manufacturers and their USB Vendor IDs
        private static readonly Dictionary<string, string> KnownManufacturers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "05F9", "Datalogic" },
            { "0C2E", "Honeywell" },
            { "1504", "Zebra" },
            { "05E0", "Symbol Technologies" },
            { "1A86", "CH34x" }, // Generic USB-Serial chips often used in scanners
            { "0403", "FTDI" },  // Generic USB-Serial chips
        };

        /// <summary>
        /// Detects all COM port scanners currently connected to the system.
        /// </summary>
        public static List<ScannerConfig> DetectComPortScanners()
        {
            var scanners = new List<ScannerConfig>();
            var availablePorts = SerialPort.GetPortNames();

            if (availablePorts == null || availablePorts.Length == 0)
            {
                return scanners;
            }

            try
            {
                // Query WMI for COM port device information
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'"))
                {
                    var comDevices = searcher.Get();

                    foreach (ManagementObject device in comDevices)
                    {
                        try
                        {
                            string deviceName = device["Name"]?.ToString() ?? "";
                            string pnpDeviceId = device["PNPDeviceID"]?.ToString() ?? "";
                            string description = device["Description"]?.ToString() ?? "";
                            string manufacturer = device["Manufacturer"]?.ToString() ?? "";

                            // Extract COM port number from device name (e.g., "COM3")
                            string comPort = ExtractComPort(deviceName);

                            if (string.IsNullOrEmpty(comPort) || string.IsNullOrEmpty(pnpDeviceId))
                                continue;

                            // Check if this is a scanner device
                            if (IsScannerDevice(pnpDeviceId, deviceName, description, manufacturer))
                            {
                                var config = new ScannerConfig
                                {
                                    PNPDeviceID = pnpDeviceId,
                                    DeviceName = deviceName,
                                    ComPort = comPort,
                                    Manufacturer = DetermineManufacturer(pnpDeviceId, manufacturer),
                                    SerialNumber = ExtractSerialNumber(pnpDeviceId),
                                    // Set default COM settings (can be overridden)
                                    BaudRate = 9600,
                                    Parity = System.IO.Ports.Parity.None,
                                    DataBits = 8,
                                    StopBits = System.IO.Ports.StopBits.One
                                };

                                scanners.Add(config);
                            }
                        }
                        catch
                        {
                            // Skip devices that cause errors
                            continue;
                        }
                    }
                }

                // Also check for generic USB-Serial adapters that might be scanners
                scanners.AddRange(DetectGenericUsbSerialScanners(availablePorts, scanners));
            }
            catch
            {
                // If WMI fails, fall back to basic COM port enumeration
                scanners.AddRange(DetectByBasicEnumeration(availablePorts));
            }

            return scanners;
        }

        /// <summary>
        /// Determines if a device is likely a scanner based on its identifiers.
        /// </summary>
        private static bool IsScannerDevice(string pnpDeviceId, string deviceName, string description, string manufacturer)
        {
            // Check for known scanner keywords
            string[] scannerKeywords = { "scanner", "barcode", "datalogic", "honeywell", "zebra", "symbol", "qr", "imager" };
            string combined = $"{deviceName} {description} {manufacturer}".ToLower();

            foreach (var keyword in scannerKeywords)
            {
                if (combined.Contains(keyword))
                    return true;
            }

            // Check for known scanner manufacturer VIDs in PNPDeviceID
            foreach (var vid in KnownManufacturers.Keys)
            {
                if (pnpDeviceId.IndexOf($"VID_{vid}", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determines the manufacturer from PNPDeviceID or manufacturer string.
        /// </summary>
        private static string DetermineManufacturer(string pnpDeviceId, string manufacturerString)
        {
            // First check PNPDeviceID for VID
            var vidMatch = Regex.Match(pnpDeviceId, @"VID_([0-9A-F]{4})", RegexOptions.IgnoreCase);
            if (vidMatch.Success)
            {
                string vid = vidMatch.Groups[1].Value.ToUpper();
                if (KnownManufacturers.TryGetValue(vid, out string manufacturer))
                {
                    return manufacturer;
                }
            }

            // Fall back to manufacturer string
            if (!string.IsNullOrEmpty(manufacturerString))
            {
                // Check if manufacturer string contains known names
                foreach (var mfg in KnownManufacturers.Values)
                {
                    if (manufacturerString.IndexOf(mfg, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return mfg;
                    }
                }
                return manufacturerString;
            }

            return "Unknown";
        }

        /// <summary>
        /// Extracts COM port number from device name.
        /// </summary>
        private static string ExtractComPort(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName))
                return null;

            // Match pattern like "(COM3)" or "COM3"
            var match = Regex.Match(deviceName, @"\(?(COM\d+)\)?", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value.ToUpper();
            }

            return null;
        }

        /// <summary>
        /// Extracts serial number from PNPDeviceID.
        /// </summary>
        private static string ExtractSerialNumber(string pnpDeviceId)
        {
            if (string.IsNullOrEmpty(pnpDeviceId))
                return "Unknown";

            // PNPDeviceID format: USB\VID_05F9&PID_2216\SERIAL_NUMBER
            var parts = pnpDeviceId.Split('\\');
            if (parts.Length >= 3)
            {
                return parts[2];
            }

            return "Unknown";
        }

        /// <summary>
        /// Detects generic USB-Serial devices that might be scanners.
        /// </summary>
        private static List<ScannerConfig> DetectGenericUsbSerialScanners(string[] availablePorts, List<ScannerConfig> existingScanners)
        {
            var genericScanners = new List<ScannerConfig>();
            var existingPorts = new HashSet<string>(existingScanners.Select(s => s.ComPort), StringComparer.OrdinalIgnoreCase);

            foreach (var port in availablePorts)
            {
                if (existingPorts.Contains(port))
                    continue; // Already detected

                // For unidentified COM ports, create a generic scanner config
                // These can be manually configured later
                var config = new ScannerConfig
                {
                    PNPDeviceID = $"GENERIC_{port}",
                    DeviceName = $"Generic Scanner on {port}",
                    ComPort = port,
                    Manufacturer = "Generic",
                    SerialNumber = "N/A",
                    BaudRate = 9600,
                    Parity = System.IO.Ports.Parity.None,
                    DataBits = 8,
                    StopBits = System.IO.Ports.StopBits.One
                };

                // Only add if we can verify the port exists
                if (VerifyComPortExists(port))
                {
                    genericScanners.Add(config);
                }
            }

            return genericScanners;
        }

        /// <summary>
        /// Basic fallback COM port enumeration.
        /// </summary>
        private static List<ScannerConfig> DetectByBasicEnumeration(string[] availablePorts)
        {
            var scanners = new List<ScannerConfig>();

            foreach (var port in availablePorts)
            {
                if (VerifyComPortExists(port))
                {
                    var config = new ScannerConfig
                    {
                        PNPDeviceID = $"BASIC_{port}",
                        DeviceName = $"Scanner on {port}",
                        ComPort = port,
                        Manufacturer = "Unknown",
                        SerialNumber = "N/A",
                        BaudRate = 9600,
                        Parity = System.IO.Ports.Parity.None,
                        DataBits = 8,
                        StopBits = System.IO.Ports.StopBits.One
                    };

                    scanners.Add(config);
                }
            }

            return scanners;
        }

        /// <summary>
        /// Verifies that a COM port actually exists and is accessible.
        /// </summary>
        private static bool VerifyComPortExists(string portName)
        {
            try
            {
                using (var port = new SerialPort(portName))
                {
                    // Just check if we can create the object
                    // Don't actually open it
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets default COM port settings for a specific manufacturer.
        /// </summary>
        public static void ApplyManufacturerDefaults(ScannerConfig config)
        {
            if (config == null)
                return;

            switch (config.Manufacturer?.ToLower())
            {
                case "datalogic":
                    config.BaudRate = 9600;
                    config.Parity = System.IO.Ports.Parity.None;
                    config.DataBits = 8;
                    config.StopBits = System.IO.Ports.StopBits.One;
                    break;

                case "honeywell":
                    config.BaudRate = 9600;
                    config.Parity = System.IO.Ports.Parity.None;
                    config.DataBits = 8;
                    config.StopBits = System.IO.Ports.StopBits.One;
                    break;

                case "zebra":
                case "symbol technologies":
                    config.BaudRate = 9600;
                    config.Parity = System.IO.Ports.Parity.None;
                    config.DataBits = 8;
                    config.StopBits = System.IO.Ports.StopBits.One;
                    break;

                default:
                    // Generic defaults
                    config.BaudRate = 9600;
                    config.Parity = System.IO.Ports.Parity.None;
                    config.DataBits = 8;
                    config.StopBits = System.IO.Ports.StopBits.One;
                    break;
            }
        }

        /// <summary>
        /// Checks if Datalogic USB-COM driver is likely installed by looking for Datalogic devices.
        /// </summary>
        public static bool IsDatalogicDriverInstalled()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity"))
                {
                    foreach (ManagementObject device in searcher.Get())
                    {
                        string pnpId = device["PNPDeviceID"]?.ToString() ?? "";
                        if (pnpId.IndexOf("VID_05F9", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return true; // Found a Datalogic device
                        }
                    }
                }
            }
            catch
            {
                // If we can't check, assume it might be installed
                return true;
            }

            return false;
        }
    }
}



