using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using TestFusion.Core.Interfaces;
using TestFusion.Services.Models;

public class PlaywrightService : TestFusion.Core.Interfaces.IPlaywright
{
    private readonly ILogger<PlaywrightService> _logger;
    private readonly SiteSettings _siteSettings;
    private readonly AuthSettings _authSettings;

    private Microsoft.Playwright.IPlaywright? _playwright;
    private IBrowserContext? _context;

    private readonly SemaphoreSlim _browserLock = new(1, 1);

    private const string SessionFolder = "playwright-session";

    public PlaywrightService(
        ILogger<PlaywrightService> logger,
        IOptions<SiteSettings> siteSettings,
        IOptions<AuthSettings> authSettings)
    {
        _logger = logger;
        _siteSettings = siteSettings.Value;
        _authSettings = authSettings.Value;
    }

    public async Task<List<string>> GetAllIDs()
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Retrieving report IDs");

        var page = await CreatePage();

        try
        {
            await GoToSite(page);
            await EnsureLoggedIn(page);

            var lengthSelect = page.Locator(
                "select[name='client_machine_report_length']");

            await lengthSelect.WaitForAsync(new()
            {
                State = WaitForSelectorState.Visible
            });

            var responseTask = page.WaitForResponseAsync(r =>
                r.Url.Contains("get-machines-report-list") &&
                r.Url.Contains("length=-1") &&
                r.Request.Method == "GET" &&
                r.Status == 200);

            await lengthSelect.SelectOptionAsync("-1");

            var selectedValue = await lengthSelect.InputValueAsync();

            if (selectedValue != "-1")
            {
                _logger.LogWarning(
                    "Report list length could not be set to All. Actual value: {Value}",
                    selectedValue);
            }
            else
            {
                _logger.LogDebug("Report list length set to All");
            }

            var response = await responseTask;

            _logger.LogDebug(
                "Report list request completed: {Url}",
                response.Request.Url);

            var json = await response.TextAsync();

            using var doc = JsonDocument.Parse(json);

            var root = doc.RootElement;

            var recordsTotal =
                root.TryGetProperty("recordsTotal", out var recordsTotalElement)
                    ? recordsTotalElement.GetInt32()
                    : 0;

            var recordsFiltered =
                root.TryGetProperty("recordsFiltered", out var recordsFilteredElement)
                    ? recordsFilteredElement.GetInt32()
                    : 0;

            var data = root.GetProperty("data");
            var responseCount = data.GetArrayLength();

            var ids = new List<string>();

            foreach (var item in data.EnumerateArray())
            {
                if (!item.TryGetProperty("_id", out var idElement))
                {
                    continue;
                }

                var id = idElement.GetString();

                if (!string.IsNullOrWhiteSpace(id))
                {
                    ids.Add(id);
                }
            }

            stopwatch.Stop();

            if (responseCount != ids.Count)
            {
                _logger.LogWarning(
                    "Retrieved {ValidIdCount} valid IDs from {ReturnedCount} returned records. Total: {TotalCount}, filtered: {FilteredCount}",
                    ids.Count,
                    responseCount,
                    recordsTotal,
                    recordsFiltered);
            }
            else
            {
                _logger.LogInformation(
                    "Retrieved {Count} report IDs in {ElapsedMilliseconds} ms",
                    ids.Count,
                    stopwatch.ElapsedMilliseconds);
            }

            return ids;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Failed to retrieve report IDs after {ElapsedMilliseconds} ms",
                stopwatch.ElapsedMilliseconds);

