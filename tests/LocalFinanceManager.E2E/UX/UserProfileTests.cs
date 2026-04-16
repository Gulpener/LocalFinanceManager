using Microsoft.Playwright;

namespace LocalFinanceManager.E2E.UX;

/// <summary>
/// E2E tests for UserStory-21: User Profile Page.
/// Tests page access control and basic page rendering.
/// NOTE: Supabase file upload is not tested in E2E (requires live Supabase instance).
/// </summary>
[TestFixture]
public class UserProfileTests : E2ETestBase
{
    [SetUp]
    public async Task SetUp()
    {
        await Factory!.TruncateTablesAsync();
    }

    // ── Unauthenticated access ───────────────────────────────────────────────

    [Test]
    [Description("Visiting /useraccount without authentication must redirect to /login.")]
    public async Task UnauthenticatedBrowser_Account_RedirectsToLogin()
    {
        // Use a fresh browser context with no auth cookie so the request is anonymous.
        await using var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{BaseUrl}/useraccount", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 30_000
        });

        await Assertions.Expect(page).ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex("/login"),
            new() { Timeout = 20_000 });

        Assert.That(page.Url, Does.Contain("/login"),
            "Unauthenticated access to /useraccount must redirect to /login");
    }

    // ── Authenticated access ─────────────────────────────────────────────────

    [Test]
    [Description("Authenticated user visiting /useraccount sees the profile page.")]
    public async Task AuthenticatedUser_Account_ShowsProfilePage()
    {
        // The default Page in E2ETestBase includes the auth cookie set by TestWebApplicationFactory.
        await Page.GotoAsync($"{BaseUrl}/useraccount", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 30_000
        });

        // Wait for the Blazor circuit to render the page
        var pageContainer = Page.Locator("[data-testid='account-profile-page']");
        await Expect(pageContainer).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 15_000 });

        // Verify key elements are present
        var saveButton = Page.Locator("[data-testid='save-profile-btn']");
        await Expect(saveButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10_000 });

        var firstNameInput = Page.Locator("[data-testid='first-name-input']");
        await Expect(firstNameInput).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10_000 });

        var lastNameInput = Page.Locator("[data-testid='last-name-input']");
        await Expect(lastNameInput).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10_000 });
    }

    [Test]
    [Description("Page title contains 'My account'.")]
    public async Task AuthenticatedUser_Account_PageTitleIsCorrect()
    {
        await Page.GotoAsync($"{BaseUrl}/useraccount", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 30_000
        });

        await Expect(Page).ToHaveTitleAsync(
            new System.Text.RegularExpressions.Regex("My account", System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            new PageAssertionsToHaveTitleOptions { Timeout = 15_000 });
    }
}
