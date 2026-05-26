using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TestFusion.Core.Interfaces;
using TestFusion.Core.Models;
using TestFusion.Data;
using TestFusion.SyncService.Services;

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
        try
        {
            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var playwright = scope.ServiceProvider.GetRequiredService<IPlaywright>();

            _logger.LogInformation("STATUS: sync started");

            var allIds = await playwright.GetAllIDs();

            var existingIds = await db.TestItems
                .Select(x => x.Id)
                .ToListAsync(ct);

            var newIds = allIds
                .Where(id => !existingIds.Contains(id));

            var limitedIds = newIds
                // Limit to # items to fetch, to avoid overloading the system
                // Recommend 5-10
                .Take(5)
                .ToList();

            _logger.LogInformation("STATUS: New items to fetch: {count}", newIds.ToList().Count);
            _logger.LogInformation("STATUS: New items actually going to be fetched: {count}", limitedIds.Count);

            var jsonResults = await Task.WhenAll(
                limitedIds.Select(id => playwright.GetDataForId(id))
            );

            var listItems = new List<TestListItemModel>();
            var storedJsons = new List<StoredJsonModel>();

            foreach (var json in jsonResults)
            {
                var testResultModel = _jsonService.ConvertToTestResultModel(json);

                listItems.Add(_jsonService.ConvertToTestListModel(testResultModel));

                storedJsons.Add(new StoredJsonModel
                {
                    Id = testResultModel.Id,
                    Json = _jsonService.ConvertToJson(testResultModel)
                });
            }

            db.TestItems.AddRange(listItems);
            db.StoredJsons.AddRange(storedJsons);

            await db.SaveChangesAsync(ct);

            _logger.LogInformation("SUCCESS: sync done");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERROR: sync failed");
        }
    }
}