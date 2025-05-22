using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using docflow.Models;
using Microsoft.UI.Dispatching;
using Newtonsoft.Json.Linq;

namespace docflow.Services
{
    public class ConfigurationService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static Timer? _pollingTimer;
        private static readonly ApplicationConfig _currentConfig = new ApplicationConfig();
        private static readonly TaskCompletionSource<bool> _initialConfigLoadedTcs = new TaskCompletionSource<bool>();
        
        public static event EventHandler<ApplicationConfig>? ConfigUpdated;

        private static readonly string CONFIG_API_URL = string.Concat(AppConfig.admin_api, "windows-app-instance/machine/");

        public static ApplicationConfig CurrentConfig => _currentConfig;

        public static Task InitialConfigLoaded => _initialConfigLoadedTcs.Task;

        public static async Task Initialize(DispatcherQueue? dispatcherQueue = null)
        {
            await FetchConfigAsync(isInitialFetch: true);
            
            StartPolling();
        }

        public static async Task FetchConfigAsync(bool isInitialFetch = false)
        {
            try
            {
                // Log that we're fetching configuration
                await ClientLogService.LogActionAsync(ClientActionType.CONFIG_FETCHED);
                
                // Build the URL with machine ID
                string url = $"{CONFIG_API_URL}{_currentConfig.MachineId}";
                
                // Make the request
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    JObject jsonObject = JObject.Parse(jsonResponse);
                    
                    // Update configuration properties from response with null checks
                    JToken? idToken = jsonObject["id"];
                    if (idToken != null) _currentConfig.Id = idToken.Value<int>();
                    
                    JToken? titleToken = jsonObject["title"];
                    if (titleToken != null) _currentConfig.Title = titleToken.Value<string>() ?? _currentConfig.Title;
                    
                    JToken? locationToken = jsonObject["location"];
                    if (locationToken != null) _currentConfig.Location = locationToken.Value<string>() ?? _currentConfig.Location;
                    
                    JToken? machineIdToken = jsonObject["machine_id"];
                    if (machineIdToken != null) _currentConfig.MachineId = machineIdToken.Value<string>() ?? _currentConfig.MachineId;
                    
                    JToken? operationalModeToken = jsonObject["operational_mode"];
                    if (operationalModeToken != null) _currentConfig.OperationalMode = operationalModeToken.Value<string>() ?? _currentConfig.OperationalMode;
                    
                    JToken? pollingFrequencyToken = jsonObject["polling_frequency"];
                    if (pollingFrequencyToken != null) _currentConfig.PollingFrequency = pollingFrequencyToken.Value<int>();
                    
                    JToken? createdByToken = jsonObject["created_by"];
                    if (createdByToken != null) _currentConfig.CreatedBy = createdByToken.Value<int>();
                    
                    JToken? updatedByToken = jsonObject["updated_by"];
                    if (updatedByToken != null) _currentConfig.UpdatedBy = updatedByToken.Value<int>();
                    
                    JToken? createdAtToken = jsonObject["createdAt"];
                    if (createdAtToken != null) _currentConfig.CreatedAt = createdAtToken.Value<DateTime>();
                    
                    JToken? updatedAtToken = jsonObject["updatedAt"];
                    if (updatedAtToken != null) _currentConfig.UpdatedAt = updatedAtToken.Value<DateTime>();
                    
                    _currentConfig.LastFetched = DateTime.Now;
                    _currentConfig.IsConfigured = true;
                    
                    // Notify subscribers
                    ConfigUpdated?.Invoke(null, _currentConfig);
                    
                    // Update polling timer based on new frequency
                    UpdatePollingTimer();
                    
                    // Signal that initial configuration has been loaded
                    if (isInitialFetch && !_initialConfigLoadedTcs.Task.IsCompleted)
                    {
                        _initialConfigLoadedTcs.SetResult(true);
                    }
                      System.Diagnostics.Debug.WriteLine($"Configuration updated: {_currentConfig.Title}, Mode: {_currentConfig.OperationalMode}, Polling: {_currentConfig.PollingFrequency}h");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to fetch configuration. Status: {response.StatusCode}");
                    
                    // If this is the initial fetch and it failed, we still need to continue
                    if (isInitialFetch && !_initialConfigLoadedTcs.Task.IsCompleted)
                    {
                        _initialConfigLoadedTcs.SetResult(false);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching configuration: {ex.Message}");
                
                // If this is the initial fetch and it failed, we still need to continue
                if (isInitialFetch && !_initialConfigLoadedTcs.Task.IsCompleted)
                {
                    _initialConfigLoadedTcs.SetResult(false);
                }
            }
        }
        
        private static void StartPolling()
        {
            // Set initial polling interval based on config
            UpdatePollingTimer();
        }
          private static void UpdatePollingTimer()
        {
            // Dispose existing timer if any
            _pollingTimer?.Dispose();
            
            // Calculate polling interval in milliseconds from hours
            // Convert hours to milliseconds (1 hour = 3600 seconds = 3,600,000 milliseconds)
            // Ensure a minimum polling interval of 10 minutes (600,000 ms) for very small hour values
            int pollingIntervalMs = Math.Max(_currentConfig.PollingFrequency * 3600 * 1000, 600000);
            
            // Create new timer for periodic polling
            _pollingTimer = new Timer(async _ => 
            {
                await FetchConfigAsync();
            }, null, pollingIntervalMs, pollingIntervalMs);
            
            System.Diagnostics.Debug.WriteLine($"Polling timer updated: {_currentConfig.PollingFrequency} hours ({pollingIntervalMs}ms)");
        }
        
        public static void StopPolling()
        {
            _pollingTimer?.Dispose();
            _pollingTimer = null;
        }
    }
}
