using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TestFusion.Core.Interfaces;
using TestFusion.Data;
using TestFusion.SyncService.Models;

namespace TestFusion.SyncService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly Intervals _intervals;
        private readonly IPlaywrightInterface _playwrightService;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public Worker(
            ILogger<Worker> logger,
            IOptions<Intervals> intervals,
            IPlaywrightInterface playwrightService,
            IDbContextFactory<AppDbContext> dbFactory)
        {
            _logger = logger;
            _intervals = intervals.Value;
            _playwrightService = playwrightService;
            _dbFactory = dbFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var workerLoop = RunWorkerLoop(stoppingToken);
            var testloop = GetDataAndSaveToDb(stoppingToken);

            await Task.WhenAll(workerLoop, testloop);
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

        private async Task GetDataAndSaveToDb(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("SYNC START");

                var ids = (await _playwrightService.GetAllIDs())
                    .Take(5)
                    .ToList();

                await using var db = await _dbFactory.CreateDbContextAsync();

                foreach (var id in ids)
                {
                    try
                    {
                        var item = await _playwrightService.GetDataForId(id);

                        var existing = await db.TestItems
                            .FirstOrDefaultAsync(x => x.Id == item.Id);

                        if (existing == null)
                        {
                            db.TestItems.Add(item);
                        }
                        else
                        {
                            db.Entry(existing)
                                .CurrentValues
                                .SetValues(item);
                        }

                        // Delay to reduce captcha
                        await Task.Delay(500, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing ID {Id}", id);
                    }
                }

                await db.SaveChangesAsync();

                _logger.LogInformation("SYNC DONE");

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}