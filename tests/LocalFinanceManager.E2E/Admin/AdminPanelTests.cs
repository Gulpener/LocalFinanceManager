using LocalFinanceManager.Data;
using LocalFinanceManager.E2E.Helpers;
using LocalFinanceManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace LocalFinanceManager.E2E.Admin;

/// <summary>
/// E2E tests for the Admin Panel: tab navigation, user management, and admin-only access.
/// </summary>
[TestFixture]
public class AdminPanelTests : E2ETestBase
{
    private static readonly Guid AdditionalUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [SetUp]
    public async Task SetUp()
    {
        await Factory!.TruncateTablesAsync();

        // Re-create the seed user as admin after truncation
        using var scope = Factory.CreateDbScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!await context.Users.AnyAsync(u => u.Id == AppDbContext.SeedUserId))
        {
            context.Users.Add(new User
            {
                Id = AppDbContext.SeedUserId,
                SupabaseUserId = "00000000-0000-0000-0000-000000000001",
                Email = AppDbContext.SeedUserEmail,
                DisplayName = "Dev User",
                EmailConfirmed = true,
                IsAdmin = true
            });
            await context.SaveChangesAsync();
        }

        if (!await context.Users.AnyAsync(u => u.Id == AdditionalUserId))
        {
            context.Users.Add(new User
            {
                Id = AdditionalUserId,
                SupabaseUserId = "11111111-1111-1111-1111-111111111111",
                Email = "second.user@localfinancemanager.local",
                DisplayName = "Second User",
                EmailConfirmed = true,
                IsAdmin = false
            });
        }

        context.UserPreferences.RemoveRange(context.UserPreferences.Where(p =>
            p.UserId == AppDbContext.SeedUserId || p.UserId == AdditionalUserId));

        context.UserPreferences.AddRange(
            new UserPreferences
            {
                Id = Guid.NewGuid(),
                UserId = AppDbContext.SeedUserId,
                FirstName = "Dev",
                LastName = "Admin",
                ProfileImagePath = "profiles/dev-admin.png",
                Theme = "light"
            },
            new UserPreferences
            {
                Id = Guid.NewGuid(),
                UserId = AdditionalUserId,
                FirstName = "Solo",
                LastName = null,
                ProfileImagePath = null,
                Theme = "light"
            });

