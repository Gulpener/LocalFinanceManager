namespace LocalFinanceManager.Services;

/// <summary>
/// Cached lookup service to validate budget line to account relationships.
/// Ensures transaction splits reference budget lines from the correct account's budget plan.
/// </summary>
public interface IBudgetAccountLookupService
{
    /// <summary>
    /// Gets the account ID associated with a budget line (via BudgetPlan).
    /// Results are cached for performance.
    /// </summary>
    /// <param name="budgetLineId">Budget line ID to look up.</param>
    /// <returns>Account ID, or null if budget line not found.</returns>
    Task<Guid?> GetAccountIdForBudgetLineAsync(Guid budgetLineId);

    /// <summary>
    /// Batch query for multiple budget line IDs.
    /// Optimizes database queries by fetching uncached entries in single query.
    /// </summary>
    /// <param name="budgetLineIds">Budget line IDs to look up.</param>
    /// <returns>Dictionary mapping BudgetLineId → AccountId.</returns>
    Task<Dictionary<Guid, Guid>> GetAccountIdsForBudgetLinesAsync(IEnumerable<Guid> budgetLineIds);

    /// <summary>
    /// Invalidates all cache entries for a specific account.
    /// Used when account's current budget plan changes.
    /// Pattern: "BudgetPlanValidation:{accountId}:*"
    /// </summary>
    void InvalidateAccountCache(Guid accountId);

    /// <summary>
    /// Invalidates all cache entries for a specific category.
    /// Used when category's budget plan association changes.
    /// Pattern: "BudgetPlanValidation:*:{categoryId}"
    /// </summary>
    void InvalidateCategoryCache(Guid categoryId);

    /// <summary>
    /// Clears all cached validation entries.
    /// Used when budget plans are deleted or major structural changes occur.
    /// </summary>
    void ClearAllCache();
}
