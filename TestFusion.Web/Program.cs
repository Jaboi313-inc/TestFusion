using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TestFusion.Core.Interfaces;
using TestFusion.Data;
using TestFusion.SyncService.Models;
using TestFusion.SyncService.Services;
using TestFusion.Web.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

var postgresConnection = builder.Configuration.GetConnectionString("PostGresConnection")
    ?? throw new InvalidOperationException("Connection string 'PostGresConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(postgresConnection));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddSingleton<JSONService>();
builder.Services.AddSingleton<IPlaywright, PlaywrightService>();
builder.Services.AddScoped<ISyncService, SyncService>();

builder.Services.Configure<SiteSettings>(
    builder.Configuration.GetSection("SiteSettings"));

builder.Services.Configure<AuthSettings>(
    builder.Configuration.GetSection("AuthSettings"));

builder.Services.Configure<Intervals>(
    builder.Configuration.GetSection("Intervals"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

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
