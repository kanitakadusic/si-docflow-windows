using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.UI;
using docflow.Services;
using docflow.Models;
using Microsoft.UI.Dispatching;

namespace docflow
{
    public partial class App : Application
    {
        private static LoginPage? LoginWindow;
        private static bool _isHeadlessMode = false;
        private static DispatcherQueue? _dispatcherQueue;
        private static DispatcherQueueTimer? _keepAliveTimer;
        private static bool _httpListenerStarted = false;

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

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
            // Store dispatcher queue for later use
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            // Log that the application is starting
            await ClientLogService.LogActionAsync(ClientActionType.INSTANCE_STARTED);

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
            LoginWindow = new LoginPage();
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

            // Start the HTTP listener when switching to headless mode
            await StartHttpListenerAsync();
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
                LoginWindow = new LoginPage();
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

        public static (int Width, int Height) GetPrimaryScreenSize()
        {
            int width = GetSystemMetrics(SM_CXSCREEN);
            int height = GetSystemMetrics(SM_CYSCREEN);
            return (width, height);
        }

        public static ContentDialog CreateContentDialog(string title, string message, XamlRoot xamlRoot, bool isError = true)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = xamlRoot,
                DefaultButton = ContentDialogButton.Close,
                CornerRadius = new CornerRadius(24)
            };

            var titleBlock = new TextBlock
            {
                Text = title,
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(20, 20, 20, 20),
                TextWrapping = TextWrapping.Wrap
            };

            var contentBlock = new TextBlock
            {
                Text = message,
                FontSize = 24,
                Margin = new Thickness(20, 20, 20, 20),
                TextWrapping = TextWrapping.Wrap
            };

            var background = isError
                ? new SolidColorBrush(Color.FromArgb(255, 220, 53, 69))
                : new SolidColorBrush(Color.FromArgb(255, 32, 156, 238));

            var closeButton = new Button
            {
                Content = "OK",
                FontSize = 24,
                Padding = new Thickness(40, 20, 40, 20),
                Margin = new Thickness(20, 20, 20, 20),
                HorizontalAlignment = HorizontalAlignment.Right,
                Foreground = new SolidColorBrush(Colors.White),
                Background = background,
                CornerRadius = new CornerRadius(14)
            };

            closeButton.Click += (_, _) => dialog.Hide();

            var stackPanel = new StackPanel();
            stackPanel.Children.Add(titleBlock);
            stackPanel.Children.Add(contentBlock);
            stackPanel.Children.Add(closeButton);

            dialog.Content = stackPanel;

            return dialog;
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