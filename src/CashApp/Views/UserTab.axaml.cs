using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CashApp.Views
{
    public partial class UserTab : UserControl
    {
        public UserTab()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
