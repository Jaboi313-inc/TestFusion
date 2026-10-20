using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using TestFusion.Services;
using TestFusion.Services.Models;

namespace TestFusion.Tests.Services;

public class PlaywrightServiceTests
{
    [Fact]
    public async Task DisposeAsync_WhenBrowserWasNeverStarted_DoesNotThrow()
    {
        var service = CreateEmptyService();

        var exception = await Record.ExceptionAsync(async () => await service.DisposeAsync());

        Assert.Null(exception);
    }

    [Fact(Skip = "Integration test: requires Microsoft Edge, network access and valid credentials.")]
    public async Task GetAllIDs_Live_ReturnsIds()
    {
        await using var service = CreateLiveService();

        var ids = await service.GetAllIDs();

        Assert.NotEmpty(ids);
        Assert.All(ids, id => Assert.False(string.IsNullOrWhiteSpace(id)));
    }

    [Fact(Skip = "Integration test: requires Microsoft Edge, network access and valid credentials.")]
    public async Task GetDataForId_Live_ReturnsJsonForRequestedReport()
    {
        await using var service = CreateLiveService();

        var ids = await service.GetAllIDs();
        Assert.NotEmpty(ids);

        var id = ids.First();
        var json = await service.GetDataForId(id);

        using var document = JsonDocument.Parse(json);

        Assert.Equal(id, document.RootElement.GetProperty("_id").GetString());
    }

    [Fact]
    public async Task InitializeBrowser_ThenDisposeAsync_ClosesEdgeProcesses()
    {
        var processesBefore = GetEdgeProcessIds();

        var service = new PlaywrightService(
            NullLogger<PlaywrightService>.Instance,
            Options.Create(new SiteSettings()),
            Options.Create(new AuthSettings()));

        var initializeBrowser = typeof(PlaywrightService).GetMethod(
            "InitializeBrowser",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(initializeBrowser);

        var initializeTask = (Task)initializeBrowser.Invoke(service, null)!;
        await initializeTask;

        await Task.Delay(500);

        var startedProcesses = GetEdgeProcessIds()
            .Except(processesBefore)
            .ToList();

        Assert.NotEmpty(startedProcesses);

        await service.DisposeAsync();

        var timeout = DateTime.UtcNow.AddSeconds(10);

        while (DateTime.UtcNow < timeout && startedProcesses.Any(IsProcessRunning))
            await Task.Delay(250);

        Assert.All(startedProcesses, processId =>
            Assert.False(IsProcessRunning(processId), $"Edge process {processId} was not closed."));
    }

    private static HashSet<int> GetEdgeProcessIds()
    {
        return Process.GetProcessesByName("msedge")
            .Select(process => process.Id)
            .ToHashSet();
    }

    private static bool IsProcessRunning(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static PlaywrightService CreateEmptyService()
    {
        return new PlaywrightService(
            NullLogger<PlaywrightService>.Instance,
            Options.Create(new SiteSettings()),
            Options.Create(new AuthSettings()));
    }

    private static PlaywrightService CreateLiveService()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var siteSettings = configuration.GetSection("SiteSettings").Get<SiteSettings>()
            ?? throw new InvalidOperationException("SiteSettings could not be loaded.");

        var authSettings = configuration.GetSection("AuthSettings").Get<AuthSettings>()
            ?? new AuthSettings();

        if (string.IsNullOrWhiteSpace(siteSettings.BaseUrl))
            throw new InvalidOperationException("SiteSettings:BaseUrl is empty.");

        if (string.IsNullOrWhiteSpace(siteSettings.ReportUrl))
            throw new InvalidOperationException("SiteSettings:ReportUrl is empty.");

        if (string.IsNullOrWhiteSpace(authSettings.Email))
            throw new InvalidOperationException("AuthSettings:Email environment variable is empty.");

        if (string.IsNullOrWhiteSpace(authSettings.Password))
            throw new InvalidOperationException("AuthSettings:Password environment variable is empty.");

        return new PlaywrightService(
            NullLogger<PlaywrightService>.Instance,
            Options.Create(siteSettings),
            Options.Create(authSettings));
    }
}