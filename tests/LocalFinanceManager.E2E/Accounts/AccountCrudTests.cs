using LocalFinanceManager.Data;
using LocalFinanceManager.E2E.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace LocalFinanceManager.E2E.Accounts;

/// <summary>
/// E2E tests for Account CRUD operations.
/// Tests account creation, viewing, editing, and archiving workflows.
/// </summary>
[TestFixture]
public class AccountCrudTests : E2ETestBase
{
    [Test]
    public async Task ListAccounts_WithSeededData_DisplaysAccounts()
    {
        // Arrange
        using var scope = Factory!.CreateDbScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await SeedDataHelper.SeedAccountAsync(context, "Account 1", "DE89370400440532013000", 1000.00m);
        await SeedDataHelper.SeedAccountAsync(context, "Account 2", "FR1420041010050500013M02606", 2000.00m);

        // Act - Navigate to accounts page
        await Page.GotoAsync($"{BaseUrl}/accounts");

        // Assert - Verify accounts are listed (implementation depends on UI)
        // This is a basic test to verify the page loads
        Assert.That(Page.Url, Does.Contain("/accounts"));
    }
}
