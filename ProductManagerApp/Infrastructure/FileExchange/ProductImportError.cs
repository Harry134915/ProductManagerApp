namespace ProductManagerApp.Infrastructure.FileExchange
{
    /// <summary>
    /// 描述导入文件中一条可定位到源行和字段的错误。
    /// </summary>
    public sealed record ProductImportError(int RowNumber, string Field, string Message);
}
