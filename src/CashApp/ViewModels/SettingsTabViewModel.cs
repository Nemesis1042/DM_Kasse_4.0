using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Controls;
using CashApp.Services;
using CommunityToolkit.Mvvm.Input;

namespace CashApp.ViewModels
{
    public class SettingsTabViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private readonly PrinterService _printerService;
        private readonly BackupService _backupService;

        private string _storeName = "Ihr Geschäft";
        private string _storeAddress = "Ihre Adresse";
        private string _storePhone = "Tel: 01234/56789";
        private string _selectedPrinter = "";
        private string _backupDirectory = "";
        private string _autoBackupTime = "02:00";
        private bool _autoBackupEnabled = false;
        private bool _testModeEnabled = false;
        private bool _detailedLogging = true;
        private string _selectedTheme = "Hell";
        private string _databaseSize = "";
        private int _productCount = 0;
        private int _userCount = 0;
        private int _orderCount = 0;

        public SettingsTabViewModel()
        {
            _databaseService = App.ServiceProvider.GetRequiredService<DatabaseService>();
            _printerService = App.ServiceProvider.GetRequiredService<PrinterService>();
            _backupService = App.ServiceProvider.GetRequiredService<BackupService>();

            SaveStoreInfoCommand = new RelayCommand(SaveStoreInfo);
            TestPrintCommand = new AsyncRelayCommand(TestPrintAsync);
            SavePrinterSettingsCommand = new RelayCommand(SavePrinterSettings);
            BrowseBackupDirectoryCommand = new AsyncRelayCommand(BrowseBackupDirectoryAsync);
            CreateBackupCommand = new AsyncRelayCommand(CreateBackupAsync);
            RestoreBackupCommand = new AsyncRelayCommand(RestoreBackupAsync);
            CleanupDatabaseCommand = new AsyncRelayCommand(CleanupDatabaseAsync);
            SaveSettingsCommand = new RelayCommand(SaveSettings);

            AvailableThemes = new ObservableCollection<string> { "Hell", "Dunkel" };
            _ = LoadDataAsync();
        }

        public string StoreName
        {
            get => _storeName;
            set
            {
                if (_storeName != value)
                {
                    _storeName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StoreAddress
        {
            get => _storeAddress;
            set
            {
                if (_storeAddress != value)
                {
                    _storeAddress = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StorePhone
        {
            get => _storePhone;
            set
            {
                if (_storePhone != value)
                {
                    _storePhone = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> AvailablePrinters { get; } = new();

        public string SelectedPrinter
        {
            get => _selectedPrinter;
            set
            {
                if (_selectedPrinter != value)
                {
                    _selectedPrinter = value;
                    OnPropertyChanged();
                }
            }
        }

        public string BackupDirectory
        {
            get => _backupDirectory;
            set
            {
                if (_backupDirectory != value)
                {
                    _backupDirectory = value;
                    OnPropertyChanged();
                }
            }
        }

        public string AutoBackupTime
        {
            get => _autoBackupTime;
            set
            {
                if (_autoBackupTime != value)
                {
                    _autoBackupTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool AutoBackupEnabled
        {
            get => _autoBackupEnabled;
            set
            {
                if (_autoBackupEnabled != value)
                {
                    _autoBackupEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool TestModeEnabled
        {
            get => _testModeEnabled;
            set
            {
                if (_testModeEnabled != value)
                {
                    _testModeEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool DetailedLogging
        {
            get => _detailedLogging;
            set
            {
                if (_detailedLogging != value)
                {
                    _detailedLogging = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> AvailableThemes { get; }

        public string SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                if (_selectedTheme != value)
                {
                    _selectedTheme = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DatabaseSize
        {
            get => _databaseSize;
            set
            {
                if (_databaseSize != value)
                {
                    _databaseSize = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ProductCount
        {
            get => _productCount;
            set
            {
                if (_productCount != value)
                {
                    _productCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public int UserCount
        {
            get => _userCount;
            set
            {
                if (_userCount != value)
                {
                    _userCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public int OrderCount
        {
            get => _orderCount;
            set
            {
                if (_orderCount != value)
                {
                    _orderCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand SaveStoreInfoCommand { get; }
        public ICommand TestPrintCommand { get; }
        public ICommand SavePrinterSettingsCommand { get; }
        public ICommand BrowseBackupDirectoryCommand { get; }
        public ICommand CreateBackupCommand { get; }
        public ICommand RestoreBackupCommand { get; }
        public ICommand CleanupDatabaseCommand { get; }
        public ICommand SaveSettingsCommand { get; }

        private async Task LoadDataAsync()
        {
            await Task.WhenAll(
                LoadPrintersAsync(),
                LoadSystemInfoAsync()
            );
        }

        private async Task LoadPrintersAsync()
        {
            try
            {
                var printers = await _printerService.GetAvailablePrintersAsync();
                AvailablePrinters.Clear();
                foreach (var printer in printers)
                {
                    AvailablePrinters.Add(printer);
                }

                if (AvailablePrinters.Any())
                {
                    SelectedPrinter = AvailablePrinters.First();
                }
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private async Task LoadSystemInfoAsync()
        {
            try
            {
                var stats = await _databaseService.GetStatisticsAsync();
                DatabaseSize = stats.FormattedDatabaseSize;
                ProductCount = stats.TotalProducts;
                UserCount = stats.TotalUsers;
                OrderCount = stats.TotalOrders;
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private void SaveStoreInfo()
        {
            // Save store information to configuration
            _printerService.ConfigurePrinterSettings(SelectedPrinter, StoreName, StoreAddress, StorePhone);
        }

        private async Task TestPrintAsync()
        {
            try
            {
                await _printerService.PrintTestPageAsync();
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private void SavePrinterSettings()
        {
            // Save printer settings
            _printerService.ConfigurePrinterSettings(SelectedPrinter, StoreName, StoreAddress, StorePhone);
        }

        private async Task BrowseBackupDirectoryAsync()
        {
            try
            {
                var dialog = new OpenFolderDialog
                {
                    Title = "Sicherungsverzeichnis auswählen..."
                };

                var mainWindow = App.ServiceProvider.GetRequiredService<MainWindow>();
                var result = await dialog.ShowAsync(mainWindow);

                if (!string.IsNullOrEmpty(result))
                {
                    BackupDirectory = result;
                }
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private async Task CreateBackupAsync()
        {
            try
            {
                string? customPath = null;
                if (!string.IsNullOrEmpty(BackupDirectory))
                {
                    customPath = BackupDirectory;
                }

                await _backupService.CreateBackupAsync(customPath);
                await LoadSystemInfoAsync(); // Refresh stats
            }
            catch (Exception ex)
            {
                // Handle error
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
                        new FileDialogFilter { Name = "Zip Files", Extensions = new List<string> { "zip" } }
                    }
                };

                var mainWindow = App.ServiceProvider.GetRequiredService<MainWindow>();
                var result = await dialog.ShowAsync(mainWindow);

                if (result != null && result.Any())
                {
                    var backupPath = result.First();
                    await _backupService.RestoreBackupAsync(backupPath);
                    await LoadSystemInfoAsync(); // Refresh stats
                }
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private async Task CleanupDatabaseAsync()
        {
            try
            {
                await _databaseService.CleanupOldLogsAsync();
                await LoadSystemInfoAsync(); // Refresh stats
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private void SaveSettings()
        {
            // Save application settings
            // This would typically save to a configuration file
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
