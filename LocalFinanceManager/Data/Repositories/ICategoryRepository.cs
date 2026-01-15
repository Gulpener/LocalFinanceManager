using LocalFinanceManager.Models;

namespace LocalFinanceManager.Data.Repositories;

/// <summary>
/// Repository interface for Category entities.
/// Provides specialized queries beyond generic repository operations.
/// </summary>
public interface ICategoryRepository : IRepository<Category>
{
    /// <summary>
    /// Gets a category by name, excluding archived categories.
    /// </summary>
    Task<Category?> GetByNameAsync(string name);
}
