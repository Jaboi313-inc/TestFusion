using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Localization;
using System.Globalization;
using TestFusion.Core;
using TestFusion.Core.Interfaces;

namespace TestFusion.Tests.Helpers;

internal sealed class TestStringLocalizer : IStringLocalizer<SharedResource>
{
    private static readonly Dictionary<string, string> Values = new()
    {
        ["SymbolDot"] = ".",
        ["SymbolQuestionmark"] = "?",
        ["DebugNoRowsSelected"] = "No rows selected",
        ["DebugSelectedRowsDeleted"] = "{0} selected TestReports deleted",
        ["DebugAllRowsDeleted"] = "All TestReports deleted{1} {0} rows deleted",
        ["DebugConfirmationRequired"] = "Confirmation is required",
        ["DebugIdRequired"] = "ID is required",
        ["DebugStoredJsonNotFound"] = "No stored JSON was found for ID '{0}'",
        ["ErrorDiffirentInjectorPartNumber"] = "Different part number"
    };

    public LocalizedString this[string name]
    {
        get
        {
            var value = Values.TryGetValue(name, out var localized) ? localized : name;
            return new LocalizedString(name, value, resourceNotFound: false);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var value = this[name].Value;
            return new LocalizedString(
                name,
                string.Format(CultureInfo.InvariantCulture, value, arguments),
                resourceNotFound: false);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        return Values.Select(x => new LocalizedString(x.Key, x.Value, resourceNotFound: false));
    }
}

internal sealed class FakePlaywright : IPlaywright
{
    public List<string> Ids { get; set; } = [];
    public Dictionary<string, string> JsonById { get; } = [];
    public List<string> RequestedIds { get; } = [];
    public Exception? GetAllIdsException { get; set; }
    public Exception? GetDataException { get; set; }

    public Task<List<string>> GetAllIDs()
    {
        if (GetAllIdsException != null)
            throw GetAllIdsException;

        return Task.FromResult(Ids.ToList());
    }

    public Task<string> GetDataForId(string id)
    {
        RequestedIds.Add(id);

        if (GetDataException != null)
            throw GetDataException;

        if (!JsonById.TryGetValue(id, out var json))
            throw new KeyNotFoundException($"No JSON configured for '{id}'.");

        return Task.FromResult(json);
    }
}

internal sealed class FakeSyncService : ISyncService
{
    public int CallCount { get; private set; }
    public int FailuresBeforeSuccess { get; set; }
    public TaskCompletionSource<bool> FirstCall { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource<bool> SecondCall { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task RunSync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        CallCount++;

        if (CallCount >= 1)
            FirstCall.TrySetResult(true);

        if (CallCount >= 2)
            SecondCall.TrySetResult(true);

        if (CallCount <= FailuresBeforeSuccess)
            throw new InvalidOperationException("Sync failed for test.");

        return Task.CompletedTask;
    }
}

internal sealed class PassthroughTimeZoneService : ITimeZoneService
{
    public string SourceTimeZoneId => "UTC";
    public string UserTimeZoneId => "UTC";
    public DateTime? LastUserDateTime { get; private set; }

    public DateTimeOffset ConvertSourceToUtc(DateTime sourceDateTime)
        => new(DateTime.SpecifyKind(sourceDateTime, DateTimeKind.Utc));

    public DateTimeOffset ParseSourceToUtc(string sourceDateTime)
        => DateTimeOffset.Parse(sourceDateTime, CultureInfo.InvariantCulture);

    public DateTimeOffset ConvertUtcToUser(DateTimeOffset utcDateTime)
        => utcDateTime.ToUniversalTime();

    public DateTimeOffset ConvertUserToUtc(DateTime userDateTime)
    {
        LastUserDateTime = userDateTime;
        return new DateTimeOffset(DateTime.SpecifyKind(userDateTime, DateTimeKind.Utc));
    }

    public string FormatForUser(DateTimeOffset utcDateTime, string format = "dd-MM-yyyy HH:mm")
        => ConvertUtcToUser(utcDateTime).ToString(format, CultureInfo.InvariantCulture);
}

internal sealed class MemoryTempDataProvider : ITempDataProvider
{
    private Dictionary<string, object> _data = [];

    public IDictionary<string, object> LoadTempData(HttpContext context)
        => new Dictionary<string, object>(_data);

    public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        => _data = new Dictionary<string, object>(values);
}

internal sealed class TestWebHostEnvironment : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "TestFusion.Tests";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; } = string.Empty;
    public string EnvironmentName { get; set; } = "Development";
    public string ContentRootPath { get; set; } = string.Empty;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

internal static class ControllerTestHelper
{
    public static DefaultHttpContext AttachHttpContext(Controller controller, string? cookie = null, bool isHttps = false)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = isHttps ? "https" : "http";

        if (!string.IsNullOrWhiteSpace(cookie))
            httpContext.Request.Headers.Cookie = cookie;

        var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor());

        controller.ControllerContext = new ControllerContext(actionContext);
        controller.TempData = new TempDataDictionary(httpContext, new MemoryTempDataProvider());
        controller.Url = new UrlHelper(actionContext);

        return httpContext;
    }
}
