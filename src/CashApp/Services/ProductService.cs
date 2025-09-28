using CashApp.Models;
using Microsoft.Extensions.Logging;
using Serilog;

namespace CashApp.Services
{
    public class ProductService
    {
        private readonly DatabaseService _databaseService;
        private readonly ILogger<ProductService> _logger;

        public ProductService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _logger = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog();
            }).CreateLogger<ProductService>();
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync(bool includeInactive = false)
        {
            try
            {
                using var context = _databaseService.GetContext();
                var query = context.Products.AsQueryable();

                if (!includeInactive)
                {
                    query = query.Where(p => p.IsActive);
                }

                return await query
                    .OrderBy(p => p.Category)
                    .ThenBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all products");
                return new List<Product>();
            }
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(ProductCategory category)
        {
            try
            {
                using var context = _databaseService.GetContext();
                return await context.Products
                    .Where(p => p.Category == category && p.IsActive)
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get products by category: {Category}", category);
                return new List<Product>();
            }
        }

        public async Task<Product?> GetProductByIdAsync(int productId)
        {
            try
            {
                using var context = _databaseService.GetContext();
                return await context.Products
                    .FirstOrDefaultAsync(p => p.Id == productId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get product by ID: {ProductId}", productId);
                return null;
            }
        }

        public async Task<Product?> GetProductByBarcodeAsync(string barcode)
        {
            try
            {
                using var context = _databaseService.GetContext();
                return await context.Products
                    .FirstOrDefaultAsync(p => p.Barcode == barcode && p.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get product by barcode: {Barcode}", barcode);
                return null;
            }
        }

        public async Task<bool> CreateProductAsync(Product product)
        {
            try
            {
                using var context = _databaseService.GetContext();

                // Check if barcode already exists
                if (!string.IsNullOrEmpty(product.Barcode) &&
                    await context.Products.AnyAsync(p => p.Barcode == product.Barcode))
                {
                    return false;
                }

                product.CreatedAt = DateTime.UtcNow;
                product.UpdatedAt = DateTime.UtcNow;

                await context.Products.AddAsync(product);
                await context.SaveChangesAsync();

                await _databaseService.LogActivityAsync(0, AuditAction.ProductCreated,
                    $"Product '{product.Name}' created", AuditLogLevel.Info,
                    $"Category: {product.Category}, Price: {product.Price:C}");

                _logger.LogInformation("Product '{ProductName}' created successfully", product.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create product: {ProductName}", product.Name);
                return false;
            }
        }

        public async Task<bool> UpdateProductAsync(Product product)
        {
            try
            {
                using var context = _databaseService.GetContext();

                var existingProduct = await context.Products.FindAsync(product.Id);
                if (existingProduct == null)
                {
                    return false;
                }

                // Check if barcode already exists (excluding current product)
                if (!string.IsNullOrEmpty(product.Barcode) &&
                    await context.Products.AnyAsync(p => p.Barcode == product.Barcode && p.Id != product.Id))
                {
                    return false;
                }

                var changes = new List<string>();

                if (existingProduct.Name != product.Name)
                    changes.Add($"Name: '{existingProduct.Name}' -> '{product.Name}'");

                if (existingProduct.Price != product.Price)
                    changes.Add($"Price: {existingProduct.Price:C} -> {product.Price:C}");

                if (existingProduct.TaxRate != product.TaxRate)
                    changes.Add($"TaxRate: {existingProduct.TaxRate}% -> {product.TaxRate}%");

                if (existingProduct.Category != product.Category)
                    changes.Add($"Category: {existingProduct.Category} -> {product.Category}");

                if (existingProduct.Description != product.Description)
                    changes.Add("Description changed");

                if (existingProduct.Barcode != product.Barcode)
                    changes.Add($"Barcode: '{existingProduct.Barcode}' -> '{product.Barcode}'");

                if (existingProduct.IsActive != product.IsActive)
                    changes.Add($"IsActive: {existingProduct.IsActive} -> {product.IsActive}");

                if (existingProduct.RequiresDeposit != product.RequiresDeposit)
                    changes.Add($"RequiresDeposit: {existingProduct.RequiresDeposit} -> {product.RequiresDeposit}");

                if (existingProduct.DepositAmount != product.DepositAmount)
                    changes.Add($"DepositAmount: {existingProduct.DepositAmount:C} -> {product.DepositAmount:C}");

                if (existingProduct.StockQuantity != product.StockQuantity)
                    changes.Add($"StockQuantity: {existingProduct.StockQuantity} -> {product.StockQuantity}");

                // Update all properties
                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                existingProduct.TaxRate = product.TaxRate;
                existingProduct.Category = product.Category;
                existingProduct.Barcode = product.Barcode;
                existingProduct.IsActive = product.IsActive;
                existingProduct.RequiresDeposit = product.RequiresDeposit;
                existingProduct.DepositAmount = product.DepositAmount;
                existingProduct.StockQuantity = product.StockQuantity;
                existingProduct.MinStockLevel = product.MinStockLevel;
                existingProduct.ImagePath = product.ImagePath;
                existingProduct.UpdateTimestamp();

                await context.SaveChangesAsync();

                if (changes.Any())
                {
                    await _databaseService.LogActivityAsync(0, AuditAction.ProductModified,
                        $"Product '{product.Name}' updated: {string.Join(", ", changes)}", AuditLogLevel.Info);
                }

                _logger.LogInformation("Product '{ProductName}' updated successfully", product.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update product ID: {ProductId}", product.Id);
                return false;
            }
        }

        public async Task<bool> DeleteProductAsync(int productId)
        {
            try
            {
                using var context = _databaseService.GetContext();

                var product = await context.Products.FindAsync(productId);
                if (product == null)
                {
                    return false;
                }

                // Check if product is used in any orders
                var isUsedInOrders = await context.OrderItems.AnyAsync(oi => oi.ProductId == productId);
                if (isUsedInOrders)
                {
                    // Don't delete, just deactivate
                    product.IsActive = false;
                    product.UpdateTimestamp();
                    await context.SaveChangesAsync();

                    await _databaseService.LogActivityAsync(0, AuditAction.ProductModified,
                        $"Product '{product.Name}' deactivated (has existing orders)", AuditLogLevel.Info);

                    _logger.LogInformation("Product '{ProductName}' deactivated (has existing orders)", product.Name);
                }
                else
                {
                    // Safe to delete
                    context.Products.Remove(product);
                    await context.SaveChangesAsync();

                    await _databaseService.LogActivityAsync(0, AuditAction.ProductDeleted,
                        $"Product '{product.Name}' deleted", AuditLogLevel.Info);

                    _logger.LogInformation("Product '{ProductName}' deleted successfully", product.Name);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete product ID: {ProductId}", productId);
                return false;
            }
        }

        public async Task<bool> UpdateStockAsync(int productId, int newQuantity, string reason = "Manual adjustment")
        {
            try
            {
                using var context = _databaseService.GetContext();

                var product = await context.Products.FindAsync(productId);
                if (product == null)
                {
                    return false;
                }

                var oldQuantity = product.StockQuantity;
                product.StockQuantity = newQuantity;
                product.UpdateTimestamp();
                await context.SaveChangesAsync();

                await _databaseService.LogActivityAsync(0, AuditAction.ProductModified,
                    $"Stock updated for '{product.Name}': {oldQuantity} -> {newQuantity} ({reason})", AuditLogLevel.Info);

                _logger.LogInformation("Stock updated for product '{ProductName}': {OldQuantity} -> {NewQuantity}",
                    product.Name, oldQuantity, newQuantity);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update stock for product ID: {ProductId}", productId);
                return false;
            }
        }

        public async Task<IEnumerable<Product>> GetLowStockProductsAsync()
        {
            try
            {
                using var context = _databaseService.GetContext();
                return await context.Products
                    .Where(p => p.IsActive && p.MinStockLevel > 0 && p.StockQuantity <= p.MinStockLevel)
                    .OrderBy(p => p.StockQuantity)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get low stock products");
                return new List<Product>();
            }
        }

        public async Task<bool> IsBarcodeAvailableAsync(string barcode, int? excludeProductId = null)
        {
            try
            {
                using var context = _databaseService.GetContext();
                var query = context.Products.Where(p => p.Barcode == barcode);

                if (excludeProductId.HasValue)
                {
                    query = query.Where(p => p.Id != excludeProductId.Value);
                }

                return !await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check barcode availability: {Barcode}", barcode);
                return false;
            }
        }

        public async Task<Dictionary<ProductCategory, int>> GetProductCategoryDistributionAsync()
        {
            try
            {
                using var context = _databaseService.GetContext();
                return await context.Products
                    .Where(p => p.IsActive)
                    .GroupBy(p => p.Category)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get product category distribution");
                return new Dictionary<ProductCategory, int>();
            }
        }

        public async Task<decimal> GetTotalInventoryValueAsync()
        {
            try
            {
                using var context = _databaseService.GetContext();
                return await context.Products
                    .Where(p => p.IsActive)
                    .SumAsync(p => p.Price * p.StockQuantity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get total inventory value");
                return 0;
            }
        }

        public async Task<int> GetTotalProductCountAsync()
        {
            try
            {
                using var context = _databaseService.GetContext();
                return await context.Products.CountAsync(p => p.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get total product count");
                return 0;
            }
        }

        public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm)
        {
            try
            {
                using var context = _databaseService.GetContext();
                var term = searchTerm.ToLower();

                return await context.Products
                    .Where(p => p.IsActive && (
                        p.Name.ToLower().Contains(term) ||
                        p.Description.ToLower().Contains(term) ||
                        p.Barcode.Contains(term) ||
                        p.Category.ToString().ToLower().Contains(term)
                    ))
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search products: {SearchTerm}", searchTerm);
                return new List<Product>();
            }
        }
    }
}
