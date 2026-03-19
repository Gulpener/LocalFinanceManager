using LocalFinanceManager.Data;
using LocalFinanceManager.E2E.Helpers;
using LocalFinanceManager.E2E.Pages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace LocalFinanceManager.E2E.Tests;

[TestFixture]
public class BasicAssignmentTests : E2ETestBase
{
    private TransactionsPageModel _transactionsPage = null!;
    private AssignmentModalPageModel _assignmentModal = null!;
    private Guid _accountId;
    private Guid _budgetLineFood;
    private Guid _budgetLineTransport;

    [SetUp]
    public async Task SetUp()
    {
        _transactionsPage = new TransactionsPageModel(Page, BaseUrl);
        _assignmentModal = new AssignmentModalPageModel(Page, BaseUrl);

        await Factory!.TruncateTablesAsync();

        using var scope = Factory.CreateDbScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var account = await SeedDataHelper.SeedAccountAsync(context, "Assignment Test Account", "NL91ABNA0417164300", 1000m);
        _accountId = account.Id;

        var categories = await SeedDataHelper.SeedCategoriesAsync(context, account.CurrentBudgetPlanId!.Value, "Food", "Transport");
        _budgetLineFood = (await SeedDataHelper.SeedBudgetLineAsync(context, account.CurrentBudgetPlanId!.Value, categories[0].Id, 500m)).Id;
        _budgetLineTransport = (await SeedDataHelper.SeedBudgetLineAsync(context, account.CurrentBudgetPlanId!.Value, categories[1].Id, 400m)).Id;

        await SeedDataHelper.SeedTransactionAsync(context, _accountId, -15m, DateTime.UtcNow.AddDays(-1), "Unassigned Tx A");
        await SeedDataHelper.SeedTransactionAsync(context, _accountId, -45m, DateTime.UtcNow.AddDays(-2), "Unassigned Tx B");

        // Clear localStorage filter state to prevent cross-test contamination.
        // FilterStateService persists 'transactionFilters' across navigations; a stale
        // 'Assigned' filter would hide unassigned rows in the next test.
        await Page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Page.EvaluateAsync("() => localStorage.removeItem('transactionFilters')");
    }

    [Test]
    public async Task TransactionsPage_ShowsUnassignedWarningBadges()
    {
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_accountId);

        var warnings = await Page.Locator("tr[data-testid='transaction-row'] .badge.bg-warning").CountAsync();
        Assert.That(warnings, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public async Task ClickingAssign_OpensModal_WithTransactionDetails()
    {
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_accountId);

        await Page.Locator("tr[data-testid='transaction-row']:has-text('Unassigned Tx A') button:has-text('Toewijzen')").ClickAsync();
        await _assignmentModal.WaitForModalAsync();

        Assert.That(await _assignmentModal.IsModalVisibleAsync(), Is.True);
        Assert.That(await Page.Locator("#transactionAssignModal").TextContentAsync(), Does.Contain("Unassigned Tx A"));
    }

    [Test]
    public async Task AssignTransaction_UpdatesRowToAssigned()
    {
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_accountId);

        await Page.Locator("tr[data-testid='transaction-row']:has-text('Unassigned Tx A') button:has-text('Toewijzen')").ClickAsync();
        await Page.SelectOptionAsync("#budgetLineSelect", _budgetLineFood.ToString());
        await Expect(Page.Locator("#assignSaveButton")).ToBeEnabledAsync(new LocatorAssertionsToBeEnabledOptions { Timeout = 5_000 });
        await Page.ClickAsync("#assignSaveButton");

