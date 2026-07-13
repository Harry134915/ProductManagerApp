namespace ProductManagerApp.Infrastructure.FileExchange
{
    /// <summary>
    /// 返回一次文件预检得到的有效记录、行级错误和原始数据行数。
    /// </summary>
    public sealed class ProductImportReadResult
    {
        public List<ProductImportRecord> Records { get; } = new();

        public List<ProductImportError> Errors { get; } = new();

        public int TotalRows { get; set; }

        public bool IsValid => TotalRows > 0 && Errors.Count == 0;
    }
}
