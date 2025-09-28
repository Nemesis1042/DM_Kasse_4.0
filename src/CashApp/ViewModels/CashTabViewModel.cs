using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CashApp.Models;
using CashApp.Services;
using CommunityToolkit.Mvvm.Input;

namespace CashApp.ViewModels
{
    public class CashTabViewModel : INotifyPropertyChanged
    {
        private readonly AuthService _authService;
        private readonly ProductService _productService;
        private readonly OrderService _orderService;

        private string _searchTerm = "";
        private Product? _selectedProduct;
        private Order? _currentOrder;
        private decimal _paidAmount;
        private PaymentMethod _selectedPaymentMethod = PaymentMethod.Bar;
        private ObservableCollection<OrderItem> _currentOrderItems = new();
        private ObservableCollection<Product> _filteredProducts = new();

        public CashTabViewModel()
        {
            _authService = App.ServiceProvider.GetRequiredService<AuthService>();
            _productService = App.ServiceProvider.GetRequiredService<ProductService>();
            _orderService = App.ServiceProvider.GetRequiredService<OrderService>();

            // Initialize commands
            SearchCommand = new RelayCommand(SearchProducts);
            SelectCategoryCommand = new RelayCommand<string>(SelectCategory);
            NewOrderCommand = new AsyncRelayCommand(NewOrderAsync);
            AddProductCommand = new AsyncRelayCommand<Product>(AddProductAsync);
            RemoveItemCommand = new AsyncRelayCommand<OrderItem>(RemoveItemAsync);
            CompletePaymentCommand = new AsyncRelayCommand(CompletePaymentAsync);
            CancelOrderCommand = new AsyncRelayCommand(CancelOrderAsync);

            // Load initial data
            LoadProductsAsync().ConfigureAwait(false);
            _ = NewOrderAsync();
        }

        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                if (_searchTerm != value)
                {
                    _searchTerm = value;
                    OnPropertyChanged();
                    SearchProducts();
                }
            }
        }

        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (_selectedProduct != value)
                {
                    _selectedProduct = value;
                    OnPropertyChanged();
                }
            }
        }

        public Order? CurrentOrder
        {
            get => _currentOrder;
            set
            {
                if (_currentOrder != value)
                {
                    _currentOrder = value;
                    OnPropertyChanged();
                    RefreshOrderItems();
                }
            }
        }

        public decimal PaidAmount
        {
            get => _paidAmount;
            set
            {
                if (_paidAmount != value)
                {
                    _paidAmount = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ChangeAmount));
                }
            }
        }

        public PaymentMethod SelectedPaymentMethod
        {
            get => _selectedPaymentMethod;
            set
            {
                if (_selectedPaymentMethod != value)
                {
                    _selectedPaymentMethod = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<OrderItem> CurrentOrderItems
        {
            get => _currentOrderItems;
            set
            {
                if (_currentOrderItems != value)
                {
                    _currentOrderItems = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<Product> FilteredProducts
        {
            get => _filteredProducts;
            set
            {
                if (_filteredProducts != value)
                {
                    _filteredProducts = value;
                    OnPropertyChanged();
                }
            }
        }

        // Calculated properties
        public decimal Subtotal => CurrentOrder?.Subtotal ?? 0;
        public decimal TaxAmount => CurrentOrder?.TaxAmount ?? 0;
        public decimal DepositTotal => CurrentOrder?.DepositTotal ?? 0;
        public decimal TotalAmount => CurrentOrder?.TotalAmount ?? 0;
        public decimal ChangeAmount => PaidAmount - TotalAmount;

        public ICommand SearchCommand { get; }
        public ICommand SelectCategoryCommand { get; }
        public ICommand NewOrderCommand { get; }
        public ICommand AddProductCommand { get; }
        public ICommand RemoveItemCommand { get; }
        public ICommand CompletePaymentCommand { get; }
        public ICommand CancelOrderCommand { get; }

        private async Task LoadProductsAsync()
        {
            var products = await _productService.GetAllProductsAsync();
            FilteredProducts = new ObservableCollection<Product>(products);
        }

        private void SearchProducts()
        {
            if (string.IsNullOrWhiteSpace(SearchTerm))
            {
                LoadProductsAsync().ConfigureAwait(false);
                return;
            }

            var filtered = _productService.SearchProductsAsync(SearchTerm).Result;
            FilteredProducts = new ObservableCollection<Product>(filtered);
        }

        private void SelectCategory(string category)
        {
            if (category == "All")
            {
                LoadProductsAsync().ConfigureAwait(false);
                return;
            }

            if (Enum.TryParse<ProductCategory>(category, out var productCategory))
            {
                var products = _productService.GetProductsByCategoryAsync(productCategory).Result;
                FilteredProducts = new ObservableCollection<Product>(products);
            }
        }

        private async Task NewOrderAsync()
        {
            try
            {
                CurrentOrder = await _orderService.CreateOrderAsync(_authService.CurrentUser?.Id ?? 0);
                CurrentOrderItems.Clear();
                PaidAmount = 0;
                OnPropertyChanged(nameof(Subtotal));
                OnPropertyChanged(nameof(TaxAmount));
                OnPropertyChanged(nameof(DepositTotal));
                OnPropertyChanged(nameof(TotalAmount));
                OnPropertyChanged(nameof(ChangeAmount));
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private async Task AddProductAsync(Product? product)
        {
            if (product == null || CurrentOrder == null)
                return;

            try
            {
                var orderItem = await _orderService.AddItemToOrderAsync(CurrentOrder.Id, product.Id);
                RefreshOrderItems();
                OnPropertyChanged(nameof(Subtotal));
                OnPropertyChanged(nameof(TaxAmount));
                OnPropertyChanged(nameof(DepositTotal));
                OnPropertyChanged(nameof(TotalAmount));
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private async Task RemoveItemAsync(OrderItem? orderItem)
        {
            if (orderItem == null || CurrentOrder == null)
                return;

            try
            {
                await _orderService.RemoveItemFromOrderAsync(CurrentOrder.Id, orderItem.Id);
                RefreshOrderItems();
                OnPropertyChanged(nameof(Subtotal));
                OnPropertyChanged(nameof(TaxAmount));
                OnPropertyChanged(nameof(DepositTotal));
                OnPropertyChanged(nameof(TotalAmount));
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private async Task CompletePaymentAsync()
        {
            if (CurrentOrder == null || PaidAmount < TotalAmount)
                return;

            try
            {
                await _orderService.ProcessPaymentAsync(CurrentOrder.Id, SelectedPaymentMethod, PaidAmount);
                await NewOrderAsync(); // Start new order
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private async Task CancelOrderAsync()
        {
            if (CurrentOrder == null)
                return;

            try
            {
                await _orderService.CancelOrderAsync(CurrentOrder.Id);
                await NewOrderAsync(); // Start new order
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private void RefreshOrderItems()
        {
            if (CurrentOrder != null)
            {
                CurrentOrderItems = new ObservableCollection<OrderItem>(CurrentOrder.OrderItems);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
