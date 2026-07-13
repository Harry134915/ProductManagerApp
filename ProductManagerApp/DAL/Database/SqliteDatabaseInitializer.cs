using Dapper;
using System.Data;

namespace ProductManagerApp.DAL.Database
{
    /// <summary>
    /// 在单个事务中按版本执行 SQLite 迁移，并维护 PRAGMA user_version。
    /// </summary>
    public class SqliteDatabaseInitializer : IDatabaseInitializer
    {
        public const int CurrentVersion = InitialProductSchemaMigration.SchemaVersion;

        private readonly IDbProvider _dbProvider;
        private readonly IReadOnlyList<IDatabaseMigration> _migrations;

        public SqliteDatabaseInitializer(IDbProvider dbProvider)
            : this(dbProvider, new IDatabaseMigration[]
            {
                new InitialProductSchemaMigration()
            })
        {
        }

        internal SqliteDatabaseInitializer(
            IDbProvider dbProvider,
            IEnumerable<IDatabaseMigration> migrations)
        {
            _dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            ArgumentNullException.ThrowIfNull(migrations);

            _migrations = migrations.OrderBy(migration => migration.Version).ToArray();
            ValidateMigrationSequence(_migrations);
        }

        public void Initialize()
        {
            using var connection = _dbProvider.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var databaseVersion = connection.ExecuteScalar<int>(
                    "PRAGMA user_version;",
                    transaction: transaction);
                var latestSupportedVersion = _migrations[^1].Version;

                if (databaseVersion > latestSupportedVersion)
                {
                    throw new InvalidOperationException(
                        $"数据库版本 {databaseVersion} 高于当前应用支持的版本 " +
                        $"{latestSupportedVersion}，请升级应用后重试。");
                }

                foreach (var migration in _migrations.Where(
                    migration => migration.Version > databaseVersion))
                {
                    var completed = migration.Apply(connection, transaction);
                    if (!completed)
                    {
                        // 暂缓的迁移会提交已完成的兼容措施，但不能执行后续版本。
                        break;
                    }

                    // 版本号和结构变更位于同一事务，失败时不会留下半完成版本。
                    connection.Execute(
                        $"PRAGMA user_version = {migration.Version};",
                        transaction: transaction);
                }

                transaction.Commit();
            }
            catch
            {
                TryRollback(transaction);
                throw;
            }
        }

        private static void ValidateMigrationSequence(
            IReadOnlyList<IDatabaseMigration> migrations)
        {
            if (migrations.Count == 0)
            {
                throw new ArgumentException("至少需要提供一个数据库迁移。", nameof(migrations));
            }

            for (var index = 0; index < migrations.Count; index++)
            {
                var expectedVersion = index + 1;
                if (migrations[index].Version != expectedVersion)
                {
                    throw new ArgumentException(
                        $"数据库迁移版本必须从 1 开始连续递增，缺少版本 {expectedVersion}。",
                        nameof(migrations));
                }
            }
        }

        private static void TryRollback(IDbTransaction transaction)
        {
            try
            {
                transaction.Rollback();
            }
            catch
            {
                // 保留原始迁移异常，避免回滚失败覆盖真正的问题原因。
            }
        }
    }
}
