using TestFusion.Core.Interfaces;
using TestFusion.Core.Models;
using TestFusion.Core.Models.WebModels;

namespace TestFusion.Tests.Helpers;

public class TestSettingsService : ISettingsService
{
    public ApplicationSettings Settings { get; set; } = new()
    {
        Id = 1
    };

    public CarbonXCredentials Credentials { get; set; } = new();

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
        Settings.ShowDebugInformation = model.ShowDebugInformation;
        Settings.GroupTestsByBaseName = model.GroupTestsByBaseName;
        Settings.MatchCustomerName = model.MatchCustomerName;
        Settings.MatchCustomerPhone = model.MatchCustomerPhone;
        Settings.MatchCustomerMail = model.MatchCustomerMail;
        Settings.MatchCustomerNotes = model.MatchCustomerNotes;
        Settings.PdfMaxInjectorsPerPage = model.PdfMaxInjectorsPerPage;

        return Task.CompletedTask;
    }

    public Task ResetToDefaultsAsync()
    {
        Settings.ResetToDefaults();

        return Task.CompletedTask;
    }

    public Task<CarbonXCredentials> GetCarbonXCredentialsAsync()
    {
        return Task.FromResult(Credentials);
    }
}