using ProductManagerApp.BLL.Services;
using ProductManagerApp.BLL.Validators;
using ProductManagerApp.DTO;
using ProductManagerApp.Entity;
using ProductManagerApp.Infrastructure.Exceptions;
using ProductManagerApp.Tests.Fakes;
using ProductManagerApp.ViewModels;

namespace ProductManagerApp.Tests.ViewModels;

/// <summary>
/// 验证主窗口对新增/编辑模式、命令协作、删除确认、取消和日志的编排。
/// </summary>
public class MainWindowViewModelTests
{
    [Fact]
    public void InitialState_UsesCreateMode()
    {
        var viewModel = CreateViewModel();

        try
        {
            Assert.False(viewModel.IsEditMode);
            Assert.Equal("新增商品", viewModel.FormTitle);
            Assert.Equal("填写完整商品信息后即可添加", viewModel.FormModeHint);
            Assert.Equal("清空表单", viewModel.ClearFormButtonText);
            Assert.False(viewModel.EscapeCommand.CanExecute(null));
        }
        finally
        {
            viewModel.CancelOperations();
        }
    }

    [Fact]
    public async Task SelectingProduct_UsesEditModeAndDisablesAdd()
    {
        var viewModel = CreateViewModel();

        try
        {
            await WaitUntilAsync(() => viewModel.List.HasLoaded && !viewModel.List.IsRefreshing);
            viewModel.List.SelectedProduct = CreateProduct();

            Assert.True(viewModel.IsEditMode);
            Assert.Equal("编辑商品：Phone", viewModel.FormTitle);
            Assert.Equal("正在编辑已选商品，商品编码不可修改", viewModel.FormModeHint);
            Assert.Equal("退出编辑", viewModel.ClearFormButtonText);
            Assert.Equal("P001", viewModel.Form.Code);
            Assert.False(viewModel.AddCommand.CanExecute(null));
            Assert.True(viewModel.UpdateCommand.CanExecute(null));
            Assert.True(viewModel.DeleteCommand.CanExecute(null));
        }
        finally
        {
            viewModel.CancelOperations();
        }
    }

    [Fact]
    public void ClearFormCommand_FromEditMode_ReturnsToCreateMode()
    {
        var viewModel = CreateViewModel();

        try
        {
            viewModel.List.SelectedProduct = CreateProduct();
            string? focusedProperty = null;
            viewModel.Form.FocusRequested += propertyName => focusedProperty = propertyName;

            viewModel.ClearFormCommand.Execute(null);

            Assert.False(viewModel.IsEditMode);
            Assert.Null(viewModel.List.SelectedProduct);
            Assert.Equal("新增商品", viewModel.FormTitle);
            Assert.Equal("清空表单", viewModel.ClearFormButtonText);
            Assert.Equal(string.Empty, viewModel.Form.Code);
            Assert.Equal(string.Empty, viewModel.Form.Name);
            Assert.Equal(string.Empty, viewModel.Form.Price);
            Assert.Equal(string.Empty, viewModel.Form.Stock);
            Assert.Equal(string.Empty, viewModel.Form.Description);
            Assert.Equal(nameof(ProductFormViewModel.Code), focusedProperty);
        }
        finally
        {
            viewModel.CancelOperations();
        }
    }

    [Fact]
    public async Task RefreshCommand_WithSelectedProduct_PreservesEditModeAndDisablesAdd()
    {
        var repository = new FakeProductRepository();
        var logger = new FakeAppLogger();
        repository.Products.Add(new ProductManagerApp.Entity.Product
        {
            Id = 1,
            Code = "P001",
            Name = "Phone",
            Price = 1999m,
            Stock = 10,
            Description = "Flagship phone"
        });
        var viewModel = CreateViewModel(repository, logger);

        try
        {
            await WaitUntilAsync(() => viewModel.List.HasLoaded && !viewModel.List.IsRefreshing);
            viewModel.List.SelectedProduct = Assert.Single(viewModel.List.Products);

            viewModel.RefreshCommand.Execute(null);
            await WaitUntilAsync(() => !viewModel.RefreshCommand.IsExecuting);

            Assert.True(viewModel.IsEditMode);
            Assert.NotNull(viewModel.List.SelectedProduct);
            Assert.Equal(1, viewModel.List.SelectedProduct.Id);
            Assert.Equal("P001", viewModel.Form.Code);
            Assert.False(viewModel.AddCommand.CanExecute(null));
            Assert.Contains(
                logger.Entries,
                entry => entry.Level == "Information"
                    && entry.Message == "刷新商品列表完成，共 1 条商品。");
        }
        finally
        {
            viewModel.CancelOperations();
        }
    }

