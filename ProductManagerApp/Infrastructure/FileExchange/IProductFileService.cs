using ProductManagerApp.DTO;

namespace ProductManagerApp.Infrastructure.FileExchange
{
    /// <summary>
    /// 定义商品 CSV/XLSX 的解析、导出、模板和错误报告能力。
    /// </summary>
    public interface IProductFileService
    {
        ProductImportReadResult ReadImport(string path, ProductFileFormat format);

        void Export(string path, ProductFileFormat format, IReadOnlyCollection<ProductQueryDto> products);

        void WriteTemplate(string path, ProductFileFormat format);

        void WriteErrorReport(string path, IReadOnlyCollection<ProductImportError> errors);
    }
}
