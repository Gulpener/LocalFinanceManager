using Microsoft.EntityFrameworkCore;
using LocalFinanceManager.Models;
using LocalFinanceManager.Services;

namespace LocalFinanceManager.Data.Repositories;

/// <summary>
/// Repository implementation for BudgetPlan entities.
/// </summary>
public class BudgetPlanRepository : Repository<BudgetPlan>, IBudgetPlanRepository
{
    private readonly IUserContext _userContext;

    public BudgetPlanRepository(AppDbContext context, ILogger<Repository<BudgetPlan>> logger, IUserContext userContext)
        : base(context, logger)
    {
        _userContext = userContext;
    }

    public async Task<List<BudgetPlan>> GetByAccountIdAsync(Guid accountId)
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return new List<BudgetPlan>();
        }

        var query = _dbSet.Where(bp => !bp.IsArchived && bp.AccountId == accountId
            && (bp.UserId == userId
                || bp.Shares.Any(s => s.SharedWithUserId == userId && s.Status == Models.ShareStatus.Accepted && !s.IsArchived)));

        return await query
            .Include(bp => bp.BudgetLines.Where(bl => !bl.IsArchived))
            .ThenInclude(bl => bl.Category)
            .Include(bp => bp.Shares)
            .OrderByDescending(bp => bp.Year)
            .ToListAsync();
    }

    public async Task<BudgetPlan?> GetByIdWithLinesAsync(Guid id)
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return null;
        }

        var query = _dbSet.Where(bp => !bp.IsArchived && bp.Id == id
            && (bp.UserId == userId
                || bp.Shares.Any(s => s.SharedWithUserId == userId && s.Status == Models.ShareStatus.Accepted && !s.IsArchived)));

        return await query
            .Include(bp => bp.BudgetLines.Where(bl => !bl.IsArchived))
            .ThenInclude(bl => bl.Category)
            .Include(bp => bp.Shares)
            .FirstOrDefaultAsync();
    }

    public async Task<BudgetPlan?> GetByAccountAndYearAsync(Guid accountId, int year)
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return null;
        }

        var query = _dbSet.Where(bp => !bp.IsArchived && bp.AccountId == accountId && bp.Year == year
            && (bp.UserId == userId
                || bp.Shares.Any(s => s.SharedWithUserId == userId && s.Status == Models.ShareStatus.Accepted && !s.IsArchived)));

        return await query
            .Include(bp => bp.BudgetLines.Where(bl => !bl.IsArchived))
            .ThenInclude(bl => bl.Category)
            .FirstOrDefaultAsync();
    }
}
