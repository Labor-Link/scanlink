function Get-UpcCheckDigit {
    param(
        [Parameter(Mandatory = $true)]
        [string]$upc11
    )

    if (-not $upc11 -or $upc11.Length -ne 11 -or ($upc11 -notmatch '^\d{11}$')) {
        throw "UPC-11 payload must contain exactly 11 digits."
    }

    $sumOdd = 0
    $sumEven = 0
    for ($i = 0; $i -lt 11; $i++) {
        $digit = [int]::Parse($upc11.Substring($i, 1))
        if ($i % 2 -eq 0) {
            $sumOdd += $digit
        }
        else {
            $sumEven += $digit
        }
    }

    $total = ($sumOdd * 3) + $sumEven
    $remainder = $total % 10
    if ($remainder -eq 0) {
        return 0
    }
    return 10 - $remainder
}

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
            $currentConnectionType = "USB-COM"
            
            foreach ($line in $assignmentLines) {
                $trimmedLine = $line.Trim()
                
                if ($trimmedLine.StartsWith("PNPDeviceID:")) {
                    $currentPNPDeviceID = $trimmedLine.Substring("PNPDeviceID:".Length).Trim()
                }
                elseif ($trimmedLine.StartsWith("Connection Type:")) {
                    $currentConnectionType = $trimmedLine.Substring("Connection Type:".Length).Trim()
                }
                elseif ($trimmedLine.StartsWith("COM Port:")) {
                    $currentComPort = $trimmedLine.Substring("COM Port:".Length).Trim()
                    if ($currentComPort -eq "Auto-detect") { $currentComPort = "" }
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
                        if (-not $currentConnectionType) { $currentConnectionType = "USB-COM" }
                        $assignmentKey = "$currentConnectionType::$currentPNPDeviceID"
                        $assignments[$assignmentKey] = @{
                            ConnectionType = $currentConnectionType
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
                    $currentConnectionType = "USB-COM"
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
        [string]$productId,
        [string]$parsedInfo,
        [string]$scanStatus,
        [string]$cropId
    )

    # Validate required fields - reject scan if any are null/empty
    $productIdTrimmed = if ($productId) { $productId.Trim() } else { $null }
    $cropIdTrimmed = if ($cropId) { $cropId.Trim() } else { $null }
    $parsedInfoTrimmed = if ($parsedInfo) { $parsedInfo.Trim() } else { $null }

    if ([string]::IsNullOrWhiteSpace($parsedInfoTrimmed) -or
        [string]::IsNullOrWhiteSpace($productIdTrimmed) -or
        [string]::IsNullOrWhiteSpace($cropIdTrimmed)) {
        Write-Host "API log rejected: Missing required field(s) - EmployeeID: '$parsedInfoTrimmed', ProductID: '$productIdTrimmed', CropID: '$cropIdTrimmed'" -ForegroundColor Yellow
        return
    }

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
            productId = $productId
            scanStatus = "SCANNED"
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
        [string]$sourceScanner = "Unknown",    # Optional: source scanner name
        [string]$connectionType = $null,
        [hashtable]$deviceMeta = $null
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
        $resolvedConnectionType = if ($connectionType) { $connectionType } else { "USB-COM" }
        if ($deviceMeta -and $deviceMeta.Keys.Count -eq 0) { $deviceMeta = $null }

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
        elseif ($cleanCode) {
            $digitsOnly = ($cleanCode.ToCharArray() | Where-Object { $_ -match '\d' }) -join ''
            if ($digitsOnly.Length -ge 12) {
                try {
                    $upc = $digitsOnly.Substring($digitsOnly.Length - 12)
                    $employeeId = $upc.Substring(0,5)
                    $productId = $upc.Substring(5,3)
                    $cropId = $upc.Substring(8,3)
                } catch {
                    Write-Host "Scan rejected: Unable to parse UPC payload - $($_.Exception.Message)" -ForegroundColor Yellow
                    return
                }
            }
        }

        # Validate required fields - reject scan if any are null/empty
        $employeeIdTrimmed = if ($employeeId) { $employeeId.Trim() } else { $null }
        $productIdTrimmed = if ($productId) { $productId.Trim() } else { $null }
        $cropIdTrimmed = if ($cropId) { $cropId.Trim() } else { $null }

        if ([string]::IsNullOrWhiteSpace($employeeIdTrimmed) -or
            [string]::IsNullOrWhiteSpace($productIdTrimmed) -or
            [string]::IsNullOrWhiteSpace($cropIdTrimmed)) {
            Write-Host "Scan rejected: Missing required field(s) - EmployeeID: '$employeeIdTrimmed', ProductID: '$productIdTrimmed', CropID: '$cropIdTrimmed'" -ForegroundColor Yellow
            return
        }

        # Determine which scanner was used
        $scannerInfo = $null
        if ($detectedScannerPNPId) {
            $scannerInfo = Get-ScannerByPNPDeviceID -PNPDeviceID $detectedScannerPNPId
        }
        
        # If no specific scanner detected, use the first available scanner as default for COM mode
        if (-not $scannerInfo -and $resolvedConnectionType -eq "USB-COM" -and $allScanners.Count -gt 0) {
            $scannerInfo = $allScanners[0]
        }
        
        # Extract VID/PID from scanner info
        $vid = ""
        $usbPid = ""
        $pnpId = ""
        $serial = ""
        $friendlyName = ""
        $comPort = ""
        $manufacturer = "Unknown"
        $status = "Unknown"
        $devicePath = ""
        $isDatalogic = $false
        $lineID = ""
        $blockID = ""
        
        if ($scannerInfo) {
            $pnpId = $scannerInfo.PNPDeviceID
            $serial = $scannerInfo.SerialNumber
            $friendlyName = $scannerInfo.DeviceName
            $comPort = $scannerInfo.ComPort
            $manufacturer = $scannerInfo.Manufacturer
            $status = $scannerInfo.Status
            $isDatalogic = ($scannerInfo.Manufacturer -eq 'Datalogic')
            
            $vidMatch = [regex]::Match($pnpId, 'VID_([0-9A-F]{4})', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
            $pidMatch = [regex]::Match($pnpId, 'PID_([0-9A-F]{4})', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
            if ($vidMatch.Success) { $vid = $vidMatch.Groups[1].Value.ToUpperInvariant() }
            if ($pidMatch.Success) { $usbPid = $pidMatch.Groups[1].Value.ToUpperInvariant() }
            
            # Get LineID and BlockID from assignments (freshly reloaded)
            $assignmentKey = "$resolvedConnectionType::$pnpId"
            if ($scannerAssignments.ContainsKey($assignmentKey)) {
                $assignment = $scannerAssignments[$assignmentKey]
                $lineID = $assignment.LineID
                $blockID = $assignment.BlockID
                if (-not $comPort -and $assignment.ComPort) {
                    $comPort = $assignment.ComPort
                }
            }
        }
        
        if (-not $pnpId -and $detectedScannerPNPId) {
            $pnpId = $detectedScannerPNPId
        }

        if ($deviceMeta) {
            if ($deviceMeta.ContainsKey('deviceId') -and $deviceMeta.deviceId) { $pnpId = $deviceMeta.deviceId }
            if ($deviceMeta.ContainsKey('vid') -and $deviceMeta.vid) { $vid = $deviceMeta.vid.ToUpperInvariant() }
            if ($deviceMeta.ContainsKey('pid') -and $deviceMeta.pid) { $usbPid = $deviceMeta.pid.ToUpperInvariant() }
            if ($deviceMeta.ContainsKey('serial') -and $deviceMeta.serial) { $serial = $deviceMeta.serial }
            if ($deviceMeta.ContainsKey('friendlyName') -and $deviceMeta.friendlyName) { $friendlyName = $deviceMeta.friendlyName }
            if ($deviceMeta.ContainsKey('manufacturer') -and $deviceMeta.manufacturer) { $manufacturer = $deviceMeta.manufacturer }
            if ($deviceMeta.ContainsKey('devicePath') -and $deviceMeta.devicePath) { $devicePath = $deviceMeta.devicePath }
            if ($deviceMeta.ContainsKey('comPort') -and $deviceMeta.comPort) { $comPort = $deviceMeta.comPort }
            if ($deviceMeta.ContainsKey('lineID') -and $deviceMeta.lineID) { $lineID = $deviceMeta.lineID }
            if ($deviceMeta.ContainsKey('blockID') -and $deviceMeta.blockID) { $blockID = $deviceMeta.blockID }
            if ($deviceMeta.ContainsKey('status') -and $deviceMeta.status) { $status = $deviceMeta.status }
        }
        
        if (-not $friendlyName -and $pnpId) { $friendlyName = $pnpId }
        if (-not $pnpId) { $pnpId = "UNKNOWN" }

        if (-not $lineID -and $pnpId -and $resolvedConnectionType) {
            $assignmentLookupKey = "$resolvedConnectionType::$pnpId"
            if ($scannerAssignments.ContainsKey($assignmentLookupKey)) {
                $assignment = $scannerAssignments[$assignmentLookupKey]
                if (-not $lineID -and $assignment.LineID) { $lineID = $assignment.LineID }
                if (-not $blockID -and $assignment.BlockID) { $blockID = $assignment.BlockID }
                if (-not $comPort -and $assignment.ComPort) { $comPort = $assignment.ComPort }
            }
        }
        
        # Create new scan record with scanner-specific information
        $deviceDetails = [ordered]@{
            vid = $vid
            serial = $serial
            pnpId = $pnpId
            comPort = $comPort
            friendlyName = $friendlyName
            isDatalogic = $isDatalogic
            manufacturer = $manufacturer
            connectionType = $resolvedConnectionType
            lineID = $lineID
            blockID = $blockID
        }
        if ($usbPid) { $deviceDetails["pid"] = $usbPid }
        if ($devicePath) { $deviceDetails["devicePath"] = $devicePath }

        $newRecord = [PSCustomObject]@{
            date = Get-Date -Format "yyyy-MM-dd"
            time = Get-Date -Format "HH:mm:ss"
            employeeId = $employeeId
            cropId = $cropId
            productId = $productId
            lineNumber = $lineID
            blockNumber = $blockID
            sourceScanner = $sourceScanner
            device = [PSCustomObject]$deviceDetails
        }
        
        # Add new record to array
        $scanRecords = @($scanRecords) + $newRecord
        
        # Convert back to JSON and save
        $jsonOutput = $scanRecords | ConvertTo-Json -Depth 6
        Set-Content -Path $OutputFile -Value $jsonOutput -Encoding UTF8
        
        # Also create a simple CSV backup for compatibility
        $csvFile = Join-Path $ProgramDataDir "scans_backup.csv"
        if (-not (Test-Path $csvFile)) {
            Add-Content -Path $csvFile -Value "Timestamp,EmployeeId,CropId,ProductId,LineNumber,BlockNumber,DeviceVid,DeviceSerial,DevicePnpId,DeviceComPort,DeviceFriendlyName"
        }
        $csvLine = "$($newRecord.date) $($newRecord.time),$($newRecord.employeeId),$($newRecord.cropId),$($newRecord.productId),$($newRecord.lineNumber),$($newRecord.blockNumber),$($newRecord.device.vid),$($newRecord.device.serial),$($newRecord.device.pnpId),$($newRecord.device.comPort),$($newRecord.device.friendlyName)"
        Add-Content -Path $csvFile -Value $csvLine
        
        # Create API upload log (userId and siteId will be filled by C# service from token)
        Add-ApiUploadLog -userId "" -siteId "" -lineNumber $lineID -blockNumber $blockID -productId $productId -parsedInfo $employeeId -scanStatus $serial -cropId $cropId
        
        Write-Host "Scan recorded: $code" -ForegroundColor Green
        Write-Host "  Connection: $resolvedConnectionType" -ForegroundColor Yellow
        if ($friendlyName) {
            Write-Host "  Device: $friendlyName" -ForegroundColor Yellow
        }
        if ($lineID -or $blockID) {
            Write-Host "  Line ID: $lineID, Block ID: $blockID" -ForegroundColor Cyan
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
                $hidKeyboardScans = ($records | Where-Object { $_.device.connectionType -eq "USB-HID-KEYBOARD" }).Count
                $hidRawScans = ($records | Where-Object { $_.device.connectionType -eq "USB-HID-RAW" }).Count
                $manualScans = ($records | Where-Object { $_.device.connectionType -eq "MANUAL" }).Count
                
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
                Write-Host "HID Keyboard scans: $hidKeyboardScans" -ForegroundColor White
                [Console]::Out.Flush()
                Write-Host "Raw HID scans: $hidRawScans" -ForegroundColor White
                [Console]::Out.Flush()
                Write-Host "Manual entry scans: $manualScans" -ForegroundColor White
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
    Add-ScanRecord -code $Barcode -detectedScannerPNPId $defaultScannerPNPId -sourceScanner "Manual Input" -connectionType "MANUAL"
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
            
            # Parse input format: "<meta>|BarcodeData"
            $scannerPNPId = $null
            $barcodeData = $inputLine
            $connectionTypeFromMeta = $null
            $deviceMeta = $null
            $sourceLabel = "COM Port Scanner"
            
            if ($inputLine.Contains('|')) {
                $inputParts = $inputLine.Split('|', 2)
                if ($inputParts.Length -ge 2) {
                    $scannerPNPId = $inputParts[0].Trim()
                    $barcodeData = $inputParts[1].Trim()
                }
            }
            
            if ($scannerPNPId) {
                if ($scannerPNPId.StartsWith('{')) {
                    $metaObject = $null
                    try {
                        $metaObject = $scannerPNPId | ConvertFrom-Json -ErrorAction Stop
                    }
                    catch {
                        Write-Host "[DEBUG-PS] Failed to parse JSON metadata: $($_.Exception.Message)" -ForegroundColor DarkYellow
                    }
                    
                    if ($metaObject) {
                        if ($metaObject.connectionType) { $connectionTypeFromMeta = [string]$metaObject.connectionType }
                        if ($metaObject.deviceId) { $scannerPNPId = [string]$metaObject.deviceId }
                        $deviceMeta = @{}
                        if ($metaObject.devicePath) { $deviceMeta.devicePath = [string]$metaObject.devicePath }
                        if ($metaObject.friendlyName) { $deviceMeta.friendlyName = [string]$metaObject.friendlyName }
                        if ($metaObject.vid) { $deviceMeta.vid = [string]$metaObject.vid }
                        if ($metaObject.pid) { $deviceMeta.pid = [string]$metaObject.pid }
                        if ($metaObject.serial) { $deviceMeta.serial = [string]$metaObject.serial }
                        if ($metaObject.manufacturer) { $deviceMeta.manufacturer = [string]$metaObject.manufacturer }
                        if ($metaObject.comPort) { $deviceMeta.comPort = [string]$metaObject.comPort }
                        if ($metaObject.lineID) { $deviceMeta.lineID = [string]$metaObject.lineID }
                        if ($metaObject.blockID) { $deviceMeta.blockID = [string]$metaObject.blockID }
                        if ($metaObject.status) { $deviceMeta.status = [string]$metaObject.status }
                        if ($metaObject.source) { $sourceLabel = [string]$metaObject.source }
                    }
                }
                elseif ($scannerPNPId -match '^(?<type>[^:]+)::(?<id>.*)$') {
                    $connectionTypeFromMeta = $matches['type']
                    $scannerPNPId = $matches['id']
                }
            }

            if (-not $connectionTypeFromMeta -or [string]::IsNullOrWhiteSpace($connectionTypeFromMeta)) {
                if ($scannerPNPId) {
                    $connectionTypeFromMeta = "USB-COM"
                }
                else {
                    $connectionTypeFromMeta = $null
                }
            }

            if ($sourceLabel -eq "COM Port Scanner" -and ($connectionTypeFromMeta -like "USB-HID*")) {
                $sourceLabel = "HID Scanner"
            }

            if ($deviceMeta -and $deviceMeta.Count -eq 0) {
                $deviceMeta = $null
            }
            
            # Record scan with specific scanner identification
            if (-not [string]::IsNullOrWhiteSpace($barcodeData)) {
                Add-ScanRecord -code $barcodeData -detectedScannerPNPId $scannerPNPId -sourceScanner $sourceLabel -connectionType $connectionTypeFromMeta -deviceMeta $deviceMeta
            }
        }
    }
    catch {
        Write-Host "Exiting stdin read: $($_.Exception.Message)" -ForegroundColor Red
        [Console]::Out.Flush()
    }
}
