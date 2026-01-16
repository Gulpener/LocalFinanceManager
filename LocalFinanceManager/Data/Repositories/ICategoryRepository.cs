using LocalFinanceManager.Models;

namespace LocalFinanceManager.Data.Repositories;

/// <summary>
/// Repository interface for Category entities.
/// Provides specialized queries beyond generic repository operations.
/// </summary>
public interface ICategoryRepository : IRepository<Category>
{
    /// <summary>
    /// Gets all active categories for a specific budget plan.
    /// </summary>
    Task<List<Category>> GetByBudgetPlanAsync(Guid budgetPlanId);

    /// <summary>
    /// Gets a category by name within a budget plan, excluding archived categories.
    /// </summary>
    Task<Category?> GetByNameAsync(Guid budgetPlanId, string name);
}
