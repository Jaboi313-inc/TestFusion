using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using TestFusion.Core.Interfaces;
using TestFusion.Core.Models;
using TestFusion.Data;
using TestFusion.Services.Services;

public class SyncService : ISyncService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SyncService> _logger;
    private readonly JSONService _jsonService;

    public SyncService(
        IServiceScopeFactory scopeFactory,
        ILogger<SyncService> logger,
        JSONService jsonService)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _jsonService = jsonService;
    }

    public async Task RunSync(CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Sync started");

            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var playwright = scope.ServiceProvider.GetRequiredService<IPlaywright>();

            var allIds = await playwright.GetAllIDs();

            _logger.LogInformation(
                "Found {Count} reports at source",
                allIds.Count);

            var existingIds = await db.TestReports
                .Select(x => x.Id)
                .ToListAsync(ct);

            var existingIdSet = existingIds.ToHashSet();

            var newIds = allIds
                .Where(id => !existingIdSet.Contains(id))
                .ToList();

            var limitedIds = newIds
                // Limit the amount of reports fetched per sync
                .Take(5)
                .ToList();

            _logger.LogInformation(
                "Found {NewCount} new reports, fetching {FetchCount}",
                newIds.Count,
                limitedIds.Count);

            if (limitedIds.Count == 0)
            {
                stopwatch.Stop();

                _logger.LogInformation(
                    "Sync completed, no new reports found in {ElapsedMilliseconds} ms",
                    stopwatch.ElapsedMilliseconds);

                return;
            }

            var jsonResults = await Task.WhenAll(
                limitedIds.Select(id => playwright.GetDataForId(id)));

            var testReports = new List<TestReportModel>();

            foreach (var json in jsonResults)
            {
                var testResultModel =
                    _jsonService.ConvertToTestResultModel(json);

                var testReport =
                    _jsonService.ConvertToTestReportModel(testResultModel);

                testReports.Add(testReport);

                _logger.LogInformation(
                    "Processed report {ReportId}",
                    testReport.Id);
            }

            db.TestReports.AddRange(testReports);

            await db.SaveChangesAsync(ct);

            stopwatch.Stop();

            _logger.LogInformation(
                "Sync completed, {Count} reports stored in {ElapsedMilliseconds} ms",
                testReports.Count,
                stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            stopwatch.Stop();

            _logger.LogWarning(
                "Sync cancelled after {ElapsedMilliseconds} ms",
                stopwatch.ElapsedMilliseconds);

            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Sync failed after {ElapsedMilliseconds} ms",
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}