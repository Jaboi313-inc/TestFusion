using TestFusion.Core.Helpers;

namespace TestFusion.Tests.Core;

public class FromStringHelperTests
{
    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("   ", 0)]
    [InlineData("abc", 0)]
    [InlineData("42", 42)]
    [InlineData("-17", -17)]
    public void ToInt_ReturnsExpectedValue(string? input, int expected)
    {
        Assert.Equal(expected, FromStringHelper.ToInt(input));
    }

    [Theory]
    [InlineData(null, "0")]
    [InlineData("", "0")]
    [InlineData("   ", "0")]
    [InlineData("abc", "0")]
    [InlineData("42.5", "42.5")]
    [InlineData("-17.25", "-17.25")]
    public void ToDecimal_ReturnsExpectedValue(string? input, string expected)
    {
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), FromStringHelper.ToDecimal(input));
    }
}
