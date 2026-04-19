using TestFusion.Core.Interfaces;
using TestFusion.SyncService.Models;
using TestFusion.SyncService.Services;

var builder = WebApplication.CreateBuilder(args);

#region CONFIG
builder.Services.Configure<SiteSettings>(
    builder.Configuration.GetSection("SiteSettings"));

builder.Services.Configure<AuthSettings>(
    builder.Configuration.GetSection("Auth"));

builder.Services.Configure<Intervals>(
    builder.Configuration.GetSection("Intervals"));
#endregion

#region SERVICES
builder.Services.AddScoped<IPlaywrightInterface, PlaywrightService>();
builder.Services.AddSingleton<JSONService>();
#endregion

#region API
var app = builder.Build();

app.MapGet("/analysis", async (IPlaywrightInterface service) =>
{
    return await service.GetAllIDs();
});

app.MapGet("/analysis/{id}", async (IPlaywrightInterface service, string id) =>
{
    return await service.GetDataForId(id);
});
#endregion

app.Run();