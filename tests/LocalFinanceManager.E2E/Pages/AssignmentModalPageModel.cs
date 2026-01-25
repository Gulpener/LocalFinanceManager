using Microsoft.Playwright;

namespace LocalFinanceManager.E2E.Pages;

/// <summary>
/// Page Object Model for the Assignment Modal.
/// Provides methods to interact with the transaction assignment modal (category selection, notes, etc.).
/// </summary>
public class AssignmentModalPageModel : PageObjectBase
{
    // Selectors
    private const string ModalSelector = "[data-testid='assignment-modal']";
    private const string CategoryDropdownSelector = "#category-select";
    private const string BudgetLineDropdownSelector = "#budgetline-select";
    private const string NoteInputSelector = "#assignment-note";
    private const string AssignButtonSelector = "button[data-action='submit-assign']";
    private const string CancelButtonSelector = "button[data-action='cancel-assign']";
    private const string SplitButtonSelector = "button[data-action='split-transaction']";

    /// <summary>
    /// Initializes a new instance of the AssignmentModalPageModel class.
    /// </summary>
    /// <param name="page">Playwright page instance.</param>
    /// <param name="baseUrl">Base URL for the application.</param>
    public AssignmentModalPageModel(IPage page, string baseUrl) : base(page, baseUrl)
    {
    }

    /// <summary>
    /// Waits for the assignment modal to appear.
    /// </summary>
    public async Task WaitForModalAsync()
    {
        await WaitForSelectorAsync(ModalSelector);
    }

    /// <summary>
    /// Selects a category from the category dropdown.
    /// </summary>
    /// <param name="categoryId">ID of the category to select.</param>
    public async Task SelectCategoryAsync(Guid categoryId)
    {
        await Page.SelectOptionAsync(CategoryDropdownSelector, categoryId.ToString());
    }

    /// <summary>
    /// Selects a budget line from the budget line dropdown.
    /// </summary>
    /// <param name="budgetLineId">ID of the budget line to select.</param>
    public async Task SelectBudgetLineAsync(Guid budgetLineId)
    {
        await Page.SelectOptionAsync(BudgetLineDropdownSelector, budgetLineId.ToString());
    }

    /// <summary>
    /// Enters a note in the assignment note input field.
    /// </summary>
    /// <param name="note">Note text to enter.</param>
    public async Task EnterNoteAsync(string note)
    {
        await Page.FillAsync(NoteInputSelector, note);
    }

    /// <summary>
    /// Clicks the Assign button to submit the assignment.
    /// </summary>
    public async Task ClickAssignAsync()
    {
        await Page.ClickAsync(AssignButtonSelector);
        await WaitForModalToCloseAsync();
    }

    /// <summary>
    /// Clicks the Cancel button to close the modal without assigning.
    /// </summary>
    public async Task ClickCancelAsync()
    {
        await Page.ClickAsync(CancelButtonSelector);
        await WaitForModalToCloseAsync();
    }

    /// <summary>
    /// Clicks the Split button to open the split editor.
    /// </summary>
    public async Task ClickSplitAsync()
    {
        await Page.ClickAsync(SplitButtonSelector);
    }

    /// <summary>
    /// Checks if the modal is currently visible.
    /// </summary>
    /// <returns>True if modal is visible, false otherwise.</returns>
    public async Task<bool> IsModalVisibleAsync()
    {
        var modal = await Page.QuerySelectorAsync(ModalSelector);
        return modal != null && await modal.IsVisibleAsync();
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
