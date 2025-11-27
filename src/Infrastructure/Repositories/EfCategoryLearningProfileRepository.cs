using LocalFinanceManager.Application.Interfaces;
using LocalFinanceManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation of the category learning profile repository.
/// </summary>
public class EfCategoryLearningProfileRepository : ICategoryLearningProfileRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCategoryLearningProfileRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public EfCategoryLearningProfileRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<CategoryLearningProfile?> GetByCategoryIdAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.CategoryLearningProfiles
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.CategoryId == categoryId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategoryLearningProfile>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CategoryLearningProfiles
            .Include(p => p.Category)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CategoryLearningProfile> AddAsync(CategoryLearningProfile profile, CancellationToken cancellationToken = default)
    {
        _context.CategoryLearningProfiles.Add(profile);
        await _context.SaveChangesAsync(cancellationToken);
        return profile;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(CategoryLearningProfile profile, CancellationToken cancellationToken = default)
    {
        _context.CategoryLearningProfiles.Update(profile);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CategoryLearningProfile> GetOrCreateAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var profile = await GetByCategoryIdAsync(categoryId, cancellationToken);
        if (profile == null)
        {
            profile = new CategoryLearningProfile
            {
                CategoryId = categoryId
            };
            await AddAsync(profile, cancellationToken);
        }
        return profile;
    }
}
