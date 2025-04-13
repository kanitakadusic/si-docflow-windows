using Microsoft.UI.Xaml;
using System;

namespace docflow
{
    public partial class App : Application
    {
        public static MainWindow MainWindow = new();
        public static LoginPage LoginWindow = new();
        public static string ComputerName = Environment.MachineName;
        public static string UserName = string.Empty;

        public App() => this.InitializeComponent();

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            LoginWindow.Activate();
        }
    }
}
