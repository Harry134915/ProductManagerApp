using ProductManagerApp.Entity;
using ProductManagerApp.DTO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductManagerApp.BLL
{
    public interface IProductsBLL
    {
        //查询
        List<Product> GetAllProducts();

        //新增
        void AddProduct(ProductCreateDto dto);

        //删除
        void DeleteProduct(int productId);

        //更新
        void UpdateProduct(Product product);

        void UpdateProductPrice(int productId, decimal newPrice);
    }
}