    [Fact]
    public async Task AddCommand_WithInvalidForm_UsesInlineErrorsWithoutCallingRepository()
    {
        var repository = new FakeProductRepository();
        var logger = new FakeAppLogger();
        var viewModel = CreateViewModel(repository, logger);

        try
        {
            await WaitUntilAsync(() => viewModel.List.HasLoaded && !viewModel.List.IsRefreshing);
            viewModel.AddCommand.Execute(null);

            Assert.True(viewModel.Form.HasErrors);
            Assert.Equal(0, repository.AddProductCallCount);
            Assert.True(string.IsNullOrEmpty(viewModel.ErrorMessage));
            Assert.False(viewModel.AddCommand.IsExecuting);
            Assert.Contains(
                logger.Entries,
                entry => entry.Level == "Warning"
                    && entry.Message == "新增商品提交未通过表单校验。");
        }
        finally
        {
            viewModel.CancelOperations();
        }
    }

    [Fact]
    public async Task AddCommand_WithValidForm_ClearsFormAndFocusesCode()
    {
        var repository = new FakeProductRepository();
        var logger = new FakeAppLogger();
        var viewModel = CreateViewModel(repository, logger);

        try
        {
            await WaitUntilAsync(() => viewModel.List.HasLoaded && !viewModel.List.IsRefreshing);
            viewModel.Form.Code = "P002";
            viewModel.Form.Name = "Tablet";
            viewModel.Form.Price = "2999.00";
            viewModel.Form.Stock = "8";
            viewModel.Form.Description = "Portable tablet";
            string? focusedProperty = null;
            viewModel.Form.FocusRequested += propertyName => focusedProperty = propertyName;

            viewModel.AddCommand.Execute(null);
            await WaitUntilAsync(() => !viewModel.AddCommand.IsExecuting);

            Assert.Equal(1, repository.AddProductCallCount);
            Assert.Equal(nameof(ProductFormViewModel.Code), focusedProperty);
            Assert.Equal(string.Empty, viewModel.Form.Code);
            Assert.False(viewModel.IsEditMode);
            Assert.Contains(
                logger.Entries,
                entry => entry.Level == "Information"
                    && entry.Message == "新增商品成功，商品编码：P002。");
        }
        finally
        {
            viewModel.CancelOperations();
        }
    }

    [Fact]
    public async Task EscapeCommand_CancelsDeleteBeforeExitingEditMode()
    {
        var repository = new FakeProductRepository();
        var viewModel = CreateViewModel(repository);

        try
        {
            await WaitUntilAsync(() => viewModel.List.HasLoaded && !viewModel.List.IsRefreshing);
            viewModel.List.SelectedProduct = CreateProduct();
            viewModel.DeleteCommand.Execute(null);

            Assert.True(viewModel.DeleteConfirm.IsVisible);
            Assert.Equal(0, repository.DeleteProductCallCount);
            Assert.True(viewModel.EscapeCommand.CanExecute(null));

            viewModel.EscapeCommand.Execute(null);

            Assert.False(viewModel.DeleteConfirm.IsVisible);
            Assert.True(viewModel.IsEditMode);

            viewModel.EscapeCommand.Execute(null);

            Assert.False(viewModel.IsEditMode);
            Assert.Null(viewModel.List.SelectedProduct);
            Assert.False(viewModel.EscapeCommand.CanExecute(null));
            Assert.Equal(0, repository.DeleteProductCallCount);
        }
        finally
        {
            viewModel.CancelOperations();
        }
    }

