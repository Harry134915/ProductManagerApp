using ProductManagerApp.Views;
using System.Windows;

namespace ProductManagerApp.Infrastructure.FileExchange
{
    /// <summary>
    /// 创建模态结果窗口，并为错误报告导出提供所需服务。
    /// </summary>
    public sealed class ProductImportResultPresenter : IProductImportResultPresenter
    {
        private readonly IProductFileService _fileService;
        private readonly IProductFileDialogService _dialogService;

        public ProductImportResultPresenter(
            IProductFileService fileService,
            IProductFileDialogService dialogService)
        {
            _fileService = fileService;
            _dialogService = dialogService;
        }

        public void Show(IReadOnlyCollection<ProductImportError> errors)
        {
            var window = new ImportResultWindow(errors, _fileService, _dialogService)
            {
                Owner = Application.Current?.MainWindow
            };
            window.ShowDialog();
        }
    }
}
