using TestFusion.Core.Interfaces;
using TestFusion.Core.Models;
using TestFusion.Core.Models.WebModels;

namespace TestFusion.Tests.Helpers;

public class TestSettingsService : ISettingsService
{
    public ApplicationSettings Settings { get; set; } = new() { Id = 1 };
    public CarbonXCredentials Credentials { get; set; } = new();
    public Dictionary<string, PageNavigationSettings> NavigationPages { get; set; } =
        PageNavigationDefaults.Create();

    public Task<ApplicationSettings> GetAsync() => Task.FromResult(Settings);

    public Task<SettingsViewModel> GetForEditAsync()
    {
        return Task.FromResult(new SettingsViewModel
        {
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

    public Task UpdateAsync(SettingsViewModel model)
    {
        Settings.GroupTestsByBaseName = model.GroupTestsByBaseName;
        Settings.MatchCustomerName = model.MatchCustomerName;
        Settings.MatchCustomerPhone = model.MatchCustomerPhone;
        Settings.MatchCustomerMail = model.MatchCustomerMail;
        Settings.MatchCustomerNotes = model.MatchCustomerNotes;
        Settings.PdfMaxInjectorsPerPage = model.PdfMaxInjectorsPerPage;
        NavigationPages = CloneNavigationPages(model.NavigationPages);

        return Task.CompletedTask;
    }

    public Task UpdateDebugAsync(bool showDebugInformation)
    {
        Settings.ShowDebugInformation = showDebugInformation;
        return Task.CompletedTask;
    }

    public Task<PageNavigationSettings> GetPageNavigationAsync(string pageKey)
    {
        return Task.FromResult(
            NavigationPages.TryGetValue(pageKey, out var settings)
                ? settings
                : new PageNavigationSettings());
    }

    public Task ResetToDefaultsAsync()
    {
        Settings.ResetToDefaults();
        NavigationPages = PageNavigationDefaults.Create();
        return Task.CompletedTask;
    }

    public Task<CarbonXCredentials> GetCarbonXCredentialsAsync() =>
        Task.FromResult(Credentials);

    private static Dictionary<string, PageNavigationSettings> CloneNavigationPages(
        Dictionary<string, PageNavigationSettings> source)
    {
        return source.ToDictionary(
            page => page.Key,
            page => new PageNavigationSettings
            {
                ShowBackToLeft = page.Value.ShowBackToLeft,
                ShowBackToTop = page.Value.ShowBackToTop,
                ShowBackToStart = page.Value.ShowBackToStart
            },
            StringComparer.OrdinalIgnoreCase);
    }
}