using ProductManagerApp.DAL;
using ProductManagerApp.DAL.Database;
using System.Data.SQLite;

namespace ProductManagerApp.Tests.DAL;

/// <summary>
/// 为单个测试创建隔离的 SQLite 文件，并在释放时清理数据库及 sidecar 文件。
/// </summary>
internal sealed class SqliteTestDatabase : IDisposable
{
    private bool _disposed;

    public SqliteTestDatabase(bool initialize = true)
    {
        DatabasePath = Path.Combine(
            Path.GetTempPath(),
            $"ProductManagerApp-Test-{Guid.NewGuid():N}.db");

        var connectionString = new SQLiteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Version = 3,
            Pooling = false
        }.ConnectionString;

        Provider = new SqliteProvider(connectionString);
        Repository = new ProductRepository(Provider);

        if (initialize)
        {
            new SqliteDatabaseInitializer(Provider).Initialize();
        }
    }

    public string DatabasePath { get; }

    public IDbProvider Provider { get; }

    public ProductRepository Repository { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // System.Data.SQLite 可能缓存原生句柄，先清池才能在 Windows 上稳定删除文件。
        SQLiteConnection.ClearAllPools();
        DeleteIfExists(DatabasePath);
        DeleteIfExists(DatabasePath + "-journal");
        DeleteIfExists(DatabasePath + "-shm");
        DeleteIfExists(DatabasePath + "-wal");
        _disposed = true;
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
