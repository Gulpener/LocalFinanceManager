using Microsoft.EntityFrameworkCore;
using LocalFinanceManager.Models;

namespace LocalFinanceManager.Data.Repositories;

/// <summary>
/// Repository implementation for BudgetPlan entities.
/// </summary>
public class BudgetPlanRepository : Repository<BudgetPlan>, IBudgetPlanRepository
{
    public BudgetPlanRepository(AppDbContext context, ILogger<Repository<BudgetPlan>> logger)
        : base(context, logger)
    {
    }

    public async Task<List<BudgetPlan>> GetByAccountIdAsync(Guid accountId)
    {
        return await _dbSet
            .Where(bp => !bp.IsArchived && bp.AccountId == accountId)
            .Include(bp => bp.BudgetLines.Where(bl => !bl.IsArchived))
            .ThenInclude(bl => bl.Category)
            .OrderByDescending(bp => bp.Year)
            .ToListAsync();
    }

    public async Task<BudgetPlan?> GetByIdWithLinesAsync(Guid id)
    {
        return await _dbSet
            .Where(bp => !bp.IsArchived && bp.Id == id)
            .Include(bp => bp.BudgetLines.Where(bl => !bl.IsArchived))
            .ThenInclude(bl => bl.Category)
            .FirstOrDefaultAsync();
    }

    public async Task<BudgetPlan?> GetByAccountAndYearAsync(Guid accountId, int year)
    {
        return await _dbSet
            .Where(bp => !bp.IsArchived && bp.AccountId == accountId && bp.Year == year)
            .Include(bp => bp.BudgetLines.Where(bl => !bl.IsArchived))
            .ThenInclude(bl => bl.Category)
            .FirstOrDefaultAsync();
    }
}
