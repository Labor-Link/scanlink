param(
    [string]$Barcode = ""
)

# Include the unified scanner detection functions
. "$PSScriptRoot\scanner_detection.ps1"

# Use ProgramData for writable storage
$ProgramDataDir = Join-Path $env:ProgramData "ScanLink"
try { New-Item -Path $ProgramDataDir -ItemType Directory -Force | Out-Null } catch {}

# Paths to writable files
$OutputFile = Join-Path $ProgramDataDir "scans.txt"
if (-not (Test-Path $OutputFile)) {
    New-Item -Path $OutputFile -ItemType File -Force | Out-Null
}

# API upload logs file (for syncing to backend when internet is available)
$ApiLogsFile = Join-Path $ProgramDataDir "api_upload_logs.json"
if (-not (Test-Path $ApiLogsFile)) {
    New-Item -Path $ApiLogsFile -ItemType File -Force | Out-Null
    Add-Content -Path $ApiLogsFile -Value "[]"
}

# Get all connected COM port scanners using the unified detection
Write-Host "Detecting connected COM port scanners..." -ForegroundColor Cyan
$allScanners = Get-ComPortScanners -UseDetailedMethods $false

# Ensure $allScanners is always an array
if ($allScanners -is [array]) {
    # Already an array
} else {
    # Single object, wrap in array
    $allScanners = @($allScanners)
}

if ($allScanners.Count -eq 0) {
    Write-Host "WARNING: No COM port scanner devices found." -ForegroundColor Yellow
    Write-Host "Please ensure:" -ForegroundColor Yellow
    Write-Host "  1. Scanner is connected via USB" -ForegroundColor White
    Write-Host "  2. USB-COM driver is installed (e.g., Datalogic USB-COM driver)" -ForegroundColor White
    Write-Host "  3. Scanner is configured for USB-COM mode (not HID keyboard mode)" -ForegroundColor White
    Write-Host "  4. Check Device Manager > Ports (COM & LPT) for the scanner" -ForegroundColor White
} else {
    Write-Host "`nDetected $($allScanners.Count) COM Port Scanner(s):" -ForegroundColor Green
    for ($i = 0; $i -lt $allScanners.Count; $i++) {
        $scanner = $allScanners[$i]
        Write-Host "  Scanner #$($i + 1): $($scanner.DeviceName)" -ForegroundColor White
        Write-Host "    COM Port: $($scanner.ComPort)" -ForegroundColor Yellow
        Write-Host "    PNPDeviceID: $($scanner.PNPDeviceID)" -ForegroundColor DarkYellow
        Write-Host "    Serial: $($scanner.SerialNumber)" -ForegroundColor White
        Write-Host "    Manufacturer: $($scanner.Manufacturer)" -ForegroundColor White
    }
}
Write-Host ""

# Function to load scanner assignments (LineID, BlockID, and COM settings) from file
function Load-ScannerAssignments {
    $assignments = @{}
    try {
        $assignmentsPath = Join-Path $ProgramDataDir "scanner_assignments.txt"
        if (Test-Path $assignmentsPath) {
            $assignmentLines = Get-Content -Path $assignmentsPath
            $currentPNPDeviceID = $null
            $currentComPort = ""
            $currentLineID = ""
            $currentBlockID = ""
            $currentBaudRate = "9600"
            $currentParity = "None"
            $currentDataBits = "8"
            $currentStopBits = "One"
            
            foreach ($line in $assignmentLines) {
                $trimmedLine = $line.Trim()
                
                if ($trimmedLine.StartsWith("PNPDeviceID:")) {
                    $currentPNPDeviceID = $trimmedLine.Substring("PNPDeviceID:".Length).Trim()
                }
                elseif ($trimmedLine.StartsWith("COM Port:")) {
                    $currentComPort = $trimmedLine.Substring("COM Port:".Length).Trim()
                }
                elseif ($trimmedLine.StartsWith("Line ID:")) {
                    $currentLineID = $trimmedLine.Substring("Line ID:".Length).Trim()
                }
                elseif ($trimmedLine.StartsWith("Block ID:")) {
                    $currentBlockID = $trimmedLine.Substring("Block ID:".Length).Trim()
                }
                elseif ($trimmedLine.StartsWith("Baud Rate:")) {
                    $currentBaudRate = $trimmedLine.Substring("Baud Rate:".Length).Trim()
                }
                elseif ($trimmedLine.StartsWith("Parity:")) {
                    $currentParity = $trimmedLine.Substring("Parity:".Length).Trim()
                }
                elseif ($trimmedLine.StartsWith("Data Bits:")) {
                    $currentDataBits = $trimmedLine.Substring("Data Bits:".Length).Trim()
                }
                elseif ($trimmedLine.StartsWith("Stop Bits:")) {
                    $currentStopBits = $trimmedLine.Substring("Stop Bits:".Length).Trim()
                    
                    # Save the complete entry
                    if ($currentPNPDeviceID) {
                        $assignments[$currentPNPDeviceID] = @{
                            ComPort = $currentComPort
                            LineID = $currentLineID
                            BlockID = $currentBlockID
                            BaudRate = $currentBaudRate
                            Parity = $currentParity
                            DataBits = $currentDataBits
                            StopBits = $currentStopBits
                        }
                    }
                    
                    # Reset for next entry
                    $currentPNPDeviceID = $null
                    $currentComPort = ""
                    $currentLineID = ""
                    $currentBlockID = ""
                    $currentBaudRate = "9600"
                    $currentParity = "None"
                    $currentDataBits = "8"
                    $currentStopBits = "One"
                }
            }
        }
    }
    catch {
        Write-Host "Could not load scanner assignments: $($_.Exception.Message)" -ForegroundColor Yellow
    }
    return $assignments
}

