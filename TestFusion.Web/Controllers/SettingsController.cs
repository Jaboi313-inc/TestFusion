using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using TestFusion.Core;
using TestFusion.Core.Interfaces;
using TestFusion.Core.Models.WebModels;

namespace TestFusion.Web.Controllers;

public class SettingsController : Controller {
    private readonly ISettingsService _settingsService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public SettingsController(ISettingsService settingsService, IStringLocalizer<SharedResource> localizer) {
        _settingsService = settingsService;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? returnUrl) {
        var model = await _settingsService.GetForEditAsync();

        model.NavigationPages = model.NavigationPages
            .Where(page => !page.Key.StartsWith("Debug/", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(page => page.Key, page => page.Value, StringComparer.OrdinalIgnoreCase);

        model.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl)
            ? null
            : Url.IsLocalUrl(returnUrl) ? returnUrl : null;

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

        TempData["Success"] = _localizer["SettingsSaved"].Value + _localizer["SymbolDot"].Value;
        TempData["SuccessDuration"] = 4000;

        return RedirectToAction(nameof(Index), new {
            returnUrl = model.ReturnUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetToDefaults(string? returnUrl) {
        await _settingsService.ResetToDefaultsAsync();

        TempData["Success"] = _localizer["SettingsDefaultsRestored"].Value + _localizer["SymbolDot"].Value;
        TempData["SuccessDuration"] = 6000;

        var safeReturnUrl = string.IsNullOrWhiteSpace(returnUrl)
            ? null
            : Url.IsLocalUrl(returnUrl) ? returnUrl : null;

        return RedirectToAction(nameof(Index), new {
            returnUrl = safeReturnUrl
        });
    }
}