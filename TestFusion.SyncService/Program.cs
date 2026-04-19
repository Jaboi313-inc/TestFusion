using TestFusion.SyncService;
using TestFusion.SyncService.Models;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<SiteSettings>(
    builder.Configuration.GetSection("SiteSettings"));

builder.Services.Configure<Intervals>(
    builder.Configuration.GetSection("Intervals"));

builder.Services.Configure<AuthSettings>(
    builder.Configuration.GetSection("Auth"));

builder.Services.AddSingleton<PlaywrightService>();
builder.Services.AddSingleton<JSONService>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();