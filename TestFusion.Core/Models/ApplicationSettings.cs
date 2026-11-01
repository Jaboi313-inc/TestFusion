namespace TestFusion.Core.Models;

public class ApplicationSettings
{
    private const bool DefaultShowDebugInformation = false;
    private const bool DefaultGroupTestsByBaseName = true;
    private const bool DefaultMatchCustomerName = true;
    private const bool DefaultMatchCustomerPhone = true;
    private const bool DefaultMatchCustomerMail = true;
    private const bool DefaultMatchCustomerNotes = true;
    private const int DefaultPdfMaxInjectorsPerPage = 10;

    public ApplicationSettings()
    {
        ResetToDefaults();
    }

    public int Id { get; set; }
    public bool ShowDebugInformation { get; set; }
    public bool GroupTestsByBaseName { get; set; }
    public bool MatchCustomerName { get; set; }
    public bool MatchCustomerPhone { get; set; }
    public bool MatchCustomerMail { get; set; }
    public bool MatchCustomerNotes { get; set; }
    public int PdfMaxInjectorsPerPage { get; set; }
    public string? CarbonXEmailEncrypted { get; set; }
    public string? CarbonXPasswordEncrypted { get; set; }

    public void ResetToDefaults()
    {
        ShowDebugInformation = DefaultShowDebugInformation;
        GroupTestsByBaseName = DefaultGroupTestsByBaseName;
        MatchCustomerName = DefaultMatchCustomerName;
        MatchCustomerPhone = DefaultMatchCustomerPhone;
        MatchCustomerMail = DefaultMatchCustomerMail;
        MatchCustomerNotes = DefaultMatchCustomerNotes;
        PdfMaxInjectorsPerPage = DefaultPdfMaxInjectorsPerPage;
    }
}