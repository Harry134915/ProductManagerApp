using ProductManagerApp.BLL.Validators;
using ProductManagerApp.Entity;
using ProductManagerApp.Infrastructure.Exceptions;

namespace ProductManagerApp.Tests.BLL.Validators;

public class ProductValidatorTests
{
    private readonly ProductValidator _validator = new();

    [Fact]
    public void Validate_WithValidProduct_DoesNotThrow()
    {
        var product = CreateValidProduct();

        var exception = Record.Exception(() => _validator.Validate(product));

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WithNullProduct_ThrowsValidationException()
    {
        Assert.Throws<ProductValidationException>(() => _validator.Validate(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyCode_ThrowsValidationException(string code)
    {
        var product = CreateValidProduct();
        product.Code = code;

        var exception = Assert.Throws<ProductValidationException>(
            () => _validator.Validate(product));

        Assert.Equal(ProductValidationRules.CodeRequiredMessage, exception.Message);
    }

    [Theory]
    [InlineData("跳跳糖")]
    [InlineData("P 001")]
    [InlineData("P001!")]
    public void Validate_WithInvalidCodeFormat_ThrowsValidationException(string code)
    {
        var product = CreateValidProduct();
        product.Code = code;

        var exception = Assert.Throws<ProductValidationException>(
            () => _validator.Validate(product));

        Assert.Equal(ProductCodeRules.InvalidFormatMessage, exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyName_ThrowsValidationException(string name)
    {
        var product = CreateValidProduct();
        product.Name = name;

        var exception = Assert.Throws<ProductValidationException>(
            () => _validator.Validate(product));

        Assert.Equal(ProductValidationRules.NameRequiredMessage, exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithInvalidPrice_ThrowsValidationException(decimal price)
    {
        var product = CreateValidProduct();
        product.Price = price;

        var exception = Assert.Throws<ProductValidationException>(
            () => _validator.Validate(product));

        Assert.Equal(ProductValidationRules.PricePositiveMessage, exception.Message);
    }

    [Fact]
    public void Validate_WithNegativeStock_ThrowsValidationException()
    {
        var product = CreateValidProduct();
        product.Stock = -1;

        var exception = Assert.Throws<ProductValidationException>(
            () => _validator.Validate(product));

        Assert.Equal(ProductValidationRules.StockNonNegativeMessage, exception.Message);
    }

    [Fact]
    public void Validate_WithBlankDescription_ThrowsValidationException()
    {
        var product = CreateValidProduct();
        product.Description = "   ";

        var exception = Assert.Throws<ProductValidationException>(
            () => _validator.Validate(product));

        Assert.Equal(ProductValidationRules.DescriptionRequiredMessage, exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValidateId_WithInvalidId_ThrowsValidationException(int id)
    {
        Assert.Throws<ProductValidationException>(() => _validator.ValidateId(id));
    }

    [Fact]
    public void ValidateCodeUnchanged_WhenCodeChanged_ThrowsValidationException()
    {
        Assert.Throws<ProductValidationException>(
            () => _validator.ValidateCodeUnchanged("P001", "P002"));
    }

    [Fact]
    public void ValidateCodeUnchanged_WhenCodeSame_DoesNotThrow()
    {
        var exception = Record.Exception(() => _validator.ValidateCodeUnchanged("P001", "P001"));

        Assert.Null(exception);
    }

    private static Product CreateValidProduct()
    {
        return new Product
        {
            Id = 1,
            Code = "P001",
            Name = "Phone",
            Price = 1999m,
            Stock = 10,
            Description = "Valid product"
        };
    }
}
