using Microsoft.UI.Xaml;

namespace docflow
{
    public partial class App : Application
    {
        public static MainWindow MainWindow = new();
        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MainWindow.Activate();
        }
    }
}
