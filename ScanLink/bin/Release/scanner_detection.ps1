# Unified Scanner Detection Script - COM Port Mode
# Detects scanners connected via COM ports (USB-COM mode)
# Usage: 
#   .\scanner_detection.ps1                    # Simple mode
#   .\scanner_detection.ps1 -Detailed          # Detailed mode
#   .\scanner_detection.ps1 -Export            # Export to files

param(
    [switch]$Simple = $false,      # Simple output (default)
    [switch]$Detailed = $false,    # Detailed output with all methods
    [switch]$Export = $false,      # Export to files
    [string]$OutputFile = "scanner_detection.txt"  # Output file name
)

# Known scanner manufacturer VIDs
$scannerManufacturers = @{
    '05F9' = 'Datalogic'
    '0C2E' = 'Honeywell'
    '1504' = 'Zebra'
    '05E0' = 'Symbol Technologies'
    '1A86' = 'CH34x (Generic USB-Serial)'
    '0403' = 'FTDI (Generic USB-Serial)'
}

function Get-ComPortScanners {
    [CmdletBinding()]
    param(
        [bool]$UseDetailedMethods = $false
    )
    
    $allScanners = @()
    
    try {
        if ($UseDetailedMethods) {
            Write-Host "`nMethod: COM Port PnP Detection" -ForegroundColor Yellow
        }
        
        # Get all COM ports from WMI
        $comPorts = Get-CimInstance Win32_PnPEntity | Where-Object {
            $_.Name -match '\(COM\d+\)' -and $_.PNPDeviceID -match 'VID_[0-9A-F]{4}'
        }
        
        foreach ($port in $comPorts) {
            $deviceName = if ($port.Name) { $port.Name } else { "Unknown Device" }
            $pnpId = $port.PNPDeviceID
            $description = if ($port.Description) { $port.Description } else { "" }
            $manufacturer = if ($port.Manufacturer) { $port.Manufacturer } else { "Unknown" }
            $isScanner = $false
            $detectedManufacturer = "Unknown"
            
            # Extract COM port number
            $comPortMatch = [regex]::Match($deviceName, '\(?(COM\d+)\)?', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
            $comPortNumber = if ($comPortMatch.Success) { $comPortMatch.Groups[1].Value.ToUpper() } else { $null }
            
            if (-not $comPortNumber) {
                continue  # Skip if we can't extract COM port
            }
            
            # Check if this is a scanner device
            # Method 1: Check for scanner keywords in name/description
            $scannerKeywords = @('scanner', 'barcode', 'datalogic', 'honeywell', 'zebra', 'symbol', 'qr', 'imager')
            $combinedText = "$deviceName $description $manufacturer".ToLower()
            
            foreach ($keyword in $scannerKeywords) {
                if ($combinedText -match $keyword) {
                    $isScanner = $true
                    break
                }
            }
            
            # Method 2: Check for known scanner manufacturer VIDs
            $vidMatch = [regex]::Match($pnpId, 'VID_([0-9A-F]{4})', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
            if ($vidMatch.Success) {
                $vid = $vidMatch.Groups[1].Value.ToUpperInvariant()
                if ($scannerManufacturers.ContainsKey($vid)) {
                    $isScanner = $true
                    $detectedManufacturer = $scannerManufacturers[$vid]
                }
            }
            
            if ($isScanner) {
                # Extract serial number from PNPDeviceID
                $serial = "Not Found"
                if ($pnpId -and $pnpId.Contains('\')) {
                    $parts = $pnpId.Split('\')
                    if ($parts.Length -ge 3) {
                        $serial = $parts[2]
                    }
                }
                
                $pidMatch = [regex]::Match($pnpId, 'PID_([0-9A-F]{4})', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
                
                $scannerInfo = [PSCustomObject]@{
                    DeviceName = $deviceName
                    PNPDeviceID = $pnpId
                    ComPort = $comPortNumber
                    SerialNumber = $serial
                    VID = if ($vidMatch.Success) { $vidMatch.Groups[1].Value.ToUpperInvariant() } else { "Unknown" }
                    PID = if ($pidMatch.Success) { $pidMatch.Groups[1].Value.ToUpperInvariant() } else { "Unknown" }
                    Manufacturer = if ($detectedManufacturer -ne "Unknown") { $detectedManufacturer } else { $manufacturer }
                    Status = $port.Status
                    Method = "COM_Port_Detection"
                    ConnectionType = "USB-COM"
                }
                
                $allScanners += $scannerInfo
            }
        }
        
        # Also check for available COM ports that might not show in PnP (rare case)
        if ($UseDetailedMethods) {
            Write-Host "`nMethod 2: Direct COM Port Enumeration" -ForegroundColor Yellow
            try {
                $availablePorts = [System.IO.Ports.SerialPort]::GetPortNames()
                $existingPorts = $allScanners | Select-Object -ExpandProperty ComPort
                
                foreach ($portName in $availablePorts) {
                    if ($existingPorts -notcontains $portName) {
                        # Found a COM port not in our scanner list
                        $genericScanner = [PSCustomObject]@{
                            DeviceName = "Generic Scanner on $portName"
                            PNPDeviceID = "GENERIC_$portName"
                            ComPort = $portName
                            SerialNumber = "N/A"
                            VID = "Unknown"
                            PID = "Unknown"
                            Manufacturer = "Generic"
                            Status = "OK"
                            Method = "Direct_Enumeration"
                            ConnectionType = "COM"
                        }
                        
                        $allScanners += $genericScanner
                    }
                }
            }
            catch {
                Write-Host "Direct COM enumeration failed: $($_.Exception.Message)" -ForegroundColor Yellow
            }
        }
    }
    catch {
        Write-Host "Error during scanner detection: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    return $allScanners
}

function Show-ScannerResults {
    param(
        [array]$Scanners,
        [bool]$Detailed = $false
    )
    
    if ($Scanners.Count -eq 0) {
        Write-Host "`nNo COM port scanner devices found." -ForegroundColor Red
        Write-Host "Make sure your scanner devices are:" -ForegroundColor Yellow
        Write-Host "  - Connected to the computer via USB" -ForegroundColor White
        Write-Host "  - Configured for USB-COM mode (not HID keyboard mode)" -ForegroundColor White
        Write-Host "  - Properly installed with USB-COM drivers" -ForegroundColor White
        Write-Host "`nFor Datalogic scanners:" -ForegroundColor Cyan
        Write-Host "  1. Install the Datalogic USB-COM driver from the manufacturer website" -ForegroundColor White
        Write-Host "  2. Scan the programming barcode to enable USB-COM mode" -ForegroundColor White
        Write-Host "  3. Check Device Manager > Ports (COM & LPT) to see the COM port" -ForegroundColor White
        return
    }
    
    if ($Detailed) {
        Write-Host "`nFound $($Scanners.Count) COM port scanner device(s):" -ForegroundColor Green
        Write-Host "=" * 70 -ForegroundColor Green
        
        for ($i = 0; $i -lt $Scanners.Count; $i++) {
            $scanner = $Scanners[$i]
            Write-Host "`nScanner #$($i + 1):" -ForegroundColor Cyan
            Write-Host "  Device Name     : $($scanner.DeviceName)" -ForegroundColor White
            Write-Host "  COM Port        : $($scanner.ComPort)" -ForegroundColor Yellow
            Write-Host "  PNPDeviceID     : $($scanner.PNPDeviceID)" -ForegroundColor DarkYellow
            Write-Host "  Serial Number   : $($scanner.SerialNumber)" -ForegroundColor White
            Write-Host "  VID/PID         : $($scanner.VID)/$($scanner.PID)" -ForegroundColor White
            Write-Host "  Manufacturer    : $($scanner.Manufacturer)" -ForegroundColor White
            Write-Host "  Status          : $($scanner.Status)" -ForegroundColor White
            Write-Host "  Connection Type : $($scanner.ConnectionType)" -ForegroundColor White
            Write-Host "  Detection Method: $($scanner.Method)" -ForegroundColor Gray
        }
    }
    else {
        Write-Host "`nFound $($Scanners.Count) COM port scanner device(s):" -ForegroundColor Green
        Write-Host "=" * 50 -ForegroundColor Green
        
        for ($i = 0; $i -lt $Scanners.Count; $i++) {
            $scanner = $Scanners[$i]
            Write-Host "`nScanner #$($i + 1):" -ForegroundColor Cyan
            Write-Host "  Device Name: $($scanner.DeviceName)" -ForegroundColor White
            Write-Host "  COM Port: $($scanner.ComPort)" -ForegroundColor Yellow
            Write-Host "  Manufacturer: $($scanner.Manufacturer)" -ForegroundColor White
            Write-Host "  Status: $($scanner.Status)" -ForegroundColor White
        }
    }
    
    Write-Host "`n" -ForegroundColor Green
    Write-Host "💡 Configuration Tips:" -ForegroundColor Cyan
    Write-Host "  - Default COM settings: 9600 baud, 8 data bits, No parity, 1 stop bit" -ForegroundColor White
    Write-Host "  - Use Scanner Management UI to configure Line ID and Block ID" -ForegroundColor White
    Write-Host "  - Test connection using the 'Test Scanner' button in management UI" -ForegroundColor White
}

function Export-ScannerResults {
    param(
        [array]$Scanners,
        [string]$FilePath
    )
    
    try {
        $exportData = @()
        foreach ($scanner in $Scanners) {
            $exportData += [PSCustomObject]@{
                DeviceName = $scanner.DeviceName
                ComPort = $scanner.ComPort
                PNPDeviceID = $scanner.PNPDeviceID
                SerialNumber = $scanner.SerialNumber
                VID = $scanner.VID
                PID = $scanner.PID
                Manufacturer = $scanner.Manufacturer
                Status = $scanner.Status
                ConnectionType = $scanner.ConnectionType
                DetectionMethod = $scanner.Method
                Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
            }
        }
        
        # Export as CSV
        $csvFile = $FilePath -replace '\.txt$', '.csv'
        $exportData | Export-Csv -Path $csvFile -NoTypeInformation -Encoding UTF8
        
        # Export as JSON
        $jsonFile = $FilePath -replace '\.txt$', '.json'
        $exportData | ConvertTo-Json -Depth 2 | Set-Content -Path $jsonFile -Encoding UTF8
        
        # Export as text
        $textContent = @()
        $textContent += "COM Port Scanner Detection Results - Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
        $textContent += "=" * 70
        $textContent += ""
        
        for ($i = 0; $i -lt $Scanners.Count; $i++) {
            $scanner = $Scanners[$i]
            $textContent += "Scanner #$($i + 1):"
            $textContent += "  Device Name: $($scanner.DeviceName)"
            $textContent += "  COM Port: $($scanner.ComPort)"
            $textContent += "  PNPDeviceID: $($scanner.PNPDeviceID)"
            $textContent += "  Serial Number: $($scanner.SerialNumber)"
            $textContent += "  VID/PID: $($scanner.VID)/$($scanner.PID)"
            $textContent += "  Manufacturer: $($scanner.Manufacturer)"
            $textContent += "  Status: $($scanner.Status)"
            $textContent += "  Connection Type: $($scanner.ConnectionType)"
            $textContent += "  Detection Method: $($scanner.Method)"
            $textContent += ""
        }
        
        $textContent | Set-Content -Path $FilePath -Encoding UTF8
        
        Write-Host "`nScanner results exported to:" -ForegroundColor Green
        Write-Host "  CSV: $csvFile" -ForegroundColor White
        Write-Host "  JSON: $jsonFile" -ForegroundColor White
        Write-Host "  TXT: $FilePath" -ForegroundColor White
    }
    catch {
        Write-Host "Error exporting scanner results: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Main execution
# If running in Simple mode, output machine-readable format to STDOUT
# Otherwise, show formatted output using Write-Host

if ($Simple) {
    # Simple mode - output machine-readable format to STDOUT for C# parsing
    # First try to detect scanners using detection logic
    $scanners = Get-ComPortScanners -UseDetailedMethods $true
    
    # Output format parseable by C# ScannerManagementForm
    for ($i = 0; $i -lt $scanners.Count; $i++) {
        $scanner = $scanners[$i]
        Write-Output "Scanner #$($i + 1):"
        Write-Output "COM Port: $($scanner.ComPort)"
        Write-Output "PNPDeviceID: $($scanner.PNPDeviceID)"
        Write-Output "DeviceName: $($scanner.DeviceName)"
        Write-Output "Manufacturer: $($scanner.Manufacturer)"
        Write-Output "SerialNumber: $($scanner.SerialNumber)"
        Write-Output "VID: $($scanner.VID)"
        Write-Output "PID: $($scanner.PID)"
        Write-Output "Status: $($scanner.Status)"
        Write-Output ""
    }
} else {
    # Detailed or Default mode - show formatted output
    Write-Host "COM Port Scanner Detection Tool" -ForegroundColor Magenta
    Write-Host "=" * 50 -ForegroundColor Magenta

    # Determine mode
    $useDetailed = $Detailed -or (-not $Simple -and -not $Detailed)
    $useExport = $Export

    if ($useDetailed) {
        Write-Host "Running in Detailed mode..." -ForegroundColor Cyan
    } else {
        Write-Host "Running in Simple mode..." -ForegroundColor Cyan
    }

    # Get scanners
    $scanners = Get-ComPortScanners -UseDetailedMethods $useDetailed

    # Show results
    Show-ScannerResults -Scanners $scanners -Detailed $useDetailed

    # Export if requested
    if ($useExport -and $scanners.Count -gt 0) {
        $outputPath = Join-Path $PSScriptRoot $OutputFile
        Export-ScannerResults -Scanners $scanners -FilePath $outputPath
    }

    Write-Host "`nDetection complete. Found $($scanners.Count) COM port scanner device(s)." -ForegroundColor Green
}
