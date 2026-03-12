using System.Globalization;
using Microsoft.Playwright;

namespace LocalFinanceManager.E2E.Pages;

/// <summary>
/// Page Object Model for the Split Editor modal.
/// Provides methods to add/remove split rows, set amounts, select categories, and validate sum.
/// </summary>
public class SplitEditorPageModel : PageObjectBase
{
    // Selectors
    private const string ModalSelector = "[data-testid='split-editor-modal']";
    private const string SplitRowSelector = "[data-testid='split-row']";
    private const string AddSplitButtonSelector = "button[data-action='add-split']";
    private const string RemoveSplitButtonSelector = "button[data-action='remove-split']";
    private const string SplitAmountInputSelector = "input[data-field='split-amount']";
    private const string SplitCategorySelectSelector = "select[data-field='split-category']";
    private const string SumValidationIndicatorSelector = "[data-testid='sum-validation']";
    private const string SaveSplitButtonSelector = "button[data-action='save-split']";
    private const string CancelSplitButtonSelector = "button[data-action='cancel-split']";

    /// <summary>
    /// Initializes a new instance of the SplitEditorPageModel class.
    /// </summary>
    /// <param name="page">Playwright page instance.</param>
    /// <param name="baseUrl">Base URL for the application.</param>
    public SplitEditorPageModel(IPage page, string baseUrl) : base(page, baseUrl)
    {
    }

    /// <summary>
    /// Checks whether the split editor modal is currently visible.
    /// </summary>
    /// <returns>True if the modal is visible, false otherwise.</returns>
    public async Task<bool> IsVisibleAsync()
    {
        var modal = await Page.QuerySelectorAsync(ModalSelector);
        return modal != null && await modal.IsVisibleAsync();
    }

    /// <summary>
    /// Waits for the split editor modal to appear and for at least 2 split rows to be rendered.
    /// </summary>
    public async Task WaitForModalAsync()
    {
        await WaitForSelectorAsync(ModalSelector, 10000);
        // Wait for Blazor to render the initial 2 split rows (InitializeSplits adds 2 rows)
        await Page.WaitForFunctionAsync(
            "() => document.querySelectorAll('[data-testid=\"split-row\"]').length >= 2",
            new PageWaitForFunctionOptions { Timeout = 8000 });
    }

    /// <summary>
    /// Adds a new split row to the editor.
    /// </summary>
    public async Task AddSplitRowAsync()
    {
        var currentCount = await Page.Locator(SplitRowSelector).CountAsync();
        await Page.ClickAsync(AddSplitButtonSelector);
        // Wait for new split row to appear in DOM (increase timeout for Blazor Server SignalR)
        await Page.WaitForFunctionAsync(
            "(expected) => document.querySelectorAll('[data-testid=\"split-row\"]').length === expected",
            currentCount + 1,
            new() { Timeout = 5000 });
    }

