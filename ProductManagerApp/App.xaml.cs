using ProductManagerApp.BLL;
using ProductManagerApp.DAL;
using ProductManagerApp.ViewModels;
using ProductManagerApp.Views;
using System.Windows;

//Startup、Exit、异常处理等逻辑写在这里
//用 App.xaml.cs 代码控制启动 View + ViewModel
//App.xaml.cs → 手动创建 View → 手动绑定 ViewModel → 启动


namespace ProductManagerApp
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            const string connStr = "Data Source=database.db;Version=3;";
            IDbProvider dbProvider = new SqliteProvider(connStr);
            IProductRepository repo = new ProductRepository(dbProvider);
            IProductBLL bll = new ProductBLL(repo);

            // 创建 ViewModel 并绑定
            var viewModel = new MainWindowViewModel(bll);

            // 创建窗口
            var mainWindow = new MainWindow
            {
                DataContext = viewModel
            };

            // 显示窗口
            mainWindow.Show();
        }
    }
}

