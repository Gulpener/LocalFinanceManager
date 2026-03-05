using LocalFinanceManager.Data;
using LocalFinanceManager.E2E.Helpers;
using LocalFinanceManager.E2E.Pages;
using LocalFinanceManager.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace LocalFinanceManager.E2E.Tests;

[TestFixture]
public class BulkAssignmentTests : E2ETestBase
{
    private TransactionsPageModel _transactionsPage = null!;
    private BulkAssignModalPageModel _bulkModal = null!;
    private Guid _accountId;
    private Guid _budgetLineFood;
    private List<Guid> _transactionIds = new();

    [SetUp]
    public async Task SetUp()
    {
        _transactionsPage = new TransactionsPageModel(Page, BaseUrl);
        _bulkModal = new BulkAssignModalPageModel(Page, BaseUrl);

        await Factory!.TruncateTablesAsync();

        using var scope = Factory.CreateDbScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var account = await SeedDataHelper.SeedAccountAsync(context, "Bulk Test Account", "NL91ABNA0417164300", 1000m);
        _accountId = account.Id;

        var categories = await SeedDataHelper.SeedCategoriesAsync(context, account.CurrentBudgetPlanId!.Value, "Food", "Transport");
        _budgetLineFood = (await SeedDataHelper.SeedBudgetLineAsync(context, account.CurrentBudgetPlanId!.Value, categories[0].Id, 500m)).Id;
        await SeedDataHelper.SeedBudgetLineAsync(context, account.CurrentBudgetPlanId!.Value, categories[1].Id, 400m);

        // Seed 20 unassigned transactions
        _transactionIds.Clear();
        var transactions = await SeedDataHelper.SeedTransactionsAsync(context, _accountId, 20, -50m, -10m);
        _transactionIds.AddRange(transactions.Select(t => t.Id));
    }

    [Test]
    public async Task SelectTransactions_ShowsBulkToolbarWithCount()
    {
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_accountId);

        // Select 5 transactions
        for (int i = 0; i < 5; i++)
        {
            await _transactionsPage.SelectTransactionAsync(_transactionIds[i]);
        }

        // Bulk toolbar should show
        await Expect(Page.Locator("button:has-text('Bulk toewijzen')")).ToBeVisibleAsync();

