using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.UI;
using Windows.Graphics;
using WinRT.Interop;
using System.Runtime.InteropServices;
using Windows.UI.ViewManagement;
using Microsoft.UI.Windowing;



// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace docflow
{
    public sealed partial class LoginPage : Window
    {

        public LoginPage()
        {
            this.InitializeComponent();

            SetWindowSize();
            LoadDocumentTypes();
        }

        private void SetWindowSize()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow =AppWindow.GetFromWindowId(windowId);

            var (width, height) = App.GetPrimaryScreenSize();
            appWindow.Resize(new SizeInt32(width,height));

            appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
            OverlappedPresenter presenter = (OverlappedPresenter)appWindow.Presenter;
            presenter.Maximize();

        }

        private class DocumentType
        {
            public int id { get; set; }
            public string name { get; set; }
            public string description { get; set; }
        }

        private class ApiResponse
        {
            public List<DocumentType> data { get; set; }
            public string message { get; set; }
        }

        private async void LoadDocumentTypes()
        {
            const string url = "https://docflow-server.up.railway.app/document/types";
            DocumentTypesList.Items.Clear();

            try
            {
                using HttpClient client = new HttpClient();
                string response = await client.GetStringAsync(url);

                ApiResponse apiResponse = JsonSerializer.Deserialize<ApiResponse>(response);

                if (apiResponse?.data != null)
                {
                    foreach (var documentType in apiResponse.data)
                    {
                        ComboBoxItem item = new ComboBoxItem();
                        item.Content = documentType.name;
                        DocumentTypesList.Items.Add(item);
                    }
                }
                else
                {
                    ContentDialog dialog = new ContentDialog
                    {
                        Title = "No document types found",
                        Content = "It looks like there are no document types available at the moment. Please check back later or contact support if the issue persists.",
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await dialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                ContentDialog dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Error: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }

        private async void OnContinueButton(object sender, RoutedEventArgs e)
        {
            try
            {
                string pc = Environment.MachineName;

                string username = UsernameTextBox.Text;
                if (string.IsNullOrEmpty(username))
                {
                    ContentDialog dialog = new ContentDialog
                    {
                        Title = "Missing input",
                        Content = "Please fill in your name to continue.",
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await dialog.ShowAsync();
                    return;
                }

                string documentType = (DocumentTypesList.SelectedItem as ComboBoxItem)?.Content?.ToString();
                if (string.IsNullOrEmpty(documentType))
                {
                    ContentDialog dialog = new ContentDialog
                    {
                        Title = "Selection required",
                        Content = "Please select a document type from the dropdown list to continue.",
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await dialog.ShowAsync();
                    return;
                }

                var mainWindow = new MainWindow(username, documentType);
                mainWindow.Activate();
                this.Close();
            }
            catch (Exception ex)
            {
                 ContentDialog dialog = new ContentDialog
                 {
                      Title = "Error",
                      Content = $"Error: {ex.Message}",
                      CloseButtonText = "OK",
                      XamlRoot = this.Content.XamlRoot
                 };
                 await dialog.ShowAsync();
            }
        }
    }
}