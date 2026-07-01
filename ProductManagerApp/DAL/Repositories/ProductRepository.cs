using Dapper;
using ProductManagerApp.BLL.Exceptions;
using ProductManagerApp.Entity;
using System.Data;
using System.Data.SQLite;

namespace ProductManagerApp.DAL
{
    internal class ProductRepository : IProductRepository
    {
        private readonly IDbProvider _dbProvider;

        public ProductRepository(IDbProvider dbProvider)
        {
            _dbProvider = dbProvider;
        }

        // =======================================================
        // 查询全部
        // =======================================================
        public List<Product> GetAllProducts()
        {
            const string sql = "SELECT id,code,name, price, stock, description FROM products";

            try
            {
                using var conn = _dbProvider.CreateConnection();
                return conn.Query<Product>(sql).ToList();
            }
            catch (Exception ex) when (IsDataAccessException(ex))
            {
                throw CreateDataAccessException("查询商品列表", ex);
            }
        }

        // =======================================================
        // 查询单个
        // =======================================================
        public Product? GetProductById(int id)
        {
            const string sql = "SELECT id,code, name, price, stock, description FROM products WHERE id=@Id";

            try
            {
                using var conn = _dbProvider.CreateConnection();
                return conn.QueryFirstOrDefault<Product>(sql, new { Id = id });
            }
            catch (Exception ex) when (IsDataAccessException(ex))
            {
                throw CreateDataAccessException("查询商品", ex);
            }
        }

        // =======================================================
        // 新增
        // =======================================================
        public int AddProduct(Product product)
        {
            const string sql = @"
                INSERT INTO products (code, name, price, stock, description)
                VALUES (@Code, @Name, @Price, @Stock, @Description)
            ";

            try
            {
                using var conn = _dbProvider.CreateConnection();
                return conn.Execute(sql, product);
            }
            catch (Exception ex) when (IsDataAccessException(ex))
            {
                throw CreateDataAccessException("新增商品", ex);
            }
        }

        // =======================================================
        // 更新
        // 商品编码不可修改，和界面中编码输入框选中商品后只读的规则保持一致。
        // =======================================================
        public int UpdateProduct(Product product)
        {
            const string sql = @"
                UPDATE products
                SET name=@Name, price=@Price, stock=@Stock, description=@Description
                WHERE id=@Id
            ";

            try
            {
                using var conn = _dbProvider.CreateConnection();
                return conn.Execute(sql, product);
            }
            catch (Exception ex) when (IsDataAccessException(ex))
            {
                throw CreateDataAccessException("更新商品", ex);
            }
        }

        // =======================================================
        // 更新价格
        // =======================================================
        public int UpdateProductPrice(int productId, decimal newPrice)
        {
            const string sql = "UPDATE products SET price = @Price WHERE id = @Id";

            try
            {
                using var conn = _dbProvider.CreateConnection();
                return conn.Execute(sql, new { Price = newPrice, Id = productId });
            }
            catch (Exception ex) when (IsDataAccessException(ex))
            {
                throw CreateDataAccessException("更新商品价格", ex);
            }
        }

        // =======================================================
        // 删除
        // =======================================================
        public int DeleteProduct(int productId)
        {
            const string sql = "DELETE FROM products WHERE id=@Id";

            try
            {
                using var conn = _dbProvider.CreateConnection();
                return conn.Execute(sql, new { Id = productId });
            }
            catch (Exception ex) when (IsDataAccessException(ex))
            {
                throw CreateDataAccessException("删除商品", ex);
            }
        }

        private static bool IsDataAccessException(Exception ex)
        {
            return ex is SQLiteException or DataException or InvalidOperationException;
        }

        private static DataAccessException CreateDataAccessException(string operation, Exception ex)
        {
            return new DataAccessException($"{operation}失败，数据库访问异常。", ex);
        }

        // =======================================================
        // 手写 映射辅助方法
        // =======================================================
        [Obsolete]
        private Product MapToProduct(DataRow row)
        {
            return new Product
            {
                Id = row["id"] != DBNull.Value ? Convert.ToInt32(row["id"]) : 0,
                Code = row["code"]?.ToString() ?? "",
                Name = row["name"]?.ToString() ?? "",
                Price = row["price"] != DBNull.Value ? Convert.ToDecimal(row["price"]) : 0m,
                Stock = row["stock"] != DBNull.Value ? Convert.ToInt32(row["stock"]) : 0,
                Description = row["description"]?.ToString() ?? ""
            };
        }
    }
}
