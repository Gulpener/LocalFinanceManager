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
    /// Waits for the split editor modal to appear.
    /// </summary>
    public async Task WaitForModalAsync()
    {
        await WaitForSelectorAsync(ModalSelector);
    }

    /// <summary>
    /// Adds a new split row to the editor.
    /// </summary>
    public async Task AddSplitRowAsync()
    {
        await Page.ClickAsync(AddSplitButtonSelector);
        await Task.Delay(200); // Small delay for DOM update
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

        await removeButton.ClickAsync();
        await Task.Delay(200); // Small delay for DOM update
    }

    /// <summary>
    /// Sets the amount for a split row at the specified index.
    /// </summary>
    /// <param name="index">Zero-based index of the split row.</param>
    /// <param name="amount">Amount to set (decimal value).</param>
    public async Task SetSplitAmountAsync(int index, decimal amount)
    {
        var rows = await Page.QuerySelectorAllAsync(SplitRowSelector);
        if (index >= rows.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index),
                $"Split row index {index} is out of range. Only {rows.Count} rows found.");
        }

        var amountInput = await rows[index].QuerySelectorAsync(SplitAmountInputSelector);
        if (amountInput == null)
        {
            throw new InvalidOperationException($"Amount input not found for split row {index}.");
        }

        await amountInput.FillAsync(amount.ToString("0.00"));
    }

    /// <summary>
    /// Selects a category for a split row at the specified index.
    /// </summary>
    /// <param name="index">Zero-based index of the split row.</param>
    /// <param name="categoryId">ID of the category to select.</param>
    public async Task SelectSplitCategoryAsync(int index, Guid categoryId)
    {
        var rows = await Page.QuerySelectorAllAsync(SplitRowSelector);
        if (index >= rows.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index),
                $"Split row index {index} is out of range. Only {rows.Count} rows found.");
        }

        var categorySelect = await rows[index].QuerySelectorAsync(SplitCategorySelectSelector);
        if (categorySelect == null)
        {
            throw new InvalidOperationException($"Category select not found for split row {index}.");
        }

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
        return classList?.Contains("valid") ?? false;
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
