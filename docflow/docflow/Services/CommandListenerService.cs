using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using docflow.Models;
using Newtonsoft.Json;

namespace docflow.Services
{
    public static class CommandListenerService
    {
        private static readonly string API_URL = string.Concat(AppConfig.admin_api, "remote/commands/");
        private static readonly HttpClient _client = new HttpClient();
        private static bool _isProcessing;
        private static readonly ApplicationConfig _currentConfig = new ApplicationConfig();

        public static async Task CheckForCommandsAsync()
        {
            if (_isProcessing) return;

            try
            {
                var response = await _client.GetAsync(API_URL + _currentConfig.MachineId);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var commands = JsonConvert.DeserializeObject<List<RemoteCommand>>(content);

                    foreach (var command in commands)
                    {
                        _isProcessing = true;
                        await ProcessCommandAsync(command);
                        _isProcessing = false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Command error: {ex.Message}");
            }
        }

        private static async Task ProcessCommandAsync(RemoteCommand command)
        {
            try
            {
                await DocumentProcessingService.ProcessRemoteCommand(command);
                await ClientLogService.LogActionAsync(ClientActionType.COMMAND_PROCESSED);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Command processing failed: {ex.Message}");
            }
        }

        public class RemoteCommand
        {
            public string transaction_id { get; set; }
            public string document_type_id { get; set; }
            public string file_name { get; set; }
        }
    }
}
