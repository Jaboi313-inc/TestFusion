using System.ComponentModel.DataAnnotations;
using TestFusion.Core.Models;

namespace TestFusion.Core.Models.WebModels;

public class SettingsViewModel
{
    public bool ShowDebugInformation { get; set; }
    public bool GroupTestsByBaseName { get; set; }
    public bool MatchCustomerName { get; set; }
    public bool MatchCustomerPhone { get; set; }
    public bool MatchCustomerMail { get; set; }
    public bool MatchCustomerNotes { get; set; }

    [Range(1, 10, ErrorMessage = "Het aantal verstuivers moet tussen 1 en 10 liggen.")]
    public int PdfMaxInjectorsPerPage { get; set; }

    public Dictionary<string, PageNavigationSettings> NavigationPages { get; set; } = [];
    public string? CarbonXEmail { get; set; }
    public bool HasCarbonXPassword { get; set; }
    public string? NewCarbonXPassword { get; set; }
    public string? ReturnUrl { get; set; }
}