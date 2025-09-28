using Avalonia.Controls;
using Avalonia.Interactivity;
using CashApp.ViewModels;

namespace CashApp.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            DataContext = new LoginViewModel();

            // Handle Enter key in password field
            txtPassword.KeyUp += (sender, e) =>
            {
                if (e.Key == Avalonia.Input.Key.Enter)
                {
                    PerformLogin();
                }
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            PerformLogin();
        }

        private async void PerformLogin()
        {
            if (DataContext is LoginViewModel viewModel)
            {
                var success = await viewModel.LoginAsync();
                if (success)
                {
                    // Show main window
                    var mainWindow = new MainWindow();
                    mainWindow.Show();

                    // Close login window
                    this.Close();
                }
            }
        }
    }
}
