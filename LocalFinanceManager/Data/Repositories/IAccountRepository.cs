using LocalFinanceManager.Models;

namespace LocalFinanceManager.Data.Repositories;

/// <summary>
/// Repository interface for Account-specific operations.
/// </summary>
public interface IAccountRepository : IRepository<Account>
{
    /// <summary>
    /// Get all active (non-archived) accounts.
    /// </summary>
    Task<List<Account>> GetAllActiveAsync();

    /// <summary>
    /// Get an account by ID that is readable by the current user (owned or accepted shared).
    /// Use for read-only operations. For write operations, use GetByIdAsync (owner-only).
    /// </summary>
    Task<Account?> GetReadableByIdAsync(Guid id);

    /// <summary>
    /// Check if an account with the given label already exists.
    /// </summary>
    Task<bool> LabelExistsAsync(string label, Guid? excludeId = null);

    /// <summary>
    /// Returns the count of active (non-archived) accounts accessible by the current user (owned or accepted shared).
    /// </summary>
    Task<int> CountActiveAsync();
}
