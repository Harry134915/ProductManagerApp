namespace ProductManagerApp.Tests.DAL;

/// <summary>
/// 将 SQLite 集成测试串行化，避免全局连接池清理影响其他数据库用例。
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SqliteIntegrationCollection
{
    public const string Name = "SQLite integration";
}
