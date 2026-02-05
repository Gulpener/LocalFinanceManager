using Microsoft.Playwright;

namespace LocalFinanceManager.E2E.Pages;

/// <summary>
/// Page Object Model for the Monitoring Dashboard page.
/// Provides methods to interact with auto-apply statistics, alerts, and history.
/// </summary>
public class MonitoringDashboardPageModel : PageObjectBase
{
    // Selectors
    private const string TotalCountCardSelector = "[data-testid='total-auto-applied']";
    private const string UndoRateCardSelector = "[data-testid='undo-rate']";
    private const string AcceptanceRateCardSelector = "[data-testid='acceptance-rate']";
    private const string LastRunCardSelector = "[data-testid='last-run']";
    private const string AlertBannerSelector = "[data-testid='alert-banner']";
    private const string StatusIndicatorSelector = "[data-testid='status-indicator']";
    private const string HistoryTableSelector = "table[data-testid='auto-apply-history']";
    private const string HistoryRowSelector = "tbody tr[data-testid='history-row']";
    private const string UndoButtonSelector = "button[data-action='undo']";
    private const string ConfirmDialogSelector = "[data-testid='confirm-dialog']";
    private const string ConfirmYesButtonSelector = "[data-testid='confirm-yes']";

    /// <summary>
    /// Initializes a new instance of the MonitoringDashboardPageModel class.
    /// </summary>
    /// <param name="page">Playwright page instance.</param>
    /// <param name="baseUrl">Base URL for the application.</param>
    public MonitoringDashboardPageModel(IPage page, string baseUrl) : base(page, baseUrl)
    {
    }

    /// <summary>
    /// Navigates to the Monitoring Dashboard page.
    /// </summary>
    public async Task NavigateAsync()
    {
        await NavigateToAsync("/admin/monitoring");

        // Wait for stats to load (spinner to disappear)
        await Page.WaitForSelectorAsync(".spinner-border", new() { State = WaitForSelectorState.Hidden, Timeout = 5000 });
    }

    /// <summary>
    /// Gets the total count of auto-applied transactions displayed in the metrics card.
    /// </summary>
    /// <returns>Total count as integer.</returns>
    public async Task<int> GetTotalAutoAppliedCountAsync()
    {
        var text = await Page.InnerTextAsync(TotalCountCardSelector);
        // Extract number from text like "100 transactions auto-applied"
        var match = System.Text.RegularExpressions.Regex.Match(text, @"\d+");
        return match.Success ? int.Parse(match.Value) : 0;
    }

    /// <summary>
    /// Gets the undo rate percentage displayed in the metrics card.
    /// </summary>
    /// <returns>Undo rate as string (e.g., "8%").</returns>
    public async Task<string> GetUndoRateAsync()
    {
        var text = await Page.InnerTextAsync(UndoRateCardSelector);
        // Extract percentage from text like "Undo Rate: 8,0%" (Dutch formatting with comma)
        var match = System.Text.RegularExpressions.Regex.Match(text, @"\d+[,.]?\d*\s?%");
        return match.Success ? match.Value.Trim() : "0%";
    }

    /// <summary>
    /// Gets the acceptance count (not rate) displayed in the metrics card.
    /// </summary>
    /// <returns>Acceptance count as string (e.g., "92").</returns>
    public async Task<string> GetAcceptanceRateAsync()
    {
        var text = await Page.InnerTextAsync(AcceptanceRateCardSelector);
        // Extract number from text like "Geaccepteerd: 92"
        var match = System.Text.RegularExpressions.Regex.Match(text, @"\d+");
        return match.Success ? match.Value : "0";
    }

    /// <summary>
    /// Checks if the alert banner is visible on the page.
    /// </summary>
    /// <returns>True if alert banner is visible, false otherwise.</returns>
    public async Task<bool> IsAlertBannerVisibleAsync()
    {
        return await Page.IsVisibleAsync(AlertBannerSelector);
    }

    /// <summary>
    /// Gets the alert banner message text.
    /// </summary>
    /// <returns>Alert message text or null if not visible.</returns>
    public async Task<string?> GetAlertMessageAsync()
    {
        var isVisible = await IsAlertBannerVisibleAsync();
        return isVisible ? await Page.InnerTextAsync(AlertBannerSelector) : null;
    }

    /// <summary>
    /// Gets the status indicator color class (green, yellow, red).
    /// </summary>
    /// <returns>Status indicator class name.</returns>
    public async Task<string> GetStatusIndicatorClassAsync()
    {
        var element = await Page.QuerySelectorAsync(StatusIndicatorSelector);
        if (element == null) return "unknown";

        var className = await element.GetAttributeAsync("class") ?? "";
        if (className.Contains("bg-success") || className.Contains("green")) return "green";
        if (className.Contains("bg-warning") || className.Contains("yellow")) return "yellow";
        if (className.Contains("bg-danger") || className.Contains("red")) return "red";
        return "unknown";
    }

