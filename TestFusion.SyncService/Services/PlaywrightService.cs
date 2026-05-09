using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using System.Collections;
using System.Text.Json;
using TestFusion.Core.Interfaces;
using TestFusion.Core.Models;
using TestFusion.SyncService.Models;
using TestFusion.SyncService.Services;

public class PlaywrightService : IPlaywrightInterface
{
    private readonly ILogger<PlaywrightService> _logger;
    private readonly SiteSettings _siteSettings;
    private readonly AuthSettings _authSettings;

    public PlaywrightService(
        ILogger<PlaywrightService> logger,
        IOptions<SiteSettings> siteSettings,
        IOptions<AuthSettings> authSettings,
    {
        _logger = logger;
        _siteSettings = siteSettings.Value;
        _authSettings = authSettings.Value;

    }

    public async Task<List<string>> GetAllIDs()
    {
        _logger.LogInformation("STATUS: Retrieving all IDs");

        using var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new() { Headless = false });

        try
        {
            var page = await browser.NewPageAsync();

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

            _logger.LogInformation("SUCCES: Retrieved IDs: \n{Ids}", string.Join("\n", ids));

            return ids;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERROR: Retrieving ID's");
            throw;
        }
        finally
        {
            await browser.CloseAsync();
            _logger.LogInformation("FINALLY: Browser closed");
            _logger.LogInformation("STATUS: Finished retrieving IDs");
        }
    }

    public async Task<TestListItemModel> GetDataForId(string id)
    {
        _logger.LogInformation("STATUS: Retrieving data for ID: {id}", id);

        using var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new() { Headless = false });

        try
        {
            var page = await browser.NewPageAsync();

            var url = _siteSettings.ReportUrl?.Replace("{id}", id);
            await page.GotoAsync(url);

            if (await page.Locator("#btn-login").IsVisibleAsync())
            {
                await Login(page);
            }

            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.WaitForFunctionAsync(
                "() => window.appdatam && window.appdatam._id");

            var json = await page.EvaluateAsync<string>(
                "() => JSON.stringify(window.appdatam)"
            );

            _logger.LogInformation("SUCCES: Retrieved data for ID: {id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERROR: Retrieving data for ID: {id}", id);
            throw;
        }
        finally
        {
            await browser.CloseAsync();
            _logger.LogInformation("FINALLY: Browser closed");
            _logger.LogInformation("STATUS: Finished retrieving data for ID: {id}", id);
        }
    }

    private async Task GoToSite(IPage page)
    {
        _logger.LogInformation("STATUS: Navigating to site");

        await page.GotoAsync(_siteSettings.BaseUrl, new()
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        _logger.LogInformation("STATUS: Navigated to site");
    }

    private async Task Login(IPage page)
    {
        _logger.LogInformation("STATUS: Logging in to site");

        await page.Locator("#email").FillAsync(_authSettings.Email);
        await page.Locator("#password").FillAsync(_authSettings.Password);

        await page.Locator("#btn-login").ClickAsync();

        _logger.LogInformation("STATUS: Logged in");
    }
}