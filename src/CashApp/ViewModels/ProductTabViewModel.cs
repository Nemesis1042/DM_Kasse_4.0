using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CashApp.Models;
using CashApp.Services;
using CommunityToolkit.Mvvm.Input;

namespace CashApp.ViewModels
{
    public class ProductTabViewModel : INotifyPropertyChanged
    {
        private readonly ProductService _productService;
        private ObservableCollection<Product> _products = new();
        private Product? _selectedProduct;

        public ProductTabViewModel()
        {
            _productService = App.ServiceProvider.GetRequiredService<ProductService>();

            // Initialize commands
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            NewProductCommand = new RelayCommand(NewProduct);

            // Load initial data
            _ = RefreshAsync();
        }

        public ObservableCollection<Product> Products
        {
            get => _products;
            set
            {
                if (_products != value)
                {
                    _products = value;
                    OnPropertyChanged();
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

        public ICommand RefreshCommand { get; }
        public ICommand NewProductCommand { get; }

        private async Task RefreshAsync()
        {
            try
            {
                var products = await _productService.GetAllProductsAsync();
                Products = new ObservableCollection<Product>(products);
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private void NewProduct()
        {
            // In a full implementation, this would open a product editor dialog
            // For now, we'll just refresh the list
            _ = RefreshAsync();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
