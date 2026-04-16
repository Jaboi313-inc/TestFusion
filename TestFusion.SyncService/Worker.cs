using Microsoft.Extensions.Options;

namespace TestFusion.SyncService
{
    public class Worker(
        ILogger<Worker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var workerLoop = RunWorkerLoop(stoppingToken);
            var playwrightLoop = RunPlaywrightLoop(stoppingToken);

            await Task.WhenAll(workerLoop, playwrightLoop);
        }

        private async Task RunWorkerLoop(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task RunPlaywrightLoop(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromMinutes(5);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    logger.LogInformation("Starting PlaywrightService");

                    await PlaywrightService.Run();

                    logger.LogInformation("PlaywrightService done");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while running PlaywrightService");
                }

                await Task.Delay(interval, stoppingToken);
            }
        }
    }
}