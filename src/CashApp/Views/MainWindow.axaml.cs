using Avalonia.Controls;
using Avalonia.Threading;
using CashApp.ViewModels;

namespace CashApp.Views
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();

            // Update time every second
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (sender, e) => UpdateCurrentTime();
            _timer.Start();

            UpdateCurrentTime();

            // Handle window closing
            Closing += (sender, e) =>
            {
                if (DataContext is MainWindowViewModel viewModel)
                {
                    viewModel.Logout();
                }
                _timer.Stop();
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void UpdateCurrentTime()
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.UpdateCurrentTime();
            }
        }
    }
}
