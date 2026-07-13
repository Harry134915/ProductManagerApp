using ProductManagerApp.BLL.Interfaces;
using ProductManagerApp.DTO;
using ProductManagerApp.ViewModels;

namespace ProductManagerApp.Tests.ViewModels;

/// <summary>
/// 验证商品列表的加载状态、错误恢复、搜索过滤和选择恢复行为。
/// </summary>
public class ProductListViewModelTests
{
    [Fact]
    public async Task LoadAsync_WhilePending_ShowsLoadingThenEmptyState()
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
        var viewModel = new ProductListViewModel(service);

        var loadTask = viewModel.LoadAsync();
        Assert.True(started.Wait(TimeSpan.FromSeconds(1)));

        try
        {
            Assert.True(viewModel.IsRefreshing);
            Assert.False(viewModel.HasLoaded);
            Assert.False(viewModel.IsEmpty);
            Assert.Equal("正在加载商品列表...", viewModel.LoadingMessage);
        }
        finally
        {
            release.Set();
        }

        await loadTask;

        Assert.False(viewModel.IsRefreshing);
        Assert.True(viewModel.HasLoaded);
        Assert.True(viewModel.IsEmpty);
        Assert.False(viewModel.HasLoadError);
    }

    [Fact]
    public async Task LoadAsync_WithProducts_HidesEmptyState()
    {
        var service = new StubProductService
        {
            GetAllProductsHandler = () => new List<ProductQueryDto>
            {
                CreateProduct()
            }
        };
        var viewModel = new ProductListViewModel(service);

        await viewModel.LoadAsync();

        Assert.Single(viewModel.Products);
        Assert.True(viewModel.HasLoaded);
        Assert.False(viewModel.IsEmpty);
        Assert.False(viewModel.HasLoadError);
    }

    [Fact]
    public async Task LoadAsync_WhenServiceFails_ShowsRetryableErrorState()
    {
        var service = new StubProductService
        {
            GetAllProductsHandler = () => throw new InvalidOperationException("Database unavailable")
        };
        var viewModel = new ProductListViewModel(service);

        await Assert.ThrowsAsync<InvalidOperationException>(() => viewModel.LoadAsync());

        Assert.False(viewModel.IsRefreshing);
        Assert.True(viewModel.HasLoaded);
        Assert.True(viewModel.HasLoadError);
        Assert.False(viewModel.IsEmpty);
        Assert.Equal("商品列表加载失败，请稍后重试。", viewModel.LoadErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_WhenCancelled_HidesLoadingWithoutShowingError()
    {
        using var started = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var cancellationTokenSource = new CancellationTokenSource();
        var service = new StubProductService
        {
            GetAllProductsHandler = () =>
            {
                started.Set();
                release.Wait();
                return new List<ProductQueryDto>();
            }
        };
        var viewModel = new ProductListViewModel(service);

        var loadTask = viewModel.LoadAsync(cancellationTokenSource.Token);
        Assert.True(started.Wait(TimeSpan.FromSeconds(1)));

        cancellationTokenSource.Cancel();
        release.Set();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => loadTask);

        Assert.False(viewModel.IsRefreshing);
        Assert.False(viewModel.HasLoadError);
        Assert.False(viewModel.IsEmpty);
    }

    [Fact]
    public async Task LoadAsync_AfterFailure_ClearsErrorAndRestoresEmptyState()
    {
        var shouldFail = true;
        var service = new StubProductService
        {
            GetAllProductsHandler = () => shouldFail
                ? throw new InvalidOperationException("Database unavailable")
                : new List<ProductQueryDto>()
        };
        var viewModel = new ProductListViewModel(service);
        await Assert.ThrowsAsync<InvalidOperationException>(() => viewModel.LoadAsync());

        shouldFail = false;
        await viewModel.LoadAsync();

        Assert.False(viewModel.HasLoadError);
        Assert.Null(viewModel.LoadErrorMessage);
        Assert.True(viewModel.IsEmpty);
        Assert.Equal("正在重新加载商品列表...", viewModel.LoadingMessage);
    }

    [Fact]
    public async Task LoadAsync_WithSelectedProduct_RestoresSelectionFromRefreshedData()
    {
        var productName = "Phone";
        var service = new StubProductService
        {
            GetAllProductsHandler = () => new List<ProductQueryDto>
            {
                new()
                {
                    Id = 1,
                    Code = "P001",
                    Name = productName,
                    Price = 1999m,
                    Stock = 10,
                    Description = "Flagship phone"
                }
            }
        };
        var viewModel = new ProductListViewModel(service);
        await viewModel.LoadAsync();
        var originalSelection = Assert.Single(viewModel.Products);
        viewModel.SelectedProduct = originalSelection;

        productName = "Phone Pro";
        await viewModel.LoadAsync();

        Assert.NotNull(viewModel.SelectedProduct);
        Assert.NotSame(originalSelection, viewModel.SelectedProduct);
        Assert.Equal(1, viewModel.SelectedProduct.Id);
        Assert.Equal("Phone Pro", viewModel.SelectedProduct.Name);
    }

    [Theory]
    [InlineData(" p001 ", 1)]
    [InlineData(" PHONE ", 1)]
    [InlineData("accessory", 2)]
    public async Task SearchText_FiltersCodeAndNameIgnoringCaseAndOuterSpaces(
        string searchText,
        int expectedProductId)
    {
        var service = CreateSearchService();
        var viewModel = new ProductListViewModel(service);
        await viewModel.LoadAsync();

        viewModel.SearchText = searchText;

        var visibleProduct = Assert.Single(GetVisibleProducts(viewModel));
        Assert.Equal(expectedProductId, visibleProduct.Id);
        Assert.Equal(3, viewModel.Products.Count);
        Assert.Equal(1, viewModel.FilteredProductCount);
        Assert.Equal("显示 1 / 共 3 条", viewModel.ResultCountText);
    }

    [Fact]
    public async Task SearchText_WithNoMatch_ShowsSearchEmptyStateWithoutChangingSource()
    {
        var viewModel = new ProductListViewModel(CreateSearchService());
        await viewModel.LoadAsync();

        viewModel.SearchText = "missing";

        Assert.Empty(GetVisibleProducts(viewModel));
        Assert.Equal(3, viewModel.Products.Count);
        Assert.True(viewModel.HasNoSearchResults);
        Assert.False(viewModel.IsEmpty);
        Assert.Equal("未找到与“missing”匹配的商品", viewModel.NoSearchResultsMessage);
    }

    [Fact]
    public async Task ClearSearchCommand_RestoresAllProducts()
    {
        var viewModel = new ProductListViewModel(CreateSearchService());
        await viewModel.LoadAsync();
        viewModel.SearchText = "phone";

        Assert.True(viewModel.ClearSearchCommand.CanExecute(null));
        viewModel.ClearSearchCommand.Execute(null);

        Assert.Equal(string.Empty, viewModel.SearchText);
        Assert.False(viewModel.HasActiveSearch);
        Assert.False(viewModel.ClearSearchCommand.CanExecute(null));
        Assert.Equal(3, GetVisibleProducts(viewModel).Count);
        Assert.Equal("共 3 条", viewModel.ResultCountText);
    }

    [Fact]
    public async Task ClearSearchCommand_WithWhitespaceText_RemainsAvailable()
    {
        var viewModel = new ProductListViewModel(CreateSearchService());
        await viewModel.LoadAsync();
        viewModel.SearchText = "   ";

        Assert.False(viewModel.HasActiveSearch);
        Assert.True(viewModel.ClearSearchCommand.CanExecute(null));

        viewModel.ClearSearchCommand.Execute(null);

        Assert.Equal(string.Empty, viewModel.SearchText);
        Assert.False(viewModel.ClearSearchCommand.CanExecute(null));
    }

    [Fact]
    public async Task LoadAsync_WithActiveSearch_PreservesSearchAndFiltersRefreshedData()
    {
        var products = new List<ProductQueryDto>
        {
            CreateProduct(1, "P001", "Phone")
        };
        var service = new StubProductService
        {
            GetAllProductsHandler = () => products.ToList()
        };
        var viewModel = new ProductListViewModel(service);
        await viewModel.LoadAsync();
        viewModel.SearchText = " phone ";

        products = new List<ProductQueryDto>
        {
            CreateProduct(1, "P001", "Phone Pro"),
            CreateProduct(2, "A001", "Accessory")
        };
        await viewModel.LoadAsync();

        Assert.Equal(" phone ", viewModel.SearchText);
        var visibleProduct = Assert.Single(GetVisibleProducts(viewModel));
        Assert.Equal("Phone Pro", visibleProduct.Name);
        Assert.Equal(2, viewModel.Products.Count);
        Assert.Equal("显示 1 / 共 2 条", viewModel.ResultCountText);
    }

    [Fact]
    public async Task SearchText_WhenSelectedProductIsFilteredOut_ClearsSelection()
    {
        var viewModel = new ProductListViewModel(CreateSearchService());
        await viewModel.LoadAsync();
        viewModel.SelectedProduct = viewModel.Products.Single(product => product.Id == 2);

        viewModel.SearchText = "P001";

        Assert.Null(viewModel.SelectedProduct);
    }

    private static StubProductService CreateSearchService()
    {
        return new StubProductService
        {
            GetAllProductsHandler = () => new List<ProductQueryDto>
            {
                CreateProduct(1, "P001", "Phone"),
                CreateProduct(2, "A001", "Accessory"),
                CreateProduct(3, "T001", "Tablet")
            }
        };
    }

    private static List<ProductQueryDto> GetVisibleProducts(ProductListViewModel viewModel)
    {
        return viewModel.FilteredProducts.ToList();
    }

    private static ProductQueryDto CreateProduct(
        int id = 1,
        string code = "P001",
        string name = "Phone")
    {
        return new ProductQueryDto
        {
            Id = id,
            Code = code,
            Name = name,
            Price = 1999m,
            Stock = 10,
            Description = "Flagship phone"
        };
    }

    private sealed class StubProductService : IProductService
    {
        public required Func<List<ProductQueryDto>> GetAllProductsHandler { get; init; }

        public List<ProductQueryDto> GetAllProducts() => GetAllProductsHandler();

        public void AddProduct(ProductCreateDto dto) => throw new NotSupportedException();

        public void DeleteProduct(int productId) => throw new NotSupportedException();

        public void UpdateProduct(ProductUpdateDto dto) => throw new NotSupportedException();

        public void UpdateProductPrice(int productId, decimal newPrice) =>
            throw new NotSupportedException();
    }
}
