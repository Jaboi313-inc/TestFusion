using TestFusion.Core.Models;

namespace TestFusion.Web.Models;

public class DebugViewModel
{
    public List<TestReportModel> TestReports { get; set; } = [];
    public List<DebugLogEntry> Logs { get; set; } = [];

    public string JsonId { get; set; } = string.Empty;

    public string? RawJson { get; set; }
    public string? ProcessedJson { get; set; }
    public string? StoredJson { get; set; }

    public bool StoredJsonFound { get; set; }
    public bool? ProcessedMatchesStored { get; set; }

    public string? JsonError { get; set; }
}
