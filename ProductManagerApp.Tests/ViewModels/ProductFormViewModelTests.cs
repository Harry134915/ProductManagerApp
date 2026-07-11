using ProductManagerApp.BLL.Validators;
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
        Assert.Equal(
            ProductValidationRules.CodeRequiredMessage,
            GetError(viewModel, nameof(ProductFormViewModel.Code)));
        Assert.Equal(
            ProductValidationRules.NameRequiredMessage,
            GetError(viewModel, nameof(ProductFormViewModel.Name)));
        Assert.Equal("请输入价格。", GetError(viewModel, nameof(ProductFormViewModel.Price)));
        Assert.Equal("请输入库存数量。", GetError(viewModel, nameof(ProductFormViewModel.Stock)));
        Assert.Equal(
            ProductValidationRules.DescriptionRequiredMessage,
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
    public void RequestFocus_WithKnownField_RaisesFocusRequested()
    {
        var viewModel = new ProductFormViewModel();
        string? focusedProperty = null;
        viewModel.FocusRequested += propertyName => focusedProperty = propertyName;

        viewModel.RequestFocus(nameof(ProductFormViewModel.Price));

        Assert.Equal(nameof(ProductFormViewModel.Price), focusedProperty);
    }

    [Fact]
    public void RequestFocus_WithUnknownField_DoesNotRaiseFocusRequested()
    {
        var viewModel = new ProductFormViewModel();
        var requestCount = 0;
        viewModel.FocusRequested += _ => requestCount++;

        viewModel.RequestFocus("UnknownField");

        Assert.Equal(0, requestCount);
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
    [InlineData("跳跳糖")]
    [InlineData("P 001")]
    [InlineData("P001!")]
    public void ValidateForSubmit_WithInvalidCodeFormat_ShowsCodeError(string code)
    {
        var viewModel = CreateValidForm();
        viewModel.Code = code;

        var isValid = viewModel.ValidateForSubmit();

        Assert.False(isValid);
        Assert.Equal(
            "商品编码只能包含英文字母、数字、连字符（-）和下划线（_）。",
            GetError(viewModel, nameof(ProductFormViewModel.Code)));
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
    public void ReportInputError_WhenPriceIsCorrected_ClearsInputError()
    {
        var viewModel = CreateValidForm();
        viewModel.ReportInputError(
            nameof(ProductFormViewModel.Price),
            "价格只能输入数字和一个小数点。");

        Assert.Equal(
            "价格只能输入数字和一个小数点。",
            GetError(viewModel, nameof(ProductFormViewModel.Price)));

        viewModel.Price = "88.50";

        Assert.Null(GetError(viewModel, nameof(ProductFormViewModel.Price)));
        Assert.False(viewModel.HasErrors);
    }

    [Fact]
    public void ReportInputError_WhenStockIsCorrected_ClearsInputError()
    {
        var viewModel = CreateValidForm();
        viewModel.ReportInputError(
            nameof(ProductFormViewModel.Stock),
            "库存只能输入非负整数。");

        Assert.Equal(
            "库存只能输入非负整数。",
            GetError(viewModel, nameof(ProductFormViewModel.Stock)));

        viewModel.Stock = "20";

        Assert.Null(GetError(viewModel, nameof(ProductFormViewModel.Stock)));
        Assert.False(viewModel.HasErrors);
    }

    [Fact]
    public void TryCreateDto_WithValidForm_ReturnsMappedDto()
    {
        var viewModel = CreateValidForm();

        var success = viewModel.TryCreateDto(out var dto);

        Assert.True(success);
        Assert.NotNull(dto);
        Assert.Equal("P001", dto.Code);
        Assert.Equal("Phone", dto.Name);
        Assert.Equal(1999.90m, dto.Price);
        Assert.Equal(10, dto.Stock);
        Assert.Equal("Flagship phone", dto.Description);
    }

    [Fact]
    public void TryCreateDto_WithInvalidForm_ReturnsFalseAndNoDto()
    {
        var viewModel = new ProductFormViewModel();
        string? focusedProperty = null;
        viewModel.FocusRequested += propertyName => focusedProperty = propertyName;

        var success = viewModel.TryCreateDto(out var dto);

        Assert.False(success);
        Assert.Null(dto);
        Assert.Equal(nameof(ProductFormViewModel.Code), focusedProperty);
        Assert.True(viewModel.HasErrors);
    }

    [Fact]
    public void TryUpdateDto_WithValidForm_PreservesSelectedIdentity()
    {
        var viewModel = CreateValidForm();
        var selected = new ProductManagerApp.DTO.ProductQueryDto
        {
            Id = 7,
            Code = "P007",
            Name = "Old name",
            Price = 1m,
            Stock = 1,
            Description = "Old description"
        };

        var success = viewModel.TryUpdateDto(selected, out var dto);

        Assert.True(success);
        Assert.NotNull(dto);
        Assert.Equal(7, dto.Id);
        Assert.Equal("P007", dto.Code);
        Assert.Equal("Phone", dto.Name);
        Assert.Equal(1999.90m, dto.Price);
        Assert.Equal(10, dto.Stock);
        Assert.Equal("Flagship phone", dto.Description);
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
