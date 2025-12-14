namespace ProductManagerApp.DTO
{
    /// <summary>
    /// UI → BLL 用的数据对象（新增商品）
    /// 不包含 Id
    /// 不包含任何业务逻辑
    /// </summary>
    public class ProductCreateDto
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Description { get; set; }
    }
}
