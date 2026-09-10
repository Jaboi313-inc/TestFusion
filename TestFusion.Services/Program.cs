using Microsoft.EntityFrameworkCore;
using TestFusion.Core.Interfaces;
using TestFusion.Data;
using TestFusion.Services;
using TestFusion.Services.Models;
using TestFusion.Services.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<SiteSettings>(
    builder.Configuration.GetSection("SiteSettings"));

builder.Services.Configure<AuthSettings>(
    builder.Configuration.GetSection("AuthSettings"));

builder.Services.Configure<Intervals>(
    builder.Configuration.GetSection("Intervals"));

builder.Services.AddSingleton<JSONService>();
builder.Services.AddScoped<IPlaywright, PlaywrightService>();
builder.Services.AddScoped<ISyncService, SyncService>();

builder.Services.AddHostedService<Worker>();

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("PostGresConnection")));

var host = builder.Build();
host.Run();