    /// <summary>
    /// Gets the count of rows in the auto-apply history table.
    /// </summary>
    /// <returns>Number of history rows.</returns>
    public async Task<int> GetHistoryRowCountAsync()
    {
        var rows = await Page.QuerySelectorAllAsync(HistoryRowSelector);
        return rows.Count;
    }

    /// <summary>
    /// Clicks the "Undo" button for a specific history row.
    /// </summary>
    /// <param name="rowIndex">Zero-based index of the history row.</param>
    public async Task ClickUndoButtonForRowAsync(int rowIndex)
    {
        var rows = await Page.QuerySelectorAllAsync(HistoryRowSelector);
        if (rowIndex >= rows.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(rowIndex),
                $"Row index {rowIndex} is out of range. Only {rows.Count} rows found.");
        }

        var undoButton = await rows[rowIndex].QuerySelectorAsync(UndoButtonSelector);
        if (undoButton == null)
        {
            throw new InvalidOperationException($"Undo button not found for row {rowIndex}.");
        }

        await undoButton.ClickAsync();
    }

    /// <summary>
    /// Confirms the undo action in the browser's confirmation dialog.
    /// </summary>
    public async Task ConfirmUndoAsync()
    {
        // Handle browser's native confirm dialog
        Page.Dialog += async (_, dialog) =>
        {
            await dialog.AcceptAsync();
        };
        // Dialog will be handled automatically when it appears
        await Task.Delay(100); // Small delay to ensure handler is set
    }

    /// <summary>
    /// Undoes an auto-applied transaction by clicking undo and confirming.
    /// </summary>
    /// <param name="rowIndex">Zero-based index of the history row.</param>
    public async Task UndoTransactionAsync(int rowIndex)
    {
        // Set up dialog handler before clicking
        Page.Dialog += async (_, dialog) =>
        {
            await dialog.AcceptAsync();
        };

        await ClickUndoButtonForRowAsync(rowIndex);
        await Task.Delay(500); // Wait for action to complete
    }

    /// <summary>
    /// Checks if the undo button is enabled for a specific history row.
    /// </summary>
    /// <param name="rowIndex">Zero-based index of the history row.</param>
    /// <returns>True if button is enabled, false if disabled.</returns>
    public async Task<bool> IsUndoButtonEnabledForRowAsync(int rowIndex)
    {
        var rows = await Page.QuerySelectorAllAsync(HistoryRowSelector);
        if (rowIndex >= rows.Count) return false;

        var undoButton = await rows[rowIndex].QuerySelectorAsync(UndoButtonSelector);
        if (undoButton == null) return false;

        var isDisabled = await undoButton.IsDisabledAsync();
        return !isDisabled;
    }

    /// <summary>
    /// Waits for metrics to update (auto-refresh).
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    public async Task WaitForMetricsUpdateAsync(int timeoutMs = 30000)
    {
        // Capture initial metric values
        var totalCountInitial = await Page.InnerTextAsync(TotalCountCardSelector);
        var undoRateInitial = await Page.InnerTextAsync(UndoRateCardSelector);
        var acceptanceRateInitial = await Page.InnerTextAsync(AcceptanceRateCardSelector);
        var lastRunInitial = await Page.InnerTextAsync(LastRunCardSelector);

        const int pollIntervalMs = 500;
        var elapsed = 0;

        while (elapsed < timeoutMs)
        {
            var totalCountCurrent = await Page.InnerTextAsync(TotalCountCardSelector);
            var undoRateCurrent = await Page.InnerTextAsync(UndoRateCardSelector);
            var acceptanceRateCurrent = await Page.InnerTextAsync(AcceptanceRateCardSelector);
            var lastRunCurrent = await Page.InnerTextAsync(LastRunCardSelector);

            var hasChanged =
                !string.Equals(totalCountInitial, totalCountCurrent, StringComparison.Ordinal) ||
                !string.Equals(undoRateInitial, undoRateCurrent, StringComparison.Ordinal) ||
                !string.Equals(acceptanceRateInitial, acceptanceRateCurrent, StringComparison.Ordinal) ||
                !string.Equals(lastRunInitial, lastRunCurrent, StringComparison.Ordinal);

            if (hasChanged)
            {
                return;
            }

            await Page.WaitForTimeoutAsync(pollIntervalMs);
            elapsed += pollIntervalMs;
        }
    }
}
