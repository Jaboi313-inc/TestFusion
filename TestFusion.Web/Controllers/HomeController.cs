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
        private readonly ITimeZoneService _timeZoneService;

        public HomeController(
            AppDbContext db,
            ISyncService sync,
            ITimeZoneService timeZoneService)
        {
            _db = db;
            _sync = sync;
            _timeZoneService = timeZoneService;
        }


        public async Task<IActionResult> Index(
            int? resultLimit = null,
            string? partNumber = null,
            string? brand = null,
            string? type = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            const int defaultLimit = 50;
            const string cookieName = "TestFusion.ResultLimit";

            var allowedLimits =
                new[] { 25, 50, 100, 0 };

            int selectedLimit;


            // Result limit
            if (
                resultLimit.HasValue &&
                allowedLimits.Contains(
                    resultLimit.Value)
            )
            {
                selectedLimit =
                    resultLimit.Value;

                Response.Cookies.Append(
                    cookieName,
                    selectedLimit.ToString(),
                    new CookieOptions
                    {
                        Expires =
                            DateTimeOffset.UtcNow.AddYears(1),

                        IsEssential = true,
                        HttpOnly = true,
                        SameSite = SameSiteMode.Lax,
                        Secure = Request.IsHttps,
                        Path = "/"
                    });
            }
            else if (
                Request.Cookies.TryGetValue(
                    cookieName,
                    out var savedValue) &&
                int.TryParse(
                    savedValue,
                    out var savedLimit) &&
                allowedLimits.Contains(
                    savedLimit)
            )
            {
                selectedLimit =
                    savedLimit;
            }
            else
            {
                selectedLimit =
                    defaultLimit;
            }


            var hasActiveFilters =
                !string.IsNullOrWhiteSpace(partNumber) ||
                !string.IsNullOrWhiteSpace(brand) ||
                !string.IsNullOrWhiteSpace(type) ||
                dateFrom.HasValue ||
                dateTo.HasValue;


            var totalCount =
                await _db.TestItems
                    .CountAsync();


            var query =
                _db.TestItems
                    .AsNoTracking()
                    .AsQueryable();


            // Filters
            if (!string.IsNullOrWhiteSpace(
                partNumber))
            {
                var value =
                    partNumber.Trim();

                query =
                    query.Where(x =>
                        EF.Functions.ILike(
                            x.PartNumber,
                            $"%{value}%"));
            }


            if (!string.IsNullOrWhiteSpace(
                brand))
            {
                var value =
                    brand.Trim();

                query =
                    query.Where(x =>
                        EF.Functions.ILike(
                            x.PartBrand,
                            $"%{value}%"));
            }


            if (!string.IsNullOrWhiteSpace(
                type))
            {
                var value =
                    type.Trim();

                query =
                    query.Where(x =>
                        EF.Functions.ILike(
                            x.PartType,
                            $"%{value}%"));
            }


            if (dateFrom.HasValue)
            {
                var fromUtc =
                    _timeZoneService.ConvertUserToUtc(
                        dateFrom.Value.Date);

                query =
                    query.Where(x =>
                        x.DateTime >= fromUtc);
            }


            if (dateTo.HasValue)
            {
                var toUtcExclusive =
                    _timeZoneService.ConvertUserToUtc(
                        dateTo.Value.Date.AddDays(1));

                query =
                    query.Where(x =>
                        x.DateTime < toUtcExclusive);
            }


            var orderedQuery =
                query.OrderByDescending(
                    x => x.DateTime);


            List<TestFusion.Core.Models.TestListItemModel> items;


            if (hasActiveFilters)
            {
                items =
                    await orderedQuery
                        .ToListAsync();
            }
            else if (selectedLimit == 0)
            {
                items =
                    await orderedQuery
                        .ToListAsync();
            }
            else
            {
                items =
                    await orderedQuery
                        .Take(selectedLimit)
                        .ToListAsync();
            }


            ViewBag.ResultLimit =
                selectedLimit;

            ViewBag.TotalCount =
                totalCount;

            ViewBag.HasActiveFilters =
                hasActiveFilters;


            return View(items);
        }


        [HttpPost]
        public async Task<IActionResult> Refresh()
        {
            await _sync.RunSync();

            return RedirectToAction(
                nameof(Index));
        }
    }
}