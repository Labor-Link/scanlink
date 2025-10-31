# Unified Scanner Detection Script
# Combines simple and detailed scanner detection with multiple output options
# Usage: 
#   .\scanner_detection.ps1                    # Simple mode
#   .\scanner_detection.ps1 -Detailed          # Detailed mode
#   .\scanner_detection.ps1 -Export            # Export to files
#   .\scanner_detection.ps1 -Detailed -Export  # Both detailed and export

param(
    [switch]$Simple = $false,      # Simple output (default)
    [switch]$Detailed = $false,    # Detailed output with all methods
    [switch]$Export = $false,      # Export to files
    [string]$OutputFile = "scanner_detection.txt"  # Output file name
)

# Configuration
$scannerKeywords = @('datalogic')
$datalogicScanners = @{
    '05F9' = 'Datalogic'
}

function Get-ScannerDevices {
    [CmdletBinding()]
    param(
        [bool]$UseDetailedMethods = $false
    )
    
    $allScanners = @()
    
    try {
        if ($UseDetailedMethods) {
            Write-Host "`nMethod 1: Win32_PnPEntity Detection" -ForegroundColor Yellow
        }
        
        # Method 1: Get all PnP devices with VID/PID
        $pnpDevices = Get-CimInstance Win32_PnPEntity | Where-Object {
            $_.PNPDeviceID -match 'VID_[0-9A-F]{4}' -and $_.PNPDeviceID -match 'PID_[0-9A-F]{4}'
        }
        
        foreach ($device in $pnpDevices) {
            $deviceName = if ($device.Name) { $device.Name } else { "Unknown Device" }
            $pnpId = $device.PNPDeviceID
            $isScanner = $false
            $manufacturer = "Unknown"
            
            # Check for specific Datalogic GPS4400 scanner (VID_05F9&PID_2216)
            if ($pnpId -like '*VID_05F9&PID_2216*') {
                $isScanner = $true
                $manufacturer = 'Datalogic GPS4400'
            }
            
            # Check device name for Datalogic keywords
            if ($deviceName -match 'datalogic' -or $pnpId -match 'datalogic') {
                $isScanner = $true
                $manufacturer = 'Datalogic'
            }
            
            # Check VID for Datalogic manufacturer
            $vidMatch = [regex]::Match($pnpId, 'VID_([0-9A-F]{4})', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
            if ($vidMatch.Success) {
                $vid = $vidMatch.Groups[1].Value.ToUpperInvariant()
                if ($datalogicScanners.ContainsKey($vid)) {
                    $isScanner = $true
                    $manufacturer = $datalogicScanners[$vid]
                }
            }
            
            if ($isScanner) {
                # Check if we already have this scanner (same VID/PID but different interface)
                $existingScanner = $allScanners | Where-Object { 
                    $_.PNPDeviceID -like "*VID_05F9&PID_2216*" -and $pnpId -like "*VID_05F9&PID_2216*"
                }
                
                if (-not $existingScanner) {
                    $serial = "Not Found"
                    if ($pnpId -and $pnpId.Contains('\')) {
                        $parts = $pnpId.Split('\')
                        if ($parts.Length -ge 3) {
                            $serial = $parts[2]
                        }
                    }
                    
                    $vidMatch = [regex]::Match($pnpId, 'VID_([0-9A-F]{4})', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
                    $pidMatch = [regex]::Match($pnpId, 'PID_([0-9A-F]{4})', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
                    
                    $scannerInfo = [PSCustomObject]@{
                        DeviceName = $deviceName
                        PNPDeviceID = $pnpId
                        SerialNumber = $serial
                        VID = if ($vidMatch.Success) { $vidMatch.Groups[1].Value.ToUpperInvariant() } else { "Unknown" }
                        PID = if ($pidMatch.Success) { $pidMatch.Groups[1].Value.ToUpperInvariant() } else { "Unknown" }
                        Manufacturer = $manufacturer
                        Status = $device.Status
                        Method = "Win32_PnPEntity"
                        SecondaryInterface = $null
                    }
                    
                    $allScanners += $scannerInfo
                }
                else {
                    # This is the same scanner in a different mode, add as secondary interface
                    $existingScanner.SecondaryInterface = $pnpId
                }
            }
        }
        
        # Method 2: Get-PnpDevice (if detailed mode and available)
        if ($UseDetailedMethods) {
            Write-Host "`nMethod 2: Get-PnpDevice Detection" -ForegroundColor Yellow
            try {
                $hasPnp = Get-Command Get-PnpDevice -ErrorAction SilentlyContinue
                if ($hasPnp) {
                    $pnpDevices = Get-PnpDevice -PresentOnly | Where-Object {
                        $_.InstanceId -match 'VID_[0-9A-F]{4}' -and $_.InstanceId -match 'PID_[0-9A-F]{4}'
                    }
                    
                    foreach ($device in $pnpDevices) {
                        $deviceName = if ($device.FriendlyName) { $device.FriendlyName } elseif ($device.Name) { $device.Name } else { "Unknown Device" }
                        $pnpId = $device.InstanceId
                        $isScanner = $false
                        $manufacturer = "Unknown"
                        
                        # Check for specific Datalogic GPS4400 scanner (VID_05F9&PID_2216)
                        if ($pnpId -like '*VID_05F9&PID_2216*') {
                            $isScanner = $true
                            $manufacturer = 'Datalogic GPS4400'
                        }
                        
                        # Check device name for Datalogic keywords
                        if ($deviceName -match 'datalogic' -or $pnpId -match 'datalogic') {
                            $isScanner = $true
                            $manufacturer = 'Datalogic'
                        }
                        
                        # Check VID for Datalogic manufacturer
                        $vidMatch = [regex]::Match($pnpId, 'VID_([0-9A-F]{4})', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
                        if ($vidMatch.Success) {
                            $vid = $vidMatch.Groups[1].Value.ToUpperInvariant()
                            if ($datalogicScanners.ContainsKey($vid)) {
                                $isScanner = $true
                                $manufacturer = $datalogicScanners[$vid]
                            }
                        }
                        
                        if ($isScanner) {
                            # Check if we already have this scanner
                            $existingScanner = $allScanners | Where-Object { 
                                $_.PNPDeviceID -like "*VID_05F9&PID_2216*" -and $pnpId -like "*VID_05F9&PID_2216*"
                            }
                            
                            if (-not $existingScanner) {
                                $scannerInfo = [PSCustomObject]@{
                                    DeviceName = $deviceName
                                    PNPDeviceID = $pnpId
                                    SerialNumber = "Not Found"
                                    VID = if ($vidMatch.Success) { $vidMatch.Groups[1].Value.ToUpperInvariant() } else { "Unknown" }
                                    PID = if ($pidMatch.Success) { $pidMatch.Groups[1].Value.ToUpperInvariant() } else { "Unknown" }
                                    Manufacturer = $manufacturer
                                    Status = $device.Status
                                    Method = "Get-PnpDevice"
                                    SecondaryInterface = $null
                                }
                                
                                $allScanners += $scannerInfo
                            }
                            else {
                                $existingScanner.SecondaryInterface = $pnpId
                            }
                        }
                    }
                }
                else {
                    Write-Host "Get-PnpDevice not available on this system" -ForegroundColor Yellow
                }
            }
            catch {
                Write-Host "Get-PnpDevice method failed: $($_.Exception.Message)" -ForegroundColor Red
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
        Write-Host "`nNo scanner devices found." -ForegroundColor Red
        Write-Host "Make sure your scanner devices are:" -ForegroundColor Yellow
        Write-Host "  - Connected to the computer" -ForegroundColor White
        Write-Host "  - Powered on" -ForegroundColor White
        Write-Host "  - Properly installed with drivers" -ForegroundColor White
        return
    }
    
    if ($Detailed) {
        Write-Host "`nFound $($Scanners.Count) scanner device(s):" -ForegroundColor Green
        Write-Host "=" * 60 -ForegroundColor Green
        
        for ($i = 0; $i -lt $Scanners.Count; $i++) {
            $scanner = $Scanners[$i]
            Write-Host "`nScanner #$($i + 1):" -ForegroundColor Cyan
            Write-Host "  Device Name    : $($scanner.DeviceName)" -ForegroundColor White
            Write-Host "  PNPDeviceID    : $($scanner.PNPDeviceID)" -ForegroundColor Yellow
            Write-Host "  Serial Number  : $($scanner.SerialNumber)" -ForegroundColor White
            Write-Host "  VID/PID        : $($scanner.VID)/$($scanner.PID)" -ForegroundColor White
            Write-Host "  Manufacturer   : $($scanner.Manufacturer)" -ForegroundColor White
            Write-Host "  Status         : $($scanner.Status)" -ForegroundColor White
            Write-Host "  Detection Method: $($scanner.Method)" -ForegroundColor Gray
            
            if ($scanner.SecondaryInterface) {
                Write-Host "  Secondary Interface: $($scanner.SecondaryInterface)" -ForegroundColor DarkYellow
                Write-Host "  (Same physical scanner in different mode)" -ForegroundColor Gray
            }
        }
    }
    else {
        Write-Host "`nFound $($Scanners.Count) scanner device(s):" -ForegroundColor Green
        Write-Host "=" * 40 -ForegroundColor Green
        
        for ($i = 0; $i -lt $Scanners.Count; $i++) {
            $scanner = $Scanners[$i]
            Write-Host "`nScanner #$($i + 1):" -ForegroundColor Cyan
            Write-Host "  Device Name: $($scanner.DeviceName)" -ForegroundColor White
            Write-Host "  PNPDeviceID: $($scanner.PNPDeviceID)" -ForegroundColor Yellow
            Write-Host "  Manufacturer: $($scanner.Manufacturer)" -ForegroundColor White
            Write-Host "  Status: $($scanner.Status)" -ForegroundColor White
            
            if ($scanner.SecondaryInterface) {
                Write-Host "  Secondary Interface: $($scanner.SecondaryInterface)" -ForegroundColor DarkYellow
                Write-Host "  (Same physical scanner in different mode)" -ForegroundColor Gray
            }
        }
    }
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
                PNPDeviceID = $scanner.PNPDeviceID
                SerialNumber = $scanner.SerialNumber
                VID = $scanner.VID
                PID = $scanner.PID
                Manufacturer = $scanner.Manufacturer
                Status = $scanner.Status
                DetectionMethod = $scanner.Method
                SecondaryInterface = $scanner.SecondaryInterface
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
        $textContent += "Scanner Detection Results - Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
        $textContent += "=" * 60
        $textContent += ""
        
        for ($i = 0; $i -lt $Scanners.Count; $i++) {
            $scanner = $Scanners[$i]
            $textContent += "Scanner #$($i + 1):"
            $textContent += "  Device Name: $($scanner.DeviceName)"
            $textContent += "  PNPDeviceID: $($scanner.PNPDeviceID)"
            $textContent += "  Serial Number: $($scanner.SerialNumber)"
            $textContent += "  VID/PID: $($scanner.VID)/$($scanner.PID)"
            $textContent += "  Manufacturer: $($scanner.Manufacturer)"
            $textContent += "  Status: $($scanner.Status)"
            $textContent += "  Detection Method: $($scanner.Method)"
            if ($scanner.SecondaryInterface) {
                $textContent += "  Secondary Interface: $($scanner.SecondaryInterface)"
            }
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
Write-Host "Unified Scanner Detection Tool" -ForegroundColor Magenta
Write-Host "=" * 40 -ForegroundColor Magenta

# Determine mode
$useDetailed = $Detailed -or (-not $Simple -and -not $Detailed)
$useExport = $Export

if ($useDetailed) {
    Write-Host "Running in Detailed mode..." -ForegroundColor Cyan
} else {
    Write-Host "Running in Simple mode..." -ForegroundColor Cyan
}

# Get scanners
$scanners = Get-ScannerDevices -UseDetailedMethods $useDetailed

# Show results
Show-ScannerResults -Scanners $scanners -Detailed $useDetailed

# Export if requested
if ($useExport -and $scanners.Count -gt 0) {
    $outputPath = Join-Path $PSScriptRoot $OutputFile
    Export-ScannerResults -Scanners $scanners -FilePath $outputPath
}

Write-Host "`nDetection complete. Found $($scanners.Count) unique scanner device(s)." -ForegroundColor Green
