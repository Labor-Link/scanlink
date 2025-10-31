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

# Get all connected scanners using the unified detection
Write-Host "Detecting connected scanners..." -ForegroundColor Cyan
$allScanners = Get-ScannerDevices -UseDetailedMethods $false

# Ensure $allScanners is always an array
if ($allScanners -is [array]) {
    # Already an array
} else {
    # Single object, wrap in array
    $allScanners = @($allScanners)
}

if ($allScanners.Count -eq 0) {
    Write-Host "WARNING: No scanner devices found." -ForegroundColor Yellow
} else {
    Write-Host "`nDetected $($allScanners.Count) Scanner Device(s):" -ForegroundColor Green
    for ($i = 0; $i -lt $allScanners.Count; $i++) {
        $scanner = $allScanners[$i]
        Write-Host "  Scanner #$($i + 1): $($scanner.DeviceName)" -ForegroundColor White
        Write-Host "    PNPDeviceID: $($scanner.PNPDeviceID)" -ForegroundColor Yellow
        Write-Host "    Serial: $($scanner.SerialNumber)" -ForegroundColor White
        Write-Host "    Manufacturer: $($scanner.Manufacturer)" -ForegroundColor White
        if ($scanner.SecondaryInterface) {
            Write-Host "    Secondary Interface: $($scanner.SecondaryInterface)" -ForegroundColor DarkYellow
        }
    }
}
Write-Host ""

