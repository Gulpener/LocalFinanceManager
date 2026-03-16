using Microsoft.Playwright;

namespace LocalFinanceManager.E2E.Pages;

/// <summary>
/// Page Object Model for the Transactions page.
/// Provides methods to interact with transaction list, filters, and pagination.
/// </summary>
public class TransactionsPageModel : PageObjectBase
{
    private const int FilterTableUpdateTimeoutMs = 30000;

    // Selectors
    private const string AccountFilterSelector = "#account-filter";
    private const string AssignmentStatusFilterSelector = "#assignmentStatusFilter";
    private const string TransactionListContainerSelector = "[data-testid='transactions-list-container']";
    private const string TransactionTableSelector = "table[data-testid='transactions-table']";
    private const string NoTransactionsSelector = "[data-testid='no-transactions-message']";
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

    private const string SplitButtonInRowSelector = "button[title='Splits transactie']";

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

        // Wait for Blazor to finish re-rendering with data for the selected account.
        // The transactions-list-container always exists and carries data-loaded-account
        // that is updated when the account data finishes loading (loading=false).
        var accountIdStr = accountId.ToString();
        await Page.WaitForFunctionAsync(
            @"arg => {
                const container = document.querySelector(arg.containerSelector);
                return !!container && container.getAttribute('data-loaded-account') === arg.accountId;
            }",
            new
            {
                containerSelector = TransactionListContainerSelector,
                accountId = accountIdStr
            },
            new PageWaitForFunctionOptions { Timeout = 15000 });
    }

    /// <summary>
    /// Selects assignment status filter by display value.
    /// </summary>
    /// <param name="filterType">All, Assigned, or Uncategorized.</param>
    public async Task SelectFilterAsync(string filterType)
    {
        if (filterType is null)
        {
            throw new ArgumentNullException(nameof(filterType));
        }

        var normalized = filterType.Trim().ToLowerInvariant();

        var value = normalized switch
        {
            "all" => "all",
            "assigned" => "assigned",
            "uncategorized" => "unassigned",
            _ => throw new ArgumentOutOfRangeException(
                nameof(filterType),
                filterType,
                "Unsupported filter type for SelectFilterAsync. Expected 'all', 'assigned', or 'uncategorized'.")
        };

        // Ensure the filter dropdown is visible (inside the always-expanded filter panel).
        await Page.Locator(AssignmentStatusFilterSelector).WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5_000 });

        // Use Locator API (handles element detachment from Blazor re-renders more reliably).
        await Page.Locator(AssignmentStatusFilterSelector).SelectOptionAsync(value);

        // The transactions-list-container always exists in the DOM (regardless of loading state)
        // and carries data-filter-assignment equal to filterState.AssignmentStatus.
        // Blazor updates this attribute synchronously during OnFiltersChanged → StateHasChanged().
        // Waiting on this stable container avoids the race where the skeleton (loading=true)
        // hides the conditional table/no-tx elements during a concurrent LoadTransactionsAsync.
        await Page.WaitForFunctionAsync(
            @"arg => {
                const container = document.querySelector(arg.containerSelector);
                return !!container && container.getAttribute('data-filter-assignment') === arg.value;
            }",
            new
            {
                containerSelector = TransactionListContainerSelector,
                value,
            },
            new PageWaitForFunctionOptions { Timeout = FilterTableUpdateTimeoutMs });
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
    /// Waits for Blazor to complete the re-render so all row checkboxes reflect the checked state.
    /// </summary>
    public async Task SelectAllOnPageAsync()
    {
        await Page.Locator(SelectAllCheckboxSelector).CheckAsync();

        // Wait for Blazor to re-render all row checkboxes as checked.
        // CheckAsync() sends the event via SignalR; the DOM update is async.
        await Page.WaitForFunctionAsync(
            @"selector => {
                const checkboxes = Array.from(document.querySelectorAll(selector));
                return checkboxes.length > 0 && checkboxes.every(cb => cb.checked);
            }",
            "tbody tr[data-testid='transaction-row'] input[type='checkbox']",
            new PageWaitForFunctionOptions { Timeout = 10_000 });
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
    /// Clicks the "Split" button for a specific transaction row by transaction ID.
    /// </summary>
    /// <param name="transactionId">ID of the transaction to open split editor for.</param>
    public async Task ClickSplitButtonAsync(Guid transactionId)
    {
        var rowSelector = string.Format(TransactionRowByIdSelector, transactionId);
        var row = Page.Locator(rowSelector);
        await row.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

        var splitButton = row.Locator(SplitButtonInRowSelector).First;
        await splitButton.ClickAsync();
    }

    /// <summary>
    /// Waits for the transactions table to be visible on the page.
    /// </summary>
    public async Task WaitForTableAsync()
    {
        await WaitForSelectorAsync(TransactionTableSelector);
    }
}
