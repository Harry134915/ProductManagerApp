using ProductManagerApp.DTO;

namespace ProductManagerApp.Infrastructure.FileExchange
{
    /// <summary>
    /// 将已解析商品与其源文件行号关联，供后续重复校验和错误展示使用。
    /// </summary>
    public sealed record ProductImportRecord(int RowNumber, ProductCreateDto Product);
}
