using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Graphics;
using Windows.System;
using WinRT.Interop;

namespace docflow
{
    public class UserData
    {
        public string Username { get; }
        public string DocumentType { get; }

        public UserData(string username, string documentType)
        {
            Username = username;
            DocumentType = documentType;
        }
    }
    public sealed partial class MainWindow : Microsoft.UI.Xaml.Window
    {
        private string _watchFolderPath;
        private FileSystemWatcher _fileWatcher;
        private HashSet<string> _documentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private List<string> _detectedDocuments = new List<string>();
        private DateTime _lastEventTime = DateTime.MinValue;
        private readonly TimeSpan _eventDebounceTime = TimeSpan.FromSeconds(1);
        private UserData userData;

        public MainWindow()
        {
            this.InitializeComponent();
            SetWindowSize();
            SetWatchFolderPath();
            EnsureWatchFolderExists();
            InitializeFileWatcher();
        }
        public MainWindow(string username, string typeDoc)
        {
            this.InitializeComponent();
            userData = new UserData(username, typeDoc);
            SetWindowSize();
            SetWatchFolderPath();
            EnsureWatchFolderExists();
            InitializeFileWatcher();
        }

        private void SetWindowSize()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new SizeInt32(1600, 1000));
        }

        private void SetWatchFolderPath()
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _watchFolderPath = Path.Combine(documentsPath, "FileFolder");
        }

        private void EnsureWatchFolderExists()
        {
            if (!Directory.Exists(_watchFolderPath))
            {
                Directory.CreateDirectory(_watchFolderPath);
                DispatcherQueue.TryEnqueue(() =>
                {
                    FolderTextBlock.Text = "Created folder:";
                    PathTextBlock.Text = _watchFolderPath;
                });
            }
            else
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    FolderTextBlock.Text = "Monitoring folder:";
                    PathTextBlock.Text = _watchFolderPath;
                });
            }
        }

        private void InitializeFileWatcher()
        {
            _fileWatcher = new FileSystemWatcher
            {
                Path = _watchFolderPath,
                Filter = "*.*",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
            };

            _fileWatcher.Created += OnFileCreated;
            _fileWatcher.EnableRaisingEvents = true;
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

        private void OptionChecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                _documentTypes.Add(checkBox.Content.ToString());
            }
        }

        private void OptionUnchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                _documentTypes.Remove(checkBox.Content.ToString());
            }
        }

        private void AllChecked(object sender, RoutedEventArgs e)
        {
            if (pdfCheckBox != null) pdfCheckBox.IsChecked = true;
            if (pngCheckBox != null) pngCheckBox.IsChecked = true;
            if (jpgCheckBox != null) jpgCheckBox.IsChecked = true;
            if (jpegCheckBox != null) jpegCheckBox.IsChecked = true;
        }

        private void AllUnchecked(object sender, RoutedEventArgs e)
        {
            pdfCheckBox.IsChecked = false;
            pngCheckBox.IsChecked = false;
            jpgCheckBox.IsChecked = false;
            jpegCheckBox.IsChecked = false;
        }

        private void AllIndeterminate(object sender, RoutedEventArgs e)
        {
            pdfCheckBox.IsChecked = null;
            pngCheckBox.IsChecked = null;
            jpgCheckBox.IsChecked = null;
            jpegCheckBox.IsChecked = null;
        }

        private void OnPathHyperlinkButton(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _watchFolderPath,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        private async void OnOpenCameraButton(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                try
                {
                    using var capture = new VideoCapture(0);

                    if (!capture.IsOpened())
                    {
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            CameraTextBlock.Text = "Failed to open camera.";
                        });

                        return;
                    }

                    using var window = new OpenCvSharp.Window("Camera preview");
                    using var frame = new Mat();

                    DispatcherQueue.TryEnqueue(() =>
                    {
                        CameraTextBlock.Text = "Press SPACE to take photo, ESC to cancel.";
                    });

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
                                DispatcherQueue.TryEnqueue(() =>
                                {
                                    CameraTextBlock.Text = "Photo capture cancelled.";
                                });

                                break;
                            }
                            else if (key == 32)
                            {
                                string documentName = $"Photo_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                                string documentPath = Path.Combine(_watchFolderPath, documentName);

                                Cv2.ImWrite(documentPath, frame);

                                DispatcherQueue.TryEnqueue(() =>
                                {
                                    CameraTextBlock.Text = $"Photo saved: {documentName}";
                                    //Process.Start(new ProcessStartInfo(documentPath) { UseShellExecute = true });

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
                catch (Exception)
                {
                    /*
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        CameraTextBlock.Text = $"Camera error: {ex.Message}";
                    });
                    */
                }
            });
        }

        private async void OnSubmitClickButton(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if (button != null)
            {
                button.IsEnabled = false;
            }
            try
            {
                if (DocumentsComboBox.SelectedItem != null)
                {
                    string apiUrl = "https://docflow-server.up.railway.app/document";
                    var requestData = new
                    {
                        user = userData.Username,
                        pc = Environment.MachineName,
                        type = userData.DocumentType
                    };

                    using (HttpClient client = new HttpClient())
                    {
                        using (var form = new MultipartFormDataContent())
                        {
                            form.Add(new StringContent(requestData.user), "user");
                            form.Add(new StringContent(requestData.pc), "pc");
                            form.Add(new StringContent(requestData.type), "type");

                            string selectedDocument = DocumentsComboBox.SelectedItem.ToString();
                            string filePath = Path.Combine(_watchFolderPath, selectedDocument);
                            if (System.IO.File.Exists(filePath))
                            {
                                string fileName = System.IO.Path.GetFileName(filePath);
                                var fileContent = new ByteArrayContent(System.IO.File.ReadAllBytes(filePath));
                                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                                form.Add(fileContent, "file", fileName);
                            }

                            HttpResponseMessage response = await client.PostAsync(apiUrl, form);
                            string responseContent = await response.Content.ReadAsStringAsync();
                            string message = $"Response: {responseContent}";

                            var dlg = new Microsoft.UI.Xaml.Controls.ContentDialog
                            {
                                Title = response.IsSuccessStatusCode ? "Success" : "Error",
                                Content = message,
                                CloseButtonText = "OK",
                                XamlRoot = this.Content.XamlRoot,
                            };

                            dlg.DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Close;
                            await dlg.ShowAsync();
                        }
                    }
                }
                else
                {
                    var dlg2 = new Microsoft.UI.Xaml.Controls.ContentDialog
                    {
                        Title = "Error",
                        Content = "Morate odabrati dokument",
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot,
                    };

                    dlg2.DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Close;
                    await dlg2.ShowAsync();
                }
            }
            finally
            {
                if (button != null)
                {
                    button.IsEnabled = true;
                }
            }
        }
    }
}
