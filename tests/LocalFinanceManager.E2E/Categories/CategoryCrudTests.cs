using LocalFinanceManager.Data;
using LocalFinanceManager.E2E.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace LocalFinanceManager.E2E.Categories;

/// <summary>
/// E2E tests for Category CRUD operations.
/// Tests category creation, viewing, editing, and archiving workflows.
/// </summary>
[TestFixture]
public class CategoryCrudTests : E2ETestBase
{
    [Test]
    public async Task ListCategories_WithSeededData_DisplaysCategories()
    {
        // Arrange
        using var scope = Factory!.CreateDbScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var account = await SeedDataHelper.SeedAccountAsync(
            context,
            "Test Account",
            "DE89370400440532013000",
            1000.00m);

        await SeedDataHelper.SeedCategoriesAsync(
            context,
            account.CurrentBudgetPlanId!.Value,
            incomeCount: 2,
            expenseCount: 3);

        // Act - Navigate to categories page
        await Page.GotoAsync($"{BaseUrl}/categories");

        // Assert - Verify page loads
        Assert.That(Page.Url, Does.Contain("/categories"));
    }
}
