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

        public async Task<IActionResult> Index(int? resultLimit = null)
        {
            const int defaultLimit = 50;
            const string cookieName = "TestFusion.ResultLimit";

            var allowedLimits = new[] { 25, 50, 100, 0 };

            int selectedLimit;

            if (resultLimit.HasValue &&
                allowedLimits.Contains(resultLimit.Value))
            {
                selectedLimit = resultLimit.Value;

                Response.Cookies.Append(
                    cookieName,
                    selectedLimit.ToString(),
                    new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddYears(1),
                        IsEssential = true,
                        HttpOnly = true,
                        SameSite = SameSiteMode.Lax,
                        Secure = Request.IsHttps,
                        Path = "/"
                    });
            }
            else if (
                Request.Cookies.TryGetValue(cookieName, out var savedValue) &&
                int.TryParse(savedValue, out var savedLimit) &&
                allowedLimits.Contains(savedLimit))
            {
                selectedLimit = savedLimit;
            }
            else
            {
                selectedLimit = defaultLimit;
            }


            var totalCount =
                await _db.TestItems.CountAsync();


            var query = _db.TestItems
                .OrderByDescending(x => x.DateTime);


            var items = selectedLimit == 0
                ? await query.ToListAsync()
                : await query.Take(selectedLimit).ToListAsync();


            ViewBag.ResultLimit = selectedLimit;
            ViewBag.TotalCount = totalCount;

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