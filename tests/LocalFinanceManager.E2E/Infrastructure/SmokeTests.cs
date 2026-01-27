using LocalFinanceManager.Data;
using LocalFinanceManager.E2E.Helpers;
using LocalFinanceManager.E2E.Pages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace LocalFinanceManager.E2E.Infrastructure;

/// <summary>
/// Smoke tests to verify E2E test infrastructure is working correctly.
/// These tests validate the test harness, seed helpers, and page object models.
/// </summary>
[TestFixture]
public class SmokeTests : E2ETestBase
{
    [Test]
    public async Task Application_And_Browser_Infrastructure_Works()
    {
        // Arrange
        TestContext.Out.WriteLine($"Navigating to: {BaseUrl}");

        // Act - Navigate to home page with extended timeout for Blazor Server initialization
        var response = await Page.GotoAsync(BaseUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 30000 // 30 seconds for Blazor Server SignalR connection
        });

        TestContext.Out.WriteLine($"Navigation response status: {response?.Status}");
        TestContext.Out.WriteLine($"Final URL: {Page.Url}");

        var title = await Page.TitleAsync();
        TestContext.Out.WriteLine($"Page title: {title}");

        // Assert - Verify application starts, page loads, and browser renders content
        Assert.That(response?.Status, Is.EqualTo(200), "Application should return 200 OK");
        Assert.That(Page.Url, Does.Contain(BaseUrl), "Page URL should match base URL");
        Assert.That(title, Is.Not.Null.And.Not.Empty, "Page title should be rendered");
    }

    [Test]
    public async Task Database_Connection_Successful()
    {
        // Arrange
        using var scope = Factory!.CreateDbScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Act - Query database to verify connection
        var accountCount = await context.Accounts.CountAsync();

        // Assert - No exception thrown, connection successful
        Assert.That(accountCount, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public async Task SeedDataHelper_Creates_Account_Successfully()
    {
        // Arrange
        using var scope = Factory!.CreateDbScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Act - Seed account using helper
        var account = await SeedDataHelper.SeedAccountAsync(
            context,
            "Test Account",
            "DE89370400440532013000",
            1000.00m,
            "EUR");

        // Assert - Verify account created
        Assert.That(account, Is.Not.Null);
        Assert.That(account.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(account.Label, Is.EqualTo("Test Account"));
        Assert.That(account.StartingBalance, Is.EqualTo(1000.00m));
        Assert.That(account.CurrentBudgetPlanId, Is.Not.Null);

        // Verify budget plan was created
        var budgetPlan = context.BudgetPlans.FirstOrDefault(bp => bp.Id == account.CurrentBudgetPlanId);
        Assert.That(budgetPlan, Is.Not.Null);
        Assert.That(budgetPlan!.Year, Is.EqualTo(DateTime.UtcNow.Year));
    }

    [Test]
    public async Task SeedDataHelper_Creates_Categories_Successfully()
    {
        // Arrange
        using var scope = Factory!.CreateDbScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var account = await SeedDataHelper.SeedAccountAsync(
            context,
            "Test Account",
            "DE89370400440532013000",
            1000.00m);

        // Act - Seed categories
        var categories = await SeedDataHelper.SeedCategoriesAsync(
            context,
            account.CurrentBudgetPlanId!.Value,
            incomeCount: 2,
            expenseCount: 3);

        // Assert
        Assert.That(categories.Count, Is.EqualTo(5));
        Assert.That(categories.Count(c => c.Type == Models.CategoryType.Income), Is.EqualTo(2));
        Assert.That(categories.Count(c => c.Type == Models.CategoryType.Expense), Is.EqualTo(3));
    }

    [Test]
    public async Task SeedDataHelper_Creates_Transactions_Successfully()
    {
        // Arrange
        using var scope = Factory!.CreateDbScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var account = await SeedDataHelper.SeedAccountAsync(
            context,
            "Test Account",
            "DE89370400440532013000",
            1000.00m);

        // Act - Seed transactions
        var transactions = await SeedDataHelper.SeedTransactionsAsync(
            context,
            account.Id,
            count: 10,
            minAmount: -100.00m,
            maxAmount: 200.00m);

        // Assert
        Assert.That(transactions.Count, Is.EqualTo(10));
        Assert.That(transactions.All(t => t.AccountId == account.Id), Is.True);
        Assert.That(transactions.All(t => t.Amount >= -100.00m && t.Amount <= 200.00m), Is.True);
    }

    [Test]
    public async Task SeedDataHelper_Creates_MLData_Successfully()
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

        await SeedDataHelper.SeedTransactionsAsync(
            context,
            account.Id,
            count: 5,
            minAmount: -100.00m,
            maxAmount: 200.00m);

        // Act - Seed ML data
        var labeledExamples = await SeedDataHelper.SeedMLDataAsync(
            context,
            account.Id,
            labeledExamplesCount: 5);

        // Assert
        Assert.That(labeledExamples.Count, Is.EqualTo(5));
        Assert.That(labeledExamples.All(le => le.CategoryId != Guid.Empty), Is.True);
        Assert.That(labeledExamples.All(le => le.TransactionId != Guid.Empty), Is.True);
    }

    [Test]
    public async Task SeedDataHelper_Creates_AutoApplyHistory_Successfully()
    {
        // Arrange
        using var scope = Factory!.CreateDbScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var account = await SeedDataHelper.SeedAccountAsync(
            context,
            "Test Account",
            "DE89370400440532013000",
            1000.00m);

        await SeedDataHelper.SeedTransactionsAsync(
            context,
            account.Id,
            count: 10,
            minAmount: -100.00m,
            maxAmount: 200.00m);

        // Act - Seed auto-apply history
        var auditEntries = await SeedDataHelper.SeedAutoApplyHistoryAsync(
            context,
            account.Id,
            totalCount: 10,
            undoCount: 3);

        // Assert
        Assert.That(auditEntries.Count, Is.EqualTo(10));
        Assert.That(auditEntries.Count(ae => ae.ActionType == "Undo"), Is.EqualTo(3));
        Assert.That(auditEntries.Count(ae => ae.ActionType == "AutoAssign"), Is.EqualTo(7));
        Assert.That(auditEntries.All(ae => ae.IsAutoApplied), Is.True);
    }

    [Test]
    public async Task PageObjectModel_Navigation_Works()
    {
        // Arrange
        using var scope = Factory!.CreateDbScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Seed test data
        var account = await SeedDataHelper.SeedAccountAsync(
            context,
            "Test Account",
            "DE89370400440532013000",
            1000.00m);

        await SeedDataHelper.SeedTransactionsAsync(
            context,
            account.Id,
            count: 5,
            minAmount: -100.00m,
            maxAmount: 200.00m);

        // Act - Use PageObjectModel to navigate
        var transactionsPage = new TransactionsPageModel(Page, BaseUrl);
        await transactionsPage.NavigateAsync();

        // Assert - Verify navigation successful
        Assert.That(Page.Url, Does.Contain("/transactions"));
    }
}
