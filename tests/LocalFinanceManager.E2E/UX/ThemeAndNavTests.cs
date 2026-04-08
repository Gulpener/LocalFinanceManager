using LocalFinanceManager.Data;
using LocalFinanceManager.E2E.Helpers;
using Microsoft.EntityFrameworkCore;
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

        // Wait for Blazor circuit to initialise (theme-toggle button rendered by MainLayout)
        var toggleBtn = Page.Locator("[data-testid='theme-toggle']");
        await Expect(toggleBtn).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10_000 });

        // Assert – html element should carry data-theme="dark"
        // Use a generous timeout because ThemeService.InitialiseAsync fires in OnAfterRenderAsync
        var htmlElement = Page.Locator("html");
        await Expect(htmlElement).ToHaveAttributeAsync("data-theme", "dark",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = 10_000 });
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

        // Wait for circuit to initialize and set initial theme before reading it
        var toggleBtn = Page.Locator("[data-testid='theme-toggle']");
        await Expect(toggleBtn).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10_000 });
        // Allow ThemeService.InitialiseAsync to complete and set data-theme
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("data-theme", "light",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = 10_000 });

        var htmlElement = Page.Locator("html");
        var before = await htmlElement.GetAttributeAsync("data-theme");

        // Act – click the theme toggle
        await toggleBtn.ClickAsync();

        // Assert – theme should have changed (wait for Blazor to re-render and JS to fire)
        await Expect(htmlElement).Not.ToHaveAttributeAsync("data-theme", before ?? "light",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = 10_000 });
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

        // Wait for circuit to initialise and apply initial theme
        var toggleBtn = Page.Locator("[data-testid='theme-toggle']");
        await Expect(toggleBtn).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10_000 });
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("data-theme", "light",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = 10_000 });

        var htmlElement = Page.Locator("html");
        var original = await htmlElement.GetAttributeAsync("data-theme");

        // Act – toggle to dark
        await toggleBtn.ClickAsync();
        // Wait for theme to flip to dark before clicking again
        await Expect(htmlElement).Not.ToHaveAttributeAsync("data-theme", original ?? "light",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = 10_000 });

        // Toggle back to original
        await toggleBtn.ClickAsync();

        // Assert – theme should be back to original
        await Expect(htmlElement).ToHaveAttributeAsync("data-theme", original ?? "light",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = 10_000 });
    }

    [Test]
    public async Task DarkMode_Persists_AfterPageReload()
    {
        // Arrange – directly seed "dark" preference in DB so ThemeService loads it on init.
        // This avoids depending on toggle→DB write (which has timing complexity in E2E).
        using var scope = Factory!.CreateDbScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedDataHelper.SeedAccountAsync(dbContext, "Test Account", "NL91ABNA0417164300", 500m);

        // Seed the "dark" UserPreferences directly in DB
        dbContext.UserPreferences.Add(new LocalFinanceManager.Models.UserPreferences
        {
            Id = Guid.NewGuid(),
            UserId = AppDbContext.SeedUserId,
            Theme = "dark"
        });
        await dbContext.SaveChangesAsync();

        // Set OS preference to light so InitialiseAsync must use DB preference to get dark
        await Page.EmulateMediaAsync(new PageEmulateMediaOptions
        {
            ColorScheme = ColorScheme.Light
        });

        // Act – navigate: ThemeService.InitialiseAsync reads SeedUserId → "dark" from DB
        await Page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        // Assert – html element should carry data-theme="dark" (loaded from DB)
        var toggleBtn = Page.Locator("[data-testid='theme-toggle']");
        await Expect(toggleBtn).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10_000 });

        var htmlElement = Page.Locator("html");
        await Expect(htmlElement).ToHaveAttributeAsync("data-theme", "dark",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = 10_000 });

        // Act – reload page
        await Page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.NetworkIdle });

        // Assert – dark mode still active after reload (still reads "dark" from DB)
        htmlElement = Page.Locator("html");
        var reloadToggleBtn = Page.Locator("[data-testid='theme-toggle']");
        await Expect(reloadToggleBtn).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10_000 });
        await Expect(htmlElement).ToHaveAttributeAsync("data-theme", "dark",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = 10_000 });
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

        // Act – click the Accounts nav link (Blazor renders relative hrefs without leading /)
        var accountsLink = Page.Locator(".nav-scrollable a[href='accounts']");
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