        await context.SaveChangesAsync();
    }

    [Test]
    public async Task AdminSettings_PageLoads()
    {
        await Page.GotoAsync($"{BaseUrl}/admin/settings");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        Assert.That(Page.Url, Does.Contain("/admin/settings"));
    }

    [Test]
    public async Task AdminPanel_ShowsTabBar()
    {
        await Page.GotoAsync($"{BaseUrl}/admin/settings");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var settingsTab = Page.Locator("[data-testid='admin-tab-settings']");
        var mlTab = Page.Locator("[data-testid='admin-tab-ml']");
        var monitoringTab = Page.Locator("[data-testid='admin-tab-monitoring']");
        var usersTab = Page.Locator("[data-testid='admin-tab-users']");

        await Assertions.Expect(settingsTab).ToBeVisibleAsync();
        await Assertions.Expect(mlTab).ToBeVisibleAsync();
        await Assertions.Expect(monitoringTab).ToBeVisibleAsync();
        await Assertions.Expect(usersTab).ToBeVisibleAsync();
    }

    [Test]
    public async Task AdminPanel_SettingsTab_IsActiveOnSettingsPage()
    {
        await Page.GotoAsync($"{BaseUrl}/admin/settings");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var settingsTab = Page.Locator("[data-testid='admin-tab-settings']");
        await Assertions.Expect(settingsTab).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("active"));
    }

    [Test]
    public async Task AdminPanel_NavigateToUsersTab_LoadsUserManagement()
    {
        await Page.GotoAsync($"{BaseUrl}/admin/settings");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var usersTab = Page.Locator("[data-testid='admin-tab-users']");
        await usersTab.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        Assert.That(Page.Url, Does.Contain("/admin/users"));
    }

    [Test]
    public async Task UserManagement_ShowsSeedUser()
    {
        await Page.GotoAsync($"{BaseUrl}/admin/users");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForSelectorAsync($"[data-testid='user-row-{AppDbContext.SeedUserId}']", new PageWaitForSelectorOptions { Timeout = 15000 });

        var userRow = Page.Locator($"[data-testid='user-row-{AppDbContext.SeedUserId}']");
        await Assertions.Expect(userRow).ToBeVisibleAsync();
    }

    [Test]
    public async Task UserManagement_AdminBadge_ShownForAdminUser()
    {
        await Page.GotoAsync($"{BaseUrl}/admin/users");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForSelectorAsync($"[data-testid='admin-badge-{AppDbContext.SeedUserId}']", new PageWaitForSelectorOptions { Timeout = 15000 });

        var badge = Page.Locator($"[data-testid='admin-badge-{AppDbContext.SeedUserId}']");
        await Assertions.Expect(badge).ToBeVisibleAsync();
        await Assertions.Expect(badge).ToContainTextAsync("Admin");
    }

    [Test]
    public async Task UserManagement_SelfToggleAdmin_ButtonIsDisabled()
    {
        await Page.GotoAsync($"{BaseUrl}/admin/users");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForSelectorAsync($"[data-testid='toggle-admin-{AppDbContext.SeedUserId}']", new PageWaitForSelectorOptions { Timeout = 15000 });

        var button = Page.Locator($"[data-testid='toggle-admin-{AppDbContext.SeedUserId}']");
        await Assertions.Expect(button).ToBeDisabledAsync();
    }

    [Test]
    public async Task UserManagement_ExpandRow_ShowsShareDetails()
    {
        await Page.GotoAsync($"{BaseUrl}/admin/users");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForSelectorAsync($"[data-testid='expand-user-{AppDbContext.SeedUserId}']", new PageWaitForSelectorOptions { Timeout = 15000 });

        var expandBtn = Page.Locator($"[data-testid='expand-user-{AppDbContext.SeedUserId}']");
        await expandBtn.ClickAsync();

        // Wait for the spinner to disappear or the content section to appear (empty or with data)
        await Page.WaitForFunctionAsync("() => !document.querySelector('.spinner-border.spinner-border-sm')");
    }

    [Test]
    public async Task UserManagement_DisplaysAvatarImage_WhenProfilePhotoExists()
    {
        await Page.GotoAsync($"{BaseUrl}/admin/users");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForSelectorAsync($"[data-testid='user-avatar-{AppDbContext.SeedUserId}'] img", new PageWaitForSelectorOptions { Timeout = 15000 });

        var avatarImage = Page.Locator($"[data-testid='user-avatar-{AppDbContext.SeedUserId}'] img");
        await Assertions.Expect(avatarImage).ToBeVisibleAsync();
    }

    [Test]
    public async Task UserManagement_UsesInitialsFallback_WhenProfilePhotoMissing()
    {
        await Page.GotoAsync($"{BaseUrl}/admin/users");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForSelectorAsync($"[data-testid='user-avatar-{AdditionalUserId}'] span", new PageWaitForSelectorOptions { Timeout = 15000 });

        var avatarInitials = Page.Locator($"[data-testid='user-avatar-{AdditionalUserId}'] span");
        await Assertions.Expect(avatarInitials).ToContainTextAsync("S");
    }

    [Test]
    public async Task UserManagement_DisplayName_PrefersFirstAndLastName()
    {
        await Page.GotoAsync($"{BaseUrl}/admin/users");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForSelectorAsync($"[data-testid='user-row-{AppDbContext.SeedUserId}']", new PageWaitForSelectorOptions { Timeout = 15000 });

        var userRow = Page.Locator($"[data-testid='user-row-{AppDbContext.SeedUserId}']");
        await Assertions.Expect(userRow).ToContainTextAsync("Dev Admin");
    }

    [Test]
    public async Task NavMenu_AdminLink_VisibleForAdminUser()
    {
        await Page.GotoAsync($"{BaseUrl}/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var adminLink = Page.GetByTestId("nav-admin-link");
        await Assertions.Expect(adminLink).ToBeVisibleAsync();
    }

    [Test]
    public async Task NavMenu_AutoApplyLink_VisibleForAllUsers()
    {
        await Page.GotoAsync($"{BaseUrl}/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var autoApplyLink = Page.Locator("nav a[href='settings/auto-apply']");
        await Assertions.Expect(autoApplyLink).ToBeVisibleAsync();
    }

    [Test]
    public async Task AdminPanel_RepeatedCrossTabNavigation_DoesNotRedirectAwayFromAdminPages()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await Page.GotoAsync($"{BaseUrl}/admin/settings");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            Assert.That(Page.Url, Does.Contain("/admin/settings"));
            await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions
            {
                Name = "Systeem Instellingen"
            })).ToBeVisibleAsync();

            await Page.ClickAsync("[data-testid='admin-tab-monitoring']");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            Assert.That(Page.Url, Does.Contain("/admin/monitoring"));
            await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions
            {
                Name = "Auto-Apply Monitoring Dashboard"
            })).ToBeVisibleAsync();

            await Page.ClickAsync("[data-testid='admin-tab-users']");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            Assert.That(Page.Url, Does.Contain("/admin/users"));
            await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions
            {
                Name = "Gebruikersbeheer"
            })).ToBeVisibleAsync();
        }
    }

    [Test]
    public async Task AdminPanel_MlToMonitoring_DoesNotRedirectToDashboard()
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            await Page.GotoAsync($"{BaseUrl}/admin/ml");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            Assert.That(Page.Url, Does.Contain("/admin/ml"));
            await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions
            {
                Name = "ML modelinformatie"
            })).ToBeVisibleAsync();

            await Page.ClickAsync("[data-testid='admin-tab-monitoring']");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            Assert.That(Page.Url, Does.Contain("/admin/monitoring"));
            Assert.That(Page.Url, Does.Not.Contain("/dashboard"));
            await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions
            {
                Name = "Auto-Apply Monitoring Dashboard"
            })).ToBeVisibleAsync();
        }
    }

    [Test]
    public async Task AdminPanel_MlMonitoringUsers_StressNavigation_NoDashboardRedirect()
    {
        var iterations = int.TryParse(Environment.GetEnvironmentVariable("ADMIN_NAV_STRESS_ITERATIONS"), out var n) ? n : 5;

        for (var attempt = 0; attempt < iterations; attempt++)
        {
            await Page.GotoAsync($"{BaseUrl}/admin/ml");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            Assert.That(Page.Url, Does.Contain("/admin/ml"));

            await Page.ClickAsync("[data-testid='admin-tab-monitoring']");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            Assert.That(Page.Url, Does.Contain("/admin/monitoring"));
            Assert.That(Page.Url, Does.Not.Contain("/dashboard"));

            await Page.ClickAsync("[data-testid='admin-tab-users']");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            Assert.That(Page.Url, Does.Contain("/admin/users"));
            Assert.That(Page.Url, Does.Not.Contain("/dashboard"));

            await Page.ClickAsync("[data-testid='admin-tab-settings']");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            Assert.That(Page.Url, Does.Contain("/admin/settings"));
            Assert.That(Page.Url, Does.Not.Contain("/dashboard"));
        }
    }
}
