using Microsoft.Extensions.DependencyInjection;
using ProductManagerApp.BLL.Interfaces;
using ProductManagerApp.BLL.Services;
using ProductManagerApp.BLL.Validators;
using ProductManagerApp.DAL;
using ProductManagerApp.DAL.Database;
using ProductManagerApp.Infrastructure.Logging;
using ProductManagerApp.Infrastructure.FileExchange;
using ProductManagerApp.ViewModels;
using ProductManagerApp.Views;
using System.Data.SQLite;
using System.IO;
using System.Windows;

namespace ProductManagerApp
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var serviceCollection = new ServiceCollection();

            ConfigureServices(serviceCollection);

            var services = serviceCollection.BuildServiceProvider();
            Services = services;

            var logger = services.GetRequiredService<IAppLogger>();
            logger.LogInformation($"应用启动，日志目录：{GetLogDirectory()}。");

            try
            {
                services.GetRequiredService<IDatabaseInitializer>().Initialize();

                var mainWindow = services.GetRequiredService<MainWindow>();
                mainWindow.Show();
            }
            catch (Exception exception)
            {
                logger.LogError("应用启动失败。", exception);
                throw;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (Services != null)
            {
                Services.GetService<IAppLogger>()?.LogInformation("应用退出。");
            }

            base.OnExit(e);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            var databasePath = Path.Combine(AppContext.BaseDirectory, "database.db");
            var connStr = new SQLiteConnectionStringBuilder
            {
                DataSource = databasePath,
                Version = 3
            }.ConnectionString;

            // 注册数据库提供程序
            services.AddSingleton<IDbProvider>(new SqliteProvider(connStr));
            // 注册数据库初始化器
            services.AddSingleton<IDatabaseInitializer, SqliteDatabaseInitializer>();
            // 注册仓储层
            services.AddTransient<IProductRepository, ProductRepository>();
            // 注册业务逻辑层
            services.AddTransient<IProductService, ProductService>();
            // 注册验证器
            services.AddTransient<ProductValidator>();
            // 商品文件服务和系统对话框保持接口隔离，便于无 UI 单元测试。
            services.AddSingleton<IProductFileService, ProductFileService>();
            services.AddSingleton<IProductFileDialogService, ProductFileDialogService>();
            services.AddTransient<IProductImportResultPresenter, ProductImportResultPresenter>();
            // 同时保留调试输出和按日文件日志。
            services.AddSingleton<IAppLogger>(_ => new CompositeAppLogger(
                new DebugAppLogger(),
                new FileAppLogger(GetLogDirectory())));
            // 注册 ViewModel
            services.AddTransient<MainWindowViewModel>();
            // 注册 View
            services.AddTransient<MainWindow>();
        }

        private static string GetLogDirectory()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ProductManagerApp",
                "Logs");
        }
    }
}

