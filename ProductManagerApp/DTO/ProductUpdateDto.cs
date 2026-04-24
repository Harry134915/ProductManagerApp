namespace ProductManagerApp.DTO
{
    /// <summary>
    /// 商品更新数据传输对象（DTO）
    /// 用于更新商品信息时在各层之间传递数据
    /// 必须包含商品Id，用于定位需要更新的记录
    /// 通常由 UI 或查询结果映射生成
    /// 仅承载数据，不包含任何业务逻辑
    /// </summary>
    public class ProductUpdateDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Description { get; set; } = "";
    }
}
