using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Console;
using Serilog;
using Serilog.Formatting.Compact;
using TestFusion.Core.Interfaces;
using TestFusion.Data;
using TestFusion.Services;
using TestFusion.Services.Models;
using TestFusion.Services.Services;

var builder = Host.CreateApplicationBuilder(args);

var dataProtectionKeysPath = Path.GetFullPath(
    Path.Combine(builder.Environment.ContentRootPath, "..", "DataProtectionKeys"));

Directory.CreateDirectory(dataProtectionKeysPath);

builder.Logging.ClearProviders();

builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "dd-MM-yyyy HH:mm:ss ";
    options.UseUtcTimestamp = false;
    options.ColorBehavior = LoggerColorBehavior.Enabled;
});

var logDirectory = Path.GetFullPath(
    Path.Combine(builder.Environment.ContentRootPath, "..", "logs"));

Directory.CreateDirectory(logDirectory);

var loggingSettings = builder.Configuration.GetSection("LoggingSettings").Get<LoggingSettings>() ?? new LoggingSettings();
var removedLogFiles = LogFileMaintenance.Cleanup(logDirectory, loggingSettings.RetentionDays);

var fileLogger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.File(
        new CompactJsonFormatter(),
        Path.Combine(logDirectory, "worker-.jsonl"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: Math.Max(loggingSettings.RetentionDays, 1),
        flushToDiskInterval: TimeSpan.FromSeconds(1))
    .CreateLogger();

builder.Logging.AddSerilog(fileLogger, dispose: true);

builder.Services.Configure<SiteSettings>(
    builder.Configuration.GetSection("SiteSettings"));

builder.Services.Configure<Intervals>(
    builder.Configuration.GetSection("Intervals"));

builder.Services.Configure<LoggingSettings>(
    builder.Configuration.GetSection("LoggingSettings"));

builder.Services
    .AddDataProtection()
    .SetApplicationName("TestFusion")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

builder.Services.AddSingleton<JSONService>();
builder.Services.AddScoped<ISecretProtector, SecretProtector>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddSingleton<IPlaywright, PlaywrightService>();
builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddSingleton<ITimeZoneService, TimeZoneService>();

builder.Services.AddHostedService<Worker>();

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("PostGresConnection")));

var host = builder.Build();

if (removedLogFiles > 0) {
    host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("LogRetention").LogInformation("Log retention cleanup removed {RemovedCount} files older than {RetentionDays} days", removedLogFiles, loggingSettings.RetentionDays);
}

host.Run();