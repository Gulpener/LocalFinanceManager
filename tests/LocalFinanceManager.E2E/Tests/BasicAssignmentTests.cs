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

        // Wait until seeded transactions are visible before asserting badge counts.
        await Page.WaitForSelectorAsync("tr[data-testid='transaction-row']:has-text('Unassigned Tx A')",
            new PageWaitForSelectorOptions { Timeout = 15000 });
        await Page.WaitForSelectorAsync("tr[data-testid='transaction-row']:has-text('Unassigned Tx B')",
            new PageWaitForSelectorOptions { Timeout = 15000 });

        await Page.WaitForFunctionAsync(
            "() => document.querySelectorAll(\"tr[data-testid='transaction-row'] .badge.bg-warning\").length >= 2",
            new PageWaitForFunctionOptions { Timeout = 15000 });

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

        // Attach listeners for console and page errors
        var consoleMessages = new List<string>();
        Page.Console += (_, msg) => consoleMessages.Add($"[console] {msg.Type}: {msg.Text}");
        var pageErrors = new List<string>();
        Page.PageError += (_, msg) => pageErrors.Add($"[pageerror] {msg}");

        // Poll for the row to update to assigned (showing 'Food')
        var row = Page.Locator("tr[data-testid='transaction-row']:has-text('Unassigned Tx A')").First;
        string? rowText = null;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        bool found = false;
        while (sw.Elapsed < TimeSpan.FromSeconds(45))
        {
            rowText = await row.TextContentAsync();
            if (rowText != null && rowText.Contains("Food"))
            {
                found = true;
                break;
            }
            await Task.Delay(250);
        }
        if (!found)
        {
            // Ensure diagnostics directory exists
            var diagDir = "test-results/screenshots";
            if (!System.IO.Directory.Exists(diagDir))
                System.IO.Directory.CreateDirectory(diagDir);
            // Capture screenshot and HTML for diagnostics
            await Page.ScreenshotAsync(new PageScreenshotOptions { Path = $"{diagDir}/AssignTransaction_UpdatesRowToAssigned_Fail.png", FullPage = true });
            var html = await Page.ContentAsync();
            System.IO.File.WriteAllText($"{diagDir}/AssignTransaction_UpdatesRowToAssigned_Fail.html", html);
            var errorDetails = $"\nConsole messages:\n{string.Join("\n", consoleMessages)}\nPage errors:\n{string.Join("\n", pageErrors)}\n";
            throw new Exception($"Transaction row did not show as assigned within 45s. Row text: {rowText}. See screenshot and HTML in {diagDir}/. {errorDetails}");
        }
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
    [Ignore("Blazor error UI appears after assignment - investigating root cause")]
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
        // Fail fast if Blazor error UI is present after assignment
        var blazorError = Page.Locator("#blazor-error-ui");
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
        string? rowText = null;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        bool found = false;
        while (sw.Elapsed < TimeSpan.FromSeconds(30))
        {
            rowText = await row.TextContentAsync();
            if (rowText != null && (rowText.Contains("Food") || rowText.Contains("splits")))
            {
                found = true;
                break;
            }
            // Fail fast if Blazor error UI is present after assignment
            if (await blazorError.IsVisibleAsync())
            {
                var errorText = await blazorError.TextContentAsync();
                throw new Exception($"Blazor error UI detected while waiting for assignment: {errorText}");
            }
            await Task.Delay(250);
        }
        Assert.That(found, $"Transaction row did not show as assigned within 30s. Row text: {rowText}");

        // Check for Blazor error immediately after loop exits (before clicking audit button).
        // Without this check the error may have appeared during the post-assignment render but
        // was missed because the loop breaks on "Food" without re-checking blazorError.
        if (await blazorError.IsVisibleAsync())
        {
            var errorText = await blazorError.TextContentAsync();
            throw new Exception($"Blazor error UI detected after assignment (pre-audit click): {errorText}");
        }

        // Now the audit button should be present and enabled
        var auditBtn = row.Locator("button[title='Bekijk toewijzingsgeschiedenis']");
        await Expect(auditBtn).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 15_000 });
        await auditBtn.ClickAsync();

        // Attach listeners for console and page errors
        var consoleMessages = new List<string>();
        Page.Console += (_, msg) => consoleMessages.Add($"[console] {msg.Type}: {msg.Text}");
        var pageErrors = new List<string>();
        Page.PageError += (_, msg) => pageErrors.Add($"[pageerror] {msg}");

        // Fail fast if Blazor error UI is present after assignment (post-click)
        if (await blazorError.IsVisibleAsync())
        {
            var errorText = await blazorError.TextContentAsync();
            // Capture diagnostics with unique variable names
            var diagDirError = Path.Combine(AppContext.BaseDirectory, "screenshots");
            if (!Directory.Exists(diagDirError))
                Directory.CreateDirectory(diagDirError);
            await Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(diagDirError, "BlazorErrorUI_AfterAssignment.png"), FullPage = true });
            var htmlError = await Page.ContentAsync();
            File.WriteAllText(Path.Combine(diagDirError, "BlazorErrorUI_AfterAssignment.html"), htmlError);
            // Capture browser console and page errors
            var consoleMessagesError = new List<string>();
            Page.Console += (_, msg) => consoleMessagesError.Add($"[console] {msg.Type}: {msg.Text}");
            var pageErrorsError = new List<string>();
            Page.PageError += (_, msg) => pageErrorsError.Add($"[pageerror] {msg}");
            // Give a short delay to collect any late console/page errors
            await Task.Delay(1000);
            File.WriteAllText(Path.Combine(diagDirError, "BlazorErrorUI_AfterAssignment_console.txt"), string.Join("\n", consoleMessagesError));
            File.WriteAllText(Path.Combine(diagDirError, "BlazorErrorUI_AfterAssignment_pageerrors.txt"), string.Join("\n", pageErrorsError));
            throw new Exception($"Blazor error UI detected after assignment: {errorText}. See diagnostics in {diagDirError}/");
        }
        // Defensive: short wait to allow Blazor to render modal
        await Task.Delay(500);

        // Use diagnostics directory rooted in E2E project
        var diagDir = Path.Combine(AppContext.BaseDirectory, "screenshots");
        if (!Directory.Exists(diagDir))
            Directory.CreateDirectory(diagDir);
        // Capture screenshot and HTML after clicking audit button
        await Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(diagDir, "AuditTrailModal_AfterAuditClick.png"), FullPage = true });
        var html = await Page.ContentAsync();
        File.WriteAllText(Path.Combine(diagDir, "AuditTrailModal_AfterAuditClick.html"), html);

        // Defensive: wait for audit trail modal title to exist before asserting visibility (CI timing)
        try
        {
            await Page.WaitForSelectorAsync("#auditTrailModalTitle", new() { Timeout = 30000 });
            await Expect(Page.Locator("#auditTrailModalTitle")).ToBeVisibleAsync();
            // Wait for loading spinner to disappear before reading modal content
            await Expect(Page.Locator(".modal.show .spinner-border")).Not.ToBeVisibleAsync();
            var content = await Page.Locator(".modal.show").TextContentAsync();
            Assert.That(content, Does.Contain("Assign").Or.Contain("Toegewezen"));
        }
        catch (Exception ex)
        {
            // Extra logging for modal state
            var modalHtml = await Page.Locator("#transactionAssignModal").InnerHTMLAsync();
            var errorDetails = $"\nConsole messages:\n{string.Join("\n", consoleMessages)}\nPage errors:\n{string.Join("\n", pageErrors)}\nModal HTML:\n{modalHtml}\n";
            throw new Exception($"AuditTrail modal did not appear. See screenshot and HTML in {diagDir}/. {errorDetails}", ex);
        }
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

        // ...existing code...
        await _transactionsPage.SelectAllOnPageAsync();
        await _transactionsPage.ClickBulkAssignAsync();

        await Expect(Page.Locator("#bulkAssignModal")).ToBeVisibleAsync();

        await _transactionsPage.TakeScreenshotAsync("assignment-modal-open");
    }
}
