using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Runtime.InteropServices;
using Windows.UI;

namespace docflow
{
    public partial class App : Application
    {
        private static LoginPage LoginWindow = new();

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        public App() => InitializeComponent();

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            LoginWindow.Activate();
        }

        public static (int Width, int Height) GetPrimaryScreenSize()
        {
            int width = GetSystemMetrics(SM_CXSCREEN);
            int height = GetSystemMetrics(SM_CYSCREEN);
            return (width, height);
        }

        public static ContentDialog CreateContentDialog(string title, string message, XamlRoot xamlRoot, bool isError = true)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = xamlRoot,
                DefaultButton = ContentDialogButton.Close,
                CornerRadius = new CornerRadius(24)
            };

            var titleBlock = new TextBlock
            {
                Text = title,
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(20, 20, 20, 20),
                TextWrapping = TextWrapping.Wrap
            };

            var contentBlock = new TextBlock
            {
                Text = message,
                FontSize = 24,
                Margin = new Thickness(20, 20, 20, 20),
                TextWrapping = TextWrapping.Wrap
            };

            var background = isError
                ? new SolidColorBrush(Color.FromArgb(255, 220, 53, 69))
                : new SolidColorBrush(Color.FromArgb(255, 32, 156, 238));

            var closeButton = new Button
            {
                Content = "OK",
                FontSize = 24,
                Padding = new Thickness(40, 20, 40, 20),
                Margin = new Thickness(20, 20, 20, 20),
                HorizontalAlignment = HorizontalAlignment.Right,
                Foreground = new SolidColorBrush(Colors.White),
                Background = background,
                CornerRadius = new CornerRadius(14)
            };

            closeButton.Click += (_, _) => dialog.Hide();

            var stackPanel = new StackPanel();
            stackPanel.Children.Add(titleBlock);
            stackPanel.Children.Add(contentBlock);
            stackPanel.Children.Add(closeButton);

            dialog.Content = stackPanel;

            return dialog;
        }

    }
}
