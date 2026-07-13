using ProductManagerApp.Infrastructure.Input;

namespace ProductManagerApp.Tests.Infrastructure.Input;

/// <summary>
/// 验证价格、库存以及粘贴文本的候选输入规则。
/// </summary>
public class NumericInputRulesTests
{
    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("99")]
    [InlineData("99.90")]
    [InlineData(".5")]
    [InlineData("12.")]
    public void IsValidPriceCandidate_WithSupportedInput_ReturnsTrue(string value)
    {
        Assert.True(NumericInputRules.IsValidPriceCandidate(value));
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("1.2.3")]
    [InlineData("1,000")]
    [InlineData("abc")]
    [InlineData("中文")]
    [InlineData(" ")]
    public void IsValidPriceCandidate_WithUnsupportedInput_ReturnsFalse(string value)
    {
        Assert.False(NumericInputRules.IsValidPriceCandidate(value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("100")]
    public void IsValidStockCandidate_WithNonNegativeInteger_ReturnsTrue(string value)
    {
        Assert.True(NumericInputRules.IsValidStockCandidate(value));
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("1.5")]
    [InlineData("abc")]
    [InlineData("中文")]
    [InlineData(" ")]
    public void IsValidStockCandidate_WithUnsupportedInput_ReturnsFalse(string value)
    {
        Assert.False(NumericInputRules.IsValidStockCandidate(value));
    }

    [Fact]
    public void ApplyInput_ReplacesCurrentSelection()
    {
        var candidate = NumericInputRules.ApplyInput("199.90", 0, 6, "88.5");

        Assert.Equal("88.5", candidate);
    }

    [Fact]
    public void ApplyInput_InsertsPastedTextAtCaret()
    {
        var candidate = NumericInputRules.ApplyInput("12", 2, 0, ".50");

        Assert.Equal("12.50", candidate);
    }
}
