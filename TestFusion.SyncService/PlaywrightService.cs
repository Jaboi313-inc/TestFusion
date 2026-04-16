using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Text;
using TestFusion.SyncService.Models;

namespace TestFusion.SyncService
{
    using Microsoft.Extensions.Options;
    using TestFusion.SyncService.Models;

    public class PlaywrightService
    {
        private readonly SiteSettings _siteSettings;
        private readonly Intervals _intervals;
        private readonly AuthSettings _auth;

        public PlaywrightService(
            IOptions<SiteSettings> siteSettings,
            IOptions<Intervals> intervals,
            IOptions<AuthSettings> auth)
        {
            _siteSettings = siteSettings.Value;
            _intervals = intervals.Value;
            _auth = auth.Value;
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
            await Task.Delay(10000); // Only for testing
            await browser.CloseAsync();
        }
        private async Task GoToSite(IPage page)
        {
            await page.GotoAsync(_siteSettings.BaseUrl);
        }

        private async Task Login(IPage page)
        {
            await page.Locator("#email").FillAsync(_auth.Email);
            await page.Locator("#password").FillAsync(_auth.Password);

            await page.Locator("#btn-login").ClickAsync();
        }
    }
}
