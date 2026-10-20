using Microsoft.Extensions.Options;
using TestFusion.Core.Interfaces;
using TestFusion.Services.Models;

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
        _logger.LogInformation("Worker started");

        try
        {
            var workerLoop = RunWorkerLoop(stoppingToken);
            var dataLoop = RunDataFetchingLoop(stoppingToken);

            await Task.WhenAll(workerLoop, dataLoop);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown
        }
        finally
        {
            _logger.LogInformation("Worker stopped");
        }
    }


    private async Task RunWorkerLoop(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("Worker heartbeat");

            await Task.Delay(
                _intervals.WorkerHeartbeatInterval,
                stoppingToken);
        }
    }


    private async Task RunDataFetchingLoop(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();
                await syncService.RunSync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception)
            {
                _logger.LogWarning(
                    "Sync cycle failed. Worker will retry after {Interval}",
                    _intervals.DatafetchingInterval);
            }

            await Task.Delay(_intervals.DatafetchingInterval, stoppingToken);
        }
    }
}