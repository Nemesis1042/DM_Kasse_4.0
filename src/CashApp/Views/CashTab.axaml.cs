using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CashApp.ViewModels;

namespace CashApp.Views
{
    public partial class CashTab : UserControl
    {
        public CashTab()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
