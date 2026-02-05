using Microsoft.Playwright;

namespace LocalFinanceManager.E2E.Pages;

/// <summary>
/// Page Object Model for the Auto-Apply Settings page.
/// Provides methods to configure auto-apply settings (enable/disable, confidence threshold, account selection).
/// </summary>
public class AutoApplySettingsPageModel : PageObjectBase
{
    // Selectors
    private const string EnableToggleSelector = "input[type='checkbox'][id='enable-auto-apply']";
    private const string ConfidenceSliderSelector = "input[type='range'][id='confidence-threshold']";
    private const string ConfidenceValueSelector = "[data-testid='confidence-value']";
    private const string PreviewStatsSelector = "[data-testid='preview-stats']";
    private const string AccountMultiSelectSelector = "select[id='account-ids']";
    private const string ExcludedCategoriesSelector = "select[id='excluded-categories']";
    private const string SaveButtonSelector = "button[type='submit']";
    private const string ValidationErrorSelector = ".validation-message";
    private const string SuccessToastSelector = "[data-testid='success-toast']";

    /// <summary>
    /// Initializes a new instance of the AutoApplySettingsPageModel class.
    /// </summary>
    /// <param name="page">Playwright page instance.</param>
    /// <param name="baseUrl">Base URL for the application.</param>
    public AutoApplySettingsPageModel(IPage page, string baseUrl) : base(page, baseUrl)
    {
    }

    /// <summary>
    /// Navigates to the Auto-Apply Settings page.
    /// </summary>
    public async Task NavigateAsync()
    {
        await NavigateToAsync("/admin/autoapply");
    }

    /// <summary>
    /// Toggles the "Enable Auto-Apply" switch.
    /// </summary>
    /// <param name="enabled">True to enable, false to disable.</param>
    public async Task SetEnableToggleAsync(bool enabled)
    {
        var isChecked = await Page.IsCheckedAsync(EnableToggleSelector);
        if (isChecked != enabled)
        {
            await Page.ClickAsync(EnableToggleSelector);
        }
    }

    /// <summary>
    /// Gets the current state of the enable toggle.
    /// </summary>
    /// <returns>True if enabled, false otherwise.</returns>
    public async Task<bool> IsEnabledAsync()
    {
        return await Page.IsCheckedAsync(EnableToggleSelector);
    }

    /// <summary>
    /// Sets the confidence threshold slider to a specific value.
    /// </summary>
    /// <param name="confidence">Confidence value (0.0 to 1.0).</param>
    public async Task SetConfidenceThresholdAsync(double confidence)
    {
        await Page.FillAsync(ConfidenceSliderSelector, confidence.ToString("0.00"));
    }

    /// <summary>
    /// Gets the current confidence threshold value displayed on the page.
    /// </summary>
    /// <returns>Confidence threshold as a string (e.g., "85%").</returns>
    public async Task<string> GetConfidenceDisplayValueAsync()
    {
        return await Page.InnerTextAsync(ConfidenceValueSelector);
    }

    /// <summary>
    /// Gets the preview stats text (e.g., "Based on last 100 transactions, 15 would auto-apply").
    /// </summary>
    /// <returns>Preview stats text.</returns>
    public async Task<string> GetPreviewStatsAsync()
    {
        await Page.WaitForSelectorAsync(PreviewStatsSelector, new() { Timeout = 5000 });
        return await Page.InnerTextAsync(PreviewStatsSelector);
    }

    /// <summary>
    /// Selects specific accounts for auto-apply.
    /// </summary>
    /// <param name="accountIds">Array of account IDs to select.</param>
    public async Task SelectAccountsAsync(params Guid[] accountIds)
    {
        var values = accountIds.Select(id => id.ToString()).ToArray();
        await Page.SelectOptionAsync(AccountMultiSelectSelector, values);
    }

    /// <summary>
    /// Selects categories to exclude from auto-apply.
    /// </summary>
    /// <param name="categoryIds">Array of category IDs to exclude.</param>
    public async Task SelectExcludedCategoriesAsync(params Guid[] categoryIds)
    {
        var values = categoryIds.Select(id => id.ToString()).ToArray();
        await Page.SelectOptionAsync(ExcludedCategoriesSelector, values);
    }

    /// <summary>
    /// Clicks the Save button to save settings.
    /// </summary>
    public async Task ClickSaveButtonAsync()
    {
        await Page.ClickAsync(SaveButtonSelector);
    }

    /// <summary>
    /// Waits for and retrieves the validation error message.
    /// </summary>
    /// <returns>Validation error text if present, null otherwise.</returns>
    public async Task<string?> GetValidationErrorAsync()
    {
        var errorElement = await Page.QuerySelectorAsync(ValidationErrorSelector);
        return errorElement != null ? await errorElement.InnerTextAsync() : null;
    }

    /// <summary>
    /// Waits for the success toast to appear after saving.
    /// </summary>
    /// <returns>True if success toast appeared, false otherwise.</returns>
    public async Task<bool> WaitForSuccessToastAsync()
    {
        try
        {
            await Page.WaitForSelectorAsync(SuccessToastSelector, new() { Timeout = 5000 });
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Saves settings and waits for confirmation.
    /// </summary>
    public async Task SaveSettingsAsync()
    {
        await ClickSaveButtonAsync();
        await WaitForSuccessToastAsync();
    }
}
