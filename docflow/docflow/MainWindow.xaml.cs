using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace docflow
{
    public sealed partial class MainWindow : Window
    {
        private string _watchFolderPath;
        private FileSystemWatcher _fileWatcher;
        private HashSet<string> _fileTypes = new HashSet<string>();
        private List<string> _detectedFiles = new List<string>();

        public MainWindow()
        {
            this.InitializeComponent();
            SetWatchFolderPath();
            EnsureWatchFolderExists();
            InitializeFileWatcher();
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
                    PickAFileOutputTextBlock.Text = $"Created folder: {_watchFolderPath}";
                });
            }
            else
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    PickAFileOutputTextBlock.Text = $"Monitoring folder: {_watchFolderPath}";
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
            if (_fileTypes.Any(ext => e.FullPath.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    _detectedFiles.Add(e.Name);
                    FilesListView.ItemsSource = null;
                    FilesListView.ItemsSource = _detectedFiles;
                    PickAFileOutputTextBlock.Text = $"New file detected: {e.Name}";
                });
            }
        }

        private void Option_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                _fileTypes.Add(checkBox.Content.ToString());
            }
        }

        private void Option_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                _fileTypes.Remove(checkBox.Content.ToString());
            }
        }

        private void SelectAll_Checked(object sender, RoutedEventArgs e)
        {
            pdfOption.IsChecked = true;
            pngOption.IsChecked = true;
            jpgOption.IsChecked = true;
            jpegOption.IsChecked = true;
        }

        private void SelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            pdfOption.IsChecked = false;
            pngOption.IsChecked = false;
            jpgOption.IsChecked = false;
            jpegOption.IsChecked = false;
        }

        private void SelectAll_Indeterminate(object sender, RoutedEventArgs e)
        {
            pdfOption.IsChecked = null;
            pngOption.IsChecked = null;
            jpgOption.IsChecked = null;
            jpegOption.IsChecked = null;
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _watchFolderPath,
                UseShellExecute = true,
                Verb = "open"
            });
        }
    }
}
