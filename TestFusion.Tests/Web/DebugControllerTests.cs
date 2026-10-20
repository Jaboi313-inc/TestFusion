using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using TestFusion.Core.Models.WebModels;
using TestFusion.Services.Services;
using TestFusion.Tests.Helpers;
using TestFusion.Web.Controllers;
using TestFusion.Web.Models;

namespace TestFusion.Tests.Web;

public class DebugControllerTests
{
    [Fact]
    public async Task Index_WithoutId_ReturnsReportsSortedAndLoadsLogs()
    {
        await using var db = TestData.CreateInMemoryContext();
        db.TestReports.AddRange(
            TestData.CreateReport("old", time: new DateTimeOffset(2026, 10, 18, 10, 0, 0, TimeSpan.Zero)),
            TestData.CreateReport("new", time: new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero)));
        await db.SaveChangesAsync();

        using var temp = new TempLogDirectory();
        File.WriteAllLines(
            Path.Combine(temp.LogDirectory, "worker-20261018.jsonl"),
            [
                "{\"@t\":\"2026-10-18T12:00:00.0000000Z\",\"@mt\":\"Processed report {ReportId}\",\"ReportId\":\"abc\",\"SourceContext\":\"TestFusion.Services.SyncService\"}",
                "not-json"
            ]);

        var controller = CreateController(db, new FakePlaywright(), temp.ContentRootPath);
        ControllerTestHelper.AttachHttpContext(controller);

