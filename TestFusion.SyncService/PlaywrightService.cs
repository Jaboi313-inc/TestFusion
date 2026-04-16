using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Playwright;

namespace TestFusion.SyncService
{
    public class PlaywrightService
    { 
        public static async Task Run() 
        { 
            using var playwright = await Playwright.CreateAsync(); 
            var browser = await playwright.Chromium.LaunchAsync(new() 
            { 
                Headless = false // Only for testing
            });

            var page = await browser.NewPageAsync(); 
            await GoToSite(page);
            await Task.Delay(10000); // Only for testing
            await browser.CloseAsync();
        }
        private static async Task GoToSite(IPage page)
        {
            await page.GotoAsync("https://example.com");
        }
    }
}
