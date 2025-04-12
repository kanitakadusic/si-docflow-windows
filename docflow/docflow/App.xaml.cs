using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;

namespace docflow
{
    public partial class App : Application
    {
        public static MainWindow MainWindow = new();
        public static LoginPage LoginWindow = new();
        public static string ComputerName = Environment.MachineName;
        public static string UserName = string.Empty;

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        public App() => this.InitializeComponent();

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            LoginWindow.Activate();
        }



        public static (int Width, int Height) GetPrimaryScreenSize()
        {
            int width = GetSystemMetrics(SM_CXSCREEN);
            int height = GetSystemMetrics(SM_CYSCREEN);
            return (width, height);
        }
    }
}
