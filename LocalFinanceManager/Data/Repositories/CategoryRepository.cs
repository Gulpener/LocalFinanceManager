using Microsoft.EntityFrameworkCore;
using LocalFinanceManager.Models;
using LocalFinanceManager.Services;

namespace LocalFinanceManager.Data.Repositories;

/// <summary>
/// Repository implementation for Category entities.
/// </summary>
public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    private readonly IUserContext _userContext;

    public CategoryRepository(AppDbContext context, ILogger<Repository<Category>> logger, IUserContext userContext)
        : base(context, logger)
    {
        _userContext = userContext;
    }

    public async Task<List<Category>> GetByBudgetPlanAsync(Guid budgetPlanId)
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return new List<Category>();
        }

        var query = _dbSet.Where(c => !c.IsArchived && c.BudgetPlanId == budgetPlanId && c.UserId == userId);

        return await query.ToListAsync();
    }

    public async Task<Category?> GetByNameAsync(Guid budgetPlanId, string name)
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return null;
        }

        var query = _dbSet.Where(c => !c.IsArchived && c.BudgetPlanId == budgetPlanId && c.Name == name && c.UserId == userId);

        return await query.FirstOrDefaultAsync();
    }
}
