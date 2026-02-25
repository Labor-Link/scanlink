using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace ScanLink
{
    public static class IssueLoggingService
    {
        private static readonly string ApiUrl = "https://backend-stage.labourlinksoftware.co.za/user/public/v1/issue/log";
        private static readonly HttpClient _client = new HttpClient();

        /// <summary>
        /// Asynchronously logs an issue to the cloud API.
        /// </summary>
        public static async Task LogIssueAsync(string subject, string message, string name = "ScanLink App", string email = "priyanshu@vidyayatan.com", string phone = "", string deviceInfo = "")
        {
            try
            {
                if (string.IsNullOrEmpty(deviceInfo))
                {
                    deviceInfo = $"{Environment.OSVersion} / ScanLink App";
                }

                var payload = new Dictionary<string, string>
                {
                    { "name", name },
                    { "email", email },
                    { "phone", phone },
                    { "subject", subject },
                    { "message", message },
                    { "deviceInfo", deviceInfo }
                };

                var serializer = new JavaScriptSerializer();
                string jsonPayload = serializer.Serialize(payload);

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // Public endpoint, so no auth header required
                await _client.PostAsync(ApiUrl, content);
            }
            catch (Exception)
            {
                // Silently swallow errors to prevent recursive exceptions 
                // if the cloud logging service itself is down.
            }
        }

        /// <summary>
        /// Fire and forget wrapper to easily log an issue from synchronous methods without awaiting.
        /// </summary>
        public static void LogIssue(string subject, string message, string name = "ScanLink App", string email = "priyanshu@vidyayatan.com", string phone = "", string deviceInfo = "")
        {
            Task.Run(() => LogIssueAsync(subject, message, name, email, phone, deviceInfo));
        }
    }
}
