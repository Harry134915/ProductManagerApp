using ProductManagerApp.Entity;
using System.Collections.Generic;

namespace ProductManagerApp.DAL
{
    /// <summary>
    /// 产品数据访问接口
    /// 只返回 Model，不再使用 DataTable
    /// </summary>
    public interface IProductsDAL
    {
        // 查询全部
        List<Product> GetAllProducts();

        // 查询一个
        Product? GetProductById(int id);

        // 新增
        int AddProduct(Product product);

        // 更新
        int UpdateProduct(Product product);

        // 仅更新价格
        int UpdateProductPrice(int productId, decimal newPrice);

        // 删除
        int DeleteProduct(int productId);
    }
}