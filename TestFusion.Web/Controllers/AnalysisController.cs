using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TestFusion.Core.Interfaces;
using TestFusion.Core.Models.WebModels;
using TestFusion.Data;
using Microsoft.EntityFrameworkCore;

namespace TestFusion.Web.Controllers
{
    public class AnalysisController : Controller
    {
        private readonly AppDbContext _db;
        private readonly ISyncService _sync;

        public AnalysisController(AppDbContext db, ISyncService sync)
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
