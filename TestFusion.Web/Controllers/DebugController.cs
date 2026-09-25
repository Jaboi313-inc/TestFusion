using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestFusion.Core.Interfaces;
using TestFusion.Data;
using TestFusion.Services.Services;

namespace TestFusion.Web.Controllers;

public class DebugController : Controller
{
    private readonly AppDbContext _db;
    private readonly IPlaywright _playwrightService;
    private readonly JSONService _jsonService;

    public DebugController(
        AppDbContext db,
        IPlaywright playwrightService,
        JSONService jsonService)
    {
        _db = db;
        _playwrightService = playwrightService;
        _jsonService = jsonService;
    }


    // Page

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }


    // Database

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearTestItems(bool confirmed)
    {
        if (!confirmed)
        {
            return BadRequest("Confirmation is required.");
        }

        var deletedCount =
            await _db.TestItems.ExecuteDeleteAsync();

        TempData["DebugMessage"] =
            $"TestItems cleared. {deletedCount} rows deleted.";

        return RedirectToAction(nameof(Index));
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearStoredJsons(bool confirmed)
    {
        if (!confirmed)
        {
            return BadRequest("Confirmation is required.");
        }

        var deletedCount =
            await _db.StoredJsons.ExecuteDeleteAsync();

        TempData["DebugMessage"] =
            $"StoredJsons cleared. {deletedCount} rows deleted.";

        return RedirectToAction(nameof(Index));
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearDatabase(bool confirmed)
    {
        if (!confirmed)
        {
            return BadRequest("Confirmation is required.");
        }

        var storedJsonCount =
            await _db.StoredJsons.ExecuteDeleteAsync();

        var testItemCount =
            await _db.TestItems.ExecuteDeleteAsync();

        TempData["DebugMessage"] =
            $"Test data cleared. " +
            $"{testItemCount} TestItems and " +
            $"{storedJsonCount} StoredJsons deleted.";

        return RedirectToAction(nameof(Index));
    }


    // JSON downloads

    [HttpGet]
    public async Task<IActionResult> DownloadRawJson(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("ID is required.");
        }

        var rawJson =
            await _playwrightService.GetDataForId(id);

        var bytes =
            Encoding.UTF8.GetBytes(rawJson);

        return File(
            bytes,
            "application/json",
            $"raw_{id}.json");
    }


    [HttpGet]
    public async Task<IActionResult> DownloadProcessedJson(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("ID is required.");
        }

        var rawJson =
            await _playwrightService.GetDataForId(id);

        var model = _jsonService.ConvertToTestResultModel(rawJson);

        var processedJson =
            _jsonService.ConvertToJson(
                model,
                prettyJson: true,
                useUnicodeSymbols: false);

        var bytes =
            Encoding.UTF8.GetBytes(processedJson);

        return File(
            bytes,
            "application/json",
            $"processed_{id}.json");
    }


    [HttpGet]
    public async Task<IActionResult> DownloadStoredJson(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("ID is required.");
        }

        var storedJson =
            await _db.StoredJsons
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

        if (storedJson == null)
        {
            return NotFound(
                $"No stored JSON found for ID '{id}'.");
        }

        var bytes =
            Encoding.UTF8.GetBytes(storedJson.Json);

        return File(
            bytes,
            "application/json",
            $"stored_{id}.json");
    }
}