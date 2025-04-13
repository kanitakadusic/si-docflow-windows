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

namespace docflow
{
    public sealed partial class LoginPage : Window
    {
        public LoginPage()
        {
            InitializeComponent();
            SetWindowSize();

            LoadDocumentTypes();
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
            const string url = "https://docflow-server.up.railway.app/document/types";
            DocumentTypesList.Items.Clear();

            try
            {
                using HttpClient client = new();
                string response = await client.GetStringAsync(url);

                ApiResponse? apiResponse = JsonSerializer.Deserialize<ApiResponse>(response);

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

                var mainWindow = new MainWindow(username, documentType);
                mainWindow.Activate();
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
        }
    }
}