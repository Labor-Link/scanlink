using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace ScanLink
{
    public class ApiAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly JavaScriptSerializer _jsonSerializer;
        private string _currentToken;
        private DateTime _tokenExpiry;
        private string _selectedSiteId;
        private string _selectedSiteName;

        public ApiAuthService()
        {
            _httpClient = new HttpClient();
            _jsonSerializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            
            // Set default headers to match the curl request
            _httpClient.DefaultRequestHeaders.Add("accept", "application/json, text/plain, */*");
            _httpClient.DefaultRequestHeaders.Add("accept-language", "en-GB,en-US;q=0.9,en;q=0.8");
            _httpClient.DefaultRequestHeaders.Add("cache-control", "no-cache");
            _httpClient.DefaultRequestHeaders.Add("pragma", "no-cache");
            _httpClient.DefaultRequestHeaders.Add("priority", "u=1, i");
            _httpClient.DefaultRequestHeaders.Add("referer", "https://hr.labourlinksoftware.co.za/");
            _httpClient.DefaultRequestHeaders.Add("sec-ch-ua", "\"Chromium\";v=\"140\", \"Not=A?Brand\";v=\"24\", \"Google Chrome\";v=\"140\"");
            _httpClient.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?1");
            _httpClient.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"Android\"");
            _httpClient.DefaultRequestHeaders.Add("sec-fetch-dest", "empty");
            _httpClient.DefaultRequestHeaders.Add("sec-fetch-mode", "cors");
            _httpClient.DefaultRequestHeaders.Add("sec-fetch-site", "same-site");
            _httpClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Mobile Safari/537.36");
        }

        /// <summary>
        /// Fetches all departments for the current user's site
        /// </summary>
        /// <returns>API response containing a map of departmentId -> departmentName</returns>
        public async Task<ApiResponse<Dictionary<string, string>>> GetDepartmentsAsync()
        {
            if (!IsTokenValid())
            {
                return new ApiResponse<Dictionary<string, string>>
                {
                    Success = false,
                    ErrorMessage = "No valid authentication token available",
                    StatusCode = 401
                };
            }

            try
            {
                var url = "https://backend-stage.labourlinksoftware.co.za/user/v1/site/department/all";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _currentToken);
                request.Headers.Add("Accept", "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                System.Diagnostics.Debug.WriteLine($"Departments API Response:");
                System.Diagnostics.Debug.WriteLine($"Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    Dictionary<string, string> departments = null;
                    try
                    {
                        departments = _jsonSerializer.Deserialize<Dictionary<string, string>>(responseContent);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to deserialize departments: {ex.Message}\nResponse: {responseContent}");
                        departments = new Dictionary<string, string>();
                    }

                    return new ApiResponse<Dictionary<string, string>>
                    {
                        Success = true,
                        Data = departments,
                        StatusCode = (int)response.StatusCode
                    };
                }

                return new ApiResponse<Dictionary<string, string>>
                {
                    Success = false,
                    ErrorMessage = $"Failed to fetch departments: {response.StatusCode} - {responseContent}",
                    StatusCode = (int)response.StatusCode
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<Dictionary<string, string>>
                {
                    Success = false,
                    ErrorMessage = $"Exception during departments fetch: {ex.Message}",
                    StatusCode = 0
                };
            }
        }

        public class LoginRequest
        {
            public string username { get; set; }
            public string password { get; set; }
        }

        public class LoginResponse
        {
            public string accessToken { get; set; }
            public string token_type { get; set; }
            public int expires_in { get; set; }
            public string refresh_token { get; set; }
            public string scope { get; set; }
        }

        public class ApiResponse<T>
        {
            public bool Success { get; set; }
            public T Data { get; set; }
            public string ErrorMessage { get; set; }
            public int StatusCode { get; set; }
        }

        /// <summary>
        /// Authenticates with the API and retrieves an access token
        /// </summary>
        /// <param name="username">The username/email for authentication</param>
        /// <param name="password">The password for authentication</param>
        /// <returns>API response containing the authentication result</returns>
        public async Task<ApiResponse<LoginResponse>> LoginAsync(string username, string password)
        {
            try
            {
                var loginRequest = new LoginRequest
                {
                    username = username,
                    password = password
                };

                var json = _jsonSerializer.Serialize(loginRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://backend-stage.labourlinksoftware.co.za/auth/v1/login", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = _jsonSerializer.Deserialize<LoginResponse>(responseContent);
                    
                    // Store the token and calculate expiry
                    _currentToken = loginResponse.accessToken;
                    
                    // Debug the expires_in value
                    System.Diagnostics.Debug.WriteLine($"Token expires_in value: {loginResponse.expires_in}");
                    
                    // Calculate expiry - if expires_in is very large, it might be a Unix timestamp
                    if (loginResponse.expires_in > 1000000000) // Likely a Unix timestamp
                    {
                        // Convert Unix timestamp to DateTime
                        _tokenExpiry = DateTimeOffset.FromUnixTimeSeconds(loginResponse.expires_in).DateTime;
                        System.Diagnostics.Debug.WriteLine($"Using Unix timestamp expiry: {_tokenExpiry}");
                    }
                    else
                    {
                        // Use as seconds from now
                        _tokenExpiry = DateTime.Now.AddSeconds(loginResponse.expires_in - 300); // Refresh 5 minutes before expiry
                        System.Diagnostics.Debug.WriteLine($"Using relative expiry: {_tokenExpiry}");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Login successful - Token stored: {(_currentToken != null ? "Present" : "Null")}, Expiry: {_tokenExpiry}");
                    
                    return new ApiResponse<LoginResponse>
                    {
                        Success = true,
                        Data = loginResponse,
                        StatusCode = (int)response.StatusCode
                    };
                }
                else
                {
                    return new ApiResponse<LoginResponse>
                    {
                        Success = false,
                        ErrorMessage = $"Login failed: {response.StatusCode} - {responseContent}",
                        StatusCode = (int)response.StatusCode
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<LoginResponse>
                {
                    Success = false,
                    ErrorMessage = $"Exception during login: {ex.Message}",
                    StatusCode = 0
                };
            }
        }

        /// <summary>
        /// Decodes JWT token and extracts payload information
        /// </summary>
        /// <param name="token">The JWT access token</param>
        /// <returns>Dictionary containing token payload information</returns>
        public Dictionary<string, object> DecodeJwtToken(string token)
        {
            var payloadInfo = new Dictionary<string, object>();
            
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    payloadInfo["Error"] = "Token is null or empty";
                    return payloadInfo;
                }

                // JWT tokens have 3 parts separated by dots: header.payload.signature
                var parts = token.Split('.');
                if (parts.Length != 3)
                {
                    payloadInfo["Error"] = "Invalid JWT token format";
                    return payloadInfo;
                }

                // Decode the payload (second part)
                var payload = parts[1];
                
                // Add padding if needed for Base64 decoding
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }

                // Decode Base64
                var payloadBytes = Convert.FromBase64String(payload);
                var payloadJson = Encoding.UTF8.GetString(payloadBytes);
                
                // Parse JSON payload
                var payloadData = _jsonSerializer.Deserialize<Dictionary<string, object>>(payloadJson);
                
                // Extract common JWT claims
                if (payloadData != null)
                {
                    foreach (var kvp in payloadData)
                    {
                        payloadInfo[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                payloadInfo["Error"] = $"Failed to decode token: {ex.Message}";
            }

            return payloadInfo;
        }

        /// <summary>
        /// Gets the current access token if it's still valid
        /// </summary>
        /// <returns>The access token or null if expired/missing</returns>
        public string GetCurrentToken()
        {
            if (!string.IsNullOrEmpty(_currentToken) && DateTime.Now < _tokenExpiry)
            {
                return _currentToken;
            }
            return null;
        }

        /// <summary>
        /// Checks if the current token is valid and not expired
        /// </summary>
        /// <returns>True if token is valid, false otherwise</returns>
        public bool IsTokenValid()
        {
            var isValid = !string.IsNullOrEmpty(_currentToken) && DateTime.Now < _tokenExpiry;
            System.Diagnostics.Debug.WriteLine($"Token validation: Token={(_currentToken != null ? "Present" : "Null")}, Expiry={_tokenExpiry}, Now={DateTime.Now}, Valid={isValid}");
            return isValid;
        }

        /// <summary>
        /// Clears the current token (useful for logout)
        /// </summary>
        public void ClearToken()
        {
            _currentToken = null;
            _tokenExpiry = DateTime.MinValue;
            _selectedSiteId = null;
            _selectedSiteName = null;
            ClearTokenFile();
        }

        /// <summary>
        /// Sets the selected site for the current session
        /// </summary>
        /// <param name="siteId">The selected site ID</param>
        /// <param name="siteName">The selected site name</param>
        public void SetSelectedSite(string siteId, string siteName)
        {
            _selectedSiteId = siteId;
            _selectedSiteName = siteName;
            System.Diagnostics.Debug.WriteLine($"Selected site set: ID={siteId}, Name={siteName}");
            SaveTokenToFile();
        }

        #region Token Persistence

        private string GetTokenFilePath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = System.IO.Path.Combine(appDataPath, "ScanLink");
            if (!System.IO.Directory.Exists(appFolder))
            {
                System.IO.Directory.CreateDirectory(appFolder);
            }
            return System.IO.Path.Combine(appFolder, "auth_token.dat");
        }

        public void SaveTokenToFile()
        {
            try
            {
                if (string.IsNullOrEmpty(_currentToken)) return;

                string filePath = GetTokenFilePath();
                var lines = new[]
                {
                    _currentToken,
                    _tokenExpiry.ToString("o"),
                    _selectedSiteId ?? "",
                    _selectedSiteName ?? ""
                };
                System.IO.File.WriteAllLines(filePath, lines);
                System.Diagnostics.Debug.WriteLine("Token saved to file");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save token: {ex.Message}");
            }
        }

        public bool LoadTokenFromFile()
        {
            try
            {
                string filePath = GetTokenFilePath();
                if (!System.IO.File.Exists(filePath)) return false;

                var lines = System.IO.File.ReadAllLines(filePath);
                if (lines.Length < 2) return false;

                string token = lines[0];
                DateTime expiry = DateTime.Parse(lines[1], null, System.Globalization.DateTimeStyles.RoundtripKind);

                if (string.IsNullOrEmpty(token) || DateTime.Now >= expiry)
                {
                    ClearTokenFile();
                    return false;
                }

                _currentToken = token;
                _tokenExpiry = expiry;
                if (lines.Length > 2) _selectedSiteId = string.IsNullOrEmpty(lines[2]) ? null : lines[2];
                if (lines.Length > 3) _selectedSiteName = string.IsNullOrEmpty(lines[3]) ? null : lines[3];

                System.Diagnostics.Debug.WriteLine($"Token loaded from file. Expires: {expiry}, Site: {_selectedSiteName}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load token: {ex.Message}");
                return false;
            }
        }

        private void ClearTokenFile()
        {
            try
            {
                string filePath = GetTokenFilePath();
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    System.Diagnostics.Debug.WriteLine("Token file cleared");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to clear token file: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// Gets the currently selected site ID
        /// </summary>
        /// <returns>The selected site ID or null if not set</returns>
        public string GetSelectedSiteId()
        {
            return _selectedSiteId;
        }

        /// <summary>
        /// Gets the effective, robust site ID across dropdown, active_site, and raw payloads
        /// </summary>
        public string GetEffectiveSiteId()
        {
            string id = GetSelectedSiteId();
            if (!string.IsNullOrEmpty(id)) return id;

            id = GetActiveSiteId();
            if (!string.IsNullOrEmpty(id)) return id;

            var payload = GetCurrentTokenPayload();
            if (payload != null)
            {
                var allSites = GetAllSiteIdsFromPayload(payload);
                if (allSites != null && allSites.Count > 0)
                {
                    return allSites[0].Id;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the currently selected site name
        /// </summary>
        /// <returns>The selected site name or null if not set</returns>
        public string GetSelectedSiteName()
        {
            return _selectedSiteName;
        }

        /// <summary>
        /// Makes an authenticated API request using the current token
        /// </summary>
        /// <param name="url">The API endpoint URL</param>
        /// <param name="method">HTTP method (GET, POST, etc.)</param>
        /// <param name="content">Request content (for POST/PUT requests)</param>
        /// <returns>API response</returns>
        public async Task<ApiResponse<string>> MakeAuthenticatedRequestAsync(string url, HttpMethod method, string content = null)
        {
            if (!IsTokenValid())
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    ErrorMessage = "No valid authentication token available",
                    StatusCode = 401
                };
            }

            try
            {
                var request = new HttpRequestMessage(method, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _currentToken);

                if (!string.IsNullOrEmpty(content))
                {
                    request.Content = new StringContent(content, Encoding.UTF8, "application/json");
                }

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                return new ApiResponse<string>
                {
                    Success = response.IsSuccessStatusCode,
                    Data = responseContent,
                    ErrorMessage = response.IsSuccessStatusCode ? null : $"Request failed: {response.StatusCode} - {responseContent}",
                    StatusCode = (int)response.StatusCode
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    ErrorMessage = $"Exception during authenticated request: {ex.Message}",
                    StatusCode = 0
                };
            }
        }

        /// <summary>
        /// Gets the decoded payload of the current token
        /// </summary>
        /// <returns>Dictionary containing token payload information or null if no valid token</returns>
        public Dictionary<string, object> GetCurrentTokenPayload()
        {
            var token = GetCurrentToken();
            if (string.IsNullOrEmpty(token))
                return null;
            
            return DecodeJwtToken(token);
        }

        /// <summary>
        /// Gets the active site ID from the current token's authorities
        /// </summary>
        /// <returns>The active site ID or null if not found</returns>
        public string GetActiveSiteId()
        {
            var tokenPayload = GetCurrentTokenPayload();
            if (tokenPayload == null) return null;

            return GetActiveSiteIdFromPayload(tokenPayload);
        }

        /// <summary>
        /// Gets the active site ID from a specific token payload
        /// </summary>
        /// <param name="tokenPayload">The decoded JWT token payload</param>
        /// <returns>The active site ID or null if not found</returns>
        public string GetActiveSiteIdFromPayload(Dictionary<string, object> tokenPayload)
        {
            if (tokenPayload == null) return null;

            try
            {
                if (tokenPayload.ContainsKey("authorities"))
                {
                    var authorities = tokenPayload["authorities"];
                    if (authorities is System.Collections.ArrayList authoritiesList)
                    {
                        foreach (var authority in authoritiesList)
                        {
                            if (authority is Dictionary<string, object> authorityDict)
                            {
                                if (authorityDict.ContainsKey("authority") && authorityDict["authority"]?.ToString() == "EMPLOYEE")
                                {
                                    // Check if this authority has an active_site property
                                    if (authorityDict.ContainsKey("active_site"))
                                    {
                                        var activeSite = authorityDict["active_site"];
                                        if (activeSite is Dictionary<string, object> activeSiteDict)
                                        {
                                            // Extract the "first" field which contains the site ID
                                            if (activeSiteDict.ContainsKey("first"))
                                            {
                                                return activeSiteDict["first"]?.ToString();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error if needed, but don't throw
                System.Diagnostics.Debug.WriteLine($"Error extracting active site ID: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Gets all site IDs and farm IDs from the current token's authorities
        /// </summary>
        /// <returns>List of site information with type and ID</returns>
        public List<SiteInfo> GetAllSiteIds()
        {
            var tokenPayload = GetCurrentTokenPayload();
            if (tokenPayload == null) return new List<SiteInfo>();

            return GetAllSiteIdsFromPayload(tokenPayload);
        }

        /// <summary>
        /// Gets all site IDs from authorities where authority == "EMPLOYEE" and extracts the "first" field
        /// </summary>
        /// <returns>List of site IDs</returns>
        public List<string> GetEmployeeAuthoritySiteIds()
        {
            var tokenPayload = GetCurrentTokenPayload();
            if (tokenPayload == null) return new List<string>();

            return GetEmployeeAuthoritySiteIdsFromPayload(tokenPayload);
        }

        /// <summary>
        /// Gets all site IDs from authorities where authority == "EMPLOYEE" and extracts the "first" field from a specific token payload
        /// </summary>
        /// <param name="tokenPayload">The decoded JWT token payload</param>
        /// <returns>List of site IDs</returns>
        public List<string> GetEmployeeAuthoritySiteIdsFromPayload(Dictionary<string, object> tokenPayload)
        {
            var allSites = GetAllSiteIdsFromPayload(tokenPayload);
            var siteIds = new List<string>();
            
            foreach (var site in allSites)
            {
                if (!string.IsNullOrEmpty(site.Id) && !siteIds.Contains(site.Id))
                {
                    siteIds.Add(site.Id);
                }
            }
            
            return siteIds;
        }

        /// <summary>
        /// Gets all site IDs and farm IDs from a specific token payload
        /// </summary>
        /// <param name="tokenPayload">The decoded JWT token payload</param>
        /// <returns>List of site information with type and ID</returns>
        public List<SiteInfo> GetAllSiteIdsFromPayload(Dictionary<string, object> tokenPayload)
        {
            var siteList = new List<SiteInfo>();
            if (tokenPayload == null) return siteList;

            try
            {
                if (tokenPayload.ContainsKey("authorities"))
                {
                    var authorities = tokenPayload["authorities"];
                    if (authorities is System.Collections.ArrayList authoritiesList)
                    {
                        foreach (var authority in authoritiesList)
                        {
                            if (authority is Dictionary<string, object> authorityDict)
                            {
                                string authType = authorityDict.ContainsKey("authority") ? authorityDict["authority"]?.ToString() : "Unknown";
                                if (authType == "EMPLOYEE" || authType == "OWNER")
                                {
                                    // Check for active_site
                                    if (authorityDict.ContainsKey("active_site") && authorityDict["active_site"] is Dictionary<string, object> activeSiteDict)
                                    {
                                        if (activeSiteDict.ContainsKey("first") && activeSiteDict.ContainsKey("second"))
                                        {
                                            string siteId = activeSiteDict["first"]?.ToString();
                                            if (!string.IsNullOrEmpty(siteId) && !siteList.Any(s => s.Id == siteId))
                                            {
                                                siteList.Add(new SiteInfo { Id = siteId, Name = activeSiteDict["second"]?.ToString(), Type = "Active Site", Authority = authType });
                                            }
                                        }
                                    }

                                    // Check for farm (single)
                                    if (authorityDict.ContainsKey("farm") && authorityDict["farm"] is Dictionary<string, object> farmDict)
                                    {
                                        if (farmDict.ContainsKey("first") && farmDict.ContainsKey("second"))
                                        {
                                            string siteId = farmDict["first"]?.ToString();
                                            if (!string.IsNullOrEmpty(siteId) && !siteList.Any(s => s.Id == siteId))
                                            {
                                                siteList.Add(new SiteInfo { Id = siteId, Name = farmDict["second"]?.ToString(), Type = "Owner Farm", Authority = authType });
                                            }
                                        }
                                    }

                                    // Check for farms (dictionary)
                                    if (authorityDict.ContainsKey("farms") && authorityDict["farms"] is Dictionary<string, object> farmsDict)
                                    {
                                        foreach (var kvp in farmsDict)
                                        {
                                            if (kvp.Value is Dictionary<string, object> fDict && fDict.ContainsKey("first") && fDict.ContainsKey("second"))
                                            {
                                                string siteId = fDict["first"]?.ToString();
                                                if (!string.IsNullOrEmpty(siteId) && !siteList.Any(s => s.Id == siteId))
                                                {
                                                    siteList.Add(new SiteInfo { Id = siteId, Name = fDict["second"]?.ToString(), Type = "Owner Farm", Authority = authType });
                                                }
                                            }
                                        }
                                    }

                                    // Check for sites array or single object
                                    if (authorityDict.ContainsKey("sites"))
                                    {
                                        var sites = authorityDict["sites"];
                                        if (sites is Dictionary<string, object> sitesDict)
                                        {
                                            if (sitesDict.ContainsKey("first") && sitesDict.ContainsKey("second"))
                                            {
                                                string siteId = sitesDict["first"]?.ToString();
                                                if (!string.IsNullOrEmpty(siteId) && !siteList.Any(s => s.Id == siteId))
                                                {
                                                    siteList.Add(new SiteInfo { Id = siteId, Name = sitesDict["second"]?.ToString(), Type = "Site", Authority = authType });
                                                }
                                            }
                                        }
                                        else if (sites is System.Collections.ArrayList sitesList)
                                        {
                                            foreach (var siteObj in sitesList)
                                            {
                                                if (siteObj is Dictionary<string, object> sDict && sDict.ContainsKey("first") && sDict.ContainsKey("second"))
                                                {
                                                    string siteId = sDict["first"]?.ToString();
                                                    if (!string.IsNullOrEmpty(siteId) && !siteList.Any(s => s.Id == siteId))
                                                    {
                                                        siteList.Add(new SiteInfo { Id = siteId, Name = sDict["second"]?.ToString(), Type = "Site", Authority = authType });
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error if needed, but don't throw
                System.Diagnostics.Debug.WriteLine($"Error extracting site IDs: {ex.Message}");
            }

            return siteList;
        }

        /// <summary>
        /// Represents a site or farm with its details
        /// </summary>
        public class SiteInfo
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
            public string Authority { get; set; }
        }

        /// <summary>
        /// Represents an employee with basic information
        /// </summary>
        public class EmployeeInfo
        {
            public string user_id { get; set; }
            public string first_name { get; set; }
            public string last_name { get; set; }
            public string employee_site_id { get; set; }
            public string nick_name { get; set; }
            public string department { get; set; }
            public string status { get; set; }
            public string employment_status { get; set; }
            public string phone_number { get; set; }
            public string country { get; set; }
            public string gender { get; set; }
            public string employment_type { get; set; }
            public string employee_type { get; set; }
            public string nationality { get; set; }
            public string identity_serial { get; set; }
            public string start_date { get; set; }
            public string end_date { get; set; }
            public string email { get; set; }
            public string face_file_id { get; set; }
            public bool? has_employment { get; set; }
            public string dob { get; set; }
            public int? age { get; set; }
            public string emergency_contact_name { get; set; }
            public string emergency_contact_phone { get; set; }
            public string race { get; set; }
            public string emergency_contact_relation { get; set; }
            public string allergies { get; set; }
            public string chronic_diseases { get; set; }
            public bool? has_stay { get; set; }
            public string company_name { get; set; }
            public string company_address { get; set; }
            public int? contract_renewal_days { get; set; }
            public string scanlink_id { get; set; }
            // site_id is an object with site ID as key and site name as value
            public Dictionary<string, string> site_id { get; set; }
        }

        /// <summary>
        /// Represents the employee search request
        /// </summary>
        public class EmployeeSearchRequest
        {
            public string name { get; set; } = "";
            public string[] employment_status { get; set; } = new string[0];
            public string[] departments { get; set; } = new string[0];
            public string[] site_ids { get; set; }
            public string[] employment_type { get; set; } = new string[0];
            public string[] employee_type { get; set; } = new string[] { "LABOUR" };
            public string[] nationality { get; set; } = new string[0];
            public string[] gender { get; set; } = new string[0];
            public string[] id_verification_status { get; set; } = new string[0];
            public string[] face_biometric_status { get; set; } = new string[0];
            public string[] finger_biometric_status { get; set; } = new string[0];
            public string[] face_ids { get; set; } = new string[0];
            public string[] fingerprints { get; set; } = new string[0];
            public Dictionary<string, string> sort_columns { get; set; } = new Dictionary<string, string> { { "em.start_date", "DESC" } };
            public string pageNo { get; set; } = "0";
            public string pageSize { get; set; } = "15";
            public string actionByUserType { get; set; } = "SITE";
            public string actionByTypeId { get; set; }
        }

        /// <summary>
        /// Represents the employee search response
        /// </summary>
        public class EmployeeSearchResponse
        {
            public EmployeeInfo[] content { get; set; }
            public int page_no { get; set; }
            public int page_size { get; set; }
            public int total_elements { get; set; }
            public int total_pages { get; set; }
            public bool last { get; set; }
        }

        /// <summary>
        /// Fetches employees from the API using the active site ID
        /// </summary>
        /// <param name="searchRequest">The search criteria for employees</param>
        /// <param name="pageNo">Page number (0-based)</param>
        /// <param name="pageSize">Number of employees per page</param>
        /// <returns>API response containing employee data</returns>
        public async Task<ApiResponse<EmployeeSearchResponse>> GetEmployeesAsync(EmployeeSearchRequest searchRequest = null, int pageNo = 0, int pageSize = 15)
        {
            System.Diagnostics.Debug.WriteLine($"GetEmployeesAsync called - Token validation starting...");
            
            if (!IsTokenValid())
            {
                System.Diagnostics.Debug.WriteLine($"Token validation failed - returning error");
                return new ApiResponse<EmployeeSearchResponse>
                {
                    Success = false,
                    ErrorMessage = $"No valid authentication token available. Token: {(_currentToken != null ? "Present" : "Null")}, Expiry: {_tokenExpiry}",
                    StatusCode = 401
                };
            }

            try
            {
                // Get active site ID
                // var activeSiteId = GetActiveSiteId();
                // var allSiteIds = GetAllSiteIds();
                // List<string> allSiteIds = (GetAllSiteIds() ?? new List<ApiAuthService.SiteInfo>()).Select(s => s.Id).ToList();
                // if (allSiteIds.Count == 0)
                // {
                //     return new ApiResponse<EmployeeSearchResponse>
                //     {
                //         Success = false,
                //         ErrorMessage = "No site ID found in token",
                //         StatusCode = 400
                //     };
                // }

                // Use default search request if none provided
                if (searchRequest == null)
                {
                    searchRequest = new EmployeeSearchRequest();
                }

                // Respect incoming searchRequest values; only fill missing defaults
                if (searchRequest.site_ids == null || searchRequest.site_ids.Length == 0)
                {
                    // First priority: Use selected site from dashboard
                    if (!string.IsNullOrEmpty(_selectedSiteId))
                    {
                        searchRequest.site_ids = new string[] { _selectedSiteId };
                        System.Diagnostics.Debug.WriteLine($"Using selected site ID: {_selectedSiteId}");
                    }
                    // Fallback: Get site IDs from employee authorities in the current token
                    else
                    {
                        var tokenPayload = GetCurrentTokenPayload();
                        if (tokenPayload != null)
                        {
                            var employeeSiteIds = GetEmployeeAuthoritySiteIdsFromPayload(tokenPayload);
                            if (employeeSiteIds.Count > 0)
                            {
                                searchRequest.site_ids = employeeSiteIds.ToArray();
                                System.Diagnostics.Debug.WriteLine($"Using employee authority site IDs: [{string.Join(", ", employeeSiteIds)}]");
                            }
                        }
                    }
                }
                if (string.IsNullOrWhiteSpace(searchRequest.pageNo))
                {
                    searchRequest.pageNo = pageNo.ToString();
                }
                if (string.IsNullOrWhiteSpace(searchRequest.pageSize))
                {
                    searchRequest.pageSize = pageSize.ToString();
                }
                if (string.IsNullOrWhiteSpace(searchRequest.actionByUserType))
                {
                    searchRequest.actionByUserType = "SITE";
                }
                if (string.IsNullOrWhiteSpace(searchRequest.actionByTypeId))
                {
                    // First priority: Use selected site from dashboard
                    if (!string.IsNullOrEmpty(_selectedSiteId))
                    {
                        searchRequest.actionByTypeId = _selectedSiteId;
                        System.Diagnostics.Debug.WriteLine($"Using selected site ID for actionByTypeId: {_selectedSiteId}");
                    }
                    // Fallback: Prefer active site ID from token payload; fallback to first EMPLOYEE site
                    else
                    {
                        var tokenPayload = GetCurrentTokenPayload();
                        if (tokenPayload != null)
                        {
                            var activeSiteId = GetActiveSiteIdFromPayload(tokenPayload);
                            if (!string.IsNullOrWhiteSpace(activeSiteId))
                            {
                                searchRequest.actionByTypeId = activeSiteId;
                                System.Diagnostics.Debug.WriteLine($"Using active site ID for actionByTypeId: {activeSiteId}");
                            }
                            else
                            {
                                var employeeSiteIds = GetEmployeeAuthoritySiteIdsFromPayload(tokenPayload);
                                if (employeeSiteIds.Count > 0)
                                {
                                    searchRequest.actionByTypeId = employeeSiteIds[0];
                                    System.Diagnostics.Debug.WriteLine($"Active site not present; using first employee site ID: {employeeSiteIds[0]}");
                                }
                            }
                        }
                    }
                }

                // Build URL using values from searchRequest
                var urlBase = "https://backend-stage.labourlinksoftware.co.za/user/v1/employee/all";
                var url = $"{urlBase}?pageNo={searchRequest.pageNo}&pageSize={searchRequest.pageSize}&actionByUserType={searchRequest.actionByUserType}&actionByTypeId={searchRequest.actionByTypeId}";

                // Serialize request body
                var json = _jsonSerializer.Serialize(searchRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                // Debug output
                Console.WriteLine($"Employee API Request:");
                Console.WriteLine($"URL: {url}");
                Console.WriteLine($"Request Body: {json}");
                Console.WriteLine($"Token: {(_currentToken != null ? "Present" : "Null")}");
                
                // Debug department filter specifically
                if (searchRequest.departments != null && searchRequest.departments.Length > 0)
                {
                    Console.WriteLine($"Department filter in request: [{string.Join(", ", searchRequest.departments)}]");
                }
                else
                {
                    Console.WriteLine("No department filter in request");
                }

                // Create request with authorization header
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _currentToken);
                request.Headers.Add("Accept", "application/json");
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Debug output
                System.Diagnostics.Debug.WriteLine($"Employee API Response:");
                System.Diagnostics.Debug.WriteLine($"Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"Deserializing employee response...");
                    System.Diagnostics.Debug.WriteLine($"Raw API Response Content: {responseContent}");
                    
                    EmployeeSearchResponse employeeResponse = null;
                    
                    try
                    {
                        // Try to deserialize with JavaScriptSerializer first
                        employeeResponse = _jsonSerializer.Deserialize<EmployeeSearchResponse>(responseContent);
                        System.Diagnostics.Debug.WriteLine($"JavaScriptSerializer deserialization successful");
                    }
                    catch (Exception deserializeEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"JavaScriptSerializer failed: {deserializeEx.Message}");
                        
                        // If deserialization fails, create a simple response with raw data
                        employeeResponse = new EmployeeSearchResponse();
                        employeeResponse.content = new EmployeeInfo[0];
                        employeeResponse.page_no = 3;
                        employeeResponse.page_size = 0;
                        employeeResponse.total_elements = 0;
                        employeeResponse.total_pages = 0;
                        employeeResponse.last = true;
                        
                        // Store the raw response for debugging
                        System.Diagnostics.Debug.WriteLine($"Raw response stored for manual inspection: {responseContent}");
                    }
                    
                    if (employeeResponse != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Employee response deserialized: {employeeResponse.content?.Length ?? 0} employees found");
                        System.Diagnostics.Debug.WriteLine($"Total elements: {employeeResponse.total_elements}, Page: {employeeResponse.page_no}/{employeeResponse.total_pages}");
                        
                        if (employeeResponse.content != null && employeeResponse.content.Length > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"First employee: {employeeResponse.content[0].first_name} {employeeResponse.content[0].last_name} (ID: {employeeResponse.content[0].employee_site_id})");
                            
                            // Show first employee's full data structure
                            var firstEmployeeJson = _jsonSerializer.Serialize(employeeResponse.content[0]);
                            System.Diagnostics.Debug.WriteLine($"First employee full data: {firstEmployeeJson}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Employee response deserialization returned null");
                    }
                    
                    return new ApiResponse<EmployeeSearchResponse>
                    {
                        Success = true,
                        Data = employeeResponse,
                        StatusCode = (int)response.StatusCode
                    };
                }
                else
                {
                    return new ApiResponse<EmployeeSearchResponse>
                    {
                        Success = false,
                        ErrorMessage = $"Failed to fetch employees: {response.StatusCode} - {responseContent}",
                        StatusCode = (int)response.StatusCode
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<EmployeeSearchResponse>
                {
                    Success = false,
                    ErrorMessage = $"Exception during employee fetch: {ex.Message}",
                    StatusCode = 0
                };
            }
        }

        /// <summary>
        /// Extends the current token expiry (for debugging purposes)
        /// </summary>
        public void ExtendTokenExpiry()
        {
            if (!string.IsNullOrEmpty(_currentToken))
            {
                _tokenExpiry = DateTime.Now.AddHours(24); // Extend to 24 hours for testing
                System.Diagnostics.Debug.WriteLine($"Token expiry extended to: {_tokenExpiry}");
            }
        }

        /// <summary>
        /// Disposes of the HTTP client resources
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
