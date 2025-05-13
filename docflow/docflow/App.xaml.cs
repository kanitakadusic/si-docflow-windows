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
        
        private void StartHeadlessMode()
        {
            System.Diagnostics.Debug.WriteLine("Starting in headless mode - no UI will be shown");
            _isHeadlessMode = true;
            
            // Create a timer to keep the application alive in headless mode
            // This timer prevents the app from exiting due to no active windows
            if (_dispatcherQueue != null)
            {
                // Create a timer that runs every 10 minutes (longer than the standard keep-alive)
                _keepAliveTimer = _dispatcherQueue.CreateTimer();
                
                // Null check the timer to satisfy the compiler
                if (_keepAliveTimer != null)
                {
                    _keepAliveTimer.Interval = TimeSpan.FromMinutes(10); // Check every 10 minutes
                    _keepAliveTimer.Tick += (s, e) => {
                        // This is a keep-alive tick to ensure the app doesn't exit
                        System.Diagnostics.Debug.WriteLine($"Headless mode active - {DateTime.Now}");
                        
                        // Do any periodic background tasks here if needed
                    };
                    _keepAliveTimer.Start();
                    
                    System.Diagnostics.Debug.WriteLine("Headless mode timer started");
                }
            }
        }
        
        private void StartStandaloneMode()
        {
            System.Diagnostics.Debug.WriteLine("Starting in standalone mode with UI");
            _isHeadlessMode = false;
            
            // Stop the keep-alive timer if it's running
            StopKeepAliveTimer();
            
            // Create and activate the login window
            LoginWindow = new LoginPage();
            LoginWindow.Activate();
        }
        
        private void SwitchToHeadlessMode()
        {
            System.Diagnostics.Debug.WriteLine("Switching to headless mode");
            _isHeadlessMode = true;
            
            // Close any open UI windows
            if (LoginWindow != null)
            {
                // Note: This won't trigger the Closed event since we're closing programmatically
                LoginWindow.Close();
                LoginWindow = null;
            }
            
            // Start the keep-alive timer
            if (_dispatcherQueue != null)
            {
                // Create a timer that runs every 10 minutes (longer than the standard keep-alive)
                _keepAliveTimer = _dispatcherQueue.CreateTimer();
                
                // Null check the timer to satisfy the compiler
                if (_keepAliveTimer != null)
                {
                    _keepAliveTimer.Interval = TimeSpan.FromMinutes(10); // Check every 10 minutes
                    _keepAliveTimer.Tick += (s, e) => {
                        System.Diagnostics.Debug.WriteLine($"Headless mode active - {DateTime.Now}");
                    };
                    _keepAliveTimer.Start();
                    
                    System.Diagnostics.Debug.WriteLine("Headless mode timer started");
                }
            }
        }
        
        private void SwitchToStandaloneMode()
        {
            System.Diagnostics.Debug.WriteLine("Switching to standalone mode with UI");
            _isHeadlessMode = false;
            
            // Stop the keep-alive timer
            StopKeepAliveTimer();
            
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
            
            await ClientLogService.LogActionAsync(ClientActionType.INSTANCE_STOPPED);
        }
    }
}
