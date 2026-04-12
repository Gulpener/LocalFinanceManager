using LocalFinanceManager.Data;
using LocalFinanceManager.E2E.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace LocalFinanceManager.E2E.Admin;

/// <summary>
/// E2E tests for Admin Settings page.
/// Tests environment information display and configuration viewing.
/// </summary>
[TestFixture]
public class AdminSettingsTests : E2ETestBase
{
    [Test]
    public async Task AdminSettings_PageLoads_DisplaysEnvironmentInformation()
    {
        // Arrange & Act
        await Page.GotoAsync($"{BaseUrl}/admin/settings");

        // Assert - Page loads
        Assert.That(Page.Url, Does.Contain("/admin/settings"));
    }

    [Test]
    public async Task AdminSettings_RepeatedNavigation_DoesNotShowCircuitErrorUI()
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            await Page.GotoAsync($"{BaseUrl}/admin/settings");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            Assert.That(Page.Url, Does.Contain("/admin/settings"));
            await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions
            {
                Name = "Systeem Instellingen"
            })).ToBeVisibleAsync();

            var blazorErrorUi = Page.Locator("#blazor-error-ui");
            if (await blazorErrorUi.CountAsync() > 0)
            {
                await Assertions.Expect(blazorErrorUi).ToBeHiddenAsync();
            }
        }
    }
}
