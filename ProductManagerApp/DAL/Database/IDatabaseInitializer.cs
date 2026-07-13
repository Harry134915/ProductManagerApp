namespace ProductManagerApp.DAL.Database
{
    /// <summary>
    /// 定义应用启动阶段的数据库结构初始化入口。
    /// </summary>
    public interface IDatabaseInitializer
    {
        void Initialize();
    }
}