    [Fact]
    public async Task UpdateCommand_WithInvalidForm_UsesInlineErrorsWithoutCallingRepository()
    {
        var repository = new FakeProductRepository();
        var logger = new FakeAppLogger();
        var viewModel = CreateViewModel(repository, logger);

        try
        {
            await WaitUntilAsync(() => viewModel.List.HasLoaded && !viewModel.List.IsRefreshing);
            viewModel.List.SelectedProduct = CreateProduct();
            viewModel.Form.Price = "0";

            viewModel.UpdateCommand.Execute(null);

            Assert.True(viewModel.Form.HasErrors);
            Assert.Equal(0, repository.UpdateProductCallCount);
            Assert.True(string.IsNullOrEmpty(viewModel.ErrorMessage));
            Assert.False(viewModel.UpdateCommand.IsExecuting);
            Assert.Contains(
                logger.Entries,
                entry => entry.Level == "Warning"
                    && entry.Message == "更新商品提交未通过表单校验。");
        }
        finally
        {
            viewModel.CancelOperations();
        }
    }

    [Fact]
    public async Task UpdateCommand_WithValidForm_LogsProductIdentifier()
    {
        var repository = new FakeProductRepository();
        var logger = new FakeAppLogger();
        repository.Products.Add(new ProductManagerApp.Entity.Product
        {
            Id = 1,
            Code = "P001",
            Name = "Phone",
            Price = 1999m,
            Stock = 10,
            Description = "Flagship phone"
        });
        var viewModel = CreateViewModel(repository, logger);

        try
        {
            await WaitUntilAsync(() => viewModel.List.HasLoaded && !viewModel.List.IsRefreshing);
            viewModel.List.SelectedProduct = Assert.Single(viewModel.List.Products);
            viewModel.Form.Name = "Updated phone";

            viewModel.UpdateCommand.Execute(null);
            await WaitUntilAsync(() => !viewModel.UpdateCommand.IsExecuting);

            Assert.Equal(1, repository.UpdateProductCallCount);
            Assert.Contains(
                logger.Entries,
                entry => entry.Level == "Information"
                    && entry.Message == "更新商品成功，商品 ID：1，商品编码：P001。");
        }
        finally
        {
            viewModel.CancelOperations();
        }
    }

    [Fact]
    public async Task ConfirmDeleteCommand_WhenSuccessful_LogsProductIdentifier()
    {
        var repository = new FakeProductRepository();
        var logger = new FakeAppLogger();
        repository.Products.Add(new ProductManagerApp.Entity.Product
        {
            Id = 1,
            Code = "P001",
            Name = "Phone",
            Price = 1999m,
            Stock = 10,
            Description = "Flagship phone"
        });
        var viewModel = CreateViewModel(repository, logger);

        try
        {
            await WaitUntilAsync(() => viewModel.List.HasLoaded && !viewModel.List.IsRefreshing);
            viewModel.List.SelectedProduct = Assert.Single(viewModel.List.Products);
            viewModel.DeleteCommand.Execute(null);

            viewModel.ConfirmDeleteCommand.Execute(null);
            await WaitUntilAsync(() => !viewModel.ConfirmDeleteCommand.IsExecuting);

            Assert.Equal(1, repository.DeleteProductCallCount);
            Assert.Contains(
                logger.Entries,
                entry => entry.Level == "Information"
                    && entry.Message == "删除商品成功，商品 ID：1，商品编码：P001。");
        }
        finally
        {
            viewModel.CancelOperations();
        }
    }

    [Fact]
    public async Task InitialLoad_WhenSuccessful_LogsProductCount()
    {
        var repository = new FakeProductRepository();
        var logger = new FakeAppLogger();
        repository.Products.Add(new ProductManagerApp.Entity.Product
        {
            Id = 1,
            Code = "P001",
            Name = "Phone",
            Price = 1999m,
            Stock = 10,
            Description = "Flagship phone"
        });
        var viewModel = CreateViewModel(repository, logger);

        try
        {
            await WaitUntilAsync(() => logger.Entries.Any(
                entry => entry.Message.StartsWith("商品列表初始加载完成", StringComparison.Ordinal)));

            Assert.Contains(
                logger.Entries,
                entry => entry.Level == "Information"
                    && entry.Message == "商品列表初始加载完成，共 1 条商品。");
        }
        finally
        {
            viewModel.CancelOperations();
        }
    }

