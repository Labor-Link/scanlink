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
        private Timer _uploadTimer;
        private readonly int _uploadIntervalSeconds = 30; // Check every 30 seconds
        private bool _isUploading = false;
        private readonly object _uploadLock = new object();

        public event EventHandler<string> LogMessage;

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
            // Start the timer to periodically check and upload logs
            _uploadTimer = new Timer(async _ => await TryUploadLogs(), null, TimeSpan.Zero, TimeSpan.FromSeconds(_uploadIntervalSeconds));
            OnLogMessage("Scan log upload service started");
        }

        public void Stop()
        {
            _uploadTimer?.Dispose();
            _uploadTimer = null;
            OnLogMessage("Scan log upload service stopped");
        }

        private async Task TryUploadLogs()
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
                // Check if log file exists and has content
                if (!File.Exists(_apiLogsFilePath))
                {
                    return;
                }

                string fileContent = File.ReadAllText(_apiLogsFilePath);
                if (string.IsNullOrWhiteSpace(fileContent) || fileContent.Trim() == "[]")
                {
                    return;
                }

                // Parse the logs (support array or single object)
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                var logs = serializer.Deserialize<List<Dictionary<string, object>>>(fileContent);
                if (logs == null)
                {
                    var single = serializer.Deserialize<Dictionary<string, object>>(fileContent);
                    if (single != null)
                    {
                        logs = new List<Dictionary<string, object>> { single };
                    }
                }

                if (logs == null || logs.Count == 0)
                {
                    return;
                }

                OnLogMessage($"Found {logs.Count} log(s) to upload");

                // Enrich logs with userId and siteId from token (always), and write back for visibility
                var enrichedLogs = EnrichLogsWithTokenData(logs);
                try
                {
                    JavaScriptSerializer writerSerializer = new JavaScriptSerializer();
                    string enrichedJson = writerSerializer.Serialize(enrichedLogs);
                    File.WriteAllText(_apiLogsFilePath, enrichedJson);
                }
                catch { }

                // Check if authenticated before attempting upload
                if (!_authService.IsTokenValid())
                {
                    OnLogMessage("Skipping log upload: not authenticated");
                    return;
                }

                // Upload to API
                bool success = await UploadLogsToApi(enrichedLogs);

                if (success)
                {
                    // Clear the uploaded logs from the file
                    File.WriteAllText(_apiLogsFilePath, "[]");
                    OnLogMessage($"Successfully uploaded and cleared {enrichedLogs.Count} log(s)");
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
            }

            return logs;
        }

        // Public method to enrich and persist without attempting upload
        public void EnrichLogsFileOnce()
        {
            try
            {
                if (!File.Exists(_apiLogsFilePath)) return;
                string fileContent = File.ReadAllText(_apiLogsFilePath);
                if (string.IsNullOrWhiteSpace(fileContent)) return;

                JavaScriptSerializer serializer = new JavaScriptSerializer();
                var logs = serializer.Deserialize<List<Dictionary<string, object>>>(fileContent);
                if (logs == null)
                {
                    var single = serializer.Deserialize<Dictionary<string, object>>(fileContent);
                    if (single != null) logs = new List<Dictionary<string, object>> { single };
                }
                if (logs == null || logs.Count == 0) return;

                var enriched = EnrichLogsWithTokenData(logs);
                File.WriteAllText(_apiLogsFilePath, serializer.Serialize(enriched));
                OnLogMessage($"Enriched and persisted {enriched.Count} log(s) to file without upload");
            }
            catch (Exception ex)
            {
                OnLogMessage($"Error enriching logs file: {ex.Message}");
            }
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
                JavaScriptSerializer serializer = new JavaScriptSerializer();
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
                // Always enrich and persist so file shows userId/siteId
                if (!File.Exists(_apiLogsFilePath))
                {
                    return (0, 0, "Queue file not found");
                }

                string fileContent = File.ReadAllText(_apiLogsFilePath);
                if (string.IsNullOrWhiteSpace(fileContent) || fileContent.Trim() == "[]")
                {
                    return (0, 0, "No logs to upload");
                }

                JavaScriptSerializer serializer = new JavaScriptSerializer();
                var logs = serializer.Deserialize<List<Dictionary<string, object>>>(fileContent);
                if (logs == null)
                {
                    var single = serializer.Deserialize<Dictionary<string, object>>(fileContent);
                    if (single != null) logs = new List<Dictionary<string, object>> { single };
                }
                if (logs == null) logs = new List<Dictionary<string, object>>();
                if (logs.Count == 0)
                {
                    return (0, 0, "No logs to upload");
                }

                // Enrich and persist
                var enriched = EnrichLogsWithTokenData(logs);
                try
                {
                    File.WriteAllText(_apiLogsFilePath, serializer.Serialize(enriched));
                }
                catch { }

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
                var remaining = new List<Dictionary<string, object>>();

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
                            }
                            else
                            {
                                bad++;
                                remaining.Add(entry);
                                lastErr = $"{(int)response.StatusCode} {response.StatusCode}: " + await response.Content.ReadAsStringAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            bad++;
                            remaining.Add(entry);
                            lastErr = ex.Message;
                        }
                    }
                }

                // Persist only remaining (failed) entries
                try
                {
                    File.WriteAllText(_apiLogsFilePath, serializer.Serialize(remaining));
                }
                catch { }

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
                { "productId", GetString("productId") },
                { "parsedInfo", GetString("parsedInfo") },
                { "scanStatus", GetString("scanStatus") ?? "SCANNED" },
                { "cropId", GetString("cropId") }
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

