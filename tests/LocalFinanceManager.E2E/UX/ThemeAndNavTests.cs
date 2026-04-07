using LocalFinanceManager.Data;
using LocalFinanceManager.E2E.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace LocalFinanceManager.E2E.UX;

/// <summary>
/// E2E tests for US16 Design Overhaul:
/// - Dark mode toggle and DB persistence
/// - Mobile-responsive hamburger navigation
/// </summary>
[TestFixture]
public class ThemeAndNavTests : E2ETestBase
{
    [SetUp]
    public async Task SetUp()
    {
        await Factory!.TruncateTablesAsync();
        await Page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Page.EvaluateAsync("() => localStorage.clear()");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Dark mode
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task DarkMode_OsPrefersDark_AppliesOnFirstVisit()
    {
        // Arrange – emulate OS dark preference
        await Page.EmulateMediaAsync(new PageEmulateMediaOptions
        {
            ColorScheme = ColorScheme.Dark
        });

        // Seed an account so the page isn't empty/redirected
        using var scope = Factory!.CreateDbScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedDataHelper.SeedAccountAsync(dbContext, "Test Account", "NL91ABNA0417164300", 500m);

        // Act
        await Page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        // Assert – html element should carry data-theme="dark"
        var htmlElement = Page.Locator("html");
        await Expect(htmlElement).ToHaveAttributeAsync("data-theme", "dark");
    }

    [Test]
    public async Task DarkMode_OsPrefersLight_AppliesOnFirstVisit()
    {
        // Arrange – emulate OS light preference
        await Page.EmulateMediaAsync(new PageEmulateMediaOptions
        {
            ColorScheme = ColorScheme.Light
        });

        using var scope = Factory!.CreateDbScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedDataHelper.SeedAccountAsync(dbContext, "Test Account", "NL91ABNA0417164300", 500m);

        // Act
        await Page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        // Assert – html element should NOT have data-theme="dark"
        var htmlElement = Page.Locator("html");
        var theme = await htmlElement.GetAttributeAsync("data-theme");
        Assert.That(theme, Is.Not.EqualTo("dark"), "Light OS preference should not apply dark theme");
    }

    [Test]
    public async Task DarkMode_ToggleButton_SwitchesTheme()
    {
        // Arrange
        await Page.EmulateMediaAsync(new PageEmulateMediaOptions
        {
            ColorScheme = ColorScheme.Light
        });

        using var scope = Factory!.CreateDbScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedDataHelper.SeedAccountAsync(dbContext, "Test Account", "NL91ABNA0417164300", 500m);

        await Page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        var htmlElement = Page.Locator("html");
        var before = await htmlElement.GetAttributeAsync("data-theme");

        // Act – click the theme toggle
        var toggleBtn = Page.Locator("[data-testid='theme-toggle']");
        await Expect(toggleBtn).ToBeVisibleAsync();
        await toggleBtn.ClickAsync();

        // Assert – theme should have changed
        var after = await htmlElement.GetAttributeAsync("data-theme");
        Assert.That(after, Is.Not.EqualTo(before), "Theme should flip after clicking toggle");
    }

    [Test]
    public async Task DarkMode_ToggleTwice_ReturnsToOriginalTheme()
    {
        // Arrange
        await Page.EmulateMediaAsync(new PageEmulateMediaOptions
        {
            ColorScheme = ColorScheme.Light
        });

        using var scope = Factory!.CreateDbScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedDataHelper.SeedAccountAsync(dbContext, "Test Account", "NL91ABNA0417164300", 500m);

        await Page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        var htmlElement = Page.Locator("html");
        var original = await htmlElement.GetAttributeAsync("data-theme");

        var toggleBtn = Page.Locator("[data-testid='theme-toggle']");
        await Expect(toggleBtn).ToBeVisibleAsync();

        // Act – toggle twice
        await toggleBtn.ClickAsync();
        await toggleBtn.ClickAsync();

        // Assert – theme should be back to original
        var final = await htmlElement.GetAttributeAsync("data-theme");
        Assert.That(final, Is.EqualTo(original), "Double-toggle should restore the original theme");
    }

    [Test]
    public async Task DarkMode_Persists_AfterPageReload()
    {
        // Arrange – start in light
        await Page.EmulateMediaAsync(new PageEmulateMediaOptions
        {
            ColorScheme = ColorScheme.Light
        });

        using var scope = Factory!.CreateDbScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedDataHelper.SeedAccountAsync(dbContext, "Test Account", "NL91ABNA0417164300", 500m);

        await Page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        // Toggle to dark
        var toggleBtn = Page.Locator("[data-testid='theme-toggle']");
        await Expect(toggleBtn).ToBeVisibleAsync();
        await toggleBtn.ClickAsync();

        var htmlElement = Page.Locator("html");
        await Expect(htmlElement).ToHaveAttributeAsync("data-theme", "dark");

        // Act – reload page (same circuit re-initialises ThemeService from DB)
        await Page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.NetworkIdle });

