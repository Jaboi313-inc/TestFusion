using Microsoft.AspNetCore.Mvc;
using TestFusion.Web.Controllers;
using TestFusion.Tests.Helpers;

namespace TestFusion.Tests.Web;

public class HomeControllerTests
{
    [Fact]
    public async Task Index_WithoutParameters_UsesDefaultLimitAndSortsNewestFirst()
    {
        await using var db = TestData.CreateInMemoryContext();
        db.TestReports.AddRange(
            Enumerable.Range(1, 60)
                .Select(i => TestData.CreateReport(
                    $"report-{i}",
                    time: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMinutes(i))));
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        ControllerTestHelper.AttachHttpContext(controller);

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<TestFusion.Core.Models.TestReportModel>>(view.Model);
        Assert.Equal(50, model.Count);
        Assert.Equal("report-60", model[0].Id);
        Assert.Equal("report-11", model[^1].Id);
        Assert.Equal(50, controller.ViewBag.ResultLimit);
        Assert.Equal(60, controller.ViewBag.TotalCount);
        Assert.False(controller.ViewBag.HasActiveFilters);
    }

    [Fact]
    public async Task Index_ValidResultLimit_StoresCookieAndAppliesLimit()
    {
        await using var db = TestData.CreateInMemoryContext();
        db.TestReports.AddRange(Enumerable.Range(1, 40).Select(i => TestData.CreateReport($"report-{i}")));
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var httpContext = ControllerTestHelper.AttachHttpContext(controller, isHttps: true);

        var result = await controller.Index(resultLimit: 25);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<TestFusion.Core.Models.TestReportModel>>(view.Model);
        Assert.Equal(25, model.Count);
        Assert.Equal(25, controller.ViewBag.ResultLimit);
        Assert.Contains("TestFusion.ResultLimit=25", httpContext.Response.Headers["Set-Cookie"].ToString());
        Assert.Contains("secure", httpContext.Response.Headers["Set-Cookie"].ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Index_ResultLimitZero_ReturnsAllRows()
    {
        await using var db = TestData.CreateInMemoryContext();
        db.TestReports.AddRange(Enumerable.Range(1, 70).Select(i => TestData.CreateReport($"report-{i}")));
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        ControllerTestHelper.AttachHttpContext(controller);

        var result = await controller.Index(resultLimit: 0);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<TestFusion.Core.Models.TestReportModel>>(view.Model);
        Assert.Equal(70, model.Count);
        Assert.Equal(0, controller.ViewBag.ResultLimit);
    }

    [Fact]
    public async Task Index_InvalidResultLimit_UsesValidSavedCookie()
    {
        await using var db = TestData.CreateInMemoryContext();
        db.TestReports.AddRange(Enumerable.Range(1, 80).Select(i => TestData.CreateReport($"report-{i}")));
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        ControllerTestHelper.AttachHttpContext(controller, "TestFusion.ResultLimit=25");

        var result = await controller.Index(resultLimit: 999);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<TestFusion.Core.Models.TestReportModel>>(view.Model);
        Assert.Equal(25, model.Count);
        Assert.Equal(25, controller.ViewBag.ResultLimit);
    }

    [Fact]
    public async Task Index_InvalidResultLimitAndInvalidCookie_UsesDefaultLimit()
    {
        await using var db = TestData.CreateInMemoryContext();
        db.TestReports.AddRange(Enumerable.Range(1, 80).Select(i => TestData.CreateReport($"report-{i}")));
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        ControllerTestHelper.AttachHttpContext(controller, "TestFusion.ResultLimit=999");

        var result = await controller.Index(resultLimit: 999);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<TestFusion.Core.Models.TestReportModel>>(view.Model);
        Assert.Equal(50, model.Count);
        Assert.Equal(50, controller.ViewBag.ResultLimit);
    }

    [Fact]
    public async Task Index_DateFromFilter_ReturnsAllMatchingRowsIgnoringLimit()
    {
        await using var db = TestData.CreateInMemoryContext();
        var start = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

        db.TestReports.AddRange(
            Enumerable.Range(0, 70)
                .Select(i => TestData.CreateReport($"report-{i}", time: start.AddDays(i))));
        await db.SaveChangesAsync();

        var timeZone = new PassthroughTimeZoneService();
        var controller = CreateController(db, timeZone: timeZone);
        ControllerTestHelper.AttachHttpContext(controller);

        var result = await controller.Index(dateFrom: new DateTime(2026, 10, 10));

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<TestFusion.Core.Models.TestReportModel>>(view.Model);
        Assert.Equal(61, model.Count);
        Assert.True(controller.ViewBag.HasActiveFilters);
        Assert.Equal(new DateTime(2026, 10, 10), timeZone.LastUserDateTime);
    }

    [Fact]
    public async Task Index_DateToFilter_IsInclusiveForSelectedDay()
    {
        await using var db = TestData.CreateInMemoryContext();
        db.TestReports.AddRange(
            TestData.CreateReport("before", time: new DateTimeOffset(2026, 10, 17, 23, 59, 0, TimeSpan.Zero)),
            TestData.CreateReport("same-day", time: new DateTimeOffset(2026, 10, 18, 23, 59, 0, TimeSpan.Zero)),
            TestData.CreateReport("next-day", time: new DateTimeOffset(2026, 10, 19, 0, 0, 0, TimeSpan.Zero)));
        await db.SaveChangesAsync();

        var controller = CreateController(db, timeZone: new PassthroughTimeZoneService());
        ControllerTestHelper.AttachHttpContext(controller);

        var result = await controller.Index(dateTo: new DateTime(2026, 10, 18));

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<TestFusion.Core.Models.TestReportModel>>(view.Model);
        Assert.Equal(2, model.Count);
        Assert.DoesNotContain(model, x => x.Id == "next-day");
    }

    [Fact]
    public async Task Refresh_RunsSyncAndRedirectsToIndex()
    {
        await using var db = TestData.CreateInMemoryContext();
        var sync = new FakeSyncService();
        var controller = CreateController(db, sync);
        ControllerTestHelper.AttachHttpContext(controller);

        var result = await controller.Refresh();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(HomeController.Index), redirect.ActionName);
        Assert.Equal(1, sync.CallCount);
    }

    private static HomeController CreateController(
        TestFusion.Data.AppDbContext db,
        FakeSyncService? sync = null,
        TestFusion.Core.Interfaces.ITimeZoneService? timeZone = null)
    {
        return new HomeController(
            db,
            sync ?? new FakeSyncService(),
            timeZone ?? new PassthroughTimeZoneService());
    }
}
