using System.Globalization;
using CashApp.Models;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Serilog;

namespace CashApp.Services
{
    public class PdfExportService
    {
        private readonly ILogger<PdfExportService> _logger;

        public PdfExportService()
        {
            _logger = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog();
            }).CreateLogger<PdfExportService>();

            // Set QuestPDF license to community (free)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<string> ExportOrderToPdfAsync(Order order)
        {
            try
            {
                var fileName = $"Bestellung_{order.OrderNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var filePath = Path.Combine(Path.GetTempPath(), fileName);

                await Task.Run(() => GenerateOrderPdf(order, filePath));

                await LogExportActivityAsync(order.UserId, $"Order PDF exported: {order.OrderNumber}");
                _logger.LogInformation("Order PDF exported: {OrderNumber} to {FilePath}", order.OrderNumber, filePath);

                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export order {OrderNumber} to PDF", order.OrderNumber);
                throw;
            }
        }

        public async Task<string> ExportDailyReportToPdfAsync(DateTime date, IEnumerable<Order> orders, decimal totalRevenue)
        {
            try
            {
                var fileName = $"Tagesbericht_{date:yyyyMMdd}.pdf";
                var filePath = Path.Combine(Path.GetTempPath(), fileName);

                await Task.Run(() => GenerateDailyReportPdf(date, orders, totalRevenue, filePath));

                await LogExportActivityAsync(0, $"Daily report PDF exported: {date:dd.MM.yyyy}");
                _logger.LogInformation("Daily report PDF exported for {Date} to {FilePath}", date, filePath);

                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export daily report for {Date} to PDF", date);
                throw;
            }
        }

        public async Task<string> ExportProductCatalogToPdfAsync(IEnumerable<Product> products)
        {
            try
            {
                var fileName = $"Produktkatalog_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var filePath = Path.Combine(Path.GetTempPath(), fileName);

                await Task.Run(() => GenerateProductCatalogPdf(products, filePath));

                await LogExportActivityAsync(0, "Product catalog PDF exported");
                _logger.LogInformation("Product catalog PDF exported to {FilePath}", filePath);

                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export product catalog to PDF");
                throw;
            }
        }

        public async Task<string> ExportStatisticsToPdfAsync(DateTime startDate, DateTime endDate,
            StatisticsData statistics)
        {
            try
            {
                var fileName = $"Statistik_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf";
                var filePath = Path.Combine(Path.GetTempPath(), fileName);

                await Task.Run(() => GenerateStatisticsPdf(startDate, endDate, statistics, filePath));

                await LogExportActivityAsync(0, $"Statistics PDF exported: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}");
                _logger.LogInformation("Statistics PDF exported to {FilePath}", filePath);

                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export statistics to PDF");
                throw;
            }
        }

        private void GenerateOrderPdf(Order order, string filePath)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(2, Unit.Centimetre);
                    page.Header().Column(header =>
                    {
                        header.Item().Text("RECHNUNG").FontSize(24).Bold().AlignCenter();
                        header.Item().PaddingBottom(20);
                        header.Item().Text($"Bestellnummer: {order.OrderNumber}").FontSize(12);
                        header.Item().Text($"Datum: {order.CreatedAt:dd.MM.yyyy HH:mm}").FontSize(12);
                        header.Item().Text($"Kassierer: {order.User.FullName}").FontSize(12);
                    });

