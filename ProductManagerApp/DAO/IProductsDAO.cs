using ProductManagerApp.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductManagerApp.DAO
{
    public interface IProductsDAO
    {
        int AddProduct(Product product);
        List<Product> GetAllProducts();
        DataTable QueryProducts();
        int UpdateProduct(Product product);
    }
}
