using ProductManagerApp.ViewModels;

namespace ProductManagerApp.Tests.ViewModels;

public class ProductFormViewModelTests
{
    [Fact]
    public void ValidateForSubmit_WithEmptyForm_ReturnsFalseAndFocusesCode()
    {
        var viewModel = new ProductFormViewModel();
        string? focusedProperty = null;
        viewModel.FocusRequested += propertyName => focusedProperty = propertyName;

        var isValid = viewModel.ValidateForSubmit();

        Assert.False(isValid);
        Assert.True(viewModel.HasErrors);
        Assert.Equal(nameof(ProductFormViewModel.Code), focusedProperty);
        Assert.Equal("请输入商品编码。", GetError(viewModel, nameof(ProductFormViewModel.Code)));
        Assert.Equal("请输入商品名称。", GetError(viewModel, nameof(ProductFormViewModel.Name)));
        Assert.Equal("请输入价格。", GetError(viewModel, nameof(ProductFormViewModel.Price)));
        Assert.Equal("请输入库存数量。", GetError(viewModel, nameof(ProductFormViewModel.Stock)));
        Assert.Equal(
            "请输入商品描述，且不能只包含空格。",
            GetError(viewModel, nameof(ProductFormViewModel.Description)));
    }

    [Fact]
    public void ValidateForSubmit_WithValidForm_ReturnsTrueWithoutRequestingFocus()
    {
        var viewModel = CreateValidForm();
        string? focusedProperty = null;
        viewModel.FocusRequested += propertyName => focusedProperty = propertyName;

        var isValid = viewModel.ValidateForSubmit();

        Assert.True(isValid);
        Assert.False(viewModel.HasErrors);
        Assert.Null(focusedProperty);
    }

    [Fact]
    public void ValidateForSubmit_WithWhitespaceTextFields_ShowsRequiredErrors()
    {
        var viewModel = CreateValidForm();
        viewModel.Code = "   ";
        viewModel.Name = "   ";
        viewModel.Description = "   ";

        var isValid = viewModel.ValidateForSubmit();

        Assert.False(isValid);
        Assert.Equal("请输入商品编码。", GetError(viewModel, nameof(ProductFormViewModel.Code)));
        Assert.Equal("请输入商品名称。", GetError(viewModel, nameof(ProductFormViewModel.Name)));
        Assert.Equal(
            "请输入商品描述，且不能只包含空格。",
            GetError(viewModel, nameof(ProductFormViewModel.Description)));
    }

    [Theory]
    [InlineData("abc", "请输入有效数字，例如 99.90。")]
    [InlineData("0", "价格必须大于 0。")]
    [InlineData("-1", "价格必须大于 0。")]
    public void ValidateField_WithInvalidPrice_ShowsHelpfulError(
        string price,
        string expectedError)
    {
        var viewModel = CreateValidForm();
        viewModel.Price = price;

        viewModel.ValidateField(nameof(ProductFormViewModel.Price));

        Assert.Equal(expectedError, GetError(viewModel, nameof(ProductFormViewModel.Price)));
    }

    [Theory]
    [InlineData("1.5", "库存必须是整数，例如 10。")]
    [InlineData("-1", "库存不能小于 0。")]
    public void ValidateField_WithInvalidStock_ShowsHelpfulError(
        string stock,
        string expectedError)
    {
        var viewModel = CreateValidForm();
        viewModel.Stock = stock;

        viewModel.ValidateField(nameof(ProductFormViewModel.Stock));

        Assert.Equal(expectedError, GetError(viewModel, nameof(ProductFormViewModel.Stock)));
    }

    [Fact]
    public void ValidatedField_WhenCorrected_ClearsErrorImmediately()
    {
        var viewModel = CreateValidForm();
        viewModel.Price = "invalid";
        viewModel.ValidateField(nameof(ProductFormViewModel.Price));

        viewModel.Price = "99.90";

        Assert.Null(GetError(viewModel, nameof(ProductFormViewModel.Price)));
        Assert.False(viewModel.HasErrors);
    }

    [Fact]
    public void Clear_RemovesValuesAndValidationErrors()
    {
        var viewModel = new ProductFormViewModel();
        viewModel.ValidateForSubmit();

        viewModel.Clear();

        Assert.False(viewModel.HasErrors);
        Assert.Equal(string.Empty, viewModel.Code);
        Assert.Equal(string.Empty, viewModel.Name);
        Assert.Equal(string.Empty, viewModel.Price);
        Assert.Equal(string.Empty, viewModel.Stock);
        Assert.Equal(string.Empty, viewModel.Description);
    }

    private static ProductFormViewModel CreateValidForm()
    {
        return new ProductFormViewModel
        {
            Code = "P001",
            Name = "Phone",
            Price = "1999.90",
            Stock = "10",
            Description = "Flagship phone"
        };
    }

    private static string? GetError(
        ProductFormViewModel viewModel,
        string propertyName)
    {
        return viewModel.GetErrors(propertyName).Cast<string>().SingleOrDefault();
    }
}
