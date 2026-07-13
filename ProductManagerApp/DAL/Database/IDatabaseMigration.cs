using System.Data;

namespace ProductManagerApp.DAL.Database
{
    /// <summary>
    /// 表示一个只能向前执行的数据库结构迁移步骤。
    /// </summary>
    internal interface IDatabaseMigration
    {
        int Version { get; }

        /// <returns>
        /// true 表示迁移完整完成并可推进版本；false 表示兼容措施已执行，但需下次启动重试。
        /// </returns>
        bool Apply(IDbConnection connection, IDbTransaction transaction);
    }
}
