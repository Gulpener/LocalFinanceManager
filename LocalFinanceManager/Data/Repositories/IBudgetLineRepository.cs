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
}
