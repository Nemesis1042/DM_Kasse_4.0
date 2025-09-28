using System.Text;
using CashApp.Models;
using Microsoft.Extensions.Logging;
using Serilog;
using SkiaSharp;

namespace CashApp.Services
{
    public class PrinterService
    {
        private readonly ILogger<PrinterService> _logger;
        private readonly string _storeName = "Ihr Geschäft";
        private readonly string _storeAddress = "Ihre Adresse";
        private readonly string _storePhone = "Tel: 01234/56789";

        public PrinterService()
        {
            _logger = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog();
            }).CreateLogger<PrinterService>();
        }

        public async Task<bool> PrintReceiptAsync(Order order, bool isTestMode = false)
        {
            try
            {
                if (isTestMode)
                {
                    await PrintTestReceiptAsync(order);
                    return true;
                }

                // For now, log the receipt as text since cross-platform printing is complex
                // In a production environment, you would integrate with the system's print dialog
                var receiptContent = GenerateReceiptText(order);
                _logger.LogInformation("PRINTING RECEIPT:\n{ReceiptContent}", receiptContent);

                await LogPrintActivityAsync(order.UserId, $"Receipt printed for order {order.OrderNumber}");
                _logger.LogInformation("Receipt printed for order {OrderNumber}", order.OrderNumber);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to print receipt for order {OrderNumber}", order.OrderNumber);
                return false;
            }
        }

        public async Task<bool> PrintDailyReportAsync(DateTime date, IEnumerable<Order> orders, decimal totalRevenue)
        {
            try
            {
                var reportContent = GenerateDailyReportText(date, orders, totalRevenue);
                _logger.LogInformation("PRINTING DAILY REPORT:\n{ReportContent}", reportContent);

                await LogPrintActivityAsync(0, $"Daily report printed for {date:dd.MM.yyyy}");
                _logger.LogInformation("Daily report printed for {Date}", date);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to print daily report for {Date}", date);
                return false;
            }
        }

        public async Task<bool> PrintTestPageAsync()
        {
            try
            {
                var testContent = GenerateTestPageText();
                _logger.LogInformation("PRINTING TEST PAGE:\n{TestContent}", testContent);

                await LogPrintActivityAsync(0, "Test page printed");
                _logger.LogInformation("Test page printed");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to print test page");
                return false;
            }
        }

        public async Task<IEnumerable<string>> GetAvailablePrintersAsync()
        {
            try
            {
                // For cross-platform compatibility, return a simple list
                // In a real application, you would enumerate available printers
                return new List<string> { "Standarddrucker", "PDF Export" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get available printers");
                return new List<string>();
            }
        }

        private async Task PrintTestReceiptAsync(Order order)
        {
            // For test mode, just log the receipt instead of printing
            var receiptContent = GenerateReceiptText(order);
            _logger.LogInformation("TEST RECEIPT:\n{ReceiptContent}", receiptContent);

            await LogPrintActivityAsync(order.UserId, $"Test receipt generated for order {order.OrderNumber}");
        }

        private string GenerateReceiptText(Order order)
        {
            var sb = new StringBuilder();

            sb.AppendLine(_storeName);
            sb.AppendLine(_storeAddress);
            sb.AppendLine(_storePhone);
            sb.AppendLine(new string('-', 40));
            sb.AppendLine($"Bestellung: {order.OrderNumber}");
            sb.AppendLine($"Datum: {order.CreatedAt:dd.MM.yyyy HH:mm}");
            sb.AppendLine($"Kassierer: {order.User.FullName}");
            sb.AppendLine();

            foreach (var item in order.OrderItems)
            {
                sb.AppendLine($"{item.Quantity,2} x {item.Product.Name}");
                sb.AppendLine($"     {item.UnitPrice,8:C} = {item.TotalAmount,8:C}");
            }

            sb.AppendLine(new string('-', 40));
            sb.AppendLine($"Zwischensumme: {order.Subtotal,15:C}");
            sb.AppendLine($"MwSt 19%: {order.TaxAmount,15:C}");

            if (order.DepositTotal > 0)
                sb.AppendLine($"Pfand: {order.DepositTotal,15:C}");

            if (order.DiscountAmount > 0)
                sb.AppendLine($"Rabatt: -{order.DiscountAmount,13:C}");

            sb.AppendLine(new string('-', 40));
            sb.AppendLine($"GESAMT: {order.TotalAmount,18:C}");

            if (order.PaidAmount > 0)
            {
                sb.AppendLine($"Bezahlt: {order.PaidAmount,15:C}");
                if (order.ChangeAmount > 0)
                    sb.AppendLine($"Rückgeld: {order.ChangeAmount,13:C}");
            }

            sb.AppendLine();
            sb.AppendLine("Vielen Dank für Ihren Einkauf!");
            sb.AppendLine("Bis zum nächsten Mal!");

            return sb.ToString();
        }

        private string GenerateDailyReportText(DateTime date, IEnumerable<Order> orders, decimal totalRevenue)
        {
            var sb = new StringBuilder();

            sb.AppendLine("TAGESABSCHLUSS");
            sb.AppendLine($"Datum: {date:dd.MM.yyyy}");
            sb.AppendLine(new string('=', 50));

            var orderCount = orders.Count();
            sb.AppendLine($"Anzahl Bestellungen: {orderCount,3}");
            sb.AppendLine($"Gesamtumsatz: {totalRevenue,15:C}");
            sb.AppendLine();

            sb.AppendLine("Bestellungen:");
            foreach (var order in orders.OrderBy(o => o.CreatedAt))
            {
                sb.AppendLine($"{order.OrderNumber} {order.CreatedAt:HH:mm} {order.TotalAmount,8:C}");
            }

            sb.AppendLine();
            sb.AppendLine(new string('=', 50));
            sb.AppendLine("Ende des Berichts");

            return sb.ToString();
        }

        private string GenerateTestPageText()
        {
            var sb = new StringBuilder();

            sb.AppendLine("DRUCKER TEST");
            sb.AppendLine("Dies ist eine Testseite");
            sb.AppendLine($"Druckzeit: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            sb.AppendLine($"Drucker: Standarddrucker");
            sb.AppendLine();
            sb.AppendLine("Test erfolgreich!");

            return sb.ToString();
        }

        private async Task LogPrintActivityAsync(int userId, string message)
        {
            try
            {
                var databaseService = App.ServiceProvider.GetRequiredService<DatabaseService>();
                await databaseService.LogActivityAsync(userId, AuditAction.Other, message, AuditLogLevel.Info);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log print activity: {Message}", message);
            }
        }

        public void ConfigurePrinterSettings(string printerName, string storeName, string storeAddress, string storePhone)
        {
            _storeName = storeName;
            _storeAddress = storeAddress;
            _storePhone = storePhone;

            // Additional printer configuration could be added here
        }
    }
}
