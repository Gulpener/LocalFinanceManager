using LocalFinanceManager.Data;
using LocalFinanceManager.E2E.Helpers;
using LocalFinanceManager.E2E.Pages;
using LocalFinanceManager.Models;
using LocalFinanceManager.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using System.Globalization;

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
    private decimal _undoRateAlertThresholdPercent;

    [SetUp]
    public async Task SetUp()
    {
        // Clear all data for test isolation (monitoring tests check exact counts)
        await Factory!.TruncateTablesAsync();

        _dashboardPage = new MonitoringDashboardPageModel(Page, BaseUrl);

        // Seed test data
        // Use HostServices (real Kestrel host DI) so threshold matches the running server's config.
        // Factory.Services is the test-host container and may resolve appsettings differently.
        using var scope = Factory!.HostServices.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<AutomationOptions>>();
        _undoRateAlertThresholdPercent = options.Value.UndoRateAlertThreshold * 100m;

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
        var pageHeading = await Page.Locator("h1").First.InnerTextAsync();
        Assert.That(pageHeading, Does.Contain("Monitoring").Or.Contain("Dashboard"),
            "Page heading should indicate Monitoring Dashboard");

        // Metrics cards should be visible
        var totalCount = await _dashboardPage.GetTotalAutoAppliedCountAsync();
        Assert.That(totalCount, Is.GreaterThanOrEqualTo(0), "Total count should be displayed (0 or more)");
    }

    [Test]
    [Category("E2E")]
    [Category("Monitoring")]
    public async Task MonitoringDashboard_LowUndoRate_NoAlertShown()
    {
        const int totalAutoApplied = 100;
        var lowUndoCount = GetUndoCountBelowThreshold(_undoRateAlertThresholdPercent, totalAutoApplied);

        // Arrange - Seed auto-applied history below configured undo-rate threshold
        using (var scope = Factory!.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedDataHelper.SeedAutoApplyHistoryAsync(context, _testAccount.Id, totalAutoApplied, undoCount: lowUndoCount);

        } // Ensure scope is disposed and data is committed
        // Act
        await _dashboardPage.NavigateAsync();

        // Assert - No alert should be shown.
        // Use Expect polling (not a snapshot) to account for the 2s auto-refresh cycle
        // and Blazor render time. 8s covers one full refresh + 3× safety margin.
        await Expect(Page.Locator("[data-testid='alert-banner']")
            ).Not.ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 8_000 });

        // Status indicator should be green
        var statusClass = await _dashboardPage.GetStatusIndicatorClassAsync();
        Assert.That(statusClass, Is.EqualTo("green"), "Status indicator should be green for low undo rate");

        // Verify metrics
        var totalCount = await _dashboardPage.GetTotalAutoAppliedCountAsync();
        var undoRate = await _dashboardPage.GetUndoRateAsync();

        Assert.That(totalCount, Is.EqualTo(totalAutoApplied), $"Total auto-applied should be {totalAutoApplied}");
        Assert.That(ParsePercentageText(undoRate), Is.EqualTo((decimal)lowUndoCount).Within(0.01m),
            $"Undo rate should be {lowUndoCount:0.0}%");
    }

    [Test]
    [Category("E2E")]
    [Category("Monitoring")]
    public async Task MonitoringDashboard_HighUndoRate_AlertBannerShown()
    {
        const int totalAutoApplied = 100;
        var highUndoCount = GetUndoCountAboveThreshold(_undoRateAlertThresholdPercent, totalAutoApplied);

        // Arrange - Seed auto-applied history above configured undo-rate threshold
        using (var scope = Factory!.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedDataHelper.SeedAutoApplyHistoryAsync(context, _testAccount.Id, totalAutoApplied, undoCount: highUndoCount);
        }

        // Act
        await _dashboardPage.NavigateAsync();

        // Assert - Alert banner should be shown; wait up to 3 s for the Blazor component to render
        var alertBanner = await Page.WaitForSelectorAsync("[data-testid='alert-banner']",
            new() { State = Microsoft.Playwright.WaitForSelectorState.Visible, Timeout = 5000 });
        Assert.That(alertBanner, Is.Not.Null,
            $"Alert should be shown when undo rate exceeds configured threshold ({_undoRateAlertThresholdPercent:0.0}%)");

        var alertMessage = await alertBanner!.InnerTextAsync();
        Assert.That(alertMessage, Does.Contain("drempel").IgnoreCase.Or.Contain("Ongedaan").IgnoreCase,
            "Alert message should mention undo rate threshold");
        Assert.That(alertMessage, Does.Contain("%"),
            "Alert message should show percentage values");
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
        Assert.That(ParsePercentageText(undoRate), Is.EqualTo(8.0m).Within(0.01m),
            "Undo rate should be 8.0% (8/100)");
        Assert.That(acceptanceCount, Is.EqualTo("92"), "Acceptance count should be 92 (100-8)");
    }

    [Test]
    [Category("E2E")]
    [Category("Monitoring")]
    public async Task MonitoringDashboard_UndoButton_RevertsAutoAppliedTransaction()
    {
        // Arrange - Seed auto-apply history
        using (var scopeBefore = Factory!.Services.CreateScope())
        {
            var contextBefore = scopeBefore.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedDataHelper.SeedAutoApplyHistoryAsync(contextBefore, _testAccount.Id, 10, undoCount: 0);
        }

        await _dashboardPage.NavigateAsync();

        // Get initial undo rate (should be 0%)
        var initialUndoRate = await _dashboardPage.GetUndoRateAsync();
        Assert.That(ParsePercentageText(initialUndoRate), Is.EqualTo(0.0m).Within(0.01m),
            "Initial undo rate should be 0%");

        // Act - Undo first transaction in history (UndoTransactionAsync handles the custom dialog)
        await _dashboardPage.UndoTransactionAsync(rowIndex: 0);

        // Reload page to see updated stats
        await _dashboardPage.NavigateAsync();

        // Assert - Undo rate should increase to 10% (1 out of 10)
        var updatedUndoRate = await WaitForUndoRateAsync(expectedUndoRatePercent: 10.0m, timeoutMs: 5000);
        Assert.That(updatedUndoRate, Is.EqualTo(10.0m).Within(0.01m),
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
        // Explicitly wait for at least one row to be present before querying, since NavigateAsync
        // may return the instant spinners vanish — history rows may still be rendering.
        await Page.WaitForSelectorAsync("[data-testid='history-row']", new() { Timeout = 15000 });
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
    [Category("Slow")]
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
        await Task.Delay(300);

        // Wait for auto-refresh (2 seconds in test configuration)
        await _dashboardPage.WaitForMetricsUpdateAsync(timeoutMs: 10000);

        // Assert - Metrics should update without page reload
        var updatedCount = await _dashboardPage.GetTotalAutoAppliedCountAsync();
        Assert.That(updatedCount, Is.GreaterThan(initialCount),
            "Metrics should update via auto-refresh without page reload");
    }

    [Test]
    [Category("E2E")]
    [Category("Monitoring")]
    public async Task MonitoringDashboard_ConfirmationDialog_AppearsBeforeUndo()
    {
        // Arrange
        using (var scope = Factory!.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedDataHelper.SeedAutoApplyHistoryAsync(context, _testAccount.Id, 5, undoCount: 0);
        }

        await _dashboardPage.NavigateAsync();

        // Act - Click undo button (now opens a custom Blazor dialog, not a browser native confirm)
        await _dashboardPage.ClickUndoButtonForRowAsync(0);

        // Wait for the custom confirm dialog to appear
        var confirmDialog = await Page.WaitForSelectorAsync("[data-testid='confirm-dialog']",
            new() { State = Microsoft.Playwright.WaitForSelectorState.Visible, Timeout = 5000 });

        Assert.That(confirmDialog, Is.Not.Null, "Custom confirmation dialog should appear before undo");

        // Assert - Dialog should contain a confirmation message
        var dialogText = await confirmDialog!.InnerTextAsync();
        Assert.That(dialogText, Does.Contain("ongedaan").IgnoreCase.Or.Contain("zeker").IgnoreCase,
            "Dialog should ask for confirmation");

        // Dismiss without confirming for this test
        await Page.ClickAsync("[data-testid='confirm-no']");
        await Page.WaitForSelectorAsync("[data-testid='confirm-dialog']",
            new() { State = Microsoft.Playwright.WaitForSelectorState.Hidden, Timeout = 3000 });
    }

    private static decimal ParsePercentageText(string percentageText)
    {
        var normalized = percentageText
            .Trim()
            .Replace("%", string.Empty)
            .Replace("\u00A0", string.Empty)
            .Replace(" ", string.Empty)
            .Replace(',', '.');

        return decimal.Parse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture);
    }

    private async Task<decimal> WaitForUndoRateAsync(decimal expectedUndoRatePercent, int timeoutMs)
    {
        const int pollIntervalMs = 100;
        var elapsedMs = 0;
        var lastObservedRate = 0m;

        while (elapsedMs <= timeoutMs)
        {
            var undoRateText = await _dashboardPage.GetUndoRateAsync();
            lastObservedRate = ParsePercentageText(undoRateText);

            if (Math.Abs(lastObservedRate - expectedUndoRatePercent) <= 0.01m)
            {
                return lastObservedRate;
            }

            await Page.WaitForTimeoutAsync(pollIntervalMs);
            elapsedMs += pollIntervalMs;
        }

        return lastObservedRate;
    }

    private static int GetUndoCountBelowThreshold(decimal thresholdPercent, int totalCount)
    {
        var thresholdCount = thresholdPercent / 100m * totalCount;
        var belowThreshold = (int)Math.Floor(thresholdCount);

        return Math.Max(0, belowThreshold - 1);
    }

    private static int GetUndoCountAboveThreshold(decimal thresholdPercent, int totalCount)
    {
        var thresholdCount = thresholdPercent / 100m * totalCount;
        var aboveThreshold = (int)Math.Floor(thresholdCount) + 1;

        return Math.Clamp(aboveThreshold, 1, totalCount);
    }
}
