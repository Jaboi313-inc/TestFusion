using TestFusion.Core.Interfaces;
using TestFusion.Core.Models;
using TestFusion.Core.Models.WebModels;

namespace TestFusion.Tests.Helpers;

public class TestSettingsService : ISettingsService {
    public ApplicationSettings Settings { get; set; } = new() { Id = 1 };
    public CarbonXCredentials Credentials { get; set; } = new();
    public Dictionary<string, PageNavigationSettings> NavigationPages { get; set; } = PageNavigationDefaults.Create();

    public Task<ApplicationSettings> GetAsync() {
        return Task.FromResult(Settings);
    }

    public Task<SettingsViewModel> GetForEditAsync() {
        return Task.FromResult(new SettingsViewModel {
            ShowDebugInformation = Settings.ShowDebugInformation,
            GroupTestsByBaseName = Settings.GroupTestsByBaseName,
            MatchCustomerName = Settings.MatchCustomerName,
            MatchCustomerPhone = Settings.MatchCustomerPhone,
            MatchCustomerMail = Settings.MatchCustomerMail,
            MatchCustomerNotes = Settings.MatchCustomerNotes,
            PdfMaxInjectorsPerPage = Settings.PdfMaxInjectorsPerPage,
            NavigationPages = CloneNavigationPages(NavigationPages)
        });
    }

    public Task UpdateAsync(SettingsViewModel model) {
        Settings.GroupTestsByBaseName = model.GroupTestsByBaseName;
        Settings.MatchCustomerName = model.MatchCustomerName;
        Settings.MatchCustomerPhone = model.MatchCustomerPhone;
        Settings.MatchCustomerMail = model.MatchCustomerMail;
        Settings.MatchCustomerNotes = model.MatchCustomerNotes;
        Settings.PdfMaxInjectorsPerPage = model.PdfMaxInjectorsPerPage;

        foreach (var navigationPage in model.NavigationPages) {
            if (navigationPage.Key.StartsWith("Debug/", StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            NavigationPages[navigationPage.Key] = CopyNavigationSettings(navigationPage.Value);
        }

        return Task.CompletedTask;
    }

    public Task UpdateDebugAsync(bool showDebugInformation, Dictionary<string, PageNavigationSettings> navigationPages) {
        Settings.ShowDebugInformation = showDebugInformation;

        foreach (var navigationPage in navigationPages) {
            if (!navigationPage.Key.StartsWith("Debug/", StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            NavigationPages[navigationPage.Key] = CopyNavigationSettings(navigationPage.Value);
        }

        return Task.CompletedTask;
    }

    public Task<PageNavigationSettings> GetPageNavigationAsync(string pageKey) {
        return Task.FromResult(
            NavigationPages.TryGetValue(pageKey, out var settings)
                ? CopyNavigationSettings(settings)
                : new PageNavigationSettings());
    }

    public Task ResetToDefaultsAsync() {
        var debugNavigationPages = NavigationPages
            .Where(page => page.Key.StartsWith("Debug/", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(page => page.Key, page => CopyNavigationSettings(page.Value), StringComparer.OrdinalIgnoreCase);

        var showDebugInformation = Settings.ShowDebugInformation;

        Settings.ResetToDefaults();
        Settings.ShowDebugInformation = showDebugInformation;

        NavigationPages = PageNavigationDefaults.Create();

        foreach (var debugNavigationPage in debugNavigationPages) {
            NavigationPages[debugNavigationPage.Key] = debugNavigationPage.Value;
        }

        return Task.CompletedTask;
    }

    public Task<CarbonXCredentials> GetCarbonXCredentialsAsync() {
        return Task.FromResult(Credentials);
    }

    private static Dictionary<string, PageNavigationSettings> CloneNavigationPages(Dictionary<string, PageNavigationSettings> source) {
        return source.ToDictionary(
            page => page.Key,
            page => CopyNavigationSettings(page.Value),
            StringComparer.OrdinalIgnoreCase);
    }

    private static PageNavigationSettings CopyNavigationSettings(PageNavigationSettings settings) {
        return new PageNavigationSettings {
            ShowBackToLeft = settings.ShowBackToLeft,
            ShowBackToTop = settings.ShowBackToTop,
            ShowBackToStart = settings.ShowBackToStart
        };
    }
}