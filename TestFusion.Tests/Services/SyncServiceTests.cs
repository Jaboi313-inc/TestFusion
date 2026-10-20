using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TestFusion.Core.Interfaces;
using TestFusion.Data;
using TestFusion.Services.Services;
using TestFusion.Tests.Helpers;

namespace TestFusion.Tests.Services;

public class SyncServiceTests
{
    [Fact]
    public async Task RunSync_NoNewIds_DoesNotFetchReports()
    {
        var fakePlaywright = new FakePlaywright { Ids = ["existing-1"] };
        using var root = BuildProvider(fakePlaywright);

        await using (var seedScope = root.CreateAsyncScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.TestReports.Add(TestData.CreateReport("existing-1"));
            await db.SaveChangesAsync();
        }

        var service = CreateService(root);

        await service.RunSync();

        Assert.Empty(fakePlaywright.RequestedIds);

        await using var assertScope = root.CreateAsyncScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Single(assertDb.TestReports);
    }

    [Fact]
    public async Task RunSync_OnlyFetchesFirstFiveNewReports()
    {
        var ids = Enumerable.Range(1, 8).Select(x => $"new-{x}").ToList();
        var fakePlaywright = new FakePlaywright { Ids = ids };

        foreach (var id in ids)
            fakePlaywright.JsonById[id] = TestData.CreateSourceJson(id);

        using var root = BuildProvider(fakePlaywright);
        var service = CreateService(root);

        await service.RunSync();

        Assert.Equal(ids.Take(5), fakePlaywright.RequestedIds);

        await using var scope = root.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(5, db.TestReports.Count());
        Assert.All(db.TestReports, report => Assert.StartsWith("new-", report.Id));
    }

    [Fact]
    public async Task RunSync_IgnoresIdsAlreadyStored()
    {
        var fakePlaywright = new FakePlaywright { Ids = ["existing", "new"] };
        fakePlaywright.JsonById["new"] = TestData.CreateSourceJson("new");

        using var root = BuildProvider(fakePlaywright);

        await using (var seedScope = root.CreateAsyncScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.TestReports.Add(TestData.CreateReport("existing"));
            await db.SaveChangesAsync();
        }

        var service = CreateService(root);
        await service.RunSync();

        Assert.Equal(new[] { "new" }, fakePlaywright.RequestedIds);

        await using var assertScope = root.CreateAsyncScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(2, assertDb.TestReports.Count());
    }

    [Fact]
    public async Task RunSync_PlaywrightFailure_IsRethrown()
    {
        var expected = new InvalidOperationException("Source failed");
        var fakePlaywright = new FakePlaywright { GetAllIdsException = expected };
        using var root = BuildProvider(fakePlaywright);
        var service = CreateService(root);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RunSync());

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task RunSync_CancelledToken_ThrowsOperationCanceledException()
    {
        var fakePlaywright = new FakePlaywright { Ids = ["new"] };
        using var root = BuildProvider(fakePlaywright);
        var service = CreateService(root);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.RunSync(cts.Token));
    }

    private static ServiceProvider BuildProvider(FakePlaywright fakePlaywright)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var services = new ServiceCollection();

        services.AddSingleton(connection);
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
        services.AddScoped<IPlaywright>(_ => fakePlaywright);

        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        return provider;
    }

    private static SyncService CreateService(ServiceProvider provider)
    {
        return new SyncService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<SyncService>.Instance,
            new JSONService(new TimeZoneService()));
    }
}
