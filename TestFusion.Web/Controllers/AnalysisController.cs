using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TestFusion.Core.Enums;
using TestFusion.Core.Interfaces;
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

        public AnalysisController(
            AppDbContext db,
            ISyncService sync)
        {
            _db = db;
            _sync = sync;
        }

        [HttpPost]
        public async Task<IActionResult> Refresh()
        {
            await _sync.RunSync();
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Index()
        {
            var items = await _db.TestItems
                .OrderByDescending(x => x.DateTime)
                .Take(50)
                .ToListAsync();

            return View(items);
        }

        [HttpPost]
        public async Task<IActionResult> Generate(List<string> selectedIds)
        {
            var model = await BuildGeneratedModel(selectedIds);

            if (model == null)
            {
                TempData["Error"] =
                    "Je kunt alleen verstuivers met hetzelfde onderdeelnummer vergelijken.";

                return RedirectToAction("Index");
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> GeneratePdf(List<string> selectedIds, PdfLayoutModeEnum layoutMode = PdfLayoutModeEnum.Compact)
        {
            var model = await BuildGeneratedModel(selectedIds);

            if (model == null)
            {
                TempData["Error"] =
                    "Je kunt alleen verstuivers met hetzelfde onderdeelnummer vergelijken.";

                return RedirectToAction("Index");
            }

            byte[] pdf = PDFService.Generate(
                model,
                layoutMode
            );

            return File(
                pdf,
                "application/pdf",
                $"Analyse-{DateTime.Now:yyyy-MM-dd-HHmm}.pdf"
            );
        }

        private async Task<GeneratedModel?> BuildGeneratedModel(
            List<string> selectedIds)
        {
            // Get JSON from db
            var jsons = await _db.StoredJsons
                .Where(x => selectedIds.Contains(x.Id))
                .ToListAsync();


            // JSON -> TestResultModel
            var injectors = jsons
                .Select(x => new TestResultViewModel
                {
                    Data = JsonSerializer.Deserialize<TestResultModel>(x.Json)!
                })
                .ToList();

            // Check if all injectors have the same part number
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
                    var firstValid = g.FirstOrDefault(x => x.TestStatus != 1) ?? g.First();

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
                        var firstValid = g.FirstOrDefault(x => x.TestStatus != 1) ?? g.First();

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