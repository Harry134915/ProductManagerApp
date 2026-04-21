using Microsoft.Extensions.DependencyInjection;
using ProductManagerApp.BLL.Interfaces;
using ProductManagerApp.BLL.Services;
using ProductManagerApp.BLL.Validators;
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

            //#region  第一种方式:手动创建实例并注入依赖
            //const string connStr = "Data Source=database.db;Version=3;";
            ////负责底层数据库连接、执行 SQL。
            //IDbProvider dbProvider = new SqliteProvider(connStr);

            ////仓储层依赖数据库访问层。
            //IProductRepository repo = new ProductRepository(dbProvider);

            ////业务逻辑层依赖仓储层。
            //IProductBLL bll = new ProductBLL(repo);

            //// 创建 ViewModel 并绑定
            //var viewModel = new MainWindowViewModel(bll);

            //// 创建窗口
            //var mainWindow1 = new MainWindow
            //{
            //    DataContext = viewModel
            //};
            //// 显示窗口
            //mainWindow1.Show();
            //#endregion



            #region 第二种方式:使用 Microsoft.Extensions.DependencyInjection 进行依赖注入
            var serviceCollection = new ServiceCollection();

            ConfigureServices(serviceCollection);

            var services = serviceCollection.BuildServiceProvider();

            var mainWindow2 = services.GetRequiredService<MainWindow>();
            mainWindow2.Show();
            #endregion

        }

        private void ConfigureServices(IServiceCollection services)
        {
            const string connStr = "Data Source=database.db;Version=3;";
            // 注册数据库提供程序
            services.AddSingleton<IDbProvider>(new SqliteProvider(connStr));
            // 注册仓储层
            services.AddTransient<IProductRepository, ProductRepository>();
            // 注册业务逻辑层
            services.AddTransient<IProductService, ProductService>();
            // 注册验证器
            services.AddTransient<ProductValidator>();
            // 注册 ViewModel
            services.AddTransient<MainWindowViewModel>();
            // 注册 View
            services.AddTransient<MainWindow>();
        }
    }
}