        // Do NOT wait for the modal to close here — that relies on the JS interop releaseFocusTrap
        // call (step 5), which is slow under sustained SignalR load.
        // The row updates in OnAssignmentSuccess → LoadTransactionsAsync → StateHasChanged (step 4),
        // which always completes BEFORE the modal close. Poll directly for the expected row state.
        await Expect(Page.Locator("tr[data-testid='transaction-row']:has-text('Unassigned Tx A')")
            ).ToContainTextAsync("Food", new LocatorAssertionsToContainTextOptions { Timeout = 45_000 });
    }

    [Test]
    public async Task AssignedTransaction_ShowsBudgetLineBadge()
    {
        using (var scope = Factory!.CreateDbScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tx = await context.Transactions.FirstAsync(t => t.Description == "Unassigned Tx A");
            await SeedDataHelper.AssignTransactionAsync(context, tx.Id, _budgetLineFood, "preset assignment");
        }

        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_accountId);

        var rowText = await Page.Locator("tr[data-testid='transaction-row']:has-text('Unassigned Tx A')").TextContentAsync();
        Assert.That(rowText, Does.Contain("Food"));
        Assert.That(rowText, Does.Not.Contain("Niet toegewezen"));
    }

    [Test]
    public async Task Reassign_ChangesBudgetLineFromFoodToTransport()
    {
        Guid transactionId;

        using (var scope = Factory!.CreateDbScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tx = await context.Transactions.FirstAsync(t => t.Description == "Unassigned Tx A");
            transactionId = tx.Id;
            await SeedDataHelper.AssignTransactionAsync(context, tx.Id, _budgetLineFood, "initial");
        }

        using (var scope = Factory!.CreateDbScope())
        {
            var assignmentService = scope.ServiceProvider.GetRequiredService<LocalFinanceManager.Services.ITransactionAssignmentService>();
            await assignmentService.AssignTransactionAsync(transactionId, new LocalFinanceManager.DTOs.AssignTransactionRequest
            {
                BudgetLineId = _budgetLineTransport,
                Note = "reassigned"
            });
        }

        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_accountId);
        var rowText = await Page.Locator("tr[data-testid='transaction-row']:has-text('Unassigned Tx A')").TextContentAsync();
        Assert.That(rowText, Does.Contain("Transport").Or.Contain("splits"));
    }

    [Test]
    public async Task CrossBudgetPlanAssignment_IsRejectedByService()
    {
        Guid foreignBudgetLineId;
        Guid transactionId;

        using (var scope = Factory!.CreateDbScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var otherAccount = await SeedDataHelper.SeedAccountAsync(context, "Other Account", "NL91ABNA0417164301", 500m);
            var categories = await SeedDataHelper.SeedCategoriesAsync(context, otherAccount.CurrentBudgetPlanId!.Value, "Entertainment");
            foreignBudgetLineId = (await SeedDataHelper.SeedBudgetLineAsync(context, otherAccount.CurrentBudgetPlanId!.Value, categories[0].Id, 300m)).Id;
            transactionId = await context.Transactions
                .Where(t => t.AccountId == _accountId && t.Description == "Unassigned Tx A")
                .Select(t => t.Id)
                .FirstAsync();
        }

        using var assertionScope = Factory.CreateDbScope();
        var assignmentService = assertionScope.ServiceProvider.GetRequiredService<LocalFinanceManager.Services.ITransactionAssignmentService>();

        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await assignmentService.AssignTransactionAsync(transactionId, new LocalFinanceManager.DTOs.AssignTransactionRequest
            {
                BudgetLineId = foreignBudgetLineId
            }));

        Assert.That(ex!.Message, Does.Contain("different account budget plan"));
    }

    [Test]
    public async Task AuditTrailButton_OpensHistoryAfterAssignment()
    {
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_accountId);

        await Page.Locator("tr[data-testid='transaction-row']:has-text('Unassigned Tx A') button:has-text('Toewijzen')").ClickAsync();
        await Page.SelectOptionAsync("#budgetLineSelect", _budgetLineFood.ToString());
        // Wait for save button to be enabled (CanAssign=true) before clicking
        await Expect(Page.Locator("#assignSaveButton")).ToBeEnabledAsync(new LocatorAssertionsToBeEnabledOptions { Timeout = 5_000 });
        await Page.ClickAsync("#assignSaveButton");

        // Confirm the click registered on the server: isSubmitting=true disables the button briefly.
        // Under sustained suite load the SignalR round-trip may be slow; allow 15s.
        // If the button is still enabled after 15s the click may have been swallowed — retry once.
        var clickRegistered = false;
        try
        {
            await Expect(Page.Locator("#assignSaveButton")).ToBeDisabledAsync(
                new LocatorAssertionsToBeDisabledOptions { Timeout = 15_000 });
            clickRegistered = true;
        }
        catch (Microsoft.Playwright.PlaywrightException) { /* slow SignalR — check below */ }

        if (!clickRegistered)
        {
            // Only retry if the modal is still open; if the modal already closed the assignment
            // succeeded and the button is gone. Use a short timeout to avoid a 30s hang.
            // Playwright may throw PlaywrightException or TimeoutException when the element
            // disappears from the DOM before the timeout completes.
            try
            {
                if (await Page.Locator("#assignSaveButton").IsEnabledAsync(new LocatorIsEnabledOptions { Timeout = 2_000 }))
                    await Page.ClickAsync("#assignSaveButton");
            }
            catch (Microsoft.Playwright.PlaywrightException) { /* Button gone — modal already closed */ }
            catch (System.TimeoutException) { /* Button disappeared — modal already closed */ }
        }

        // Wait for the assignment modal to close before looking for the audit button.
        // The audit trail button only renders once IsAssigned=true (after Blazor re-renders).
        // Full chain: DB write → OnAssignmentSuccess → LoadTransactionsAsync → StateHasChanged
        //             → JS releaseFocusTrap → OnClose → re-render. Allow 60s under load.
        await Expect(Page.Locator("#transactionAssignModal")).Not.ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });

        // Wait for the transaction row to show as assigned (contains 'Food' or 'splits') before clicking the audit button
        var row = Page.Locator("tr[data-testid='transaction-row']:has-text('Unassigned Tx A')").First;
        await Expect(row).ToContainTextAsync(new[] { "Food", "splits" }, new LocatorAssertionsToContainTextOptions { Timeout = 30000 });

        // Now the audit button should be present and enabled
        var auditBtn = row.Locator("button[title='Bekijk toewijzingsgeschiedenis']");
        await Expect(auditBtn).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 15_000 });
        await auditBtn.ClickAsync();

        // Defensive: short wait to allow Blazor to render modal
        await Task.Delay(500);
        // Defensive: wait for audit trail modal title to exist before asserting visibility (CI timing)
        await Page.WaitForSelectorAsync("#auditTrailModalTitle", new() { Timeout = 30000 });
        await Expect(Page.Locator("#auditTrailModalTitle")).ToBeVisibleAsync();
        // Wait for loading spinner to disappear before reading modal content
        await Expect(Page.Locator(".modal.show .spinner-border")).Not.ToBeVisibleAsync();
        var content = await Page.Locator(".modal.show").TextContentAsync();
        Assert.That(content, Does.Contain("Assign").Or.Contain("Toegewezen"));
    }

    [Test]
    public async Task FilterUncategorized_ShowsOnlyUnassignedRows()
    {
        using (var scope = Factory!.CreateDbScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tx = await context.Transactions.FirstAsync(t => t.Description == "Unassigned Tx A");
            await SeedDataHelper.AssignTransactionAsync(context, tx.Id, _budgetLineFood, "for filter test");
        }

        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_accountId);
        await _transactionsPage.SelectFilterAsync("Uncategorized");

        var assignedRowsVisible = await Page.Locator("tr[data-testid='transaction-row']:has-text('Food')").CountAsync();
        Assert.That(assignedRowsVisible, Is.EqualTo(0));
    }

    [Test]
    public async Task FilterAssigned_ShowsOnlyAssignedRows()
    {
        using (var scope = Factory!.CreateDbScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tx = await context.Transactions.FirstAsync(t => t.Description == "Unassigned Tx A");
            await SeedDataHelper.AssignTransactionAsync(context, tx.Id, _budgetLineFood, "for assigned filter");
        }

        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_accountId);
        await _transactionsPage.SelectFilterAsync("Assigned");

        var rowCount = await Page.Locator("tr[data-testid='transaction-row']").CountAsync();
        var assignedRows = await Page.Locator("tr[data-testid='transaction-row']:has-text('Food')").CountAsync();
        Assert.That(rowCount, Is.GreaterThan(0));
        Assert.That(assignedRows, Is.GreaterThan(0));
    }

    [Test]
    public async Task SelectAllAndDeselectAll_UpdatesSelectionState()
    {
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_accountId);

        await _transactionsPage.SelectAllOnPageAsync();
        await Expect(Page.Locator("button:has-text('Bulk toewijzen')")).ToBeVisibleAsync();

        await _transactionsPage.DeselectAllAsync();
        await Expect(Page.Locator("button:has-text('Bulk toewijzen')")).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task BulkAssignButton_OpensBulkAssignModal()
    {
        using (var scope = Factory!.CreateDbScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedDataHelper.SeedTransactionAsync(context, _accountId, -80m, DateTime.UtcNow.AddDays(-3), "Bulk Tx");
        }

        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_accountId);

        await _transactionsPage.SelectAllOnPageAsync();
        await _transactionsPage.ClickBulkAssignAsync();

        await Expect(Page.Locator("#bulkAssignModal")).ToBeVisibleAsync();

        await _transactionsPage.TakeScreenshotAsync("assignment-modal-open");
    }
}
