using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace docflow
{

    public sealed partial class LoginPage : Window
    {
        public LoginPage()
        {
            this.InitializeComponent();


             LoadDocumentTypes();

            

        }

        
        private async void LoadDocumentTypes()
        {
            try
            {
                using HttpClient client = new HttpClient();
                string response = await client.GetStringAsync("http://localhost:5000/document-types");
                using JsonDocument json = JsonDocument.Parse(response);

                DocumentTypesList.Items.Clear();

                foreach (JsonElement element in json.RootElement.EnumerateArray())
                {
                    string name = element.GetProperty("name").GetString();

                    ListViewItem item = new ListViewItem();
                    item.Content = name;
                    DocumentTypesList.Items.Add(item);

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

        private async void OnLoginClicked(object sender, RoutedEventArgs e)
        {

            try
            {
                string username = UsernameTextBox.Text;
                if (string.IsNullOrEmpty(username))
                {
                    var dlg = new Microsoft.UI.Xaml.Controls.ContentDialog
                    {
                        Title = "Error",
                        Content = "Enter a username.",
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await dlg.ShowAsync();
                    return;
                }

                string pc = Environment.MachineName;

                string documentType = (DocumentTypesList.SelectedItem as ListViewItem)?.Content?.ToString();
                if (string.IsNullOrEmpty(documentType))
                {
                    var dlg = new Microsoft.UI.Xaml.Controls.ContentDialog
                    {
                        Title = "Error",
                        Content = "Please select a document.",
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await dlg.ShowAsync();
                    return;
                }

                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".png");
                picker.FileTypeFilter.Add(".pdf");

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(((App)Application.Current).m_window);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                //var fileStream = await file.OpenReadAsync();
                //using var stream = fileStream.AsStreamForRead();
                //byte[] fileBytes = new byte[stream.Length];
                //await stream.ReadAsync(fileBytes, 0, fileBytes.Length);

                //var content = new MultipartFormDataContent();
                //var byteContent = new ByteArrayContent(fileBytes);
                //byteContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                //content.Add(byteContent, "file", file.Name);
                //content.Add(new StringContent(username), "user");
                //content.Add(new StringContent(pc), "pc");
                //content.Add(new StringContent(documentType), "type");

                //var client = new HttpClient();
                //var response = await client.PostAsync("http://localhost:5000/receive-document", content);
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



            ((App)Application.Current).m_window?.ContentFrame.Navigate(typeof(HomePage));            
            
            App.MainWindow.Activate();
        }

        //private void OnForgotPasswordClicked(object sender,RoutedEventArgs e)
        //{
        //    forgotPasswordText.Text = forgotPasswordText.Text + "1";
        //}
        //private void OnSignUpClicked(object sender, RoutedEventArgs e)
        //{
        //    signUpText.Text = signUpText.Text + "2";
        //}
    }
}
