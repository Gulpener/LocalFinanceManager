using Microsoft.Playwright;

namespace LocalFinanceManager.E2E.Pages;

/// <summary>
/// Page Object Model for the Transactions page.
/// Provides methods to interact with transaction list, filters, and pagination.
/// </summary>
public class TransactionsPageModel : PageObjectBase
{
    // Selectors
    private const string AccountFilterSelector = "#account-filter";
    private const string AssignmentStatusFilterSelector = "#assignmentStatusFilter";
    private const string TransactionTableSelector = "table[data-testid='transactions-table']";
    private const string TransactionRowSelector = "tbody tr[data-testid='transaction-row']";
    private const string TransactionRowByIdSelector = "tbody tr[data-testid='transaction-row'][data-transaction-id='{0}']";
    private const string TransactionCheckboxByIdSelector = "tbody tr[data-testid='transaction-row'][data-transaction-id='{0}'] input[type='checkbox']";
    private const string SelectAllCheckboxSelector = "thead input[type='checkbox'][aria-label='Selecteer alle zichtbare transacties']";
    private const string BulkAssignButtonSelector = "button:has-text('Bulk toewijzen')";
    private const string DeselectAllButtonSelector = "button:has-text('Deselecteer alles')";
    private const string AssignButtonInRowSelector = "button:has-text('Toewijzen'), button:has-text('Wijzig')";
    private const string AuditButtonInRowSelector = "button[title='Bekijk toewijzingsgeschiedenis']";
    private const string PaginationSelector = ".pagination";
    private const string PageButtonSelector = ".pagination button[data-page='{0}']";
    private const string NextPageButtonSelector = ".pagination button[aria-label='Next']";
    private const string PreviousPageButtonSelector = ".pagination button[aria-label='Previous']";

    /// <summary>
    /// Initializes a new instance of the TransactionsPageModel class.
    /// </summary>
    /// <param name="page">Playwright page instance.</param>
    /// <param name="baseUrl">Base URL for the application.</param>
    public TransactionsPageModel(IPage page, string baseUrl) : base(page, baseUrl)
    {
    }

    /// <summary>
    /// Navigates to the transactions page.
    /// </summary>
    public async Task NavigateAsync()
    {
        await NavigateToAsync("/transactions");
    }

    /// <summary>
    /// Selects an account from the account filter dropdown.
    /// </summary>
    /// <param name="accountId">ID of the account to filter by.</param>
    public async Task SelectAccountFilterAsync(Guid accountId)
    {
        await Page.SelectOptionAsync(AccountFilterSelector, accountId.ToString());
        await WaitForSelectorAsync(TransactionRowSelector); // Wait for table to reload
    }

    /// <summary>
    /// Selects assignment status filter by display value.
    /// </summary>
    /// <param name="filterType">All, Assigned, or Uncategorized.</param>
    public async Task SelectFilterAsync(string filterType)
    {
        var value = filterType.Trim().ToLowerInvariant() switch
        {
            "all" => "all",
            "assigned" => "assigned",
            "uncategorized" => "unassigned",
            _ => "all"
        };

        await Page.SelectOptionAsync(AssignmentStatusFilterSelector, value);
        await Page.WaitForFunctionAsync(
            @"arg => {
                const select = document.querySelector(arg.selector);
                return !!select && select.value === arg.value;
            }",
            new { selector = AssignmentStatusFilterSelector, value });
    }

    /// <summary>
    /// Gets all transaction rows currently visible on the page.
    /// </summary>
    /// <returns>List of transaction row elements.</returns>
    public async Task<IReadOnlyList<IElementHandle>> GetTransactionRowsAsync()
    {
        return await Page.QuerySelectorAllAsync(TransactionRowSelector);
    }

    /// <summary>
    /// Gets the count of transaction rows currently visible.
    /// </summary>
    /// <returns>Number of transaction rows.</returns>
    public async Task<int> GetTransactionCountAsync()
    {
        var rows = await GetTransactionRowsAsync();
        return rows.Count;
    }

    /// <summary>
    /// Navigates to a specific page number in the pagination.
    /// </summary>
    /// <param name="pageNumber">Page number to navigate to (1-based).</param>
    public async Task NavigateToPageAsync(int pageNumber)
    {
        var pageButtonSelector = string.Format(PageButtonSelector, pageNumber);
        await Page.ClickAsync(pageButtonSelector);
        await WaitForSelectorAsync(TransactionRowSelector); // Wait for table to reload
    }

    /// <summary>
    /// Clicks the next page button in pagination.
    /// </summary>
    public async Task ClickNextPageAsync()
    {
        await Page.ClickAsync(NextPageButtonSelector);
        await WaitForSelectorAsync(TransactionRowSelector);
    }

    /// <summary>
    /// Clicks the previous page button in pagination.
    /// </summary>
    public async Task ClickPreviousPageAsync()
    {
        await Page.ClickAsync(PreviousPageButtonSelector);
        await WaitForSelectorAsync(TransactionRowSelector);
    }

    /// <summary>
    /// Clicks the "Assign" button for a specific transaction row.
    /// </summary>
    /// <param name="rowIndex">Zero-based index of the transaction row.</param>
    public async Task ClickAssignButtonForRowAsync(int rowIndex)
    {
        var rows = await GetTransactionRowsAsync();
        if (rowIndex >= rows.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(rowIndex),
                $"Row index {rowIndex} is out of range. Only {rows.Count} rows found.");
        }

        var assignButton = Page.Locator(TransactionRowSelector).Nth(rowIndex).Locator(AssignButtonInRowSelector).First;
        if (await assignButton.CountAsync() == 0)
        {
            throw new InvalidOperationException($"Assign button not found for row {rowIndex}.");
        }

        await assignButton.ClickAsync();
    }

    /// <summary>
    /// Clicks audit trail button for a specific transaction.
    /// </summary>
    public async Task ClickAuditTrailAsync(Guid transactionId)
    {
        var selector = string.Format(TransactionRowByIdSelector, transactionId);
        var row = Page.Locator(selector);
        await row.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

        var auditButton = row.Locator(AuditButtonInRowSelector).First;
        await auditButton.ClickAsync();
    }

    /// <summary>
    /// Selects a transaction checkbox by transaction id.
    /// </summary>
    public async Task SelectTransactionAsync(Guid transactionId)
    {
        var selector = string.Format(TransactionCheckboxByIdSelector, transactionId);
        var checkbox = Page.Locator(selector).First;
        await checkbox.CheckAsync();
    }

    /// <summary>
    /// Selects all currently visible transactions.
    /// </summary>
    public async Task SelectAllOnPageAsync()
    {
        await Page.Locator(SelectAllCheckboxSelector).CheckAsync();
    }

    /// <summary>
    /// Clears all selected transactions.
    /// </summary>
    public async Task DeselectAllAsync()
    {
        var deselectButton = Page.Locator(DeselectAllButtonSelector);
        if (await deselectButton.CountAsync() > 0)
        {
            await deselectButton.First.ClickAsync();
            return;
        }

        await Page.Locator(SelectAllCheckboxSelector).UncheckAsync();
    }

    /// <summary>
    /// Opens bulk assign modal from bottom action bar.
    /// </summary>
    public async Task ClickBulkAssignAsync()
    {
        await Page.Locator(BulkAssignButtonSelector).First.ClickAsync();
    }

    /// <summary>
    /// Waits for the transactions table to be visible on the page.
    /// </summary>
    public async Task WaitForTableAsync()
    {
        await WaitForSelectorAsync(TransactionTableSelector);
    }
}