# Load scanner assignments initially
$scannerAssignments = Load-ScannerAssignments
if ($scannerAssignments.Count -gt 0) {
    Write-Host "Loaded scanner assignments from file." -ForegroundColor Cyan
    Write-Host "  Total assigned scanners: $($scannerAssignments.Count)" -ForegroundColor White
}

# Create a function to get scanner info by PNPDeviceID
function Get-ScannerByPNPDeviceID {
    param([string]$PNPDeviceID)
    
    foreach ($scanner in $allScanners) {
        if ($scanner.PNPDeviceID -eq $PNPDeviceID) {
            return $scanner
        }
    }
    return $null
}

# Initialize JSON array if file is empty
try {
    $fileInfo = Get-Item $OutputFile -ErrorAction SilentlyContinue
    if ($fileInfo -and $fileInfo.Length -eq 0) {
        Add-Content -Path $OutputFile -Value "[]"
    }
}
catch {}

function Add-ApiUploadLog {
    param(
        [string]$userId,
        [string]$siteId,
        [string]$lineNumber,
        [string]$blockNumber,
        [string]$productCode,
        [string]$parsedInfo,
        [string]$scanStatus,
        [string]$cropId
    )
    
    try {
        # Read existing API logs
        $apiLogsContent = Get-Content -Path $ApiLogsFile -Raw -ErrorAction SilentlyContinue
        if (-not $apiLogsContent) { $apiLogsContent = "[]" }
        
        # Parse existing logs
        $apiLogs = $apiLogsContent | ConvertFrom-Json -ErrorAction SilentlyContinue
        if (-not $apiLogs) { $apiLogs = @() }
        
        # Create timestamp in required format (UTC for API logs)
        $timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")
        
        # Create new API log entry
        $newApiLog = [PSCustomObject]@{
            userId = $userId
            siteId = $siteId
            timestamp = $timestamp
            lineNumber = $lineNumber
            blockNumber = $blockNumber
            productCode = $productCode
            scanStatus = "SCANNED"
            errorMessage = ""
            parsedInfo = $parsedInfo
            cropId = $cropId
        }
        
        # Add new log to array
        $apiLogs = @($apiLogs) + $newApiLog
        
        # Convert back to JSON and save
        $jsonOutput = $apiLogs | ConvertTo-Json -Depth 4
        Set-Content -Path $ApiLogsFile -Value $jsonOutput -Encoding UTF8
        Write-Host "[API-LOG] Appended to api_upload_logs.json ($($apiLogs.Count) total)" -ForegroundColor DarkGreen
        
        Write-Host "  API upload log created (will sync when online)" -ForegroundColor DarkCyan
    }
    catch {
        Write-Host "Error saving API upload log: $($_.Exception.Message)" -ForegroundColor Red
    }
}

