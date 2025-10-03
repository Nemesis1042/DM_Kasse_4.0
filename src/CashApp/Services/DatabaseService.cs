using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using CashApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;

namespace CashApp.Services
{
    public class CashAppDbContext : DbContext
    {
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        public CashAppDbContext(DbContextOptions<CashAppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure decimal precision for money values
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Product>()
                .Property(p => p.TaxRate)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Product>()
                .Property(p => p.DepositAmount)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.Subtotal)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.TaxAmount)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.DepositTotal)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.DiscountAmount)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.PaidAmount)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.ChangeAmount)
                .HasPrecision(10, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.UnitPrice)
                .HasPrecision(10, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.TaxRate)
                .HasPrecision(5, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.DepositAmount)
                .HasPrecision(10, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.DiscountAmount)
                .HasPrecision(10, 2);

            // Configure indexes for better performance
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Barcode)
                .IsUnique();

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Category);

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.IsActive);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Role);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderNumber)
                .IsUnique();

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.CreatedAt);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.Status);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.UserId);

            modelBuilder.Entity<OrderItem>()
                .HasIndex(oi => oi.OrderId);

            modelBuilder.Entity<OrderItem>()
                .HasIndex(oi => oi.ProductId);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(l => l.Timestamp);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(l => l.Level);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(l => l.Action);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(l => l.UserId);
        }
    }

    public class DatabaseService
    {
        private readonly string _databasePath;
        private readonly ILogger<DatabaseService> _logger;
        private CashAppDbContext _context = null!;

        public DatabaseService(string databasePath = "CashApp.db")
        {
            _databasePath = databasePath;
            _logger = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog();
            }).CreateLogger<DatabaseService>();
        }

        public async Task InitializeAsync()
        {
            try
            {
                var options = new DbContextOptionsBuilder<CashAppDbContext>()
                    .UseSqlite($"Data Source={_databasePath}")
                    .UseLoggerFactory(LoggerFactory.Create(builder => builder.AddSerilog()))
                    .Options;

                _context = new CashAppDbContext(options);

                // Ensure database is created and migrations are applied
                await _context.Database.EnsureCreatedAsync();

                // Seed initial data if database is empty
                await SeedInitialDataAsync();

                _logger.LogInformation("Database initialized successfully at {DatabasePath}", _databasePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize database");
                throw;
            }
        }

        private async Task SeedInitialDataAsync()
        {
            if (!await _context.Users.AnyAsync())
            {
                // Create default admin user
                var adminUser = new User
                {
                    Username = "admin",
                    PasswordHash = HashPassword("admin123"), // In production, use a secure password
                    FirstName = "System",
                    LastName = "Administrator",
                    Role = UserRole.Admin,
                    IsActive = true
                };

                await _context.Users.AddAsync(adminUser);

                // Create some sample products
                var sampleProducts = new List<Product>
                {
                    new Product
                    {
                        Name = "Bier 0,5l",
                        Description = "Helles Bier in 0,5l Flasche",
                        Price = 3.50m,
                        TaxRate = 19.00m,
                        Category = ProductCategory.Getränke,
                        RequiresDeposit = true,
                        DepositAmount = 0.08m,
                        IsActive = true,
                        StockQuantity = 100
                    },
                    new Product
                    {
                        Name = "Cola 0,5l",
                        Description = "Cola in 0,5l Flasche",
                        Price = 2.80m,
                        TaxRate = 19.00m,
                        Category = ProductCategory.Getränke,
                        RequiresDeposit = true,
                        DepositAmount = 0.15m,
                        IsActive = true,
                        StockQuantity = 50
                    },
                    new Product
                    {
                        Name = "Currywurst",
                        Description = "Currywurst mit Pommes",
                        Price = 8.50m,
                        TaxRate = 19.00m,
                        Category = ProductCategory.Essen,
                        IsActive = true,
                        StockQuantity = 20
                    }
                };

                await _context.Products.AddRangeAsync(sampleProducts);
                await _context.SaveChangesAsync();

                // Log the creation
                await LogActivityAsync(0, AuditAction.UserCreated, "Default admin user created", AuditLogLevel.Info);
                await LogActivityAsync(0, AuditAction.ProductCreated, "Sample products created", AuditLogLevel.Info);

                _logger.LogInformation("Initial data seeded successfully");
            }
        }

        private string HashPassword(string password)
        {
            // In a real application, use a proper password hashing library like BCrypt
            // For now, we'll use a simple hash for demonstration
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public CashAppDbContext GetContext()
        {
            return _context;
        }

        public async Task LogActivityAsync(int userId, AuditAction action, string message, AuditLogLevel level = AuditLogLevel.Info, string? details = null)
        {
            try
            {
                var log = new AuditLog
                {
                    UserId = userId,
                    Action = action,
                    Message = message,
                    Level = level,
                    Timestamp = DateTime.UtcNow,
                    Details = details
                };

                await _context.AuditLogs.AddAsync(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log activity: {Message}", message);
            }
        }

        public async Task BackupDatabaseAsync(string backupPath)
        {
            try
            {
                // Close the current connection
                await _context.DisposeAsync();

                // Copy the database file
                File.Copy(_databasePath, backupPath, true);

                // Recreate the context
                await InitializeAsync();

                _logger.LogInformation("Database backed up to {BackupPath}", backupPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to backup database");
                throw;
            }
        }

        public async Task RestoreDatabaseAsync(string backupPath)
        {
            try
            {
                // Close the current connection
                await _context.DisposeAsync();

                // Copy the backup file
                File.Copy(backupPath, _databasePath, true);

                // Recreate the context
                await InitializeAsync();

                _logger.LogInformation("Database restored from {BackupPath}", backupPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore database");
                throw;
            }
        }

        public async Task<DatabaseStatistics> GetStatisticsAsync()
        {
            return new DatabaseStatistics
            {
                TotalProducts = await _context.Products.CountAsync(p => p.IsActive),
                TotalUsers = await _context.Users.CountAsync(u => u.IsActive),
                TotalOrders = await _context.Orders.CountAsync(),
                TotalOrderItems = await _context.OrderItems.CountAsync(),
                TotalAuditLogs = await _context.AuditLogs.CountAsync(),
                DatabaseSize = new FileInfo(_databasePath).Length
            };
        }

        public async Task CleanupOldLogsAsync(int daysToKeep = 90)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            var oldLogsCount = await _context.AuditLogs
                .Where(l => l.Timestamp < cutoffDate && l.Level == AuditLogLevel.Debug)
                .CountAsync();

            if (oldLogsCount > 0)
            {
                var deletedCount = await _context.AuditLogs
                    .Where(l => l.Timestamp < cutoffDate && l.Level == AuditLogLevel.Debug)
                    .ExecuteDeleteAsync();

                _logger.LogInformation("Cleaned up {DeletedCount} old debug log entries", deletedCount);
            }
        }
    }

    public class DatabaseStatistics
    {
        public int TotalProducts { get; set; }
        public int TotalUsers { get; set; }
        public int TotalOrders { get; set; }
        public int TotalOrderItems { get; set; }
        public int TotalAuditLogs { get; set; }
        public long DatabaseSize { get; set; }

        public string FormattedDatabaseSize
        {
            get
            {
                const long KB = 1024;
                const long MB = KB * 1024;
                const long GB = MB * 1024;

                return DatabaseSize switch
                {
                    >= GB => $"{DatabaseSize / (double)GB:F2} GB",
                    >= MB => $"{DatabaseSize / (double)MB:F2} MB",
                    >= KB => $"{DatabaseSize / (double)KB:F2} KB",
                    _ => $"{DatabaseSize} Bytes"
                };
            }
        }
    }
}
