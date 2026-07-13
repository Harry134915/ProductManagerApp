namespace ProductManagerApp.DTO
{
    /// <summary>
    /// 承载业务层返回给列表界面的商品查询结果。
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
