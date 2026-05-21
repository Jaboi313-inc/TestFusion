using Microsoft.Extensions.Options;
using TestFusion.Core.Interfaces;
using TestFusion.SyncService.Models;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly Intervals _intervals;
    private readonly IServiceScopeFactory _scopeFactory;

    public Worker(
        ILogger<Worker> logger,
        IOptions<Intervals> intervals,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _intervals = intervals.Value;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var workerLoop = RunWorkerLoop(stoppingToken);
        var dataLoop = RunDataFetchingLoop(stoppingToken);

        await Task.WhenAll(workerLoop, dataLoop);
    }

    private async Task RunWorkerLoop(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            await Task.Delay(_intervals.WorkerHeartbeatInterval, stoppingToken);
        }
    }

    private async Task RunDataFetchingLoop(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();

            var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();

            await syncService.RunSync(stoppingToken);

            await Task.Delay(_intervals.DatafetchingInterval, stoppingToken);
        }
    }
}