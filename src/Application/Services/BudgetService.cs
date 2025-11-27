using LocalFinanceManager.Application.Interfaces;
using LocalFinanceManager.Domain.Entities;

namespace LocalFinanceManager.Application.Services;

/// <summary>
/// Service for budget calculations and summaries.
/// </summary>
public class BudgetService : IBudgetService
{
    private readonly IBudgetRepository _budgetRepository;
    private readonly ITransactionRepository _transactionRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetService"/> class.
    /// </summary>
    /// <param name="budgetRepository">The budget repository.</param>
    /// <param name="transactionRepository">The transaction repository.</param>
    public BudgetService(
        IBudgetRepository budgetRepository,
        ITransactionRepository transactionRepository)
    {
        _budgetRepository = budgetRepository;
        _transactionRepository = transactionRepository;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BudgetSummary>> GetMonthlySummaryAsync(DateTime month, CancellationToken cancellationToken = default)
    {
        var monthStart = new DateTime(month.Year, month.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var budgets = await _budgetRepository.GetByMonthAsync(monthStart, cancellationToken);
        var transactions = await _transactionRepository.GetByDateRangeAsync(monthStart, monthEnd, cancellationToken);

        var summaries = new List<BudgetSummary>();

        foreach (var budget in budgets)
        {
            var actualAmount = CalculateActualAmount(budget, transactions);

            summaries.Add(new BudgetSummary
            {
                Budget = budget,
                PlannedAmount = budget.PlannedAmount,
                ActualAmount = actualAmount
            });
        }

        return summaries;
    }

    /// <inheritdoc />
    public async Task<BudgetSummary?> GetCategoryBudgetSummaryAsync(int categoryId, DateTime month, CancellationToken cancellationToken = default)
    {
        var summaries = await GetMonthlySummaryAsync(month, cancellationToken);
        return summaries.FirstOrDefault(s => s.Budget.CategoryId == categoryId);
    }

    /// <inheritdoc />
    public async Task<BudgetSummary?> GetAccountBudgetSummaryAsync(int accountId, DateTime month, CancellationToken cancellationToken = default)
    {
        var summaries = await GetMonthlySummaryAsync(month, cancellationToken);
        return summaries.FirstOrDefault(s => s.Budget.AccountId == accountId);
    }

    private static decimal CalculateActualAmount(Budget budget, IEnumerable<Transaction> transactions)
    {
        var filtered = transactions.AsEnumerable();

        // Filter by category if specified
        if (budget.CategoryId.HasValue)
        {
            filtered = filtered.Where(t => t.CategoryId == budget.CategoryId.Value);
        }

        // Filter by account if specified
        if (budget.AccountId.HasValue)
        {
            filtered = filtered.Where(t => t.AccountId == budget.AccountId.Value);
        }

        // Filter by envelope if specified
        if (budget.EnvelopeId.HasValue)
        {
            filtered = filtered.Where(t => t.EnvelopeId == budget.EnvelopeId.Value);
        }

        // Sum the amounts (expenses are negative)
        return filtered.Sum(t => t.Amount);
    }
}
