namespace ProductManagerApp.Infrastructure.FileExchange
{
    /// <summary>
    /// 表示导入文件被占用或无读取权限等用户可处理的读取失败。
    /// </summary>
    public sealed class ProductFileReadException : Exception
    {
        public ProductFileReadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
