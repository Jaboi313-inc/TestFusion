using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using TestFusion.Core;
using TestFusion.Core.Interfaces;
using TestFusion.Data;
using TestFusion.Services.Services;
using TestFusion.Web.Models;

namespace TestFusion.Web.Controllers;

public class DebugController : Controller
{
    private readonly AppDbContext _db;
    private readonly IPlaywright _playwrightService;
    private readonly JSONService _jsonService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public DebugController(
        AppDbContext db,
        IPlaywright playwrightService,
        JSONService jsonService,
        IStringLocalizer<SharedResource> localizer)
    {
        _db = db;
        _playwrightService = playwrightService;
        _jsonService = jsonService;
        _localizer = localizer;
    }


    // Page

    [HttpGet]
    public async Task<IActionResult> Index(string? id)
    {
        var testReports = await _db.TestReports
            .AsNoTracking()
            .OrderByDescending(x => x.TimeOffTesting)
            .ToListAsync();

        var viewModel = new DebugViewModel
        {
            TestReports = testReports,
            JsonId = id?.Trim() ?? string.Empty
        };

        if (string.IsNullOrWhiteSpace(viewModel.JsonId))
        {
            return View(viewModel);
        }

        var storedReport = testReports.FirstOrDefault(x => x.Id == viewModel.JsonId);

        if (storedReport != null)
        {
            viewModel.StoredJsonFound = true;
            viewModel.StoredJson = FormatJson(storedReport.Json);
        }

        try
        {
            var rawJson = await _playwrightService.GetDataForId(viewModel.JsonId);
            var processedModel = _jsonService.ConvertToTestResultModel(rawJson);

            var processedJson = _jsonService.ConvertToJson(
                processedModel,
                prettyJson: true,
                useUnicodeSymbols: false);

            viewModel.RawJson = FormatJson(rawJson);
            viewModel.ProcessedJson = FormatJson(processedJson);

            if (storedReport != null)
            {
                viewModel.ProcessedMatchesStored =
                    JsonEquals(processedJson, storedReport.Json);
            }
        }
        catch (Exception ex)
        {
            viewModel.JsonError = ex.Message;
        }

        return View(viewModel);
    }


    // Database

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSelectedTestReports(List<string> selectedIds)
    {
        if (selectedIds.Count == 0)
        {
            TempData["DebugMessage"] =
            _localizer["DebugNoRowsSelected"].Value +
            _localizer["SymbolDot"].Value;
            return RedirectToAction(nameof(Index));
        }

        var deletedCount = await _db.TestReports
            .Where(x => selectedIds.Contains(x.Id))
            .ExecuteDeleteAsync();

        TempData["DebugMessage"] =
            _localizer["DebugSelectedRowsDeleted", deletedCount].Value +
            _localizer["SymbolDot"].Value;

        return RedirectToAction(nameof(Index));
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearTestReports(bool confirmed)
    {
        if (!confirmed)
        {
            return BadRequest(
                _localizer["DebugConfirmationRequired"].Value +
                _localizer["SymbolDot"].Value);
        }

        var deletedCount = await _db.TestReports.ExecuteDeleteAsync();

        TempData["DebugMessage"] =
            _localizer["DebugAllRowsDeleted", deletedCount, _localizer["SymbolDot"].Value].Value +
            _localizer["SymbolDot"].Value;

        return RedirectToAction(nameof(Index));
    }


    // JSON downloads

    [HttpGet]
    public async Task<IActionResult> DownloadRawJson(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(
                _localizer["DebugIdRequired"].Value +
                _localizer["SymbolDot"].Value);
        }

        var rawJson = await _playwrightService.GetDataForId(id);
        var bytes = Encoding.UTF8.GetBytes(rawJson);

        return File(bytes, "application/json", $"raw_{id}.json");
    }


    [HttpGet]
    public async Task<IActionResult> DownloadProcessedJson(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(
                _localizer["DebugIdRequired"].Value +
                _localizer["SymbolDot"].Value);
        }

        var rawJson = await _playwrightService.GetDataForId(id);
        var model = _jsonService.ConvertToTestResultModel(rawJson);

        var processedJson = _jsonService.ConvertToJson(
            model,
            prettyJson: true,
            useUnicodeSymbols: false);

        var bytes = Encoding.UTF8.GetBytes(processedJson);

        return File(bytes, "application/json", $"processed_{id}.json");
    }


    [HttpGet]
    public async Task<IActionResult> DownloadStoredJson(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(
                _localizer["DebugIdRequired"].Value +
                _localizer["SymbolDot"].Value);
        }

        var testReport = await _db.TestReports
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (testReport == null)
        {
            return NotFound(
                _localizer["DebugStoredJsonNotFound", id].Value +
                _localizer["SymbolDot"].Value);
        }

        var bytes = Encoding.UTF8.GetBytes(testReport.Json);

        return File(bytes, "application/json", $"stored_{id}.json");
    }


    // JSON helpers

    private static string FormatJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return string.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(json);

            return JsonSerializer.Serialize(
                document.RootElement,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });
        }
        catch (JsonException)
        {
            return json;
        }
    }


    private static bool JsonEquals(string firstJson, string secondJson)
    {
        try
        {
            var first = JsonNode.Parse(firstJson);
            var second = JsonNode.Parse(secondJson);

            return JsonNode.DeepEquals(first, second);
        }
        catch (JsonException)
        {
            return string.Equals(
                firstJson.Trim(),
                secondJson.Trim(),
                StringComparison.Ordinal);
        }
    }
}
