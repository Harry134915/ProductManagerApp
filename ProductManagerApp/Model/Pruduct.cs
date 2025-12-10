namespace ProductManagerApp.Model
{
    public class Product
    {
        /// <summary>
        /// 获取或设置唯一标识符
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 获取或设置商品名称
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 获取或设置商品价格
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// 获取或设置库存数量
        /// </summary>
        public int Stock { get; set; }

        /// <summary>
        /// 获取或设置商品描述
        /// </summary>
        public string Description { get; set; } = "";
    }
}