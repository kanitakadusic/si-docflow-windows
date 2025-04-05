using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace docflow
{
    public sealed partial class LoginPage : Window
    {
        public LoginPage() => this.InitializeComponent();

        private void LoginButton_Click(object sender, RoutedEventArgs e) { 
        
            if(string.IsNullOrWhiteSpace(UsernameBox.Text))
            {
                UsernameBox.Focus(FocusState.Programmatic);
                return;
            }
            App.UserName = UsernameBox.Text;
            App.MainWindow.Activate();
            this.Close();
        }
    }
}
