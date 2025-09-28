using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CashApp.Views
{
    public partial class StatsTab : UserControl
    {
        public StatsTab()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
