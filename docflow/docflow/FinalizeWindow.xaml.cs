using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using docflow.Services;
using docflow.Utilities;
using docflow.Models.ApiModels;

namespace docflow
{
    public sealed partial class FinalizeWindow : Microsoft.UI.Xaml.Window
    {
        private readonly ProcessDocumentResult _processResult;

        private readonly ApiService _apiService = new(AppSettings.PROCESSING_SERVER_BASE_URL);

        private readonly ObservableCollection<NameText> _nameTextFields = [];

        public FinalizeWindow(ProcessDocumentResult processResult)
        {
            InitializeComponent();
            WindowUtil.MaximizeWindow(this);

            _processResult = processResult;

            ShowProcessingResults();
        }

        public class NameText
        {
            public string Name { get; set; } = string.Empty;
            public string Text { get; set; } = string.Empty;
        }

        private void ShowProcessingResults()
        {
            foreach (MappedOcrResult ocr in _processResult.Ocr)
            {
                _nameTextFields.Add(new NameText
                {
                    Name = ocr.Field.Name + ": ",
                    Text = ocr.Result.Text
                });
            }

            ResultsListView.ItemsSource = _nameTextFields;
        }

        private async void FinalizeButton_Click(object sender, RoutedEventArgs e)
        {
            var finalizeButton = sender as Button;
            if (finalizeButton != null)
            {
                finalizeButton.IsEnabled = false;
            }

            try
            {
                int length = Math.Min(_processResult.Ocr.Count, _nameTextFields.Count);
                for (int i = 0; i < length; i++)
                {
                    _processResult.Ocr[i].Result.Text = _nameTextFields[i].Text;
                }

                bool success = await _apiService.FinalizeDocumentAsync(_processResult);
                if (success)
                {
                    await DialogUtil.CreateContentDialog(
                        title: "Success",
                        message: "The document has been successfully finalized.",
                        dialogType: DialogType.Success,
                        xamlRoot: Content.XamlRoot
                    ).ShowAsync();

                    Close();
                }
                else
                {
                    await DialogUtil.CreateContentDialog(
                        title: "Error",
                        message: "An error occured while finalizing the document.",
                        dialogType: DialogType.Error,
                        xamlRoot: Content.XamlRoot
                    ).ShowAsync();
                }
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
