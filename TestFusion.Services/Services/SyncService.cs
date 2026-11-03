using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TestFusion.Core.Interfaces;
using TestFusion.Core.Models;
using TestFusion.Data;
using TestFusion.Services.Models;

namespace TestFusion.Services.Services;

public class SyncService : ISyncService {
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SyncService> _logger;
    private readonly JSONService _jsonService;
    private readonly LoggingSettings _loggingSettings;

    public SyncService(IServiceScopeFactory scopeFactory, ILogger<SyncService> logger, JSONService jsonService, IOptions<LoggingSettings> loggingSettings) {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _jsonService = jsonService;
        _loggingSettings = loggingSettings.Value;
    }

    public async Task RunSync(CancellationToken ct = default) {
        var stopwatch = Stopwatch.StartNew();

        try {
            _logger.LogInformation("Synchronization started");
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var playwright = scope.ServiceProvider.GetRequiredService<IPlaywright>();

            var allIds = await playwright.GetAllIDs();
            var existingIds = await db.TestReports.Select(x => x.Id).ToListAsync(ct);
            var existingIdSet = existingIds.ToHashSet();
            var newIds = allIds.Where(id => !existingIdSet.Contains(id)).ToList();
            var limitedIds = newIds.Take(5).ToList();

            _logger.LogInformation("Synchronization found {SourceCount} reports at the source, {ExistingCount} already stored and {NewCount} new reports", allIds.Count, existingIds.Count, newIds.Count);

            if (limitedIds.Count == 0) {
                stopwatch.Stop();
                LogCompletion(0, stopwatch.ElapsedMilliseconds);
                return;
            }

            _logger.LogDebug("Fetching {FetchCount} reports: {ReportIds}", limitedIds.Count, string.Join(", ", limitedIds));
            var jsonResults = await Task.WhenAll(limitedIds.Select(id => playwright.GetDataForId(id)));
            var testReports = new List<TestReportModel>();

            foreach (var json in jsonResults) {
                var testResultModel = _jsonService.ConvertToTestResultModel(json);
                var testReport = _jsonService.ConvertToTestReportModel(testResultModel);
                testReports.Add(testReport);
                _logger.LogDebug("Processed report {ReportId} with part number {PartNumber}", testReport.Id, testReport.PartNumber);
            }

            db.TestReports.AddRange(testReports);
            await db.SaveChangesAsync(ct);
            stopwatch.Stop();
            LogCompletion(testReports.Count, stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) {
            stopwatch.Stop();
            using var scope = _logger.BeginScope(new Dictionary<string, object> { ["DurationMs"] = stopwatch.ElapsedMilliseconds });
            _logger.LogWarning("Synchronization cancelled");
            throw;
        }
        catch (Exception ex) {
            stopwatch.Stop();
            using var scope = _logger.BeginScope(new Dictionary<string, object> { ["DurationMs"] = stopwatch.ElapsedMilliseconds });
            _logger.LogError(ex, "Synchronization failed");
            throw;
        }
    }

    private void LogCompletion(int storedCount, long durationMs) {
        using var scope = _logger.BeginScope(new Dictionary<string, object> { ["DurationMs"] = durationMs });
        if (durationMs >= _loggingSettings.SlowOperationThresholds.SynchronizationMs) _logger.LogWarning("Synchronization completed with {Count} reports stored, but took longer than expected", storedCount);
        else _logger.LogInformation("Synchronization completed with {Count} reports stored", storedCount);
    }
}