function Add-ScanRecord {
    param(
        [string]$code,
        [string]$detectedScannerPNPId = $null,  # Required: specific scanner PNPDeviceID from C#
        [string]$sourceScanner = "Unknown"      # Optional: source scanner name
    )
    
    try {
        # Reload scanner assignments to get latest LineID/BlockID (supports real-time updates)
        $script:scannerAssignments = Load-ScannerAssignments
        
        # Read existing JSON data
        $jsonContent = Get-Content -Path $OutputFile -Raw -ErrorAction SilentlyContinue
        if (-not $jsonContent) { $jsonContent = "[]" }
        
        # Parse existing JSON
        $scanRecords = $jsonContent | ConvertFrom-Json -ErrorAction SilentlyContinue
        if (-not $scanRecords) { $scanRecords = @() }
        
        $employeeId = $null
        $productId = $null
        $cropId = $null

        # Sanitize barcode: trim, remove AIM Symbology Identifier (e.g., ]C1) and non-printables
        $cleanCode = $code
        if ($cleanCode) { $cleanCode = $cleanCode.Trim() }
        if ($cleanCode -and $cleanCode.StartsWith(']') -and $cleanCode.Length -ge 3) {
            # Remove leading AIM identifier like ]C1, ]A0, etc.
            $cleanCode = $cleanCode.Substring(3)
        }
        if ($cleanCode) {
            $cleanCode = ($cleanCode.ToCharArray() | Where-Object { [int]$_ -ge 32 -and [int]$_ -le 126 }) -join ''
        }

        if ($cleanCode -and $cleanCode.Contains('|')) {
            $parts = $cleanCode.Split('|')
            if ($parts.Length -ge 2) {
                $employeeId = $parts[0].Trim()
                $productId = $parts[1].Trim()
                if ($parts.Length -ge 3) { $cropId = $parts[2].Trim() }
            }
        }
        elseif ($cleanCode -and $cleanCode.Length -ge 16) {
            try {
                $employeeId = $cleanCode.Substring(0,10)
                $productId = $cleanCode.Substring(10,3)
                $cropId = $cleanCode.Substring(13,3)
            } catch {}
        }
        
        # Determine which scanner was used
        $scannerInfo = $null
        if ($detectedScannerPNPId) {
            $scannerInfo = Get-ScannerByPNPDeviceID -PNPDeviceID $detectedScannerPNPId
        }
        
        # If no specific scanner detected, use the first available scanner as default
        if (-not $scannerInfo -and $allScanners.Count -gt 0) {
            $scannerInfo = $allScanners[0]
        }
        
        # Extract VID/PID from scanner info
        $vid = ""
        $usbPid = ""
        $pnpId = ""
        $serial = ""
        $friendlyName = ""
        $comPort = ""
        $isDatalogic = $false
        $lineID = ""
        $blockID = ""
        
        if ($scannerInfo) {
            $pnpId = $scannerInfo.PNPDeviceID
            $serial = $scannerInfo.SerialNumber
            $friendlyName = $scannerInfo.DeviceName
            $comPort = $scannerInfo.ComPort
            $isDatalogic = ($scannerInfo.Manufacturer -eq 'Datalogic')
            
            $vidMatch = [regex]::Match($pnpId, 'VID_([0-9A-F]{4})', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
            $pidMatch = [regex]::Match($pnpId, 'PID_([0-9A-F]{4})', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
            if ($vidMatch.Success) { $vid = $vidMatch.Groups[1].Value.ToUpperInvariant() }
            if ($pidMatch.Success) { $usbPid = $pidMatch.Groups[1].Value.ToUpperInvariant() }
            
            # Get LineID and BlockID from assignments (freshly reloaded)
            if ($scannerAssignments.ContainsKey($pnpId)) {
                $assignment = $scannerAssignments[$pnpId]
                $lineID = $assignment.LineID
                $blockID = $assignment.BlockID
            }
        }
        
        # Create new scan record with scanner-specific information
        $newRecord = [PSCustomObject]@{
            date = Get-Date -Format "yyyy-MM-dd"
            time = Get-Date -Format "HH:mm:ss"
            barcode = ($cleanCode ?? $code)
            employeeId = $employeeId
            productId = $productId
            cropId = $cropId
            sourceScanner = $sourceScanner
            device = [PSCustomObject]@{
                vid = $vid
                serial = $serial
                pnpId = $pnpId
                comPort = $comPort
                friendlyName = $friendlyName
                isDatalogic = $isDatalogic
                manufacturer = if ($scannerInfo) { $scannerInfo.Manufacturer } else { "Unknown" }
                status = if ($scannerInfo) { $scannerInfo.Status } else { "Unknown" }
                connectionType = "USB-COM"
                lineID = $lineID
                blockID = $blockID
            }
            session = [PSCustomObject]@{
                user = $env:USERNAME
                computer = $env:COMPUTERNAME
            }
        }
        
        # Add new record to array
        $scanRecords = @($scanRecords) + $newRecord
        
        # Convert back to JSON and save
        $jsonOutput = $scanRecords | ConvertTo-Json -Depth 4
        Set-Content -Path $OutputFile -Value $jsonOutput -Encoding UTF8
        
        # Also create a simple CSV backup for compatibility
        $csvFile = Join-Path $ProgramDataDir "scans_backup.csv"
        if (-not (Test-Path $csvFile)) {
            Add-Content -Path $csvFile -Value "Timestamp,Barcode,EmployeeId,ProductId,CropId,DeviceVid,DeviceSerial,DevicePnpId,DeviceComPort,DeviceFriendlyName,LineID,BlockID"
        }
        $csvLine = "$($newRecord.date) $($newRecord.time),$($newRecord.barcode),$($newRecord.employeeId),$($newRecord.productId),$($newRecord.cropId),$($newRecord.device.vid),$($newRecord.device.serial),$($newRecord.device.pnpId),$($newRecord.device.comPort),$($newRecord.device.friendlyName),$($newRecord.device.lineID),$($newRecord.device.blockID)"
        Add-Content -Path $csvFile -Value $csvLine
        
        # Create API upload log (userId and siteId will be filled by C# service from token)
        Add-ApiUploadLog -userId "" -siteId "" -lineNumber $lineID -blockNumber $blockID -productCode $productId -parsedInfo $employeeId -scanStatus $serial -cropId $cropId
        
        Write-Host "Scan recorded: $code" -ForegroundColor Green
        if ($scannerInfo) {
            Write-Host "  Scanner: $($scannerInfo.DeviceName) (COM $($scannerInfo.ComPort))" -ForegroundColor Yellow
            if ($lineID -or $blockID) {
                Write-Host "  Line ID: $lineID, Block ID: $blockID" -ForegroundColor Cyan
            }
        }
        [Console]::Out.Flush()
    }
    catch {
        Write-Host "Error saving scan: $($_.Exception.Message)" -ForegroundColor Red
        [Console]::Out.Flush()
    }
}

function Show-ScanStats {
    try {
        $jsonContent = Get-Content -Path $OutputFile -Raw -ErrorAction SilentlyContinue
        if ($jsonContent) {
            $records = $jsonContent | ConvertFrom-Json -ErrorAction SilentlyContinue
            if ($records) {
                $totalScans = $records.Count
                $todayScans = ($records | Where-Object { $_.date -eq (Get-Date -Format "yyyy-MM-dd") }).Count
                $datalogicScans = ($records | Where-Object { $_.device.isDatalogic -eq $true }).Count
                $comPortScans = ($records | Where-Object { $_.device.connectionType -eq "USB-COM" }).Count
                
                Write-Host "`n=== Scan Statistics ===" -ForegroundColor Cyan
                [Console]::Out.Flush()
                Write-Host "Total scans: $totalScans" -ForegroundColor White
                [Console]::Out.Flush()
                Write-Host "Today's scans: $todayScans" -ForegroundColor White
                [Console]::Out.Flush()
                Write-Host "Datalogic scans: $datalogicScans" -ForegroundColor White
                [Console]::Out.Flush()
                Write-Host "COM Port scans: $comPortScans" -ForegroundColor White
                [Console]::Out.Flush()
                Write-Host "=======================`n" -ForegroundColor Cyan
                [Console]::Out.Flush()
            }
        }
    }
    catch {}
}

Show-ScanStats

if (-not [string]::IsNullOrWhiteSpace($Barcode)) {
    # If a barcode is passed as an argument, process it directly
    # Use the first available scanner for manual input
    $defaultScannerPNPId = if ($allScanners.Count -gt 0) { $allScanners[0].PNPDeviceID } else { $null }
    Add-ScanRecord -code $Barcode -detectedScannerPNPId $defaultScannerPNPId -sourceScanner "Manual Input"
}
else {
    # Continuously read from stdin for barcodes from the C# application
    # C# will send data in format: "PNPDeviceID|BarcodeData"
    try {
        Write-Host "Ready. Waiting for barcode input from C# COM Port Manager..." -ForegroundColor DarkYellow
        Write-Host "All connected COM port scanners are monitored by C# application." -ForegroundColor Cyan
        Write-Host "This script processes scan records received from C#." -ForegroundColor Cyan
        [Console]::Out.Flush()
        while ($true) {
            $inputLine = [Console]::In.ReadLine()
            if ($inputLine -eq $null) { # StandardInput stream has closed
                Write-Host "[DEBUG-PS] StandardInput stream closed. Exiting loop." -ForegroundColor DarkGray
                [Console]::Out.Flush()
                break
            }
            if ([string]::IsNullOrWhiteSpace($inputLine)) {
                continue 
            }
            
            # Parse input format: "PNPDeviceID|BarcodeData"
            $scannerPNPId = $null
            $barcodeData = $inputLine
            
            if ($inputLine.Contains('|')) {
                $inputParts = $inputLine.Split('|', 2)
                if ($inputParts.Length -ge 2) {
                    $scannerPNPId = $inputParts[0].Trim()
                    $barcodeData = $inputParts[1].Trim()
                }
            }
            
            # Record scan with specific scanner identification
            if (-not [string]::IsNullOrWhiteSpace($barcodeData)) {
                Add-ScanRecord -code $barcodeData -detectedScannerPNPId $scannerPNPId -sourceScanner "COM Port Scanner"
            }
        }
    }
    catch {
        Write-Host "Exiting stdin read: $($_.Exception.Message)" -ForegroundColor Red
        [Console]::Out.Flush()
    }
}
