namespace ProductManagerApp.Infrastructure.FileExchange
{
    /// <summary>
    /// 隔离 WPF 文件对话框，便于 ViewModel 测试用户确认和取消路径。
    /// </summary>
    public interface IProductFileDialogService
    {
        FileDialogSelection? OpenImportFile();

        FileDialogSelection? SaveProductFile(string title, string suggestedName);

        string? SaveErrorReport(string suggestedName);
    }
}
