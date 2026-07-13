using ProductManagerApp.Entity;
using System.Collections.Generic;

namespace ProductManagerApp.DAL
{
    /// <summary>
    /// 商品数据访问接口。
    /// </summary>
    public interface IProductRepository
    {
        // 查询全部
        List<Product> GetAllProducts();

        // 查询一个
        Product? GetProductById(int id);

        // 按编码查询
        Product? GetProductByCode(string code);

        // 新增
        int AddProduct(Product product);

        /// <summary>
        /// 在同一事务中新增整批商品，任一写入失败时不保留部分数据。
        /// </summary>
        int AddProducts(IReadOnlyCollection<Product> products);

        // 更新
        int UpdateProduct(Product product);

        // 仅更新价格
        int UpdateProductPrice(int productId, decimal newPrice);

        // 删除
        int DeleteProduct(int productId);
    }
}
