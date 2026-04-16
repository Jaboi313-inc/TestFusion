using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using TestFusion.SyncService.Models;

namespace TestFusion.SyncService
{
    using Microsoft.Extensions.Options;
    using System.Runtime;
    using System.Text.Json;
    using TestFusion.SyncService.Models;

    public class PlaywrightService
    {
        private readonly ILogger<PlaywrightService> _logger;
        private readonly SiteSettings _siteSettings;
        private readonly AuthSettings _auth;
        private readonly JSONService _jsonService;

        public PlaywrightService(
        ILogger<PlaywrightService> logger,
        IOptions<SiteSettings> siteSettings,
        IOptions<AuthSettings> auth,
        JSONService jsonService)
        {
            _logger = logger;
            _siteSettings = siteSettings.Value;
            _auth = auth.Value;
            _jsonService = jsonService;
        }
        public async Task Run()
        {
            using var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new()
            {
                Headless = false // Only for testing
            });

            var page = await browser.NewPageAsync();

            await GoToSite(page);
            await Login(page);

            await GetAllIDs(page);
            await GetDataForID(page, "69df3ba6edd88a4a881e6a22");

            await Task.Delay(10000); // Only for testing
            await browser.CloseAsync();
        }
        private async Task GoToSite(IPage page)
        {
            await page.GotoAsync(_siteSettings.BaseUrl);
            _logger.LogInformation("Playwrightservice : Navigation complete");
        }

        private async Task Login(IPage page)
        {
            await page.Locator("#email").FillAsync(_auth.Email);
            await page.Locator("#password").FillAsync(_auth.Password);

            await page.Locator("#btn-login").ClickAsync();

            await page.WaitForURLAsync(_siteSettings.BaseUrl);
            _logger.LogInformation("Playwrightservice : Login successfull");
        }

        private async Task GetAllIDs(IPage page)
        {
            var response = await page.WaitForResponseAsync(r =>
            r.Url.Contains("get-machines-report-list") &&
            r.Request.Method == "GET" &&
            r.Status == 200
            );

            var json = await response.TextAsync();

            var doc = JsonDocument.Parse(json);

            var ids = new List<string>();

            foreach (var item in doc.RootElement.GetProperty("data").EnumerateArray())
            {
                var id = item.GetProperty("_id").GetString();
                ids.Add(id);
            }

            _logger.LogInformation("Playwrightservice : ID retrieval succesfull");
            _logger.LogInformation("Ids: \n{Ids}", string.Join("\n", ids));
        }

        private async Task GetDataForID(IPage page, string id)
        {
            var url = _siteSettings.ReportUrl.Replace("{id}", id);

            await page.GotoAsync(url);
            await page.WaitForURLAsync(url);

            await page.WaitForFunctionAsync(
                "() => window.appdatam && window.appdatam._id"
            );

            var json = await page.EvaluateAsync<string>(
                "() => JSON.stringify(window.appdatam)"
            );
            _logger.LogInformation("Playwrightservice : Data retrieval for ID : {Id} successful", id);
            _logger.LogInformation(json);
            await SaveJsonToFile(_jsonService.ConvertToSimpleJSON(json), "SimpleJSON");
        }

        private async Task SaveJsonToFile(List<string> data, string fileName)
        {
            var folder = Path.Combine(Directory.GetCurrentDirectory(), "data");
            Directory.CreateDirectory(folder);

            var filePath = Path.Combine(folder, $"{fileName}_{DateTime.Now:yyyy_MM_dd__HH_mm_ss}.json");

            var options = new JsonSerializerOptions { WriteIndented = true };

            var json = JsonSerializer.Serialize(data, options);

            await File.WriteAllTextAsync(filePath, json);
        }
    }
}
