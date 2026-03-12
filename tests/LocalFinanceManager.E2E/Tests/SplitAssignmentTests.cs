using LocalFinanceManager.Data;
using LocalFinanceManager.E2E.Helpers;
using LocalFinanceManager.E2E.Pages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace LocalFinanceManager.E2E.Tests;

[TestFixture]
public class SplitAssignmentTests : E2ETestBase
{
    private TransactionsPageModel _transactionsPage = null!;
    private SplitEditorPageModel _splitEditor = null!;
    private Guid _accountId;
    private Guid _budgetLineFood;
    private Guid _budgetLineTransport;
    private Guid _budgetLineEntertainment;
    private Guid _transactionId;

    [SetUp]
    public async Task SetUp()
    {
        _transactionsPage = new TransactionsPageModel(Page, BaseUrl);
        _splitEditor = new SplitEditorPageModel(Page, BaseUrl);

        await Factory!.TruncateTablesAsync();

        using var scope = Factory.CreateDbScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var account = await SeedDataHelper.SeedAccountAsync(context, "Split Test Account", "NL91ABNA0417164300", 1000m);
        _accountId = account.Id;

        var categories = await SeedDataHelper.SeedCategoriesAsync(context, account.CurrentBudgetPlanId!.Value, "Food", "Transport", "Entertainment");
        _budgetLineFood = (await SeedDataHelper.SeedBudgetLineAsync(context, account.CurrentBudgetPlanId!.Value, categories[0].Id, 500m)).Id;
        _budgetLineTransport = (await SeedDataHelper.SeedBudgetLineAsync(context, account.CurrentBudgetPlanId!.Value, categories[1].Id, 400m)).Id;
        _budgetLineEntertainment = (await SeedDataHelper.SeedBudgetLineAsync(context, account.CurrentBudgetPlanId!.Value, categories[2].Id, 300m)).Id;

        var tx = await SeedDataHelper.SeedTransactionAsync(context, _accountId, -100m, DateTime.UtcNow.AddDays(-1), "Split Test Transaction");
        _transactionId = tx.Id;

        // Clear localStorage filter state to prevent cross-test contamination.
        await Page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Page.EvaluateAsync("() => localStorage.removeItem('transactionFilters')");
    }

    [Test]
    public async Task ClickSplit_OpensSplitEditorModal()
    {
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_accountId);

        await _transactionsPage.ClickSplitButtonAsync(_transactionId);
        await _splitEditor.WaitForModalAsync();

