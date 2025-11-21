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

    public class ProductCombinationsService
    {
        private readonly ApiAuthService _apiAuthService;
        private readonly JavaScriptSerializer _jsonSerializer;
        private readonly HttpClient _httpClient;
        private List<ProductCombination> _cachedCombinations;

        public ProductCombinationsService(ApiAuthService apiAuthService)
        {
            _apiAuthService = apiAuthService;
            _jsonSerializer = new JavaScriptSerializer();
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
