using Microsoft.Extensions.DependencyInjection;
using ProductManagerApp.BLL.Interfaces;
using ProductManagerApp.BLL.Services;
using ProductManagerApp.BLL.Validators;
using ProductManagerApp.DAL;
using ProductManagerApp.DAL.Database;
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

            services.GetRequiredService<IDatabaseInitializer>().Initialize();

            var mainWindow2 = services.GetRequiredService<MainWindow>();
            mainWindow2.Show();
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
            // 注册 ViewModel
            services.AddTransient<MainWindowViewModel>();
            // 注册 View
            services.AddTransient<MainWindow>();
        }
    }
}

