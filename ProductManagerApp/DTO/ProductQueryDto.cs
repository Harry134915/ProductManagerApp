namespace ProductManagerApp.DTO
{
    /// <summary>
    /// 商品查询数据传输对象（DTO）
    /// 用于商品数据查询/列表展示时在各层之间传递数据
    /// 通常由 Entity 映射得到，用于 UI 展示或接口返回
    /// 仅承载数据，不包含任何业务逻辑
    /// </summary>
    public class ProductQueryDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Description { get; set; } = "";
    }
}
