namespace TestFusion.Web.Models;

public class DebugLogEntry
{
    public DateTimeOffset Timestamp { get; set; }

    public string Application { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string SourceContext { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }

    public Dictionary<string, string> Properties { get; set; } = [];
}
