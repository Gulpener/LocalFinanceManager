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
        await Page.ClickAsync("#assignSaveButton");

        await Expect(Page.Locator("#transactionAssignModal")).Not.ToBeVisibleAsync();
        var rowText = await Page.Locator("tr[data-testid='transaction-row']:has-text('Unassigned Tx A')").TextContentAsync();
        Assert.That(rowText, Does.Contain("Food"));
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

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
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
        await Page.ClickAsync("#assignSaveButton");

        var row = Page.Locator("tr[data-testid='transaction-row']:has-text('Unassigned Tx A')").First;
        await row.Locator("button[title='Bekijk toewijzingsgeschiedenis']").ClickAsync();

        await Expect(Page.Locator("#auditTrailModalTitle")).ToBeVisibleAsync();
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
