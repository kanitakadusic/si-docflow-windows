using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Net.Http;
using Newtonsoft.Json;
using docflow.Services;
using docflow.Utilities;

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
            WindowUtil.MaximizeWindow(this);

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
        private static string url = AppSettings.PROCESSING_SERVER_BASE_URL + "document/finalize";

        private async void OnFinalizeButton(object sender, RoutedEventArgs e)
        {
            var finalizeButton = sender as Button;
            if (finalizeButton != null)
            {
                finalizeButton.IsEnabled = false;
            }


            try
            {
                // Log that a command is being received (finalization)
                await ClientLogService.LogActionAsync(ClientActionType.COMMAND_RECEIVED);

                var finalizedData = new JObject();

                finalizedData["document_type_id"] = int.Parse(_documentTypeId);
                finalizedData["engine"] = AppSettings.OCR_ENGINE;

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
                finalizedData["tripletIds"] = _data[0]["tripletIds"];

                string jsonContent = JsonConvert.SerializeObject(finalizedData);

                using HttpClient client = new();
                HttpContent content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(url, content);

                string responseContent = await response.Content.ReadAsStringAsync();
                JObject jsonObject = JObject.Parse(responseContent);

                await DialogUtil.CreateContentDialog(
                    title: response.IsSuccessStatusCode ? "Success" : "Error",
                    message: jsonObject["message"]?.ToString() ?? "Unexpected server response.",
                    dialogType: response.IsSuccessStatusCode ? DialogType.Success : DialogType.Error,
                    xamlRoot: Content.XamlRoot
                ).ShowAsync();

                var loginPageLoad = new LoginPage();
                loginPageLoad.Activate();

                // Log the application shutdown from this window
                await App.LogApplicationShutdownAsync();

                Close();

            }
            catch (Exception ex)
            {
                await DialogUtil.CreateContentDialog(
                    title: "Error",
                    message: ex.Message,
                    dialogType: DialogType.Error,
                    xamlRoot: Content.XamlRoot
                ).ShowAsync();
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
