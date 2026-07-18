using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace ScanLink
{
    // Data classes for API response
    public class ProductCombination
    {
        public string id { get; set; }
        public string crop_id { get; set; }
        public string crop_name { get; set; }
        public string product_id { get; set; }
        public string product_name { get; set; }
        public string variety_id { get; set; }
        public string variety_name { get; set; }
        public string grade_id { get; set; }
        public string grade_name { get; set; }
        public string count_id { get; set; }
        public string count_name { get; set; }
        public string carton_type_id { get; set; }
        public string carton_type_name { get; set; }
        public double avg_weight_kg { get; set; }
    }

    public class CropItem
    {
        public string crop_id { get; set; }
        public string crop_name { get; set; }
    }

    public class ProductItem
    {
        public string product_id { get; set; }
        public string product_name { get; set; }
    }

    public class VarietyItem
    {
        public string variety_id { get; set; }
        public string variety_name { get; set; }
    }

    public class GradeItem
    {
        public string grade_id { get; set; }
        public string grade_name { get; set; }
    }

    public class CountItem
    {
        public string count_id { get; set; }
        public string count_name { get; set; }
    }

    public class ProductCombinationsResponse
    {
        public List<ProductCombination> combinations { get; set; }
        public int total_count { get; set; }
    }

    // Request body for POST /product-combinations - combines existing master values into a new row.
    public class CreateProductCombinationRequest
    {
        public string crop_id { get; set; }
        public string variety_id { get; set; }
        public string grade_id { get; set; }
        public string count_id { get; set; }
        public string carton_type_id { get; set; }
        public double avg_weight_kg { get; set; }
    }

    // Request/response shapes for the master-data "+ Add new" create endpoints. Crop/variety are
    // NOT creatable - only grade/count/carton_type, per the desktop app's Add Combination dialog.
    public class CreateNameRequest
    {
        public string name { get; set; }
    }

    public class CreateCartonTypeRequest
    {
        public string name { get; set; }
        public double weight_kg { get; set; }
    }

    public class NamedMasterItem
    {
        public string id { get; set; }
        public string name { get; set; }
        public double? weight_kg { get; set; } // only populated for carton types
    }

    public class ProductCombinationsService
    {
        private readonly ApiAuthService _apiAuthService;
        private readonly JavaScriptSerializer _jsonSerializer;
        private readonly HttpClient _httpClient;
        private List<ProductCombination> _cachedCombinations;

        public ProductCombinationsService(ApiAuthService apiAuthService)
        {
            _apiAuthService = apiAuthService;
            _jsonSerializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            _httpClient = new HttpClient();

            // Set default headers to match the ApiAuthService pattern
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

            _cachedCombinations = null;
        }

        /// <summary>
        /// Fetches product combinations from the API and caches them
        /// </summary>
        public async Task<ApiAuthService.ApiResponse<bool>> FetchAndCacheProductCombinationsAsync()
        {
            if (!_apiAuthService.IsTokenValid())
            {
                return new ApiAuthService.ApiResponse<bool>
                {
                    Success = false,
                    ErrorMessage = "No valid authentication token available"
                };
            }

            try
            {
                var url = "https://backend-stage.labourlinksoftware.co.za/user/v1/scan-link/product-combinations";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiAuthService.GetCurrentToken());
                request.Headers.Add("Accept", "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"Product Combinations API Response:");
                System.Diagnostics.Debug.WriteLine($"Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = _jsonSerializer.Deserialize<ProductCombinationsResponse>(responseContent);

                    if (apiResponse?.combinations != null)
                    {
                        _cachedCombinations = apiResponse.combinations;
                        return new ApiAuthService.ApiResponse<bool>
                        {
                            Success = true,
                            Data = true,
                            StatusCode = (int)response.StatusCode
                        };
                    }
                    else
                    {
                        return new ApiAuthService.ApiResponse<bool>
                        {
                            Success = false,
                            ErrorMessage = "Invalid API response format",
                            StatusCode = (int)response.StatusCode
                        };
                    }
                }
                else
                {
                    return new ApiAuthService.ApiResponse<bool>
                    {
                        Success = false,
                        ErrorMessage = $"API request failed: {response.StatusCode} - {responseContent}",
                        StatusCode = (int)response.StatusCode
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiAuthService.ApiResponse<bool>
                {
                    Success = false,
                    ErrorMessage = $"Exception occurred: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Gets unique crops from cached combinations
        /// </summary>
        public List<CropItem> GetUniqueCrops()
        {
            if (_cachedCombinations == null || _cachedCombinations.Count == 0)
                return new List<CropItem>();

            return _cachedCombinations
                .GroupBy(c => c.crop_id)
                .Select(g => new CropItem
                {
                    crop_id = g.Key,
                    crop_name = g.First().crop_name
                })
                .OrderBy(c => c.crop_name)
                .ToList();
        }

        /// <summary>
        /// Gets unique varieties from cached combinations
        /// </summary>
        public List<VarietyItem> GetUniqueVarieties()
        {
            if (_cachedCombinations == null || _cachedCombinations.Count == 0)
                return new List<VarietyItem>();

            return _cachedCombinations
                .GroupBy(c => c.variety_id)
                .Select(g => new VarietyItem
                {
                    variety_id = g.Key,
                    variety_name = g.First().variety_name
                })
                .OrderBy(v => v.variety_name)
                .ToList();
        }

        /// <summary>
        /// Gets unique grades from cached combinations
        /// </summary>
        public List<GradeItem> GetUniqueGrades()
        {
            if (_cachedCombinations == null || _cachedCombinations.Count == 0)
                return new List<GradeItem>();

            return _cachedCombinations
                .GroupBy(c => c.grade_id)
                .Select(g => new GradeItem
                {
                    grade_id = g.Key,
                    grade_name = g.First().grade_name
                })
                .OrderBy(g => g.grade_name)
                .ToList();
        }

        /// <summary>
        /// Gets unique counts from cached combinations
        /// </summary>
        public List<CountItem> GetUniqueCounts()
        {
            if (_cachedCombinations == null || _cachedCombinations.Count == 0)
                return new List<CountItem>();

            return _cachedCombinations
                .GroupBy(c => c.count_id)
                .Select(g => new CountItem
                {
                    count_id = g.Key,
                    count_name = g.First().count_name
                })
                .OrderBy(c => c.count_name)
                .ToList();
        }

        /// <summary>
        /// Gets unique carton types from cached combinations
        /// </summary>
        public List<ProductCombination> GetUniqueCartonTypes()
        {
            if (_cachedCombinations == null || _cachedCombinations.Count == 0)
                return new List<ProductCombination>();

            return _cachedCombinations
                .GroupBy(c => c.carton_type_id)
                .Select(g => g.First())
                .OrderBy(c => c.carton_type_name)
                .ToList();
        }

        /// <summary>
        /// Creates a new product combination on the server from EXISTING crop/variety/grade/count/
        /// carton-type values (no new master data is created). On success the local cache is
        /// refreshed so the new combination is immediately available in the dropdowns.
        /// </summary>
        public async Task<ApiAuthService.ApiResponse<ProductCombination>> CreateProductCombinationAsync(
            string cropId, string varietyId, string gradeId, string countId, string cartonTypeId, double avgWeightKg)
        {
            if (!_apiAuthService.IsTokenValid())
            {
                return new ApiAuthService.ApiResponse<ProductCombination>
                {
                    Success = false,
                    ErrorMessage = "No valid authentication token available"
                };
            }

            try
            {
                var url = "https://backend-stage.labourlinksoftware.co.za/user/v1/scan-link/product-combinations";
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiAuthService.GetCurrentToken());
                request.Headers.Add("Accept", "application/json");

                var body = new CreateProductCombinationRequest
                {
                    crop_id = cropId,
                    variety_id = varietyId,
                    grade_id = gradeId,
                    count_id = countId,
                    carton_type_id = cartonTypeId,
                    avg_weight_kg = avgWeightKg
                };
                request.Content = new System.Net.Http.StringContent(
                    _jsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                try
                {
                    response = await _httpClient.SendAsync(request);
                }
                catch (Exception netEx)
                {
                    // HttpClient wraps DNS/connection-refused/no-network failures as
                    // HttpRequestException (and slow/unreachable hosts as TaskCanceledException
                    // via the request timeout) - both mean "couldn't reach the server", not a
                    // server-side rejection, so give the user something actionable instead of
                    // the raw .NET exception text.
                    System.Diagnostics.Debug.WriteLine($"CreateProductCombinationAsync network error: {netEx}");
                    return new ApiAuthService.ApiResponse<ProductCombination>
                    {
                        Success = false,
                        ErrorMessage = "No connection to the server. Check your internet connection and try again."
                    };
                }

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var created = _jsonSerializer.Deserialize<ProductCombination>(responseContent);
                    // Refresh the cache so the new combination shows up in dropdowns right away.
                    await FetchAndCacheProductCombinationsAsync();
                    return new ApiAuthService.ApiResponse<ProductCombination>
                    {
                        Success = true,
                        Data = created,
                        StatusCode = (int)response.StatusCode
                    };
                }

                return new ApiAuthService.ApiResponse<ProductCombination>
                {
                    Success = false,
                    ErrorMessage = DescribeCreateError(response.StatusCode, responseContent, "Invalid selection"),
                    StatusCode = (int)response.StatusCode
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateProductCombinationAsync failed: {ex}");
                return new ApiAuthService.ApiResponse<ProductCombination>
                {
                    Success = false,
                    ErrorMessage = "Something went wrong creating the combination. Please try again."
                };
            }
        }

        // The backend returns errors as {"ex": "<message>", ...} (see GlobalExceptionHandler /
        // ErrorInfo). Pull that message out and add a plain-language prefix per status code so
        // the dialog shows something an operator can act on instead of a raw HTTP/JSON dump.
        // `invalidPrefix` lets callers phrase the 400 case for what they were creating
        // ("Invalid selection" for a combination, "Invalid name" for a new grade/count/carton).
        private string DescribeCreateError(System.Net.HttpStatusCode statusCode, string responseContent, string invalidPrefix)
        {
            string serverMessage = null;
            try
            {
                var parsed = _jsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                if (parsed != null && parsed.ContainsKey("ex") && parsed["ex"] != null)
                    serverMessage = parsed["ex"].ToString();
            }
            catch
            {
                // Not JSON (or unexpected shape) - fall through and use a generic message below.
            }

            switch (statusCode)
            {
                case System.Net.HttpStatusCode.BadRequest:
                    return invalidPrefix + ": " + (serverMessage ?? "the value entered is not valid.");
                case System.Net.HttpStatusCode.Conflict:
                    return serverMessage ?? "That already exists.";
                case System.Net.HttpStatusCode.Unauthorized:
                case System.Net.HttpStatusCode.Forbidden:
                    return "Your session has expired. Please log in again and retry.";
                default:
                    return !string.IsNullOrWhiteSpace(serverMessage)
                        ? serverMessage
                        : $"Server error ({(int)statusCode}). Please try again later.";
            }
        }

        // Generic POST helper for the three master-data create endpoints below. `path` is the
        // resource segment (e.g. "grades"); `body` is serialized as-is; the parsed response is
        // deserialized as NamedMasterItem {id, name, weight_kg?}. No cache refresh here - unlike
        // combinations, a brand-new grade/count/carton_type has no combinations referencing it
        // yet, so callers add the returned item straight into their own dropdown instead.
        private async Task<ApiAuthService.ApiResponse<NamedMasterItem>> CreateNamedMasterItemAsync(string path, object body, string invalidPrefix)
        {
            if (!_apiAuthService.IsTokenValid())
            {
                return new ApiAuthService.ApiResponse<NamedMasterItem>
                {
                    Success = false,
                    ErrorMessage = "No valid authentication token available"
                };
            }

            try
            {
                var url = $"https://backend-stage.labourlinksoftware.co.za/user/v1/scan-link/{path}";
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiAuthService.GetCurrentToken());
                request.Headers.Add("Accept", "application/json");
                request.Content = new System.Net.Http.StringContent(
                    _jsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                try
                {
                    response = await _httpClient.SendAsync(request);
                }
                catch (Exception netEx)
                {
                    System.Diagnostics.Debug.WriteLine($"CreateNamedMasterItemAsync({path}) network error: {netEx}");
                    return new ApiAuthService.ApiResponse<NamedMasterItem>
                    {
                        Success = false,
                        ErrorMessage = "No connection to the server. Check your internet connection and try again."
                    };
                }

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var created = _jsonSerializer.Deserialize<NamedMasterItem>(responseContent);
                    return new ApiAuthService.ApiResponse<NamedMasterItem>
                    {
                        Success = true,
                        Data = created,
                        StatusCode = (int)response.StatusCode
                    };
                }

                return new ApiAuthService.ApiResponse<NamedMasterItem>
                {
                    Success = false,
                    ErrorMessage = DescribeCreateError(response.StatusCode, responseContent, invalidPrefix),
                    StatusCode = (int)response.StatusCode
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateNamedMasterItemAsync({path}) failed: {ex}");
                return new ApiAuthService.ApiResponse<NamedMasterItem>
                {
                    Success = false,
                    ErrorMessage = "Something went wrong creating that. Please try again."
                };
            }
        }

        /// <summary>Creates a new grade (e.g. a new "Class") on the server.</summary>
        public Task<ApiAuthService.ApiResponse<NamedMasterItem>> CreateGradeAsync(string name)
        {
            return CreateNamedMasterItemAsync("grades", new CreateNameRequest { name = name }, "Invalid name");
        }

        /// <summary>Creates a new count (size) on the server.</summary>
        public Task<ApiAuthService.ApiResponse<NamedMasterItem>> CreateCountAsync(string name)
        {
            return CreateNamedMasterItemAsync("counts", new CreateNameRequest { name = name }, "Invalid name");
        }

        /// <summary>Creates a new carton type on the server.</summary>
        public Task<ApiAuthService.ApiResponse<NamedMasterItem>> CreateCartonTypeAsync(string name, double weightKg)
        {
            return CreateNamedMasterItemAsync("carton-types",
                new CreateCartonTypeRequest { name = name, weight_kg = weightKg }, "Invalid name/weight");
        }

        /// <summary>
        /// Gets products for a specific crop from cached combinations
        /// </summary>
        public List<ProductCombination> GetProductsForCrop(string cropId)
        {
            if (_cachedCombinations == null || _cachedCombinations.Count == 0 || string.IsNullOrEmpty(cropId))
                return new List<ProductCombination>();

            return _cachedCombinations
                .Where(c => c.crop_id == cropId)
                .ToList();
        }

        /// <summary>
        /// Gets all cached combinations
        /// </summary>
        public List<ProductCombination> GetAllCombinations()
        {
            return _cachedCombinations ?? new List<ProductCombination>();
        }

        /// <summary>
        /// Clears the cached data
        /// </summary>
        public void ClearCache()
        {
            _cachedCombinations = null;
        }

        /// <summary>
        /// Checks if data is cached
        /// </summary>
        public bool HasCachedData()
        {
            return _cachedCombinations != null && _cachedCombinations.Count > 0;
        }
    }
}
