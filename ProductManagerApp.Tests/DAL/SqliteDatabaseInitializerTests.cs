using Dapper;
using ProductManagerApp.DAL;
using ProductManagerApp.DAL.Database;
using System.Data;

namespace ProductManagerApp.Tests.DAL;

/// <summary>
/// 验证数据库版本推进、事务回滚以及旧数据库兼容行为。
/// </summary>
[Collection(SqliteIntegrationCollection.Name)]
public class SqliteDatabaseInitializerTests
{
    [Fact]
    public void Initialize_NewDatabase_CreatesVersionOneSchema()
    {
        using var database = new SqliteTestDatabase();
        using var connection = database.Provider.CreateConnection();

        var version = connection.ExecuteScalar<int>("PRAGMA user_version;");
        var productTableCount = connection.ExecuteScalar<int>(@"
            SELECT COUNT(*)
            FROM sqlite_master
            WHERE type = 'table' AND name = 'products'
        ");
        var triggerCount = connection.ExecuteScalar<int>(@"
            SELECT COUNT(*)
            FROM sqlite_master
            WHERE type = 'trigger'
              AND name IN (
                  'trg_products_code_unique_insert',
                  'trg_products_code_unique_update'
              )
        ");

        Assert.Equal(SqliteDatabaseInitializer.CurrentVersion, version);
        Assert.Equal(1, productTableCount);
        Assert.Equal(2, triggerCount);
    }

    [Fact]
    public void Initialize_WhenCalledTwice_DoesNotReapplyCompletedMigration()
    {
        using var database = new SqliteTestDatabase(initialize: false);
        var applyCount = 0;
        var migration = new TestMigration(
            version: 1,
            apply: (connection, transaction) =>
            {
                applyCount++;
                connection.Execute(
                    "CREATE TABLE migration_probe (id INTEGER PRIMARY KEY);",
                    transaction: transaction);
                return true;
            });
        var initializer = new SqliteDatabaseInitializer(
            database.Provider,
            new[] { migration });

        initializer.Initialize();
        initializer.Initialize();

        Assert.Equal(1, applyCount);
        using var connection = database.Provider.CreateConnection();
        Assert.Equal(1, connection.ExecuteScalar<int>("PRAGMA user_version;"));
    }

    [Fact]
    public void Initialize_WhenMigrationFails_RollsBackSchemaAndVersion()
    {
        using var database = new SqliteTestDatabase(initialize: false);
        var migrations = new IDatabaseMigration[]
        {
            new TestMigration(
                version: 1,
                apply: (connection, transaction) =>
                {
                    connection.Execute(
                        "CREATE TABLE migration_one (id INTEGER PRIMARY KEY);",
                        transaction: transaction);
                    return true;
                }),
            new TestMigration(
                version: 2,
                apply: (connection, transaction) =>
                {
                    connection.Execute(
                        "CREATE TABLE migration_two (id INTEGER PRIMARY KEY);",
                        transaction: transaction);
                    throw new InvalidOperationException("migration failed");
                })
        };
        var initializer = new SqliteDatabaseInitializer(database.Provider, migrations);

        Assert.Throws<InvalidOperationException>(() => initializer.Initialize());

        using var connection = database.Provider.CreateConnection();
        Assert.Equal(0, connection.ExecuteScalar<int>("PRAGMA user_version;"));
        Assert.Equal(0, connection.ExecuteScalar<int>(@"
            SELECT COUNT(*)
            FROM sqlite_master
            WHERE type = 'table'
              AND name IN ('migration_one', 'migration_two')
        "));
    }

    [Fact]
    public void Initialize_WhenDatabaseVersionIsNewer_RejectsDowngrade()
    {
        using var database = new SqliteTestDatabase(initialize: false);
        using (var connection = database.Provider.CreateConnection())
        {
            connection.Execute("PRAGMA user_version = 2;");
        }

        var initializer = new SqliteDatabaseInitializer(database.Provider);

        var exception = Assert.Throws<InvalidOperationException>(
            () => initializer.Initialize());

        Assert.Contains("高于当前应用支持的版本", exception.Message);
        using var verificationConnection = database.Provider.CreateConnection();
        Assert.Equal(2, verificationConnection.ExecuteScalar<int>("PRAGMA user_version;"));
    }

    [Fact]
    public void Constructor_WithMissingMigrationVersion_RejectsSequence()
    {
        using var database = new SqliteTestDatabase(initialize: false);
        var migration = new TestMigration(version: 2, apply: (_, _) => true);

        var exception = Assert.Throws<ArgumentException>(() =>
            new SqliteDatabaseInitializer(database.Provider, new[] { migration }));

        Assert.Contains("缺少版本 1", exception.Message);
    }

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
            Assert.Equal(0, connection.ExecuteScalar<int>("PRAGMA user_version;"));

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
        Assert.Equal(
            SqliteDatabaseInitializer.CurrentVersion,
            verificationConnection.ExecuteScalar<int>("PRAGMA user_version;"));
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

    /// <summary>
    /// 允许测试精确控制迁移副作用和失败位置，不依赖生产迁移 SQL。
    /// </summary>
    private sealed class TestMigration : IDatabaseMigration
    {
        private readonly Func<IDbConnection, IDbTransaction, bool> _apply;

        public TestMigration(
            int version,
            Func<IDbConnection, IDbTransaction, bool> apply)
        {
            Version = version;
            _apply = apply;
        }

        public int Version { get; }

        public bool Apply(IDbConnection connection, IDbTransaction transaction)
        {
            return _apply(connection, transaction);
        }
    }
}
