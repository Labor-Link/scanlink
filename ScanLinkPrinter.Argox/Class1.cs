using System.Threading;
using System.Threading.Tasks;
using ScanLinkPrinter.Protocol;

namespace ScanLinkPrinter.Argox;

public sealed class ArgoxProvider : IPrinterProvider
{
    public string Id => "argox";
    public string DisplayName => "Argox";

    private static class ProviderLogger
    {
        private static string LogDir => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScanLinkPrinter", "logs");
        public static void Log(string message)
        {
            try
            {
                if (!System.IO.Directory.Exists(LogDir)) System.IO.Directory.CreateDirectory(LogDir);
                var path = System.IO.Path.Combine(LogDir, System.DateTime.Now.ToString("yyyyMMdd") + ".log");
                System.IO.File.AppendAllText(path, System.DateTime.Now.ToString("HH:mm:ss.fff ") + message + Environment.NewLine);
            }
            catch { }
        }
    }

    public async Task PrintAsync(PrinterConnectionSettings connection, PrintTextCommand command, CancellationToken cancellationToken = default)
    {
        // Invoke the .NET Framework worker (x64). For USB, try multiple target path formats if needed.
        var workerPath = System.IO.Path.Combine(AppContext.BaseDirectory, "ScanLinkPrinter.ArgoxWorker.exe");

        var targetsToTry = new System.Collections.Generic.List<string>();
        if (connection.ConnectionType == PrinterConnectionType.Usb)
        {
            var t = NormalizeUsbTarget(connection.UsbDevicePath);
            if (t.StartsWith("\\\\?\\USB#", System.StringComparison.OrdinalIgnoreCase))
            {
                // Prefer USBPRINT interface first, then generic USB interface
                var basePart = t.Contains("#{") ? t[..t.LastIndexOf('#')] : t;
                var usbprint = basePart + "#{28d78fad-5a12-11d1-ae5b-0000f803a8c2}";
                var generic = basePart + "#{a5dcbf10-6530-11d2-901f-00c04fb951ed}";
                // Try generic first for some models where USBPRINT interface is inert
                if (!targetsToTry.Contains(generic)) targetsToTry.Add(generic);
                if (!targetsToTry.Contains(usbprint)) targetsToTry.Add(usbprint);
            }
            else if (t.StartsWith("USB\\", System.StringComparison.OrdinalIgnoreCase))
            {
                foreach (var cand in ConvertPnpIdToInterfacePaths(t))
                {
                    // Ensure USBPRINT path is tried before others
                    if (cand.EndsWith("#{a5dcbf10-6530-11d2-901f-00c04fb951ed}", System.StringComparison.OrdinalIgnoreCase))
                    {
                        if (!targetsToTry.Contains(cand)) targetsToTry.Insert(0, cand);
                    }
                    else
                    {
                        if (!targetsToTry.Contains(cand)) targetsToTry.Add(cand);
                    }
                }
            }
            else
            {
                targetsToTry.Add(t);
            }
        }
        else
        {
            // Non-USB: single attempt with provided settings
            targetsToTry.Add(string.Empty);
        }

        System.Exception? lastEx = null;
        bool anySuccess = false;
        ProviderLogger.Log($"Provider start Conn={connection.ConnectionType} Target={connection.UsbDevicePath ?? connection.OutputFilePath ?? connection.SerialPortName ?? connection.TcpAddress}");
        if (targetsToTry.Count > 0) { for (int i = 0; i < targetsToTry.Count; i++) ProviderLogger.Log($"USB candidate[{i}]={targetsToTry[i]}"); }
        foreach (var targetOverride in targetsToTry)
        {
            ProviderLogger.Log($"Launching worker for target='{(string.IsNullOrEmpty(targetOverride) ? (connection.ConnectionType == PrinterConnectionType.Usb ? connection.UsbDevicePath : "") : targetOverride)}'");
            var json = BuildRequestJson(connection, command, string.IsNullOrEmpty(targetOverride) ? null : targetOverride);
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = workerPath,
                Arguments = CreateTempArgFile(json),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            ProviderLogger.Log($"Worker exe: {workerPath}");
            var proc = System.Diagnostics.Process.Start(psi);
            if (proc == null) throw new System.InvalidOperationException("Failed to start Argox worker");

            await Task.Run(() => proc.WaitForExit(), cancellationToken);
            var stdOut = await proc.StandardOutput.ReadToEndAsync();
            var stdErr = await proc.StandardError.ReadToEndAsync();
            ProviderLogger.Log($"Worker exit code={proc.ExitCode}; stdout='{stdOut?.Trim()}' stderr='{stdErr?.Trim()}'");
            if (proc.ExitCode == 0)
            {
                anySuccess = true;
                // For USB, continue to try the alternate interface as well (debugging endpoint behavior)
                if (connection.ConnectionType != PrinterConnectionType.Usb)
                {
                    break;
                }
                else
                {
                    continue;
                }
            }
            var err = string.IsNullOrWhiteSpace(stdErr) ? stdOut : stdErr;
            lastEx = new System.Exception(string.IsNullOrWhiteSpace(err) ? $"Worker failed with code {proc.ExitCode}" : err);
            // If non-USB, stop on first failure
            if (connection.ConnectionType != PrinterConnectionType.Usb) break;
        }
        if (anySuccess) return;
        if (lastEx != null) throw lastEx;
        throw new System.Exception("Print failed without explicit error");
    }

