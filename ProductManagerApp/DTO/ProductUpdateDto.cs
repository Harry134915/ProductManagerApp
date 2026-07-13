namespace ProductManagerApp.DTO
{
    /// <summary>
    /// 承载 ViewModel 提交给业务层的商品标识和可编辑字段。
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
