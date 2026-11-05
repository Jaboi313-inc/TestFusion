using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Console;
using Serilog;
using Serilog.Formatting.Compact;
using System.Globalization;
using TestFusion.Core;
using TestFusion.Core.Interfaces;
using TestFusion.Data;
using TestFusion.Services.Models;
using TestFusion.Services.Services;
using TestFusion.Web.Data;
using TestFusion.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

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
        Path.Combine(logDirectory, "web-.jsonl"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: Math.Max(loggingSettings.RetentionDays, 1),
        flushToDiskInterval: TimeSpan.FromSeconds(1))
    .CreateLogger();

builder.Logging.AddSerilog(fileLogger, dispose: true);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

var postgresConnection = builder.Configuration.GetConnectionString("PostGresConnection")
    ?? throw new InvalidOperationException("Connection string 'PostGresConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(postgresConnection));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddDataProtection()
    .SetApplicationName("TestFusion")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

builder.Services.AddScoped<ISecretProtector, SecretProtector>();
builder.Services.AddScoped<ISettingsService, SettingsService>();

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources";
});

builder.Services
    .AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options => {
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(SharedResource));
    });

builder.Services.AddRazorPages();

builder.Services.AddSingleton<ITimeZoneService, TimeZoneService>();
builder.Services.AddSingleton<JSONService>();
builder.Services.AddSingleton<IPlaywright, PlaywrightService>();
builder.Services.AddScoped<ISyncService, SyncService>();

builder.Services.Configure<SiteSettings>(
    builder.Configuration.GetSection("SiteSettings"));

builder.Services.Configure<Intervals>(
    builder.Configuration.GetSection("Intervals"));

builder.Services.Configure<LoggingSettings>(
    builder.Configuration.GetSection("LoggingSettings"));

var app = builder.Build();

if (removedLogFiles > 0) {
    app.Logger.LogInformation("Log retention cleanup removed {RemovedCount} files older than {RetentionDays} days", removedLogFiles, loggingSettings.RetentionDays);
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

var supportedCultures = new[]
{
    new CultureInfo("nl-NL"),
    new CultureInfo("en-US")
};

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("nl-NL"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};

app.UseRequestLocalization(localizationOptions);

app.UseHttpsRedirection();
app.UseRouting();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
    .WithStaticAssets();

app.Run();