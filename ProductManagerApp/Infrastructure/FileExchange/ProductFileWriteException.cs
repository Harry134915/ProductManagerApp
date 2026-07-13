namespace ProductManagerApp.Infrastructure.FileExchange
{
    /// <summary>
    /// 表示目标文件被占用或无写入权限等用户可处理的保存失败。
    /// </summary>
    public sealed class ProductFileWriteException : Exception
    {
        public ProductFileWriteException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
