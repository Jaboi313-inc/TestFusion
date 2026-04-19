using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using System.Text.Json;
using TestFusion.Core.Models;
using TestFusion.Core.Interfaces;
using TestFusion.SyncService.Models;

public class PlaywrightService : IPlaywrightInterface
{
    private readonly ILogger<PlaywrightService> _logger;
    private readonly SiteSettings _siteSettings;
    private readonly AuthSettings _auth;

    public PlaywrightService(
        ILogger<PlaywrightService> logger,
        IOptions<SiteSettings> siteSettings,
        IOptions<AuthSettings> auth)
    {
        _logger = logger;
        _siteSettings = siteSettings.Value;
        _auth = auth.Value;

        _logger.LogInformation("CTOR instance {Hash}", GetHashCode());
    }

    public async Task<List<string>> GetAllIDs()
    {
        using var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new() { Headless = false });

        try
        {
            var page = await browser.NewPageAsync();

            _logger.LogInformation("BaseUrl from config: {BaseUrl}", _siteSettings.BaseUrl);

            await GoToSite(page);
            await Login(page);

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
                if (id != null)
                    ids.Add(id);
            }

            return ids;
        }
        finally
        {
            await browser.CloseAsync();
        }
    }

    public async Task<TestListItemModel> GetDataForId(string id)
    {
        using var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new() { Headless = false });

        try
        {

            var page = await browser.NewPageAsync();

            await GoToSite(page);
            await Login(page);

            var url = _siteSettings.ReportUrl?.Replace("{id}", id);

            _logger.LogInformation("Navigating to URL: '{Url}'", url);

            if (string.IsNullOrWhiteSpace(url))
                throw new Exception("ReportUrl became empty!");

            await page.GotoAsync(url);

            await page.WaitForFunctionAsync("() => window.appdatam && window.appdatam._id");

            var json = await page.EvaluateAsync<string>(
                "() => JSON.stringify(window.appdatam)"
            );

            return JsonSerializer.Deserialize<TestListItemModel>(json);
        }
        finally
        {
            await browser.CloseAsync();
        }
    }

    private async Task GoToSite(IPage page)
    {
        _logger.LogInformation("GoToSite instance {Hash}", GetHashCode());
        _logger.LogInformation("GO TO SITE BaseUrl = '{BaseUrl}'", _siteSettings.BaseUrl ?? "<null>");

        if (string.IsNullOrWhiteSpace(_siteSettings.BaseUrl))
            throw new Exception("BaseUrl is EMPTY in THIS instance");

        await page.GotoAsync(_siteSettings.BaseUrl);
    }

    private async Task Login(IPage page)
    {
        await page.Locator("#email").FillAsync(_auth.Email);
        await page.Locator("#password").FillAsync(_auth.Password);
        await page.Locator("#btn-login").ClickAsync();
    }
}