using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.Json;
using TestFusion.Core;
using TestFusion.Core.Enums;
using TestFusion.Core.Interfaces;
using TestFusion.Core.Models.TestResult;
using TestFusion.Core.Models.WebModels;
using TestFusion.Data;
using TestFusion.Services.Services;
using TestFusion.Services.Models;

namespace TestFusion.Web.Controllers;

public class AnalysisController : Controller {
    private readonly AppDbContext _db;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ITimeZoneService _timeZoneService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<AnalysisController> _logger;
    private readonly LoggingSettings _loggingSettings;

    public AnalysisController(AppDbContext db, IStringLocalizer<SharedResource> localizer, ITimeZoneService timeZoneService, ISettingsService settingsService, ILogger<AnalysisController> logger, IOptions<LoggingSettings> loggingSettings) {
        _db = db;
        _localizer = localizer;
        _timeZoneService = timeZoneService;
        _settingsService = settingsService;
        _logger = logger;
        _loggingSettings = loggingSettings.Value;
    }

    [HttpPost]
    public IActionResult Generate(List<string> selectedIds) {
        if (selectedIds == null || selectedIds.Count == 0) {
            _logger.LogWarning("Analysis start rejected because no injectors were selected");
            return RedirectToAction("Index", "Home");
        }

        _logger.LogInformation("Analysis started with {Count} injectors: {InjectorIds}", selectedIds.Count, string.Join(", ", selectedIds));
        return RedirectToAction(nameof(Index), new { selectedIds });
    }

    [HttpGet]
    public async Task<IActionResult> Index(List<string> selectedIds) {
        if (selectedIds == null || selectedIds.Count == 0) {
            _logger.LogWarning("Analysis page requested without selected injectors");
            return RedirectToAction("Index", "Home");
        }

        var stopwatch = Stopwatch.StartNew();
        var model = await BuildGeneratedModel(selectedIds);
        stopwatch.Stop();

        if (model == null) {
            _logger.LogWarning("Analysis rejected because selected injectors have different part numbers: {InjectorIds}", string.Join(", ", selectedIds));
            TempData["Error"] = _localizer["ErrorDiffirentInjectorPartNumber"].Value + _localizer["SymbolDot"];
            return RedirectToAction("Index", "Home");
        }

        using var scope = _logger.BeginScope(new Dictionary<string, object> { ["DurationMs"] = stopwatch.ElapsedMilliseconds });
        if (stopwatch.ElapsedMilliseconds >= _loggingSettings.SlowOperationThresholds.AnalysisMs) _logger.LogWarning("Analysis completed successfully for {Count} injectors and {TestCount} tests, but took longer than expected", model.Injectors.Count, model.AllTests.Count);
        else _logger.LogInformation("Analysis completed successfully for {Count} injectors and {TestCount} tests", model.Injectors.Count, model.AllTests.Count);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> GeneratePdf(List<string> selectedIds, PdfLayoutModeEnum layoutMode = PdfLayoutModeEnum.Compact) {
        if (selectedIds == null || selectedIds.Count == 0) {
            _logger.LogWarning("PDF generation rejected because no injectors were selected");
            return RedirectToAction("Index", "Home");
        }

        var stopwatch = Stopwatch.StartNew();
        var model = await BuildGeneratedModel(selectedIds);

        if (model == null) {
            stopwatch.Stop();
            _logger.LogWarning("PDF generation rejected because selected injectors have different part numbers: {InjectorIds}", string.Join(", ", selectedIds));
            TempData["Error"] = _localizer["ErrorDiffirentInjectorPartNumber"].Value + _localizer["SymbolDot"];
            return RedirectToAction("Index", "Home");
        }

        var pdf = PDFService.Generate(model, layoutMode, _localizer, _timeZoneService);
        var fileName = $"Analyse-{DateTime.Now:yyyy-MM-dd-HHmm}.pdf";
        stopwatch.Stop();

        using var scope = _logger.BeginScope(new Dictionary<string, object> { ["DurationMs"] = stopwatch.ElapsedMilliseconds });
        if (stopwatch.ElapsedMilliseconds >= _loggingSettings.SlowOperationThresholds.PdfGenerationMs) _logger.LogWarning("PDF generated successfully for {Count} injectors, but took longer than expected. File: {FileName}, size: {ByteCount} bytes", model.Injectors.Count, fileName, pdf.Length);
        else _logger.LogInformation("PDF generated successfully for {Count} injectors. File: {FileName}, size: {ByteCount} bytes", model.Injectors.Count, fileName, pdf.Length);

        return File(pdf, "application/pdf", fileName);
    }

    private async Task<GeneratedModel?> BuildGeneratedModel(List<string> selectedIds) {
        var reports = await _db.TestReports.AsNoTracking().Where(x => selectedIds.Contains(x.Id)).ToListAsync();

        if (reports.Count != selectedIds.Count) {
            var foundIds = reports.Select(x => x.Id).ToHashSet();
            var missingIds = selectedIds.Where(id => !foundIds.Contains(id)).ToList();
            _logger.LogWarning("Only {FoundCount} of {RequestedCount} selected reports were found. Missing IDs: {MissingIds}", reports.Count, selectedIds.Count, string.Join(", ", missingIds));
        }

        var injectors = reports.Select(x => new TestResultViewModel { Data = JsonSerializer.Deserialize<TestResultModel>(x.Json)! }).ToList();
        var partNumbers = injectors.Select(x => x.Data.PartNumber).Distinct().ToList();

        if (partNumbers.Count > 1) {
            return null;
        }

        var allTests = injectors.SelectMany(x => x.Data.Tests).GroupBy(x => NormalizeTestName(x.TestName)).Select(g => {
            var firstValid = g.FirstOrDefault(x => x.TestStatus != 1) ?? g.First();
            return new TestModel {
                TestId = firstValid.TestId,
                TestName = NormalizeTestName(firstValid.TestName),
                TestOrder = firstValid.TestOrder,
                TestStatus = firstValid.TestStatus,
                TestTime = firstValid.TestTime,
                TestType = firstValid.TestType,
                TestResponseTime = firstValid.TestResponseTime,
                SubTests = firstValid.SubTests ?? new()
            };
        }).OrderBy(x => x.TestOrder).ToList();

        foreach (var injector in injectors) {
            injector.NormalizedTests = injector.Data.Tests.GroupBy(x => NormalizeTestName(x.TestName)).Select(g => {
                var firstValid = g.FirstOrDefault(x => x.TestStatus != 1) ?? g.First();
                return new TestCellModel {
                    TestId = firstValid.TestId,
                    TestName = NormalizeTestName(firstValid.TestName),
                    Exists = true,
                    IsSkipped = firstValid.TestStatus == 1,
                    Status = firstValid.TestStatus,
                    Time = firstValid.TestTime,
                    Response = firstValid.TestResponseTime.ToString(),
                    SubTests = firstValid.SubTests ?? new()
                };
            }).OrderBy(x => x.TestName).ToList();
        }

        return new GeneratedModel { Injectors = injectors, AllTests = allTests, SelectedIds = selectedIds, Settings = await _settingsService.GetAsync() };
    }

    private static string NormalizeTestName(string name) {
        return name.Replace(" : SKIPPED", "").Trim();
    }
}
