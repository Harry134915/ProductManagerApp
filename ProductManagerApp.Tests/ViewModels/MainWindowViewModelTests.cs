using ProductManagerApp.BLL.Services;
using ProductManagerApp.BLL.Validators;
using ProductManagerApp.DTO;
using ProductManagerApp.Tests.Fakes;
using ProductManagerApp.ViewModels;

namespace ProductManagerApp.Tests.ViewModels;

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
        repository.Products.Add(new ProductManagerApp.Entity.Product
        {
            Id = 1,
            Code = "P001",
            Name = "Phone",
            Price = 1999m,
            Stock = 10,
            Description = "Flagship phone"
        });
        var viewModel = CreateViewModel(repository);

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
        var viewModel = CreateViewModel(repository);

        try
        {
            await WaitUntilAsync(() => viewModel.List.HasLoaded && !viewModel.List.IsRefreshing);
            viewModel.AddCommand.Execute(null);

            Assert.True(viewModel.Form.HasErrors);
            Assert.Equal(0, repository.AddProductCallCount);
            Assert.True(string.IsNullOrEmpty(viewModel.ErrorMessage));
            Assert.False(viewModel.AddCommand.IsExecuting);
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
        var viewModel = CreateViewModel(repository);

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
        var viewModel = CreateViewModel(repository);

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
        }
        finally
        {
            viewModel.CancelOperations();
        }
    }

    private static MainWindowViewModel CreateViewModel(
        FakeProductRepository? repository = null)
    {
        repository ??= new FakeProductRepository();
        var service = new ProductService(repository, new ProductValidator());
        return new MainWindowViewModel(service);
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
}
