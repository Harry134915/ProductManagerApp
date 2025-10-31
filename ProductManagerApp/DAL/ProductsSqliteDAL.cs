using ProductManagerApp.Data;
using ProductManagerApp.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductManagerApp.DAL
{
    internal class ProductsSqliteDAL : IProductsDAL
    {
        /// <summary>
        /// 查询商品列表
        /// </summary>
        /// <returns></returns>
        public DataTable QueryProducts()
        {
            return DatabaseHelper.Query("SELECT * FROM products");
        }

        public List<Product> GetAllProducts()
        {
            var dt = DatabaseHelper.Query("SELECT * FROM products");
            var products = new List<Product>();
            foreach (DataRow row in dt.Rows)
            {
                var product = new Product
                {
                    Id = Convert.ToInt32(row["id"]),
                    Name = row["name"].ToString(),
                    Price = Convert.ToDouble(row["price"]),
                    Stock = Convert.ToInt32(row["stock"]),
                    Description = row["description"].ToString()
                };
                products.Add(product);
            }
            return products;
        }

        /// <summary>
        /// 添加商品
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        public int AddProduct(Product product)
        {
            string sql = "INSERT INTO products (name, price, stock) VALUES (@name, @price, @stock)";
            SQLiteParameter[] parameters =
            {
                new SQLiteParameter("@name", product.Name),
                new SQLiteParameter("@price", product.Price),
                new SQLiteParameter("@stock", product.Stock),
                new SQLiteParameter("@description", product.Description)
            };
            return DatabaseHelper.Execute(sql, parameters.ToArray());
        }

        public int UpdateProduct(Product product)
        {
            string sql = "UPDATE products SET name=@name, price=@price, stock=@stock, description=@description WHERE id=@id";
            SQLiteParameter[] parameters =
            {
                new SQLiteParameter("@name", product.Name),
                new SQLiteParameter("@price", product.Price),
                new SQLiteParameter("@stock", product.Stock),
                new SQLiteParameter("@description", product.Description),
                new SQLiteParameter("@id", product.Id)
            };
            return DatabaseHelper.Execute(sql, parameters.ToArray());
        }

        public int UpdateProductPrice(int productId, double newPrice)
        {
            string sql = "UPDATE products SET price=@price WHERE id=@id";
            SQLiteParameter[] parameters =
            {
                new SQLiteParameter("@price", newPrice),
                new SQLiteParameter("@id", productId)
            };
            return DatabaseHelper.Execute(sql, parameters.ToArray());
        }

        public Product GetProductById(int productId)
        {
            throw new NotImplementedException();
        }
    }
}