using ProductManagerApp.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProductManagerApp.Views
{
    public partial class ProductListView : UserControl
    {
        public ProductListView()
        {
            InitializeComponent();
        }

        private void OnProductDataGridPreviewKeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.Key != Key.Delete ||
                Window.GetWindow(this)?.DataContext is not MainWindowViewModel viewModel ||
                !viewModel.DeleteCommand.CanExecute(null))
            {
                return;
            }

            viewModel.DeleteCommand.Execute(null);
            e.Handled = true;
        }
    }
}
