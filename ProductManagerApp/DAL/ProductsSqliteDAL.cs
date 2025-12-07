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

        /// <summary>
        /// 根据 Id 查询单个商品
        /// </summary>
        public DataTable QueryProductById(int productId)
        {
            string sql = "SELECT * FROM products WHERE id = @id";
            SQLiteParameter[] parameters =
            {
                new SQLiteParameter("@id",productId)
            };
            return DatabaseHelper.Query(sql, parameters);
        }

        /// <summary>
        /// 添加商品
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        public int AddProduct(DataRow row)
        {
            string sql = "INSERT INTO products (name, price, stock,description) VALUES (@name, @price, @stock,@description)";
            SQLiteParameter[] parameters =
            {
                new SQLiteParameter("@name", row["Name"]),
                new SQLiteParameter("@price", row["Price"]),
                new SQLiteParameter("@stock", row["stock"]),
                new SQLiteParameter("@description",row["description"] )
            };
            return DatabaseHelper.Execute(sql, parameters);
        }

        public int UpdateProduct(DataRow row)
        {
            string sql = "UPDATE products SET name=@name, price=@price, stock=@stock, description=@description WHERE id=@id";
            SQLiteParameter[] parameters =
            {
                new SQLiteParameter("@name", row["Name"]),
                new SQLiteParameter("@price", row["Price"]),
                new SQLiteParameter("@stock", row["stock"]),
                new SQLiteParameter("@description", row["description"]),
                new SQLiteParameter("@id", row["id"])
            };
            return DatabaseHelper.Execute(sql, parameters);
        }

        public int UpdateProductPrice(int productId, double newPrice)
        {
            string sql = "UPDATE products SET price=@price WHERE id=@id";
            SQLiteParameter[] parameters =
            {
                new SQLiteParameter("@price", newPrice),
                new SQLiteParameter("@id", productId)
            };
            return DatabaseHelper.Execute(sql, parameters);
        }

        /// <summary>
        /// 删除商品
        /// </summary>
        public int DeleteProduct(int productId)
        {
            string sql = "DELETE FROM products WHERE id=@id";
            SQLiteParameter[] parameters =
            {
                new SQLiteParameter("@id", productId)
            };
            return DatabaseHelper.Execute(sql, parameters);
        }


    }
}