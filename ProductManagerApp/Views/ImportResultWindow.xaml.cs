using ProductManagerApp.Infrastructure.FileExchange;
using System.Windows;

namespace ProductManagerApp.Views
{
    /// <summary>
    /// 展示导入预检错误，并允许用户将相同明细保存为 CSV。
    /// </summary>
    public partial class ImportResultWindow : Window
    {
        private readonly IReadOnlyCollection<ProductImportError> _errors;
        private readonly IProductFileService _fileService;
        private readonly IProductFileDialogService _dialogService;

        public ImportResultWindow(
            IReadOnlyCollection<ProductImportError> errors,
            IProductFileService fileService,
            IProductFileDialogService dialogService)
        {
            InitializeComponent();
            _errors = errors;
            _fileService = fileService;
            _dialogService = dialogService;
            DataContext = errors;
        }

        private void SaveReport_Click(object sender, RoutedEventArgs e)
        {
            var path = _dialogService.SaveErrorReport(
                $"商品导入错误-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
            if (path == null)
            {
                return;
            }

            try
            {
                _fileService.WriteErrorReport(path, _errors);
                MessageBox.Show(this, "错误报告已保存。", "保存成功",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, $"保存错误报告失败：{exception.Message}", "保存失败",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
