using Microsoft.Playwright;

namespace LocalFinanceManager.E2E.Pages;

/// <summary>
/// Page Object Model for the Transaction Audit Trail page (/transactions/{id}/audit).
/// </summary>
public class TransactionAuditPageModel : PageObjectBase
{
    private const string AuditContainerSelector = "[data-testid='audit-trail-container']";
    private const string AuditEntrySelector = "[data-testid='audit-entry']";
    private const string AutoAppliedBadgeSelector = "[data-testid='auto-applied-badge']";
    private const string ConfidenceScoreSelector = "[data-testid='confidence-score']";
    private const string ModelVersionSelector = "[data-testid='model-version']";
    private const string ActionTypeSelector = "[data-testid='audit-action-type']";
    private const string ActorSelector = "[data-testid='audit-actor']";
    private const string TimestampSelector = "[data-testid='audit-timestamp']";
    private const string BeforeStateSelector = "[data-testid='before-state']";
    private const string AfterStateSelector = "[data-testid='after-state']";
    private const string AuditReasonSelector = "[data-testid='audit-reason']";

    /// <summary>
    /// Initializes a new instance of the TransactionAuditPageModel class.
    /// </summary>
    /// <param name="page">Playwright page instance.</param>
    /// <param name="baseUrl">Base URL for the application.</param>
    public TransactionAuditPageModel(IPage page, string baseUrl) : base(page, baseUrl)
    {
    }

    /// <summary>
    /// Navigates to the audit trail page for the specified transaction.
    /// </summary>
    public async Task NavigateAsync(Guid transactionId)
    {
        await NavigateToAsync($"/transactions/{transactionId}/audit");
    }

    /// <summary>
    /// Waits for the audit trail container to be visible.
    /// </summary>
    public async Task WaitForPageLoadAsync()
    {
        await WaitForSelectorAsync(AuditContainerSelector, 15000);
    }

    /// <summary>
    /// Returns the number of audit entries visible on the current page.
    /// </summary>
    public async Task<int> GetAuditEntryCountAsync()
    {
        return await Page.Locator(AuditEntrySelector).CountAsync();
    }

    /// <summary>
    /// Returns whether the auto-applied badge is visible for the given entry index.
    /// </summary>
    public async Task<bool> HasAutoAppliedBadgeAsync(int entryIndex)
    {
        var entry = Page.Locator(AuditEntrySelector).Nth(entryIndex);
        return await entry.Locator(AutoAppliedBadgeSelector).CountAsync() > 0;
    }

    /// <summary>
    /// Returns the confidence score text for the given entry index.
    /// </summary>
    public async Task<string> GetConfidenceScoreAsync(int entryIndex)
    {
        var entry = Page.Locator(AuditEntrySelector).Nth(entryIndex);
        var score = entry.Locator(ConfidenceScoreSelector);
        if (await score.CountAsync() == 0) return string.Empty;
        return await score.TextContentAsync() ?? string.Empty;
    }

    /// <summary>
    /// Returns the model version text for the given entry index.
    /// </summary>
    public async Task<string> GetModelVersionAsync(int entryIndex)
    {
        var entry = Page.Locator(AuditEntrySelector).Nth(entryIndex);
        var version = entry.Locator(ModelVersionSelector);
        if (await version.CountAsync() == 0) return string.Empty;
        return await version.TextContentAsync() ?? string.Empty;
    }

    /// <summary>
    /// Returns the action type text for the given entry index.
    /// </summary>
    public async Task<string> GetActionTypeAsync(int entryIndex)
    {
        var entry = Page.Locator(AuditEntrySelector).Nth(entryIndex);
        return await entry.Locator(ActionTypeSelector).TextContentAsync() ?? string.Empty;
    }

    /// <summary>
    /// Returns the actor (ChangedBy) text for the given entry index.
    /// </summary>
    public async Task<string> GetActorAsync(int entryIndex)
    {
        var entry = Page.Locator(AuditEntrySelector).Nth(entryIndex);
        return await entry.Locator(ActorSelector).TextContentAsync() ?? string.Empty;
    }

    /// <summary>
    /// Returns the timestamp text for the given entry index.
    /// </summary>
    public async Task<string> GetTimestampTextAsync(int entryIndex)
    {
        var entry = Page.Locator(AuditEntrySelector).Nth(entryIndex);
        return await entry.Locator(TimestampSelector).TextContentAsync() ?? string.Empty;
    }

    /// <summary>
    /// Expands the state changes section for the given entry index.
    /// </summary>
    public async Task ExpandStateChangesAsync(int entryIndex)
    {
        var entry = Page.Locator(AuditEntrySelector).Nth(entryIndex);
        var expandButton = entry.Locator("button:has-text('Toon statuswijzigingen')");
        if (await expandButton.CountAsync() > 0)
        {
            await expandButton.ClickAsync();
        }
    }

    /// <summary>
    /// Returns the before-state JSON text for the given entry index (must be expanded first).
    /// </summary>
    public async Task<string> GetBeforeStateAsync(int entryIndex)
    {
        var entry = Page.Locator(AuditEntrySelector).Nth(entryIndex);
        var el = entry.Locator(BeforeStateSelector);
        if (await el.CountAsync() == 0) return string.Empty;
        return await el.TextContentAsync() ?? string.Empty;
    }

    /// <summary>
    /// Returns the after-state JSON text for the given entry index (must be expanded first).
    /// </summary>
    public async Task<string> GetAfterStateAsync(int entryIndex)
    {
        var entry = Page.Locator(AuditEntrySelector).Nth(entryIndex);
        var el = entry.Locator(AfterStateSelector);
        if (await el.CountAsync() == 0) return string.Empty;
        return await el.TextContentAsync() ?? string.Empty;
    }

    /// <summary>
    /// Returns the reason text for the given entry index.
    /// </summary>
    public async Task<string> GetReasonAsync(int entryIndex)
    {
        var entry = Page.Locator(AuditEntrySelector).Nth(entryIndex);
        var el = entry.Locator(AuditReasonSelector);
        if (await el.CountAsync() == 0) return string.Empty;
        return await el.TextContentAsync() ?? string.Empty;
    }
}
