using ProductManagerApp.Infrastructure.Input;
using ProductManagerApp.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace ProductManagerApp.Views
{
    public partial class ProductFormView : UserControl
    {
        private const string PriceInputError = "价格只能输入数字和一个小数点。";
        private const string StockInputError = "库存只能输入非负整数。";

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

        private void OnNumericPreviewTextInput(
            object sender,
            TextCompositionEventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                return;
            }

            var candidate = NumericInputRules.ApplyInput(
                textBox.Text,
                textBox.SelectionStart,
                textBox.SelectionLength,
                e.Text);

            if (IsValidNumericCandidate(textBox, candidate))
            {
                return;
            }

            e.Handled = true;
            ReportNumericInputError(textBox);
        }

        private void OnNumericPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (sender is not TextBox textBox ||
                !e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, true))
            {
                e.CancelCommand();
                return;
            }

            var pastedText = e.SourceDataObject.GetData(DataFormats.UnicodeText) as string;
            if (pastedText == null)
            {
                e.CancelCommand();
                return;
            }

            var candidate = NumericInputRules.ApplyInput(
                textBox.Text,
                textBox.SelectionStart,
                textBox.SelectionLength,
                pastedText);

            if (IsValidNumericCandidate(textBox, candidate))
            {
                return;
            }

            e.CancelCommand();
            ReportNumericInputError(textBox);
        }

        private static bool IsValidNumericCandidate(TextBox textBox, string candidate)
        {
            return textBox.Tag switch
            {
                nameof(ProductFormViewModel.Price) =>
                    NumericInputRules.IsValidPriceCandidate(candidate),
                nameof(ProductFormViewModel.Stock) =>
                    NumericInputRules.IsValidStockCandidate(candidate),
                _ => true
            };
        }

        private void ReportNumericInputError(TextBox textBox)
        {
            if (DataContext is not ProductFormViewModel viewModel ||
                textBox.Tag is not string propertyName)
            {
                return;
            }

            var message = propertyName == nameof(ProductFormViewModel.Price)
                ? PriceInputError
                : StockInputError;
            viewModel.ReportInputError(propertyName, message);
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
