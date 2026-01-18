using Microsoft.EntityFrameworkCore;
using LocalFinanceManager.Models;

namespace LocalFinanceManager.Data.Repositories;

/// <summary>
/// Repository implementation for Category entities.
/// </summary>
public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(AppDbContext context, ILogger<Repository<Category>> logger)
        : base(context, logger)
    {
    }

    public async Task<List<Category>> GetByBudgetPlanAsync(Guid budgetPlanId)
    {
        return await _dbSet
            .Where(c => !c.IsArchived && c.BudgetPlanId == budgetPlanId)
            .ToListAsync();
    }

    public async Task<Category?> GetByNameAsync(Guid budgetPlanId, string name)
    {
        return await _dbSet
            .Where(c => !c.IsArchived && c.BudgetPlanId == budgetPlanId && c.Name == name)
            .FirstOrDefaultAsync();
    }
}
