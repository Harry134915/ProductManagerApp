namespace ProductManagerApp.DTO
{
    /// <summary>
    /// 承载 ViewModel 提交给业务层的新增商品数据。
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
