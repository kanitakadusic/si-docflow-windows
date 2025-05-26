using docflow.Models;
using docflow.Services;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using WIA;
using Windows.Devices.Enumeration;
using Windows.Graphics;
using Windows.Storage;
using Windows.System;
using WinRT.Interop;
using static docflow.LoginPage;

namespace docflow
{
    public sealed partial class MainWindow : Microsoft.UI.Xaml.Window
    {
        private readonly string _username;
        private readonly string _documentType;
        private readonly string _documentTypeId;

        private HashSet<string> _documentTypes = new(StringComparer.OrdinalIgnoreCase);
        private string _watchFolderPath = null!;
        private FileSystemWatcher _fileWatcher = null!;

        private readonly List<string> _detectedDocuments = [];
        private DateTime _lastEventTime = DateTime.MinValue;
        private readonly TimeSpan _eventDebounceTime = TimeSpan.FromSeconds(1);

        public MainWindow(string username, string documentType, string documentTypeId)
        {
            InitializeComponent();
            SetWindowSize();
            _username = username;
            _documentType = documentType;
            _documentTypeId = documentTypeId;

            this.Closed += MainWindow_Closed;

            AddExtensions();
            SetWatchFolderPath();
            SetFileWatcher();
            //ProcessingResults.Visibility = Visibility.Collapsed;
            //EnglishButton.IsChecked = true;
            //TesseractButton.IsChecked = true;

        }
        private async void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            await App.LogApplicationShutdownAsync();
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

        private void AddExtensions()
        {
            _documentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".pdf", ".jpg", ".jpeg", ".png"
            };
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

            if (
                _documentTypes.Any(ext => e.FullPath.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) &&
                !_detectedDocuments.Contains(e.Name, StringComparer.OrdinalIgnoreCase)
            )
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

        private async void OnScanButton(object sender, RoutedEventArgs e)
        {
            bool hasOpenCameraFailed = false;

             string path = Path.Combine(AppContext.BaseDirectory, "deviceSettings.json");
            if (!File.Exists(path))
            {
                hasOpenCameraFailed = true;
                return;
            }

            string jsonString = File.ReadAllText(path);
            var savedDevice = JsonSerializer.Deserialize<InfoDev>(jsonString);
            if (savedDevice == null || string.IsNullOrEmpty(savedDevice.Name))
            {
                hasOpenCameraFailed = true;
                return;
            }
            if (savedDevice.Device == DeviceTYPE.Camera)
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
            }else if(savedDevice.Device == DeviceTYPE.Scanner)
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
                var dialog = App.CreateContentDialog(
                    title: "Error",
                    message: "Device not found",
                    xamlRoot: Content.XamlRoot
                );
                await dialog.ShowAsync();
            }
        }
        private static string url = AppConfig.docflow_api + "document/process?lang=bos&engines=googleVision";

        private static string GetMimeTypeFromExtension(string extension)
        {
            return extension.ToLower() switch
            {
                ".pdf" => "application/pdf",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }

        private async void OnSubmitButton(object sender, RoutedEventArgs e)
        {
            var submitButton = sender as Button;
            if (submitButton != null)
            {
                submitButton.IsEnabled = false;
            }
            LoadingRing.IsActive = true;
            LoadingRing.Visibility = Visibility.Visible;


            /*
            string lang = "";
            if (EnglishButton.IsChecked == true) lang = "eng";
            if (BosnianButton.IsChecked == true) lang = "bos";*/


            try
            {
                var selectedDocument = DocumentsComboBox.SelectedItem;

                if (selectedDocument != null)
                {
                    using var form = new MultipartFormDataContent();

                    string selectedDocumentName = selectedDocument.ToString()!;
                    string selectedDocumentPath = Path.Combine(_watchFolderPath, selectedDocumentName);
                    if (File.Exists(selectedDocumentPath))
                    {
                        string mimeType = GetMimeTypeFromExtension(Path.GetExtension(selectedDocumentPath));
                        var selectedDocumentContent = new ByteArrayContent(File.ReadAllBytes(selectedDocumentPath));
                        selectedDocumentContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
                        form.Add(selectedDocumentContent, "file", selectedDocumentName);

                    }                    
                    form.Add(new StringContent(_username), "user");
                    form.Add(new StringContent(Environment.MachineName), "machineId");
                    form.Add(new StringContent(_documentTypeId), "documentTypeId");

                    await ClientLogService.LogActionAsync(ClientActionType.PROCESSING_REQ_SENT);

                    using HttpClient client = new();
                    HttpResponseMessage response = await client.PostAsync(url, form);

                    string responseContent = await response.Content.ReadAsStringAsync();
                    JObject jsonObject;
                    try
                    {
                        jsonObject = JObject.Parse(responseContent);
                    }
                    catch (Exception)
                    {
                        await App.CreateContentDialog(
                           title: "Error",
                           message: "The document cannot be processed at the moment.",
                           xamlRoot: Content.XamlRoot,
                           isError: true
                       ).ShowAsync();

                        var loginPage = new LoginPage();
                        loginPage.Activate();
                        Close();
                        return;
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        var dataPart = jsonObject["data"];
                        if (dataPart == null)
                        {
                            await App.CreateContentDialog(
                                title: "Error",
                                message: "The server did not return any data.",
                                xamlRoot: Content.XamlRoot,
                                isError: true
                            ).ShowAsync();
                            return;
                        }

                        var dialog = App.CreateContentDialog(
                        title: "Success",
                        message: jsonObject["message"]?.ToString() ?? "Unexpected server response.",
                        xamlRoot: Content.XamlRoot,
                        isError: false
                         );
                        await dialog.ShowAsync();

                        var processResults = new ProcessResults(dataPart, _documentTypeId);
                        processResults.Activate();

                        Close();
                    }
                    else 
                    {
                        await App.CreateContentDialog(
                           title: "Error",
                           message: jsonObject["message"]?.ToString() ?? "Unexpected server response.",
                           xamlRoot: Content.XamlRoot,
                           isError: true
                       ).ShowAsync();

                        var loginPage = new LoginPage();
                        loginPage.Activate();
                        Close();
                        return;
                    }
                }
                else
                {
                    var dialog = App.CreateContentDialog(
                        title: "Missing document",
                        message: "Please select the document you would like to process.",
                        xamlRoot: Content.XamlRoot
                    );
                    await dialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                var dialog = App.CreateContentDialog(
                    title: "Error",
                    message: ex.Message,
                    xamlRoot: Content.XamlRoot
                );
                await dialog.ShowAsync();
            }
            finally
            {
                if (submitButton != null)
                {
                    submitButton.IsEnabled = true;
                }
                LoadingRing.IsActive = false;
                LoadingRing.Visibility = Visibility.Collapsed;

            }
        }

        private void OnSettingsClick(object sender, RoutedEventArgs e)
        {
            var deviceSettings = new DeviceSettings();
            deviceSettings.Activate();
        }
    }
}
