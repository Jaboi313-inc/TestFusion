using Microsoft.Extensions.Options;
using TestFusion.Core.Interfaces;
using TestFusion.SyncService.Models;

namespace TestFusion.SyncService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly Intervals _intervals;
        private readonly IPlaywrightInterface _playwrightService;

        public Worker(
            ILogger<Worker> logger,
            IOptions<Intervals> intervals,
            IPlaywrightInterface playwrightService)
        {
            _logger = logger;
            _intervals = intervals.Value;
            _playwrightService = playwrightService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var workerLoop = RunWorkerLoop(stoppingToken);

            await workerLoop;
        }

        private async Task RunWorkerLoop(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                await Task.Delay(_intervals.WorkerHeartbeatInterval, stoppingToken);
            }
        }

        private async Task RunPlaywrightLoop(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting PlaywrightService");

                    await _playwrightService.GetAllIDs();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while running PlaywrightService");
                }

                _logger.LogInformation("PlaywrightService done");

                await Task.Delay(_intervals.PlaywrightInterval, stoppingToken);
            }
        }
    }
}