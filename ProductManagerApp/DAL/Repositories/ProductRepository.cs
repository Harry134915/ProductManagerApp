using Dapper;
using ProductManagerApp.Entity;
using ProductManagerApp.Infrastructure.Exceptions;
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

        // 查询全部商品
        public List<Product> GetAllProducts()
        {
            const string sql = "SELECT id, code, name, price, stock, description FROM products";

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

        // 查询单个商品
        public Product? GetProductById(int id)
        {
            const string sql = "SELECT id, code, name, price, stock, description FROM products WHERE id = @Id";

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

        // 新增商品
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

        // 更新商品。商品编码不可修改，和界面中编码输入框选中商品后只读的规则保持一致。
        public int UpdateProduct(Product product)
        {
            const string sql = @"
                UPDATE products
                SET name = @Name, price = @Price, stock = @Stock, description = @Description
                WHERE id = @Id
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

        // 更新商品价格
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

        // 删除商品
        public int DeleteProduct(int productId)
        {
            const string sql = "DELETE FROM products WHERE id = @Id";

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
    }
}
