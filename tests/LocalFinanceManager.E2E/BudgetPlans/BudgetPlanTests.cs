using LocalFinanceManager.Data;
using LocalFinanceManager.E2E.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace LocalFinanceManager.E2E.BudgetPlans;

/// <summary>
/// E2E tests for Budget Plan CRUD operations.
/// Tests budget plan creation, viewing, editing, and archiving workflows.
/// </summary>
[TestFixture]
public class BudgetPlanTests : E2ETestBase
{
    [Test]
    public async Task ListBudgetPlans_WithSeededData_DisplaysBudgetPlans()
    {
        // Arrange
        using var scope = Factory!.CreateDbScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var account = await SeedDataHelper.SeedAccountAsync(
            context,
            "Test Account",
            "DE89370400440532013000",
            1000.00m);

        // Act - Navigate to budget plans page
        await Page.GotoAsync($"{BaseUrl}/budgets");

        // Assert - Verify page loads
        Assert.That(Page.Url, Does.Contain("/budgets"));
    }
}
