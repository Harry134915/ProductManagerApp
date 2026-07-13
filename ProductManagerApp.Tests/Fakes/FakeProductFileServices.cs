using ProductManagerApp.DTO;
using ProductManagerApp.Infrastructure.FileExchange;

namespace ProductManagerApp.Tests.Fakes;

/// <summary>
/// 为主窗口测试提供无文件系统副作用的文件服务、对话框和结果展示替身。
/// </summary>
internal sealed class FakeProductFileService : IProductFileService
{
    public ProductImportReadResult ImportResult { get; set; } = new();
    public int ExportCallCount { get; private set; }
    public IReadOnlyCollection<ProductQueryDto>? LastExportedProducts { get; private set; }

    public ProductImportReadResult ReadImport(string path, ProductFileFormat format) => ImportResult;

    public void Export(string path, ProductFileFormat format, IReadOnlyCollection<ProductQueryDto> products)
    {
        ExportCallCount++;
        LastExportedProducts = products;
    }

    public void WriteTemplate(string path, ProductFileFormat format)
    {
    }

    public void WriteErrorReport(string path, IReadOnlyCollection<ProductImportError> errors)
    {
    }
}

internal sealed class FakeProductFileDialogService : IProductFileDialogService
{
    public FileDialogSelection? ImportSelection { get; set; }
    public FileDialogSelection? SaveSelection { get; set; }

    public FileDialogSelection? OpenImportFile() => ImportSelection;

    public FileDialogSelection? SaveProductFile(string title, string suggestedName) => SaveSelection;

    public string? SaveErrorReport(string suggestedName) => null;
}

internal sealed class FakeProductImportResultPresenter : IProductImportResultPresenter
{
    public IReadOnlyCollection<ProductImportError>? LastErrors { get; private set; }

    public void Show(IReadOnlyCollection<ProductImportError> errors)
    {
        LastErrors = errors;
    }
}
