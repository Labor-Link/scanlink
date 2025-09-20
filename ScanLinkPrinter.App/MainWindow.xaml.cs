using System.Windows;
using System.Threading;
using System.Threading.Tasks;
using ScanLinkPrinter.Protocol;
using ScanLinkPrinter.Argox;
using System.Linq;

namespace ScanLinkPrinter.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ConnectionTypeCombo.SelectionChanged += (_, __) => _ = RefreshTargetsAsync();
        // Live preview updates
        TestText.TextChanged += (_, __) => OnUpdatePreview(this, new RoutedEventArgs());
        BarcodeDataBox.TextChanged += (_, __) => OnUpdatePreview(this, new RoutedEventArgs());
        BarcodeXBox.TextChanged += (_, __) => OnUpdatePreview(this, new RoutedEventArgs());
        BarcodeYBox.TextChanged += (_, __) => OnUpdatePreview(this, new RoutedEventArgs());
        BarcodeHeightBox.TextChanged += (_, __) => OnUpdatePreview(this, new RoutedEventArgs());
        BarcodeModuleBox.TextChanged += (_, __) => OnUpdatePreview(this, new RoutedEventArgs());
        PrintWidthBox.TextChanged += (_, __) => OnUpdatePreview(this, new RoutedEventArgs());
        LabelLengthBox.TextChanged += (_, __) => OnUpdatePreview(this, new RoutedEventArgs());
        LoadSettings();
        _ = RefreshTargetsAsync();
        // Auto reconnect will be triggered after first render to avoid await in ctor
        this.Loaded += async (_, __) =>
        {
            if (AutoReconnectCheck.IsChecked == true)
            {
                StatusText.Text = "Auto reconnecting...";
                StatusBarText.Text = "Auto reconnecting...";
                try
                {
                    var settings = BuildConnectionSettings();
                    var provider = new ArgoxProvider();
                    await provider.PrintAsync(settings, new PrintTextCommand { Text = "" });
                    StatusText.Text = "Connected";
                    StatusBarText.Text = "Connected";
                    ConnectionIndicator.Fill = System.Windows.Media.Brushes.LimeGreen;
                }
                catch { StatusText.Text = ""; StatusBarText.Text = ""; ConnectionIndicator.Fill = System.Windows.Media.Brushes.Gray; }
            }
        };
    }

    private async void OnPrintClick(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Printing...";
        BusyBar.Visibility = Visibility.Visible;
        try
        {
            var settings = BuildConnectionSettings();
            var command = new PrintTextCommand
            {
                Text = string.IsNullOrWhiteSpace(TestText.Text) ? "Hello from Scan Link Printer" : TestText.Text,
                Argox = BuildArgoxSettings()
            };

            var provider = new ArgoxProvider();
            await provider.PrintAsync(settings, command);
            StatusText.Text = "Done";
            StatusBarText.Text = $"Printed via {ConnectionTypeCombo.Text} at {System.DateTime.Now:t}";
            SaveSettings();
            // add to history
            HistoryList.Items.Insert(0, $"{DateTime.Now:t}: Printed ({ConnectionTypeCombo.Text}) '{command.Text}'");
            ConnectionIndicator.Fill = System.Windows.Media.Brushes.LimeGreen;
        }
        catch (System.Exception ex)
        {
            StatusText.Text = ex.Message;
            ConnectionIndicator.Fill = System.Windows.Media.Brushes.OrangeRed;
        }
        finally { BusyBar.Visibility = Visibility.Collapsed; }
    }

    private sealed class AppSettings
    {
        public string? ConnectionType { get; set; }
        public string? Target { get; set; }
        public ArgoxSettings? Argox { get; set; }
        public bool AutoReconnect { get; set; }
    }

    private static string SettingsPath => System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "ScanLinkPrinter", "settings.json");

    private void LoadSettings()
    {
        try
        {
            var path = SettingsPath;
            if (System.IO.File.Exists(path))
            {
                var json = System.IO.File.ReadAllText(path);
                var settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                {
                    // Connection type
                    foreach (var item in ConnectionTypeCombo.Items)
                    {
                        if (item is System.Windows.Controls.ComboBoxItem cbi && string.Equals(cbi.Content?.ToString(), settings.ConnectionType))
                        {
                            ConnectionTypeCombo.SelectedItem = cbi;
                            break;
                        }
                    }
                    TargetCombo.Text = settings.Target ?? string.Empty;
                    AutoReconnectCheck.IsChecked = settings.AutoReconnect;
                    if (settings.Argox != null)
                    {
                        if (settings.Argox.Darkness.HasValue) DarknessSlider.Value = settings.Argox.Darkness.Value;
                        if (settings.Argox.PrintRate.HasValue) RateSlider.Value = settings.Argox.PrintRate.Value;
                        if (settings.Argox.LabelLength.HasValue) LabelLengthBox.Text = settings.Argox.LabelLength.Value.ToString();
                        if (settings.Argox.PrintWidth.HasValue) PrintWidthBox.Text = settings.Argox.PrintWidth.Value.ToString();
                    }
                }
            }
        }
        catch { }
    }

    private void SaveSettings()
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(SettingsPath);
            if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir!);
            var typeText = (ConnectionTypeCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "File";
            var payload = new AppSettings
            {
                ConnectionType = typeText,
                Target = TargetCombo.Text,
                Argox = BuildArgoxSettings(),
                AutoReconnect = AutoReconnectCheck.IsChecked == true
            };
            var json = System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }

    private ArgoxSettings BuildArgoxSettings()
    {
        var s = new ArgoxSettings();
        try { s.Darkness = (int)DarknessSlider.Value; } catch { }
        try { s.PrintRate = (int)RateSlider.Value; } catch { }
        // Do not override user/test values; let device defaults apply when unspecified
        if (int.TryParse(LabelLengthBox.Text, out var ll)) s.LabelLength = ll;
        if (int.TryParse(PrintWidthBox.Text, out var pw)) s.PrintWidth = pw;

        // Ensure required media dimensions have defaults if not provided
        if (!s.PrintWidth.HasValue) s.PrintWidth = 812;      // ~4 inches @ 203dpi
        if (!s.LabelLength.HasValue) s.LabelLength = 609;    // ~3 inches @ 203dpi

        // Barcode
        var sym = (BarcodeSymCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Code128";
        var data = BarcodeDataBox.Text;
        if (!string.IsNullOrWhiteSpace(data))
        {
            var b = new ArgoxBarcodeSettings { Symbology = sym, Data = data };
            if (int.TryParse(BarcodeXBox.Text, out var bx)) b.X = bx;
            if (int.TryParse(BarcodeYBox.Text, out var by)) b.Y = by;
            if (int.TryParse(BarcodeHeightBox.Text, out var bh)) b.Height = bh;
            if (int.TryParse(BarcodeModuleBox.Text, out var bm)) b.ModuleSize = bm;
            s.Barcode = b;
        }
        // Image
        if (!string.IsNullOrWhiteSpace(ImagePathBox.Text))
        {
            var img = new ArgoxImageSettings { Path = ImagePathBox.Text };
            if (int.TryParse(ImageXBox.Text, out var ix)) img.X = ix;
            if (int.TryParse(ImageYBox.Text, out var iy)) img.Y = iy;
            if (int.TryParse(ImageWidthBox.Text, out var iw)) img.Width = iw;
            if (int.TryParse(ImageHeightBox.Text, out var ih)) img.Height = ih;
            s.Image = img;
        }
        return s;
    }

    private async void OnCalibrateClick(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Calibrating...";
        BusyBar.Visibility = Visibility.Visible;
        try
        {
            var settings = BuildConnectionSettings();
            var provider = new ArgoxProvider();
            // Reuse Print API by sending special text marker; provider builds Op=Calibrate when Text == null
            await provider.PrintAsync(settings, new PrintTextCommand { Text = "__CALIBRATE__" });
            StatusText.Text = "Calibration command sent";
            StatusBarText.Text = $"Calibration sent via {ConnectionTypeCombo.Text} at {System.DateTime.Now:t}";
            SaveSettings();
            HistoryList.Items.Insert(0, $"{DateTime.Now:t}: Calibrate ({ConnectionTypeCombo.Text})");
            ConnectionIndicator.Fill = System.Windows.Media.Brushes.LimeGreen;
        }
        catch (System.Exception ex)
        {
            StatusText.Text = ex.Message;
            ConnectionIndicator.Fill = System.Windows.Media.Brushes.OrangeRed;
        }
        finally { BusyBar.Visibility = Visibility.Collapsed; }
    }

    private void OnUpdatePreview(object sender, RoutedEventArgs e)
    {
        // Clear
        PreviewCanvas.Children.Clear();
        // Compute scale to fit
        if (!int.TryParse(PrintWidthBox.Text, out var w)) w = 812;
        if (!int.TryParse(LabelLengthBox.Text, out var h)) h = 609;
        double maxW = Math.Max(PreviewCanvas.ActualWidth, PreviewCanvas.MinWidth);
        double maxH = Math.Max(PreviewCanvas.ActualHeight, PreviewCanvas.MinHeight);
        double sx = maxW / w;
        double sy = maxH / h;
        double s = Math.Max(0.1, Math.Min(sx, sy) * 0.9);

        // Border rectangle
        var rect = new System.Windows.Shapes.Rectangle { Width = w * s, Height = h * s, Stroke = System.Windows.Media.Brushes.Gray, StrokeThickness = 1, Fill = System.Windows.Media.Brushes.White };
        PreviewCanvas.Children.Add(rect);
        System.Windows.Controls.Canvas.SetLeft(rect, 10);
        System.Windows.Controls.Canvas.SetTop(rect, 10);

        double ox = 10;
        double oy = 10;

        // Text preview
        if (!string.IsNullOrWhiteSpace(TestText.Text))
        {
            var tb = new System.Windows.Controls.TextBlock { Text = TestText.Text, Foreground = System.Windows.Media.Brushes.Black };
            PreviewCanvas.Children.Add(tb);
            System.Windows.Controls.Canvas.SetLeft(tb, ox + 30 * s);
            System.Windows.Controls.Canvas.SetTop(tb, oy + 30 * s);
        }

        // Barcode preview (simple box)
        if (!string.IsNullOrWhiteSpace(BarcodeDataBox.Text))
        {
            if (int.TryParse(BarcodeXBox.Text, out var bx) && int.TryParse(BarcodeYBox.Text, out var by))
            {
                var brect = new System.Windows.Shapes.Rectangle { Width = 200 * s, Height = 80 * s, Stroke = System.Windows.Media.Brushes.Black, StrokeDashArray = new System.Windows.Media.DoubleCollection { 2, 2 } };
                PreviewCanvas.Children.Add(brect);
                System.Windows.Controls.Canvas.SetLeft(brect, ox + bx * s);
                System.Windows.Controls.Canvas.SetTop(brect, oy + by * s);
            }
        }
        // Image preview (simple box)
        if (!string.IsNullOrWhiteSpace(ImagePathBox.Text))
        {
            if (int.TryParse(ImageXBox.Text, out var ix) && int.TryParse(ImageYBox.Text, out var iy))
            {
                var iw = 120 * s; var ih = 120 * s;
                if (int.TryParse(ImageWidthBox.Text, out var iwd)) iw = iwd * s;
                if (int.TryParse(ImageHeightBox.Text, out var ihd)) ih = ihd * s;
                var irect = new System.Windows.Shapes.Rectangle { Width = iw, Height = ih, Stroke = System.Windows.Media.Brushes.DodgerBlue, StrokeDashArray = new System.Windows.Media.DoubleCollection { 4, 2 } };
                PreviewCanvas.Children.Add(irect);
                System.Windows.Controls.Canvas.SetLeft(irect, ox + ix * s);
                System.Windows.Controls.Canvas.SetTop(irect, oy + iy * s);
            }
        }
    }

    private void OnPresetChanged(object sender, RoutedEventArgs e)
    {
        var sel = (PresetCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Custom";
        // 203 dpi → 8 dots/mm. 4x6 in → 812x1218 dots approx; 2x1 → 406x203; 3x2 → 609x406
        switch (sel)
        {
            case "4x6 Shipping (203dpi)":
                DarknessSlider.Value = 15;
                RateSlider.Value = 4;
                PrintWidthBox.Text = "812";
                LabelLengthBox.Text = "1218";
                break;
            case "2x1 Label (203dpi)":
                DarknessSlider.Value = 12;
                RateSlider.Value = 3;
                PrintWidthBox.Text = "406";
                LabelLengthBox.Text = "203";
                break;
            case "3x2 Product (203dpi)":
                DarknessSlider.Value = 12;
                RateSlider.Value = 3;
                PrintWidthBox.Text = "609";
                LabelLengthBox.Text = "406";
                break;
        }
        OnUpdatePreview(this, new RoutedEventArgs());
    }

    private void OnSavePreset(object sender, RoutedEventArgs e)
    {
        var name = string.IsNullOrWhiteSpace(PresetNameBox.Text) ? "Custom" : PresetNameBox.Text.Trim();
        var preset = new { Name = name, Darkness = (int)DarknessSlider.Value, Rate = (int)RateSlider.Value, Width = PrintWidthBox.Text, Length = LabelLengthBox.Text };
        var dir = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "ScanLinkPrinter");
        if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
        var file = System.IO.Path.Combine(dir, "presets.json");
        System.Collections.Generic.List<object> list;
        if (System.IO.File.Exists(file))
        {
            try { list = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<object>>(System.IO.File.ReadAllText(file)) ?? new System.Collections.Generic.List<object>(); }
            catch { list = new System.Collections.Generic.List<object>(); }
        }
        else list = new System.Collections.Generic.List<object>();
        list.Add(preset);
        System.IO.File.WriteAllText(file, System.Text.Json.JsonSerializer.Serialize(list, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        StatusBarText.Text = $"Saved preset '{name}'";
    }

    private void OnReloadPresets(object sender, RoutedEventArgs e)
    {
        var dir = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "ScanLinkPrinter");
        var file = System.IO.Path.Combine(dir, "presets.json");
        if (!System.IO.File.Exists(file)) return;
        try
        {
            var items = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.Nodes.JsonArray>(System.IO.File.ReadAllText(file));
            if (items == null) return;
            // Append to presets dropdown
            foreach (var node in items)
            {
                var name = node?["Name"]?.ToString();
                if (string.IsNullOrWhiteSpace(name)) continue;
                if (!PresetCombo.Items.OfType<System.Windows.Controls.ComboBoxItem>().Any(i => string.Equals(i.Content?.ToString(), name)))
                {
                    PresetCombo.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = name });
                }
            }
            StatusBarText.Text = "Presets reloaded";
        }
        catch { }
    }

    private PrinterConnectionSettings BuildConnectionSettings()
    {
        var typeText = (ConnectionTypeCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "File";
        var settings = new PrinterConnectionSettings();
        switch (typeText)
        {
            case "File":
                settings.ConnectionType = PrinterConnectionType.File;
                var fileTarget = TargetCombo.Text;
                settings.OutputFilePath = string.IsNullOrWhiteSpace(fileTarget) ? System.IO.Path.Combine(System.IO.Path.GetTempPath(), "argox.out.txt") : fileTarget;
                break;
            case "Serial":
                settings.ConnectionType = PrinterConnectionType.Serial;
                settings.SerialPortName = string.IsNullOrWhiteSpace(TargetCombo.Text) ? "COM1" : TargetCombo.Text;
                if (!System.IO.Ports.SerialPort.GetPortNames().Contains(settings.SerialPortName)) throw new System.Exception("Invalid COM port");
                break;
            case "USB":
                settings.ConnectionType = PrinterConnectionType.Usb;
                // Prefer the raw PNPDeviceID or interface path stored in the ComboBoxItem.Tag
                string rawUsb = TargetCombo.Text;
                if (TargetCombo.SelectedItem is System.Windows.Controls.ComboBoxItem ci && ci.Tag is string tag && !string.IsNullOrWhiteSpace(tag))
                {
                    rawUsb = tag;
                }
                else
                {
                    // If the text is in the friendly format "Name | PNPID", take the part after '|'
                    var txt = TargetCombo.Text ?? string.Empty;
                    int bar = txt.LastIndexOf('|');
                    if (bar >= 0 && bar + 1 < txt.Length)
                    {
                        rawUsb = txt.Substring(bar + 1).Trim();
                    }
                }
                settings.UsbDevicePath = rawUsb;
                break;
            case "TCP/IP":
                settings.ConnectionType = PrinterConnectionType.TcpIp;
                var target = TargetCombo.Text;
                if (!string.IsNullOrWhiteSpace(target) && target.Contains(":"))
                {
                    var parts = target.Split(':');
                    settings.TcpAddress = parts[0];
                    if (parts.Length > 1 && int.TryParse(parts[1], out var port)) settings.TcpPort = port;
                }
                else
                {
                    settings.TcpAddress = string.IsNullOrWhiteSpace(target) ? "127.0.0.1" : target;
                }
                if (string.IsNullOrWhiteSpace(settings.TcpAddress)) throw new System.Exception("Host required");
                break;
        }
        return settings;
    }

    private async Task RefreshTargetsAsync()
    {
        try
        {
            var typeText = (ConnectionTypeCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "File";
            TargetCombo.Items.Clear();
            switch (typeText)
            {
                case "Serial":
                    foreach (var name in System.IO.Ports.SerialPort.GetPortNames())
                        TargetCombo.Items.Add(name);
                    break;
                case "USB":
                    // Enhanced: list Argox USB devices by VID and show friendly name
                    try
                    {
                        var query = "SELECT DeviceID, PNPDeviceID, Name FROM Win32_PnPEntity WHERE PNPDeviceID LIKE '%VID_1664%'";
                        var searcher = new System.Management.ManagementObjectSearcher(query);
                        var list = searcher.Get().Cast<System.Management.ManagementObject>().ToList();
                        foreach (var obj in list)
                        {
                            var pnpId = (obj["PNPDeviceID"] as string) ?? string.Empty;
                            var name = (obj["Name"] as string) ?? "ARGOX USB";
                            // Use ComboBoxItem.Tag to carry raw PNPDeviceID; Content is friendly
                            var item = new System.Windows.Controls.ComboBoxItem { Content = $"{name} | {pnpId}", Tag = pnpId };
                            TargetCombo.Items.Add(item);
                        }
                        if (TargetCombo.Items.Count == 0) TargetCombo.Items.Add("\\\\?\\USB#Vid_1664&Pid_XXXX#...");
                    }
                    catch { TargetCombo.Items.Add("\\\\?\\USB#Vid_1664&Pid_XXXX#..."); }
                    break;
                case "TCP/IP":
                    TargetCombo.Items.Add("192.168.1.50:9100");
                    // if a host is typed, check connectivity
                    if (!string.IsNullOrWhiteSpace(TargetCombo.Text))
                    {
                        _ = Task.Run(() =>
                        {
                            try
                            {
                                var t = TargetCombo.Text;
                                string host = t;
                                int port = 9100;
                                if (t.Contains(":"))
                                {
                                    var parts = t.Split(':');
                                    host = parts[0];
                                    if (parts.Length > 1 && int.TryParse(parts[1], out var p)) port = p;
                                }
                                using var client = new System.Net.Sockets.TcpClient();
                                var ar = client.BeginConnect(host, port, null, null);
                                if (!ar.AsyncWaitHandle.WaitOne(1500)) client.Close();
                            }
                            catch { }
                        });
                    }
                    break;
                case "File":
                    TargetCombo.Items.Add(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "argox.out.txt"));
                    break;
            }
        }
        catch { }
    }

    private async void OnRefreshTargets(object sender, RoutedEventArgs e)
    {
        await RefreshTargetsAsync();
    }

    private async void OnTestConnection(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Testing...";
        try
        {
            var typeText = (ConnectionTypeCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "File";
            switch (typeText)
            {
                case "Serial":
                    var port = TargetCombo.Text;
                    if (string.IsNullOrWhiteSpace(port)) throw new System.Exception("Select a COM port");
                    using (var sp = new System.IO.Ports.SerialPort(port)) { sp.ReadTimeout = 500; sp.WriteTimeout = 500; sp.Open(); }
                    StatusText.Text = "Serial OK";
                    ConnectionIndicator.Fill = System.Windows.Media.Brushes.LimeGreen;
                    break;
                case "TCP/IP":
                    var t = TargetCombo.Text;
                    if (string.IsNullOrWhiteSpace(t)) throw new System.Exception("Enter host:port");
                    string host = t; int p = 9100; if (t.Contains(":")) { var parts = t.Split(':'); host = parts[0]; if (parts.Length > 1 && int.TryParse(parts[1], out var tp)) p = tp; }
                    using (var client = new System.Net.Sockets.TcpClient()) { var ar = client.BeginConnect(host, p, null, null); if (!ar.AsyncWaitHandle.WaitOne(1500)) throw new System.Exception("TCP timeout"); }
                    StatusText.Text = "TCP OK";
                    ConnectionIndicator.Fill = System.Windows.Media.Brushes.LimeGreen;
                    break;
                case "USB":
                    StatusText.Text = "USB detected (ensure correct device path)";
                    ConnectionIndicator.Fill = System.Windows.Media.Brushes.Goldenrod;
                    break;
                default:
                    StatusText.Text = "Ready";
                    ConnectionIndicator.Fill = System.Windows.Media.Brushes.Gray;
                    break;
            }
        }
        catch (System.Exception ex)
        {
            StatusText.Text = ex.Message;
            ConnectionIndicator.Fill = System.Windows.Media.Brushes.OrangeRed;
        }
    }

    private void OnCopyError(object sender, RoutedEventArgs e)
    {
        try
        {
            var combined = $"{StatusText.Text}\n{StatusBarText.Text}";
            System.Windows.Clipboard.SetText(combined);
            StatusBarText.Text = "Error text copied";
        }
        catch { }
    }

    private void OnBrowseImage(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*"
        };
        var res = dlg.ShowDialog(this);
        if (res == true)
        {
            ImagePathBox.Text = dlg.FileName;
            OnUpdatePreview(this, new RoutedEventArgs());
        }
    }

    // Simulation removed once provider is wired.
}