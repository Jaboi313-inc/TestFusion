using System.ComponentModel.DataAnnotations.Schema;
using TestFusion.Core.Enums;

namespace TestFusion.Core.Models;

public class ApplicationSettings
{
    private const bool DefaultShowDebugInformation = false;
    private const bool DefaultGroupTestsByBaseName = true;
    private const bool DefaultShowFullySkippedTests = true;
    private const bool DefaultMatchCustomerName = true;
    private const bool DefaultMatchCustomerPhone = true;
    private const bool DefaultMatchCustomerMail = true;
    private const bool DefaultMatchCustomerNotes = true;
    private const PdfLayoutModeEnum DefaultPdfLayoutMode = PdfLayoutModeEnum.KeepTestTogether;

    public ApplicationSettings() {
        ResetToDefaults();
    }

    public int Id { get; set; }
    public bool ShowDebugInformation { get; set; }
    public bool GroupTestsByBaseName { get; set; }
    public bool ShowFullySkippedTests { get; set; }
    public bool MatchCustomerName { get; set; }
    public bool MatchCustomerPhone { get; set; }
    public bool MatchCustomerMail { get; set; }
    public bool MatchCustomerNotes { get; set; }
    public PdfLayoutModeEnum PdfDefaultLayoutMode { get; set; }
    public int PdfMaxInjectorsPerPage { get; set; }

    [Column(TypeName = "jsonb")]
    public string? PageNavigationSettingsJson { get; set; }

    public string? CarbonXEmailEncrypted { get; set; }
    public string? CarbonXPasswordEncrypted { get; set; }

    public void ResetToDefaults() {
        ShowDebugInformation = DefaultShowDebugInformation;
        GroupTestsByBaseName = DefaultGroupTestsByBaseName;
        ShowFullySkippedTests = DefaultShowFullySkippedTests;
        MatchCustomerName = DefaultMatchCustomerName;
        MatchCustomerPhone = DefaultMatchCustomerPhone;
        MatchCustomerMail = DefaultMatchCustomerMail;
        MatchCustomerNotes = DefaultMatchCustomerNotes;
        PdfDefaultLayoutMode = DefaultPdfLayoutMode;
        PdfMaxInjectorsPerPage = DefaultPdfMaxInjectorsPerPage;
        PageNavigationSettingsJson = null;
    }
}