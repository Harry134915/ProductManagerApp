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

        void DeleteProduct(int productId);

        void UpdateProduct(ProductUpdateDto dto);

        void UpdateProductPrice(int productId, decimal newPrice);
    }
}
