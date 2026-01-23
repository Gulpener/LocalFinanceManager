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

    public override async Task<BudgetLine?> GetByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(bl => bl.BudgetPlan)
            .Include(bl => bl.Category)
            .FirstOrDefaultAsync(bl => bl.Id == id && !bl.IsArchived);
    }

    public async Task<List<BudgetLine>> GetByBudgetPlanIdAsync(Guid budgetPlanId)
    {
        _logger.LogInformation("Loading budget lines for BudgetPlanId: {BudgetPlanId}", budgetPlanId);

        var budgetLines = await _dbSet
            .Where(bl => !bl.IsArchived && bl.BudgetPlanId == budgetPlanId)
            .Include(bl => bl.Category)
            .OrderBy(bl => bl.Category.Name)
            .ToListAsync();

        _logger.LogInformation("Loaded {Count} budget lines for BudgetPlanId: {BudgetPlanId}",
            budgetLines.Count, budgetPlanId);

        return budgetLines;
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
