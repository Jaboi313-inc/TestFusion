using Microsoft.EntityFrameworkCore;
using TestFusion.Core.Interfaces;
using TestFusion.Core.Models;
using TestFusion.Core.Models.WebModels;
using TestFusion.Data;

namespace TestFusion.Services.Services;

public class SettingsService : ISettingsService
{
    private readonly AppDbContext _dbContext;
    private readonly ISecretProtector _secretProtector;

    public SettingsService(AppDbContext dbContext, ISecretProtector secretProtector)
    {
        _dbContext = dbContext;
        _secretProtector = secretProtector;
    }

    public async Task<ApplicationSettings> GetAsync()
    {
        return await GetSettingsAsync();
    }

    public async Task<SettingsViewModel> GetForEditAsync()
    {
        var settings = await GetSettingsAsync();

        return new SettingsViewModel
        {
            ShowDebugInformation = settings.ShowDebugInformation,
            GroupTestsByBaseName = settings.GroupTestsByBaseName,
            MatchCustomerName = settings.MatchCustomerName,
            MatchCustomerPhone = settings.MatchCustomerPhone,
            MatchCustomerMail = settings.MatchCustomerMail,
            MatchCustomerNotes = settings.MatchCustomerNotes,
            PdfMaxInjectorsPerPage = settings.PdfMaxInjectorsPerPage,
            CarbonXEmail = string.IsNullOrWhiteSpace(settings.CarbonXEmailEncrypted)
                ? null
                : _secretProtector.Unprotect(settings.CarbonXEmailEncrypted),
            HasCarbonXPassword = !string.IsNullOrWhiteSpace(settings.CarbonXPasswordEncrypted)
        };
    }

    public async Task UpdateAsync(SettingsViewModel model)
    {
        var settings = await GetSettingsAsync();

        settings.ShowDebugInformation = model.ShowDebugInformation;
        settings.GroupTestsByBaseName = model.GroupTestsByBaseName;
        settings.MatchCustomerName = model.MatchCustomerName;
        settings.MatchCustomerPhone = model.MatchCustomerPhone;
        settings.MatchCustomerMail = model.MatchCustomerMail;
        settings.MatchCustomerNotes = model.MatchCustomerNotes;
        settings.PdfMaxInjectorsPerPage = model.PdfMaxInjectorsPerPage;

        settings.CarbonXEmailEncrypted = string.IsNullOrWhiteSpace(model.CarbonXEmail)
            ? null
            : _secretProtector.Protect(model.CarbonXEmail);

        if (!string.IsNullOrWhiteSpace(model.NewCarbonXPassword))
            settings.CarbonXPasswordEncrypted = _secretProtector.Protect(model.NewCarbonXPassword);

        await _dbContext.SaveChangesAsync();
    }

    public async Task ResetToDefaultsAsync()
    {
        var settings = await GetSettingsAsync();

        settings.ResetToDefaults();

        await _dbContext.SaveChangesAsync();
    }

    public async Task<CarbonXCredentials> GetCarbonXCredentialsAsync()
    {
        var settings = await GetSettingsAsync();

        return new CarbonXCredentials
        {
            Email = string.IsNullOrWhiteSpace(settings.CarbonXEmailEncrypted)
                ? string.Empty
                : _secretProtector.Unprotect(settings.CarbonXEmailEncrypted),
            Password = string.IsNullOrWhiteSpace(settings.CarbonXPasswordEncrypted)
                ? string.Empty
                : _secretProtector.Unprotect(settings.CarbonXPasswordEncrypted)
        };
    }

    private async Task<ApplicationSettings> GetSettingsAsync()
    {
        var settings = await _dbContext.ApplicationSettings.FirstOrDefaultAsync(x => x.Id == 1);

        if (settings != null)
            return settings;

        settings = new ApplicationSettings
        {
            Id = 1
        };

        _dbContext.ApplicationSettings.Add(settings);
        await _dbContext.SaveChangesAsync();

        return settings;
    }
}