using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Net.Http;
using System.Text.Json;

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