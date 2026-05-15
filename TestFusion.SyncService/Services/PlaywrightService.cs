using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using System.Collections;
using System.Text.Json;
using TestFusion.Core.Interfaces;
using TestFusion.Core.Models;
using TestFusion.SyncService.Models;
using TestFusion.SyncService.Services;

public class PlaywrightService : TestFusion.Core.Interfaces.IPlaywright
{
    private readonly ILogger<PlaywrightService> _logger;
    private readonly SiteSettings _siteSettings;
    private readonly AuthSettings _authSettings;
    private readonly JSONService _jsonService;

    private Microsoft.Playwright.IPlaywright? _playwright;
    private IBrowserContext? _context;

    private readonly SemaphoreSlim _browserLock = new(1, 1);

    private const string SessionFolder = "playwright-session";

    public PlaywrightService(
        ILogger<PlaywrightService> logger,
        IOptions<SiteSettings> siteSettings,
        IOptions<AuthSettings> authSettings,
        JSONService jsonService)
    {
        _logger = logger;
        _siteSettings = siteSettings.Value;
        _authSettings = authSettings.Value;
        _jsonService = jsonService;
    }

    public async Task<List<string>> GetAllIDs()
    {
        _logger.LogInformation("STATUS: Retrieving all IDs");

        var page = await CreatePage();

        try
        {
            await GoToSite(page);
            await EnsureLoggedIn(page);

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

                if (!string.IsNullOrWhiteSpace(id))
                {
                    ids.Add(id);
                }
            }

            _logger.LogInformation("SUCCESS: Retrieved {Count} IDs", ids.Count);

            return ids;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERROR: Retrieving IDs");
            throw;
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    public async Task<TestListItemModel> GetDataForId(string id)
    {
        _logger.LogInformation("STATUS: Retrieving data for ID: {Id}", id);

        var page = await CreatePage();

        try
        {
            var url = _siteSettings.ReportUrl?.Replace("{id}", id);

            await page.GotoAsync(url, new()
            {
                WaitUntil = WaitUntilState.DOMContentLoaded
            });

            await EnsureLoggedIn(page);

            await page.WaitForFunctionAsync(
                "() => window.appdatam && window.appdatam._id");

            var json = await page.EvaluateAsync<string>(
                "() => JSON.stringify(window.appdatam)");

            _logger.LogInformation("SUCCESS: Retrieved data for ID: {Id}", id);

            return _jsonService.ConvertToTestListItem(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERROR: Retrieving data for ID: {Id}", id);
            throw;
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private async Task InitializeBrowser()
    {
        if (_context != null)
            return;

        await _browserLock.WaitAsync();

        try
        {
            if (_context != null)
                return;

            _logger.LogInformation("STATUS: Starting browser");

            _playwright = await Playwright.CreateAsync();

            var sessionPath = Path.Combine(
                Environment.CurrentDirectory,
                SessionFolder);

            _context = await _playwright.Chromium
                .LaunchPersistentContextAsync(
                    sessionPath,
                    new BrowserTypeLaunchPersistentContextOptions
                    {
                        Headless = true,

                        Channel = "msedge",

                        ViewportSize = new()
                        {
                            Width = 1920,
                            Height = 1080
                        },

                        Locale = "nl-NL",

                        TimezoneId = "Europe/Amsterdam",

                        UserAgent =
                            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                            "AppleWebKit/537.36 (KHTML, like Gecko) " +
                            "Chrome/135.0.0.0 Safari/537.36",

                        Args =
                        [
                            "--disable-blink-features=AutomationControlled"
                        ]
                    });

            await _context.AddInitScriptAsync("""
                Object.defineProperty(navigator, 'webdriver', {
                    get: () => undefined
                });
            """);

            _logger.LogInformation("SUCCESS: Browser started");
        }
        finally
        {
            _browserLock.Release();
        }
    }

    private async Task<IPage> CreatePage()
    {
        await InitializeBrowser();

        return await _context!.NewPageAsync();
    }

    private async Task GoToSite(IPage page)
    {
        _logger.LogInformation("STATUS: Navigating to site");

        await page.GotoAsync(_siteSettings.BaseUrl, new()
            {
                WaitUntil = WaitUntilState.DOMContentLoaded
            });

        await RandomDelay();

        _logger.LogInformation("SUCCESS: Navigated to site");
    }

    private async Task EnsureLoggedIn(IPage page)
    {
        try
        {
            var loginButton = page.Locator("#btn-login");

            if (await loginButton.IsVisibleAsync())
            {
                _logger.LogInformation("STATUS: Login required");

                await Login(page);
            }
            else
            {
                _logger.LogInformation("STATUS: Existing session detected, reloading page");

                await page.ReloadAsync();

                _logger.LogInformation("SUCCESS: Page reloaded");
            }
        }
        catch
        {
            _logger.LogInformation("ERROR: Login page not detected");
        }
    }

    private async Task Login(IPage page)
    {
        _logger.LogInformation("STATUS: Logging in");

        await TypeHumanly(page.Locator("#email"), _authSettings.Email);

        await RandomDelay();

        await TypeHumanly(page.Locator("#password"), _authSettings.Password);

        await RandomDelay();

        await page.Mouse.MoveAsync(400, 300);

        await RandomDelay();

        await page.Locator("#btn-login").ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await RandomDelay();

        _logger.LogInformation("SUCCESS: Logged in");
    }

    private async Task TypeHumanly(ILocator locator, string text)
    {
        await locator.ClickAsync();

        await locator.PressSequentiallyAsync(text, new()
            {
                Delay = Random.Shared.Next(50, 120)
            });
    }

    private async Task RandomDelay(int min = 300, int max = 1200)
    {
        await Task.Delay(Random.Shared.Next(min, max));
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("STATUS: Disposing browser");

        try
        {
            if (_context != null)
            {
                await _context.CloseAsync();
            }

            _playwright?.Dispose();

            _browserLock.Dispose();

            _logger.LogInformation("SUCCESS: Browser disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERROR: Disposing browser");
        }
    }
}