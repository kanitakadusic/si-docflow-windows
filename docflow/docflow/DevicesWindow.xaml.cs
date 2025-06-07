using docflow.Models;
using docflow.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

namespace docflow
{
    public sealed partial class DevicesWindow : Microsoft.UI.Xaml.Window
    {
        public DevicesWindow()
        {
            InitializeComponent();
            WindowUtil.MaximizeWindow(this);

            FindDevicesAsync();
        }

        private async void FindDevicesAsync()
        {
            try
            {
                List<DeviceConfig> deviceList = await DeviceUtil.FindDevicesAsync();
                DevicesComboBox.ItemsSource = deviceList;
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

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            FindDevicesAsync();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var saveButton = sender as Button;
            if (saveButton != null)
            {
                saveButton.IsEnabled = false;
            }

            try
            {
                if (DevicesComboBox.SelectedItem is DeviceConfig selectedDevice)
                {
                    await DeviceUtil.SaveDeviceAsync(selectedDevice);
                    this.Close();
                }
                else
                {
                    await DialogUtil.CreateContentDialog(
                        title: "Missing device",
                        message: "Please select an image capturing device.",
                        dialogType: DialogType.Warning,
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
                if (saveButton != null)
                {
                    saveButton.IsEnabled = true;
                }
            }
        }
    }
}
