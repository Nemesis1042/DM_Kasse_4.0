using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using CashApp.Services;
using CommunityToolkit.Mvvm.Input;

namespace CashApp.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly AuthService _authService;
        private readonly DatabaseService _databaseService;
        private string _statusMessage = "Bereit";
        private string _currentUserInfo = "";
        private string _currentTime = "";
        private int _selectedTabIndex = 0;
        private bool _isTestMode = false;

        public MainWindowViewModel()
        {
            _authService = App.ServiceProvider.GetRequiredService<AuthService>();
            _databaseService = App.ServiceProvider.GetRequiredService<DatabaseService>();

            // Initialize ViewModels
            CashTabViewModel = App.ServiceProvider.GetRequiredService<CashTabViewModel>();
            ProductTabViewModel = App.ServiceProvider.GetRequiredService<ProductTabViewModel>();
            PfandTabViewModel = App.ServiceProvider.GetRequiredService<PfandTabViewModel>();
            StatsTabViewModel = App.ServiceProvider.GetRequiredService<StatsTabViewModel>();
            SettingsTabViewModel = App.ServiceProvider.GetRequiredService<SettingsTabViewModel>();
            UserTabViewModel = App.ServiceProvider.GetRequiredService<UserTabViewModel>();

            // Initialize commands
            CreateBackupCommand = new AsyncRelayCommand(CreateBackupAsync);
            RestoreBackupCommand = new AsyncRelayCommand(RestoreBackupAsync);
            ExitCommand = new RelayCommand(Exit);
            CleanupDatabaseCommand = new AsyncRelayCommand(CleanupDatabaseAsync);
            AboutCommand = new RelayCommand(ShowAbout);

            UpdateCurrentUserInfo();
        }

        public CashTabViewModel CashTabViewModel { get; }
        public ProductTabViewModel ProductTabViewModel { get; }
        public PfandTabViewModel PfandTabViewModel { get; }
        public StatsTabViewModel StatsTabViewModel { get; }
        public SettingsTabViewModel SettingsTabViewModel { get; }
        public UserTabViewModel UserTabViewModel { get; }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CurrentUserInfo
        {
            get => _currentUserInfo;
            set
            {
                if (_currentUserInfo != value)
                {
                    _currentUserInfo = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CurrentTime
        {
            get => _currentTime;
            set
            {
                if (_currentTime != value)
                {
                    _currentTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (_selectedTabIndex != value)
                {
                    _selectedTabIndex = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsTestMode
        {
            get => _isTestMode;
            set
            {
                if (_isTestMode != value)
                {
                    _isTestMode = value;
                    OnPropertyChanged();
                    StatusMessage = _isTestMode ? "Testmodus aktiviert" : "Bereit";
                }
            }
        }

        public bool CanManageUsers => _authService.CanManageUsers;

        public ICommand CreateBackupCommand { get; }
        public ICommand RestoreBackupCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand CleanupDatabaseCommand { get; }
        public ICommand AboutCommand { get; }

        public void UpdateCurrentTime()
        {
            CurrentTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
        }

        private void UpdateCurrentUserInfo()
        {
            if (_authService.CurrentUser != null)
            {
                CurrentUserInfo = $"{_authService.CurrentUser.FullName} ({_authService.CurrentUser.Role})";
            }
        }

        public void Logout()
        {
            _authService.Logout();
        }

        private async Task CreateBackupAsync()
        {
            try
            {
                var dialog = new OpenFolderDialog
                {
                    Title = "Sicherung speichern unter..."
                };

                var result = await dialog.ShowAsync(App.ServiceProvider.GetRequiredService<MainWindow>());
                if (!string.IsNullOrEmpty(result))
                {
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var backupPath = Path.Combine(result, $"CashApp_Backup_{timestamp}.db");

                    await _databaseService.BackupDatabaseAsync(backupPath);
                    StatusMessage = $"Sicherung erstellt: {backupPath}";

                    await _databaseService.LogActivityAsync(_authService.CurrentUser?.Id ?? 0,
                        Models.AuditAction.BackupCreated, $"Database backup created: {backupPath}");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Fehler beim Erstellen der Sicherung: {ex.Message}";
            }
        }

        private async Task RestoreBackupAsync()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Sicherung auswählen...",
                    Filters = new List<FileDialogFilter>
                    {
                        new FileDialogFilter { Name = "Database Files", Extensions = new List<string> { "db" } }
                    }
                };

                var result = await dialog.ShowAsync(App.ServiceProvider.GetRequiredService<MainWindow>());
                if (result != null && result.Any())
                {
                    var backupPath = result.First();

                    await _databaseService.RestoreDatabaseAsync(backupPath);
                    StatusMessage = $"Sicherung wiederhergestellt: {backupPath}";

                    await _databaseService.LogActivityAsync(_authService.CurrentUser?.Id ?? 0,
                        Models.AuditAction.BackupRestored, $"Database restored from: {backupPath}");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Fehler beim Wiederherstellen der Sicherung: {ex.Message}";
            }
        }

        private void Exit()
        {
            App.ServiceProvider.GetRequiredService<MainWindow>()?.Close();
        }

        private async Task CleanupDatabaseAsync()
        {
            try
            {
                await _databaseService.CleanupOldLogsAsync();
                StatusMessage = "Datenbank bereinigt";

                await _databaseService.LogActivityAsync(_authService.CurrentUser?.Id ?? 0,
                    Models.AuditAction.DatabaseMaintenance, "Database cleanup performed");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Fehler beim Bereinigen der Datenbank: {ex.Message}";
            }
        }

        private void ShowAbout()
        {
            var aboutWindow = new Window
            {
                Title = "Über Kassensystem",
                Width = 400,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Thickness(20),
                    Spacing = 10,
                    Children =
                    {
                        new TextBlock { Text = "Kassensystem v1.0", FontSize = 24, FontWeight = "Bold" },
                        new TextBlock { Text = "Ein modernes Kassensystem für Veranstaltungen und kleinere Betriebe" },
                        new TextBlock { Text = "" },
                        new TextBlock { Text = "Entwickelt mit:" },
                        new TextBlock { Text = "• Avalonia UI" },
                        new TextBlock { Text = "• .NET 8" },
                        new TextBlock { Text = "• Entity Framework Core" },
                        new TextBlock { Text = "• SQLite" },
                        new TextBlock { Text = "" },
                        new TextBlock { Text = "© 2024" }
                    }
                }
            };

            aboutWindow.ShowDialog(App.ServiceProvider.GetRequiredService<MainWindow>());
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
