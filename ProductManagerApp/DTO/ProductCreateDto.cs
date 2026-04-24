namespace ProductManagerApp.DTO
{
    /// <summary>
    /// 商品创建数据传输对象（DTO）
    /// 用于新增商品时在各层之间传递数据（如 UI → Service → DAL）
    /// 仅承载数据，不包含任何业务逻辑或行为
    /// </summary>
    public class ProductCreateDto
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Description { get; set; } = "";
    }
}
