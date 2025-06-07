using docflow.Models;
using docflow.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WIA;
using Windows.Devices.Enumeration;

namespace docflow
{
    public partial class App : Application
    {
        private static WelcomeWindow? LoginWindow;
        private static bool _isHeadlessMode = false;
        private static DispatcherQueue? _dispatcherQueue;
        private static DispatcherQueueTimer? _keepAliveTimer;
        private static bool _httpListenerStarted = false;

        public App()
        {
            InitializeComponent();

            // Register for unhandled exceptions to ensure we log on crash
            this.UnhandledException += App_UnhandledException;
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // Log the exception
            System.Diagnostics.Debug.WriteLine($"Unhandled exception: {e.Message}");

            // Make sure we log the application shutdown
            Task.Run(async () => await LogApplicationShutdownAsync()).Wait();

            // Mark as handled so the app doesn't terminate abruptly
            e.Handled = true;
        }

      
        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            //Reset JSON file for device settings
            InfoDev devEmpty = new InfoDev("", "", 0);
            string jsonString = JsonSerializer.Serialize(devEmpty);
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(folderPath, "docflow");
            Directory.CreateDirectory(appFolder);
            string fullPath = Path.Combine(appFolder, "DevicesWindow.json");
            File.WriteAllText(fullPath, jsonString);

            // Store dispatcher queue for later use
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            // Log that the application is starting
            await ClientLogService.LogActionAsync(ClientActionType.INSTANCE_STARTED);

            // Prvo provjeri argumente komandne linije
            var cmdArgs = Environment.GetCommandLineArgs();
            bool isHeadlessRequested = CheckForHeadlessArgument(cmdArgs);

            if (isHeadlessRequested)
            {
                // Ako je --headless argument prisutan, odmah pokreni headless mod
                StartHeadlessMode();
                return;
            }

            // Nastavi s originalnom logikom...
            // Initialize configuration service and wait for configuration to load
            await ConfigurationService.Initialize(_dispatcherQueue);

            // Wait for initial configuration to load before determining app mode
            await ConfigurationService.InitialConfigLoaded;

            // Configure application based on the operational mode
            var config = ConfigurationService.CurrentConfig;
            System.Diagnostics.Debug.WriteLine($"Application configured: Title={config.Title}, Mode={config.OperationalMode}");

            // Subscribe to configuration updates
            ConfigurationService.ConfigUpdated += OnConfigurationUpdated;

            // Check operational mode and launch appropriately
            if (config.OperationalMode.ToLower() == "headless")
            {
                // Headless mode - don't show UI
                StartHeadlessMode();
            }
            else
            {
                // Default standalone mode with UI
                StartStandaloneMode();
            }
        }

        private void OnConfigurationUpdated(object? sender, ApplicationConfig config)
        {
            // Handle configuration updates here, such as operational mode changes
            System.Diagnostics.Debug.WriteLine($"Configuration updated: Title={config.Title}, Mode={config.OperationalMode}");

            // Handle operational mode changes
            if (config.OperationalMode.ToLower() == "headless" && !_isHeadlessMode)
            {
                // Switch to headless mode
                SwitchToHeadlessMode();
            }
            else if (config.OperationalMode.ToLower() == "standalone" && _isHeadlessMode)
            {
                // Switch to standalone mode with UI
                SwitchToStandaloneMode();
            }
        }

        private async void StartHeadlessMode()
        {
            System.Diagnostics.Debug.WriteLine("Starting in headless mode - no UI will be shown");
            _isHeadlessMode = true;

            if (_dispatcherQueue != null)
            {
                _keepAliveTimer = _dispatcherQueue.CreateTimer();

                if (_keepAliveTimer != null)
                {
                    // Checking for commands every minute
                    _keepAliveTimer.Interval = TimeSpan.FromMinutes(1);
                    _keepAliveTimer.Tick += async (s, e) =>
                    {
                        System.Diagnostics.Debug.WriteLine($"Headless mode active - {DateTime.Now}");

                        // Periodically check for commands
                        await CommandListenerService.CheckForCommandsAsync();
                    };
                    _keepAliveTimer.Start();

                    System.Diagnostics.Debug.WriteLine("Headless mode timer started");
                }
            }
            await SendDevicesToServer();
            // Start the HTTP listener if not already started
            await StartHttpListenerAsync();
        }

        private void StartStandaloneMode()
        {
            System.Diagnostics.Debug.WriteLine("Starting in standalone mode with UI");
            _isHeadlessMode = false;

            // Stop the keep-alive timer if it's running
            StopKeepAliveTimer();

            // Stop the HTTP listener if it's running
            StopHttpListener();

            // Create and activate the login window
            LoginWindow = new WelcomeWindow();
            LoginWindow.Activate();
        }

