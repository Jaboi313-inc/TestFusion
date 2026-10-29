using System.ComponentModel.DataAnnotations;

namespace TestFusion.Core.Models.WebModels;

public class SettingsViewModel
{
    public string? ReturnUrl { get; set; }
    public bool ShowDebugInformation { get; set; }
    public bool GroupTestsByBaseName { get; set; }

    public bool MatchCustomerName { get; set; }
    public bool MatchCustomerPhone { get; set; }
    public bool MatchCustomerMail { get; set; }
    public bool MatchCustomerNotes { get; set; }

    [Range(1, 20, ErrorMessage = "Het aantal verstuivers moet tussen 1 en 20 liggen.")]
    public int PdfMaxInjectorsPerPage { get; set; }

    public string? CarbonXEmail { get; set; }
    public bool HasCarbonXPassword { get; set; }
    public string? NewCarbonXPassword { get; set; }
}