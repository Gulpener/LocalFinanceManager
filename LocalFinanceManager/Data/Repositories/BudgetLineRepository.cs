using Microsoft.EntityFrameworkCore;
using LocalFinanceManager.Models;
using LocalFinanceManager.Services;

namespace LocalFinanceManager.Data.Repositories;

/// <summary>
/// Repository implementation for BudgetLine entities.
/// BudgetLines are owned indirectly through BudgetPlan → Account → User.
/// </summary>
public class BudgetLineRepository : Repository<BudgetLine>, IBudgetLineRepository
{
    private readonly IUserContext _userContext;

    public BudgetLineRepository(AppDbContext context, ILogger<Repository<BudgetLine>> logger, IUserContext userContext)
        : base(context, logger)
    {
        _userContext = userContext;
    }

    public override async Task<BudgetLine?> GetByIdAsync(Guid id)
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return null;
        }

        var query = _dbSet.Where(bl => bl.Id == id && !bl.IsArchived
            && (bl.BudgetPlan.UserId == userId
                || bl.BudgetPlan.Shares.Any(s => !s.IsArchived && s.SharedWithUserId == userId && s.Status == ShareStatus.Accepted)));

        return await query
            .Include(bl => bl.BudgetPlan)
                .ThenInclude(bp => bp.Shares)
            .Include(bl => bl.Category)
            .FirstOrDefaultAsync();
    }

    public async Task<List<BudgetLine>> GetByBudgetPlanIdAsync(Guid budgetPlanId)
    {
        _logger.LogInformation("Loading budget lines for BudgetPlanId: {BudgetPlanId}", budgetPlanId);

        var userId = _userContext.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return new List<BudgetLine>();
        }

        var query = _dbSet.Where(bl => !bl.IsArchived && bl.BudgetPlanId == budgetPlanId && bl.BudgetPlan.UserId == userId);

        var budgetLines = await query
            .Include(bl => bl.Category)
            .OrderBy(bl => bl.Category.Name)
            .ToListAsync();

        _logger.LogInformation("Loaded {Count} budget lines for BudgetPlanId: {BudgetPlanId}",
            budgetLines.Count, budgetPlanId);

        return budgetLines;
    }

    public async Task<Guid?> GetAccountIdForBudgetLineAsync(Guid budgetLineId)
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return null;
        }

        var query = _dbSet.Where(bl => !bl.IsArchived && bl.Id == budgetLineId && bl.BudgetPlan.UserId == userId);

        return await query.Select(bl => bl.BudgetPlan.AccountId).FirstOrDefaultAsync();
    }

    public async Task<Dictionary<Guid, Guid>> GetAccountMappingsAsync(IEnumerable<Guid> budgetLineIds)
    {
        var budgetLineIdList = budgetLineIds.ToList();
        var userId = _userContext.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return new Dictionary<Guid, Guid>();
        }

        var query = _dbSet.Where(bl => !bl.IsArchived && budgetLineIdList.Contains(bl.Id) && bl.BudgetPlan.UserId == userId);

        return await query
            .Select(bl => new { bl.Id, bl.BudgetPlan.AccountId })
            .ToDictionaryAsync(x => x.Id, x => x.AccountId);
    }
}
