using Microsoft.EntityFrameworkCore;
using LocalFinanceManager.Models;

namespace LocalFinanceManager.Data.Repositories;

/// <summary>
/// Repository implementation for BudgetLine entities.
/// </summary>
public class BudgetLineRepository : Repository<BudgetLine>, IBudgetLineRepository
{
    public BudgetLineRepository(AppDbContext context, ILogger<Repository<BudgetLine>> logger)
        : base(context, logger)
    {
    }

    public async Task<List<BudgetLine>> GetByBudgetPlanIdAsync(Guid budgetPlanId)
    {
        return await _dbSet
            .Where(bl => !bl.IsArchived && bl.BudgetPlanId == budgetPlanId)
            .Include(bl => bl.Category)
            .OrderBy(bl => bl.Category.Name)
            .ToListAsync();
    }
}
