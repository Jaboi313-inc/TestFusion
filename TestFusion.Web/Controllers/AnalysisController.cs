using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TestFusion.Core.Interfaces;
using TestFusion.Core.Models;
using TestFusion.Data;
using Microsoft.EntityFrameworkCore;

namespace TestFusion.Web.Controllers
{
    public class AnalysisController : Controller
    {
        private readonly IPlaywrightInterface _playwrightInterface;
        private readonly AppDbContext _db;

        public AnalysisController(IPlaywrightInterface playwrightInterface, AppDbContext db)
        {
            _playwrightInterface = playwrightInterface;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var items = await _db.TestItems
                .OrderByDescending(x => x.DateTime)
                .Take(50)
                .ToListAsync();

            return View(items);
        }

            /*
            var debugJson = JsonSerializer.Serialize(
            item,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

            System.IO.File.WriteAllText(
                @"D:\Progammeren\repos\TestFusion\TestFusion.Web\mapped.json",
                debugJson
            );

            return View(new List<TestListItemModel> { item });
            */

            /*
            var items = await Task.WhenAll(
                ids.Select(id => _playwrightInterface.GetDataForId(id))
            );

            return View(items.ToList());
            */
        
        [HttpPost]
        public IActionResult Generate(List<string> selectedIds)
        {
            var model = new GeneratedModel
            {
                SelectedIds = selectedIds
            };

            return View(model);
        }
    }
}