        // Assert – dark mode should still be active
        htmlElement = Page.Locator("html");
        await Expect(htmlElement).ToHaveAttributeAsync("data-theme", "dark");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Responsive navigation
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task ResponsiveNav_Mobile_HamburgerVisible_SidebarHidden()
    {
        // Arrange – mobile viewport
        await Page.SetViewportSizeAsync(375, 812);

        using var scope = Factory!.CreateDbScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedDataHelper.SeedAccountAsync(dbContext, "Test Account", "NL91ABNA0417164300", 500m);

        // Act
        await Page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        // Assert – hamburger button visible
        var hamburger = Page.Locator("[data-testid='hamburger-btn']");
        await Expect(hamburger).ToBeVisibleAsync();

        // Nav links should not be visible (sidebar collapsed)
        var navScrollable = Page.Locator(".nav-scrollable");
        var isOpen = await navScrollable.EvaluateAsync<bool>("el => el.classList.contains('open')");
        Assert.That(isOpen, Is.False, "Nav should be closed on mobile by default");
    }

    [Test]
    public async Task ResponsiveNav_Mobile_HamburgerClick_OpensSidebar()
    {
        // Arrange – mobile viewport
        await Page.SetViewportSizeAsync(375, 812);

        using var scope = Factory!.CreateDbScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedDataHelper.SeedAccountAsync(dbContext, "Test Account", "NL91ABNA0417164300", 500m);

        await Page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        // Act – click hamburger
        var hamburger = Page.Locator("[data-testid='hamburger-btn']");
        await hamburger.ClickAsync();

        // Assert – nav-scrollable should have 'open' class
        var navScrollable = Page.Locator(".nav-scrollable");
        var isOpen = await navScrollable.EvaluateAsync<bool>("el => el.classList.contains('open')");
        Assert.That(isOpen, Is.True, "Nav should open after hamburger click");

        // Overlay should be visible
        var overlay = Page.Locator("[data-testid='nav-overlay']");
        await Expect(overlay).ToBeVisibleAsync();
    }

    [Test]
    public async Task ResponsiveNav_Mobile_NavItemClick_ClosesSidebar()
    {
        // Arrange – mobile viewport
        await Page.SetViewportSizeAsync(375, 812);

        using var scope = Factory!.CreateDbScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedDataHelper.SeedAccountAsync(dbContext, "Test Account", "NL91ABNA0417164300", 500m);

        await Page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        // Open sidebar
        await Page.Locator("[data-testid='hamburger-btn']").ClickAsync();
        var navScrollable = Page.Locator(".nav-scrollable");
        var openBefore = await navScrollable.EvaluateAsync<bool>("el => el.classList.contains('open')");
        Assert.That(openBefore, Is.True, "Nav should be open before navigation");

        // Act – click the Accounts nav link
        var accountsLink = Page.Locator(".nav-scrollable a[href='/accounts']");
        await accountsLink.ClickAsync();
        await Page.WaitForURLAsync("**/accounts", new PageWaitForURLOptions { WaitUntil = WaitUntilState.NetworkIdle });

        // Assert – sidebar should be closed again
        var isOpenAfter = await navScrollable.EvaluateAsync<bool>("el => el.classList.contains('open')");
        Assert.That(isOpenAfter, Is.False, "Nav should close after navigating to a page");
    }

    [Test]
    public async Task ResponsiveNav_Desktop_SidebarAlwaysVisible_HamburgerHidden()
    {
        // Arrange – desktop viewport (≥768px hides hamburger via d-md-none)
        await Page.SetViewportSizeAsync(1280, 800);

        using var scope = Factory!.CreateDbScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedDataHelper.SeedAccountAsync(dbContext, "Test Account", "NL91ABNA0417164300", 500m);

        // Act
        await Page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        // Assert – hamburger not visible on desktop
        var hamburger = Page.Locator("[data-testid='hamburger-btn']");
        await Expect(hamburger).Not.ToBeVisibleAsync();

        // Nav-scrollable visible (sidebar always open on desktop)
        var navItems = Page.Locator(".nav-scrollable .nav-item");
        await Expect(navItems.First).ToBeVisibleAsync();
    }
}
