using TestFusion.Core.Interfaces;
using TestFusion.Core.Models;
using TestFusion.Core.Models.WebModels;

namespace TestFusion.Tests.Helpers;

public class TestSettingsService : ISettingsService
{
    public ApplicationSettings Settings { get; set; } = new()
    {
        Id = 1,
        ShowDebugInformation = false,
        GroupTestsByBaseName = true,
        MatchCustomerName = true,
        MatchCustomerPhone = true,
        MatchCustomerMail = true,
        MatchCustomerNotes = true,
        PdfMaxInjectorsPerPage = 8
    };

    public Task<ApplicationSettings> GetAsync()
    {
        return Task.FromResult(Settings);
    }

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
            PdfMaxInjectorsPerPage = Settings.PdfMaxInjectorsPerPage
        });
    }

    public Task UpdateAsync(SettingsViewModel model)
    {
        return Task.CompletedTask;
    }

    public Task<CarbonXCredentials> GetCarbonXCredentialsAsync()
    {
        return Task.FromResult(new CarbonXCredentials());
    }
}