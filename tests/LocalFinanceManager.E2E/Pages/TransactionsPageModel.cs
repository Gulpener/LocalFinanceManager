using Microsoft.Playwright;

namespace LocalFinanceManager.E2E.Pages;

/// <summary>
/// Page Object Model for the Transactions page.
/// Provides methods to interact with transaction list, filters, and pagination.
/// </summary>
public class TransactionsPageModel : PageObjectBase
{
    private const int FilterTableStableWaitMs = 300;
    private const int FilterTableUpdateTimeoutMs = 30000;
    private const string FilterPollingStateKey = "__lfm_transactions_filter_polling_state";

    // Selectors
    private const string AccountFilterSelector = "#account-filter";
    private const string AssignmentStatusFilterSelector = "#assignmentStatusFilter";
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
        // The table and no-transactions elements carry a data-loaded-account attribute
        // that equals the currently loaded account ID (set when loading=false).
        // This is the most reliable way to detect Blazor re-render completion because
        // the row-ID comparison fails when the same transactions show for all accounts.
        var accountIdStr = accountId.ToString();
        await Page.WaitForFunctionAsync(
            @"arg => {
                const table = document.querySelector(arg.tableSelector);
                if (table && table.getAttribute('data-loaded-account') === arg.accountId) return true;
                const noTx = document.querySelector(arg.noTxSelector);
                if (noTx && noTx.getAttribute('data-loaded-account') === arg.accountId) return true;
                return false;
            }",
            new
            {
                tableSelector = TransactionTableSelector,
                noTxSelector = NoTransactionsSelector,
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

        var expectedStatusMode = value switch
        {
            "assigned" => "assigned",
            "unassigned" => "unassigned",
            _ => "all"
        };

        var previousValue = await Page.EvaluateAsync<string>(
            @"selector => {
                const select = document.querySelector(selector);
                return select ? select.value : '';
            }",
            AssignmentStatusFilterSelector);

        var initialTableSignature = await Page.EvaluateAsync<string>(
            @"selector => {
                const rows = Array.from(document.querySelectorAll(selector));
                return rows
                    .map(row => {
                        const id = row.getAttribute('data-transaction-id') ?? '';
                        const isUnassigned = !!row.querySelector('[aria-label=""Niet toegewezen""]');
                        const status = isUnassigned ? 'unassigned' : 'assigned';
                        return `${id}|${status}`;
                    })
                    .join(';');
            }",
            TransactionRowSelector);

        await Page.EvaluateAsync(
            @"arg => {
                window[arg.stateKey] = { startedAt: Date.now() };
            }",
            new
            {
                stateKey = FilterPollingStateKey,
            });

        try
        {
            await Page.SelectOptionAsync(AssignmentStatusFilterSelector, value);
            await Page.WaitForFunctionAsync(
                @"arg => {
                    const select = document.querySelector(arg.selector);
                    return !!select && select.value === arg.value;
                }",
                new { selector = AssignmentStatusFilterSelector, value });

            await Page.WaitForFunctionAsync(
                @"arg => {
                    const rows = Array.from(document.querySelectorAll(arg.rowSelector));
                    const currentSignature = rows
                        .map(row => {
                            const id = row.getAttribute('data-transaction-id') ?? '';
                            const isUnassigned = !!row.querySelector('[aria-label=""Niet toegewezen""]');
                            const status = isUnassigned ? 'unassigned' : 'assigned';
                            return `${id}|${status}`;
                        })
                        .join(';');

                    const hasTableChanged = currentSignature !== arg.initialTableSignature;
                    const rowsMatchFilter = arg.expectedStatusMode === 'unassigned'
                        ? rows.every(row => !!row.querySelector('[aria-label=""Niet toegewezen""]'))
                        : arg.expectedStatusMode === 'assigned'
                            ? rows.every(row => !row.querySelector('[aria-label=""Niet toegewezen""]'))
                            : true;

                    if (!rowsMatchFilter)
                    {
                        return false;
                    }

                    // If the table contents have changed compared to the initial signature,
                    // and rows match the filter, we can consider the update completed.
                    // Require at least one row here so a transient empty table during reload
                    // does not prematurely satisfy the wait; stable empty tables are handled
                    // by the stability window logic below.
                    if (hasTableChanged && rows.length > 0)
                    {
                        return true;
                    }

                    // Fallback: no detected table change yet (or empty table). Use a small stability window
                    // tracked on window[arg.stateKey] to avoid flakiness.
                    const state = window[arg.stateKey] || (window[arg.stateKey] = {});

                    if (state.currentValue !== arg.value)
                    {
                        state.currentValue = arg.value;
                        state.startedAt = Date.now();
                    }

                    const startedAt = state.startedAt ?? Date.now();
                    const elapsed = Date.now() - startedAt;

                    if (arg.previousValue === arg.value)
                    {
                        // Filter value did not change (e.g. already persisted in localStorage).
                        // The table may already reflect the correct state – just require the
                        // short stability window before returning to avoid acting on a
                        // transient in-progress Blazor re-render.
                        return elapsed >= arg.minStableMs;
                    }

                    if (arg.expectedStatusMode === 'all')
                    {
                        return elapsed >= arg.minStableMs;
                    }

                    // For 'assigned' and 'unassigned', also require a minimum stable duration.
                    return elapsed >= arg.minStableMs;
                }",
                new
                {
                    rowSelector = TransactionRowSelector,
                    initialTableSignature,
                    expectedStatusMode,
                    previousValue,
                    value,
                    stateKey = FilterPollingStateKey,
                    minStableMs = FilterTableStableWaitMs,
                },
                new PageWaitForFunctionOptions
                {
                    Timeout = FilterTableUpdateTimeoutMs
                });
        }
        finally
        {
            await Page.EvaluateAsync(
                @"stateKey => {
                    delete window[stateKey];
                }",
                FilterPollingStateKey);
        }
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
