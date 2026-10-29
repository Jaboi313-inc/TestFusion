namespace TestFusion.Core.Models;

public class ApplicationSettings
{
    public int Id { get; set; }

    public bool ShowDebugInformation { get; set; } = false;
    public bool GroupTestsByBaseName { get; set; } = true;

    public bool MatchCustomerName { get; set; } = true;
    public bool MatchCustomerPhone { get; set; } = true;
    public bool MatchCustomerMail { get; set; } = true;
    public bool MatchCustomerNotes { get; set; } = true;

    public int PdfMaxInjectorsPerPage { get; set; } = 8;

    public string? CarbonXEmailEncrypted { get; set; }
    public string? CarbonXPasswordEncrypted { get; set; }
}