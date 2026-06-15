using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace ScanLink
{
    public class DailyStatsService
    {
        private readonly ApiAuthService _apiAuthService;
        private readonly JavaScriptSerializer _jsonSerializer;
        private readonly string _baseUrl = "https://backend-stage.labourlinksoftware.co.za";

        public DailyStatsService(ApiAuthService apiAuthService)
        {
            _apiAuthService = apiAuthService;
            _jsonSerializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
        }

        public async Task<Dictionary<string, string>> GetDailyStatsAsync(string siteId, DateTime date)
        {
            try
            {
                string dateStr = date.ToString("yyyy-MM-dd");
                string url = $"{_baseUrl}/user/v1/daily-stats/date?siteId={siteId}&statDate={dateStr}";

                var response = await _apiAuthService.MakeAuthenticatedRequestAsync(url, HttpMethod.Get);
                if (response.Success && !string.IsNullOrEmpty(response.Data))
                {
                    var data = _jsonSerializer.Deserialize<Dictionary<string, object>>(response.Data);
                    var resultMap = new Dictionary<string, string>();
                    
                    if (data.ContainsKey("stats") && data["stats"] is System.Collections.ArrayList statsList)
                    {
                        foreach (Dictionary<string, object> stat in statsList)
                        {
                            if (stat.ContainsKey("stat_type") && stat.ContainsKey("stat_value"))
                            {
                                string type = stat["stat_type"]?.ToString();
                                string val = stat["stat_value"]?.ToString();
                                if (!string.IsNullOrEmpty(type))
                                {
                                    resultMap[type] = val;
                                }
                            }
                        }
                    }
                    return resultMap;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get daily stats: {ex.Message}");
            }
            return new Dictionary<string, string>();
        }

        public async Task<(bool Success, string ErrorMessage, string Payload)> SaveDailyStatsAsync(string siteId, DateTime date, Dictionary<string, string> stats)
        {
            try
            {
                if (stats == null || stats.Count == 0) return (true, "", "");

                string url = $"{_baseUrl}/user/v1/daily-stats/bulk";
                
                var statsArray = stats.Select(kvp => new { stat_type = kvp.Key, stat_value = kvp.Value }).ToArray();
                
                var payload = new
                {
                    site_id = siteId,
                    stat_date = date.ToString("yyyy-MM-dd"),
                    stats = statsArray
                };

                string jsonContent = _jsonSerializer.Serialize(payload);
                
                var response = await _apiAuthService.MakeAuthenticatedRequestAsync(url, HttpMethod.Post, jsonContent);
                return (response.Success, response.ErrorMessage, jsonContent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save daily stats: {ex.Message}");
                return (false, ex.Message, "");
            }
        }
    }
}