            throw;
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    public async Task<string> GetDataForId(string id)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Retrieving report {ReportId}",
            id);

        var page = await CreatePage();

        try
        {
            await GoToSite(page);
            await EnsureLoggedIn(page);

            var url = _siteSettings.ReportUrl?.Replace("{id}", id);

            if (string.IsNullOrWhiteSpace(url))
            {
                throw new InvalidOperationException(
                    "ReportUrl is not configured.");
            }

            _logger.LogDebug(
                "Navigating to report {ReportId}: {Url}",
                id,
                url);

            await page.GotoAsync(url, new()
            {
                WaitUntil = WaitUntilState.DOMContentLoaded
            });

            await page.WaitForFunctionAsync(
                """
                expectedId => {
                    return window.appdatam &&
                           window.appdatam._id === expectedId;
                }
                """,
                id,
                new PageWaitForFunctionOptions
                {
                    Timeout = 60000
                });

            var json = await page.EvaluateAsync<string>(
                "() => JSON.stringify(window.appdatam)");

            stopwatch.Stop();

            _logger.LogInformation(
                "Retrieved report {ReportId} in {ElapsedMilliseconds} ms",
                id,
                stopwatch.ElapsedMilliseconds);

            return json;
        }
        catch (TimeoutException ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Timed out retrieving report {ReportId} after {ElapsedMilliseconds} ms. Current URL: {Url}",
                id,
                stopwatch.ElapsedMilliseconds,
                page.Url);

            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Failed to retrieve report {ReportId} after {ElapsedMilliseconds} ms",
                id,
                stopwatch.ElapsedMilliseconds);

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

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("Starting browser");

            _playwright = await Playwright.CreateAsync();

            var sessionPath = Path.Combine(Environment.CurrentDirectory, SessionFolder);

            _context = await _playwright.Chromium.LaunchPersistentContextAsync(
                sessionPath,
                new BrowserTypeLaunchPersistentContextOptions
                {
                    Headless = true,
                    Channel = "msedge",
                    ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                    Locale = "nl-NL",
                    TimezoneId = "Europe/Amsterdam",
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/135.0.0.0 Safari/537.36",
                    Args = ["--disable-blink-features=AutomationControlled"]
                });

            await _context.AddInitScriptAsync("""Object.defineProperty(navigator, 'webdriver', {get: () => undefined});""");

            stopwatch.Stop();
            _logger.LogInformation("Browser started in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start browser");

            if (_context != null)
            {
                try { await _context.CloseAsync(); }
                catch { }

                _context = null;
            }

            _playwright?.Dispose();
            _playwright = null;

            throw;
        }
        finally
        {
            _browserLock.Release();
        }
    }

    private async Task<IPage> CreatePage()
    {
        await InitializeBrowser();

        var page = await _context!.NewPageAsync();

        _logger.LogDebug("Created browser page");

        return page;
    }

    private async Task GoToSite(IPage page)
    {
        _logger.LogDebug(
            "Navigating to source site {Url}",
            _siteSettings.BaseUrl);

        await page.GotoAsync(_siteSettings.BaseUrl, new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await RandomDelay();

        _logger.LogDebug("Source site loaded");
    }

    private async Task EnsureLoggedIn(IPage page)
    {
        try
        {
            var loginButton = page.Locator("#btn-login");

            if (await loginButton.IsVisibleAsync())
            {
                _logger.LogInformation("Login required");

                await Login(page);

                return;
            }

            _logger.LogDebug("Existing login session detected");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Could not determine login state");
        }
    }

    private async Task Login(IPage page)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Logging in");

        await TypeHumanly(
            page.Locator("#email"),
            _authSettings.Email);

        await RandomDelay();

        await TypeHumanly(
            page.Locator("#password"),
            _authSettings.Password);

        await RandomDelay();

        await page.Mouse.MoveAsync(400, 300);

        await RandomDelay();

        await page.Locator("#btn-login").ClickAsync();

        await page.WaitForLoadStateAsync(
            LoadState.DOMContentLoaded);

        await RandomDelay();

        stopwatch.Stop();

        _logger.LogInformation(
            "Login completed in {ElapsedMilliseconds} ms",
            stopwatch.ElapsedMilliseconds);
    }

    private async Task TypeHumanly(
        ILocator locator,
        string text)
    {
        await locator.ClickAsync();

        await locator.PressSequentiallyAsync(text, new()
        {
            Delay = Random.Shared.Next(50, 120)
        });
    }

    private async Task RandomDelay(
        int min = 300,
        int max = 1200)
    {
        await Task.Delay(
            Random.Shared.Next(min, max));
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing browser");

        if (_context != null)
        {
            try
            {
                foreach (var page in _context.Pages.ToList())
                {
                    if (!page.IsClosed)
                        await page.CloseAsync();
                }

                await _context.CloseAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to close browser context");
            }
            finally
            {
                _context = null;
            }
        }

        if (_playwright != null)
        {
            try
            {
                _playwright.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to dispose Playwright");
            }
            finally
            {
                _playwright = null;
            }
        }

        _browserLock.Dispose();

        _logger.LogInformation("Browser disposed");
    }
}
