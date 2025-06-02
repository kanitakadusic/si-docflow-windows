using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;

namespace docflow.Services
{
    public enum ClientActionType
    {
        INSTANCE_STARTED,
        INSTANCE_STOPPED,
        PROCESSING_REQ_SENT,
        PROCESSING_RESULT_RECEIVED,
        COMMAND_RECEIVED,
        DEVICES_DELIVERED
    }
    
    public class ClientLogService
    {
        private static readonly string LOG_API_URL = string.Concat(AppSettings.ADMIN_SERVER_BASE_URL, "client-log/");
        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task LogActionAsync(ClientActionType actionType)
        {
            try
            {
                var logData = new ClientLogData
                {
                    machine_id = ConfigurationService.CurrentConfig.MachineId,
                    action = actionType.ToString().ToLower()
                };

                string jsonContent = JsonConvert.SerializeObject(logData);
                StringContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(LOG_API_URL, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to log client action: {actionType}. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in client logging: {ex.Message}");
            }
        }
        
        private class ClientLogData
        {
            public string machine_id { get; set; } = string.Empty;
            public string action { get; set; } = string.Empty;
        }
    }
}
