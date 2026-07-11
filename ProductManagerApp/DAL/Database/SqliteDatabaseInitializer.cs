using Dapper;

namespace ProductManagerApp.DAL.Database
{
    public class SqliteDatabaseInitializer : IDatabaseInitializer
    {
        private readonly IDbProvider _dbProvider;

        public SqliteDatabaseInitializer(IDbProvider dbProvider)
        {
            _dbProvider = dbProvider;
        }

        public void Initialize()
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

            using var conn = _dbProvider.CreateConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();

            conn.Execute(createTableSql, transaction: transaction);
            conn.Execute(createInsertTriggerSql, transaction: transaction);
            conn.Execute(createUpdateTriggerSql, transaction: transaction);

            var duplicateGroupCount = conn.ExecuteScalar<int>(
                duplicateGroupCountSql,
                transaction: transaction);

            // 旧数据库可能已经存在重复编码。先阻止新增重复数据，待用户清理后再补唯一索引。
            if (duplicateGroupCount == 0)
            {
                conn.Execute(createUniqueIndexSql, transaction: transaction);
            }

            transaction.Commit();
        }
    }
}
