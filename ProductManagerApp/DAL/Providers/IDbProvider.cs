using System.Data;

namespace ProductManagerApp.DAL
{
    public interface IDbProvider
    {
        IDbConnection CreateConnection();
    }
}
