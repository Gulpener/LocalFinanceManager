using LocalFinanceManager.Domain.Entities;

namespace LocalFinanceManager.Application.Interfaces;

/// <summary>
/// Repository interface for CategoryLearningProfile entity operations.
/// </summary>
public interface ICategoryLearningProfileRepository
{
    /// <summary>
    /// Gets a learning profile by category ID.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The profile if found; otherwise null.</returns>
    Task<CategoryLearningProfile?> GetByCategoryIdAsync(int categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all learning profiles.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all profiles.</returns>
    Task<IReadOnlyList<CategoryLearningProfile>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new learning profile.
    /// </summary>
    /// <param name="profile">The profile to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added profile with generated ID.</returns>
    Task<CategoryLearningProfile> AddAsync(CategoryLearningProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing learning profile.
    /// </summary>
    /// <param name="profile">The profile to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(CategoryLearningProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or creates a learning profile for a category.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The existing or newly created profile.</returns>
    Task<CategoryLearningProfile> GetOrCreateAsync(int categoryId, CancellationToken cancellationToken = default);
}
