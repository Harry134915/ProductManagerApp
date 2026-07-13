using Microsoft.Win32;

namespace ProductManagerApp.Infrastructure.FileExchange
{
    /// <summary>
    /// 使用系统原生文件对话框选择商品交换文件。
    /// </summary>
    public sealed class ProductFileDialogService : IProductFileDialogService
    {
        private const string ProductFilter =
            "Excel 工作簿 (*.xlsx)|*.xlsx|CSV 文件 (*.csv)|*.csv";

        public FileDialogSelection? OpenImportFile()
        {
            var dialog = new OpenFileDialog
            {
                Title = "导入商品",
                Filter = ProductFilter,
                CheckFileExists = true,
                Multiselect = false
            };

            return dialog.ShowDialog() == true
                ? CreateSelection(dialog.FileName)
                : null;
        }

        public FileDialogSelection? SaveProductFile(string title, string suggestedName)
        {
            var dialog = new SaveFileDialog
            {
                Title = title,
                FileName = suggestedName,
                Filter = ProductFilter,
                FilterIndex = 1,
                DefaultExt = ".xlsx",
                AddExtension = true,
                OverwritePrompt = true
            };

            return dialog.ShowDialog() == true
                ? CreateSelection(dialog.FileName)
                : null;
        }

        public string? SaveErrorReport(string suggestedName)
        {
            var dialog = new SaveFileDialog
            {
                Title = "保存导入错误报告",
                FileName = suggestedName,
                Filter = "CSV 文件 (*.csv)|*.csv",
                DefaultExt = ".csv",
                AddExtension = true,
                OverwritePrompt = true
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        private static FileDialogSelection CreateSelection(string path)
        {
            return new FileDialogSelection(path, ProductFileService.GetFormatFromPath(path));
        }
    }
}
