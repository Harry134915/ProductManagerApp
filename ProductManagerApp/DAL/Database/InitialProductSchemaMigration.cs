using Dapper;
using System.Data;

namespace ProductManagerApp.DAL.Database
{
    /// <summary>
    /// 将无版本数据库升级为包含商品表和编码唯一性保护的 Version 1。
    /// </summary>
    internal sealed class InitialProductSchemaMigration : IDatabaseMigration
    {
        public const int SchemaVersion = 1;

        public int Version => SchemaVersion;

        public bool Apply(IDbConnection connection, IDbTransaction transaction)
        {
            const string createTableSql = @"
                CREATE TABLE IF NOT EXISTS products (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    code TEXT NOT NULL COLLATE NOCASE UNIQUE,
                    name TEXT NOT NULL,
                    price NUMERIC NOT NULL,
                    stock INTEGER NOT NULL,
                    description TEXT NOT NULL DEFAULT ''
                );
            ";

            const string createInsertTriggerSql = @"
                CREATE TRIGGER IF NOT EXISTS trg_products_code_unique_insert
                BEFORE INSERT ON products
                FOR EACH ROW
                WHEN EXISTS (
                    SELECT 1
                    FROM products
                    WHERE code COLLATE NOCASE = NEW.code COLLATE NOCASE
                )
                BEGIN
                    SELECT RAISE(ABORT, 'duplicate product code');
                END;
            ";

            const string createUpdateTriggerSql = @"
                CREATE TRIGGER IF NOT EXISTS trg_products_code_unique_update
                BEFORE UPDATE OF code ON products
                FOR EACH ROW
                WHEN EXISTS (
                    SELECT 1
                    FROM products
                    WHERE id <> OLD.id
                      AND code COLLATE NOCASE = NEW.code COLLATE NOCASE
                )
                BEGIN
                    SELECT RAISE(ABORT, 'duplicate product code');
                END;
            ";

            const string duplicateGroupCountSql = @"
                SELECT COUNT(*)
                FROM (
                    SELECT code
                    FROM products
                    GROUP BY code COLLATE NOCASE
                    HAVING COUNT(*) > 1
                );
            ";

            const string createUniqueIndexSql = @"
                CREATE UNIQUE INDEX IF NOT EXISTS ux_products_code_nocase
                ON products (code COLLATE NOCASE);
            ";

            connection.Execute(createTableSql, transaction: transaction);
            connection.Execute(createInsertTriggerSql, transaction: transaction);
            connection.Execute(createUpdateTriggerSql, transaction: transaction);

            var duplicateGroupCount = connection.ExecuteScalar<int>(
                duplicateGroupCountSql,
                transaction: transaction);

            // 重复数据未清理前暂缓完成迁移，但保留触发器以阻止继续产生重复项。
            if (duplicateGroupCount > 0)
            {
                return false;
            }

            connection.Execute(createUniqueIndexSql, transaction: transaction);
            return true;
        }
    }
}
