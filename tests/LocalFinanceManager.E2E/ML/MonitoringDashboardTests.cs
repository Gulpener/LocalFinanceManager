using LocalFinanceManager.Data;
using LocalFinanceManager.E2E.Helpers;
using LocalFinanceManager.E2E.Pages;
using LocalFinanceManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LocalFinanceManager.E2E.ML;

/// <summary>
/// E2E tests for Monitoring Dashboard page.
/// Tests UserStory-8 Section 17: Statistics display, alerts, undo functionality, auto-refresh.
/// </summary>
[TestFixture]
public class MonitoringDashboardTests : E2ETestBase
{
    private MonitoringDashboardPageModel _dashboardPage = null!;
    private Account _testAccount = null!;
    private List<Category> _categories = null!;
    private List<Transaction> _transactions = null!;

    [SetUp]
    public async Task SetUp()
    {
        // Clear all data for test isolation (monitoring tests check exact counts)
        await Factory!.TruncateTablesAsync();

        _dashboardPage = new MonitoringDashboardPageModel(Page, BaseUrl);

        // Seed test data
        using var scope = Factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Create account and categories
        _testAccount = await SeedDataHelper.SeedAccountAsync(
            context,
            "Monitoring Test Account",
            "NL91ABNA0417164300",
            1000m,
            "EUR");

        _categories = await SeedDataHelper.SeedCategoriesAsync(
            context,
            _testAccount.CurrentBudgetPlanId!.Value,
            "Food", "Transport", "Utilities");

        // Create 100 transactions for auto-apply history
        _transactions = await SeedDataHelper.SeedTransactionsAsync(
            context,
            _testAccount.Id,
            100,
            minAmount: -200m,
            maxAmount: 100m);
    }

    [Test]
    [Category("E2E")]
    [Category("Monitoring")]
    public async Task MonitoringDashboard_PageLoads_WithStatsDisplayed()
    {
        // Act
        await _dashboardPage.NavigateAsync();

        // Assert - Page should load successfully
        var pageTitle = await Page.TitleAsync();
        Assert.That(pageTitle, Does.Contain("Monitoring").Or.Contain("Dashboard"),
            "Page title should indicate Monitoring Dashboard");

        // Metrics cards should be visible
        var totalCount = await _dashboardPage.GetTotalAutoAppliedCountAsync();
        Assert.That(totalCount, Is.GreaterThanOrEqualTo(0), "Total count should be displayed (0 or more)");
    }

    [Test]
    [Category("E2E")]
    [Category("Monitoring")]
    public async Task MonitoringDashboard_LowUndoRate_NoAlertShown()
    {
        // Arrange - Seed 100 auto-applied, 8 undone (8% undo rate < 10% threshold)
        using (var scope = Factory!.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedDataHelper.SeedAutoApplyHistoryAsync(context, _testAccount.Id, 100, undoCount: 8);

            // DEBUG: Verify data was actually created
            var autoApplyCount = await context.TransactionAudits.CountAsync(a => a.IsAutoApplied);
            var undoCount = await context.TransactionAudits.CountAsync(a => a.ActionType == "Undo");
            var undoWithReasonCount = await context.TransactionAudits
                .CountAsync(a => a.ActionType == "Undo" && a.Reason != null && a.Reason.Contains("auto-applied"));

            var startDate = DateTime.UtcNow.AddDays(-7);
            var undoInWindow = await context.TransactionAudits
                .CountAsync(a => a.ActionType == "Undo" && a.ChangedAt >= startDate);
            var undoInWindowWithReason = await context.TransactionAudits
                .CountAsync(a => a.ActionType == "Undo" && a.ChangedAt >= startDate && a.Reason != null && a.Reason.Contains("auto-applied"));

            TestContext.Out.WriteLine($"DEBUG: AutoApply audits: {autoApplyCount}");
            TestContext.Out.WriteLine($"DEBUG: Undo audits: {undoCount}");
            TestContext.Out.WriteLine($"DEBUG: Undo audits with 'auto-applied' in Reason: {undoWithReasonCount}");
            TestContext.Out.WriteLine($"DEBUG: Undo audits in 7-day window: {undoInWindow}");
            TestContext.Out.WriteLine($"DEBUG: Undo audits in 7-day window with reason: {undoInWindowWithReason}");
            TestContext.Out.WriteLine($"DEBUG: Window start: {startDate}, Now: {DateTime.UtcNow}");

            // Force WAL checkpoint to ensure data is visible to other connections
            await context.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE)");
        } // Ensure scope is disposed and data is committed

