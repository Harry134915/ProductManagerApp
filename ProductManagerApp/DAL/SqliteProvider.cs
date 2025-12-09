using System.Data;
using System.Data.SQLite;

namespace ProductManagerApp.DAL
{
    public class SqliteProvider : IDbProvider
    {
        private readonly string connStr;

        public SqliteProvider(string connectionString)
        {
            connStr = connectionString;
        }

        public IDbConnection CreateConnection()
        {
            return new SQLiteConnection(connStr);
        }
    }
}
