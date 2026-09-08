using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestFusion.Core.Interfaces;
using TestFusion.Data;

namespace TestFusion.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;
        private readonly ISyncService _sync;

        public HomeController(
            AppDbContext db,
            ISyncService sync)
        {
            _db = db;
            _sync = sync;
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
        public async Task<IActionResult> Refresh()
        {
            await _sync.RunSync();

            return RedirectToAction("Index");
        }
    }
}