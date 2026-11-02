using Microsoft.AspNetCore.Mvc;
using TestFusion.Core.Interfaces;
using TestFusion.Core.Models;
using TestFusion.Core.Models.TestResult;
using TestFusion.Core.Models.WebModels;
using TestFusion.Services.Services;
using TestFusion.Tests.Helpers;
using TestFusion.Web.Controllers;

namespace TestFusion.Tests.Web;

public class AnalysisControllerTests
{
    [Fact]
    public void Generate_NoSelection_RedirectsToHome()
    {
        using var db = TestData.CreateInMemoryContext();
        var controller = CreateController(db);
        ControllerTestHelper.AttachHttpContext(controller);

        var result = controller.Generate([]);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public void Generate_WithSelection_RedirectsToAnalysisIndex()
    {
        using var db = TestData.CreateInMemoryContext();
        var controller = CreateController(db);
        ControllerTestHelper.AttachHttpContext(controller);
        var selected = new List<string> { "one", "two" };

        var result = controller.Generate(selected);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AnalysisController.Index), redirect.ActionName);
        Assert.NotNull(redirect.RouteValues);
        Assert.Same(selected, redirect.RouteValues!["selectedIds"]);
    }

    [Fact]
    public async Task Index_NoSelection_RedirectsToHome()
    {
        await using var db = TestData.CreateInMemoryContext();
        var controller = CreateController(db);
        ControllerTestHelper.AttachHttpContext(controller);

        var result = await controller.Index([]);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task Index_DifferentPartNumbers_RedirectsAndSetsError()
    {
        await using var db = TestData.CreateInMemoryContext();
        db.TestReports.AddRange(
            TestData.CreateReport("one", "PN-1"),
            TestData.CreateReport("two", "PN-2"));
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        ControllerTestHelper.AttachHttpContext(controller);

        var result = await controller.Index(["one", "two"]);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Home", redirect.ControllerName);
        Assert.Equal("Different part number.", controller.TempData["Error"]);
    }

    [Fact]
    public async Task Index_SamePartNumber_BuildsNormalizedModel()
    {
        await using var db = TestData.CreateInMemoryContext();

        var first = TestData.CreateResult("one", "PN-1");
        first.Tests =
        [
            new TestModel
            {
                TestId = 1,
                TestName = " Leak test : SKIPPED ",
                TestStatus = 1,
                TestOrder = 2
            },
            new TestModel
            {
                TestId = 1,
                TestName = "Leak test",
                TestStatus = 4,
                TestOrder = 2,
                TestTime = 12,
                TestType = "Flow",
                TestResponseTime = 33
            }
        ];

        var second = TestData.CreateResult("two", "PN-1");
        second.Tests =
        [
            new TestModel
            {
                TestId = 2,
                TestName = "Second test",
                TestStatus = 1,
                TestOrder = 1
            }
        ];

        db.TestReports.AddRange(ToReport(first), ToReport(second));
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        ControllerTestHelper.AttachHttpContext(controller);

        var result = await controller.Index(["one", "two"]);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<GeneratedModel>(view.Model);
        Assert.Equal(2, model.Injectors.Count);
        Assert.Equal(new[] { "Second test", "Leak test" }, model.AllTests.Select(x => x.TestName));
        Assert.Equal(4, model.AllTests.Single(x => x.TestName == "Leak test").TestStatus);

        var normalizedLeak = model.Injectors[0].NormalizedTests.Single(x => x.TestName == "Leak test");
        Assert.False(normalizedLeak.IsSkipped);
        Assert.Equal(4, normalizedLeak.Status);
        Assert.Equal(12, normalizedLeak.Time);
        Assert.Equal("33", normalizedLeak.Response);
    }

    [Fact]
    public async Task GeneratePdf_NoSelection_RedirectsToHome()
    {
        await using var db = TestData.CreateInMemoryContext();
        var controller = CreateController(db);
        ControllerTestHelper.AttachHttpContext(controller);

        var result = await controller.GeneratePdf([]);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task GeneratePdf_ValidSelection_ReturnsPdfFile()
    {
        await using var db = TestData.CreateInMemoryContext();
        db.TestReports.Add(TestData.CreateReport("one"));
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        ControllerTestHelper.AttachHttpContext(controller);

        var result = await controller.GeneratePdf(["one"]);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.StartsWith("Analyse-", file.FileDownloadName);
        Assert.EndsWith(".pdf", file.FileDownloadName);
        Assert.True(file.FileContents.Length > 4);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(file.FileContents, 0, 4));
    }

    private static AnalysisController CreateController(TestFusion.Data.AppDbContext db)
        => new(db, new TestStringLocalizer(), new TimeZoneService(), new TestSettingsService());

    private static TestFusion.Core.Models.TestReportModel ToReport(TestResultModel result)
    {
        var service = new JSONService(new TimeZoneService());
        return service.ConvertToTestReportModel(result);
    }
}
