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

    public async Task<Category?> GetByNameAsync(string name)
    {
        return await _dbSet
            .Where(c => !c.IsArchived && c.Name == name)
            .FirstOrDefaultAsync();
    }
}
