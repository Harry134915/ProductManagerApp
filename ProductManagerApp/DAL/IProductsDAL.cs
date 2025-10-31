using ProductManagerApp.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductManagerApp.DAL
{
    /// <summary>
    /// 产品数据访问对象接口，定义了产品相关的数据操作方法。
    /// </summary>
    public interface IProductsDAL
    {
        /// <summary>
        /// 添加一个新产品。
        /// </summary>
        /// <param name="product">要添加的产品对象。</param>
        /// <returns>受影响的行数。</returns>
        int AddProduct(Product product);

        /// <summary>
        /// 获取所有产品的列表。
        /// </summary>
        /// <returns>产品列表。</returns>
        List<Product> GetAllProducts();

        /// <summary>
        /// 查询所有产品并以 DataTable 形式返回。
        /// </summary>
        /// <returns>包含产品信息的 DataTable。</returns>
        DataTable QueryProducts();

        /// <summary>
        /// 根据产品ID获取产品信息。
        /// </summary>
        /// <param name="productId">产品ID。</param>
        /// <returns>对应的产品对象。</returns>
        Product GetProductById(int productId);

        /// <summary>
        /// 更新产品信息。
        /// </summary>
        /// <param name="product">要更新的产品对象。</param>
        /// <returns>受影响的行数。</returns>
        int UpdateProduct(Product product);
    }
}