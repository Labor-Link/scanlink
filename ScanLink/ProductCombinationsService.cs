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

                var response = await _httpClient.SendAsync(request);
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
                    ErrorMessage = $"API request failed: {response.StatusCode} - {responseContent}",
                    StatusCode = (int)response.StatusCode
                };
            }
            catch (Exception ex)
            {
                return new ApiAuthService.ApiResponse<ProductCombination>
                {
                    Success = false,
                    ErrorMessage = $"Exception occurred: {ex.Message}"
                };
            }
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
