using ProductManagerApp.BLL.Interfaces;
using ProductManagerApp.DTO;
using ProductManagerApp.ViewModels;

namespace ProductManagerApp.Tests.ViewModels;

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
