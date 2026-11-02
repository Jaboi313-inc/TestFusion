using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TestFusion.Core.Interfaces;
using TestFusion.Core.Models;
using TestFusion.Core.Models.WebModels;
using TestFusion.Data;

namespace TestFusion.Services.Services;

public class SettingsService : ISettingsService {
    private static readonly JsonSerializerOptions NavigationJsonOptions = new(JsonSerializerDefaults.Web) {
        WriteIndented = true
    };

    private readonly AppDbContext _dbContext;
    private readonly ISecretProtector _secretProtector;

    public SettingsService(AppDbContext dbContext, ISecretProtector secretProtector) {
        _dbContext = dbContext;
        _secretProtector = secretProtector;
    }

    public async Task<ApplicationSettings> GetAsync() {
        return await GetSettingsAsync();
    }

    public async Task<SettingsViewModel> GetForEditAsync() {
        var settings = await GetSettingsAsync();

        return new SettingsViewModel {
            ShowDebugInformation = settings.ShowDebugInformation,
            GroupTestsByBaseName = settings.GroupTestsByBaseName,
            MatchCustomerName = settings.MatchCustomerName,
            MatchCustomerPhone = settings.MatchCustomerPhone,
            MatchCustomerMail = settings.MatchCustomerMail,
            MatchCustomerNotes = settings.MatchCustomerNotes,
            PdfMaxInjectorsPerPage = settings.PdfMaxInjectorsPerPage,
            NavigationPages = GetNavigationPages(settings),
            CarbonXEmail = string.IsNullOrWhiteSpace(settings.CarbonXEmailEncrypted)
                ? null
                : _secretProtector.Unprotect(settings.CarbonXEmailEncrypted),
            HasCarbonXPassword = !string.IsNullOrWhiteSpace(settings.CarbonXPasswordEncrypted)
        };
    }

    public async Task UpdateAsync(SettingsViewModel model) {
        var settings = await GetSettingsAsync();

        settings.GroupTestsByBaseName = model.GroupTestsByBaseName;
        settings.MatchCustomerName = model.MatchCustomerName;
        settings.MatchCustomerPhone = model.MatchCustomerPhone;
        settings.MatchCustomerMail = model.MatchCustomerMail;
        settings.MatchCustomerNotes = model.MatchCustomerNotes;
        settings.PdfMaxInjectorsPerPage = model.PdfMaxInjectorsPerPage;

        var navigationPages = GetNavigationPages(settings);

        foreach (var navigationPage in model.NavigationPages) {
            if (navigationPage.Key.StartsWith("Debug/", StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            if (!navigationPages.ContainsKey(navigationPage.Key)) {
                continue;
            }

            navigationPages[navigationPage.Key] = CopyNavigationSettings(navigationPage.Value);
        }

        settings.PageNavigationSettingsJson = SerializeNavigationPages(navigationPages);

        settings.CarbonXEmailEncrypted = string.IsNullOrWhiteSpace(model.CarbonXEmail)
            ? null
            : _secretProtector.Protect(model.CarbonXEmail);

        if (!string.IsNullOrWhiteSpace(model.NewCarbonXPassword)) {
            settings.CarbonXPasswordEncrypted = _secretProtector.Protect(model.NewCarbonXPassword);
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateDebugAsync(bool showDebugInformation, Dictionary<string, PageNavigationSettings> navigationPages) {
        var settings = await GetSettingsAsync();
        settings.ShowDebugInformation = showDebugInformation;

        var currentNavigationPages = GetNavigationPages(settings);

        foreach (var navigationPage in navigationPages) {
            if (!navigationPage.Key.StartsWith("Debug/", StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            if (!currentNavigationPages.ContainsKey(navigationPage.Key)) {
                continue;
            }

            currentNavigationPages[navigationPage.Key] = CopyNavigationSettings(navigationPage.Value);
        }

        settings.PageNavigationSettingsJson = SerializeNavigationPages(currentNavigationPages);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<PageNavigationSettings> GetPageNavigationAsync(string pageKey) {
        var navigationPages = GetNavigationPages(await GetSettingsAsync());

        return navigationPages.TryGetValue(pageKey, out var pageSettings)
            ? pageSettings
            : new PageNavigationSettings();
    }

    public async Task ResetToDefaultsAsync() {
        var settings = await GetSettingsAsync();
        var currentNavigationPages = GetNavigationPages(settings);

        var debugNavigationPages = currentNavigationPages
            .Where(page => page.Key.StartsWith("Debug/", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(page => page.Key, page => CopyNavigationSettings(page.Value), StringComparer.OrdinalIgnoreCase);

        var showDebugInformation = settings.ShowDebugInformation;

        settings.ResetToDefaults();
        settings.ShowDebugInformation = showDebugInformation;

        var defaultNavigationPages = PageNavigationDefaults.Create();

        foreach (var debugNavigationPage in debugNavigationPages) {
            defaultNavigationPages[debugNavigationPage.Key] = debugNavigationPage.Value;
        }

        settings.PageNavigationSettingsJson = SerializeNavigationPages(defaultNavigationPages);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<CarbonXCredentials> GetCarbonXCredentialsAsync() {
        var settings = await GetSettingsAsync();

        return new CarbonXCredentials {
            Email = string.IsNullOrWhiteSpace(settings.CarbonXEmailEncrypted)
                ? string.Empty
                : _secretProtector.Unprotect(settings.CarbonXEmailEncrypted),
            Password = string.IsNullOrWhiteSpace(settings.CarbonXPasswordEncrypted)
                ? string.Empty
                : _secretProtector.Unprotect(settings.CarbonXPasswordEncrypted)
        };
    }

    private async Task<ApplicationSettings> GetSettingsAsync() {
        var settings = await _dbContext.ApplicationSettings.FirstOrDefaultAsync(x => x.Id == 1);

        if (settings != null) {
            if (string.IsNullOrWhiteSpace(settings.PageNavigationSettingsJson)) {
                settings.PageNavigationSettingsJson = SerializeNavigationPages(PageNavigationDefaults.Create());
                await _dbContext.SaveChangesAsync();
            }

            return settings;
        }

        settings = new ApplicationSettings {
            Id = 1,
            PageNavigationSettingsJson = SerializeNavigationPages(PageNavigationDefaults.Create())
        };

        _dbContext.ApplicationSettings.Add(settings);
        await _dbContext.SaveChangesAsync();

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

    private static PageNavigationSettings CopyNavigationSettings(PageNavigationSettings settings) {
        return new PageNavigationSettings {
            ShowBackToLeft = settings.ShowBackToLeft,
            ShowBackToTop = settings.ShowBackToTop,
            ShowBackToStart = settings.ShowBackToStart
        };
    }

    private static string SerializeNavigationPages(Dictionary<string, PageNavigationSettings> navigationPages) {
        return JsonSerializer.Serialize(navigationPages, NavigationJsonOptions);
    }
}
