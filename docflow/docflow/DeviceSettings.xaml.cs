using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;



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

                FindCameraDeviceAsync();
            }
        }
        private async void FindCameraDeviceAsync()
        {
            var allVideoDevicesInfo = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);


            List<string> cameraNames = new List<string>();

            if (allVideoDevicesInfo.Any())
            {
                foreach (var device in allVideoDevicesInfo)
                {
                    cameraNames.Add(device.Name); 
                }
            }
            else
            {

            }

            if (cameraNames.Any())
            {
                DevicesComboBox.ItemsSource = cameraNames;
                DevicesComboBox.SelectedIndex = 0; 
            }
            else
            {
                DevicesComboBox.ItemsSource = new List<string> { "Nijedan uredaj nije dostupan." };
                DevicesComboBox.SelectedIndex = 0;
            }

        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            FindCameraDeviceAsync();
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            FindCameraDeviceAsync();
        }
    }
}
