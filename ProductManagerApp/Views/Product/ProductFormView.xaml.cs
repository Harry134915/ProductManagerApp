using ProductManagerApp.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace ProductManagerApp.Views
{
    public partial class ProductFormView : UserControl
    {
        public ProductFormView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is ProductFormViewModel oldViewModel)
            {
                oldViewModel.FocusRequested -= OnFocusRequested;
            }

            if (e.NewValue is ProductFormViewModel newViewModel)
            {
                newViewModel.FocusRequested += OnFocusRequested;
            }
        }

        private void OnFieldLostKeyboardFocus(
            object sender,
            KeyboardFocusChangedEventArgs e)
        {
            if (DataContext is ProductFormViewModel viewModel &&
                sender is FrameworkElement { Tag: string propertyName })
            {
                viewModel.ValidateField(propertyName);
            }
        }

        private void OnFocusRequested(string propertyName)
        {
            var textBox = propertyName switch
            {
                nameof(ProductFormViewModel.Code) => CodeBox,
                nameof(ProductFormViewModel.Name) => NameBox,
                nameof(ProductFormViewModel.Price) => PriceBox,
                nameof(ProductFormViewModel.Stock) => StockBox,
                nameof(ProductFormViewModel.Description) => DescBox,
                _ => null
            };

            if (textBox == null)
            {
                return;
            }

            Dispatcher.BeginInvoke(() =>
            {
                textBox.Focus();
                textBox.SelectAll();
            }, DispatcherPriority.Input);
        }
    }
}
