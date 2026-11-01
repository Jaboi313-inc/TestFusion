using TestFusion.Core.Models;
using TestFusion.Core.Models.WebModels;

namespace TestFusion.Core.Interfaces;

public interface ISettingsService
{
    Task<ApplicationSettings> GetAsync();
    Task<SettingsViewModel> GetForEditAsync();
    Task UpdateAsync(SettingsViewModel model);
    Task UpdateDebugAsync(bool showDebugInformation);
    Task ResetToDefaultsAsync();
    Task<CarbonXCredentials> GetCarbonXCredentialsAsync();
}