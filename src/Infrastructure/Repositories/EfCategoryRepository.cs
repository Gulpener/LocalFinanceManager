using LocalFinanceManager.Application.Interfaces;
using LocalFinanceManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation of the ICategoryRepository interface.
/// </summary>
public class EfCategoryRepository : ICategoryRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCategoryRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public EfCategoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .Include(c => c.ParentCategory)
            .Include(c => c.ChildCategories)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .Include(c => c.ChildCategories)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Category>> GetRootCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .Include(c => c.ChildCategories)
            .Where(c => c.ParentCategoryId == null)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Category>> GetChildCategoriesAsync(int parentId, CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .Include(c => c.ChildCategories)
            .Where(c => c.ParentCategoryId == parentId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Category> AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);
        return category;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        _context.Categories.Update(category);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _context.Categories.FindAsync([id], cancellationToken);
        if (category == null)
        {
            return false;
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
