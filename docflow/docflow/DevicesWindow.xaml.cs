using docflow.Models;
using docflow.Utilities;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using WIA;
using Windows.Devices.Enumeration;

namespace docflow
{
    public sealed partial class DevicesWindow : Window
    {
        private bool _initialized = false;
        public DevicesWindow()
        {
            this.InitializeComponent();
            WindowUtil.MaximizeWindow(this);
            this.Activated += DevicesWindow_Activated;
        }

        private async void DevicesWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            this.Activated -= DevicesWindow_Activated;
            if (!_initialized)
            {
                _initialized = true;

                await FindDeviceAsync();
            }
        }

        private async Task FindDeviceAsync()
        {
            try
            {
                var allVideoDevicesInfo = await DeviceInformation.FindAllAsync(Windows.Devices.Enumeration.DeviceClass.VideoCapture);
                List<InfoDev> deviceList = new List<InfoDev>();

                foreach (var device in allVideoDevicesInfo)
                {
                    deviceList.Add(new InfoDev(device.Id, device.Name, DeviceTYPE.Camera));
                }
                DeviceManager deviceManager = new DeviceManager();

                for (int i = 1; i <= deviceManager.DeviceInfos.Count; i++) // WIA is 1-based
                {
                    DeviceInfo info = deviceManager.DeviceInfos[i];
                    if (info.Type == WiaDeviceType.ScannerDeviceType)
                    {
                        string name = info.Properties["Name"].get_Value().ToString();
                        string id = info.DeviceID;
                        deviceList.Add(new InfoDev(id, name, DeviceTYPE.Scanner));
                    }
                }

                DevicesComboBox.ItemsSource = deviceList;
                DevicesComboBox.SelectedIndex = 0;
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

        private async void OnSaveClick(object sender, RoutedEventArgs e)
        {
            var selectedDevice = DevicesComboBox.SelectedItem as InfoDev;
            if (selectedDevice != null)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"Saving device: {selectedDevice?.Name} ({selectedDevice?.Id})");
                    string jsonString = JsonSerializer.Serialize(selectedDevice);
                    string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string appFolder = Path.Combine(folderPath, "docflow");
                    Directory.CreateDirectory(appFolder);

                    string fullPath = Path.Combine(appFolder, "DevicesWindow.json");
                    File.WriteAllText(fullPath, jsonString);
                    System.Diagnostics.Debug.WriteLine($"JSON file saved at: {fullPath}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving device settings: {ex.Message}");

                    await DialogUtil.CreateContentDialog(
                        title: "Error",
                        message: ex.Message,
                        dialogType: DialogType.Error,
                        xamlRoot: Content.XamlRoot
                    ).ShowAsync();
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
