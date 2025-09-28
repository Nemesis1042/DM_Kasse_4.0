using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CashApp.Services;
using CashApp.Views;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.IO;

namespace CashApp
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs/cashapp-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                // Set up dependency injection
                var services = new ServiceCollection();
                ConfigureServices(services);
                ServiceProvider = services.BuildServiceProvider();

                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    // Initialize database
                    var databaseService = ServiceProvider.GetRequiredService<DatabaseService>();
                    databaseService.InitializeAsync().Wait();

                    // Show login window first
                    var loginWindow = new LoginWindow();
                    loginWindow.Show();

                    desktop.MainWindow = loginWindow;

                    // Handle shutdown
                    desktop.ShutdownRequested += (sender, e) =>
                    {
                        Log.CloseAndFlush();
                    };
                }

                base.OnFrameworkInitializationCompleted();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Application startup failed");
                throw;
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register services
            services.AddSingleton<DatabaseService>();
            services.AddSingleton<AuthService>();
            services.AddSingleton<ProductService>();
            services.AddSingleton<OrderService>();
            services.AddSingleton<PfandService>();
            services.AddSingleton<PrinterService>();
            services.AddSingleton<BackupService>();
            services.AddSingleton<PdfExportService>();

            // Register ViewModels
            services.AddTransient<CashTabViewModel>();
            services.AddTransient<ProductTabViewModel>();
            services.AddTransient<PfandTabViewModel>();
            services.AddTransient<StatsTabViewModel>();
            services.AddTransient<SettingsTabViewModel>();
            services.AddTransient<UserTabViewModel>();

            // Register Views
            services.AddTransient<MainWindow>();
            services.AddTransient<LoginWindow>();
        }
    }
}
