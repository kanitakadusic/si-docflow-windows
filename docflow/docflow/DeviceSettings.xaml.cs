using docflow.Models;
using Microsoft.UI;
using Microsoft.UI.Windowing;
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
using WIA;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using Windows.Storage;
using WinRT.Interop;




namespace docflow
{
    public sealed partial class DeviceSettings : Window
    {
        private bool _initialized = false;
        public DeviceSettings()
        {
            this.InitializeComponent();
            SetWindowSize();
            this.Activated += DeviceSettings_Activated;
        }

        private async void DeviceSettings_Activated(object sender, WindowActivatedEventArgs args)
        {
            this.Activated -= DeviceSettings_Activated;
            if (!_initialized)
            {
                _initialized = true;

                await FindDeviceAsync();
            }
        }
        private async Task FindDeviceAsync()
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

        private async void OnSaveClick(object sender, RoutedEventArgs e)
        {
            var selectedDevice = DevicesComboBox.SelectedItem as InfoDev;
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
    }
}
