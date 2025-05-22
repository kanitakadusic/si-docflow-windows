using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;
using docflow.Models;

namespace docflow.Services
{
    public enum ClientActionType
    {
        CONFIG_FETCHED,
        INSTANCE_STARTED,
        INSTANCE_STOPPED,
        PROCESSING_REQ_SENT,
        PROCESSING_RESULT_RECEIVED,
        COMMAND_RECEIVED,
        COMMAND_PROCESSED
    }
    
    public class ClientLogService
    {
        private static readonly string _defaultMachineId = Environment.MachineName;
        private static readonly string LOG_API_URL = string.Concat(AppConfig.admin_api, "client-log/");
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly ApplicationConfig _currentConfig = new ApplicationConfig();

        public static async Task LogActionAsync(ClientActionType actionType)
        {
            try
            {
                var logData = new ClientLogData
                {
                    machine_id = _currentConfig.MachineId,
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