# Function to load scanner assignments (LineID and BlockID) from file
function Load-ScannerAssignments {
    $assignments = @{}
    try {
        $assignmentsPath = Join-Path $ProgramDataDir "scanner_assignments.txt"
        if (Test-Path $assignmentsPath) {
            $assignmentLines = Get-Content -Path $assignmentsPath
            $currentPNPDeviceID = $null
            $currentLineID = ""
            $currentBlockID = ""
            
            foreach ($line in $assignmentLines) {
                $trimmedLine = $line.Trim()
                
                if ($trimmedLine.StartsWith("PNPDeviceID:")) {
                    $currentPNPDeviceID = $trimmedLine.Substring("PNPDeviceID:".Length).Trim()
                }
                elseif ($trimmedLine.StartsWith("Line ID:")) {
                    $currentLineID = $trimmedLine.Substring("Line ID:".Length).Trim()
                }
                elseif ($trimmedLine.StartsWith("Block ID:")) {
                    $currentBlockID = $trimmedLine.Substring("Block ID:".Length).Trim()
                    
                    # Save the complete entry
                    if ($currentPNPDeviceID) {
                        $assignments[$currentPNPDeviceID] = @{
                            LineID = $currentLineID
                            BlockID = $currentBlockID
                        }
                    }
                    
                    # Reset for next entry
                    $currentPNPDeviceID = $null
                    $currentLineID = ""
                    $currentBlockID = ""
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
}

# Create a function to get scanner info by PNPDeviceID
function Get-ScannerByPNPDeviceID {
    param([string]$PNPDeviceID)
    
    foreach ($scanner in $allScanners) {
        if ($scanner.PNPDeviceID -eq $PNPDeviceID) {
            return $scanner
        }
        # Also check secondary interface
        if ($scanner.SecondaryInterface -eq $PNPDeviceID) {
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
        [string]$scanStatus
    )
    
    try {
        # Read existing API logs
        $apiLogsContent = Get-Content -Path $ApiLogsFile -Raw -ErrorAction SilentlyContinue
        if (-not $apiLogsContent) { $apiLogsContent = "[]" }
        
        # Parse existing logs
        $apiLogs = $apiLogsContent | ConvertFrom-Json -ErrorAction SilentlyContinue
        if (-not $apiLogs) { $apiLogs = @() }
        
        # Create timestamp in required format
        $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        
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
            status = "ACTIVE"
            parsedInfo = $parsedInfo
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
        [string]$detectedScannerPNPId = $null,  # Optional: specific scanner PNPDeviceID
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
        if ($code -and $code.Contains('|')) {
            $parts = $code.Split('|')
            if ($parts.Length -ge 2) {
                $employeeId = $parts[0].Trim()
                $productId = $parts[1].Trim()
            }
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
        $isDatalogic = $false
        $lineID = ""
        $blockID = ""
        
        if ($scannerInfo) {
            $pnpId = $scannerInfo.PNPDeviceID
            $serial = $scannerInfo.SerialNumber
            $friendlyName = $scannerInfo.DeviceName
            $isDatalogic = ($pnpId -like "*VID_05F9*")
            
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
            barcode = $code
            employeeId = $employeeId
            productId = $productId
            sourceScanner = $sourceScanner
            device = [PSCustomObject]@{
                vid = $vid
                serial = $serial
                pnpId = $pnpId
                friendlyName = $friendlyName
                isDatalogic = $isDatalogic
                manufacturer = if ($scannerInfo) { $scannerInfo.Manufacturer } else { "Unknown" }
                status = if ($scannerInfo) { $scannerInfo.Status } else { "Unknown" }
                secondaryInterface = if ($scannerInfo -and $scannerInfo.SecondaryInterface) { $scannerInfo.SecondaryInterface } else { $null }
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
            Add-Content -Path $csvFile -Value "Timestamp,Barcode,EmployeeId,ProductId,DeviceVid,DeviceSerial,DevicePnpId,DeviceFriendlyName,LineID,BlockID"
        }
        $csvLine = "$($newRecord.date) $($newRecord.time),$($newRecord.barcode),$($newRecord.employeeId),$($newRecord.productId),$($newRecord.device.vid),$($newRecord.device.serial),$($newRecord.device.pnpId),$($newRecord.device.friendlyName),$($newRecord.device.lineID),$($newRecord.device.blockID)"
        Add-Content -Path $csvFile -Value $csvLine
        
        # Create API upload log (userId and siteId will be filled by C# service from token)
        Add-ApiUploadLog -userId "" -siteId "" -lineNumber $lineID -blockNumber $blockID -productCode $productId -parsedInfo $employeeId -scanStatus $serial
        
        Write-Host "Scan recorded: $code" -ForegroundColor Green
        if ($scannerInfo) {
            Write-Host "  Scanner: $($scannerInfo.DeviceName) ($($scannerInfo.PNPDeviceID))" -ForegroundColor Yellow
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
                
                Write-Host "`n=== Scan Statistics ===" -ForegroundColor Cyan
                [Console]::Out.Flush()
                Write-Host "Total scans: $totalScans" -ForegroundColor White
                [Console]::Out.Flush()
                Write-Host "Today's scans: $todayScans" -ForegroundColor White
                [Console]::Out.Flush()
                Write-Host "Datalogic scans: $datalogicScans" -ForegroundColor White
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
    try {
        Write-Host "Ready. Waiting for barcode input from UI..." -ForegroundColor DarkYellow
        Write-Host "All connected scanners will be monitored for barcode input." -ForegroundColor Cyan
        [Console]::Out.Flush()
        while ($true) {
            $code = [Console]::In.ReadLine()
            if ($code -eq $null) { # StandardInput stream has closed
                Write-Host "[DEBUG-PS] StandardInput stream closed. Exiting loop." -ForegroundColor DarkGray
                [Console]::Out.Flush()
                break
            }
            if ([string]::IsNullOrWhiteSpace($code)) {
                continue 
            }
            # Record scan with automatic scanner detection
            # Use the first available scanner as default (since we can't determine which scanner sent the data)
            $defaultScannerPNPId = if ($allScanners.Count -gt 0) { $allScanners[0].PNPDeviceID } else { $null }
            Add-ScanRecord -code $code -detectedScannerPNPId $defaultScannerPNPId -sourceScanner "Scanner Input"
        }
    }
    catch {
        Write-Host "Exiting stdin read: $($_.Exception.Message)" -ForegroundColor Red
        [Console]::Out.Flush()
    }
}