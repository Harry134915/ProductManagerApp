using Dapper;
using ProductManagerApp.DAL;
using ProductManagerApp.DAL.Database;

namespace ProductManagerApp.Tests.DAL;

/// <summary>
/// 验证初始化器兼容包含重复编码的旧数据库且不会破坏已有数据。
/// </summary>
[Collection(SqliteIntegrationCollection.Name)]
public class SqliteDatabaseInitializerTests
{
    [Fact]
    public void Initialize_WithLegacyDuplicates_PreservesDataAndBlocksNewDuplicates()
    {
        using var database = new SqliteTestDatabase(initialize: false);
        var provider = database.Provider;

        CreateLegacyDatabaseWithDuplicates(provider);

        var initializer = new SqliteDatabaseInitializer(provider);
        initializer.Initialize();

        using (var connection = provider.CreateConnection())
        {
            var existingCount = connection.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM products WHERE code = 'P001'");
            Assert.Equal(2, existingCount);

            Assert.Throws<System.Data.SQLite.SQLiteException>(() => connection.Execute(@"
                    INSERT INTO products (code, name, price, stock, description)
                    VALUES ('p001', 'Duplicate', 10, 1, 'Duplicate code')
                "));

            connection.Execute(@"
                    DELETE FROM products
                    WHERE id = (SELECT MAX(id) FROM products WHERE code = 'P001')
                ");
        }

        initializer.Initialize();

        using var verificationConnection = provider.CreateConnection();
        var uniqueIndexCount = verificationConnection.ExecuteScalar<int>(@"
                SELECT COUNT(*)
                FROM sqlite_master
                WHERE type = 'index'
                  AND name = 'ux_products_code_nocase'
            ");
        Assert.Equal(1, uniqueIndexCount);
    }

    private static void CreateLegacyDatabaseWithDuplicates(IDbProvider provider)
    {
        using var connection = provider.CreateConnection();
        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                code TEXT NOT NULL,
                name TEXT NOT NULL,
                price NUMERIC NOT NULL,
                stock INTEGER NOT NULL,
                description TEXT NOT NULL DEFAULT ''
            );

            INSERT INTO products (code, name, price, stock, description)
            VALUES ('P001', 'Phone', 1999, 10, 'First');

            INSERT INTO products (code, name, price, stock, description)
            VALUES ('P001', 'Phone duplicate', 1999, 10, 'Second');
        ");
    }
}
