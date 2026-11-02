using Microsoft.AspNetCore.Mvc;
using TestFusion.Core.Interfaces;
using TestFusion.Core.Models.WebModels;

namespace TestFusion.Web.Controllers;

public class SettingsController : Controller {
    private readonly ISettingsService _settingsService;

    public SettingsController(ISettingsService settingsService) {
        _settingsService = settingsService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? returnUrl) {
        var model = await _settingsService.GetForEditAsync();

        model.NavigationPages = model.NavigationPages
            .Where(page => !page.Key.StartsWith("Debug/", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(page => page.Key, page => page.Value, StringComparer.OrdinalIgnoreCase);

        model.ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SettingsViewModel model) {
        if (!ModelState.IsValid) {
            var currentSettings = await _settingsService.GetForEditAsync();
            model.HasCarbonXPassword = currentSettings.HasCarbonXPassword;

            model.NavigationPages = model.NavigationPages
                .Where(page => !page.Key.StartsWith("Debug/", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(page => page.Key, page => page.Value, StringComparer.OrdinalIgnoreCase);

            return View(model);
        }

        await _settingsService.UpdateAsync(model);

        TempData["Success"] = "Instellingen zijn opgeslagen.";
        TempData["SuccessDuration"] = 4000;

        return RedirectToAction(nameof(Index), new {
            returnUrl = model.ReturnUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetToDefaults(string? returnUrl) {
        await _settingsService.ResetToDefaultsAsync();

        TempData["Success"] = "De standaardinstellingen zijn hersteld.";
        TempData["SuccessDuration"] = 6000;

        return RedirectToAction(nameof(Index), new {
            returnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null
        });
    }
}
