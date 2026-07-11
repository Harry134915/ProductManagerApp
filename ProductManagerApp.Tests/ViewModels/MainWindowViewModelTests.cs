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
        }
        finally
        {
            viewModel.CancelOperations();
        }
    }

    [Fact]
    public void SelectingProduct_UsesEditModeAndDisablesAdd()
    {
        var viewModel = CreateViewModel();

        try
        {
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
        }
        finally
        {
            viewModel.CancelOperations();
        }
    }

    [Fact]
    public void AddCommand_WithInvalidForm_UsesInlineErrorsWithoutCallingRepository()
    {
        var repository = new FakeProductRepository();
        var viewModel = CreateViewModel(repository);

        try
        {
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
    public void UpdateCommand_WithInvalidForm_UsesInlineErrorsWithoutCallingRepository()
    {
        var repository = new FakeProductRepository();
        var viewModel = CreateViewModel(repository);

        try
        {
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
}
