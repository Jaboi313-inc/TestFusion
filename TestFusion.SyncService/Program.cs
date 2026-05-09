using TestFusion.Core.Interfaces;
using TestFusion.SyncService;
using TestFusion.SyncService.Models;
using TestFusion.SyncService.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<SiteSettings>(
    builder.Configuration.GetSection("SiteSettings"));

builder.Services.Configure<AuthSettings>(
    builder.Configuration.GetSection("AuthSettings"));

builder.Services.Configure<Intervals>(
    builder.Configuration.GetSection("Intervals"));

builder.Services.AddSingleton<JSONService>();

builder.Services.AddSingleton<IPlaywrightInterface, PlaywrightService>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();