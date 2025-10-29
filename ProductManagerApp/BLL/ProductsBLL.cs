using ProductManagerApp.DAO;
using ProductManagerApp.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductManagerApp.BLL
{
    internal class ProductsBLL : IProductsBLL
    {
        private IProductsDAO m_productDAO;
        public ProductsBLL()
        {
            m_productDAO = new ProductsSqliteDAO();
        }

        public DataTable QueryProducts()
        {
            return m_productDAO.QueryProducts();
        }

        public List<Product> GetAllProducts()
        {
            return m_productDAO.GetAllProducts();
        }

        public void AddProduct(Product product)
        {
            m_productDAO.AddProduct(product);
        }

        public void UpdateProductPrice(Product product, double newPrice)
        {
            product.Price = newPrice;

            m_productDAO.UpdateProduct(product);
        }

        public void UpdateProduct(Product product)
        {

        }

        public void DeleteProduct(int productId)
        {
        }
    }
}