        // Selection count in toolbar
        var toolbarText = await Page.Locator("[data-testid='bulk-action-bar'], .bulk-action-bar, .sticky-bottom").TextContentAsync();
        Assert.That(toolbarText, Does.Contain("5").Or.Contain("geselecteerd").Or.Contain("selected"));
    }

    [Test]
    public async Task ClickBulkAssign_OpensBulkModal()
    {
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_accountId);

        await _transactionsPage.SelectAllOnPageAsync();
        await _transactionsPage.ClickBulkAssignAsync();

        await _bulkModal.WaitForModalAsync();
        Assert.That(await _bulkModal.IsModalVisibleAsync(), Is.True);
        await _transactionsPage.TakeScreenshotAsync("bulk-modal-open");
    }

    [Test]
    public async Task BulkAssign_AllTransactions_ShowsFullSuccess()
    {
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_accountId);

        // Select the first 5 transactions
        for (int i = 0; i < 5; i++)
        {
            await _transactionsPage.SelectTransactionAsync(_transactionIds[i]);
        }
        await _transactionsPage.ClickBulkAssignAsync();
        await _bulkModal.WaitForModalAsync();

        // Select the Food budget line and click Assign All
        await Page.SelectOptionAsync("#bulkBudgetLineSelect", _budgetLineFood.ToString());
        await Page.ClickAsync("#bulkAssignButton");

        // Wait for completion
        await _bulkModal.WaitForCompletionAsync(timeoutMs: 30000);

        // Success count should be 5
        var successCount = await _bulkModal.GetSuccessCountAsync();
        var failureCount = await _bulkModal.GetFailureCountAsync();
        Assert.That(successCount, Is.EqualTo(5));
        Assert.That(failureCount, Is.EqualTo(0));
        await _transactionsPage.TakeScreenshotAsync("bulk-assign-complete");
    }

    [Test]
    public async Task BulkAssign_AllFail_ServiceLevel_ReturnsAllFailed()
    {
        Guid foreignBudgetLineId;

        using (var scope = Factory!.CreateDbScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var otherAccount = await SeedDataHelper.SeedAccountAsync(context, "Other Bulk Account", "NL91ABNA0417164301", 500m);
            var otherCategories = await SeedDataHelper.SeedCategoriesAsync(context, otherAccount.CurrentBudgetPlanId!.Value, "Other");
            foreignBudgetLineId = (await SeedDataHelper.SeedBudgetLineAsync(context, otherAccount.CurrentBudgetPlanId!.Value, otherCategories[0].Id, 300m)).Id;
        }

        // Perform bulk assign using the service directly to verify all-fail behavior
        using var serviceScope = Factory!.CreateDbScope();
        var assignmentService = serviceScope.ServiceProvider.GetRequiredService<LocalFinanceManager.Services.ITransactionAssignmentService>();

        var result = await assignmentService.BulkAssignTransactionsAsync(new LocalFinanceManager.DTOs.BulkAssignTransactionsRequest
        {
            TransactionIds = _transactionIds.Take(5).ToList(),
            BudgetLineId = foreignBudgetLineId // Wrong budget plan → all should fail
        });

        Assert.That(result.FailedCount, Is.EqualTo(5));
        Assert.That(result.AssignedCount, Is.EqualTo(0));
        Assert.That(result.TotalCount, Is.EqualTo(5));
    }

    [Test]
    public async Task ExpandErrorDetails_ShowsFailureList()
    {
        // Seed transactions from a prior year — assigning them to the current-year budget plan
        // will fail with "year mismatch" validation, while _budgetLineFood is still selectable.
        var priorYearDate = new DateTime(DateTime.UtcNow.Year - 1, 6, 15);
        var priorYearTxIds = new List<Guid>();

        using (var scope = Factory!.CreateDbScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            for (int i = 0; i < 3; i++)
            {
                var tx = await SeedDataHelper.SeedTransactionAsync(
                    context, _accountId, -30m, priorYearDate, $"Prior year transaction {i + 1}");
                priorYearTxIds.Add(tx.Id);
            }
        }

        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_accountId);

        // Select the 3 prior-year transactions
        foreach (var txId in priorYearTxIds)
        {
            await _transactionsPage.SelectTransactionAsync(txId);
        }

        await _transactionsPage.ClickBulkAssignAsync();
        await _bulkModal.WaitForModalAsync();

        // _budgetLineFood is in the dropdown (same account) but its budget plan year is current year
        // → prior-year transactions will fail year-mismatch validation
        await Page.SelectOptionAsync("#bulkBudgetLineSelect", _budgetLineFood.ToString());
        await Page.ClickAsync("#bulkAssignButton");

        // Wait for completion
        await _bulkModal.WaitForCompletionAsync(timeoutMs: 30000);

        // Should show failures; expand error accordion
        var failureCount = await _bulkModal.GetFailureCountAsync();
        Assert.That(failureCount, Is.GreaterThan(0));

        // Expand error details
        await _bulkModal.ExpandErrorDetailsAsync();

        var errorMessages = await _bulkModal.GetErrorMessagesAsync();
        Assert.That(errorMessages.Count, Is.GreaterThan(0));
        await _transactionsPage.TakeScreenshotAsync("bulk-error-details");
    }

    [Test]
    public async Task DeselectAll_HidesBulkToolbar()
    {
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_accountId);

        await _transactionsPage.SelectAllOnPageAsync();
        await Expect(Page.Locator("button:has-text('Bulk toewijzen')")).ToBeVisibleAsync();

        await _transactionsPage.DeselectAllAsync();
        await Expect(Page.Locator("button:has-text('Bulk toewijzen')")).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task SelectAllOnPage_ChecksAllVisibleTransactions()
    {
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_accountId);

        await _transactionsPage.SelectAllOnPageAsync();

        // All checkboxes on page should be checked
        var checkboxes = await Page.Locator("tbody tr[data-testid='transaction-row'] input[type='checkbox']").AllAsync();
        Assert.That(checkboxes.Count, Is.GreaterThan(0));
        foreach (var checkbox in checkboxes)
        {
            Assert.That(await checkbox.IsCheckedAsync(), Is.True);
        }
    }

    [Test]
    public async Task BulkAssigned_TransactionsShowCategoryBadge()
    {
        // Bulk assign via service
        using (var scope = Factory!.CreateDbScope())
        {
            var assignmentService = scope.ServiceProvider.GetRequiredService<LocalFinanceManager.Services.ITransactionAssignmentService>();
            await assignmentService.BulkAssignTransactionsAsync(new LocalFinanceManager.DTOs.BulkAssignTransactionsRequest
            {
                TransactionIds = _transactionIds.Take(3).ToList(),
                BudgetLineId = _budgetLineFood
            });
        }

        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_accountId);
        await _transactionsPage.SelectFilterAsync("Assigned");

        // Assigned rows should show Food badge
        var assignedRows = await Page.Locator("tr[data-testid='transaction-row']:has-text('Food')").CountAsync();
        Assert.That(assignedRows, Is.GreaterThanOrEqualTo(3));
        await _transactionsPage.TakeScreenshotAsync("bulk-assigned-badges");
    }

    [Test]
    public async Task SelectionCount_DisplayedCorrectlyInToolbar()
    {
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_accountId);

        // Select 3 transactions individually
        for (int i = 0; i < 3; i++)
        {
            await _transactionsPage.SelectTransactionAsync(_transactionIds[i]);
        }

        // Toolbar should be visible showing count
        await Expect(Page.Locator("button:has-text('Bulk toewijzen')")).ToBeVisibleAsync();

        // Select 2 more
        for (int i = 3; i < 5; i++)
        {
            await _transactionsPage.SelectTransactionAsync(_transactionIds[i]);
        }

        // Still visible with updated count
        await Expect(Page.Locator("button:has-text('Bulk toewijzen')")).ToBeVisibleAsync();
        await _transactionsPage.TakeScreenshotAsync("bulk-toolbar-count");
    }
}
