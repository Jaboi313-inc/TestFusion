namespace TestFusion.Services.Services;

public static class LogFileMaintenance {
    public static int Cleanup(string logDirectory, int retentionDays) {
        if (retentionDays <= 0 || !Directory.Exists(logDirectory)) return 0;

        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        var removedCount = 0;

        foreach (var file in Directory.EnumerateFiles(logDirectory, "*.jsonl")) {
            var fileName = Path.GetFileName(file);
            if (!fileName.StartsWith("web-", StringComparison.OrdinalIgnoreCase) && !fileName.StartsWith("worker-", StringComparison.OrdinalIgnoreCase)) continue;

            try {
                if (File.GetLastWriteTimeUtc(file) >= cutoff) continue;
                File.Delete(file);
                removedCount++;
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        return removedCount;
    }
}
