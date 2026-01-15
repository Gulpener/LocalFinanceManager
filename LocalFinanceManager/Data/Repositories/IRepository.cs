using LocalFinanceManager.Models;

namespace LocalFinanceManager.Data.Repositories;

/// <summary>
/// Generic repository interface for data access operations.
/// Provides CRUD operations with soft-delete filtering.
/// </summary>
/// <typeparam name="T">Entity type that inherits from BaseEntity</typeparam>
public interface IRepository<T> where T : BaseEntity
{
    /// <summary>
    /// Gets an entity by its ID, excluding archived entities.
    /// </summary>
    Task<T?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all non-archived entities.
    /// </summary>
    Task<List<T>> GetActiveAsync();

    /// <summary>
    /// Adds a new entity to the database.
    /// </summary>
    Task AddAsync(T entity);

    /// <summary>
    /// Updates an existing entity.
    /// Handles optimistic concurrency conflicts.
    /// </summary>
    Task UpdateAsync(T entity);

    /// <summary>
    /// Archives an entity (soft delete).
    /// </summary>
    Task ArchiveAsync(Guid id);

    /// <summary>
    /// Saves all pending changes to the database.
    /// </summary>
    Task<int> SaveChangesAsync();
}
