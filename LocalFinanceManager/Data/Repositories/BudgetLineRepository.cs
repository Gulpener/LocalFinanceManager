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

    public async Task<Guid?> GetAccountIdForBudgetLineAsync(Guid budgetLineId)
    {
        return await _dbSet
            .Where(bl => !bl.IsArchived && bl.Id == budgetLineId)
            .Select(bl => bl.BudgetPlan.AccountId)
            .FirstOrDefaultAsync();
    }

    public async Task<Dictionary<Guid, Guid>> GetAccountMappingsAsync(IEnumerable<Guid> budgetLineIds)
    {
        var budgetLineIdList = budgetLineIds.ToList();

        return await _dbSet
            .Where(bl => !bl.IsArchived && budgetLineIdList.Contains(bl.Id))
            .Select(bl => new { bl.Id, bl.BudgetPlan.AccountId })
            .ToDictionaryAsync(x => x.Id, x => x.AccountId);
    }
}
