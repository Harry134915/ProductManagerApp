namespace ProductManagerApp.Infrastructure.FileExchange
{
    /// <summary>
    /// 将用户选择的文件路径和格式作为一个不可分割的结果返回。
    /// </summary>
    public sealed record FileDialogSelection(string Path, ProductFileFormat Format);
}
