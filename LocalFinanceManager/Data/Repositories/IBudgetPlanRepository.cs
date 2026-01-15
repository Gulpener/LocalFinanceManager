using LocalFinanceManager.Models;

namespace LocalFinanceManager.Data.Repositories;

/// <summary>
/// Repository interface for BudgetPlan entities.
/// Provides specialized queries beyond generic repository operations.
/// </summary>
public interface IBudgetPlanRepository : IRepository<BudgetPlan>
{
    /// <summary>
    /// Gets all budget plans for a specific account, including their budget lines.
    /// </summary>
    Task<List<BudgetPlan>> GetByAccountIdAsync(Guid accountId);

    /// <summary>
    /// Gets a budget plan by ID with its budget lines included.
    /// </summary>
    Task<BudgetPlan?> GetByIdWithLinesAsync(Guid id);

    /// <summary>
    /// Gets a budget plan for a specific account and year.
    /// </summary>
    Task<BudgetPlan?> GetByAccountAndYearAsync(Guid accountId, int year);
}
