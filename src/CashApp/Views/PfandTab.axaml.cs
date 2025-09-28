using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CashApp.Views
{
    public partial class PfandTab : UserControl
    {
        public PfandTab()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
