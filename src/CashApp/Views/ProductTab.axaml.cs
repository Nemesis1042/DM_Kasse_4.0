using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CashApp.Views
{
    public partial class ProductTab : UserControl
    {
        public ProductTab()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