        Assert.That(await _splitEditor.IsVisibleAsync(), Is.True);
        await _transactionsPage.TakeScreenshotAsync("split-editor-open");
    }

    [Test]
    public async Task AddSplits_WithValidSum_ShowsGreenValidation()
    {
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_accountId);
        await _transactionsPage.ClickSplitButtonAsync(_transactionId);
        await _splitEditor.WaitForModalAsync();

        // Add a 3rd split row (editor starts with 2)
        await _splitEditor.AddSplitRowAsync();

        // Set amounts: 40 + 35 + 25 = 100
        await _splitEditor.SetSplitAmountAsync(0, 40m);
        await _splitEditor.SetSplitAmountAsync(1, 35m);
        await _splitEditor.SetSplitAmountAsync(2, 25m);

        // Select categories
        await _splitEditor.SelectSplitCategoryAsync(0, _budgetLineFood);
        await _splitEditor.SelectSplitCategoryAsync(1, _budgetLineTransport);
        await _splitEditor.SelectSplitCategoryAsync(2, _budgetLineEntertainment);

        Assert.That(await _splitEditor.IsSumValidAsync(), Is.True);
        await _transactionsPage.TakeScreenshotAsync("split-editor-valid-sum");
    }

    [Test]
    public async Task SumMismatch_ShowsInvalidValidation_AndDisablesSaveButton()
    {
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_accountId);
        await _transactionsPage.ClickSplitButtonAsync(_transactionId);
        await _splitEditor.WaitForModalAsync();

        // Set amounts: 40 + 50 = 90 ≠ 100
        await _splitEditor.SetSplitAmountAsync(0, 40m);
        await _splitEditor.SetSplitAmountAsync(1, 50m);

        Assert.That(await _splitEditor.IsSumValidAsync(), Is.False);

        var saveButton = Page.Locator("button[data-action='save-split']");
        Assert.That(await saveButton.IsDisabledAsync(), Is.True);
        await _transactionsPage.TakeScreenshotAsync("split-editor-invalid-sum");
    }

    [Test]
    public async Task AdjustSplitToMatchSum_EnablesSaveButton()
    {
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_accountId);
        await _transactionsPage.ClickSplitButtonAsync(_transactionId);
        await _splitEditor.WaitForModalAsync();

        // Initially invalid: 40 + 50 = 90
        await _splitEditor.SetSplitAmountAsync(0, 40m);
        await _splitEditor.SetSplitAmountAsync(1, 50m);
        Assert.That(await _splitEditor.IsSumValidAsync(), Is.False);

        // Fix: 40 + 60 = 100
        await _splitEditor.SetSplitAmountAsync(1, 60m);
        await _splitEditor.SelectSplitCategoryAsync(0, _budgetLineFood);
        await _splitEditor.SelectSplitCategoryAsync(1, _budgetLineTransport);

        Assert.That(await _splitEditor.IsSumValidAsync(), Is.True);

        var saveButton = Page.Locator("button[data-action='save-split']");
        Assert.That(await saveButton.IsDisabledAsync(), Is.False);
    }

    [Test]
    public async Task SaveValidSplit_ShowsSplitBadgeOnRow()
    {
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_accountId);
        await _transactionsPage.ClickSplitButtonAsync(_transactionId);
        await _splitEditor.WaitForModalAsync();

        // Set valid split: 60 + 40 = 100
        await _splitEditor.SetSplitAmountAsync(0, 60m);
        await _splitEditor.SetSplitAmountAsync(1, 40m);
        await _splitEditor.SelectSplitCategoryAsync(0, _budgetLineFood);
        await _splitEditor.SelectSplitCategoryAsync(1, _budgetLineTransport);

        await _splitEditor.ClickSaveAsync();

        // Wait for the modal to close and page to refresh
        await Expect(Page.Locator("#splitEditorModal")).Not.ToBeVisibleAsync();

        // Row should show split badge (blue info badge)
        var row = Page.Locator($"tr[data-testid='transaction-row'][data-transaction-id='{_transactionId}']");
        await row.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

        var splitBadge = row.Locator(".badge.bg-info[aria-label='Gesplitst']");
        await Expect(splitBadge).ToBeVisibleAsync();
        await _transactionsPage.TakeScreenshotAsync("split-editor-saved");
    }

    [Test]
    public async Task RemoveSplitRow_UpdatesSumValidation()
    {
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_accountId);
        await _transactionsPage.ClickSplitButtonAsync(_transactionId);
        await _splitEditor.WaitForModalAsync();

        // Add a 3rd row (starts with 2)
        await _splitEditor.AddSplitRowAsync();
        Assert.That(await _splitEditor.GetSplitRowCountAsync(), Is.EqualTo(3));

        // Set amounts in 3 rows: 40 + 35 + 25 = 100
        await _splitEditor.SetSplitAmountAsync(0, 40m);
        await _splitEditor.SetSplitAmountAsync(1, 35m);
        await _splitEditor.SetSplitAmountAsync(2, 25m);
        Assert.That(await _splitEditor.IsSumValidAsync(), Is.True);

        // Remove the last row (index 2), now 40 + 35 = 75 ≠ 100
        await _splitEditor.RemoveSplitRowAsync(2);
        Assert.That(await _splitEditor.GetSplitRowCountAsync(), Is.EqualTo(2));
        Assert.That(await _splitEditor.IsSumValidAsync(), Is.False);
    }

    [Test]
    public async Task CrossBudgetPlanAssignment_RejectedByService()
    {
        Guid foreignBudgetLineId;
        Guid transactionId2;

        using (var scope = Factory!.CreateDbScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var otherAccount = await SeedDataHelper.SeedAccountAsync(context, "Other Account Split", "NL91ABNA0417164301", 500m);
            var otherCategories = await SeedDataHelper.SeedCategoriesAsync(context, otherAccount.CurrentBudgetPlanId!.Value, "Other");
            foreignBudgetLineId = (await SeedDataHelper.SeedBudgetLineAsync(context, otherAccount.CurrentBudgetPlanId!.Value, otherCategories[0].Id, 300m)).Id;
            transactionId2 = _transactionId;
        }

        using var assertScope = Factory!.CreateDbScope();
        var assignmentService = assertScope.ServiceProvider.GetRequiredService<LocalFinanceManager.Services.ITransactionAssignmentService>();

        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await assignmentService.SplitTransactionAsync(transactionId2, new LocalFinanceManager.DTOs.SplitTransactionRequest
            {
                Splits = new List<LocalFinanceManager.DTOs.SplitAllocationDto>
                {
                    new() { BudgetLineId = _budgetLineFood, Amount = 60m },
                    new() { BudgetLineId = foreignBudgetLineId, Amount = 40m }
                },
                RowVersion = Array.Empty<byte>()
            }));

        Assert.That(ex, Is.Not.Null);
    }

    [Test]
    public async Task ResplitTransaction_ReplacesExistingSplits()
    {
        // First split: 60 + 40
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_accountId);
        await _transactionsPage.ClickSplitButtonAsync(_transactionId);
        await _splitEditor.WaitForModalAsync();

        await _splitEditor.SetSplitAmountAsync(0, 60m);
        await _splitEditor.SetSplitAmountAsync(1, 40m);
        await _splitEditor.SelectSplitCategoryAsync(0, _budgetLineFood);
        await _splitEditor.SelectSplitCategoryAsync(1, _budgetLineTransport);
        await _splitEditor.ClickSaveAsync();
        await Expect(Page.Locator("#splitEditorModal")).Not.ToBeVisibleAsync();

        // Re-split: 70 + 30
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_accountId);
        await _transactionsPage.ClickSplitButtonAsync(_transactionId);
        await _splitEditor.WaitForModalAsync();

        // Editor re-opens pre-filled with existing splits (2 rows)
        Assert.That(await _splitEditor.GetSplitRowCountAsync(), Is.EqualTo(2));

        await _splitEditor.SetSplitAmountAsync(0, 70m);
        await _splitEditor.SetSplitAmountAsync(1, 30m);
        await _splitEditor.SelectSplitCategoryAsync(0, _budgetLineFood);
        await _splitEditor.SelectSplitCategoryAsync(1, _budgetLineEntertainment);
        await _splitEditor.ClickSaveAsync();
        await Expect(Page.Locator("#splitEditorModal")).Not.ToBeVisibleAsync();

        // Verify the badge still shows split
        var row = Page.Locator($"tr[data-testid='transaction-row'][data-transaction-id='{_transactionId}']");
        var splitBadge = row.Locator(".badge.bg-info[aria-label='Gesplitst']");
        await Expect(splitBadge).ToBeVisibleAsync();

        // Verify database shows new split amounts
        using var scope = Factory!.CreateDbScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var splits = await ctx.TransactionSplits
            .Where(s => s.TransactionId == _transactionId && !s.IsArchived)
            .ToListAsync();
        Assert.That(splits.Count, Is.EqualTo(2));
        Assert.That(splits.Select(s => s.Amount), Is.EquivalentTo(new[] { 70m, 30m }));
    }

    [Test]
    public async Task AuditTrail_RecordsSplitOperation()
    {
        // Split the transaction via service
        using (var scope = Factory!.CreateDbScope())
        {
            var assignmentService = scope.ServiceProvider.GetRequiredService<LocalFinanceManager.Services.ITransactionAssignmentService>();
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tx = await ctx.Transactions.FindAsync(_transactionId);
            await assignmentService.SplitTransactionAsync(_transactionId, new LocalFinanceManager.DTOs.SplitTransactionRequest
            {
                Splits = new List<LocalFinanceManager.DTOs.SplitAllocationDto>
                {
                    new() { BudgetLineId = _budgetLineFood, Amount = 60m },
                    new() { BudgetLineId = _budgetLineTransport, Amount = 40m }
                },
                RowVersion = tx!.RowVersion
            });
        }

        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_accountId);

        await _transactionsPage.ClickAuditTrailAsync(_transactionId);
        await Expect(Page.Locator("#auditTrailModalTitle")).ToBeVisibleAsync();
        // Wait for loading spinner to disappear before reading modal content
        await Expect(Page.Locator(".modal.show .spinner-border")).Not.ToBeVisibleAsync();

        var auditText = await Page.Locator(".modal.show").TextContentAsync();
        Assert.That(auditText, Does.Contain("Split").Or.Contain("split").Or.Contain("Splits").Or.Contain("Gesplitst"));
    }
}
