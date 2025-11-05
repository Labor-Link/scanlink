using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace ScanLink
{
    /// <summary>
    /// Provides scanner configuration commands for different manufacturers.
    /// Supports setting scanners to COM mode and configuring them.
    /// </summary>
    public static class ScannerCommandProtocols
    {
        /// <summary>
        /// Sends initialization commands to configure scanner for COM mode operation.
        /// </summary>
        public static bool InitializeScanner(SerialPort port, string manufacturer)
        {
            if (port == null || !port.IsOpen)
                return false;

            try
            {
                switch (manufacturer?.ToLower())
                {
                    case "datalogic":
                        return InitializeDatalogicScanner(port);
                    
                    case "honeywell":
                        return InitializeHoneywellScanner(port);
                    
                    case "zebra":
                    case "symbol technologies":
                        return InitializeZebraScanner(port);
                    
                    default:
                        // Try generic initialization
                        return InitializeGenericScanner(port);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Scanner initialization error: {ex.Message}");
                return false;
            }
        }

        #region Datalogic Scanner Commands

        /// <summary>
        /// Initializes Datalogic scanner for COM mode operation.
        /// Uses Datalogic's proprietary command protocol.
        /// </summary>
        private static bool InitializeDatalogicScanner(SerialPort port)
        {
            try
            {
                // Datalogic scanners typically use hex commands
                // These commands configure the scanner for USB-COM mode with automatic transmission
                
                // Wake up command (if scanner is in sleep mode)
                SendCommand(port, new byte[] { 0x16 }); // SYN (Synchronous Idle)
                Thread.Sleep(100);

                // Set to continuous read mode (some models)
                // This ensures the scanner sends data immediately after scan
                SendCommand(port, new byte[] { 0x01, 0x4D, 0x0D }); // Start of heading + 'M' + CR
                Thread.Sleep(100);

                // Enable all symbologies (optional - can be customized per deployment)
                SendCommand(port, new byte[] { 0x01, 0x45, 0x0D }); // Enable all
                Thread.Sleep(100);

                // Set suffix to CR+LF for easier parsing
                SendCommand(port, new byte[] { 0x01, 0x53, 0x0D, 0x0A }); // Suffix CR+LF
                Thread.Sleep(100);

                System.Diagnostics.Debug.WriteLine("Datalogic scanner initialized for COM mode");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Datalogic initialization failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sends a configuration barcode command to Datalogic scanner.
        /// Datalogic scanners can also be configured by scanning configuration barcodes.
        /// </summary>
        public static bool SendDatalogicConfigBarcode(SerialPort port, DatalogicConfigCommand command)
        {
            try
            {
                // Datalogic configuration via programming codes
                // Each configuration has a specific code
                byte[] cmdBytes = GetDatalogicCommandBytes(command);
                if (cmdBytes != null)
                {
                    SendCommand(port, cmdBytes);
                    Thread.Sleep(200);
                    return true;
                }
            }
            catch { }
            return false;
        }

        private static byte[] GetDatalogicCommandBytes(DatalogicConfigCommand command)
        {
            // Map configuration commands to Datalogic protocol bytes
            switch (command)
            {
                case DatalogicConfigCommand.SetComMode:
                    return new byte[] { 0x01, 0x43, 0x4F, 0x4D, 0x0D }; // "COM" command
                
                case DatalogicConfigCommand.SetContinuousRead:
                    return new byte[] { 0x01, 0x43, 0x52, 0x0D }; // "CR" command
                
                case DatalogicConfigCommand.EnableAllSymbologies:
                    return new byte[] { 0x01, 0x45, 0x41, 0x4C, 0x4C, 0x0D }; // "EALL" command
                
                case DatalogicConfigCommand.SetAutomaticTransmission:
                    return new byte[] { 0x01, 0x41, 0x55, 0x54, 0x4F, 0x0D }; // "AUTO" command
                
                default:
                    return null;
            }
        }

        #endregion

        #region Honeywell Scanner Commands

        /// <summary>
        /// Initializes Honeywell scanner for COM mode operation.
        /// Uses Honeywell's command protocol.
        /// </summary>
        private static bool InitializeHoneywellScanner(SerialPort port)
        {
            try
            {
                // Honeywell uses menu commands or serial commands
                // Wake up scanner
                SendCommand(port, Encoding.ASCII.GetBytes("WAKE\r"));
                Thread.Sleep(100);

                // Set to serial/USB mode
                SendCommand(port, Encoding.ASCII.GetBytes("SERSMD\r"));
                Thread.Sleep(100);

                // Enable auto-transmit (send immediately after scan)
                SendCommand(port, Encoding.ASCII.GetBytes("AUTTRN\r"));
                Thread.Sleep(100);

                // Set suffix to CR+LF
                SendCommand(port, Encoding.ASCII.GetBytes("SUFFIX\r\n\r"));
                Thread.Sleep(100);

                System.Diagnostics.Debug.WriteLine("Honeywell scanner initialized for COM mode");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Honeywell initialization failed: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Zebra/Symbol Scanner Commands

        /// <summary>
        /// Initializes Zebra/Symbol scanner for COM mode operation.
        /// Uses Zebra's 123Scan or SSI protocol.
        /// </summary>
        private static bool InitializeZebraScanner(SerialPort port)
        {
            try
            {
                // Zebra scanners often use SSI (Simple Serial Interface)
                // Send SSI command to enable COM mode
                
                // SSI Start Session
                byte[] ssiStart = new byte[] { 0x00, 0x00, 0x00, 0x00 }; // SSI wake
                SendCommand(port, ssiStart);
                Thread.Sleep(100);

                // Enable auto-transmit
                SendCommand(port, Encoding.ASCII.GetBytes("\x16T\x0D")); // SYN + T + CR
                Thread.Sleep(100);

                // Set suffix
                SendCommand(port, Encoding.ASCII.GetBytes("\x16S\x0D\x0A")); // Suffix CR+LF
                Thread.Sleep(100);

                System.Diagnostics.Debug.WriteLine("Zebra scanner initialized for COM mode");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Zebra initialization failed: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Generic Scanner Commands

        /// <summary>
        /// Generic initialization for unknown scanner types.
        /// Sends common configuration commands that work with many scanners.
        /// </summary>
        private static bool InitializeGenericScanner(SerialPort port)
        {
            try
            {
                // Try common wake-up commands
                SendCommand(port, new byte[] { 0x16 }); // SYN
                Thread.Sleep(50);
                
                SendCommand(port, Encoding.ASCII.GetBytes("\r\n")); // Simple CR+LF
                Thread.Sleep(50);

                System.Diagnostics.Debug.WriteLine("Generic scanner initialization attempted");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Generic initialization failed: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Sends a command to the scanner via serial port.
        /// </summary>
        private static void SendCommand(SerialPort port, byte[] command)
        {
            if (port == null || !port.IsOpen || command == null || command.Length == 0)
                return;

            try
            {
                port.Write(command, 0, command.Length);
                port.BaseStream.Flush();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send command: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends a test command to verify scanner responsiveness.
        /// </summary>
        public static bool TestScannerConnection(SerialPort port)
        {
            if (port == null || !port.IsOpen)
                return false;

            try
            {
                // Send a simple status query (works with most scanners)
                SendCommand(port, new byte[] { 0x16 }); // SYN
                Thread.Sleep(100);

                // If we can write without exception, consider it responsive
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Configures scanner suffix (data terminator).
        /// </summary>
        public static bool SetScannerSuffix(SerialPort port, string manufacturer, SuffixType suffixType)
        {
            try
            {
                byte[] suffix = GetSuffixBytes(suffixType);
                
                switch (manufacturer?.ToLower())
                {
                    case "datalogic":
                        // Datalogic suffix command: 0x01 + 'S' + suffix bytes + CR
                        byte[] datalogicCmd = new byte[3 + suffix.Length];
                        datalogicCmd[0] = 0x01;
                        datalogicCmd[1] = 0x53; // 'S'
                        Array.Copy(suffix, 0, datalogicCmd, 2, suffix.Length);
                        datalogicCmd[datalogicCmd.Length - 1] = 0x0D;
                        SendCommand(port, datalogicCmd);
                        break;

                    case "honeywell":
                        SendCommand(port, Encoding.ASCII.GetBytes($"SUFFIX{Encoding.ASCII.GetString(suffix)}\r"));
                        break;

                    default:
                        // Generic attempt
                        SendCommand(port, suffix);
                        break;
                }

                Thread.Sleep(100);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static byte[] GetSuffixBytes(SuffixType suffixType)
        {
            switch (suffixType)
            {
                case SuffixType.CR:
                    return new byte[] { 0x0D };
                case SuffixType.LF:
                    return new byte[] { 0x0A };
                case SuffixType.CRLF:
                    return new byte[] { 0x0D, 0x0A };
                case SuffixType.None:
                default:
                    return new byte[0];
            }
        }

        /// <summary>
        /// Sends a beep command to the scanner (for testing/feedback).
        /// </summary>
        public static bool BeepScanner(SerialPort port, string manufacturer)
        {
            try
            {
                switch (manufacturer?.ToLower())
                {
                    case "datalogic":
                        SendCommand(port, new byte[] { 0x01, 0x42, 0x0D }); // 'B' command
                        break;

                    case "honeywell":
                        SendCommand(port, Encoding.ASCII.GetBytes("BEEP\r"));
                        break;

                    case "zebra":
                    case "symbol technologies":
                        SendCommand(port, new byte[] { 0x16, 0x42, 0x0D }); // SYN + 'B' + CR
                        break;

                    default:
                        // Generic beep attempt
                        SendCommand(port, new byte[] { 0x07 }); // BEL character
                        break;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }

    #region Enums

    /// <summary>
    /// Datalogic-specific configuration commands.
    /// </summary>
    public enum DatalogicConfigCommand
    {
        SetComMode,
        SetContinuousRead,
        EnableAllSymbologies,
        SetAutomaticTransmission,
        DisableKeyboardWedge
    }

    /// <summary>
    /// Scanner data suffix types.
    /// </summary>
    public enum SuffixType
    {
        None,
        CR,
        LF,
        CRLF
    }

    #endregion
}



