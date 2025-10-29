using ProductManagerApp.Models;
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
        void AddProduct(Product product);
        void DeleteProduct(int productId);
        List<Product> GetAllProducts();
        DataTable QueryProducts();
        void UpdateProduct(Product product);
        void UpdateProductPrice(Product product, double newPrice);
    }
}
