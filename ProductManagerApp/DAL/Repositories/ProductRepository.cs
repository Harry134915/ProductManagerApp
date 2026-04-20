using Dapper;
using ProductManagerApp.Entity;

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

            using var conn = _dbProvider.CreateConnection();
            return conn.Query<Product>(sql).ToList();
        }

        // =======================================================
        // 查询单个
        // =======================================================
        public Product? GetProductById(int id)
        {
            const string sql = "SELECT id,code, name, price, stock, description FROM products WHERE id=@Id";

            using var conn = _dbProvider.CreateConnection();
            return conn.QueryFirstOrDefault<Product>(sql, new { Id = id });
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

            using var conn = _dbProvider.CreateConnection();
            return conn.Execute(sql, product);
        }


        // =======================================================
        // 更新
        // =======================================================
        public int UpdateProduct(Product product)
        {
            string sql = @"
                UPDATE products
                SET name=@Name, price=@Price, stock=@Stock, description=@Description
                WHERE id=@Id
            ";

            using var conn = _dbProvider.CreateConnection();
            return conn.Execute(sql, product);
        }


        // =======================================================
        // 更新价格
        // =======================================================
        public int UpdateProductPrice(int productId, decimal newPrice)
        {
            const string sql = "UPDATE products SET price = @Price WHERE id = @Id";
            using var conn = _dbProvider.CreateConnection();
            return conn.Execute(sql, new { Price = newPrice, Id = productId });
        }

        // =======================================================
        // 删除
        // =======================================================
        public int DeleteProduct(int productId)
        {
            string sql = "DELETE FROM products WHERE id=@id";

            using var conn = _dbProvider.CreateConnection();
            return conn.Execute(sql, new { Id = productId });
        }


        // =======================================================
        // 手写 映射辅助方法
        // =======================================================
        [Obsolete]
        private Product MapToProduct(System.Data.DataRow row)
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
