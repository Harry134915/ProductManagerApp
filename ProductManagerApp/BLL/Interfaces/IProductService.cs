using ProductManagerApp.DTO;

namespace ProductManagerApp.BLL.Interfaces
{
    /// <summary>
    /// 定义 ViewModel 可调用的商品查询和写入用例。
    /// </summary>
    public interface IProductService
    {
        List<ProductQueryDto> GetAllProducts();

        void AddProduct(ProductCreateDto dto);

        /// <summary>
        /// 完整校验一批商品后，将其作为一个不可分割的批次写入数据库。
        /// </summary>
        int ImportProducts(IReadOnlyCollection<ProductCreateDto> products);

        void DeleteProduct(int productId);

        void UpdateProduct(ProductUpdateDto dto);

        void UpdateProductPrice(int productId, decimal newPrice);
    }
}
