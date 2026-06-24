using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace ScanLink
{
    // Upload queue design (append-only JSONL, no checkpoint file):
    //
    //   * The scanner (scan_capture.ps1) APPENDS one compact JSON object per line to
    //     api_upload_logs.jsonl, under the cross-process mutex. That append is O(1) - it never
    //     reads, parses, or rewrites the existing queue - which is what stops the per-scan cost
    //     from growing with the queue and back-pressuring scanning (the old design rewrote the
    //     whole JSON array on every scan).
    //
    //   * This service drains the queue in batches: under the mutex it reads the first N lines,
    //     releases the lock, enriches them IN MEMORY (userId/siteId/cartonTypeId from the token -
    //     never persisted) and POSTs them; on success it re-acquires the mutex, re-reads the file
    //     (PS may have appended more lines at the end during the POST) and writes back only the
    //     lines AFTER the first N. So a line is removed ONLY after it was uploaded -> no scan can
    //     be lost. A crash between a successful POST and the writeback re-uploads that batch next
    //     run (duplicates) - the same window the previous code already had; never data loss.
    //
    //   * GetPendingCount() == current line count (uploaded lines are physically removed), so the
    //     cleanup safety-gate still blocks wiping the display while scans are unsynced.
    //
    //   * Legacy api_upload_logs.json (one big JSON array) is migrated to .jsonl on startup,
    //     before the scanner script runs, so no queued scan is lost across the upgrade.
    public class ScanLogUploadService : IDisposable
    {
        private readonly string _jsonlPath;       // api_upload_logs.jsonl  (the live queue)
        private readonly string _legacyJsonPath;  // api_upload_logs.json   (pre-upgrade array, migrated once)
        private readonly string _programDataDir;
        private readonly ApiAuthService _authService;
        private ProductCombinationsService _productCombinationsService;
        private Timer _uploadTimer;
        private readonly int _uploadIntervalSeconds = 30; // Check every 30 seconds
        private const int UploadBatchSize = 500;          // records per POST (bounds payload + memory)
        private const int MaxBatchesPerCycle = 50;        // up to 25k scans drained per cycle; rest next tick
        private volatile bool _isUploading = false;
        private readonly object _uploadLock = new object();

        public event EventHandler<string> LogMessage;

        public void SetProductCombinationsService(ProductCombinationsService service)
        {
            _productCombinationsService = service;
        }

        public ScanLogUploadService(ApiAuthService authService)
        {
            _authService = authService;

            _programDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ScanLink");
            try { Directory.CreateDirectory(_programDataDir); } catch { }
            _jsonlPath = Path.Combine(_programDataDir, "api_upload_logs.jsonl");
            _legacyJsonPath = Path.Combine(_programDataDir, "api_upload_logs.json");

            // One-time, crash-resumable migration of the old JSON-array queue into the new JSONL
            // queue. Runs in the constructor (app start) before the scanner script is launched.
            MigrateLegacyQueueIfNeeded();

            try
            {
                if (!File.Exists(_jsonlPath)) File.WriteAllText(_jsonlPath, string.Empty);
            }
            catch { }

            // Ensure modern TLS is enabled for HTTPS requests
            try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; } catch { }
        }

        public void Start()
        {
            _uploadTimer = new Timer(async _ => await TryUploadLogsAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(_uploadIntervalSeconds));
            OnLogMessage("Scan log upload service started");
        }

        public void Stop()
        {
            _uploadTimer?.Dispose();
            _uploadTimer = null;
            OnLogMessage("Scan log upload service stopped");
        }

        // ---- Concurrency-safe queue helpers ------------------------------------------------------
        // The scanner (PowerShell) appends to api_upload_logs.jsonl and this service reads/compacts
        // it, from separate processes. They coordinate through a named system mutex. Appends and
        // compaction are both done under the mutex so an append can never land in a file that is
        // mid-rewrite. Compaction writes atomically (temp file + atomic replace).
        private const string QueueMutexName = "Global\\ScanLinkUploadLogs";
        private static readonly TimeSpan QueueMutexTimeout = TimeSpan.FromSeconds(5);

        // Runs <action> while holding the cross-process mutex. Returns false if the lock could not
        // be acquired in time (caller should defer, never touch the file unguarded).
        private bool WithQueueLock(Action action)
        {
            using (var mtx = new Mutex(false, QueueMutexName))
            {
                bool acquired = false;
                try
                {
                    try { acquired = mtx.WaitOne(QueueMutexTimeout); }
                    catch (AbandonedMutexException) { acquired = true; } // prior owner died; we own it now
                    if (!acquired) return false;
                    action();
                    return true;
                }
                finally
                {
                    if (acquired) { try { mtx.ReleaseMutex(); } catch { } }
                }
            }
        }

        // Writes content via a temp file then an atomic replace, so a reader/appender can never
        // observe a half-written file. MUST be called while holding the queue mutex.
        private void AtomicWriteAllText(string path, string content)
        {
            string dir = Path.GetDirectoryName(path);
            string tmp = Path.Combine(dir, Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".tmp");
            File.WriteAllText(tmp, content, new UTF8Encoding(false)); // explicit UTF-8 no BOM, matches the PowerShell writer
            try
            {
                if (File.Exists(path)) File.Replace(tmp, path, null); // atomic on NTFS
                else File.Move(tmp, path);
            }
            catch
            {
                try { if (File.Exists(path)) File.Delete(path); File.Move(tmp, path); }
                finally { if (File.Exists(tmp)) { try { File.Delete(tmp); } catch { } } }
            }
        }

        // Reads the queue as a list of raw lines. MUST be called while holding the queue mutex.
        private List<string> ReadQueueLinesLocked()
        {
            if (!File.Exists(_jsonlPath)) return new List<string>();
            try { return new List<string>(File.ReadAllLines(_jsonlPath)); }
            catch { return new List<string>(); }
        }

        // Writes the queue from a list of raw lines, each newline-terminated so the next PowerShell
        // append starts on a fresh line (otherwise it would be concatenated onto the last record).
        // MUST be called while holding the queue mutex.
        private void WriteQueueLinesLocked(IList<string> lines)
        {
            string content = (lines == null || lines.Count == 0)
                ? string.Empty
                : string.Join("\n", lines) + "\n";
            AtomicWriteAllText(_jsonlPath, content);
        }

        // Parses one JSONL line into a record, or null if the line is blank/corrupt (a torn write).
        private Dictionary<string, object> ParseLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;
            try
            {
                return new JavaScriptSerializer { MaxJsonLength = int.MaxValue }
                    .Deserialize<Dictionary<string, object>>(line);
            }
            catch { return null; }
        }
        // ------------------------------------------------------------------------------------------

        // ---- One-time legacy migration -----------------------------------------------------------
        private void MigrateLegacyQueueIfNeeded()
        {
            try
            {
                WithQueueLock(() =>
                {
                    // Resume any migration interrupted by a previous crash (claimed temp files).
                    try
                    {
                        foreach (var tmp in Directory.GetFiles(_programDataDir, "api_upload_logs.json.migrating-*"))
                            MigrateOneLegacyFile(tmp);
                    }
                    catch { }

                    if (File.Exists(_legacyJsonPath))
                    {
                        // Claim the legacy file by renaming it first, so a crash mid-migration can
                        // be resumed (the data is never deleted until it's in the .jsonl).
                        string claimed = _legacyJsonPath + ".migrating-" + Guid.NewGuid().ToString("N");
                        bool moved = false;
                        try { File.Move(_legacyJsonPath, claimed); moved = true; } catch { }
                        if (moved) MigrateOneLegacyFile(claimed);
                    }
                });
            }
            catch (Exception ex)
            {
                OnLogMessage($"Queue migration error (non-fatal): {ex.Message}");
            }
        }

        private void MigrateOneLegacyFile(string path)
        {
            try
            {
                string content = File.ReadAllText(path);
                var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                List<Dictionary<string, object>> records;
                try
                {
                    records = serializer.Deserialize<List<Dictionary<string, object>>>(content)
                              ?? new List<Dictionary<string, object>>();
                }
                catch
                {
                    records = SalvageRecords(content, serializer); // tolerate a corrupt legacy file
                }

                if (records.Count > 0)
                {
                    var sb = new StringBuilder();
                    foreach (var r in records) sb.Append(serializer.Serialize(r)).Append("\n");
                    File.AppendAllText(_jsonlPath, sb.ToString()); // append, never overwrite
                    OnLogMessage($"Migrated {records.Count} queued scan(s) from {Path.GetFileName(path)} to api_upload_logs.jsonl");
                }

                // Keep the original as a backup (do not delete) once its records are safely in the queue.
                string done = path + ".migrated";
                try { if (File.Exists(done)) File.Delete(done); File.Move(path, done); } catch { }
            }
            catch (Exception ex)
            {
                OnLogMessage($"Could not migrate {Path.GetFileName(path)}: {ex.Message}");
            }
        }
        // ------------------------------------------------------------------------------------------

        // Auto upload cycle: drain the queue in batches. Called by the 30s timer.
        public async Task TryUploadLogsAsync()
        {
            if (_isUploading) return;
            lock (_uploadLock)
            {
                if (_isUploading) return;
                _isUploading = true;
            }

            try
            {
                if (!_authService.IsTokenValid())
                {
                    // Not logged in: skip without reading the queue (the scanner keeps appending O(1)).
                    return;
                }

                int uploaded = 0;
                string err = null;

                for (int batch = 0; batch < MaxBatchesPerCycle; batch++)
                {
                    // Snapshot the next batch of lines under the lock.
                    List<Dictionary<string, object>> records = null;
                    int lineCount = 0;
                    string sampleBadLine = null;
                    bool locked = WithQueueLock(() =>
                    {
                        var lines = ReadQueueLinesLocked();
                        if (lines.Count == 0) return;
                        lineCount = Math.Min(UploadBatchSize, lines.Count);
                        records = new List<Dictionary<string, object>>(lineCount);
                        for (int i = 0; i < lineCount; i++)
                        {
                            var rec = ParseLine(lines[i]);
                            if (rec != null) records.Add(rec); // unparseable lines are still counted, so they get dropped
                            else if (sampleBadLine == null && !string.IsNullOrWhiteSpace(lines[i]))
                                sampleBadLine = lines[i].Length > 200 ? lines[i].Substring(0, 200) : lines[i];
                        }
                    });
                    if (!locked) { err = "could not acquire the upload-queue lock"; break; }
                    if (lineCount == 0) break; // queue empty

                    if (records.Count > 0)
                    {
                        var enriched = EnrichLogsWithTokenData(records);
                        bool ok = await UploadLogsToApi(enriched);
                        if (!ok) { err = "upload failed/rejected by API"; break; } // leave queue intact, retry next cycle
                        uploaded += records.Count;
                    }
                    else
                    {
                        // A whole batch that won't parse is almost always a torn write (unrecoverable);
                        // dropping it unblocks the queue, but surface it so it can't go unnoticed.
                        OnLogMessage($"Discarding {lineCount} unparseable queue line(s).");
                        IssueLoggingService.LogIssue("ScanLink queue: unparseable lines discarded",
                            $"Discarded {lineCount} queue line(s) that could not be parsed as JSON (likely a torn write). Sample: {sampleBadLine ?? "(blank)"}");
                    }

                    // Drop the first lineCount lines (just uploaded and/or unparseable). Re-read so any
                    // lines the scanner appended during the POST are preserved.
                    bool compacted = WithQueueLock(() =>
                    {
                        var cur = ReadQueueLinesLocked();
                        int drop = Math.Min(lineCount, cur.Count);
                        WriteQueueLinesLocked(cur.Skip(drop).ToList());
                    });
                    if (!compacted) { err = "could not acquire the lock to compact queue"; break; }
                }

                if (uploaded > 0)
                {
                    int remaining = GetPendingCount();
                    OnLogMessage($"Uploaded {uploaded} scan(s){(remaining > 0 ? $"; {remaining} still queued" : "; queue empty")}.");
                }
                else if (err != null)
                {
                    OnLogMessage($"Upload cycle did not progress: {err}.");
                }
            }
            catch (Exception ex)
            {
                OnLogMessage($"Error uploading logs: {ex.Message}");
                IssueLoggingService.LogIssue("Upload Service Error", ex.ToString());
            }
            finally
            {
                _isUploading = false;
            }
        }

        // Manual upload (the "Sync logs to API" button): uploads one-by-one so a single bad record
        // can't block the others, and removes only the records that actually uploaded.
        public async Task<(int succeeded, int failed, string lastError)> UploadQueuedLogsManually()
        {
            if (_isUploading) return (0, 0, "An upload is already in progress; please wait.");
            lock (_uploadLock)
            {
                if (_isUploading) return (0, 0, "An upload is already in progress; please wait.");
                _isUploading = true;
            }

            try
            {
                if (!_authService.IsTokenValid())
                {
                    int pend = GetPendingCount();
                    return (0, pend < 0 ? 0 : pend, "Not authenticated");
                }

                int ok = 0, fail = 0;
                string lastErr = null;

                for (int chunk = 0; chunk < MaxBatchesPerCycle; chunk++)
                {
                    // Snapshot a chunk (index + parsed record) under the lock.
                    List<KeyValuePair<int, Dictionary<string, object>>> items = null;
                    int take = 0;
                    bool locked = WithQueueLock(() =>
                    {
                        var lines = ReadQueueLinesLocked();
                        if (lines.Count == 0) return;
                        take = Math.Min(UploadBatchSize, lines.Count);
                        items = new List<KeyValuePair<int, Dictionary<string, object>>>(take);
                        for (int i = 0; i < take; i++)
                            items.Add(new KeyValuePair<int, Dictionary<string, object>>(i, ParseLine(lines[i])));
                    });
                    if (!locked) { lastErr = "could not acquire the upload-queue lock"; break; }
                    if (take == 0) break; // queue empty

                    var resolvedIdx = new HashSet<int>(); // uploaded OR unparseable -> safe to remove
                    foreach (var item in items)
                    {
                        var rec = item.Value;
                        if (rec == null) { resolvedIdx.Add(item.Key); continue; } // drop torn/blank line
                        try
                        {
                            var enriched = EnrichLogsWithTokenData(new List<Dictionary<string, object>> { rec });
                            bool good = await UploadLogsToApi(enriched);
                            if (good) { ok++; resolvedIdx.Add(item.Key); }
                            else { fail++; lastErr = "API rejected/failed one or more records"; }
                        }
                        catch (Exception ex) { fail++; lastErr = ex.Message; }
                    }

                    // Remove resolved lines from this chunk; keep failures and anything appended during upload.
                    bool compacted = WithQueueLock(() =>
                    {
                        var cur = ReadQueueLinesLocked();
                        int t = Math.Min(take, cur.Count);
                        var keep = new List<string>();
                        for (int i = 0; i < t; i++) if (!resolvedIdx.Contains(i)) keep.Add(cur[i]);
                        for (int i = t; i < cur.Count; i++) keep.Add(cur[i]); // appended during upload
                        WriteQueueLinesLocked(keep);
                    });
                    if (!compacted) { lastErr = "could not acquire the lock to compact queue"; break; }

                    // No progress this chunk (every record failed) -> stop to avoid hammering a dead endpoint.
                    if (resolvedIdx.Count == 0) break;
                }

                if (ok == 0 && fail == 0 && lastErr == null) lastErr = "No logs to upload";

                if (ok > 0) OnLogMessage($"Manually uploaded {ok} log(s); {fail} failed and kept in queue.");
                else OnLogMessage($"No logs uploaded{(fail > 0 ? $"; {fail} failed" : "")}.");

                return (ok, fail, lastErr);
            }
            catch (Exception ex)
            {
                return (0, 0, ex.Message);
            }
            finally
            {
                _isUploading = false;
            }
        }

        // Enrichment now happens in memory at upload time (see EnrichLogsWithTokenData), so there is
        // nothing to persist here. Kept as a no-op so existing call sites are unaffected.
        public void EnrichLogsFileOnce()
        {
            // Intentionally a no-op: userId/siteId/cartonTypeId are stamped onto each record in
            // memory right before it is POSTed, never written back to the queue file.
        }

        // Number of scans still queued for upload (i.e. NOT yet confirmed uploaded). With the
        // append-only queue, uploaded records are physically removed, so this is just the line
        // count. Returns -1 if the queue could not be read/locked (caller treats that as "unknown",
        // i.e. NOT safe to assume everything is synced).
        public int GetPendingCount()
        {
            int count = -1;
            try
            {
                bool ok = WithQueueLock(() => { count = ReadQueueLinesLocked().Count; });
                if (!ok) return -1;
            }
            catch
            {
                return -1;
            }
            return count;
        }

        private List<Dictionary<string, object>> EnrichLogsWithTokenData(List<Dictionary<string, object>> logs)
        {
            var tokenPayload = _authService.GetCurrentTokenPayload();
            // Prefer explicit 'user' claim for userId, then 'userId', then 'sub'
            string userId = GetTokenValue(tokenPayload, "user")
                            ?? GetTokenValue(tokenPayload, "userId")
                            ?? GetTokenValue(tokenPayload, "user_id")
                            ?? GetTokenValue(tokenPayload, "id")
                            ?? GetTokenValue(tokenPayload, "sub")
                            ?? "";
            // Use robust fallback sequence for site extraction
            string siteId = _authService.GetEffectiveSiteId() ?? "";

            foreach (var log in logs)
            {
                // Add or update userId and siteId
                if (!log.ContainsKey("userId") || string.IsNullOrEmpty(log["userId"]?.ToString()))
                {
                    log["userId"] = userId;
                }
                if (!log.ContainsKey("siteId") || string.IsNullOrEmpty(log["siteId"]?.ToString()))
                {
                    log["siteId"] = siteId;
                }

                // Ensure all required fields are present
                if (!log.ContainsKey("scanStatus")) log["scanStatus"] = "SCANNED";
                if (!log.ContainsKey("errorMessage")) log["errorMessage"] = "";

                // Ensure cropId field exists (default empty string for now)
                if (!log.ContainsKey("cropId")) log["cropId"] = "";

                // Enrich cartonTypeId from product combinations if not already set
                if ((!log.ContainsKey("cartonTypeId") || string.IsNullOrEmpty(log["cartonTypeId"]?.ToString()))
                    && _productCombinationsService != null && _productCombinationsService.HasCachedData())
                {
                    var productId = log.ContainsKey("productId") ? log["productId"]?.ToString() : null;
                    var cropId = log.ContainsKey("cropId") ? log["cropId"]?.ToString() : null;
                    if (!string.IsNullOrEmpty(productId) && !string.IsNullOrEmpty(cropId))
                    {
                        var combo = _productCombinationsService.GetAllCombinations()
                            .FirstOrDefault(c => c.product_id == productId && c.crop_id == cropId);
                        if (combo != null && !string.IsNullOrEmpty(combo.carton_type_id))
                        {
                            log["cartonTypeId"] = combo.carton_type_id;
                        }
                    }
                }
            }

            return logs;
        }

        private string GetTokenValue(Dictionary<string, object> tokenPayload, string key)
        {
            if (tokenPayload != null && tokenPayload.ContainsKey(key))
            {
                var value = tokenPayload[key];
                if (value != null)
                {
                    return value.ToString();
                }
            }
            return null;
        }

        private async Task<bool> UploadLogsToApi(List<Dictionary<string, object>> logs)
        {
            try
            {
                string apiUrl = "https://backend-stage.labourlinksoftware.co.za/user/v1/scan-link/store-logs";
                string token = _authService.GetCurrentToken();

                if (string.IsNullOrEmpty(token))
                {
                    OnLogMessage("Cannot upload: no authentication token");
                    return false;
                }

                // Map to API schema (snake_case keys) without mutating file structure
                var apiReadyLogs = logs.Select(MapToApiSchema).ToList();

                // Serialize logs to JSON
                JavaScriptSerializer serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                string jsonPayload = serializer.Serialize(apiReadyLogs);

                // Create HTTP request
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    OnLogMessage($"Posting {apiReadyLogs.Count} log(s) to API");
                    var response = await client.PostAsync(apiUrl, content);

                    string responseBody = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        // Some APIs return 200 with per-item failures; require totalSuccessful == count
                        try
                        {
                            var resp = serializer.Deserialize<Dictionary<string, object>>(responseBody) ?? new Dictionary<string, object>();
                            int totalSuccessful = 0;
                            if (resp.ContainsKey("totalSuccessful"))
                            {
                                int.TryParse(resp["totalSuccessful"]?.ToString(), out totalSuccessful);
                            }
                            // A 2xx WITHOUT a totalSuccessful field means the API doesn't report
                            // per-item counts; treat the success status as authoritative (same as
                            // the unparseable-body branch below). Only a present-but-short count is a
                            // real partial failure. Without this, a 200 lacking the key would loop
                            // forever: never compacted, re-POSTing the same batch every cycle.
                            if (!resp.ContainsKey("totalSuccessful") || totalSuccessful >= apiReadyLogs.Count)
                            {
                                OnLogMessage($"Logs uploaded successfully: {responseBody}");
                                return true;
                            }
                            else
                            {
                                OnLogMessage($"Partial/failed upload reported by API: {responseBody}");
                                return false;
                            }
                        }
                        catch
                        {
                            // If cannot parse, assume success
                            OnLogMessage($"Logs uploaded successfully (unparsed response): {responseBody}");
                            return true;
                        }
                    }
                    else
                    {
                        OnLogMessage($"Failed to upload logs: {response.StatusCode} - {responseBody}");
                        if (response.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            OnLogMessage("Authentication failed (401). Please login again.");
                        }
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? $" | Inner: {ex.InnerException.Message}" : string.Empty;
                OnLogMessage($"Error during API upload: {ex.Message}{inner}");
                IssueLoggingService.LogIssue("Upload API Error", $"{ex.Message}{inner}\n\nStack:\n{ex.StackTrace}");
                return false;
            }
        }

        private void OnLogMessage(string message)
        {
            LogMessage?.Invoke(this, $"[ScanLogUpload] {message}");
        }

        private Dictionary<string, object> MapToApiSchema(Dictionary<string, object> log)
        {
            string GetString(string key)
            {
                return log.ContainsKey(key) && log[key] != null ? log[key].ToString() : null;
            }

            string ts = GetString("timestamp");
            string formattedTs = ts;
            try
            {
                if (!string.IsNullOrWhiteSpace(ts))
                {
                    // If ISO/offset provided, normalize to "yyyy-MM-dd HH:mm:ss" in UTC; otherwise leave as-is
                    bool looksIsoOrOffset = ts.Contains("T") || ts.EndsWith("Z", StringComparison.OrdinalIgnoreCase) || ts.Contains("+") || ts.LastIndexOf('-') > 10;
                    if (looksIsoOrOffset)
                    {
                        if (DateTimeOffset.TryParse(ts, out var dto))
                        {
                            formattedTs = dto.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                    }
                }
            }
            catch { }

            var userId = GetString("userId");
            var siteId = GetString("siteId");

            var mapped = new Dictionary<string, object>
            {
                // API expects camelCase field names
                { "userId", userId },
                { "siteId", siteId },
                { "timestamp", formattedTs },
                { "lineNumber", GetString("lineNumber") },
                { "blockNumber", GetString("blockNumber") },
                { "supplier", GetString("supplier") },
                { "productId", GetString("productId") },
                { "parsedInfo", GetString("parsedInfo") },
                { "scanStatus", GetString("scanStatus") ?? "SCANNED" },
                { "cropId", GetString("cropId") },
                { "cartonTypeId", GetString("cartonTypeId") }
            };

            // Remove nulls to avoid API validation issues
            var keysToRemove = mapped.Where(kv => kv.Value == null).Select(kv => kv.Key).ToList();
            foreach (var k in keysToRemove) mapped.Remove(k);

            return mapped;
        }

        // Brace/string-aware extraction of every top-level {...} object that still parses. Used only
        // when migrating a corrupt legacy api_upload_logs.json so we salvage what we can.
        private List<Dictionary<string, object>> SalvageRecords(string raw, JavaScriptSerializer serializer)
        {
            var result = new List<Dictionary<string, object>>();
            if (string.IsNullOrEmpty(raw)) return result;
            int depth = 0, start = -1;
            bool inStr = false, esc = false;
            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];
                if (inStr)
                {
                    if (esc) esc = false;
                    else if (c == '\\') esc = true;
                    else if (c == '"') inStr = false;
                    continue;
                }
                if (c == '"') { inStr = true; continue; }
                if (c == '{') { if (depth == 0) start = i; depth++; }
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0 && start >= 0)
                    {
                        string obj = raw.Substring(start, i - start + 1);
                        try { var d = serializer.Deserialize<Dictionary<string, object>>(obj); if (d != null) result.Add(d); }
                        catch { }
                        start = -1;
                    }
                    else if (depth < 0) depth = 0;
                }
            }
            return result;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
