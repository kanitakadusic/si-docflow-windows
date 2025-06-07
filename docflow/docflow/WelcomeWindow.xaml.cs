using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;
using docflow.Utilities;
using docflow.Services;
using docflow.Models.ApiModels;

namespace docflow
{
    public sealed partial class WelcomeWindow : Microsoft.UI.Xaml.Window
    {
        private readonly ApiService _apiService = new(AppSettings.PROCESSING_SERVER_BASE_URL);
        
        private List<DocumentType> _fetchedDocumentTypes = [];

        public static bool HasDevicesWindowBeenShownThisSession { get; set; } = false;

        public WelcomeWindow()
        {
            InitializeComponent();
            WindowUtil.MaximizeWindow(this);

            FetchDocumentTypesAsync();

            this.Closed += WelcomeWindow_Closed;
        }

        private async void WelcomeWindow_Closed(object sender, WindowEventArgs args)
        {
            await App.LogApplicationShutdownAsync();
        }

        private async void FetchDocumentTypesAsync()
        {
            try
            {
                FetchDocumentTypesResponse? result = await _apiService.FetchDocumentTypesAsync();
                if (result?.Data != null)
                {
                    _fetchedDocumentTypes = result.Data;

                    foreach (var documentType in result.Data)
                    {
                        ComboBoxItem item = new()
                        {
                            Content = documentType.Name
                        };
                        DocumentTypesList.Items.Add(item);
                    }
                }
                else
                {
                    await DialogUtil.CreateContentDialog(
                        title: "No document types found",
                        message: "It looks like there are no document types available at the moment. Please check back later or contact support if the issue persists.",
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
        }

        private async void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            var continueButton = sender as Button;
            if (continueButton != null)
            {
                continueButton.IsEnabled = false;
            }

            try
            {
                string? user = UserTextBox.Text;
                if (string.IsNullOrEmpty(user))
                {
                    await DialogUtil.CreateContentDialog(
                        title: "Missing input",
                        message: "Please fill in your name to continue.",
                        dialogType: DialogType.Warning,
                        xamlRoot: Content.XamlRoot
                    ).ShowAsync();
                    return;
                }

                string? documentType = (DocumentTypesList.SelectedItem as ComboBoxItem)?.Content?.ToString();
                if (string.IsNullOrEmpty(documentType))
                {
                    await DialogUtil.CreateContentDialog(
                        title: "Selection required",
                        message: "Please select a document type from the dropdown list to continue.",
                        dialogType: DialogType.Warning,
                        xamlRoot: Content.XamlRoot
                    ).ShowAsync();
                    return;
                }

                DocumentType? selectedDocumentType = _fetchedDocumentTypes.FirstOrDefault(dt => dt.Name == documentType);
                if (selectedDocumentType == null)
                {
                    await DialogUtil.CreateContentDialog(
                        title: "Error",
                        message: "Selected document type not found in fetched document types.",
                        dialogType: DialogType.Error,
                        xamlRoot: Content.XamlRoot
                    ).ShowAsync();
                    return;
                }

                var processWindow = new ProcessWindow(user, selectedDocumentType.Id.ToString());
                processWindow.Activate();
                Close();

                if (HasDevicesWindowBeenShownThisSession == false)
                {
                    var devicesWindow = new DevicesWindow();
                    devicesWindow.Activate();
                    HasDevicesWindowBeenShownThisSession = true;
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
                if (continueButton != null)
                {
                    continueButton.IsEnabled = true;
                }
            }
        }
    }
}