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
            const string sql = @"
                CREATE TABLE IF NOT EXISTS products (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    code TEXT NOT NULL UNIQUE,
                    name TEXT NOT NULL,
                    price NUMERIC NOT NULL,
                    stock INTEGER NOT NULL,
                    description TEXT NOT NULL DEFAULT ''
                );
            ";

            using var conn = _dbProvider.CreateConnection();
            conn.Execute(sql);
        }
    }
}
