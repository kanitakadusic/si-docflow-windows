using docflow.Models;
using System;
using System.Collections.Generic;
using System.IO;
using WIA;
using Windows.Devices.Enumeration;
using System.Text.Json;
using System.Threading.Tasks;

namespace docflow.Utilities
{
    public static class DeviceUtil
    {
        public static async Task<List<DeviceConfig>> FindDevicesAsync()
        {
            var allVideoDevicesInfo = await DeviceInformation.FindAllAsync(Windows.Devices.Enumeration.DeviceClass.VideoCapture);
            DeviceManager deviceManager = new();

            List<DeviceConfig> deviceList = [];

            foreach (var device in allVideoDevicesInfo)
            {
                deviceList.Add(new DeviceConfig(device.Id, device.Name, DeviceType.Camera));
            }
            for (int i = 1; i <= deviceManager.DeviceInfos.Count; i++) // WIA is 1-based
            {
                DeviceInfo info = deviceManager.DeviceInfos[i];
                if (info.Type == WiaDeviceType.ScannerDeviceType)
                {
                    deviceList.Add(new DeviceConfig(
                        info.DeviceID,
                        info.Properties["Name"].get_Value().ToString(),
                        DeviceType.Scanner
                    ));
                }
            }

            return deviceList;
        }

        public static async Task SaveDeviceAsync(DeviceConfig device)
        {
            string jsonString = JsonSerializer.Serialize(device);

            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(folderPath, "docflow");
            Directory.CreateDirectory(appFolder);

            string fullPath = Path.Combine(appFolder, "image-capturing-device.json");
            await File.WriteAllTextAsync(fullPath, jsonString);

            System.Diagnostics.Debug.WriteLine($"Image capturing device saved at: {fullPath}");
        }

        public static async Task<DeviceConfig?> LoadSavedDevice()
        {
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(folderPath, "docflow");
            string fullPath = Path.Combine(appFolder, "image-capturing-device.json");

            if (!File.Exists(fullPath))
            {
                return null;
            }

            string jsonString = await File.ReadAllTextAsync(fullPath);
            return JsonSerializer.Deserialize<DeviceConfig>(jsonString);
        }
    }
}
