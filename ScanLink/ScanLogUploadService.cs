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
    public class ScanLogUploadService : IDisposable
    {
        private readonly string _apiLogsFilePath;
        private readonly ApiAuthService _authService;
        private ProductCombinationsService _productCombinationsService;
        private Timer _uploadTimer;
        private readonly int _uploadIntervalSeconds = 30; // Check every 30 seconds
        private bool _isUploading = false;
        private readonly object _uploadLock = new object();

        public event EventHandler<string> LogMessage;

        public void SetProductCombinationsService(ProductCombinationsService service)
        {
            _productCombinationsService = service;
        }

        public ScanLogUploadService(ApiAuthService authService)
        {
            _authService = authService;
            
            // Always use ProgramData for the API upload queue file
            string programDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ScanLink");
            try { Directory.CreateDirectory(programDataDir); } catch {}
            _apiLogsFilePath = Path.Combine(programDataDir, "api_upload_logs.json");
            try
            {
                if (!File.Exists(_apiLogsFilePath))
                {
                    File.WriteAllText(_apiLogsFilePath, "[]");
                }
            }
            catch {}

            // Ensure modern TLS is enabled for HTTPS requests
            try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; } catch {}
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

        // ---- Concurrency-safe queue file helpers -------------------------------------------------
        // The scanner (PowerShell scan_capture.ps1) and this service both read/write
        // api_upload_logs.json. They live in separate processes, so an in-process lock would not be
        // enough — they coordinate through a named system mutex. Every write is atomic (temp file +
        // atomic replace) and every read is self-healing (salvage + quarantine) so a single corrupt
        // file can never again silently wedge uploads.
        private const string QueueMutexName = "Global\\ScanLinkUploadLogs";
        private static readonly TimeSpan QueueMutexTimeout = TimeSpan.FromSeconds(5);

        // Runs <action> while holding the cross-process mutex. Returns false if the lock could not
        // be acquired in time (caller should defer, never write unguarded).
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

        // Writes content via a temp file then an atomic replace, so a reader can never observe a
        // half-written file. MUST be called while holding the queue mutex.
        private void AtomicWriteAllText(string path, string content)
        {
            string dir = Path.GetDirectoryName(path);
            string tmp = Path.Combine(dir, Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".tmp");
            File.WriteAllText(tmp, content); // UTF-8 no BOM, matches the PowerShell writer
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

        // Reads the queue tolerantly. On a parse failure it salvages every well-formed record,
        // quarantines the raw corrupt file for forensics, and rewrites a clean queue from the
        // salvaged records — so the service self-heals instead of throwing every cycle forever.
        // MUST be called while holding the queue mutex.
        private List<Dictionary<string, object>> ReadQueueResilient()
        {
            if (!File.Exists(_apiLogsFilePath)) return new List<Dictionary<string, object>>();
            string content = File.ReadAllText(_apiLogsFilePath);
            if (string.IsNullOrWhiteSpace(content)) return new List<Dictionary<string, object>>();

            JavaScriptSerializer serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            try
            {
                var logs = serializer.Deserialize<List<Dictionary<string, object>>>(content);
                if (logs != null) return logs;
                var single = serializer.Deserialize<Dictionary<string, object>>(content);
                if (single != null) return new List<Dictionary<string, object>> { single };
                return new List<Dictionary<string, object>>();
            }
            catch (Exception ex)
            {
                var salvaged = SalvageRecords(content, serializer);
                QuarantineCorruptFile(content, ex, salvaged.Count);
                AtomicWriteAllText(_apiLogsFilePath, serializer.Serialize(salvaged));
                OnLogMessage($"Recovered {salvaged.Count} record(s) from a corrupt queue file; quarantined the bad copy.");
                return salvaged;
            }
        }

        // Brace/string-aware extraction of every top-level {...} object that still parses.
        private List<Dictionary<string, object>> SalvageRecords(string raw, JavaScriptSerializer serializer)
        {
            var result = new List<Dictionary<string, object>>();
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

        private void QuarantineCorruptFile(string content, Exception ex, int salvagedCount)
        {
            try
            {
                string dir = Path.GetDirectoryName(_apiLogsFilePath);
                string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
                string qpath = Path.Combine(dir, $"api_upload_logs.corrupt-{stamp}.json");
                File.WriteAllText(qpath, content);
                IssueLoggingService.LogIssue("ScanLink queue auto-recovered",
                    $"Corrupt api_upload_logs.json detected and quarantined to {Path.GetFileName(qpath)}. " +
                    $"Salvaged {salvagedCount} record(s). Error: {ex.Message}");
            }
            catch { }
        }
        // -----------------------------------------------------------------------------------------

        public async Task TryUploadLogsAsync()
        {
            // Prevent concurrent uploads
            if (_isUploading)
                return;

            lock (_uploadLock)
            {
                if (_isUploading)
                    return;
                _isUploading = true;
            }

            try
            {
                // Phase 1 (under the cross-process lock): read the queue resiliently, enrich it, and
                // persist the enriched copy atomically. The lock is held only for these fast local
                // ops — never during the network upload below.
                List<Dictionary<string, object>> enrichedLogs = null;
                bool prepared = WithQueueLock(() =>
                {
                    var logs = ReadQueueResilient();
                    if (logs == null || logs.Count == 0) return;
                    enrichedLogs = EnrichLogsWithTokenData(logs);
                    AtomicWriteAllText(_apiLogsFilePath,
                        new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(enrichedLogs));
                });

                if (!prepared)
                {
                    OnLogMessage("Skipping this cycle: could not acquire the upload-queue lock");
                    return;
                }
                if (enrichedLogs == null || enrichedLogs.Count == 0)
                {
                    return;
                }

                OnLogMessage($"Found {enrichedLogs.Count} log(s) to upload");

                // Check if authenticated before attempting upload
                if (!_authService.IsTokenValid())
                {
                    OnLogMessage("Skipping log upload: not authenticated");
                    return;
                }

                // Phase 2 (no lock held): upload to API.
                bool success = await UploadLogsToApi(enrichedLogs);

                if (success)
                {
                    // Phase 3 (under the lock again): remove only the logs we just uploaded, keeping
                    // any new logs the scanner added during the API call.
                    var uploadedIds = enrichedLogs
                        .Where(l => l.ContainsKey("_uploadId"))
                        .Select(l => l["_uploadId"].ToString())
                        .ToList();
                    SafelyRemoveUploadedLogs(uploadedIds);
                    OnLogMessage($"Successfully uploaded and removed {enrichedLogs.Count} log(s) from queue.");
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

            OnLogMessage($"Enriching logs with userId='{(string.IsNullOrEmpty(userId) ? "" : userId)}' siteId='{(string.IsNullOrEmpty(siteId) ? "" : siteId)}'");

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

                // Generate a unique transaction ID for robust tracking in the queue file
                // This prevents deleting new logs that PS1 adds during an active upload
                if (!log.ContainsKey("_uploadId"))
                {
                    log["_uploadId"] = Guid.NewGuid().ToString();
                }
            }

            return logs;
        }

        private void SafelyRemoveUploadedLogs(List<string> successfulUploadIds)
        {
            if (successfulUploadIds == null || successfulUploadIds.Count == 0) return;
            
            try
            {
                WithQueueLock(() =>
                {
                    var logs = ReadQueueResilient();
                    if (logs == null || logs.Count == 0) return;

                    var idsToRemove = new HashSet<string>(successfulUploadIds);
                    // Keep logs that don't have an ID yet, or aren't in the successful list
                    var remainingLogs = logs.Where(l =>
                        !l.ContainsKey("_uploadId") ||
                        !idsToRemove.Contains(l["_uploadId"]?.ToString())
                    ).ToList();

                    AtomicWriteAllText(_apiLogsFilePath,
                        new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(remainingLogs));
                });
            }
            catch (Exception ex)
            {
                OnLogMessage($"Failed to clean up queue: {ex.Message}");
            }
        }

        // Public method to enrich and persist without attempting upload
        public void EnrichLogsFileOnce()
        {
            try
            {
                WithQueueLock(() =>
                {
                    var logs = ReadQueueResilient();
                    if (logs == null || logs.Count == 0) return;

                    var enriched = EnrichLogsWithTokenData(logs);
                    AtomicWriteAllText(_apiLogsFilePath,
                        new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(enriched));
                    OnLogMessage($"Enriched and persisted {enriched.Count} log(s) to file without upload");
                });
            }
            catch (Exception ex)
            {
                OnLogMessage($"Error enriching logs file: {ex.Message}");
            }
        }

        // Number of scans still queued for upload (i.e. NOT yet confirmed in the database).
        // Reads under the cross-process lock with the self-healing reader so the count is accurate.
        // Returns -1 if the queue could not be read/locked (caller should treat that as "unknown",
        // i.e. NOT safe to assume everything is synced).
        public int GetPendingCount()
        {
            int count = -1;
            try
            {
                bool ok = WithQueueLock(() => { count = ReadQueueResilient().Count; });
                if (!ok) return -1;
            }
            catch
            {
                return -1;
            }
            return count;
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
                            if (totalSuccessful >= apiReadyLogs.Count)
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

        // Manual upload: uploads entries one-by-one and removes only successful ones
        public async Task<(int succeeded, int failed, string lastError)> UploadQueuedLogsManually()
        {
            try
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

                // Read + enrich + persist atomically under the cross-process lock.
                List<Dictionary<string, object>> enriched = null;
                bool prepared = WithQueueLock(() =>
                {
                    var logs = ReadQueueResilient();
                    if (logs == null || logs.Count == 0) return;
                    enriched = EnrichLogsWithTokenData(logs);
                    AtomicWriteAllText(_apiLogsFilePath, serializer.Serialize(enriched));
                });

                if (!prepared)
                {
                    return (0, 0, "Could not acquire the upload-queue lock");
                }
                if (enriched == null || enriched.Count == 0)
                {
                    return (0, 0, "No logs to upload");
                }

                if (!_authService.IsTokenValid())
                {
                    return (0, enriched.Count, "Not authenticated");
                }

                string apiUrl = "https://backend-stage.labourlinksoftware.co.za/user/v1/scan-link/store-logs";
                string token = _authService.GetCurrentToken();
                if (string.IsNullOrEmpty(token))
                {
                    return (0, enriched.Count, "Missing token");
                }

                int ok = 0, bad = 0;
                string lastErr = null;
                var successfulIds = new List<string>();

                using (var client = new System.Net.Http.HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                    foreach (var entry in enriched)
                    {
                        try
                        {
                    var apiEntry = MapToApiSchema(entry);
                            string payload = serializer.Serialize(new List<Dictionary<string, object>> { apiEntry });
                            var content = new System.Net.Http.StringContent(payload, Encoding.UTF8, "application/json");
                            var response = await client.PostAsync(apiUrl, content);
                            if (response.IsSuccessStatusCode)
                            {
                                ok++;
                                successfulIds.Add(entry.ContainsKey("_uploadId") ? entry["_uploadId"].ToString() : "");
                            }
                            else
                            {
                                bad++;
                                lastErr = $"{(int)response.StatusCode} {response.StatusCode}: " + await response.Content.ReadAsStringAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            bad++;
                            lastErr = ex.Message;
                        }
                    }
                }

                // Remove securely
                SafelyRemoveUploadedLogs(successfulIds.Where(id => !string.IsNullOrEmpty(id)).ToList());

                if (ok > 0)
                {
                    OnLogMessage($"Manually uploaded {ok} log(s); {bad} failed and kept in queue");
                }
                else
                {
                    OnLogMessage($"No logs uploaded; {bad} failed");
                }

                return (ok, bad, lastErr);
            }
            catch (Exception ex)
            {
                return (0, 0, ex.Message);
            }
        }

        private async Task<bool> IsInternetAvailable()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    var response = await client.GetAsync("https://www.google.com");
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
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

        public void Dispose()
        {
            Stop();
        }
    }
}