    /// <summary>
    /// Removes a split row at the specified index.
    /// </summary>
    /// <param name="index">Zero-based index of the split row to remove.</param>
    public async Task RemoveSplitRowAsync(int index)
    {
        var rows = await Page.QuerySelectorAllAsync(SplitRowSelector);
        if (index >= rows.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index),
                $"Split row index {index} is out of range. Only {rows.Count} rows found.");
        }

        var removeButton = await rows[index].QuerySelectorAsync(RemoveSplitButtonSelector);
        if (removeButton == null)
        {
            throw new InvalidOperationException($"Remove button not found for split row {index}.");
        }

        var currentCount = await Page.Locator(SplitRowSelector).CountAsync();
        await removeButton.ClickAsync();
        // Wait for split row to be removed from DOM (increase timeout for Blazor Server SignalR)
        await Page.WaitForFunctionAsync(
            "(expected) => document.querySelectorAll('[data-testid=\"split-row\"]').length === expected",
            currentCount - 1,
            new() { Timeout = 5000 });
    }

    /// <summary>
    /// Sets the amount for a split row at the specified index.
    /// </summary>
    /// <param name="index">Zero-based index of the split row.</param>
    /// <param name="amount">Amount to set (decimal value).</param>
    public async Task SetSplitAmountAsync(int index, decimal amount)
    {
        // Use locator-based approach — Playwright retries until the row is in the DOM
        var rowLocator = Page.Locator(SplitRowSelector).Nth(index);
        await rowLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = 8000 });

        var amountInput = rowLocator.Locator(SplitAmountInputSelector);
        await amountInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });

        await amountInput.FillAsync(amount.ToString("0.00", CultureInfo.InvariantCulture));
        // Press Tab to blur the input, triggering Blazor's @bind:event="onchange" binding
        // and the subsequent @bind:after="RecalculateSum" callback for live sum validation.
        await amountInput.PressAsync("Tab");
        // Wait for Blazor Server to process the onchange event and push the updated DOM
        // back to the browser over SignalR before any caller reads validation state.
        await Page.WaitForTimeoutAsync(300);
    }

    /// <summary>
    /// Selects a category for a split row at the specified index.
    /// </summary>
    /// <param name="index">Zero-based index of the split row.</param>
    /// <param name="categoryId">ID of the category to select.</param>
    public async Task SelectSplitCategoryAsync(int index, Guid categoryId)
    {
        // Use locator-based approach — Playwright retries until the row is in the DOM
        var rowLocator = Page.Locator(SplitRowSelector).Nth(index);
        await rowLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = 8000 });

        // Wait for budget lines to load (select becomes enabled after async LoadBudgetLines())
        var categorySelect = rowLocator.Locator(SplitCategorySelectSelector + ":not([disabled])");
        await categorySelect.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 8000 });

        await categorySelect.SelectOptionAsync(categoryId.ToString());
    }

    /// <summary>
    /// Gets the sum validation status (valid/invalid indicator).
    /// </summary>
    /// <returns>Validation status text (e.g., "Valid", "Invalid - Sum does not match").</returns>
    public async Task<string> GetSumValidationStatusAsync()
    {
        var validationIndicator = await Page.QuerySelectorAsync(SumValidationIndicatorSelector);
        if (validationIndicator == null)
        {
            return "Unknown";
        }

        var text = await validationIndicator.TextContentAsync();
        return text?.Trim() ?? "Unknown";
    }

    /// <summary>
    /// Checks if the sum validation is showing a valid state.
    /// </summary>
    /// <returns>True if sum is valid, false otherwise.</returns>
    public async Task<bool> IsSumValidAsync()
    {
        var validationIndicator = await Page.QuerySelectorAsync(SumValidationIndicatorSelector);
        if (validationIndicator == null)
        {
            return false;
        }

        var classList = await validationIndicator.GetAttributeAsync("class");
        // Split by whitespace to avoid "invalid" containing "valid" as a substring
        var classes = classList?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        return classes.Contains("valid");
    }

    /// <summary>
    /// Gets the current count of split rows.
    /// </summary>
    /// <returns>Number of split rows.</returns>
    public async Task<int> GetSplitRowCountAsync()
    {
        var rows = await Page.QuerySelectorAllAsync(SplitRowSelector);
        return rows.Count;
    }

    /// <summary>
    /// Clicks the Save button to submit the split assignment.
    /// </summary>
    public async Task ClickSaveAsync()
    {
        await Page.ClickAsync(SaveSplitButtonSelector);
        await WaitForModalToCloseAsync();
    }

    /// <summary>
    /// Clicks the Cancel button to close the modal without saving.
    /// </summary>
    public async Task ClickCancelAsync()
    {
        await Page.ClickAsync(CancelSplitButtonSelector);
        await WaitForModalToCloseAsync();
    }

    /// <summary>
    /// Waits for the modal to close (disappear from DOM).
    /// </summary>
    private async Task WaitForModalToCloseAsync()
    {
        await Page.WaitForSelectorAsync(ModalSelector, new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Detached,
            Timeout = 5000
        });
    }
}
