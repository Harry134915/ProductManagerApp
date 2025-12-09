using ProductManagerApp.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace ProductManagerApp.DAL
{
    internal class ProductsSqliteDAL : IProductsDAL
    {
        // =======================================================
        // 查询全部
        // =======================================================
        public List<Product> GetAllProducts()
        {
            string sql = "SELECT id, name, price, stock, description FROM products";
            List<Product> list = new List<Product>();

            var dt = DatabaseHelper.Query(sql);

            if (dt == null) return list;

            foreach (System.Data.DataRow row in dt.Rows)
            {
                list.Add(MapToProduct(row));
            }

            return list;
        }


        // =======================================================
        // 查询单个
        // =======================================================
        public Product? GetProductById(int id)
        {
            string sql = "SELECT id, name, price, stock, description FROM products WHERE id=@id";

            var dt = DatabaseHelper.Query(sql, new SQLiteParameter("@id", id));

            if (dt == null || dt.Rows.Count == 0)
                return null;

            return MapToProduct(dt.Rows[0]);
        }


        // =======================================================
        // 新增
        // =======================================================
        public int AddProduct(Product product)
        {
            string sql = @"
                INSERT INTO products (name, price, stock, description)
                VALUES (@name, @price, @stock, @description)
            ";

            SQLiteParameter[] p =
            {
                new SQLiteParameter("@name", product.Name),
                new SQLiteParameter("@price", product.Price),
                new SQLiteParameter("@stock", product.Stock),
                new SQLiteParameter("@description", product.Description ?? string.Empty),
            };

            return DatabaseHelper.Execute(sql, p);
        }


        // =======================================================
        // 更新
        // =======================================================
        public int UpdateProduct(Product product)
        {
            string sql = @"
                UPDATE products
                SET name=@name, price=@price, stock=@stock, description=@description
                WHERE id=@id
            ";

            SQLiteParameter[] p =
            {
                new SQLiteParameter("@name", product.Name),
                new SQLiteParameter("@price", product.Price),
                new SQLiteParameter("@stock", product.Stock),
                new SQLiteParameter("@description", product.Description ?? string.Empty),
                new SQLiteParameter("@id", product.Id)
            };

            return DatabaseHelper.Execute(sql, p);
        }


        // =======================================================
        // 更新价格
        // =======================================================
        public int UpdateProductPrice(int productId, double newPrice)
        {
            string sql = "UPDATE products SET price=@price WHERE id=@id";

            SQLiteParameter[] p =
            {
                new SQLiteParameter("@price", newPrice),
                new SQLiteParameter("@id", productId)
            };

            return DatabaseHelper.Execute(sql, p);
        }


        // =======================================================
        // 删除
        // =======================================================
        public int DeleteProduct(int productId)
        {
            string sql = "DELETE FROM products WHERE id=@id";

            return DatabaseHelper.Execute(sql, new SQLiteParameter("@id", productId));
        }


        // =======================================================
        // 映射辅助方法
        // =======================================================
        private Product MapToProduct(System.Data.DataRow row)
        {
            return new Product
            {
                Id = row["id"] != DBNull.Value ? Convert.ToInt32(row["id"]) : 0,
                Name = row["name"]?.ToString() ?? "",
                Price = row["price"] != DBNull.Value ? Convert.ToDouble(row["price"]) : 0.0,
                Stock = row["stock"] != DBNull.Value ? Convert.ToInt32(row["stock"]) : 0,
                Description = row["description"]?.ToString() ?? ""
            };
        }
    }
}
