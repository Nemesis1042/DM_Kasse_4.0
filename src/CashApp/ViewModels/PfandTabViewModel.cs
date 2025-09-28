using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CashApp.Models;
using CashApp.Services;
using CommunityToolkit.Mvvm.Input;

namespace CashApp.ViewModels
{
    public class PfandTabViewModel : INotifyPropertyChanged
    {
        private readonly AuthService _authService;
        private readonly PfandService _pfandService;

        private decimal _totalDepositBalance;
        private decimal _todayDepositBalance;
        private ObservableCollection<KeyValuePair<ProductCategory, decimal>> _depositByCategory = new();
        private ObservableCollection<KeyValuePair<DateTime, decimal>> _dailyDepositStatistics = new();
        private ObservableCollection<Product> _productsWithDeposit = new();
        private Product? _selectedProductForReturn;
        private int _returnQuantity = 1;
        private DateTime _statisticsStartDate = DateTime.Now.AddDays(-30);
        private DateTime _statisticsEndDate = DateTime.Now;

        public PfandTabViewModel()
        {
            _authService = App.ServiceProvider.GetRequiredService<AuthService>();
            _pfandService = App.ServiceProvider.GetRequiredService<PfandService>();

            // Initialize commands
            RefreshStatisticsCommand = new AsyncRelayCommand(RefreshStatisticsAsync);
            ProcessDepositReturnCommand = new AsyncRelayCommand(ProcessDepositReturnAsync);

            // Load initial data
            _ = LoadDataAsync();
        }

        public decimal TotalDepositBalance
        {
            get => _totalDepositBalance;
            set
            {
                if (_totalDepositBalance != value)
                {
                    _totalDepositBalance = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal TodayDepositBalance
        {
            get => _todayDepositBalance;
            set
            {
                if (_todayDepositBalance != value)
                {
                    _todayDepositBalance = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<KeyValuePair<ProductCategory, decimal>> DepositByCategory
        {
            get => _depositByCategory;
            set
            {
                if (_depositByCategory != value)
                {
                    _depositByCategory = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<KeyValuePair<DateTime, decimal>> DailyDepositStatistics
        {
            get => _dailyDepositStatistics;
            set
            {
                if (_dailyDepositStatistics != value)
                {
                    _dailyDepositStatistics = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<Product> ProductsWithDeposit
        {
            get => _productsWithDeposit;
            set
            {
                if (_productsWithDeposit != value)
                {
                    _productsWithDeposit = value;
                    OnPropertyChanged();
                }
            }
        }

        public Product? SelectedProductForReturn
        {
            get => _selectedProductForReturn;
            set
            {
                if (_selectedProductForReturn != value)
                {
                    _selectedProductForReturn = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ReturnAmount));
                }
            }
        }

        public int ReturnQuantity
        {
            get => _returnQuantity;
            set
            {
                if (_returnQuantity != value)
                {
                    _returnQuantity = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ReturnAmount));
                }
            }
        }

        public DateTime StatisticsStartDate
        {
            get => _statisticsStartDate;
            set
            {
                if (_statisticsStartDate != value)
                {
                    _statisticsStartDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime StatisticsEndDate
        {
            get => _statisticsEndDate;
            set
            {
                if (_statisticsEndDate != value)
                {
                    _statisticsEndDate = value;
                    OnPropertyChanged();
                }
            }
        }

        // Calculated properties
        public decimal ReturnAmount => SelectedProductForReturn?.DepositAmount * ReturnQuantity ?? 0;

        public ICommand RefreshStatisticsCommand { get; }
        public ICommand ProcessDepositReturnCommand { get; }

        private async Task LoadDataAsync()
        {
            await Task.WhenAll(
                LoadDepositBalanceAsync(),
                LoadProductsWithDepositAsync(),
                RefreshStatisticsAsync()
            );
        }

        private async Task LoadDepositBalanceAsync()
        {
            try
            {
                TotalDepositBalance = await _pfandService.GetTotalDepositBalanceAsync();
                TodayDepositBalance = await _pfandService.GetDailyDepositBalanceAsync(DateTime.Now);

                var categoryBalance = await _pfandService.GetDepositBalanceByCategoryAsync();
                DepositByCategory = new ObservableCollection<KeyValuePair<ProductCategory, decimal>>(categoryBalance);
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private async Task LoadProductsWithDepositAsync()
        {
            try
            {
                var products = await _pfandService.GetProductsWithDepositAsync();
                ProductsWithDeposit = new ObservableCollection<Product>(products);
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private async Task RefreshStatisticsAsync()
        {
            try
            {
                var statistics = await _pfandService.GetDepositStatisticsAsync(StatisticsStartDate, StatisticsEndDate);

                DailyDepositStatistics = new ObservableCollection<KeyValuePair<DateTime, decimal>>(
                    statistics.DailyDeposits.OrderBy(kvp => kvp.Key)
                );
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private async Task ProcessDepositReturnAsync()
        {
            if (SelectedProductForReturn == null || ReturnQuantity <= 0)
                return;

            try
            {
                var success = await _pfandService.ProcessDepositReturnAsync(
                    SelectedProductForReturn.Id,
                    ReturnQuantity,
                    _authService.CurrentUser?.Id ?? 0
                );

                if (success)
                {
                    // Refresh data after successful return
                    await LoadDataAsync();

                    // Reset form
                    SelectedProductForReturn = null;
                    ReturnQuantity = 1;
                }
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
