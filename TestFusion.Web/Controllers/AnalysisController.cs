using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TestFusion.Core.Enums;
using TestFusion.Core.Interfaces;
using Microsoft.Extensions.Localization;
using TestFusion.Core;
using TestFusion.Core.Models.TestResult;
using TestFusion.Core.Models.WebModels;
using TestFusion.Data;
using TestFusion.Web.Services;

namespace TestFusion.Web.Controllers
{
    public class AnalysisController : Controller
    {
        private readonly AppDbContext _db;
        private readonly ISyncService _sync;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public AnalysisController(
            AppDbContext db,
            ISyncService sync,
            IStringLocalizer<SharedResource> localizer)
        {
            _db = db;
            _sync = sync;
            _localizer = localizer;
        }

        [HttpPost]
        public IActionResult Generate(List<string> selectedIds)
        {
            if (selectedIds == null || selectedIds.Count == 0)
            {
                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction(
                nameof(Index),
                new { selectedIds }
            );
        }

        [HttpGet]
        public async Task<IActionResult> Index(List<string> selectedIds)
        {
            if (selectedIds == null || selectedIds.Count == 0)
            {
                return RedirectToAction("Index", "Home");
            }

            var model = await BuildGeneratedModel(selectedIds);

            if (model == null)
            {
                TempData["Error"] =
                _localizer["ErrorDiffirentInjectorPartNumber"].Value + ".";

                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> GeneratePdf(
            List<string> selectedIds,
            PdfLayoutModeEnum layoutMode = PdfLayoutModeEnum.Compact)
        {
            var model = await BuildGeneratedModel(selectedIds);

            if (model == null)
            {
                TempData["Error"] =
                    _localizer["ErrorDiffirentInjectorPartNumber"].Value + ".";

                return RedirectToAction("Index", "Home");
            }

            var pdf = PDFService.Generate(
                model,
                layoutMode,
                _localizer);

            return File(
                pdf,
                "application/pdf",
                $"Analyse-{DateTime.Now:yyyy-MM-dd-HHmm}.pdf"
            );
        }

        private async Task<GeneratedModel?> BuildGeneratedModel(
            List<string> selectedIds)
        {
            var jsons = await _db.StoredJsons
                .Where(x => selectedIds.Contains(x.Id))
                .ToListAsync();

            var injectors = jsons
                .Select(x => new TestResultViewModel
                {
                    Data = JsonSerializer.Deserialize<TestResultModel>(x.Json)!
                })
                .ToList();

            var partNumbers = injectors
                .Select(x => x.Data.PartNumber)
                .Distinct()
                .ToList();

            if (partNumbers.Count > 1)
            {
                return null;
            }

            var allTests = injectors
                .SelectMany(x => x.Data.Tests)
                .GroupBy(x => NormalizeTestName(x.TestName))
                .Select(g =>
                {
                    var firstValid =
                        g.FirstOrDefault(x => x.TestStatus != 1)
                        ?? g.First();

                    return new TestModel
                    {
                        TestId = firstValid.TestId,
                        TestName = NormalizeTestName(firstValid.TestName),
                        TestOrder = firstValid.TestOrder,
                        TestStatus = firstValid.TestStatus,
                        TestTime = firstValid.TestTime,
                        TestType = firstValid.TestType,
                        TestResponseTime = firstValid.TestResponseTime,
                        SubTests = firstValid.SubTests ?? new()
                    };
                })
                .OrderBy(x => x.TestOrder)
                .ToList();

            foreach (var injector in injectors)
            {
                injector.NormalizedTests = injector.Data.Tests
                    .GroupBy(x => NormalizeTestName(x.TestName))
                    .Select(g =>
                    {
                        var firstValid =
                            g.FirstOrDefault(x => x.TestStatus != 1)
                            ?? g.First();

                        return new TestCellModel
                        {
                            TestId = firstValid.TestId,
                            TestName = NormalizeTestName(firstValid.TestName),
                            Exists = true,
                            IsSkipped = firstValid.TestStatus == 1,
                            Status = firstValid.TestStatus,
                            Time = firstValid.TestTime,
                            Response = firstValid.TestResponseTime.ToString(),
                            SubTests = firstValid.SubTests ?? new()
                        };
                    })
                    .OrderBy(x => x.TestName)
                    .ToList();
            }

            return new GeneratedModel
            {
                Injectors = injectors,
                AllTests = allTests,
                SelectedIds = selectedIds
            };
        }

        private static string NormalizeTestName(string name)
        {
            return name
                .Replace(" : SKIPPED", "")
                .Trim();
        }
    }
}