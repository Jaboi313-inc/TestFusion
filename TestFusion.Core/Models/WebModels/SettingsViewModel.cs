using System.ComponentModel.DataAnnotations;
using TestFusion.Core.Enums;
using TestFusion.Core.Models;

namespace TestFusion.Core.Models.WebModels;

public class SettingsViewModel {
    public bool ShowDebugInformation { get; set; }
    public bool GroupTestsByBaseName { get; set; }
    public bool ShowFullySkippedTests { get; set; }
    public bool MatchCustomerName { get; set; }
    public bool MatchCustomerPhone { get; set; }
    public bool MatchCustomerMail { get; set; }
    public bool MatchCustomerNotes { get; set; }
    public PdfLayoutModeEnum PdfDefaultLayoutMode { get; set; }

    [Range(
        ApplicationSettings.MinPdfInjectorsPerPage,
        ApplicationSettings.MaxPdfInjectorsPerPage,
        ErrorMessage = "SettingsPdfMaxInjectorsPerPageRangeError")]
    public int PdfMaxInjectorsPerPage { get; set; }

    public Dictionary<string, PageNavigationSettings> NavigationPages { get; set; } = [];
    public string? CarbonXEmail { get; set; }
    public bool HasCarbonXPassword { get; set; }
    public string? NewCarbonXPassword { get; set; }
    public string? ReturnUrl { get; set; }
}