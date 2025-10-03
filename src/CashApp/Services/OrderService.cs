using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using CashApp.Models;
using Microsoft.Extensions.Logging;
using Serilog;

namespace CashApp.Services
{
    public class OrderService
    {
        private readonly DatabaseService _databaseService;
        private readonly ILogger<OrderService> _logger;

        public OrderService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _logger = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog();
            }).CreateLogger<OrderService>();
        }

        public async Task<Order> CreateOrderAsync(int userId)
        {
            try
            {
                using var context = _databaseService.GetContext();

                var orderNumber = GenerateOrderNumber();
                var order = new Order
                {
                    UserId = userId,
                    OrderNumber = orderNumber,
                    Status = OrderStatus.Offen,
                    CreatedAt = DateTime.UtcNow
                };

                await context.Orders.AddAsync(order);
                await context.SaveChangesAsync();

                await _databaseService.LogActivityAsync(userId, AuditAction.OrderCreated,
                    $"Order {orderNumber} created", AuditLogLevel.Info);

                _logger.LogInformation("Order {OrderNumber} created by user ID: {UserId}", orderNumber, userId);
                return order;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create order for user ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            try
            {
                using var context = _databaseService.GetContext();
                return await context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.Id == orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get order by ID: {OrderId}", orderId);
                return null;
            }
        }

        public async Task<Order?> GetOrderByOrderNumberAsync(string orderNumber)
        {
            try
            {
                using var context = _databaseService.GetContext();
                return await context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get order by number: {OrderNumber}", orderNumber);
                return null;
            }
        }

        public async Task<OrderItem> AddItemToOrderAsync(int orderId, int productId, int quantity = 1)
        {
            try
            {
                using var context = _databaseService.GetContext();

                var order = await context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                    throw new ArgumentException("Order not found");

                if (order.Status != OrderStatus.Offen)
                    throw new InvalidOperationException("Cannot modify completed order");

                var product = await context.Products.FindAsync(productId);
                if (product == null || !product.IsActive)
                    throw new ArgumentException("Product not found or inactive");

                // Check if item already exists in order
                var existingItem = order.OrderItems.FirstOrDefault(oi => oi.ProductId == productId);
                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                    existingItem.CreatedAt = DateTime.UtcNow; // Update timestamp
                }
                else
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = orderId,
                        ProductId = productId,
                        Quantity = quantity,
                        UnitPrice = product.Price,
                        TaxRate = product.TaxRate,
                        DepositAmount = product.RequiresDeposit ? product.DepositAmount : null
                    };

                    await context.OrderItems.AddAsync(orderItem);
                    existingItem = orderItem;
                }

                // Recalculate order totals
                order.CalculateTotals();
                await context.SaveChangesAsync();

                await _databaseService.LogActivityAsync(order.UserId, AuditAction.OrderItemAdded,
                    $"Added {quantity}x {product.Name} to order {order.OrderNumber}", AuditLogLevel.Info);

                _logger.LogInformation("Added {Quantity}x {ProductName} to order {OrderNumber}",
                    quantity, product.Name, order.OrderNumber);

                return existingItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add item to order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<bool> RemoveItemFromOrderAsync(int orderId, int orderItemId)
        {
            try
            {
                using var context = _databaseService.GetContext();

                var orderItem = await context.OrderItems
                    .Include(oi => oi.Order)
                    .Include(oi => oi.Product)
                    .FirstOrDefaultAsync(oi => oi.Id == orderItemId && oi.OrderId == orderId);

                if (orderItem == null)
                    return false;

                if (orderItem.Order.Status != OrderStatus.Offen)
                    throw new InvalidOperationException("Cannot modify completed order");

                context.OrderItems.Remove(orderItem);

                // Recalculate order totals
                orderItem.Order.CalculateTotals();
                await context.SaveChangesAsync();

                await _databaseService.LogActivityAsync(orderItem.Order.UserId, AuditAction.OrderItemRemoved,
                    $"Removed {orderItem.Product.Name} from order {orderItem.Order.OrderNumber}", AuditLogLevel.Info);

                _logger.LogInformation("Removed item {ProductName} from order {OrderNumber}",
                    orderItem.Product.Name, orderItem.Order.OrderNumber);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove item {OrderItemId} from order {OrderId}", orderItemId, orderId);
                return false;
            }
        }

        public async Task<bool> UpdateItemQuantityAsync(int orderId, int orderItemId, int newQuantity)
        {
            try
            {
                using var context = _databaseService.GetContext();

                var orderItem = await context.OrderItems
                    .Include(oi => oi.Order)
                    .FirstOrDefaultAsync(oi => oi.Id == orderItemId && oi.OrderId == orderId);

                if (orderItem == null)
                    return false;

                if (orderItem.Order.Status != OrderStatus.Offen)
                    throw new InvalidOperationException("Cannot modify completed order");

                if (newQuantity <= 0)
                {
                    return await RemoveItemFromOrderAsync(orderId, orderItemId);
                }

                var oldQuantity = orderItem.Quantity;
                orderItem.Quantity = newQuantity;
                orderItem.CreatedAt = DateTime.UtcNow; // Update timestamp

                // Recalculate order totals
                orderItem.Order.CalculateTotals();
                await context.SaveChangesAsync();

                await _databaseService.LogActivityAsync(orderItem.Order.UserId, AuditAction.OrderModified,
                    $"Updated quantity for {orderItem.Product.Name} in order {orderItem.Order.OrderNumber}: {oldQuantity} -> {newQuantity}", AuditLogLevel.Info);

                _logger.LogInformation("Updated quantity for {ProductName} in order {OrderNumber}: {OldQuantity} -> {NewQuantity}",
                    orderItem.Product.Name, orderItem.Order.OrderNumber, oldQuantity, newQuantity);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update item quantity {OrderItemId} in order {OrderId}", orderItemId, orderId);
                return false;
            }
        }

        public async Task<bool> ApplyDiscountToOrderAsync(int orderId, decimal discountAmount, string reason = "Manual discount")
        {
            try
            {
                using var context = _databaseService.GetContext();

                var order = await context.Orders.FindAsync(orderId);
                if (order == null || order.Status != OrderStatus.Offen)
                    return false;

                order.DiscountAmount = discountAmount;
                order.CalculateTotals();
                await context.SaveChangesAsync();

                await _databaseService.LogActivityAsync(order.UserId, AuditAction.OrderModified,
                    $"Applied discount of {discountAmount:C} to order {order.OrderNumber} ({reason})", AuditLogLevel.Info);

                _logger.LogInformation("Applied discount {DiscountAmount:C} to order {OrderNumber}",
                    discountAmount, order.OrderNumber);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply discount to order {OrderId}", orderId);
                return false;
            }
        }

        public async Task<bool> ProcessPaymentAsync(int orderId, PaymentMethod paymentMethod, decimal paidAmount)
        {
            try
            {
                using var context = _databaseService.GetContext();

                var order = await context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null || order.Status != OrderStatus.Offen)
                    return false;

                order.PaymentMethod = paymentMethod;
                order.PaidAmount = paidAmount;
                order.ChangeAmount = paidAmount - order.TotalAmount;

                // Ensure change is not negative
                if (order.ChangeAmount < 0)
                    order.ChangeAmount = 0;

                order.MarkAsPaid();
                await context.SaveChangesAsync();

                // Update product stock
                foreach (var item in order.OrderItems)
                {
                    if (item.Product != null)
                    {
                        item.Product.StockQuantity -= item.Quantity;
                    }
                }

                await context.SaveChangesAsync();

                await _databaseService.LogActivityAsync(order.UserId, AuditAction.OrderPaid,
                    $"Order {order.OrderNumber} paid with {paymentMethod} - Total: {order.TotalAmount:C}, Paid: {paidAmount:C}, Change: {order.ChangeAmount:C}", AuditLogLevel.Info);

                _logger.LogInformation("Order {OrderNumber} paid successfully - Total: {TotalAmount:C}",
                    order.OrderNumber, order.TotalAmount);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process payment for order {OrderId}", orderId);
                return false;
            }
        }

        public async Task<bool> CancelOrderAsync(int orderId, string reason = "Manual cancellation")
        {
            try
            {
                using var context = _databaseService.GetContext();

                var order = await context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                    return false;

                if (order.Status == OrderStatus.Bezahlt)
                    throw new InvalidOperationException("Cannot cancel paid order");

                order.MarkAsCancelled();
                await context.SaveChangesAsync();

                // Restore product stock if order was not paid
                if (order.Status != OrderStatus.Bezahlt)
                {
                    foreach (var item in order.OrderItems)
                    {
                        if (item.Product != null)
                        {
                            item.Product.StockQuantity += item.Quantity;
                        }
                    }
                    await context.SaveChangesAsync();
                }

                await _databaseService.LogActivityAsync(order.UserId, AuditAction.OrderCancelled,
                    $"Order {order.OrderNumber} cancelled ({reason})", AuditLogLevel.Info);

                _logger.LogInformation("Order {OrderNumber} cancelled", order.OrderNumber);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel order {OrderId}", orderId);
                return false;
            }
        }

        public async Task<IEnumerable<Order>> GetOrdersForDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                using var context = _databaseService.GetContext();
                return await context.Orders
                    .Include(o => o.User)
                    .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get orders for date range {StartDate} to {EndDate}", startDate, endDate);
                return new List<Order>();
            }
        }

        public async Task<IEnumerable<Order>> GetOrdersByUserAsync(int userId)
        {
            try
            {
                using var context = _databaseService.GetContext();
                return await context.Orders
                    .Include(o => o.User)
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get orders by user ID: {UserId}", userId);
                return new List<Order>();
            }
        }

        public async Task<decimal> GetDailyRevenueAsync(DateTime date)
        {
            try
            {
                var startOfDay = date.Date;
                var endOfDay = startOfDay.AddDays(1);

                using var context = _databaseService.GetContext();
                return await context.Orders
                    .Where(o => o.Status == OrderStatus.Bezahlt && o.CreatedAt >= startOfDay && o.CreatedAt < endOfDay)
                    .SumAsync(o => o.TotalAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get daily revenue for {Date}", date);
                return 0;
            }
        }

        public async Task<int> GetDailyOrderCountAsync(DateTime date)
        {
            try
            {
                var startOfDay = date.Date;
                var endOfDay = startOfDay.AddDays(1);

                using var context = _databaseService.GetContext();
                return await context.Orders
                    .CountAsync(o => o.CreatedAt >= startOfDay && o.CreatedAt < endOfDay);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get daily order count for {Date}", date);
                return 0;
            }
        }

        private string GenerateOrderNumber()
        {
            var date = DateTime.Now;
            var random = new Random();
            return $"{date:yyyyMMdd}{random.Next(1000, 9999)}";
        }

        public async Task<bool> IsOrderNumberUniqueAsync(string orderNumber, int? excludeOrderId = null)
        {
            try
            {
                using var context = _databaseService.GetContext();
                var query = context.Orders.Where(o => o.OrderNumber == orderNumber);

                if (excludeOrderId.HasValue)
                {
                    query = query.Where(o => o.Id != excludeOrderId.Value);
                }

                return !await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check order number uniqueness: {OrderNumber}", orderNumber);
                return false;
            }
        }
    }
}
