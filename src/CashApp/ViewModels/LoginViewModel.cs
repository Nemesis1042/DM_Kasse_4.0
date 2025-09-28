using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using CashApp.Services;
using CommunityToolkit.Mvvm.Input;

namespace CashApp.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly AuthService _authService;
        private string _username = "";
        private string _password = "";
        private bool _rememberMe = false;
        private string _errorMessage = "";
        private bool _isLoading = false;

        public LoginViewModel()
        {
            _authService = App.ServiceProvider.GetRequiredService<AuthService>();
            LoginCommand = new AsyncRelayCommand(LoginAsync, CanExecuteLogin);
        }

        public string Username
        {
            get => _username;
            set
            {
                if (_username != value)
                {
                    _username = value;
                    OnPropertyChanged();
                    LoginCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (_password != value)
                {
                    _password = value;
                    OnPropertyChanged();
                    LoginCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public bool RememberMe
        {
            get => _rememberMe;
            set
            {
                if (_rememberMe != value)
                {
                    _rememberMe = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                    LoginCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public ICommand LoginCommand { get; }

        public async Task<bool> LoginAsync()
        {
            if (!CanExecuteLogin())
                return false;

            IsLoading = true;
            ErrorMessage = "";

            try
            {
                var success = await _authService.LoginAsync(Username, Password);

                if (!success)
                {
                    ErrorMessage = "Ungültiger Benutzername oder Passwort.";
                    return false;
                }

                // Login successful
                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Login fehlgeschlagen: {ex.Message}";
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanExecuteLogin()
        {
            return !IsLoading && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
