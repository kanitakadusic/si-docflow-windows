using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using Microsoft.UI;
using Windows.UI;

namespace docflow.Utilities
{
    public enum DialogType
    {
        Error,
        Success,
        Warning
    }

    public static class DialogUtil
    {
        private static readonly SolidColorBrush ErrorBrush = new SolidColorBrush(Color.FromArgb(255, 220, 53, 69));
        private static readonly SolidColorBrush SuccessBrush = new SolidColorBrush(Color.FromArgb(255, 32, 156, 238));
        private static readonly SolidColorBrush WarningBrush = new SolidColorBrush(Color.FromArgb(255, 255, 165, 0));

        private static SolidColorBrush GetBackgroundBrush(DialogType type)
        {
            return type switch
            {
                DialogType.Error => ErrorBrush,
                DialogType.Success => SuccessBrush,
                DialogType.Warning => WarningBrush,
                _ => SuccessBrush
            };
        }

        public static ContentDialog CreateContentDialog(string title, string message, DialogType dialogType, XamlRoot xamlRoot)
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

            var closeButton = new Button
            {
                Content = "OK",
                FontSize = 24,
                Padding = new Thickness(40, 20, 40, 20),
                Margin = new Thickness(20, 20, 20, 20),
                HorizontalAlignment = HorizontalAlignment.Right,
                Foreground = new SolidColorBrush(Colors.White),
                Background = GetBackgroundBrush(dialogType),
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
