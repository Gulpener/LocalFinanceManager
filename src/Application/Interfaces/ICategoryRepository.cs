using LocalFinanceManager.Domain.Entities;

namespace LocalFinanceManager.Application.Interfaces;

/// <summary>
/// Repository interface for Category entity operations.
/// </summary>
public interface ICategoryRepository
{
    /// <summary>
    /// Gets a category by its ID.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The category if found; otherwise null.</returns>
    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all categories.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all categories.</returns>
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets root categories (those without a parent).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of root categories.</returns>
    Task<IReadOnlyList<Category>> GetRootCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets child categories of a parent category.
    /// </summary>
    /// <param name="parentId">The parent category ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of child categories.</returns>
    Task<IReadOnlyList<Category>> GetChildCategoriesAsync(int parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new category.
    /// </summary>
    /// <param name="category">The category to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added category with generated ID.</returns>
    Task<Category> AddAsync(Category category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    /// <param name="category">The category to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(Category category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a category by ID.
    /// </summary>
    /// <param name="id">The category ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted; false if not found.</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
