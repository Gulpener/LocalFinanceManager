using LocalFinanceManager.Domain.Entities;

namespace LocalFinanceManager.Application.Interfaces;

/// <summary>
/// Summary of budget vs actual spending.
/// </summary>
public class BudgetSummary
{
    /// <summary>
    /// The budget record.
    /// </summary>
    public Budget Budget { get; set; } = null!;

    /// <summary>
    /// Planned amount for the period.
    /// </summary>
    public decimal PlannedAmount { get; set; }

    /// <summary>
    /// Actual amount spent/received.
    /// </summary>
    public decimal ActualAmount { get; set; }

    /// <summary>
    /// Remaining amount (Planned - Actual).
    /// </summary>
    public decimal RemainingAmount => PlannedAmount - Math.Abs(ActualAmount);

    /// <summary>
    /// Percentage used (0-100+).
    /// </summary>
    public decimal PercentageUsed => PlannedAmount != 0
        ? Math.Abs(ActualAmount) / PlannedAmount * 100
        : 0;

    /// <summary>
    /// Whether the budget is exceeded.
    /// </summary>
    public bool IsExceeded => Math.Abs(ActualAmount) > PlannedAmount;
}

/// <summary>
/// Repository interface for Budget entity operations.
/// </summary>
public interface IBudgetRepository
{
    /// <summary>
    /// Gets budgets for a specific month.
    /// </summary>
    /// <param name="month">The month (first day of month).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of budgets for the month.</returns>
    Task<IReadOnlyList<Budget>> GetByMonthAsync(DateTime month, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a budget by ID.
    /// </summary>
    /// <param name="id">The budget ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The budget if found; otherwise null.</returns>
    Task<Budget?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new budget.
    /// </summary>
    /// <param name="budget">The budget to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added budget with generated ID.</returns>
    Task<Budget> AddAsync(Budget budget, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing budget.
    /// </summary>
    /// <param name="budget">The budget to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(Budget budget, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a budget by ID.
    /// </summary>
    /// <param name="id">The budget ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted; false if not found.</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service interface for budget calculations.
/// </summary>
public interface IBudgetService
{
    /// <summary>
    /// Gets budget summaries for a month.
    /// </summary>
    /// <param name="month">The month to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Budget summaries with actual spending.</returns>
    Task<IReadOnlyList<BudgetSummary>> GetMonthlySummaryAsync(DateTime month, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets budget summary for a specific category in a month.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="month">The month to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Budget summary for the category.</returns>
    Task<BudgetSummary?> GetCategoryBudgetSummaryAsync(int categoryId, DateTime month, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets budget summary for a specific account in a month.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="month">The month to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Budget summary for the account.</returns>
    Task<BudgetSummary?> GetAccountBudgetSummaryAsync(int accountId, DateTime month, CancellationToken cancellationToken = default);
}
