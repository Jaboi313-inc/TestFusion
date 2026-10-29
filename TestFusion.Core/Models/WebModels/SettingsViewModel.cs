namespace TestFusion.Core.Models.WebModels;

public class SettingsViewModel
{
    public bool ShowDebugInformation { get; set; }
    public bool GroupTestsByBaseName { get; set; }

    public bool MatchCustomerName { get; set; }
    public bool MatchCustomerPhone { get; set; }
    public bool MatchCustomerMail { get; set; }
    public bool MatchCustomerNotes { get; set; }

    public int PdfMaxInjectorsPerPage { get; set; }

    public string CarbonXEmail { get; set; } = string.Empty;
    public bool HasCarbonXPassword { get; set; }
    public string? NewCarbonXPassword { get; set; }
}