using docflow.Models;
using docflow.Models.ApiModels;
using docflow.Services;
using docflow.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WIA;
using Windows.Devices.Enumeration;
using Windows.System;

namespace docflow
{
    public sealed partial class ProcessWindow : Microsoft.UI.Xaml.Window
    {
        private readonly string _user;
        private readonly string _documentTypeId;

        private readonly ApiService _apiService = new();

        private string _watchFolderPath = null!;
        private FileSystemWatcher _fileWatcher = null!;
        private readonly List<string> _detectedDocuments = [];
        private DateTime _lastEventTime = DateTime.MinValue;
        private readonly TimeSpan _eventDebounceTime = TimeSpan.FromSeconds(1);

        public ProcessWindow(string user, string documentTypeId)
        {
            InitializeComponent();
            WindowUtil.MaximizeWindow(this);

            _user = user;
            _documentTypeId = documentTypeId;

            SetWatchFolderPath();
            SetFileWatcher();
        }

        private void SetWatchFolderPath()
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _watchFolderPath = Path.Combine(documentsPath, "FileFolder");

            if (!Directory.Exists(_watchFolderPath))
            {
                Directory.CreateDirectory(_watchFolderPath);
            }
        }

        private void SetFileWatcher()
        {
            _fileWatcher = new FileSystemWatcher
            {
                Path = _watchFolderPath,
                Filter = "*.*",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            _fileWatcher.Created += OnFileCreated;
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            if ((DateTime.Now - _lastEventTime) < _eventDebounceTime)
            {
                return;
            }

            _lastEventTime = DateTime.Now;

            if (!_detectedDocuments.Contains(e.Name, StringComparer.OrdinalIgnoreCase))
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    _detectedDocuments.Add(e.Name);

                    DocumentsComboBox.ItemsSource = null;
                    DocumentsComboBox.ItemsSource = _detectedDocuments;

                    if (DocumentsComboBox.Items.Count > 0)
                    {
                        DocumentsComboBox.SelectedIndex = DocumentsComboBox.Items.Count - 1;
                    }
                });
            }
        }

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            bool hasOpenCameraFailed = false;

            DeviceConfig? savedDevice = await DeviceUtil.LoadSavedDevice();
            if (savedDevice == null || string.IsNullOrEmpty(savedDevice.Name))
            {
                hasOpenCameraFailed = true;
                return;
            }

            if (savedDevice.Device == DeviceType.Camera)
            {
                await Task.Run(async () =>
                {
                    try
                    {
                        string targetName = savedDevice?.Name;
                        if (string.IsNullOrEmpty(targetName))
                        {
                            hasOpenCameraFailed = true;
                            return;
                        }
                        var allDevices = DeviceInformation.FindAllAsync(Windows.Devices.Enumeration.DeviceClass.VideoCapture).AsTask().Result;
                        int targetIndex = -1;

                        for (int i = 0; i < allDevices.Count; i++)
                        {
                            if (allDevices[i].Name == targetName)
                            {
                                targetIndex = i;
                                break;
                            }
                        }

                        if (targetIndex == -1)
                        {
                            hasOpenCameraFailed = true;
                            return;
                        }
                        using var capture = new VideoCapture(targetIndex);
                        if (capture.IsOpened())
                        {
                            using var window = new OpenCvSharp.Window("Press SPACE to take photo, ESC to cancel.");
                            using var frame = new Mat();

                            while (true)
                            {
                                capture.Read(frame);
                                if (frame.Empty()) continue;

                                window.ShowImage(frame);
                                var key = Cv2.WaitKey(30);

                                if (key == 27)
                                    break;

                                if (key == 32)
                                {
                                    string documentName = $"Photo_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                                    string documentPath = Path.Combine(_watchFolderPath, documentName);
                                    Cv2.ImWrite(documentPath, frame);

                                    DispatcherQueue.TryEnqueue(() =>
                                    {
                                        if (!_detectedDocuments.Contains(documentName))
                                        {
                                            _detectedDocuments.Add(documentName);
                                            DocumentsComboBox.ItemsSource = null;
                                            DocumentsComboBox.ItemsSource = _detectedDocuments;
                                        }
                                    });
                                    break;
                                }
                            }

                        }
                        else
                        {
                            hasOpenCameraFailed = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        hasOpenCameraFailed = true;
                    }
                });
            }
            else if (savedDevice.Device == DeviceType.Scanner)
            {
                System.Diagnostics.Debug.WriteLine($"Attempting to scan with: {savedDevice.Name} (ID: {savedDevice.Id})");
                await Task.Run(async () =>
                {
                    try
                    {
                        string scannerDeviceId = savedDevice.Id;
                        string saveFolderPath = _watchFolderPath;

                        var deviceManager = new DeviceManager();
                        Device scanner = null;

                        foreach (WIA.DeviceInfo info in deviceManager.DeviceInfos)
                        {
                            if (info.Type == WiaDeviceType.ScannerDeviceType && info.DeviceID == scannerDeviceId)
                            {
                                scanner = info.Connect();
                                break;
                            }
                        }

                        if (scanner == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Scanner with ID {scannerDeviceId} not found.");
                            hasOpenCameraFailed = true;
                            return;
                        }

                        Item scanItem = scanner.Items[1];


                        Action<IProperties, object, object> SetWIAPropertyLocal = (properties, propName, propValue) =>
                        {
                            try
                            {
                                foreach (Property prop in properties)
                                {
                                    if (prop.Name == propName.ToString() || prop.PropertyID == Convert.ToInt32(propName))
                                    {
                                        prop.set_Value(propValue);
                                        return;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error setting WIA property {propName}: {ex.Message}");
                            }
                        };


                        SetWIAPropertyLocal(scanItem.Properties, "6146", 1);  // WIA_IPA_ITEM_FLAG (1 = Flatbed or Feeder)
                        SetWIAPropertyLocal(scanItem.Properties, "6147", 1);  // WIA_IPA_ACCESS_RIGHTS (1 = Read)
                        SetWIAPropertyLocal(scanItem.Properties, "4104", 4);  // WIA_IPA_DEPTH (4 = Color, 2 = Grayscale, 1 = Black and White)
                        SetWIAPropertyLocal(scanItem.Properties, "6149", 300); // WIA_IPA_DPI_X (Horizontal Resolution - npr. 300 DPI)
                        SetWIAPropertyLocal(scanItem.Properties, "6150", 300); // WIA_IPA_DPI_Y (Vertical Resolution - npr. 300 DPI)
                        SetWIAPropertyLocal(scanItem.Properties, "6154", 0);   // WIA_IPA_XPOS (X-Offset)
                        SetWIAPropertyLocal(scanItem.Properties, "6155", 0);   // WIA_IPA_YPOS (Y-Offset)




                        object image = scanItem.Transfer("{B96B3CA6-0728-11D3-9EB1-00C04F72D991}");

                        var imageFile = (ImageFile)image;

                        string fileName = $"ScannedDoc_{DateTime.Now:yyyyMMdd_HHmmss}.jpeg";
                        string fullFilePath = Path.Combine(saveFolderPath, fileName);

                        imageFile.SaveFile(fullFilePath);

                        System.Diagnostics.Debug.WriteLine($"Document scanned and saved to: {fullFilePath}");


                        DispatcherQueue.TryEnqueue(() =>
                        {

                            if (!_detectedDocuments.Contains(fileName, StringComparer.OrdinalIgnoreCase))
                            {
                                _detectedDocuments.Add(fileName);
                                DocumentsComboBox.ItemsSource = null;
                                DocumentsComboBox.ItemsSource = _detectedDocuments;
                                if (DocumentsComboBox.Items.Count > 0)
                                {
                                    DocumentsComboBox.SelectedIndex = DocumentsComboBox.Items.Count - 1;
                                }
                            }

                        });
                    }
                    catch (System.Runtime.InteropServices.COMException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"WIA Scan Error: {ex.Message} (HRESULT: {ex.ErrorCode})");
                        hasOpenCameraFailed = true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"General Scan Error: {ex.Message}");
                        hasOpenCameraFailed = true;
                    }
                });
            }
            if (hasOpenCameraFailed)
            {
                await DialogUtil.CreateContentDialog(
                    title: "Error",
                    message: "Device not found",
                    dialogType: DialogType.Error,
                    xamlRoot: Content.XamlRoot
                ).ShowAsync();
            }
        }

        private async void ProcessButton_Click(object sender, RoutedEventArgs e)
        {
            var processButton = sender as Button;
            if (processButton != null)
            {
                processButton.IsEnabled = false;
            }

            LoadingRing.IsActive = true;
            LoadingRing.Visibility = Visibility.Visible;

            try
            {
                string? selectedDocumentName = DocumentsComboBox.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(selectedDocumentName))
                {
                    await DialogUtil.CreateContentDialog(
                        title: "Missing document",
                        message: "Please select the document you would like to process.",
                        dialogType: DialogType.Warning,
                        xamlRoot: Content.XamlRoot
                    ).ShowAsync();
                    return;
                }

                string selectedDocumentPath = Path.Combine(_watchFolderPath, selectedDocumentName);
                if (!File.Exists(selectedDocumentPath))
                {
                    await DialogUtil.CreateContentDialog(
                        title: "File not found",
                        message: $"Selected document '{selectedDocumentName}' does not exist.",
                        dialogType: DialogType.Error,
                        xamlRoot: Content.XamlRoot
                    ).ShowAsync();
                    return;
                }

                ProcessDocumentResponse? result = await _apiService.ProcessDocumentAsync(
                    selectedDocumentPath,
                    _user,
                    _documentTypeId
                );
                if (result?.Data != null)
                {
                    await DialogUtil.CreateContentDialog(
                        title: "Success",
                        message: "The document has been successfully processed.",
                        dialogType: DialogType.Success,
                        xamlRoot: Content.XamlRoot
                    ).ShowAsync();

                    var finalizeWindow = new FinalizeWindow(result.Data);
                    finalizeWindow.Activate();
                    Close();
                }
                else
                {
                    await DialogUtil.CreateContentDialog(
                        title: "Error",
                        message: "An error occured while processing the document.",
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
                if (processButton != null)
                {
                    processButton.IsEnabled = true;
                }

                LoadingRing.IsActive = false;
                LoadingRing.Visibility = Visibility.Collapsed;
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var devicesWindow = new DevicesWindow();
            devicesWindow.Activate();
        }
    }
}
