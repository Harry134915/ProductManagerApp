using ProductManagerApp.DAL;
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
        private IProductsDAL m_productDAO;

        public ProductsBLL()
        {
            m_productDAO = new ProductsSqliteDAL();
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

        public void UpdateProductPrice(int productId, double newPrice)
        {
            var product = m_productDAO.GetProductById(productId);
            if (product == null)
            {
                return;
            }

            product.Price = newPrice;
            //TODO:如果我想要有一个更新指定字段的方法.
            m_productDAO.UpdateProduct(product);
        }

        public void UpdateProduct(Product product)
        {
            throw new NotImplementedException();
        }

        public void DeleteProduct(int productId)
        {
            throw new NotImplementedException();
        }
    }
}