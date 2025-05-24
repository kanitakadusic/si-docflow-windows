using docflow.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using Windows.Storage;




namespace docflow
{
    public sealed partial class DeviceSettings : Window
    {
        private bool _initialized = false;
        public DeviceSettings()
        {
            this.InitializeComponent();
            this.Activated += DeviceSettings_Activated;
        }

        private async void DeviceSettings_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (!_initialized)
            {
                _initialized = true;

                await FindDeviceAsync();
            }
        }
        private async Task FindDeviceAsync()
        {
            var allVideoDevicesInfo = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            List<DeviceInfo> deviceList = new List<DeviceInfo>();

            foreach (var device in allVideoDevicesInfo)
            {
                deviceList.Add(new DeviceInfo(device.Id, device.Name, 0));
            }

            var allScannerDevicesInfo = await DeviceInformation.FindAllAsync(DeviceClass.ImageScanner);

            foreach (var deviceInfo in allScannerDevicesInfo)
            {
                deviceList.Add(new DeviceInfo(deviceInfo.Id, deviceInfo.Name, 1));
            }

            DevicesComboBox.ItemsSource = deviceList;
            DevicesComboBox.SelectedIndex = 0;
        }

        private async void OnSaveClick(object sender, RoutedEventArgs e)
        {
            var selectedDevice = DevicesComboBox.SelectedItem as DeviceInfo;
            if (selectedDevice != null)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"Saving device: {selectedDevice?.Name} ({selectedDevice?.Id})");

                    string jsonString = JsonSerializer.Serialize(selectedDevice);
                    string folderPath = AppContext.BaseDirectory;
                    string fullPath = Path.Combine(folderPath, "deviceSettings.json");
                    File.WriteAllText(fullPath, jsonString);
                    System.Diagnostics.Debug.WriteLine($"JSON file saved at: {fullPath}");

                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving device settings: {ex.Message}");
                }
            }
            this.Close();
        }

        private async void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            await FindDeviceAsync();
        }
    }
}
