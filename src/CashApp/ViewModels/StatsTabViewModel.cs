using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CashApp.Models;
using CashApp.Services;
using CommunityToolkit.Mvvm.Input;

namespace CashApp.ViewModels
{
    public class StatsTabViewModel : INotifyPropertyChanged
    {
        private readonly OrderService _orderService;
        private readonly PdfExportService _pdfExportService;

        private DateTime _startDate = DateTime.Now.AddDays(-30);
        private DateTime _endDate = DateTime.Now;
        private decimal _totalRevenue;
        private int _totalOrders;
        private decimal _averageOrderValue;
        private decimal _averageDailyRevenue;
        private ObservableCollection<KeyValuePair<DateTime, DailyStats>> _dailyRevenue = new();
        private ObservableCollection<ProductStats> _productStatistics = new();
        private ObservableCollection<CategoryStats> _categoryStatistics = new();

        public StatsTabViewModel()
        {
            _orderService = App.ServiceProvider.GetRequiredService<OrderService>();
            _pdfExportService = App.ServiceProvider.GetRequiredService<PdfExportService>();

            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            ExportToPdfCommand = new AsyncRelayCommand(ExportToPdfAsync);

            _ = RefreshAsync();
        }

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (_startDate != value)
                {
                    _startDate = value;
                    OnPropertyChanged();
                    _ = RefreshAsync();
                }
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (_endDate != value)
                {
                    _endDate = value;
                    OnPropertyChanged();
                    _ = RefreshAsync();
                }
            }
        }

        public decimal TotalRevenue
        {
            get => _totalRevenue;
            set
            {
                if (_totalRevenue != value)
                {
                    _totalRevenue = value;
                    OnPropertyChanged();
                }
            }
        }

        public int TotalOrders
        {
            get => _totalOrders;
            set
            {
                if (_totalOrders != value)
                {
                    _totalOrders = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal AverageOrderValue
        {
            get => _averageOrderValue;
            set
            {
                if (_averageOrderValue != value)
                {
                    _averageOrderValue = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal AverageDailyRevenue
        {
            get => _averageDailyRevenue;
            set
            {
                if (_averageDailyRevenue != value)
                {
                    _averageDailyRevenue = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<KeyValuePair<DateTime, DailyStats>> DailyRevenue
        {
            get => _dailyRevenue;
            set
            {
                if (_dailyRevenue != value)
                {
                    _dailyRevenue = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<ProductStats> ProductStatistics
        {
            get => _productStatistics;
            set
            {
                if (_productStatistics != value)
                {
                    _productStatistics = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<CategoryStats> CategoryStatistics
        {
            get => _categoryStatistics;
            set
            {
                if (_categoryStatistics != value)
                {
                    _categoryStatistics = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand ExportToPdfCommand { get; }

        private async Task RefreshAsync()
        {
            try
            {
                await Task.WhenAll(
                    LoadRevenueStatisticsAsync(),
                    LoadProductStatisticsAsync(),
                    LoadCategoryStatisticsAsync()
                );
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private async Task LoadRevenueStatisticsAsync()
        {
            try
            {
                var orders = await _orderService.GetOrdersForDateRangeAsync(StartDate, EndDate);
                var ordersList = orders.ToList();

                TotalOrders = ordersList.Count;
                TotalRevenue = ordersList.Sum(o => o.TotalAmount);
                AverageOrderValue = TotalOrders > 0 ? TotalRevenue / TotalOrders : 0;

                // Calculate daily statistics
                var dailyStats = new Dictionary<DateTime, DailyStats>();
                foreach (var order in ordersList)
                {
                    var date = order.CreatedAt.Date;
                    if (!dailyStats.ContainsKey(date))
                    {
                        dailyStats[date] = new DailyStats { Date = date, Revenue = 0, OrderCount = 0 };
                    }

                    dailyStats[date].Revenue += order.TotalAmount;
                    dailyStats[date].OrderCount++;
                }

                AverageDailyRevenue = dailyStats.Any() ? dailyStats.Values.Average(d => d.Revenue) : 0;
                DailyRevenue = new ObservableCollection<KeyValuePair<DateTime, DailyStats>>(
                    dailyStats.Select(kvp => new KeyValuePair<DateTime, DailyStats>(kvp.Key, kvp.Value))
                );
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private async Task LoadProductStatisticsAsync()
        {
            try
            {
                // This would require a more complex query to get product statistics
                // For now, we'll create a placeholder
                ProductStatistics = new ObservableCollection<ProductStats>();
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private async Task LoadCategoryStatisticsAsync()
        {
            try
            {
                // This would require a more complex query to get category statistics
                // For now, we'll create a placeholder
                CategoryStatistics = new ObservableCollection<CategoryStats>();
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private async Task ExportToPdfAsync()
        {
            try
            {
                var statistics = new StatisticsData
                {
                    TotalRevenue = TotalRevenue,
                    TotalOrders = TotalOrders,
                    DailyRevenue = DailyRevenue.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Revenue),
                    DailyOrderCount = DailyRevenue.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.OrderCount)
                };

                var filePath = await _pdfExportService.ExportStatisticsToPdfAsync(StartDate, EndDate, statistics);

                // In a real application, you would show the file or open it
                // For now, we'll just log it
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

    public class DailyStats
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class ProductStats
    {
        public string ProductName { get; set; } = "";
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public decimal TaxAmount { get; set; }
    }

    public class CategoryStats
    {
        public string Category { get; set; } = "";
        public decimal Revenue { get; set; }
        public decimal Percentage { get; set; }
    }
}
