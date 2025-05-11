using Microsoft.UI;
using Microsoft.UI.Xaml;
using Newtonsoft.Json.Linq;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.System;
using WinRT.Interop;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using static docflow.LoginPage;
using System.Diagnostics;
using docflow.Services;

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

        private async void OnOpenCameraButton(object sender, RoutedEventArgs e)
        {
            bool hasOpenCameraFailed = false;

            await Task.Run(() =>
            {
                try
                {
                    using var capture = new VideoCapture(0);

                    if (capture.IsOpened())
                    {
                        using var window = new OpenCvSharp.Window("Press SPACE to take photo, ESC to cancel.");
                        using var frame = new Mat();

                        while (true)
                        {
                            try
                            {
                                capture.Read(frame);
                                if (frame.Empty()) continue;

                                window.ShowImage(frame);
                                var key = Cv2.WaitKey(30);

                                if (key == 27)
                                {
                                    break;
                                }
                                else if (key == 32)
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
                            catch (Exception)
                            {

                            }
                        }

                        Cv2.DestroyAllWindows();
                    }
                    else
                    {
                        hasOpenCameraFailed = true;
                    }
                }
                catch (Exception)
                {

                }
            });

            if (hasOpenCameraFailed)
            {
                var dialog = App.CreateContentDialog(
                    title: "Error",
                    message: "Failed to open camera.",
                    xamlRoot: Content.XamlRoot
                );
                await dialog.ShowAsync();
            }
        }

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

            const string url = "https://docflow-server.up.railway.app/document/process?lang=bos&engines=tesseract";

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
    }
}
