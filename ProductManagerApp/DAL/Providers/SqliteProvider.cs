using System.Data;
using System.Data.SQLite;

namespace ProductManagerApp.DAL
{
    public class SqliteProvider : IDbProvider
    {
        private readonly string connStr;

        /// <summary>
        /// 初始化 SQLite 连接提供器
        /// </summary>
        /// <param name="connectionString">数据库连接字符串</param>
        public SqliteProvider(string connectionString)
        {
            connStr = connectionString;
        }

        /// <summary>
        /// 创建一个新的 SQLite 数据库连接
        /// 每次调用返回独立连接实例（非单例）
        /// </summary>
        /// <returns>SQLite 数据库连接对象</returns>
        public IDbConnection CreateConnection()
        {
            return new SQLiteConnection(connStr);
        }
    }
}