        var result = await controller.Index(null);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DebugViewModel>(view.Model);
        Assert.Equal(new[] { "new", "old" }, model.TestReports.Select(x => x.Id));
        var log = Assert.Single(model.Logs);
        Assert.Equal("Worker", log.Application);
        Assert.Equal("Information", log.Level);
        Assert.Equal("SyncService", log.SourceContext);
        Assert.Equal("Processed report abc", log.Message);
        Assert.Equal("abc", log.Properties["ReportId"]);
    }

    [Fact]
    public async Task Index_WithExistingId_LoadsRawProcessedStoredAndComparesJson()
    {
        await using var db = TestData.CreateInMemoryContext();
        var rawJson = TestData.CreateSourceJson("report-1");
        var jsonService = new JSONService(new TimeZoneService());
        var processed = jsonService.ConvertToTestResultModel(rawJson);
        var report = jsonService.ConvertToTestReportModel(processed);
        db.TestReports.Add(report);
        await db.SaveChangesAsync();

        var playwright = new FakePlaywright();
        playwright.JsonById["report-1"] = rawJson;

        using var temp = new TempLogDirectory();
        var controller = CreateController(db, playwright, temp.ContentRootPath);
        ControllerTestHelper.AttachHttpContext(controller);

        var result = await controller.Index("  report-1  ");

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DebugViewModel>(view.Model);
        Assert.Equal("report-1", model.JsonId);
        Assert.True(model.StoredJsonFound);
        Assert.NotNull(model.RawJson);
        Assert.NotNull(model.ProcessedJson);
        Assert.NotNull(model.StoredJson);
        Assert.True(model.ProcessedMatchesStored);
        Assert.Null(model.JsonError);
    }

    [Fact]
    public async Task Index_WhenSourceFails_SetsJsonError()
    {
        await using var db = TestData.CreateInMemoryContext();
        var playwright = new FakePlaywright { GetDataException = new InvalidOperationException("Source unavailable") };
        using var temp = new TempLogDirectory();
        var controller = CreateController(db, playwright, temp.ContentRootPath);
        ControllerTestHelper.AttachHttpContext(controller);

        var result = await controller.Index("report-1");

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DebugViewModel>(view.Model);
        Assert.Equal("Source unavailable", model.JsonError);
    }

    [Fact]
    public async Task Index_LoadLogs_SortsNewestFirstAndLimitsTo500()
    {
        await using var db = TestData.CreateInMemoryContext();
        using var temp = new TempLogDirectory();
        var lines = Enumerable.Range(0, 510)
            .Select(i => $"{{\"@t\":\"{new DateTimeOffset(2026, 10, 18, 0, 0, 0, TimeSpan.Zero).AddSeconds(i):O}\",\"@m\":\"Message {i}\",\"SourceContext\":\"Example.Source\"}}")
            .ToArray();
        File.WriteAllLines(Path.Combine(temp.LogDirectory, "web-20261018.jsonl"), lines);

        var controller = CreateController(db, new FakePlaywright(), temp.ContentRootPath);
        ControllerTestHelper.AttachHttpContext(controller);

        var result = await controller.Index(null);

        var model = Assert.IsType<DebugViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(500, model.Logs.Count);
        Assert.Equal("Message 509", model.Logs[0].Message);
        Assert.Equal("Message 10", model.Logs[^1].Message);
    }

    [Fact]
    public async Task Index_ErrorLog_MapsLevelExceptionAndProperties()
    {
        await using var db = TestData.CreateInMemoryContext();
        using var temp = new TempLogDirectory();
        File.WriteAllText(
            Path.Combine(temp.LogDirectory, "web-20261018.jsonl"),
            "{\"@t\":\"2026-10-18T12:00:00Z\",\"@m\":\"Request failed\",\"@l\":\"Error\",\"@x\":\"System.Exception: boom\\n at Test()\",\"RequestId\":123,\"SourceContext\":\"TestFusion.Web.Controllers.DebugController\"}" + Environment.NewLine);

        var controller = CreateController(db, new FakePlaywright(), temp.ContentRootPath);
        ControllerTestHelper.AttachHttpContext(controller);

        var result = await controller.Index(null);

        var model = Assert.IsType<DebugViewModel>(Assert.IsType<ViewResult>(result).Model);
        var log = Assert.Single(model.Logs);
        Assert.Equal("Web", log.Application);
        Assert.Equal("Error", log.Level);
        Assert.Equal("DebugController", log.SourceContext);
        Assert.Contains("System.Exception", log.Exception);
        Assert.Equal("123", log.Properties["RequestId"]);
    }

    [Fact]
    public async Task DeleteSelectedTestReports_NoSelection_SetsMessageAndRedirects()
    {
        await using var db = TestData.CreateInMemoryContext();
        using var temp = new TempLogDirectory();
        var controller = CreateController(db, new FakePlaywright(), temp.ContentRootPath);
        ControllerTestHelper.AttachHttpContext(controller);

        var result = await controller.DeleteSelectedTestReports([]);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(DebugController.Index), redirect.ActionName);
        Assert.Equal("No rows selected.", controller.TempData["DebugMessage"]);
    }

    [Fact]
    public async Task DeleteSelectedTestReports_DeletesOnlySelectedRows()
    {
        var tuple = TestData.CreateSqliteContext();
        await using var db = tuple.Context;
        await using var connection = tuple.Connection;
        db.TestReports.AddRange(TestData.CreateReport("one"), TestData.CreateReport("two"));
        await db.SaveChangesAsync();

        using var temp = new TempLogDirectory();
        var controller = CreateController(db, new FakePlaywright(), temp.ContentRootPath);
        ControllerTestHelper.AttachHttpContext(controller);

        var result = await controller.DeleteSelectedTestReports(["one"]);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.False(db.TestReports.Any(x => x.Id == "one"));
        Assert.True(db.TestReports.Any(x => x.Id == "two"));
        Assert.Equal("1 selected TestReports deleted.", controller.TempData["DebugMessage"]);
    }

    [Fact]
    public async Task ClearTestReports_NotConfirmed_ReturnsBadRequest()
    {
        await using var db = TestData.CreateInMemoryContext();
        using var temp = new TempLogDirectory();
        var controller = CreateController(db, new FakePlaywright(), temp.ContentRootPath);
        ControllerTestHelper.AttachHttpContext(controller);

        var result = await controller.ClearTestReports(false);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Confirmation is required.", badRequest.Value);
    }

    [Fact]
    public async Task ClearTestReports_Confirmed_DeletesAllRows()
    {
        var tuple = TestData.CreateSqliteContext();
        await using var db = tuple.Context;
        await using var connection = tuple.Connection;
        db.TestReports.AddRange(TestData.CreateReport("one"), TestData.CreateReport("two"));
        await db.SaveChangesAsync();

        using var temp = new TempLogDirectory();
        var controller = CreateController(db, new FakePlaywright(), temp.ContentRootPath);
        ControllerTestHelper.AttachHttpContext(controller);

        var result = await controller.ClearTestReports(true);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Empty(db.TestReports);
        Assert.Equal("All TestReports deleted. 2 rows deleted.", controller.TempData["DebugMessage"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DownloadRawJson_EmptyId_ReturnsBadRequest(string id)
    {
        await using var db = TestData.CreateInMemoryContext();
        using var temp = new TempLogDirectory();
        var controller = CreateController(db, new FakePlaywright(), temp.ContentRootPath);
        ControllerTestHelper.AttachHttpContext(controller);

        var result = await controller.DownloadRawJson(id);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DownloadRawJson_ValidId_ReturnsSourceJsonFile()
    {
        await using var db = TestData.CreateInMemoryContext();
        var playwright = new FakePlaywright();
        playwright.JsonById["one"] = "{\"value\":1}";
        using var temp = new TempLogDirectory();
        var controller = CreateController(db, playwright, temp.ContentRootPath);
        ControllerTestHelper.AttachHttpContext(controller);

        var result = await controller.DownloadRawJson("one");

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/json", file.ContentType);
        Assert.Equal("raw_one.json", file.FileDownloadName);
        Assert.Equal("{\"value\":1}", Encoding.UTF8.GetString(file.FileContents));
    }

    [Fact]
    public async Task DownloadProcessedJson_ValidId_ReturnsProcessedJsonFile()
    {
        await using var db = TestData.CreateInMemoryContext();
        var playwright = new FakePlaywright();
        playwright.JsonById["one"] = TestData.CreateSourceJson("one");
        using var temp = new TempLogDirectory();
        var controller = CreateController(db, playwright, temp.ContentRootPath);
        ControllerTestHelper.AttachHttpContext(controller);

        var result = await controller.DownloadProcessedJson("one");

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("processed_one.json", file.FileDownloadName);
        var json = Encoding.UTF8.GetString(file.FileContents);
        Assert.Contains("\"Id\": \"one\"", json);
        Assert.Contains('\n', json);
    }

    [Fact]
    public async Task DownloadStoredJson_MissingId_ReturnsNotFound()
    {
        await using var db = TestData.CreateInMemoryContext();
        using var temp = new TempLogDirectory();
        var controller = CreateController(db, new FakePlaywright(), temp.ContentRootPath);
        ControllerTestHelper.AttachHttpContext(controller);

        var result = await controller.DownloadStoredJson("missing");

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("missing", notFound.Value!.ToString());
    }

    [Fact]
    public async Task DownloadStoredJson_ExistingId_ReturnsStoredJson()
    {
        await using var db = TestData.CreateInMemoryContext();
        var report = TestData.CreateReport("one");
        db.TestReports.Add(report);
        await db.SaveChangesAsync();
        using var temp = new TempLogDirectory();
        var controller = CreateController(db, new FakePlaywright(), temp.ContentRootPath);
        ControllerTestHelper.AttachHttpContext(controller);

        var result = await controller.DownloadStoredJson("one");

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("stored_one.json", file.FileDownloadName);
        Assert.Equal(report.Json, Encoding.UTF8.GetString(file.FileContents));
    }

    private static DebugController CreateController(
        TestFusion.Data.AppDbContext db,
        FakePlaywright playwright,
        string contentRootPath)
    {
        return new DebugController(
            db,
            playwright,
            new JSONService(new TimeZoneService()),
            new TestStringLocalizer(),
            new TestWebHostEnvironment { ContentRootPath = contentRootPath },
            NullLogger<DebugController>.Instance);
    }

    private sealed class TempLogDirectory : IDisposable
    {
        public string Root { get; }
        public string ContentRootPath { get; }
        public string LogDirectory { get; }

        public TempLogDirectory()
        {
            Root = Path.Combine(Path.GetTempPath(), $"testfusion-debug-{Guid.NewGuid():N}");
            ContentRootPath = Path.Combine(Root, "TestFusion.Web");
            LogDirectory = Path.Combine(Root, "logs");
            Directory.CreateDirectory(ContentRootPath);
            Directory.CreateDirectory(LogDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
        }
    }
}