        private async void SwitchToHeadlessMode()
        {
            System.Diagnostics.Debug.WriteLine("Switching to headless mode");
            _isHeadlessMode = true;

            if (LoginWindow != null)
            {
                LoginWindow.Close();
                LoginWindow = null;
            }

            if (_dispatcherQueue != null)
            {
                _keepAliveTimer = _dispatcherQueue.CreateTimer();

                if (_keepAliveTimer != null)
                {
                    _keepAliveTimer.Interval = TimeSpan.FromMinutes(1);
                    _keepAliveTimer.Tick += async (s, e) =>
                    {
                        System.Diagnostics.Debug.WriteLine($"Headless mode active - {DateTime.Now}");
                        await CommandListenerService.CheckForCommandsAsync();
                    };
                    _keepAliveTimer.Start();

                    System.Diagnostics.Debug.WriteLine("Headless mode timer started");
                }
            }
            await SendDevicesToServer();
            // Start the HTTP listener when switching to headless mode
            await StartHttpListenerAsync();
        }

        private async Task SendDevicesToServer()
        {
            var url = AppSettings.ADMIN_SERVER_BASE_URL + $"windows-app-instance/report-available-devices/{ConfigurationService.CurrentConfig.MachineId}";

            var devices = await FindDeviceAsync();

            var deviceNames = devices.Select(d =>
            {
                string suffix = d.Device == DeviceTYPE.Camera ? " 0" :
                                d.Device == DeviceTYPE.Scanner ? " 1" : "";
                return d.Name + suffix;
            }).ToList();

            var payload = new { devices = deviceNames };
            var json = JsonSerializer.Serialize(payload);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content);

                var responseString = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Response: {responseString}");
                System.Diagnostics.Debug.WriteLine($"Payload: {json}");
                if (response.IsSuccessStatusCode)
                {
                    await ClientLogService.LogActionAsync(ClientActionType.DEVICES_DELIVERED);
                }
            }
        }
        private async Task<List<InfoDev>> FindDeviceAsync()
        {
            try
            {
                var allVideoDevicesInfo = await DeviceInformation.FindAllAsync(Windows.Devices.Enumeration.DeviceClass.VideoCapture);
                List<InfoDev> deviceList = new List<InfoDev>();

                foreach (var device in allVideoDevicesInfo)
                {
                    deviceList.Add(new InfoDev(device.Id, device.Name, DeviceTYPE.Camera));
                }
                DeviceManager deviceManager = new DeviceManager();

                for (int i = 1; i <= deviceManager.DeviceInfos.Count; i++) // WIA is 1-based
                {
                    DeviceInfo info = deviceManager.DeviceInfos[i];
                    if (info.Type == WiaDeviceType.ScannerDeviceType)
                    {
                        string name = info.Properties["Name"].get_Value().ToString();
                        string id = info.DeviceID;
                        deviceList.Add(new InfoDev(id, name, DeviceTYPE.Scanner));
                    }
                }
                return deviceList;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void SwitchToStandaloneMode()
        {
            System.Diagnostics.Debug.WriteLine("Switching to standalone mode with UI");
            _isHeadlessMode = false;

            // Stop the keep-alive timer
            StopKeepAliveTimer();

            // Stop the HTTP listener
            StopHttpListener();

            // Create and show UI
            if (LoginWindow == null)
            {
                LoginWindow = new WelcomeWindow();
                LoginWindow.Activate();
            }
        }

        private void StopKeepAliveTimer()
        {
            if (_keepAliveTimer != null)
            {
                _keepAliveTimer.Stop();
                _keepAliveTimer = null;
                System.Diagnostics.Debug.WriteLine("Headless mode timer stopped");
            }
        }

        private async Task StartHttpListenerAsync()
        {
            if (!_httpListenerStarted)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("Starting HTTP listener service...");
                    await HttpListenerService.StartAsync();
                    _httpListenerStarted = true;
                    System.Diagnostics.Debug.WriteLine("HTTP listener service started successfully");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to start HTTP listener: {ex.Message}");
                }
            }
        }

        private void StopHttpListener()
        {
            if (_httpListenerStarted)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("Stopping HTTP listener service...");
                    HttpListenerService.Stop();
                    _httpListenerStarted = false;
                    System.Diagnostics.Debug.WriteLine("HTTP listener service stopped");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error stopping HTTP listener: {ex.Message}");
                }
            }
        }

        //dodao sam ovu funkciju ovdje hajde sada
        private bool CheckForHeadlessArgument(string[] args)
        {
            if (args != null)
            {
                foreach (var arg in args)
                {
                    if (arg.ToLower() == "--headless" || arg.ToLower() == "-h")
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static async Task LogApplicationShutdownAsync()
        {
            // Stop any headless mode timers
            if (_keepAliveTimer != null)
            {
                _keepAliveTimer.Stop();
                _keepAliveTimer = null;
            }

            // Make sure to stop the HTTP listener
            if (_httpListenerStarted)
            {
                HttpListenerService.Stop();
                _httpListenerStarted = false;
            }

            await ClientLogService.LogActionAsync(ClientActionType.INSTANCE_STOPPED);
        }
    }
}