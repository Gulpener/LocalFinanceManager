using LocalFinanceManager.Data;
using LocalFinanceManager.E2E.Helpers;
using LocalFinanceManager.E2E.Pages;
using LocalFinanceManager.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace LocalFinanceManager.E2E.Tests;

[TestFixture]
public class MultiAccountValidationTests : E2ETestBase
{
    [Test]
    public async Task MultiAccountWorkflow_EnforcesBudgetPlanIsolation()
    {
        await Factory!.TruncateTablesAsync();

        Guid accountAId;
        Guid accountBId;
        Guid accountABudgetLineId;
        Guid accountBBudgetLineId;
        Guid accountATransactionId;

        using (var scope = Factory.CreateDbScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var accountA = await SeedDataHelper.SeedAccountAsync(context, "Account A", "NL91ABNA0417164300", 1000m);
            var accountB = await SeedDataHelper.SeedAccountAsync(context, "Account B", "NL91ABNA0417164301", 800m);

            accountAId = accountA.Id;
            accountBId = accountB.Id;

            var categoriesA = await SeedDataHelper.SeedCategoriesAsync(context, accountA.CurrentBudgetPlanId!.Value, "Food");
            var categoriesB = await SeedDataHelper.SeedCategoriesAsync(context, accountB.CurrentBudgetPlanId!.Value, "Entertainment");

            accountABudgetLineId = (await SeedDataHelper.SeedBudgetLineAsync(context, accountA.CurrentBudgetPlanId!.Value, categoriesA[0].Id, 500m)).Id;
            accountBBudgetLineId = (await SeedDataHelper.SeedBudgetLineAsync(context, accountB.CurrentBudgetPlanId!.Value, categoriesB[0].Id, 600m)).Id;

            accountATransactionId = (await SeedDataHelper.SeedTransactionAsync(
                context,
                accountAId,
                -25m,
                new DateTime(DateTime.UtcNow.Year, 2, 20),
                "Account A Primary Tx")).Id;

            for (var i = 0; i < 24; i++)
            {
                await SeedDataHelper.SeedTransactionAsync(
                    context,
                    accountAId,
                    -10m - i,
                    new DateTime(DateTime.UtcNow.Year, 2, 1).AddDays(i),
                    $"Account A Tx {i}");

                await SeedDataHelper.SeedTransactionAsync(
                    context,
                    accountBId,
                    -11m - i,
                    new DateTime(DateTime.UtcNow.Year, 2, 1).AddDays(i),
                    $"Account B Tx {i}");
            }
        }

        var transactionsPage = new TransactionsPageModel(Page, BaseUrl);

        await transactionsPage.NavigateAsync();
        await transactionsPage.SelectAccountFilterAsync(accountAId);

        await transactionsPage.TakeScreenshotAsync("multi-account-setup");

        using (var assignScope = Factory.CreateDbScope())
        {
            var assignmentService = assignScope.ServiceProvider.GetRequiredService<ITransactionAssignmentService>();
            await assignmentService.AssignTransactionAsync(accountATransactionId, new LocalFinanceManager.DTOs.AssignTransactionRequest
            {
                BudgetLineId = accountABudgetLineId
            });
        }

        await transactionsPage.NavigateAsync();
        await transactionsPage.SelectAccountFilterAsync(accountAId);
        await transactionsPage.TakeScreenshotAsync("multi-account-budget-line-filter");

        using (var validationScope = Factory.CreateDbScope())
        {
            var assignmentService = validationScope.ServiceProvider.GetRequiredService<ITransactionAssignmentService>();
            var validationException = Assert.ThrowsAsync<ArgumentException>(async () =>
                await assignmentService.AssignTransactionAsync(accountATransactionId, new LocalFinanceManager.DTOs.AssignTransactionRequest
                {
                    BudgetLineId = accountBBudgetLineId
                }));

            Assert.That(validationException!.Message, Does.Contain("different account budget plan"));
        }

        await transactionsPage.TakeScreenshotAsync("multi-account-validation-error");

        // Verify account filter isolation still holds for account A
        var tableText = await Page.Locator("table[data-testid='transactions-table']").TextContentAsync();
        Assert.That(tableText, Does.Not.Contain("Entertainment"));

        // Open audit trail and verify assignment history is visible
        await Page.ClickAsync("tr[data-testid='transaction-row']:has-text('Account A Primary Tx') button[title='Bekijk toewijzingsgeschiedenis']");
        await Expect(Page.Locator("#auditTrailModalTitle")).ToBeVisibleAsync();

        var auditText = await Page.Locator(".modal.show").TextContentAsync();
        Assert.That(auditText, Does.Contain("Assign").Or.Contain("Toegewezen"));
    }
}