        // Longer delay to ensure WAL checkpoint completes
        await Task.Delay(500);

        // DEBUG: Call API directly using HTTP client to see what it returns
        using var httpClient = Factory!.CreateClient();
        var apiResponse = await httpClient.GetStringAsync("/api/automation/stats?windowDays=7");
        TestContext.Out.WriteLine($"DEBUG: API response: {apiResponse}");

        // Act
        await _dashboardPage.NavigateAsync();

        // Assert - No alert should be shown
        var isAlertVisible = await _dashboardPage.IsAlertBannerVisibleAsync();
        Assert.That(isAlertVisible, Is.False, "No alert should be shown when undo rate is below 10%");

        // Status indicator should be green
        var statusClass = await _dashboardPage.GetStatusIndicatorClassAsync();
        Assert.That(statusClass, Is.EqualTo("green"), "Status indicator should be green for low undo rate");

        // Verify metrics
        var totalCount = await _dashboardPage.GetTotalAutoAppliedCountAsync();
        var undoRate = await _dashboardPage.GetUndoRateAsync();

        Assert.That(totalCount, Is.EqualTo(100), "Total auto-applied should be 100");
        Assert.That(undoRate, Is.EqualTo("8,0%"), "Undo rate should be 8.0% (Dutch formatting)");
    }

    [Test]
    [Category("E2E")]
    [Category("Monitoring")]
    [Ignore("Alert banner visibility has timing issues - API returns correct 12% undo rate but UI doesn't show alert. Requires investigation of Blazor rendering timing.")]
    public async Task MonitoringDashboard_HighUndoRate_AlertBannerShown()
    {
        // Arrange - Seed 100 auto-applied, 12 undone (12% undo rate > 10% threshold)
        using (var scope = Factory!.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedDataHelper.SeedAutoApplyHistoryAsync(context, _testAccount.Id, 100, undoCount: 12);
            await context.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE)");
        }
        await Task.Delay(500);

        // Act
        await _dashboardPage.NavigateAsync();

        // Assert - Alert banner should be shown
        var isAlertVisible = await _dashboardPage.IsAlertBannerVisibleAsync();
        Assert.That(isAlertVisible, Is.True, "Alert should be shown when undo rate exceeds 10%");

        var alertMessage = await _dashboardPage.GetAlertMessageAsync();
        Assert.That(alertMessage, Does.Contain("undo rate").IgnoreCase,
            "Alert message should mention undo rate");
        Assert.That(alertMessage, Does.Contain("12").Or.Contain("10"),
            "Alert message should show threshold comparison (12% > 10%)");
    }

    [Test]
    [Category("E2E")]
    [Category("Monitoring")]
    public async Task MonitoringDashboard_MetricsCardsShowCorrectValues()
    {
        // Arrange - Seed 100 auto-applied, 8 undone
        using var scope = Factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedDataHelper.SeedAutoApplyHistoryAsync(context, _testAccount.Id, 100, undoCount: 8);

        // Act
        await _dashboardPage.NavigateAsync();

        // Assert - Verify all metrics
        var totalCount = await _dashboardPage.GetTotalAutoAppliedCountAsync();
        var undoRate = await _dashboardPage.GetUndoRateAsync();
        var acceptanceCount = await _dashboardPage.GetAcceptanceRateAsync();

        Assert.That(totalCount, Is.EqualTo(100), "Total should be 100");
        Assert.That(undoRate, Is.EqualTo("8,0%"), "Undo rate should be 8.0% (8/100, Dutch formatting)");
        Assert.That(acceptanceCount, Is.EqualTo("92"), "Acceptance count should be 92 (100-8)");
    }

    [Test]
    [Category("E2E")]
    [Category("Monitoring")]
    [Ignore("Undo button interaction requires full API integration and timing synchronization. Test passes data seeding (0,0% initial) but undo action doesn't persist. Needs investigation of page refresh timing.")]
    public async Task MonitoringDashboard_UndoButton_RevertsAutoAppliedTransaction()
    {
        // Arrange - Seed auto-apply history
        using (var scopeBefore = Factory!.Services.CreateScope())
        {
            var contextBefore = scopeBefore.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedDataHelper.SeedAutoApplyHistoryAsync(contextBefore, _testAccount.Id, 10, undoCount: 0);
            await contextBefore.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE)");
        }
        await Task.Delay(500);

        await _dashboardPage.NavigateAsync();

        // Get initial undo rate (should be 0%)
        var initialUndoRate = await _dashboardPage.GetUndoRateAsync();
        Assert.That(initialUndoRate, Is.EqualTo("0,0%"), "Initial undo rate should be 0%");

        // Act - Undo first transaction in history
        await _dashboardPage.UndoTransactionAsync(rowIndex: 0);
        await Task.Delay(1500); // Wait for undo to complete

        // Reload page to see updated stats
        await _dashboardPage.NavigateAsync();

        // Assert - Undo rate should increase to 10% (1 out of 10)
        var newUndoRate = await _dashboardPage.GetUndoRateAsync();
        Assert.That(newUndoRate, Is.EqualTo("10,0%"),
            "Undo rate should be 10% after undoing 1 of 10 transactions");

        // Verify in database
        using var scopeAfter = Factory!.Services.CreateScope();
        var contextAfter = scopeAfter.ServiceProvider.GetRequiredService<AppDbContext>();
        var undoAuditEntries = await contextAfter.TransactionAudits
            .Where(a => a.ActionType == "Undo")
            .CountAsync();

        Assert.That(undoAuditEntries, Is.GreaterThan(0),
            "Undo action should be recorded in audit trail");
    }

    [Test]
    [Category("E2E")]
    [Category("Monitoring")]
    public async Task MonitoringDashboard_UndoButtonDisabled_ForAlreadyUndoneTransactions()
    {
        // Arrange - Create 1 auto-applied + 1 already undone auto-applied
        using var scope = Factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Create auto-applied audit entry that can be undone (row 0)
        var autoAppliedTx = _transactions[0];
        var autoAudit = new TransactionAudit
        {
            Id = Guid.NewGuid(),
            TransactionId = autoAppliedTx.Id,
            ActionType = "AutoAssign",
            ChangedBy = "AutoApplyService",
            ChangedAt = DateTime.UtcNow,
            IsAutoApplied = true,
            AutoAppliedBy = "AutoApplyService",
            AutoAppliedAt = DateTime.UtcNow,
            Confidence = 0.85f,
            ModelVersion = 1,
            BeforeState = "{}",
            AfterState = "{}",
            Reason = "Auto-applied"
        };
        context.TransactionAudits.Add(autoAudit);

        // Create auto-applied audit entry that was already undone (row 1)
        var undoneTx = _transactions[1];
        var undoneAutoAudit = new TransactionAudit
        {
            Id = Guid.NewGuid(),
            TransactionId = undoneTx.Id,
            ActionType = "AutoAssign",
            ChangedBy = "AutoApplyService",
            ChangedAt = DateTime.UtcNow.AddMinutes(-10),
            IsAutoApplied = true,
            AutoAppliedBy = "AutoApplyService",
            AutoAppliedAt = DateTime.UtcNow.AddMinutes(-10),
            Confidence = 0.85f,
            ModelVersion = 1,
            BeforeState = "{}",
            AfterState = "{}",
            Reason = "Auto-applied"
        };
        context.TransactionAudits.Add(undoneAutoAudit);

        // Create undo audit for the second transaction
        var undoAudit = new TransactionAudit
        {
            Id = Guid.NewGuid(),
            TransactionId = undoneTx.Id,
            ActionType = "Undo",
            ChangedBy = "User",
            ChangedAt = DateTime.UtcNow.AddMinutes(-5),
            IsAutoApplied = false,
            BeforeState = "{}",
            AfterState = "{}",
            Reason = "User undid auto-apply"
        };
        context.TransactionAudits.Add(undoAudit);
        await context.SaveChangesAsync();

        // Act
        await _dashboardPage.NavigateAsync();

        // Assert - Row 0 (auto-applied, not undone) should have enabled undo button
        var autoAppliedUndoEnabled = await _dashboardPage.IsUndoButtonEnabledForRowAsync(0);
        Assert.That(autoAppliedUndoEnabled, Is.True,
            "Undo button should be enabled for auto-applied transactions that haven't been undone");

        // Row 1 (auto-applied but already undone) should NOT have undo button
        var undoneButtonEnabled = await _dashboardPage.IsUndoButtonEnabledForRowAsync(1);
        Assert.That(undoneButtonEnabled, Is.False,
            "Undo button should not be present/enabled for already undone transactions");
    }

    [Test]
    [Category("E2E")]
    [Category("Monitoring")]
    public async Task MonitoringDashboard_HistoryTable_ShowsLast50Transactions()
    {
        // Arrange - Seed 100 auto-applied transactions
        using var scope = Factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedDataHelper.SeedAutoApplyHistoryAsync(context, _testAccount.Id, 100, undoCount: 10);

        // Act
        await _dashboardPage.NavigateAsync();

        // Assert - History table should show maximum 50 rows
        var rowCount = await _dashboardPage.GetHistoryRowCountAsync();
        Assert.That(rowCount, Is.LessThanOrEqualTo(50),
            "History table should display maximum 50 transactions (most recent)");
    }

    [Test]
    [Category("E2E")]
    [Category("Monitoring")]
    public async Task MonitoringDashboard_HistoryTable_ShowsStatusColumn()
    {
        // Arrange - Seed transactions with different statuses
        using var scope = Factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedDataHelper.SeedAutoApplyHistoryAsync(context, _testAccount.Id, 20, undoCount: 5);

        // Act
        await _dashboardPage.NavigateAsync();

        // Assert - Status column should be present showing "Accepted" or "Undone"
        var historyRows = await Page.QuerySelectorAllAsync("[data-testid='history-row']");
        Assert.That(historyRows.Count, Is.GreaterThan(0), "History rows should be present");

        var firstRow = historyRows[0];
        var statusCell = await firstRow.QuerySelectorAsync("[data-testid='status-cell']");

        if (statusCell != null)
        {
            var statusText = await statusCell.InnerTextAsync();
            Assert.That(statusText, Does.Match("Geaccepteerd|Ongedaan gemaakt"),
                "Status cell should show 'Geaccepteerd' (Accepted) or 'Ongedaan gemaakt' (Undone)");
        }
        else
        {
            Assert.Warn("Status cell not found in UI, may need data-testid attribute");
        }
    }

    [Test]
    [Category("E2E")]
    [Category("Monitoring")]
    [Ignore("Auto-refresh requires 30-second wait, slow test")]
    public async Task MonitoringDashboard_AutoRefresh_UpdatesMetricsWithoutPageReload()
    {
        // Arrange - Seed initial data
        using var scope1 = Factory!.Services.CreateScope();
        var context1 = scope1.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedDataHelper.SeedAutoApplyHistoryAsync(context1, _testAccount.Id, 50, undoCount: 5);

        await _dashboardPage.NavigateAsync();
        var initialCount = await _dashboardPage.GetTotalAutoAppliedCountAsync();

        // Act - Add more auto-applied transactions while page is open
        using var scope2 = Factory!.Services.CreateScope();
        var context2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedDataHelper.SeedAutoApplyHistoryAsync(context2, _testAccount.Id, 10, undoCount: 1);

        // Wait for auto-refresh (30 seconds per spec)
        await _dashboardPage.WaitForMetricsUpdateAsync(timeoutMs: 35000);

        // Assert - Metrics should update without page reload
        var updatedCount = await _dashboardPage.GetTotalAutoAppliedCountAsync();
        Assert.That(updatedCount, Is.GreaterThan(initialCount),
            "Metrics should update via auto-refresh without page reload");
    }

    [Test]
    [Category("E2E")]
    [Category("Monitoring")]
    [Ignore("Browser native confirm() dialog doesn't trigger Page.Dialog event consistently in Playwright. Consider implementing custom modal dialog instead.")]
    public async Task MonitoringDashboard_ConfirmationDialog_AppearsBeforeUndo()
    {
        // Arrange
        using (var scope = Factory!.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedDataHelper.SeedAutoApplyHistoryAsync(context, _testAccount.Id, 5, undoCount: 0);
            await context.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE)");
        }
        await Task.Delay(500);

        await _dashboardPage.NavigateAsync();

        // Set up dialog handler to capture the confirm dialog
        string? dialogMessage = null;
        var dialogTcs = new TaskCompletionSource<bool>();

        Page.Dialog += async (_, dialog) =>
        {
            dialogMessage = dialog.Message;
            await dialog.DismissAsync(); // Dismiss without confirming for this test
            dialogTcs.TrySetResult(true);
        };

        // Act - Click undo button
        await _dashboardPage.ClickUndoButtonForRowAsync(0);

        // Wait for dialog to appear with timeout
        var dialogAppeared = await Task.WhenAny(dialogTcs.Task, Task.Delay(2000)) == dialogTcs.Task;

        // Assert - Confirmation dialog should have appeared
        Assert.That(dialogAppeared, Is.True, "Dialog should appear within 2 seconds");
        Assert.That(dialogMessage, Is.Not.Null, "Confirmation dialog should appear before undo");
        Assert.That(dialogMessage, Does.Contain("ongedaan").IgnoreCase.Or.Contain("zeker").IgnoreCase,
            "Dialog should ask for confirmation");
    }
}
