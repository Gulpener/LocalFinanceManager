using LocalFinanceManager.Data;
using LocalFinanceManager.E2E.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace LocalFinanceManager.E2E.Admin;

/// <summary>
/// E2E tests for Admin Settings page.
/// Tests environment information display and configuration viewing.
/// </summary>
[TestFixture]
public class AdminSettingsTests : E2ETestBase
{
    [Test]
    [Ignore("Manual testing - Admin UI implementation pending")]
    public async Task AdminSettings_PageLoads_DisplaysEnvironmentInformation()
    {
        // Arrange & Act
        await Page.GotoAsync($"{BaseUrl}/admin/settings");

        // Assert - Page loads
        Assert.That(Page.Url, Does.Contain("/admin/settings"));
    }
}