    [Fact]
    public async Task InitialLoad_WhenDatabaseFails_LogsExceptionAndShowsFriendlyMessage()
    {
        var dataAccessException = new DataAccessException(
            "查询商品列表失败，数据库访问异常。",
            new InvalidOperationException("database details"));
        var repository = new FakeProductRepository
        {
            GetAllProductsException = dataAccessException
        };
        var logger = new FakeAppLogger();
        var viewModel = CreateViewModel(repository, logger);

        try
        {
            await WaitUntilAsync(() => logger.Entries.Any(entry => entry.Level == "Error"));

            var entry = Assert.Single(logger.Entries, item => item.Level == "Error");
            Assert.Equal("初始加载商品列表时数据库访问失败。", entry.Message);
            Assert.Same(dataAccessException, entry.Exception);
            Assert.Equal(
                "数据库访问失败，请检查数据库文件或稍后重试。",
                viewModel.ErrorMessage);
            Assert.DoesNotContain("database details", viewModel.ErrorMessage);
        }
        finally
        {
            viewModel.CancelOperations();
        }
    }

    [Fact]
    public async Task CancelOperations_DuringInitialLoad_LogsInformationWithoutUserError()
    {
        using var started = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var service = new StubProductService
        {
            GetAllProductsHandler = () =>
            {
                started.Set();
                release.Wait();
                return new List<ProductQueryDto>();
            }
        };
        var logger = new FakeAppLogger();
        var viewModel = CreateViewModel(service, logger);

        try
        {
            Assert.True(started.Wait(TimeSpan.FromSeconds(1)));
            viewModel.CancelOperations();
            release.Set();

            await WaitUntilAsync(() => logger.Entries.Any(
                entry => entry.Level == "Information"));

            var entry = Assert.Single(
                logger.Entries,
                item => item.Level == "Information");
            Assert.Equal("初始加载商品列表操作已取消。", entry.Message);
            Assert.True(string.IsNullOrEmpty(viewModel.ErrorMessage));
        }
        finally
        {
            release.Set();
            viewModel.CancelOperations();
        }
    }

    [Fact]
    public async Task ImportCommand_WithPreflightErrors_ShowsResultAndDoesNotWriteRepository()
    {
        var repository = new FakeProductRepository();
        var fileService = new FakeProductFileService();
        fileService.ImportResult.TotalRows = 1;
        fileService.ImportResult.Errors.Add(
            new ProductManagerApp.Infrastructure.FileExchange.ProductImportError(
                2, "价格", "价格必须是有效数字。"));
        var dialogs = new FakeProductFileDialogService
        {
            ImportSelection = new ProductManagerApp.Infrastructure.FileExchange.FileDialogSelection(
                "products.csv",
                ProductManagerApp.Infrastructure.FileExchange.ProductFileFormat.Csv)
        };
        var presenter = new FakeProductImportResultPresenter();
        var viewModel = CreateViewModel(repository, new FakeAppLogger(), fileService, dialogs, presenter);
        await WaitUntilAsync(() => viewModel.List.HasLoaded);

        viewModel.ImportCommand.Execute(null);
        await WaitUntilAsync(() => !viewModel.ImportCommand.IsExecuting);

        Assert.NotNull(presenter.LastErrors);
        Assert.Equal(0, repository.AddProductsCallCount);
    }

    [Fact]
    public async Task ImportCommand_WithValidFile_ImportsBatchAndRefreshesList()
    {
        var repository = new FakeProductRepository();
        var fileService = new FakeProductFileService();
        fileService.ImportResult.TotalRows = 1;
        fileService.ImportResult.Records.Add(
            new ProductManagerApp.Infrastructure.FileExchange.ProductImportRecord(
                2,
                new ProductCreateDto
                {
                    Code = "P002",
                    Name = "Tablet",
                    Price = 999m,
                    Stock = 5,
                    Description = "Tablet product"
                }));
        var dialogs = new FakeProductFileDialogService
        {
            ImportSelection = new ProductManagerApp.Infrastructure.FileExchange.FileDialogSelection(
                "products.xlsx",
                ProductManagerApp.Infrastructure.FileExchange.ProductFileFormat.Xlsx)
        };
        var viewModel = CreateViewModel(
            repository,
            new FakeAppLogger(),
            fileService,
            dialogs,
            new FakeProductImportResultPresenter());
        await WaitUntilAsync(() => viewModel.List.HasLoaded);

        viewModel.ImportCommand.Execute(null);
        await WaitUntilAsync(() => !viewModel.ImportCommand.IsExecuting);

        Assert.Equal(1, repository.AddProductsCallCount);
        Assert.Single(viewModel.List.Products);
        Assert.Equal("成功导入 1 条商品", viewModel.StatusMessage);
    }

