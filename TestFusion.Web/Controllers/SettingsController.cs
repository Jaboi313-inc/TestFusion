using Microsoft.AspNetCore.Mvc;
using TestFusion.Core.Interfaces;
using TestFusion.Core.Models.WebModels;

namespace TestFusion.Web.Controllers;

public class SettingsController : Controller
{
    private readonly ISettingsService _settingsService;

    public SettingsController(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? returnUrl)
    {
        var model = await _settingsService.GetForEditAsync();
        model.ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var currentSettings = await _settingsService.GetForEditAsync();
            model.HasCarbonXPassword = currentSettings.HasCarbonXPassword;
            return View(model);
        }

        await _settingsService.UpdateAsync(model);

        TempData["Success"] = "Instellingen zijn opgeslagen.";

        return RedirectToAction(nameof(Index), new { returnUrl = model.ReturnUrl });
    }
}