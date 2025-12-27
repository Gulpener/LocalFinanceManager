using Microsoft.Playwright;
using NUnit.Framework;

namespace LocalFinanceManager.E2E;

[SetUpFixture]
public class PlaywrightFixture
{
    public static IBrowser? Browser { get; private set; }
    public static IBrowserContext? BrowserContext { get; private set; }
    public static IPage? Page { get; private set; }

    [OneTimeSetUp]
    public async Task InitializePlaywright()
    {
        var playwright = await Playwright.CreateAsync();
        Browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        BrowserContext = await Browser.NewContextAsync();
        Page = await BrowserContext.NewPageAsync();
    }

    [OneTimeTearDown]
    public async Task DisposePlaywright()
    {
        if (Page != null)
            await Page.CloseAsync();
        if (BrowserContext != null)
            await BrowserContext.CloseAsync();
        if (Browser != null)
            await Browser.CloseAsync();
    }
}