                    page.Content().Column(content =>
                    {
                        // Order items table
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3); // Product name
                                columns.RelativeColumn(1); // Quantity
                                columns.RelativeColumn(2); // Unit price
                                columns.RelativeColumn(2); // Total
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Artikel").Bold();
                                header.Cell().Text("Menge").Bold();
                                header.Cell().Text("Einzelpreis").Bold();
                                header.Cell().Text("Gesamt").Bold();
                            });

                            foreach (var item in order.OrderItems)
                            {
                                table.Cell().Text(item.Product.Name);
                                table.Cell().Text(item.Quantity.ToString());
                                table.Cell().Text(item.UnitPrice.ToString("C", CultureInfo.GetCultureInfo("de-DE")));
                                table.Cell().Text(item.TotalAmount.ToString("C", CultureInfo.GetCultureInfo("de-DE")));
                            }
                        });

                        content.Item().PaddingTop(20);
                        content.Item().Column(totals =>
                        {
                            totals.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Zwischensumme:");
                                row.RelativeItem(2).Text(order.Subtotal.ToString("C", CultureInfo.GetCultureInfo("de-DE")));
                            });

                            totals.Item().Row(row =>
                            {
                                row.RelativeItem().Text("MwSt 19%:");
                                row.RelativeItem(2).Text(order.TaxAmount.ToString("C", CultureInfo.GetCultureInfo("de-DE")));
                            });

                            if (order.DepositTotal > 0)
                            {
                                totals.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Pfand:");
                                    row.RelativeItem(2).Text(order.DepositTotal.ToString("C", CultureInfo.GetCultureInfo("de-DE")));
                                });
                            }

                            if (order.DiscountAmount > 0)
                            {
                                totals.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Rabatt:");
                                    row.RelativeItem(2).Text($"-{order.DiscountAmount.ToString("C", CultureInfo.GetCultureInfo("de-DE"))}");
                                });
                            }

                            totals.Item().PaddingTop(10);
                            totals.Item().Row(row =>
                            {
                                row.RelativeItem().Text("GESAMT:").Bold();
                                row.RelativeItem(2).Text(order.TotalAmount.ToString("C", CultureInfo.GetCultureInfo("de-DE"))).Bold();
                            });
                        });

                        // Payment info
                        if (order.PaidAmount > 0)
                        {
                            content.Item().PaddingTop(20);
                            content.Item().Column(payment =>
                            {
                                payment.Item().Text("Zahlungsinformationen:").Bold();
                                payment.Item().Text($"Zahlungsart: {order.PaymentMethod}");
                                payment.Item().Text($"Bezahlt: {order.PaidAmount.ToString("C", CultureInfo.GetCultureInfo("de-DE"))}");

                                if (order.ChangeAmount > 0)
                                {
                                    payment.Item().Text($"Rückgeld: {order.ChangeAmount.ToString("C", CultureInfo.GetCultureInfo("de-DE"))}");
                                }
                            });
                        }
                    });

                    page.Footer().Column(footer =>
                    {
                        footer.Item().Text("Vielen Dank für Ihren Einkauf!").AlignCenter();
                        footer.Item().Text(DateTime.Now.ToString("dd.MM.yyyy HH:mm")).AlignRight().FontSize(8);
                    });
                });
            });

            document.GeneratePdf(filePath);
        }

        private void GenerateDailyReportPdf(DateTime date, IEnumerable<Order> orders, decimal totalRevenue, string filePath)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(2, Unit.Centimetre);
                    page.Header().Column(header =>
                    {
                        header.Item().Text("TAGESABSCHLUSS").FontSize(24).Bold().AlignCenter();
                        header.Item().Text($"Datum: {date:dd.MM.yyyy}").FontSize(16).AlignCenter();
                        header.Item().PaddingBottom(20);
                    });

                    page.Content().Column(content =>
                    {
                        // Summary
                        content.Item().Column(summary =>
                        {
                            summary.Item().Text("Zusammenfassung:").Bold().FontSize(14);
                            summary.Item().PaddingBottom(10);

                            var orderCount = orders.Count();
                            summary.Item().Text($"Anzahl Bestellungen: {orderCount}");
                            summary.Item().Text($"Gesamtumsatz: {totalRevenue.ToString("C", CultureInfo.GetCultureInfo("de-DE"))}");
                        });

                        content.Item().PaddingTop(20);

                        // Orders table
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2); // Order number
                                columns.RelativeColumn(2); // Time
                                columns.RelativeColumn(2); // Amount
                                columns.RelativeColumn(2); // Payment method
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Bestellung").Bold();
                                header.Cell().Text("Zeit").Bold();
                                header.Cell().Text("Betrag").Bold();
                                header.Cell().Text("Zahlung").Bold();
                            });

                            foreach (var order in orders.OrderBy(o => o.CreatedAt))
                            {
                                table.Cell().Text(order.OrderNumber);
                                table.Cell().Text(order.CreatedAt.ToString("HH:mm"));
                                table.Cell().Text(order.TotalAmount.ToString("C", CultureInfo.GetCultureInfo("de-DE")));
                                table.Cell().Text(order.PaymentMethod.ToString());
                            }
                        });
                    });

                    page.Footer().Column(footer =>
                    {
                        footer.Item().Text("Automatisch generiert am " + DateTime.Now.ToString("dd.MM.yyyy HH:mm")).AlignCenter().FontSize(8);
                    });
                });
            });

            document.GeneratePdf(filePath);
        }

        private void GenerateProductCatalogPdf(IEnumerable<Product> products, string filePath)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(2, Unit.Centimetre);
                    page.Header().Column(header =>
                    {
                        header.Item().Text("PRODUKTKATALOG").FontSize(24).Bold().AlignCenter();
                        header.Item().Text($"Stand: {DateTime.Now:dd.MM.yyyy}").FontSize(12).AlignCenter();
                        header.Item().PaddingBottom(20);
                    });

                    page.Content().Column(content =>
                    {
                        var productsList = products.OrderBy(p => p.Category).ThenBy(p => p.Name).ToList();

                        foreach (var category in Enum.GetValues(typeof(ProductCategory)).Cast<ProductCategory>())
                        {
                            var categoryProducts = productsList.Where(p => p.Category == category).ToList();
                            if (!categoryProducts.Any()) continue;

                            content.Item().Text(category.ToString().ToUpper()).Bold().FontSize(14);
                            content.Item().PaddingBottom(10);

                            content.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(4); // Product name
                                    columns.RelativeColumn(2); // Price
                                    columns.RelativeColumn(1); // Stock
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text("Produkt").Bold();
                                    header.Cell().Text("Preis").Bold();
                                    header.Cell().Text("Lager").Bold();
                                });

                                foreach (var product in categoryProducts)
                                {
                                    table.Cell().Text(product.Name);
                                    table.Cell().Text(product.Price.ToString("C", CultureInfo.GetCultureInfo("de-DE")));
                                    table.Cell().Text(product.StockQuantity.ToString());
                                }
                            });

                            content.Item().PaddingBottom(20);
                        }
                    });

                    page.Footer().Column(footer =>
                    {
                        footer.Item().Text($"Produkte insgesamt: {products.Count()}").AlignCenter();
                    });
                });
            });

            document.GeneratePdf(filePath);
        }

        private void GenerateStatisticsPdf(DateTime startDate, DateTime endDate, StatisticsData statistics, string filePath)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(2, Unit.Centimetre);
                    page.Header().Column(header =>
                    {
                        header.Item().Text("UMSATZSTATISTIK").FontSize(24).Bold().AlignCenter();
                        header.Item().Text($"Zeitraum: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}").FontSize(12).AlignCenter();
                        header.Item().PaddingBottom(20);
                    });

                    page.Content().Column(content =>
                    {
                        // Summary
                        content.Item().Column(summary =>
                        {
                            summary.Item().Text("Zusammenfassung:").Bold().FontSize(14);
                            summary.Item().PaddingBottom(10);
                            summary.Item().Text($"Gesamtumsatz: {statistics.TotalRevenue.ToString("C", CultureInfo.GetCultureInfo("de-DE"))}");
                            summary.Item().Text($"Anzahl Bestellungen: {statistics.TotalOrders}");
                            summary.Item().Text($"Durchschnittlicher Bestellwert: {statistics.AverageOrderValue.ToString("C", CultureInfo.GetCultureInfo("de-DE"))}");
                        });

                        content.Item().PaddingTop(20);

                        // Daily breakdown
                        if (statistics.DailyRevenue.Any())
                        {
                            content.Item().Text("Täglicher Umsatz:").Bold().FontSize(14);
                            content.Item().PaddingBottom(10);

                            content.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2); // Date
                                    columns.RelativeColumn(2); // Revenue
                                    columns.RelativeColumn(1); // Orders
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text("Datum").Bold();
                                    header.Cell().Text("Umsatz").Bold();
                                    header.Cell().Text("Bestellungen").Bold();
                                });

                                foreach (var day in statistics.DailyRevenue.OrderBy(d => d.Key))
                                {
                                    table.Cell().Text(day.Key.ToString("dd.MM.yyyy"));
                                    table.Cell().Text(day.Value.ToString("C", CultureInfo.GetCultureInfo("de-DE")));
                                    table.Cell().Text(statistics.DailyOrderCount.ContainsKey(day.Key) ?
                                        statistics.DailyOrderCount[day.Key].ToString() : "0");
                                }
                            });
                        }
                    });

                    page.Footer().Column(footer =>
                    {
                        footer.Item().Text("Automatisch generiert am " + DateTime.Now.ToString("dd.MM.yyyy HH:mm")).AlignCenter().FontSize(8);
                    });
                });
            });

            document.GeneratePdf(filePath);
        }

        private async Task LogExportActivityAsync(int userId, string message)
        {
            try
            {
                var databaseService = App.ServiceProvider.GetRequiredService<DatabaseService>();
                await databaseService.LogActivityAsync(userId, AuditAction.ReportExported, message, AuditLogLevel.Info);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log export activity: {Message}", message);
            }
        }
    }

    public class StatisticsData
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue => TotalOrders > 0 ? TotalRevenue / TotalOrders : 0;
        public Dictionary<DateTime, decimal> DailyRevenue { get; set; } = new();
        public Dictionary<DateTime, int> DailyOrderCount { get; set; } = new();
        public Dictionary<ProductCategory, decimal> RevenueByCategory { get; set; } = new();
        public Dictionary<PaymentMethod, int> PaymentMethodUsage { get; set; } = new();
    }
}
