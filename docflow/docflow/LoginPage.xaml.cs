using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.UI;
using Windows.Graphics;
using WinRT.Interop;
using Microsoft.UI.Windowing;
using System.Linq;
using static docflow.LoginPage;
using docflow.Services;

namespace docflow
{
    public sealed partial class LoginPage : Window
    {
        private static readonly string api_route = AppConfig.PROCESSING_SERVER_BASE_URL + "document/types";

        private List<DocumentType> _loadedDocumentTypes = [];
        public static bool HasDeviceSettingsBeenShownThisSession { get; set; } = false;

        public LoginPage()
        {
            _loadedDocumentTypes = new List<DocumentType>();
            InitializeComponent();
            SetWindowSize();

            // Add Closed event handler to log when app is closed from this window
            this.Closed += LoginPage_Closed;

            LoadDocumentTypes();
        }

        private async void LoginPage_Closed(object sender, WindowEventArgs args)
        {
            // Log application shutdown when this window is closed directly
            await App.LogApplicationShutdownAsync();
        }

        private void SetWindowSize()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            var (width, height) = App.GetPrimaryScreenSize();
            appWindow.Resize(new SizeInt32(width,height));

            appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
            OverlappedPresenter presenter = (OverlappedPresenter)appWindow.Presenter;
            presenter.Maximize();
        }

        public class DocumentType
        {
            public int id { get; set; }
            public string name { get; set; } = string.Empty;
            public string description { get; set; } = string.Empty;
        }

        public class ApiResponse
        {
            public List<DocumentType> data { get; set; } = [];
            public string message { get; set; } = string.Empty;
        }

        private async void LoadDocumentTypes()
        {
            string url = api_route;
            DocumentTypesList.Items.Clear();

            try
            {
                using HttpClient client = new();
                string response = await client.GetStringAsync(url);

                ApiResponse? apiResponse = JsonSerializer.Deserialize<ApiResponse>(response);
                _loadedDocumentTypes = apiResponse.data;

                if (apiResponse?.data != null)
                {
                    foreach (var documentType in apiResponse.data)
                    {
                        ComboBoxItem item = new()
                        {
                            Content = documentType.name
                        };
                        DocumentTypesList.Items.Add(item);
                    }
                }
                else
                {
                    var dialog = App.CreateContentDialog(
                        title: "No document types found",
                        message: "It looks like there are no document types available at the moment. Please check back later or contact support if the issue persists.",
                        xamlRoot: Content.XamlRoot
                    );
                    await dialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                var dialog = App.CreateContentDialog(
                    title: "Error",
                    message: ex.Message,
                    xamlRoot: Content.XamlRoot
                );
                await dialog.ShowAsync();
            }
        }

        private async void OnContinueButton(object sender, RoutedEventArgs e)
        {
            var continueButton = sender as Button;
            if (continueButton != null)
            {
                continueButton.IsEnabled = false;
            }

            try
            {
                string? username = UsernameTextBox.Text;
                if (string.IsNullOrEmpty(username))
                {
                    var dialog = App.CreateContentDialog(
                        title: "Missing input",
                        message: "Please fill in your name to continue.",
                        xamlRoot: Content.XamlRoot
                    );
                    await dialog.ShowAsync();
                    return;
                }

                string? documentType = (DocumentTypesList.SelectedItem as ComboBoxItem)?.Content?.ToString();
                if (string.IsNullOrEmpty(documentType))
                {
                    var dialog = App.CreateContentDialog(
                        title: "Selection required",
                        message: "Please select a document type from the dropdown list to continue.",
                        xamlRoot: Content.XamlRoot
                    );
                    await dialog.ShowAsync();
                    return;
                }

                string? selectedTypeName = (DocumentTypesList.SelectedItem as ComboBoxItem)?.Content?.ToString();
                var selectedType = _loadedDocumentTypes.FirstOrDefault(dt => dt.name == selectedTypeName);
                if (selectedType == null)
                {
                    var dialog = App.CreateContentDialog(
                        title: "Error: ",
                        message: "The unexpected error.",
                        xamlRoot: Content.XamlRoot
                    );
                    await dialog.ShowAsync();
                    return;
                }
                string documentTypeId = selectedType.id.ToString();                var mainWindow = new MainWindow(username, documentType, documentTypeId);
                mainWindow.Activate();
                if (HasDeviceSettingsBeenShownThisSession == false)
                {
                    var deviceSettings = new DeviceSettings();
                    deviceSettings.Activate();
                    HasDeviceSettingsBeenShownThisSession = true;
                }
                // Don't log application shutdown here since we're just transitioning to another window
                Close();
            }
            catch (Exception ex)
            {
                var dialog = App.CreateContentDialog(
                    title: "Error",
                    message: ex.Message,
                    xamlRoot: Content.XamlRoot
                );
                await dialog.ShowAsync();
            }
            finally
            {
                if (continueButton != null)
                {
                    continueButton.IsEnabled = true;
                }
            }
        }
    }
}