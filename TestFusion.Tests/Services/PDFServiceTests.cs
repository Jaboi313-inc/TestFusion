using System.Reflection;
using System.Text;
using TestFusion.Core.Enums;
using TestFusion.Services.Services;
using TestFusion.Tests.Helpers;

namespace TestFusion.Tests.Services;

public class PDFServiceTests
{
    private readonly TestStringLocalizer _localizer = new();
    private readonly TimeZoneService _timeZoneService = new();

    [Theory]
    [InlineData(PdfLayoutModeEnum.Compact)]
    [InlineData(PdfLayoutModeEnum.KeepTestTogether)]
    [InlineData(PdfLayoutModeEnum.OneTestPerPage)]
    public void Generate_ForEachLayout_ReturnsPdf(PdfLayoutModeEnum mode)
    {
        var model = TestData.CreateGeneratedModel();

        var bytes = PDFService.Generate(model, mode, _localizer, _timeZoneService);

        Assert.NotEmpty(bytes);
        Assert.True(bytes.Length > 4);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public void Generate_UnknownLayout_FallsBackToDefaultAndReturnsPdf()
    {
        var bytes = PDFService.Generate(
            TestData.CreateGeneratedModel(),
            (PdfLayoutModeEnum)999,
            _localizer,
            _timeZoneService);

        Assert.NotEmpty(bytes);
        Assert.True(bytes.Length > 4);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public void Generate_EmptyModel_ReturnsPdf()
    {
        var bytes = PDFService.Generate(
            new TestFusion.Core.Models.WebModels.GeneratedModel(),
            PdfLayoutModeEnum.Compact,
            _localizer,
            _timeZoneService);

        Assert.NotEmpty(bytes);
        Assert.True(bytes.Length > 4);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Theory]
    [InlineData(" Leak test : SKIPPED ", true, "Leak test")]
    [InlineData("Leak test", true, "Leak test")]
    [InlineData("iVM.04 : HP ERROR", true, "iVM.04")]
    [InlineData("iVM.04 : HP ERROR : Extra", true, "iVM.04")]
    [InlineData("RSP : HP ERROR", true, "RSP")]
    public void NormalizeTestName_WhenGroupingEnabled_ReturnsBaseTestName(
        string input,
        bool groupTestsByBaseName,
        string expected)
    {
        var result = InvokePrivate<string>(
            "NormalizeTestName",
            input,
            groupTestsByBaseName);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(" Leak test : SKIPPED ", false, "Leak test")]
    [InlineData("Leak test", false, "Leak test")]
    [InlineData("iVM.04 : HP ERROR", false, "iVM.04 : HP ERROR")]
    [InlineData("iVM.04 : HP ERROR : Extra", false, "iVM.04 : HP ERROR : Extra")]
    [InlineData("RSP : HP ERROR", false, "RSP : HP ERROR")]
    public void NormalizeTestName_WhenGroupingDisabled_ReturnsCompleteTestName(
        string input,
        bool groupTestsByBaseName,
        string expected)
    {
        var result = InvokePrivate<string>(
            "NormalizeTestName",
            input,
            groupTestsByBaseName);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("<b>Hello</b>", "Hello")]
    [InlineData("Text<br>More", "TextMore")]
    [InlineData("", "")]
    public void RemoveHtml_ReturnsExpectedValue(string input, string expected)
    {
        Assert.Equal(
            expected,
            InvokePrivate<string>("RemoveHtml", input));
    }

    [Theory]
    [InlineData(null, "<NULL>")]
    [InlineData("", "<EMPTY>")]
    [InlineData("   ", "<EMPTY>")]
    [InlineData("EGA", "EGA")]
    [InlineData("ega", "EGA")]
    [InlineData("  EGA  ", "EGA")]
    [InlineData("<b>EGA</b>", "EGA")]
    [InlineData("EGA   Test", "EGA TEST")]
    public void NormalizeCustomerValue_ReturnsExpectedValue(
        string? input,
        string expected)
    {
        Assert.Equal(
            expected,
            InvokePrivate<string>("NormalizeCustomerValue", input));
    }

    [Fact]
    public void CreateCustomerKey_AllMatchesEnabled_UsesAllCustomerValues()
    {
        var result = InvokePrivate<string>(
            "CreateCustomerKey",
            "EGA",
            "0123456789",
            "test@example.com",
            "Note",
            true,
            true,
            true,
            true);

        Assert.Equal(
            "NAME:EGA\u001FPHONE:0123456789\u001FMAIL:TEST@EXAMPLE.COM\u001FNOTES:NOTE",
            result);
    }

    [Fact]
    public void CreateCustomerKey_OnlyNameEnabled_UsesOnlyCustomerName()
    {
        var result = InvokePrivate<string>(
            "CreateCustomerKey",
            "EGA",
            "0123456789",
            "test@example.com",
            "Note",
            true,
            false,
            false,
            false);

        Assert.Equal(
            "NAME:EGA",
            result);
    }

    [Fact]
    public void CreateCustomerKey_NameAndMailEnabled_UsesOnlySelectedValues()
    {
        var result = InvokePrivate<string>(
            "CreateCustomerKey",
            "EGA",
            "0123456789",
            "test@example.com",
            "Note",
            true,
            false,
            true,
            false);

        Assert.Equal(
            "NAME:EGA\u001FMAIL:TEST@EXAMPLE.COM",
            result);
    }

    [Theory]
    [InlineData(1, "#FF331C")]
    [InlineData(2, "#FF331C")]
    [InlineData(3, "#FF331C")]
    [InlineData(4, "#FF331C")]
    [InlineData(5, "#0000FF")]
    [InlineData(6, "#00AB4F")]
    [InlineData(8, "#FF331C")]
    [InlineData(0, "#FFFFFF")]
    [InlineData(99, "#FFFFFF")]
    public void GetResultBackgroundColor_ReturnsExpectedValue(
        int input,
        string expected)
    {
        Assert.Equal(
            expected,
            InvokePrivate<string>("GetResultBackgroundColor", input));
    }

    [Theory]
    [InlineData(1, "#FFFFFF")]
    [InlineData(2, "#FFFFFF")]
    [InlineData(3, "#FFFFFF")]
    [InlineData(4, "#FFFFFF")]
    [InlineData(5, "#FFFFFF")]
    [InlineData(6, "#FFFFFF")]
    [InlineData(8, "#FFFFFF")]
    [InlineData(0, "#000000")]
    [InlineData(99, "#000000")]
    public void GetResultTextColor_ReturnsExpectedValue(
        int input,
        string expected)
    {
        Assert.Equal(
            expected,
            InvokePrivate<string>("GetResultTextColor", input));
    }

    private static T InvokePrivate<T>(
        string methodName,
        params object?[] arguments)
    {
        var method = typeof(PDFService).GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        return (T)method!.Invoke(null, arguments)!;
    }
}