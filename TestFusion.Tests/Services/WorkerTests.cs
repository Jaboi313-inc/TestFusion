using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TestFusion.Core.Interfaces;
using TestFusion.Services.Models;
using TestFusion.Tests.Helpers;

namespace TestFusion.Tests.Services;

public class WorkerTests
{
    [Fact]
    public async Task Worker_StartsSyncLoop()
    {
        var sync = new FakeSyncService();
        using var provider = BuildProvider(sync);
        var worker = CreateWorker(provider, TimeSpan.FromMilliseconds(25));

        await worker.StartAsync(CancellationToken.None);
        await sync.FirstCall.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await worker.StopAsync(CancellationToken.None);

        Assert.True(sync.CallCount >= 1);
    }

    [Fact]
    public async Task Worker_WhenSyncFails_RetriesNextCycle()
    {
        var sync = new FakeSyncService { FailuresBeforeSuccess = 1 };
        using var provider = BuildProvider(sync);
        var worker = CreateWorker(provider, TimeSpan.FromMilliseconds(20));

        await worker.StartAsync(CancellationToken.None);
        await sync.SecondCall.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await worker.StopAsync(CancellationToken.None);

        Assert.True(sync.CallCount >= 2);
    }

    private static ServiceProvider BuildProvider(FakeSyncService sync)
    {
        var services = new ServiceCollection();
        services.AddScoped<ISyncService>(_ => sync);
        return services.BuildServiceProvider();
    }

    private static Worker CreateWorker(ServiceProvider provider, TimeSpan interval)
    {
        return new Worker(
            NullLogger<Worker>.Instance,
            Options.Create(new Intervals
            {
                DatafetchingInterval = interval,
                WorkerHeartbeatInterval = interval
            }),
            provider.GetRequiredService<IServiceScopeFactory>());
    }
}
