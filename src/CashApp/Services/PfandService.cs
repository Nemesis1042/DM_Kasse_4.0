using CashApp.Models;
using Microsoft.Extensions.Logging;
using Serilog;

namespace CashApp.Services
{
    public class PfandService
    {
        private readonly DatabaseService _databaseService;
        private readonly ILogger<PfandService> _logger;

        public PfandService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _logger = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog();
            }).CreateLogger<PfandService>();
        }

        public async Task<decimal> GetTotalDepositBalanceAsync()
        {
            try
            {
                using var context = _databaseService.GetContext();

                // Sum all deposit amounts from paid orders
                var totalDeposits = await context.OrderItems
                    .Where(oi => oi.Order.Status == OrderStatus.Bezahlt && oi.DepositAmount.HasValue)
                    .SumAsync(oi => oi.DepositAmount.Value * oi.Quantity);

                // Sum all returned deposits (this would need a separate table for returns)
                // For now, we'll calculate based on a simple assumption
                var returnedDeposits = 0m; // This should be tracked separately

                return totalDeposits - returnedDeposits;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get total deposit balance");
                return 0;
            }
        }

        public async Task<decimal> GetDailyDepositBalanceAsync(DateTime date)
        {
            try
            {
                var startOfDay = date.Date;
                var endOfDay = startOfDay.AddDays(1);

                using var context = _databaseService.GetContext();

                return await context.OrderItems
                    .Where(oi => oi.Order.Status == OrderStatus.Bezahlt
                                && oi.Order.CreatedAt >= startOfDay
                                && oi.Order.CreatedAt < endOfDay
                                && oi.DepositAmount.HasValue)
                    .SumAsync(oi => oi.DepositAmount.Value * oi.Quantity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get daily deposit balance for {Date}", date);
                return 0;
            }
        }

        public async Task<IEnumerable<Product>> GetProductsWithDepositAsync()
        {
            try
            {
                using var context = _databaseService.GetContext();
                return await context.Products
                    .Where(p => p.RequiresDeposit && p.IsActive)
                    .OrderBy(p => p.Category)
                    .ThenBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get products with deposit");
                return new List<Product>();
            }
        }

        public async Task<Dictionary<ProductCategory, decimal>> GetDepositBalanceByCategoryAsync()
        {
            try
            {
                using var context = _databaseService.GetContext();

                return await context.OrderItems
                    .Where(oi => oi.Order.Status == OrderStatus.Bezahlt && oi.DepositAmount.HasValue)
                    .Join(context.Products,
                          oi => oi.ProductId,
                          p => p.Id,
                          (oi, p) => new { oi, p })
                    .GroupBy(x => x.p.Category)
                    .ToDictionaryAsync(
                        g => g.Key,
                        g => g.Sum(x => x.oi.DepositAmount.Value * x.oi.Quantity)
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get deposit balance by category");
                return new Dictionary<ProductCategory, decimal>();
            }
        }

        public async Task<DepositStatistics> GetDepositStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.Now.AddDays(-30);
                var end = endDate ?? DateTime.Now;

                using var context = _databaseService.GetContext();

                var statistics = new DepositStatistics
                {
                    TotalDepositBalance = await GetTotalDepositBalanceAsync(),
                    DailyDeposits = new Dictionary<DateTime, decimal>()
                };

                // Calculate daily deposits for the period
                for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
                {
                    var dailyBalance = await GetDailyDepositBalanceAsync(date);
                    if (dailyBalance > 0)
                    {
                        statistics.DailyDeposits[date] = dailyBalance;
                    }
                }

                statistics.DepositByCategory = await GetDepositBalanceByCategoryAsync();
                statistics.ProductsWithDeposit = await GetProductsWithDepositAsync();

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get deposit statistics");
                return new DepositStatistics();
            }
        }

        public async Task<bool> ProcessDepositReturnAsync(int productId, int quantity, int userId)
        {
            try
            {
                using var context = _databaseService.GetContext();

                var product = await context.Products.FindAsync(productId);
                if (product == null || !product.RequiresDeposit)
                {
                    return false;
                }

                var returnAmount = product.DepositAmount.Value * quantity;

                // In a real system, you might want to track individual deposit returns
                // For now, we'll just log the return
                await _databaseService.LogActivityAsync(userId, AuditAction.DepositReturned,
                    $"Deposit returned: {quantity}x {product.Name} = {returnAmount:C}", AuditLogLevel.Info,
                    $"ProductId: {productId}, Quantity: {quantity}, Amount: {returnAmount}");

                _logger.LogInformation("Deposit returned: {Quantity}x {ProductName} = {Amount:C}",
                    quantity, product.Name, returnAmount);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process deposit return for product {ProductId}", productId);
                return false;
            }
        }

        public async Task<decimal> CalculateOrderDepositTotalAsync(int orderId)
        {
            try
            {
                using var context = _databaseService.GetContext();

                return await context.OrderItems
                    .Where(oi => oi.OrderId == orderId && oi.DepositAmount.HasValue)
                    .SumAsync(oi => oi.DepositAmount.Value * oi.Quantity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate order deposit total for order {OrderId}", orderId);
                return 0;
            }
        }

        public async Task<IEnumerable<OrderItem>> GetOrderItemsWithDepositAsync(int orderId)
        {
            try
            {
                using var context = _databaseService.GetContext();

                return await context.OrderItems
                    .Include(oi => oi.Product)
                    .Where(oi => oi.OrderId == orderId && oi.DepositAmount.HasValue)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get order items with deposit for order {OrderId}", orderId);
                return new List<OrderItem>();
            }
        }
    }

    public class DepositStatistics
    {
        public decimal TotalDepositBalance { get; set; }
        public Dictionary<DateTime, decimal> DailyDeposits { get; set; } = new();
        public Dictionary<ProductCategory, decimal> DepositByCategory { get; set; } = new();
        public IEnumerable<Product> ProductsWithDeposit { get; set; } = new List<Product>();

        public decimal AverageDailyDeposit => DailyDeposits.Any() ?
            DailyDeposits.Values.Average() : 0;

        public DateTime? HighestDepositDay => DailyDeposits.Any() ?
            DailyDeposits.OrderByDescending(kvp => kvp.Value).First().Key : null;

        public decimal HighestDailyAmount => DailyDeposits.Any() ?
            DailyDeposits.Values.Max() : 0;
    }
}
