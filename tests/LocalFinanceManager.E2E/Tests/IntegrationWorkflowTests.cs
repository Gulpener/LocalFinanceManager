using LocalFinanceManager.Data;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.E2E.Helpers;
using LocalFinanceManager.E2E.Pages;
using LocalFinanceManager.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace LocalFinanceManager.E2E.Tests;

/// <summary>
/// Integration workflow test validating cross-feature data flow:
/// basic assignment → bulk assignment → split assignment.
/// </summary>
[TestFixture]
public class IntegrationWorkflowTests : E2ETestBase
{
    [Test]
    public async Task IntegrationWorkflow_AssignBulkSplit_ValidatesCrossFeatureFlow()
    {
        await Factory!.TruncateTablesAsync();

        Guid accountId;
        Guid budgetLineFood;
        Guid budgetLineTransport;
        Guid budgetLineEntertainment;
        List<Guid> transactionIds;

        // === SETUP ===
        using (var scope = Factory.CreateDbScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var account = await SeedDataHelper.SeedAccountAsync(context, "Integration Workflow Account", "NL91ABNA0417164300", 5000m);
            accountId = account.Id;

            var categories = await SeedDataHelper.SeedCategoriesAsync(context, account.CurrentBudgetPlanId!.Value, "Food", "Transport", "Entertainment");
            budgetLineFood = (await SeedDataHelper.SeedBudgetLineAsync(context, account.CurrentBudgetPlanId!.Value, categories[0].Id, 600m)).Id;
            budgetLineTransport = (await SeedDataHelper.SeedBudgetLineAsync(context, account.CurrentBudgetPlanId!.Value, categories[1].Id, 500m)).Id;
            budgetLineEntertainment = (await SeedDataHelper.SeedBudgetLineAsync(context, account.CurrentBudgetPlanId!.Value, categories[2].Id, 400m)).Id;

            // Seed 35 transactions: 10 for basic, 20 for bulk, 5 for split
            var allTransactions = await SeedDataHelper.SeedTransactionsAsync(context, accountId, 35, -100m, -10m);
            transactionIds = allTransactions.Select(t => t.Id).ToList();
        }

        var transactionsPage = new TransactionsPageModel(Page, BaseUrl);
        var splitEditor = new SplitEditorPageModel(Page, BaseUrl);

        // Navigate to transactions page and confirm all 35 are visible (check unassigned)
        await transactionsPage.NavigateAsync();
        await transactionsPage.SelectAccountFilterAsync(accountId);
        await transactionsPage.TakeScreenshotAsync("workflow-start");

        var rowCount = await transactionsPage.GetTransactionCountAsync();
        Assert.That(rowCount, Is.GreaterThan(0), "Should have transactions loaded");

        // === BASIC ASSIGNMENT: Assign 10 transactions individually via service ===
        var basicAssignIds = transactionIds.Take(10).ToList();
        using (var scope = Factory.CreateDbScope())
        {
            var assignmentService = scope.ServiceProvider.GetRequiredService<ITransactionAssignmentService>();
            foreach (var txId in basicAssignIds)
            {
                await assignmentService.AssignTransactionAsync(txId, new AssignTransactionRequest
                {
                    BudgetLineId = budgetLineFood,
                    Note = "Integration workflow basic"
                });
            }
        }

        await transactionsPage.NavigateAsync();
        await transactionsPage.SelectAccountFilterAsync(accountId);
        await transactionsPage.SelectFilterAsync("Assigned");

        var assignedCount = await transactionsPage.GetTransactionCountAsync();
        Assert.That(assignedCount, Is.GreaterThanOrEqualTo(10), "10 basic assignments should be reflected");
        await transactionsPage.TakeScreenshotAsync("workflow-basic-assigned");

        // === BULK ASSIGNMENT: Bulk assign 20 transactions via service ===
        var bulkAssignIds = transactionIds.Skip(10).Take(20).ToList();
        using (var scope = Factory.CreateDbScope())
        {
            var assignmentService = scope.ServiceProvider.GetRequiredService<ITransactionAssignmentService>();
            var bulkResult = await assignmentService.BulkAssignTransactionsAsync(new BulkAssignTransactionsRequest
            {
                TransactionIds = bulkAssignIds,
                BudgetLineId = budgetLineTransport,
                Note = "Integration workflow bulk"
            });

            Assert.That(bulkResult.AssignedCount, Is.EqualTo(20), "All 20 bulk assignments should succeed");
            Assert.That(bulkResult.FailedCount, Is.EqualTo(0), "No bulk assignment failures expected");
        }

        await transactionsPage.NavigateAsync();
        await transactionsPage.SelectAccountFilterAsync(accountId);
        await transactionsPage.SelectFilterAsync("Assigned");
        var assignedAfterBulk = await transactionsPage.GetTransactionCountAsync();
        Assert.That(assignedAfterBulk, Is.GreaterThanOrEqualTo(30), "30 assigned after basic + bulk");
        await transactionsPage.TakeScreenshotAsync("workflow-bulk-assigned");

        // === SPLIT ASSIGNMENT: Split 5 transactions ===
        var splitIds = transactionIds.Skip(30).Take(5).ToList();
        using (var scope = Factory.CreateDbScope())
        {
            var assignmentService = scope.ServiceProvider.GetRequiredService<ITransactionAssignmentService>();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            foreach (var txId in splitIds)
            {
                var tx = await context.Transactions.FindAsync(txId);
                var absAmount = Math.Abs(tx!.Amount);
                var split1 = Math.Round(absAmount * 0.6m, 2);
                var split2 = absAmount - split1;

                await assignmentService.SplitTransactionAsync(txId, new SplitTransactionRequest
                {
                    Splits = new List<SplitAllocationDto>
                    {
                        new() { BudgetLineId = budgetLineFood, Amount = split1 },
                        new() { BudgetLineId = budgetLineEntertainment, Amount = split2 }
                    },
                    RowVersion = tx.RowVersion
                });
            }
        }

        await transactionsPage.NavigateAsync();
        await transactionsPage.SelectAccountFilterAsync(accountId);
        await transactionsPage.SelectFilterAsync("Assigned");
        var assignedAfterSplit = await transactionsPage.GetTransactionCountAsync();
        Assert.That(assignedAfterSplit, Is.GreaterThanOrEqualTo(35), "All 35 transactions assigned");
        await transactionsPage.TakeScreenshotAsync("workflow-split-assigned");

        // Verify split badges appear for split transactions
        await transactionsPage.NavigateAsync();
        await transactionsPage.SelectAccountFilterAsync(accountId);
        var splitBadges = await Page.Locator("tr[data-testid='transaction-row'] .badge.bg-info[aria-label='Gesplitst']").CountAsync();
        Assert.That(splitBadges, Is.GreaterThanOrEqualTo(5), "At least 5 split badges should be visible");

        // === VERIFY TOTALS: All 35 transactions assigned (10 basic + 20 bulk + 5 split) ===
        await transactionsPage.NavigateAsync();
        await transactionsPage.SelectAccountFilterAsync(accountId);
        await transactionsPage.SelectFilterAsync("Uncategorized");
        var unassignedCount = await transactionsPage.GetTransactionCountAsync();
        Assert.That(unassignedCount, Is.EqualTo(0), "All 35 transactions should be assigned");

        // === AUDIT TRAIL VERIFICATION ===
        // Check audit trail for one basic-assigned transaction
        // Switch to "Assigned" filter first: the persisted "Uncategorized" filter from the
        // previous step would hide all assigned transactions, causing ClickAuditTrailAsync
        // to time out waiting for the specific row to be visible.
        await transactionsPage.NavigateAsync();
        await transactionsPage.SelectAccountFilterAsync(accountId);
        await transactionsPage.SelectFilterAsync("Assigned");
        await transactionsPage.ClickAuditTrailAsync(basicAssignIds[0]);
        await Expect(Page.Locator("#auditTrailModalTitle")).ToBeVisibleAsync();
        var basicAuditText = await Page.Locator(".modal.show").TextContentAsync();
        Assert.That(basicAuditText, Does.Contain("assign").IgnoreCase);

        await Page.Keyboard.PressAsync("Escape");

        // Check audit trail for one split transaction
        await transactionsPage.SelectFilterAsync("Assigned");
        await transactionsPage.ClickAuditTrailAsync(splitIds[0]);
        await Expect(Page.Locator("#auditTrailModalTitle")).ToBeVisibleAsync();
        var splitAuditText = await Page.Locator(".modal.show").TextContentAsync();
        Assert.That(splitAuditText, Does.Contain("split").IgnoreCase);

        await transactionsPage.TakeScreenshotAsync("workflow-complete");
    }
}