    [Fact]
    public async Task ExportCommand_ExportsCurrentFilteredProductsOnly()
    {
        var repository = new FakeProductRepository();
        repository.Products.AddRange(new[]
        {
            new Product { Id = 1, Code = "P001", Name = "Phone", Price = 1m, Stock = 1, Description = "Phone" },
            new Product { Id = 2, Code = "P002", Name = "Tablet", Price = 2m, Stock = 2, Description = "Tablet" }
        });
        var fileService = new FakeProductFileService();
        var dialogs = new FakeProductFileDialogService
        {
            SaveSelection = new ProductManagerApp.Infrastructure.FileExchange.FileDialogSelection(
                "products.csv",
                ProductManagerApp.Infrastructure.FileExchange.ProductFileFormat.Csv)
        };
        var viewModel = CreateViewModel(
            repository,
            new FakeAppLogger(),
            fileService,
            dialogs,
            new FakeProductImportResultPresenter());
        await WaitUntilAsync(() => viewModel.List.HasLoaded);
        viewModel.List.SearchText = "Tablet";

        viewModel.ExportCommand.Execute(null);
        await WaitUntilAsync(() => !viewModel.ExportCommand.IsExecuting);

        var exported = Assert.Single(fileService.LastExportedProducts!);
        Assert.Equal("P002", exported.Code);
    }

    private static MainWindowViewModel CreateViewModel(
        FakeProductRepository? repository = null,
        FakeAppLogger? logger = null,
        FakeProductFileService? fileService = null,
        FakeProductFileDialogService? dialogs = null,
        FakeProductImportResultPresenter? presenter = null)
    {
        repository ??= new FakeProductRepository();
        logger ??= new FakeAppLogger();
        var service = new ProductService(repository, new ProductValidator());
        return new MainWindowViewModel(
            service,
            logger,
            fileService ?? new FakeProductFileService(),
            dialogs ?? new FakeProductFileDialogService(),
            presenter ?? new FakeProductImportResultPresenter());
    }

    private static MainWindowViewModel CreateViewModel(
        ProductManagerApp.BLL.Interfaces.IProductService service,
        FakeAppLogger logger)
    {
        return new MainWindowViewModel(
            service,
            logger,
            new FakeProductFileService(),
            new FakeProductFileDialogService(),
            new FakeProductImportResultPresenter());
    }

    private static ProductQueryDto CreateProduct()
    {
        return new ProductQueryDto
        {
            Id = 1,
            Code = "P001",
            Name = "Phone",
            Price = 1999m,
            Stock = 10,
            Description = "Flagship phone"
        };
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        while (!condition())
        {
            await Task.Delay(10, cancellationTokenSource.Token);
        }
    }

    private sealed class StubProductService : ProductManagerApp.BLL.Interfaces.IProductService
    {
        public required Func<List<ProductQueryDto>> GetAllProductsHandler { get; init; }

        public List<ProductQueryDto> GetAllProducts() => GetAllProductsHandler();

        public void AddProduct(ProductCreateDto dto) => throw new NotSupportedException();

        public int ImportProducts(IReadOnlyCollection<ProductCreateDto> products) =>
            throw new NotSupportedException();

        public void DeleteProduct(int productId) => throw new NotSupportedException();

        public void UpdateProduct(ProductUpdateDto dto) => throw new NotSupportedException();

        public void UpdateProductPrice(int productId, decimal newPrice) =>
            throw new NotSupportedException();
    }
}
