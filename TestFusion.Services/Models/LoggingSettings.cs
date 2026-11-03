namespace TestFusion.Services.Models;

public class LoggingSettings {
    public int RetentionDays { get; set; } = 30;
    public SlowOperationThresholds SlowOperationThresholds { get; set; } = new();
}

public class SlowOperationThresholds {
    public int RequestMs { get; set; } = 2000;
    public int AnalysisMs { get; set; } = 2000;
    public int PdfGenerationMs { get; set; } = 3000;
    public int CarbonXMs { get; set; } = 5000;
    public int SynchronizationMs { get; set; } = 10000;
}
