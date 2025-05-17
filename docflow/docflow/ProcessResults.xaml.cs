using System;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;
using Windows.Graphics;
using WinRT.Interop;
using System.Collections.ObjectModel;
using System.Net.Http;
using Newtonsoft.Json;
using docflow.Services;

namespace docflow
{
    public sealed partial class ProcessResults : Microsoft.UI.Xaml.Window
    {
        private ObservableCollection<FieldResult> _fieldResults;
        private readonly JToken _data;
        private readonly string _documentTypeId;

        public ProcessResults(JToken data, string documentTypeId)
        {
            _data = data; 
            _fieldResults = new ObservableCollection<FieldResult>();
            _documentTypeId = documentTypeId;
            InitializeComponent();
            SetWindowSize();
            
            this.Closed += ProcessResults_Closed;
            
            _ = ClientLogService.LogActionAsync(ClientActionType.PROCESSING_RESULT_RECEIVED);
            
            ShowResults();
        }

        private async void ProcessResults_Closed(object sender, WindowEventArgs args)
        {
            await App.LogApplicationShutdownAsync();
        }

        public class FieldResult
        {
            public string Name { get; set; }
            public string Value { get; set; }

        }

        private void SetWindowSize()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            var (width, height) = App.GetPrimaryScreenSize();
            appWindow.Resize(new SizeInt32(width, height));

            appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
            OverlappedPresenter presenter = (OverlappedPresenter)appWindow.Presenter;
            presenter.Maximize();
        }

        private void ShowResults()
        {
            var dataArray = _data as JArray;
            if (dataArray == null || dataArray.Count == 0 || dataArray[0]["ocr"] is not JArray ocrArray)
                return;

            foreach (var item in _data[0]["ocr"])
            {
                var field = item["field"];
                var result = item["result"];

                var fieldResult = new FieldResult
                {
                    Name = field["name"].ToString() + ": ",
                    Value = result["text"].ToString()
                };

                _fieldResults.Add(fieldResult); 
            }

            ResultsListView.ItemsSource = _fieldResults;

        }

        private async void OnFinalizeButton(object sender, RoutedEventArgs e)
        {
            var finalizeButton = sender as Button;
            if (finalizeButton != null)
            {
                finalizeButton.IsEnabled = false;
            }

            const string url = "https://si-docflow-server.up.railway.app/document/finalize";

            try
            {
                // Log that a command is being received (finalization)
                await ClientLogService.LogActionAsync(ClientActionType.COMMAND_RECEIVED);
                
                var finalizedData = new JObject();

                finalizedData["document_type_id"] = int.Parse(_documentTypeId);
                finalizedData["engine"] = "tesseract";

                var ocrArray = _data[0]["ocr"] as JArray;

                if (ocrArray != null)
                {
                    for (int i = 0; i < ocrArray.Count && i < _fieldResults.Count; i++)
                    {
                        var userEditedValue = _fieldResults[i].Value;
                        if (userEditedValue != ocrArray[i]["result"]["text"].ToString())
                        {
                            ocrArray[i]["result"]["text"] = userEditedValue;
                            ocrArray[i]["result"]["is_corrected"] = true;
                        }
                        else
                        {
                            ocrArray[i]["result"]["is_corrected"] = false;
                        }
                    }
                }

                finalizedData["ocr"] = ocrArray;

                string jsonContent = JsonConvert.SerializeObject(finalizedData);

                using HttpClient client = new();
                HttpContent content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(url, content);

                string responseContent = await response.Content.ReadAsStringAsync();
                JObject jsonObject = JObject.Parse(responseContent);

                var dialog = App.CreateContentDialog(
                    title: response.IsSuccessStatusCode ? "Success" : "Error",
                    message: jsonObject["message"]?.ToString() ?? "Unexpected server response.",
                    xamlRoot: Content.XamlRoot,
                    isError: !response.IsSuccessStatusCode
                );                await dialog.ShowAsync();

                var loginPageLoad = new LoginPage();
                loginPageLoad.Activate();
                
                // Log the application shutdown from this window
                await App.LogApplicationShutdownAsync();
                
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
                if (finalizeButton != null)
                {
                    finalizeButton.IsEnabled = true;
                }
            }
        }

    }
}
