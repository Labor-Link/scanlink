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

function Send-IssueLog {
    param(
        [string]$subject,
        [string]$message
    )
    try {
        $body = @{
            name = "ScanLink Scanner Script"
            email = "priyanshu@vidyayatan.com"
            phone = ""
            subject = $subject
            message = $message
            deviceInfo = "$($PSVersionTable.OS) / PowerShell"
        } | ConvertTo-Json

        Invoke-RestMethod -Uri "https://backend-stage.labourlinksoftware.co.za/user/public/v1/issue/log" `
            -Method Post `
            -Body $body `
            -ContentType "application/json" `
            -ErrorAction SilentlyContinue | Out-Null
    }
    catch {}
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

# API upload queue: append-only JSONL (one scan per line) for syncing to the backend.
# Appending a single line per scan is O(1) - we never read, re-serialize, or rewrite the
# whole queue here. That is what stops the per-scan cost from growing with the queue (the
# old read-modify-rewrite of one big JSON array got slower as the queue grew and eventually
# back-pressured the scanner). The C# ScanLogUploadService reads new lines from a checkpoint,
# uploads them, then compacts the file. Legacy api_upload_logs.json is migrated to .jsonl by
# the C# service on startup, before this script runs.
$ApiLogsFile = Join-Path $ProgramDataDir "api_upload_logs.jsonl"
if (-not (Test-Path $ApiLogsFile)) {
    New-Item -Path $ApiLogsFile -ItemType File -Force | Out-Null
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
            $currentSupplier = ""
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
                elseif ($trimmedLine.StartsWith("Supplier:")) {
                    $currentSupplier = $trimmedLine.Substring("Supplier:".Length).Trim()
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
                            Supplier = $currentSupplier
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
                    $currentSupplier = ""
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

# ---- Concurrency-safe queue helpers ------------------------------------------------------
# This scanner and the C# upload service both touch api_upload_logs.json from separate
# processes. They coordinate via a named system mutex; writes are atomic (temp file + atomic
# replace) and reads self-heal (salvage + quarantine), so a single corrupt file can never
# silently wedge uploads again.
$script:UploadMutexName = "Global\ScanLinkUploadLogs"

function Invoke-WithUploadLock {
    param([scriptblock]$Action, [int]$TimeoutMs = 5000)
    $mtx = New-Object System.Threading.Mutex($false, $script:UploadMutexName)
    $acquired = $false
    try {
        try { $acquired = $mtx.WaitOne($TimeoutMs) }
        catch [System.Threading.AbandonedMutexException] { $acquired = $true } # prior owner died; we own it now
        if (-not $acquired) { return $false }
        $null = & $Action
        return $true
    }
    finally {
        if ($acquired) { try { $mtx.ReleaseMutex() } catch {} }
        $mtx.Dispose()
    }
}

# NOTE: the old read-modify-rewrite queue helpers (Write-FileAtomic / Get-SalvagedRecords /
# Read-UploadQueue) were removed when the queue became append-only JSONL. The scanner now only
# APPENDS one line per scan (see Add-ApiUploadLog); reading, compaction, and corrupt-line
# tolerance all live on the C# side (ScanLogUploadService.cs). Only Invoke-WithUploadLock (the
# cross-process mutex) is still needed here, to serialise the append against C# compaction.
# ------------------------------------------------------------------------------------------

function Add-ApiUploadLog {
    param(
        [string]$userId,
        [string]$siteId,
        [string]$lineNumber,
        [string]$blockNumber,
        [string]$supplier,
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
        $rejectMsg = "API log rejected: The barcode '$cleanCode' did not contain valid Employee, Product, and Crop IDs. Expected piped format (Emp|Prod|Crop) or a 12-digit UPC sequence."
        Write-Host $rejectMsg -ForegroundColor Yellow
        Send-IssueLog -subject "Bad Barcode API Log Received" -message $rejectMsg
        return
    }

    try {
        # Create timestamp in required format (UTC for API logs)
        $timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")

        # Create new API log entry (userId/siteId are filled in later by the C# service from the token)
        $newApiLog = [PSCustomObject]@{
            userId = $userId
            siteId = $siteId
            timestamp = $timestamp
            lineNumber = $lineNumber
            blockNumber = $blockNumber
            supplier = $supplier
            productId = $productId
            scanStatus = "SCANNED"
            parsedInfo = $parsedInfo
            cropId = $cropId
        }

        # Append-only: serialize THIS scan to one compact single-line JSON record and append it
        # under the cross-process mutex. O(1) - no read/parse/re-serialize of the existing queue.
        # -Compress keeps it on one physical line so the C# reader can split the file by line.
        # The mutex serialises this append against the C# service's compaction (its atomic file
        # replace), so an append can never land in a file that is mid-replace.
        $line = ($newApiLog | ConvertTo-Json -Depth 4 -Compress)
        $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
        $appended = Invoke-WithUploadLock -Action {
            [System.IO.File]::AppendAllText($ApiLogsFile, $line + "`n", $utf8NoBom)
            Write-Host "[API-LOG] Appended 1 scan to api_upload_logs.jsonl" -ForegroundColor DarkGreen
        }

        if ($appended) {
            Write-Host "  API upload log created (will sync when online)" -ForegroundColor DarkCyan
        } else {
            Write-Host "  Could not acquire upload lock; scan preserved in scans_backup.csv only" -ForegroundColor Yellow
            Send-IssueLog -subject "ScanLink queue lock timeout (scanner)" -message "Could not acquire the upload mutex to enqueue a scan within timeout; the scan is safe in scans_backup.csv. Manual re-sync may be needed if this persists."
        }
    }
    catch {
        Write-Host "Error saving API upload log: $($_.Exception.Message)" -ForegroundColor Red
        Send-IssueLog -subject "Error saving API upload log" -message $_.Exception.Message
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
            
            # Guard digit recovery: if scanner dropped the first digit, prepend known guard '1'
            if ($digitsOnly.Length -eq 11) {
                Write-Host "Auto-recovering dropped UPC guard digit '1'" -ForegroundColor Cyan
                $digitsOnly = "1" + $digitsOnly
            }

            if ($digitsOnly.Length -ge 12) {
                try {
                    $bestUpc = $null

                    # Search for a valid 12-digit sequence that matches the checksum
                    $maxStartIndex = $digitsOnly.Length - 12
                    for ($i = 0; $i -le $maxStartIndex; $i++) {
                        $candidate = $digitsOnly.Substring($i, 12)
                        if ($candidate.StartsWith("1")) {
                            $upc11 = $candidate.Substring(0, 11)
                            $expectedCheck = Get-UpcCheckDigit -upc11 $upc11
                            $actualCheck = [int]::Parse($candidate.Substring(11, 1))
                            if ($expectedCheck -eq $actualCheck) {
                                $bestUpc = $candidate
                                break
                            }
                        }
                    }

                    if (-not $bestUpc) {
                        # Fallback: strip EAN-13 zeroes and grab first 12
                        $tempDigits = $digitsOnly
                        while ($tempDigits.Length -gt 12 -and $tempDigits.StartsWith("0")) {
                            $tempDigits = $tempDigits.Substring(1)
                        }
                        $bestUpc = $tempDigits.Substring(0, 12)
                    }

                    $employeeId = $bestUpc.Substring(1,4)  # skip guard digit
                    $productId = $bestUpc.Substring(5,3)
                    $cropId = $bestUpc.Substring(8,3)
                } catch {
                    $parseError = "Scan rejected: Unable to parse UPC payload - $($_.Exception.Message)"
                    Write-Host $parseError -ForegroundColor Yellow
                    Send-IssueLog -subject "Failed to parse UPC" -message $parseError
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
            $rejectMsg = "Scan rejected: The barcode '$cleanCode' did not contain valid Employee, Product, and Crop IDs. Expected piped format (Emp|Prod|Crop) or a 12-digit UPC sequence."
            Write-Host $rejectMsg -ForegroundColor Yellow
            Send-IssueLog -subject "Bad Barcode Scan Received" -message $rejectMsg
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
        $supplier = ""

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
                $supplier = $assignment.Supplier
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
            if ($deviceMeta.ContainsKey('supplier') -and $deviceMeta.supplier) { $supplier = $deviceMeta.supplier }
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
                if (-not $supplier -and $assignment.Supplier) { $supplier = $assignment.Supplier }
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
            supplier = $supplier
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
            supplier = $supplier
            sourceScanner = $sourceScanner
            device = [PSCustomObject]$deviceDetails
        }
        
        # Add new record to array
        $scanRecords = @($scanRecords) + $newRecord

        # Bound the on-disk display log to the most recent N scans. This keeps the file
        # small so (a) the C# app loads it instantly and (b) this writer no longer reads +
        # rewrites an ever-growing array on every scan (which got slower over time and
        # eventually crashed the C# grid at ~2 MB).
        # NOTE: this is the DISPLAY buffer only. Full history is preserved in the web portal
        # (backend, via api_upload_logs.jsonl) and in scans_backup.csv below, so nothing is
        # lost - the desktop grid is a recent-activity view. Do NOT apply this cap to
        # api_upload_logs.jsonl: that is the un-synced upload queue and trimming it would
        # drop scans that never reached the backend.
        # TRADE-OFF: with this JSON-array format the whole array is re-serialized on every
        # scan, so this number also caps per-scan write cost. Lowered from 2000 -> 300 so the
        # per-scan rewrite stays cheap (~6x less work) and the grid loads fast; 300 recent
        # scans is plenty for the on-screen activity view. (The upload queue is now append-only
        # JSONL, so the unbounded cost there is gone independently of this cap.)
        $maxDisplayRecords = 300
        if ($scanRecords.Count -gt $maxDisplayRecords) {
            $scanRecords = @($scanRecords | Select-Object -Last $maxDisplayRecords)
        }

        # Convert back to JSON and save. Force an array even for a single record: Windows
        # PowerShell's ConvertTo-Json emits a bare object '{...}' for one item, which the C#
        # reader (Deserialize<List<...>>) cannot parse and would error on.
        $jsonOutput = $scanRecords | ConvertTo-Json -Depth 6
        if ($scanRecords.Count -eq 1) { $jsonOutput = "[" + $jsonOutput + "]" }
        Set-Content -Path $OutputFile -Value $jsonOutput -Encoding UTF8
        
        # Also create a simple CSV backup for compatibility
        $csvFile = Join-Path $ProgramDataDir "scans_backup.csv"
        if (-not (Test-Path $csvFile)) {
            Add-Content -Path $csvFile -Value "Timestamp,EmployeeId,CropId,ProductId,LineNumber,BlockNumber,DeviceVid,DeviceSerial,DevicePnpId,DeviceComPort,DeviceFriendlyName"
        }
        $csvLine = "$($newRecord.date) $($newRecord.time),$($newRecord.employeeId),$($newRecord.cropId),$($newRecord.productId),$($newRecord.lineNumber),$($newRecord.blockNumber),$($newRecord.device.vid),$($newRecord.device.serial),$($newRecord.device.pnpId),$($newRecord.device.comPort),$($newRecord.device.friendlyName)"
        Add-Content -Path $csvFile -Value $csvLine
        
        # Create API upload log (userId and siteId will be filled by C# service from token)
        Add-ApiUploadLog -userId "" -siteId "" -lineNumber $lineID -blockNumber $blockID -supplier $supplier -productId $productId -parsedInfo $employeeId -scanStatus $serial -cropId $cropId
        
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
        Send-IssueLog -subject "Error saving scan record" -message $_.Exception.Message
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
        Send-IssueLog -subject "Console Read Exception" -message $_.Exception.Message
        [Console]::Out.Flush()
    }
}
