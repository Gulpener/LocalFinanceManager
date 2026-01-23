using LocalFinanceManager.Models;

namespace LocalFinanceManager.Data.Repositories;

/// <summary>
/// Repository interface for BudgetLine entities.
/// Provides specialized queries beyond generic repository operations.
/// </summary>
public interface IBudgetLineRepository : IRepository<BudgetLine>
{
    /// <summary>
    /// Gets all budget lines for a specific budget plan.
    /// </summary>
    Task<List<BudgetLine>> GetByBudgetPlanIdAsync(Guid budgetPlanId);

    /// <summary>
    /// Gets the account ID associated with a budget line (via BudgetPlan).
    /// Used for budget line to account validation.
    /// </summary>
    Task<Guid?> GetAccountIdForBudgetLineAsync(Guid budgetLineId);

    /// <summary>
    /// Batch query for multiple budget line IDs.
    /// Returns dictionary mapping BudgetLineId → AccountId.
    /// Optimizes performance by fetching all mappings in single query.
    /// </summary>
    Task<Dictionary<Guid, Guid>> GetAccountMappingsAsync(IEnumerable<Guid> budgetLineIds);
}
