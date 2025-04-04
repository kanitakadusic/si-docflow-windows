using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Storage.Pickers;

namespace docflow
{
    public sealed partial class MainWindow : Window
    {
        private string _fileType = string.Empty; // Default file type
        public MainWindow()
        {
            this.InitializeComponent();
        }

        private async void PickAFileButton_Click(object sender, RoutedEventArgs e)
        {
            var senderButton = sender as Button;
            senderButton.IsEnabled = false;

            PickAFileOutputTextBlock.Text = "";

            var openPicker = new Windows.Storage.Pickers.FileOpenPicker();

            var window = App.MainWindow;

            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.FileTypeFilter.Add(_fileType); // Add other file types as needed
            openPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;

            var file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                PickAFileOutputTextBlock.Text = "Picked file: " + file.Name;
                //TODO: implement logic for file processing
            }
            else
            {
                PickAFileOutputTextBlock.Text = "Operation cancelled.";
            }

            senderButton.IsEnabled = true;
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {

            var menuItem = sender as MenuFlyoutItem;
            if (menuItem != null)
            {
                DropDownButton.Content = "Pick a file type";
                _fileType = menuItem.Text;
                DropDownButton.Content = menuItem.Text;
                PickAFileButton.IsEnabled = true;
            }

        }
    }
}
