using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CashApp.Models;
using CashApp.Services;
using CommunityToolkit.Mvvm.Input;

namespace CashApp.ViewModels
{
    public class UserTabViewModel : INotifyPropertyChanged
    {
        private readonly AuthService _authService;
        private ObservableCollection<User> _users = new();
        private User? _selectedUser;

        public UserTabViewModel()
        {
            _authService = App.ServiceProvider.GetRequiredService<AuthService>();

            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            NewUserCommand = new RelayCommand(NewUser);

            _ = RefreshAsync();
        }

        public ObservableCollection<User> Users
        {
            get => _users;
            set
            {
                if (_users != value)
                {
                    _users = value;
                    OnPropertyChanged();
                }
            }
        }

        public User? SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (_selectedUser != value)
                {
                    _selectedUser = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand NewUserCommand { get; }

        private async Task RefreshAsync()
        {
            try
            {
                var users = await _authService.GetAllUsersAsync();
                Users = new ObservableCollection<User>(users);
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private void NewUser()
        {
            // In a full implementation, this would open a user editor dialog
            _ = RefreshAsync();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
