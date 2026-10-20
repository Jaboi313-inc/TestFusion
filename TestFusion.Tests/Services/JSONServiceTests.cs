using System.Text.Json;
using TestFusion.Core.Models.TestResult;
using TestFusion.Services.Services;
using TestFusion.Tests.Helpers;

namespace TestFusion.Tests.Services;

public class JSONServiceTests
{
    private readonly JSONService _service = new(new TimeZoneService());

    [Fact]
    public void ConvertToJson_PrettyJsonFalse_ReturnsCompactJson()
    {
        var json = _service.ConvertToJson(new { Name = "Test", Value = 1 });

        Assert.DoesNotContain('\n', json);
        Assert.Contains("\"Name\":\"Test\"", json);
    }

    [Fact]
    public void ConvertToJson_PrettyJsonTrue_ReturnsIndentedJson()
    {
        var json = _service.ConvertToJson(new { Name = "Test", Value = 1 }, prettyJson: true);

        Assert.Contains('\n', json);
        Assert.Contains("  \"Name\"", json);
    }

    [Fact]
    public void ConvertToJson_UnicodeSettingControlsEscaping()
    {
        var relaxed = _service.ConvertToJson(new { Text = "é" }, useUnicodeSymbols: true);
        var escaped = _service.ConvertToJson(new { Text = "é" }, useUnicodeSymbols: false);

        Assert.Contains("é", relaxed);
        Assert.Contains("\\u00E9", escaped, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConvertToTestReportModel_MapsAllScalarFieldsAndStoresJson()
    {
        var input = TestData.CreateResult("report-1");

        var result = _service.ConvertToTestReportModel(input);

        Assert.Equal(input.Id, result.Id);
        Assert.Equal(input.TimeOffTesting, result.TimeOffTesting);
        Assert.Equal(input.PartNumber, result.PartNumber);
        Assert.Equal(input.PartBrand, result.PartBrand);
        Assert.Equal(input.PartType, result.PartType);
        Assert.Equal(input.CustomerName, result.CustomerName);
        Assert.Equal(input.CustomerPhone, result.CustomerPhone);
        Assert.Equal(input.CustomerMail, result.CustomerMail);
        Assert.Equal(input.CustomerNotes, result.CustomerNotes);
        Assert.Equal(input.TestNotes, result.TestNotes);
        Assert.Equal(input.InjectorSerialNumber, result.InjectorSerialNumber);

        var stored = JsonSerializer.Deserialize<TestResultModel>(result.Json);
        Assert.NotNull(stored);
        Assert.Equal(input.Id, stored!.Id);
        Assert.Single(stored.Tests);
    }

    [Fact]
    public void ConvertToTestReportModel_NullSerialNumber_BecomesEmptyString()
    {
        var input = TestData.CreateResult("report-1");
        input.InjectorSerialNumber = null!;

        var result = _service.ConvertToTestReportModel(input);

        Assert.Equal(string.Empty, result.InjectorSerialNumber);
    }

    [Fact]
    public void ConvertToTestResultModel_MapsNormalTestAndSubTest()
    {
        var result = _service.ConvertToTestResultModel(TestData.CreateSourceJson("source-1"));

        Assert.Equal("source-1", result.Id);
        Assert.Equal("PN-1", result.PartNumber);
        Assert.Equal("Bosch", result.PartBrand);
        Assert.Equal("Injector", result.PartType);
        Assert.Equal("Customer", result.CustomerName);
        Assert.Equal("0612345678", result.CustomerPhone);
        Assert.Equal("customer@example.com", result.CustomerMail);
        Assert.Equal("Note", result.CustomerNotes);
        Assert.Equal("Test note", result.TestNotes);
        Assert.Equal("SN-123", result.InjectorSerialNumber);
        Assert.Equal(new DateTimeOffset(2026, 10, 18, 13, 0, 0, TimeSpan.Zero), result.TimeOffTesting);

        var test = Assert.Single(result.Tests);
        Assert.Equal(5, test.TestId);
        Assert.Equal("Leak test", test.TestName);
        Assert.Equal(4, test.TestStatus);
        Assert.Equal(2, test.TestOrder);
        Assert.Equal(15, test.TestTime);
        Assert.Equal("Flow", test.TestType);
        Assert.Equal(42, test.TestResponseTime);

        var sub = Assert.Single(test.SubTests);
        Assert.Equal("Tank 1", sub.TankName);
        Assert.Equal(1, sub.TankPosition);
        Assert.Equal(1.5m, sub.Min);
        Assert.Equal(5.5m, sub.Max);
        Assert.Equal(0.5m, sub.BlueMin);
        Assert.Equal(6.5m, sub.BlueMax);
        Assert.Equal(1.0m, sub.YellowMin);
        Assert.Equal(6.0m, sub.YellowMax);
        Assert.Equal(1.25m, sub.TolBlue);
        Assert.Equal("-10%", sub.ProcentMin);
        Assert.Equal("+10%", sub.ProcentMax);
        Assert.Equal("OK", sub.ProcentText);
        Assert.Equal("LOW", sub.ResultMin);
        Assert.Equal("OK", sub.ResultAverage);
        Assert.Equal("HIGH", sub.ResultMax);
        Assert.Equal(6, sub.ResultColor);
        Assert.Equal(new[] { 1.1m, 2.2m, 3.3m }, sub.Results);
    }

    [Fact]
    public void ConvertToTestResultModel_SkippedTest_HasNoRuntimeOrSubTests()
    {
        var result = _service.ConvertToTestResultModel(TestData.CreateSourceJson("source-1", skipped: true));

        var test = Assert.Single(result.Tests);
        Assert.Equal(1, test.TestStatus);
        Assert.Equal(0, test.TestTime);
        Assert.Equal(string.Empty, test.TestType);
        Assert.Equal(0, test.TestResponseTime);
        Assert.Empty(test.SubTests);
    }

    [Fact]
    public void ConvertToTestResultModel_MissingSlotData_LeavesSerialNumberNull()
    {
        var json = TestData.CreateSourceJson("source-1").Replace(
            "\"SlotsData\": [{ \"sn\": \"SN-123\" }],",
            string.Empty);

        var result = _service.ConvertToTestResultModel(json);

        Assert.Null(result.InjectorSerialNumber);
    }

    [Fact]
    public void ConvertToTestResultModel_PageWithoutTestData_IsIgnored()
    {
        const string json = """
        {
          "_id": "source-1",
          "datetime": "18/10/2026 15:00:00",
          "TestsDataPages": [ {}, { "TestData": [] } ]
        }
        """;

        var result = _service.ConvertToTestResultModel(json);

        Assert.Empty(result.Tests);
        Assert.Equal(string.Empty, result.PartNumber);
    }

    [Fact]
    public void ConvertToTestResultModel_InvalidJson_ThrowsJsonException()
    {
        Assert.ThrowsAny<JsonException>(() => _service.ConvertToTestResultModel("not-json"));
    }

    [Fact]
    public void ConvertToTestResultModel_MissingTestsDataPages_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() => _service.ConvertToTestResultModel("{\"_id\":\"x\"}"));
    }
}
