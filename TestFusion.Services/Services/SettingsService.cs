using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TestFusion.Core.Interfaces;
using TestFusion.Core.Models;
using TestFusion.Core.Models.WebModels;
using TestFusion.Data;

namespace TestFusion.Services.Services;

public class SettingsService : ISettingsService {
    private static readonly JsonSerializerOptions NavigationJsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly AppDbContext _dbContext;
    private readonly ISecretProtector _secretProtector;
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(AppDbContext dbContext, ISecretProtector secretProtector, ILogger<SettingsService> logger) {
        _dbContext = dbContext;
        _secretProtector = secretProtector;
        _logger = logger;
    }

    public async Task<ApplicationSettings> GetAsync() {
        return await GetSettingsAsync();
    }

    public async Task<SettingsViewModel> GetForEditAsync() {
        var settings = await GetSettingsAsync();
        return new SettingsViewModel {
            ShowDebugInformation = settings.ShowDebugInformation,
            GroupTestsByBaseName = settings.GroupTestsByBaseName,
            ShowFullySkippedTests = settings.ShowFullySkippedTests,
            MatchCustomerName = settings.MatchCustomerName,
            MatchCustomerPhone = settings.MatchCustomerPhone,
            MatchCustomerMail = settings.MatchCustomerMail,
            MatchCustomerNotes = settings.MatchCustomerNotes,
            PdfDefaultLayoutMode = settings.PdfDefaultLayoutMode,
            PdfMaxInjectorsPerPage = settings.PdfMaxInjectorsPerPage,
            NavigationPages = GetNavigationPages(settings),
            CarbonXEmail = string.IsNullOrWhiteSpace(settings.CarbonXEmailEncrypted) ? null : _secretProtector.Unprotect(settings.CarbonXEmailEncrypted),
            HasCarbonXPassword = !string.IsNullOrWhiteSpace(settings.CarbonXPasswordEncrypted)
        };
    }

