using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace LocalFinanceManager.E2E.Admin;

[TestFixture]
public class AdminSettingsTests : PageTest
{
    private PlaywrightFixture _fixture = null!;
    private string _baseUrl = null!;

    [SetUp]
    public async Task SetUp()
    {
        _fixture = new PlaywrightFixture();
        var client = _fixture.CreateClient();
        _baseUrl = client.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost";
        
        await Context.Tracing.StartAsync(new()
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });
    }

    [TearDown]
    public async Task TearDown()
    {
        await Context.Tracing.StopAsync(new()
        {
            Path = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                $"{TestContext.CurrentContext.Test.Name}.zip"
            )
        });
        
        _fixture?.Dispose();
    }

    [Test]
    [Ignore("Manual testing - Playwright browser automation requires manual verification")]
    public async Task AdminSettings_PageLoads_DisplaysEnvironmentInformation()
    {
        // Arrange & Act
        await Page.GotoAsync($"{_baseUrl}/admin/settings");

        // Assert - Page loads
        await Expect(Page).ToHaveTitleAsync(new Regex("Admin Settings"));
        await Expect(Page.Locator("h3")).ToContainTextAsync("Systeem Instellingen");

        // Assert - Environment section visible
        var environmentCard = Page.Locator(".card").First;
        await Expect(environmentCard.Locator("h5")).ToContainTextAsync("Environment Informatie");
        
        // Assert - Environment badge exists
        var environmentBadge = environmentCard.Locator(".badge");
        await Expect(environmentBadge).ToBeVisibleAsync();
    }

    [Test]
    [Ignore("Manual testing - Playwright browser automation requires manual verification")]
    public async Task AdminSettings_DatabaseConfiguration_DisplaysCorrectInformation()
    {
        // Arrange & Act
        await Page.GotoAsync($"{_baseUrl}/admin/settings");

        // Assert - Database configuration section visible
        var dbCard = Page.Locator(".card").Nth(1);
        await Expect(dbCard.Locator("h5")).ToContainTextAsync("Database Configuratie");

        // Assert - Connection string displayed
        var connectionStringRow = dbCard.Locator("tr").Filter(new() { HasTextString = "Connection String:" });
        await Expect(connectionStringRow.Locator("code")).ToBeVisibleAsync();

        // Assert - Database path displayed
        var dbPathRow = dbCard.Locator("tr").Filter(new() { HasTextString = "Database Pad:" });
        await Expect(dbPathRow.Locator("code")).ToContainTextAsync("localfinancemanager.test.db");

        // Assert - Database exists badge
        var dbExistsRow = dbCard.Locator("tr").Filter(new() { HasTextString = "Database Bestaat:" });
        var existsBadge = dbExistsRow.Locator(".badge");
        await Expect(existsBadge).ToContainTextAsync("Ja");
    }

    [Test]
    [Ignore("Manual testing - Playwright browser automation requires manual verification")]
    public async Task AdminSettings_MigrationInfo_DisplaysLastMigration()
    {
        // Arrange & Act
        await Page.GotoAsync($"{_baseUrl}/admin/settings");

        // Assert - Database configuration section visible
        var dbCard = Page.Locator(".card").Nth(1);

        // Assert - Last migration displayed
        var lastMigrationRow = dbCard.Locator("tr").Filter(new() { HasTextString = "Laatst Uitgevoerde Migratie:" });
        await Expect(lastMigrationRow.Locator("code")).ToBeVisibleAsync();

        // Assert - Pending migrations shown
        var pendingRow = dbCard.Locator("tr").Filter(new() { HasTextString = "Pending Migrations:" });
        var pendingBadge = pendingRow.Locator(".badge");
        await Expect(pendingBadge).ToBeVisibleAsync();
    }

    [Test]
    [Ignore("Manual testing - Playwright browser automation requires manual verification")]
    public async Task AdminSettings_SeedDataStatus_DisplaysCounts()
    {
        // Arrange & Act
        await Page.GotoAsync($"{_baseUrl}/admin/settings");

        // Assert - Seed data section visible
        var seedCard = Page.Locator(".card").Nth(2);
        await Expect(seedCard.Locator("h5")).ToContainTextAsync("Seed Data Status");

        // Assert - Account count displayed
        var accountRow = seedCard.Locator("tr").Filter(new() { HasTextString = "Accounts:" });
        await Expect(accountRow).ToContainTextAsync("records");

        // Assert - Category count displayed
        var categoryRow = seedCard.Locator("tr").Filter(new() { HasTextString = "Categories:" });
        await Expect(categoryRow).ToContainTextAsync("records");

        // Assert - Budget plan count displayed
        var budgetRow = seedCard.Locator("tr").Filter(new() { HasTextString = "Budget Plans:" });
        await Expect(budgetRow).ToContainTextAsync("records");

        // Assert - Seed data status badge
        var seedStatusRow = seedCard.Locator("tr").Filter(new() { HasTextString = "Seed Data Geladen:" });
        var statusBadge = seedStatusRow.Locator(".badge");
        await Expect(statusBadge).ToBeVisibleAsync();
    }

    [Test]
    [Ignore("Manual testing - Playwright browser automation requires manual verification")]
    public async Task AdminSettings_NavigationMenu_ContainsAdminSettingsLink()
    {
        // Arrange & Act
        await Page.GotoAsync(_baseUrl);

        // Assert - Admin Settings link exists in navigation menu
        var adminLink = Page.Locator("nav").Locator("a[href='admin/settings']");
        await Expect(adminLink).ToBeVisibleAsync();
        await Expect(adminLink).ToContainTextAsync("Admin Settings");

        // Act - Click the link
        await adminLink.ClickAsync();

        // Assert - Navigate to admin settings page
        await Expect(Page).ToHaveURLAsync(new Regex("/admin/settings"));
        await Expect(Page.Locator("h3")).ToContainTextAsync("Systeem Instellingen");
    }
}
