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

builder.Logging.ClearProviders();

builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "dd-MM-yyyy HH:mm:ss ";
    options.UseUtcTimestamp = false;
    options.ColorBehavior = LoggerColorBehavior.Enabled;
});

var logDirectory = Path.GetFullPath(
    Path.Combine(
        builder.Environment.ContentRootPath,
        "..",
        "logs"));

Directory.CreateDirectory(logDirectory);

var fileLogger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.File(
        new CompactJsonFormatter(),
        Path.Combine(
            logDirectory,
            "worker-.jsonl"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        flushToDiskInterval: TimeSpan.FromSeconds(1))
    .CreateLogger();

builder.Logging.AddSerilog(
    fileLogger,
    dispose: true);

builder.Services.Configure<SiteSettings>(
    builder.Configuration.GetSection("SiteSettings"));

builder.Services.Configure<AuthSettings>(
    builder.Configuration.GetSection("AuthSettings"));

builder.Services.Configure<Intervals>(
    builder.Configuration.GetSection("Intervals"));

builder.Services.AddSingleton<JSONService>();
builder.Services.AddScoped<IPlaywright, PlaywrightService>();
builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddSingleton<ITimeZoneService, TimeZoneService>();

builder.Services.AddHostedService<Worker>();

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("PostGresConnection")));

var host = builder.Build();
host.Run();