    public async Task UpdateAsync(SettingsViewModel model) {
        var settings = await GetSettingsAsync();
        LogSettingChange("Group tests by base name", settings.GroupTestsByBaseName, model.GroupTestsByBaseName);
        LogSettingChange("Show fully skipped tests", settings.ShowFullySkippedTests, model.ShowFullySkippedTests);
        LogSettingChange("Match customer name", settings.MatchCustomerName, model.MatchCustomerName);
        LogSettingChange("Match customer phone", settings.MatchCustomerPhone, model.MatchCustomerPhone);
        LogSettingChange("Match customer email", settings.MatchCustomerMail, model.MatchCustomerMail);
        LogSettingChange("Match customer notes", settings.MatchCustomerNotes, model.MatchCustomerNotes);
        LogSettingChange("PDF default layout mode", settings.PdfDefaultLayoutMode, model.PdfDefaultLayoutMode);
        LogSettingChange("PDF injectors per page", settings.PdfMaxInjectorsPerPage, model.PdfMaxInjectorsPerPage);

        settings.GroupTestsByBaseName = model.GroupTestsByBaseName;
        settings.ShowFullySkippedTests = model.ShowFullySkippedTests;
        settings.MatchCustomerName = model.MatchCustomerName;
        settings.MatchCustomerPhone = model.MatchCustomerPhone;
        settings.MatchCustomerMail = model.MatchCustomerMail;
        settings.MatchCustomerNotes = model.MatchCustomerNotes;
        settings.PdfDefaultLayoutMode = model.PdfDefaultLayoutMode;
        settings.PdfMaxInjectorsPerPage = model.PdfMaxInjectorsPerPage;

        var navigationPages = GetNavigationPages(settings);

        foreach (var navigationPage in model.NavigationPages) {
            if (navigationPage.Key.StartsWith("Debug/", StringComparison.OrdinalIgnoreCase) || !navigationPages.TryGetValue(navigationPage.Key, out var current)) {
                continue;
            }

            LogNavigationChanges(navigationPage.Key, current, navigationPage.Value);
            navigationPages[navigationPage.Key] = CopyNavigationSettings(navigationPage.Value);
        }

        settings.PageNavigationSettingsJson = SerializeNavigationPages(navigationPages);

        var oldEmail = string.IsNullOrWhiteSpace(settings.CarbonXEmailEncrypted) ? null : _secretProtector.Unprotect(settings.CarbonXEmailEncrypted);
        if (!string.Equals(oldEmail, model.CarbonXEmail, StringComparison.Ordinal)) {
            _logger.LogInformation("CarbonX email setting changed. Configured: {Configured}", !string.IsNullOrWhiteSpace(model.CarbonXEmail));
        }

        settings.CarbonXEmailEncrypted = string.IsNullOrWhiteSpace(model.CarbonXEmail) ? null : _secretProtector.Protect(model.CarbonXEmail);

        if (!string.IsNullOrWhiteSpace(model.NewCarbonXPassword)) {
            _logger.LogInformation("CarbonX password setting changed");
            settings.CarbonXPasswordEncrypted = _secretProtector.Protect(model.NewCarbonXPassword);
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Application settings saved");
    }

    public async Task UpdateDebugAsync(bool showDebugInformation, Dictionary<string, PageNavigationSettings> navigationPages) {
        var settings = await GetSettingsAsync();
        LogSettingChange("Show debug information", settings.ShowDebugInformation, showDebugInformation);
        settings.ShowDebugInformation = showDebugInformation;

        var currentNavigationPages = GetNavigationPages(settings);
        foreach (var navigationPage in navigationPages) {
            if (!navigationPage.Key.StartsWith("Debug/", StringComparison.OrdinalIgnoreCase) || !currentNavigationPages.TryGetValue(navigationPage.Key, out var current)) {
                continue;
            }

            LogNavigationChanges(navigationPage.Key, current, navigationPage.Value);
            currentNavigationPages[navigationPage.Key] = CopyNavigationSettings(navigationPage.Value);
        }

        settings.PageNavigationSettingsJson = SerializeNavigationPages(currentNavigationPages);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Debug settings saved");
    }

    public async Task<PageNavigationSettings> GetPageNavigationAsync(string pageKey) {
        var navigationPages = GetNavigationPages(await GetSettingsAsync());
        return navigationPages.TryGetValue(pageKey, out var pageSettings) ? pageSettings : new PageNavigationSettings();
    }

    public async Task ResetToDefaultsAsync() {
        var settings = await GetSettingsAsync();
        var currentNavigationPages = GetNavigationPages(settings);
        var debugNavigationPages = currentNavigationPages.Where(page => page.Key.StartsWith("Debug/", StringComparison.OrdinalIgnoreCase)).ToDictionary(page => page.Key, page => CopyNavigationSettings(page.Value), StringComparer.OrdinalIgnoreCase);
        var showDebugInformation = settings.ShowDebugInformation;

        settings.ResetToDefaults();
        settings.ShowDebugInformation = showDebugInformation;

        var defaultNavigationPages = PageNavigationDefaults.Create();
        foreach (var debugNavigationPage in debugNavigationPages) {
            defaultNavigationPages[debugNavigationPage.Key] = debugNavigationPage.Value;
        }

        settings.PageNavigationSettingsJson = SerializeNavigationPages(defaultNavigationPages);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Application settings reset to defaults. Debug settings were preserved");
    }

    public async Task<CarbonXCredentials> GetCarbonXCredentialsAsync() {
        var settings = await GetSettingsAsync();
        return new CarbonXCredentials {
            Email = string.IsNullOrWhiteSpace(settings.CarbonXEmailEncrypted) ? string.Empty : _secretProtector.Unprotect(settings.CarbonXEmailEncrypted),
            Password = string.IsNullOrWhiteSpace(settings.CarbonXPasswordEncrypted) ? string.Empty : _secretProtector.Unprotect(settings.CarbonXPasswordEncrypted)
        };
    }

    private async Task<ApplicationSettings> GetSettingsAsync() {
        var settings = await _dbContext.ApplicationSettings.FirstOrDefaultAsync(x => x.Id == 1);

        if (settings != null) {
            if (string.IsNullOrWhiteSpace(settings.PageNavigationSettingsJson)) {
                settings.PageNavigationSettingsJson = SerializeNavigationPages(PageNavigationDefaults.Create());
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Navigation settings were missing and have been initialized with defaults");
            }

            return settings;
        }

        settings = new ApplicationSettings { Id = 1, PageNavigationSettingsJson = SerializeNavigationPages(PageNavigationDefaults.Create()) };
        _dbContext.ApplicationSettings.Add(settings);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Application settings record created with default values");
        return settings;
    }

    private static Dictionary<string, PageNavigationSettings> GetNavigationPages(ApplicationSettings settings) {
        var defaults = PageNavigationDefaults.Create();
        if (string.IsNullOrWhiteSpace(settings.PageNavigationSettingsJson)) {
            return defaults;
        }

        try {
            var stored = JsonSerializer.Deserialize<Dictionary<string, PageNavigationSettings>>(settings.PageNavigationSettingsJson, NavigationJsonOptions);
            if (stored == null) {
                return defaults;
            }

            var result = new Dictionary<string, PageNavigationSettings>(stored, StringComparer.OrdinalIgnoreCase);
            foreach (var page in defaults) {
                if (!result.ContainsKey(page.Key)) {
                    result[page.Key] = page.Value;
                }
            }

            return result;
        }
        catch (JsonException) {
            return defaults;
        }
    }

    private void LogSettingChange<T>(string setting, T oldValue, T newValue) {
        if (EqualityComparer<T>.Default.Equals(oldValue, newValue)) return;
        _logger.LogInformation("Setting '{Setting}' changed from {OldValue} to {NewValue}", setting, GetReadableValue(oldValue), GetReadableValue(newValue));
    }

    private void LogNavigationChanges(string pageKey, PageNavigationSettings current, PageNavigationSettings updated) {
        LogNavigationChange(pageKey, "Back to left", current.ShowBackToLeft, updated.ShowBackToLeft);
        LogNavigationChange(pageKey, "Back to top", current.ShowBackToTop, updated.ShowBackToTop);
        LogNavigationChange(pageKey, "Back to start", current.ShowBackToStart, updated.ShowBackToStart);
    }

    private void LogNavigationChange(string pageKey, string setting, bool oldValue, bool newValue) {
        if (oldValue == newValue) return;
        _logger.LogInformation("Navigation setting '{Setting}' on {Page} changed from {OldValue} to {NewValue}", setting, pageKey, GetReadableValue(oldValue), GetReadableValue(newValue));
    }

    private static object? GetReadableValue<T>(T value) {
        if (value is bool booleanValue) return booleanValue ? "enabled" : "disabled";
        return value;
    }

    private static PageNavigationSettings CopyNavigationSettings(PageNavigationSettings settings) {
        return new PageNavigationSettings { ShowBackToLeft = settings.ShowBackToLeft, ShowBackToTop = settings.ShowBackToTop, ShowBackToStart = settings.ShowBackToStart };
    }

    private static string SerializeNavigationPages(Dictionary<string, PageNavigationSettings> navigationPages) {
        return JsonSerializer.Serialize(navigationPages, NavigationJsonOptions);
    }
}