    private static string BuildRequestJson(PrinterConnectionSettings connection, PrintTextCommand command, string? targetOverride = null)
    {
        var connType = connection.ConnectionType switch
        {
            PrinterConnectionType.File => "File",
            PrinterConnectionType.Serial => "Serial",
            PrinterConnectionType.Usb => "Usb",
            PrinterConnectionType.TcpIp => "TcpIp",
            _ => "File"
        };

        string target = string.Empty;
        switch (connection.ConnectionType)
        {
            case PrinterConnectionType.File:
                target = connection.OutputFilePath ?? string.Empty;
                break;
            case PrinterConnectionType.Serial:
                target = connection.SerialPortName ?? "COM1";
                break;
            case PrinterConnectionType.Usb:
                target = NormalizeUsbTarget(targetOverride ?? connection.UsbDevicePath);
                break;
            case PrinterConnectionType.TcpIp:
                target = string.IsNullOrWhiteSpace(connection.TcpAddress) ? string.Empty : ($"{connection.TcpAddress}:{connection.TcpPort}");
                break;
        }

        // Minimal manual JSON to avoid extra dependencies.
        bool isCalibrate = string.Equals(command.Text, "__CALIBRATE__", StringComparison.Ordinal);
        string textEscaped = (command.Text ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        string targetEscaped = (target).Replace("\\", "\\\\").Replace("\"", "\\\"");
        string op = isCalibrate ? "Calibrate" : "PrintText";
        string argoxJson = string.Empty;
        if (command.Argox != null)
        {
            string d = command.Argox.Darkness.HasValue ? $"\"Darkness\":{command.Argox.Darkness.Value}," : string.Empty;
            string r = command.Argox.PrintRate.HasValue ? $"\"PrintRate\":{command.Argox.PrintRate.Value}," : string.Empty;
            string l = command.Argox.LabelLength.HasValue ? $"\"LabelLength\":{command.Argox.LabelLength.Value}," : string.Empty;
            string w = command.Argox.PrintWidth.HasValue ? $"\"PrintWidth\":{command.Argox.PrintWidth.Value}," : string.Empty;
            string inner = (d + r + l + w).TrimEnd(',');
            argoxJson = $",\"Argox\":{{{inner}}}";
        }
        return $"{{\"ConnectionType\":\"{connType}\",\"Target\":\"{targetEscaped}\",\"Text\":\"{textEscaped}\",\"Op\":\"{op}\"{argoxJson}}}";
    }

    private static string NormalizeUsbTarget(string? input)
    {
        try
        {
            var t = input ?? string.Empty;
            if (string.IsNullOrWhiteSpace(t)) return string.Empty;
            t = t.Trim().Trim('"');
            int bar = t.LastIndexOf('|');
            if (bar >= 0 && bar + 1 < t.Length)
            {
                t = t[(bar + 1)..].Trim();
            }
            return t;
        }
        catch { return input ?? string.Empty; }
    }

    private static string CreateTempArgFile(string json)
    {
        try
        {
            var temp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "argox_req_" + System.Guid.NewGuid().ToString("N") + ".json");
            System.IO.File.WriteAllText(temp, json);
            return "@\"" + temp + "\"";
        }
        catch
        {
            return "\"" + json.Replace("\"", "\\\"") + "\"";
        }
    }

    private static System.Collections.Generic.IEnumerable<string> ConvertPnpIdToInterfacePaths(string pnpId)
    {
        // PNPDeviceID example: USB\VID_1664&PID_025E\000000001
        // Interface paths to try:
        // 1) Generic USB device: \\?\USB#VID_1664&PID_XXXX#SERIAL#{a5dcbf10-6530-11d2-901f-00c04fb951ed}
        // 2) USBPRINT interface: \\?\USB#VID_1664&PID_XXXX#SERIAL#{28d78fad-5a12-11d1-ae5b-0000f803a8c2}
        var list = new System.Collections.Generic.List<string>();
        try
        {
            var trimmed = pnpId.Trim();
            if (trimmed.StartsWith("USB\\", System.StringComparison.OrdinalIgnoreCase))
            {
                var body = trimmed.Replace('\\', '#');
                list.Add("\\\\?\\" + body + "#{28d78fad-5a12-11d1-ae5b-0000f803a8c2}");
                list.Add("\\\\?\\" + body + "#{a5dcbf10-6530-11d2-901f-00c04fb951ed}");
            }
        }
        catch { }
        if (list.Count == 0) list.Add(pnpId);
        return list;
    }
}
