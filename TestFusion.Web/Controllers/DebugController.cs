using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
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
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<DebugController> _logger;

    public DebugController(
        AppDbContext db,
        IPlaywright playwrightService,
        JSONService jsonService,
        IStringLocalizer<SharedResource> localizer,
        IWebHostEnvironment environment,
        ILogger<DebugController> logger)
    {
        _db = db;
        _playwrightService = playwrightService;
        _jsonService = jsonService;
        _localizer = localizer;
        _environment = environment;
        _logger = logger;
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

        if (!string.IsNullOrWhiteSpace(viewModel.JsonId))
        {
            await LoadJsonViewer(viewModel);
        }

        viewModel.Logs = LoadLogs(500);

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
            _localizer[
                "DebugAllRowsDeleted",
                deletedCount,
                _localizer["SymbolDot"].Value
            ].Value +
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


    // JSON viewer

    private async Task LoadJsonViewer(DebugViewModel viewModel)
    {
        var storedReport = viewModel.TestReports
            .FirstOrDefault(x => x.Id == viewModel.JsonId);

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
            _logger.LogError(
                ex,
                "Failed to load debug JSON for report {ReportId}",
                viewModel.JsonId);

            viewModel.JsonError = ex.Message;
        }
    }


    // Logs

    private List<DebugLogEntry> LoadLogs(int maxEntries)
    {
        var logDirectory = GetLogDirectory();

        if (!Directory.Exists(logDirectory))
        {
            return new List<DebugLogEntry>();
        }

        var files = Directory
            .EnumerateFiles(logDirectory, "*.jsonl")
            .Where(path =>
            {
                var name = Path.GetFileName(path);

                return name.StartsWith("web-", StringComparison.OrdinalIgnoreCase) ||
                       name.StartsWith("worker-", StringComparison.OrdinalIgnoreCase);
            })
            .OrderByDescending(path => System.IO.File.GetLastWriteTimeUtc(path))
            .ToList();

        var entries = new List<DebugLogEntry>();

        foreach (var file in files)
        {
            entries.AddRange(ReadLogFile(file, maxEntries));
        }

        return entries
            .OrderByDescending(x => x.Timestamp)
            .Take(maxEntries)
            .ToList();
    }


    private IEnumerable<DebugLogEntry> ReadLogFile(
        string filePath,
        int maxEntries)
    {
        var lines = new Queue<string>();

        try
        {
            using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);

            using var reader = new StreamReader(stream);

            while (reader.ReadLine() is { } line)
            {
                lines.Enqueue(line);

                if (lines.Count > maxEntries)
                {
                    lines.Dequeue();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to read log file {LogFile}",
                filePath);

            yield break;
        }

        foreach (var line in lines)
        {
            var entry = ParseLogLine(line, filePath);

            if (entry != null)
            {
                yield return entry;
            }
        }
    }


    private static DebugLogEntry? ParseLogLine(
        string line,
        string filePath)
    {
        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;

            if (!root.TryGetProperty("@t", out var timestampElement) ||
                !DateTimeOffset.TryParse(
                    timestampElement.GetString(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var timestamp))
            {
                return null;
            }

            var level =
                root.TryGetProperty("@l", out var levelElement)
                    ? levelElement.GetString() ?? "Information"
                    : "Information";

            var sourceContext =
                root.TryGetProperty("SourceContext", out var sourceElement)
                    ? GetJsonValue(sourceElement)
                    : string.Empty;

            if (sourceContext.Contains('.'))
            {
                sourceContext =
                    sourceContext[(sourceContext.LastIndexOf('.') + 1)..];
            }

            var properties = new Dictionary<string, string>();

            foreach (var property in root.EnumerateObject())
            {
                if (property.Name.StartsWith('@') ||
                    property.Name == "SourceContext")
                {
                    continue;
                }

                properties[property.Name] =
                    GetJsonValue(property.Value);
            }

            var message =
                root.TryGetProperty("@m", out var renderedMessageElement)
                    ? renderedMessageElement.GetString() ?? string.Empty
                    : root.TryGetProperty("@mt", out var templateElement)
                        ? RenderMessage(
                            templateElement.GetString() ?? string.Empty,
                            properties)
                        : string.Empty;

            var exception =
                root.TryGetProperty("@x", out var exceptionElement)
                    ? exceptionElement.GetString()
                    : null;

            var fileName = Path.GetFileName(filePath);

            var application =
                fileName.StartsWith("worker-", StringComparison.OrdinalIgnoreCase)
                    ? "Worker"
                    : "Web";

            return new DebugLogEntry
            {
                Timestamp = timestamp,
                Application = application,
                Level = level,
                SourceContext = sourceContext,
                Message = message,
                Exception = exception,
                Properties = properties
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }


    private static string RenderMessage(
        string template,
        IReadOnlyDictionary<string, string> properties)
    {
        return Regex.Replace(
            template,
            @"\{[@$]?([A-Za-z0-9_]+)(?:[^}]*)\}",
            match =>
            {
                var key = match.Groups[1].Value;

                return properties.TryGetValue(key, out var value)
                    ? value
                    : match.Value;
            });
    }


    private static string GetJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Null => string.Empty,
            _ => element.GetRawText()
        };
    }


    private string GetLogDirectory()
    {
        var parentDirectory =
            Directory.GetParent(
                _environment.ContentRootPath
            )?.FullName
            ?? _environment.ContentRootPath;

        return Path.Combine(
            parentDirectory,
            "logs");
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
