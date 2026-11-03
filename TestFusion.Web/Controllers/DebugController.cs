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

public class DebugController : Controller {
    private readonly AppDbContext _db;
    private readonly IPlaywright _playwrightService;
    private readonly JSONService _jsonService;
    private readonly ISettingsService _settingsService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<DebugController> _logger;

    public DebugController(AppDbContext db, IPlaywright playwrightService, JSONService jsonService, ISettingsService settingsService, IStringLocalizer<SharedResource> localizer, IWebHostEnvironment environment, ILogger<DebugController> logger) {
        _db = db;
        _playwrightService = playwrightService;
        _jsonService = jsonService;
        _settingsService = settingsService;
        _localizer = localizer;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? id) {
        var testReports = await _db.TestReports
            .AsNoTracking()
            .OrderByDescending(x => x.TimeOffTesting)
            .ToListAsync();

        var settings = await _settingsService.GetForEditAsync();

        var viewModel = new DebugViewModel {
            TestReports = testReports,
            JsonId = id?.Trim() ?? string.Empty,
            ShowDebugInformation = settings.ShowDebugInformation,
            NavigationPages = settings.NavigationPages
                .Where(page => page.Key.StartsWith("Debug/", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(page => page.Key, page => page.Value, StringComparer.OrdinalIgnoreCase)
        };

        if (!string.IsNullOrWhiteSpace(viewModel.JsonId)) {
            await LoadJsonViewer(viewModel);
        }

        viewModel.Logs = LoadLogs(500);

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDebugSettings(DebugViewModel model) {
        var navigationPages = model.NavigationPages
            .Where(page => page.Key.StartsWith("Debug/", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(page => page.Key, page => page.Value, StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation("Debug settings update requested");
        await _settingsService.UpdateDebugAsync(model.ShowDebugInformation, navigationPages);

        TempData["DebugMessage"] = "Debuginstellingen zijn opgeslagen.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSelectedTestReports(List<string> selectedIds) {
        if (selectedIds.Count == 0) {
            _logger.LogWarning("Delete selected reports requested without selected report IDs");
            TempData["DebugMessage"] =
                _localizer["DebugNoRowsSelected"].Value +
                _localizer["SymbolDot"].Value;

            return RedirectToAction(nameof(Index));
        }

        _logger.LogInformation("Deleting selected reports: {ReportIds}", string.Join(", ", selectedIds));
        var deletedCount = await _db.TestReports
            .Where(x => selectedIds.Contains(x.Id))
            .ExecuteDeleteAsync();
        _logger.LogInformation("Deleted {Count} selected reports", deletedCount);

        TempData["DebugMessage"] =
            _localizer["DebugSelectedRowsDeleted", deletedCount].Value +
            _localizer["SymbolDot"].Value;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearTestReports(bool confirmed) {
        if (!confirmed) {
            _logger.LogWarning("Delete all reports rejected because confirmation was missing");
            return BadRequest(
                _localizer["DebugConfirmationRequired"].Value +
                _localizer["SymbolDot"].Value);
        }

        _logger.LogWarning("Deleting all test reports from debug page");
        var deletedCount = await _db.TestReports.ExecuteDeleteAsync();
        _logger.LogInformation("Deleted all test reports. Deleted count: {Count}", deletedCount);

        TempData["DebugMessage"] =
            _localizer[
                "DebugAllRowsDeleted",
                deletedCount,
                _localizer["SymbolDot"].Value
            ].Value +
            _localizer["SymbolDot"].Value;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> DownloadRawJson(string id) {
        if (string.IsNullOrWhiteSpace(id)) {
            return BadRequest(
                _localizer["DebugIdRequired"].Value +
                _localizer["SymbolDot"].Value);
        }

        _logger.LogInformation("Raw JSON download requested for report {ReportId}", id);
        var rawJson = await _playwrightService.GetDataForId(id);
        var bytes = Encoding.UTF8.GetBytes(rawJson);

        return File(bytes, "application/json", $"raw_{id}.json");
    }

    [HttpGet]
    public async Task<IActionResult> DownloadProcessedJson(string id) {
        if (string.IsNullOrWhiteSpace(id)) {
            return BadRequest(
                _localizer["DebugIdRequired"].Value +
                _localizer["SymbolDot"].Value);
        }

        _logger.LogInformation("Processed JSON download requested for report {ReportId}", id);
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
    public async Task<IActionResult> DownloadStoredJson(string id) {
        if (string.IsNullOrWhiteSpace(id)) {
            return BadRequest(
                _localizer["DebugIdRequired"].Value +
                _localizer["SymbolDot"].Value);
        }

        _logger.LogInformation("Stored JSON download requested for report {ReportId}", id);
        var testReport = await _db.TestReports
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (testReport == null) {
            return NotFound(
                _localizer["DebugStoredJsonNotFound", id].Value +
                _localizer["SymbolDot"].Value);
        }

        var bytes = Encoding.UTF8.GetBytes(testReport.Json);

        return File(bytes, "application/json", $"stored_{id}.json");
    }

    [HttpGet]
    public IActionResult ExportLogs(string format = "txt", string? application = null, string? category = null, string? level = null, string? search = null) {
        var logs = FilterLogs(LoadLogs(5000), application, category, level, search);
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");

        if (format.Equals("json", StringComparison.OrdinalIgnoreCase)) {
            var json = JsonSerializer.Serialize(logs.Select(log => new {
                timestamp = log.Timestamp.ToLocalTime(),
                application = log.Application,
                category = log.Category,
                level = log.Level,
                message = log.Message,
                durationMs = log.DurationMs,
                source = log.SourceContext,
                exception = log.Exception,
                properties = log.Properties
            }), new JsonSerializerOptions { WriteIndented = true });

            return File(Encoding.UTF8.GetBytes(json), "application/json", $"testfusion-logs-{timestamp}.json");
        }

        var text = new StringBuilder();
        foreach (var log in logs) {
            text.Append(log.Timestamp.ToLocalTime().ToString("dd-MM-yyyy HH:mm:ss"));
            text.Append(" | ").Append(log.Application);
            text.Append(" | ").Append(log.Category);
            text.Append(" | ").Append(log.Level);
            text.Append(" | ").AppendLine(log.Message);
            if (log.DurationMs.HasValue) text.AppendLine($"  Duration: {log.DurationMs.Value} ms");
            text.AppendLine($"  Source: {log.SourceContext}");
            foreach (var property in log.Properties) text.AppendLine($"  {property.Key}: {property.Value}");
            if (!string.IsNullOrWhiteSpace(log.Exception)) text.AppendLine($"  Exception: {log.Exception}");
            text.AppendLine();
        }

        return File(Encoding.UTF8.GetBytes(text.ToString()), "text/plain", $"testfusion-logs-{timestamp}.txt");
    }

    private static List<DebugLogEntry> FilterLogs(IEnumerable<DebugLogEntry> logs, string? application, string? category, string? level, string? search) {
        var filtered = logs;
        if (!string.IsNullOrWhiteSpace(application)) filtered = filtered.Where(log => log.Application.Equals(application, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(category)) filtered = filtered.Where(log => log.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(level)) filtered = filtered.Where(log => log.Level.Equals(level, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(search)) {
            filtered = filtered.Where(log => {
                var searchable = string.Join(" ", new[] { log.Message, log.SourceContext, log.Application, log.Category, log.Level, log.Exception ?? string.Empty, log.DurationMs?.ToString() ?? string.Empty }.Concat(log.Properties.Select(property => $"{property.Key} {property.Value}")));
                return searchable.Contains(search, StringComparison.OrdinalIgnoreCase);
            });
        }

        return filtered.OrderByDescending(log => log.Timestamp).ToList();
    }

    private async Task LoadJsonViewer(DebugViewModel viewModel) {
        var storedReport = viewModel.TestReports.FirstOrDefault(x => x.Id == viewModel.JsonId);

        if (storedReport != null) {
            viewModel.StoredJsonFound = true;
            viewModel.StoredJson = FormatJson(storedReport.Json);
        }

        try {
            var rawJson = await _playwrightService.GetDataForId(viewModel.JsonId);
            var processedModel = _jsonService.ConvertToTestResultModel(rawJson);

            var processedJson = _jsonService.ConvertToJson(
                processedModel,
                prettyJson: true,
                useUnicodeSymbols: false);

            viewModel.RawJson = FormatJson(rawJson);
            viewModel.ProcessedJson = FormatJson(processedJson);

            if (storedReport != null) {
                viewModel.ProcessedMatchesStored = JsonEquals(processedJson, storedReport.Json);
            }
        }
        catch (Exception ex) {
            _logger.LogError(
                ex,
                "Failed to load debug JSON for report {ReportId}",
                viewModel.JsonId);

            viewModel.JsonError = ex.Message;
        }
    }

    private List<DebugLogEntry> LoadLogs(int maxEntries) {
        var logDirectory = GetLogDirectory();

        if (!Directory.Exists(logDirectory)) {
            return [];
        }

        var files = Directory
            .EnumerateFiles(logDirectory, "*.jsonl")
            .Where(path => {
                var name = Path.GetFileName(path);

                return name.StartsWith("web-", StringComparison.OrdinalIgnoreCase) ||
                       name.StartsWith("worker-", StringComparison.OrdinalIgnoreCase);
            })
            .OrderByDescending(System.IO.File.GetLastWriteTimeUtc)
            .ToList();

        var entries = new List<DebugLogEntry>();

        foreach (var file in files) {
            entries.AddRange(ReadLogFile(file, maxEntries));
        }

        return entries
            .OrderByDescending(x => x.Timestamp)
            .Take(maxEntries)
            .ToList();
    }

    private IEnumerable<DebugLogEntry> ReadLogFile(string filePath, int maxEntries) {
        var lines = new Queue<string>();

        try {
            using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);

            using var reader = new StreamReader(stream);

            while (reader.ReadLine() is { } line) {
                lines.Enqueue(line);

                if (lines.Count > maxEntries) {
                    lines.Dequeue();
                }
            }
        }
        catch (Exception ex) {
            _logger.LogWarning(
                ex,
                "Failed to read log file {LogFile}",
                filePath);

            yield break;
        }

        foreach (var line in lines) {
            var entry = ParseLogLine(line, filePath);

            if (entry != null) {
                yield return entry;
            }
        }
    }

    private static DebugLogEntry? ParseLogLine(string line, string filePath) {
        try {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;

            if (!root.TryGetProperty("@t", out var timestampElement) ||
                !DateTimeOffset.TryParse(
                    timestampElement.GetString(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var timestamp)) {
                return null;
            }

            var level = root.TryGetProperty("@l", out var levelElement)
                ? levelElement.GetString() ?? "Information"
                : "Information";

            var fullSourceContext = root.TryGetProperty("SourceContext", out var sourceElement)
                ? GetJsonValue(sourceElement)
                : string.Empty;

            var sourceContext = fullSourceContext;
            if (sourceContext.Contains('.')) {
                sourceContext = sourceContext[(sourceContext.LastIndexOf('.') + 1)..];
            }

            var properties = new Dictionary<string, string>();

            foreach (var property in root.EnumerateObject()) {
                if (property.Name.StartsWith('@') || property.Name == "SourceContext") {
                    continue;
                }

                properties[property.Name] = GetJsonValue(property.Value);
            }

            var message = root.TryGetProperty("@m", out var renderedMessageElement)
                ? renderedMessageElement.GetString() ?? string.Empty
                : root.TryGetProperty("@mt", out var templateElement)
                    ? RenderMessage(templateElement.GetString() ?? string.Empty, properties)
                    : string.Empty;

            long? durationMs = null;
            if (properties.TryGetValue("DurationMs", out var durationValue) && long.TryParse(durationValue, out var parsedDuration)) durationMs = parsedDuration;
            else if (properties.TryGetValue("ElapsedMilliseconds", out var elapsedValue) && long.TryParse(elapsedValue, out var parsedElapsed)) durationMs = parsedElapsed;

            properties.Remove("DurationMs");
            properties.Remove("ElapsedMilliseconds");

            var exception = root.TryGetProperty("@x", out var exceptionElement)
                ? exceptionElement.GetString()
                : null;

            var fileName = Path.GetFileName(filePath);

            var application = fileName.StartsWith("worker-", StringComparison.OrdinalIgnoreCase)
                ? "Worker"
                : "Web";

            return new DebugLogEntry {
                Timestamp = timestamp,
                Application = application,
                Category = GetLogCategory(fullSourceContext),
                Level = level,
                SourceContext = sourceContext,
                Message = message,
                DurationMs = durationMs,
                Exception = exception,
                Properties = properties
            };
        }
        catch (JsonException) {
            return null;
        }
    }


    private static string GetLogCategory(string sourceContext) {
        if (sourceContext.Contains("AnalysisController", StringComparison.OrdinalIgnoreCase)) return "Analysis";
        if (sourceContext.Contains("Settings", StringComparison.OrdinalIgnoreCase)) return "Settings";
        if (sourceContext.Contains("SyncService", StringComparison.OrdinalIgnoreCase) || sourceContext.Equals("Worker", StringComparison.OrdinalIgnoreCase)) return "Synchronization";
        if (sourceContext.Contains("PlaywrightService", StringComparison.OrdinalIgnoreCase)) return "CarbonX";
        if (sourceContext.Contains("RequestLoggingMiddleware", StringComparison.OrdinalIgnoreCase)) return "Navigation";
        if (sourceContext.Contains("DebugController", StringComparison.OrdinalIgnoreCase)) return "Debug";
        if (sourceContext.Contains("LogRetention", StringComparison.OrdinalIgnoreCase)) return "Maintenance";
        if (sourceContext.Contains("EntityFramework", StringComparison.OrdinalIgnoreCase) || sourceContext.Contains("DbContext", StringComparison.OrdinalIgnoreCase)) return "Database";
        if (sourceContext.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase) || sourceContext.StartsWith("System", StringComparison.OrdinalIgnoreCase)) return "System";
        return "Application";
    }

    private static string RenderMessage(string template, IReadOnlyDictionary<string, string> properties) {
        return Regex.Replace(
            template,
            @"\{[@$]?([A-Za-z0-9_]+)(?:[^}]*)\}",
            match => {
                var key = match.Groups[1].Value;

                return properties.TryGetValue(key, out var value)
                    ? value
                    : match.Value;
            });
    }

    private static string GetJsonValue(JsonElement element) {
        return element.ValueKind switch {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Null => string.Empty,
            _ => element.GetRawText()
        };
    }

    private string GetLogDirectory() {
        var parentDirectory =
            Directory.GetParent(_environment.ContentRootPath)?.FullName ??
            _environment.ContentRootPath;

        return Path.Combine(parentDirectory, "logs");
    }

    private static string FormatJson(string json) {
        if (string.IsNullOrWhiteSpace(json)) {
            return string.Empty;
        }

        try {
            using var document = JsonDocument.Parse(json);

            return JsonSerializer.Serialize(
                document.RootElement,
                new JsonSerializerOptions {
                    WriteIndented = true
                });
        }
        catch (JsonException) {
            return json;
        }
    }

    private static bool JsonEquals(string firstJson, string secondJson) {
        try {
            var first = JsonNode.Parse(firstJson);
            var second = JsonNode.Parse(secondJson);

            return JsonNode.DeepEquals(first, second);
        }
        catch (JsonException) {
            return string.Equals(
                firstJson.Trim(),
                secondJson.Trim(),
                StringComparison.Ordinal);
        }
    }
}
