using System.Data;

namespace ProductManagerApp.DAL
{
    /// <summary>
    /// 为 Repository 和初始化器创建独立数据库连接。
    /// </summary>
    public interface IDbProvider
    {
        IDbConnection CreateConnection();
    }
}
