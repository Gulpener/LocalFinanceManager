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
    /// Get an account by ID where the current user is the owner (for write operations).
    /// </summary>
    Task<Account?> GetOwnedByIdAsync(Guid id);

    /// <summary>
    /// Check if an account with the given label already exists.
    /// </summary>
    Task<bool> LabelExistsAsync(string label, Guid? excludeId = null);
}
