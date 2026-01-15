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
    /// Check if an account with the given label already exists.
    /// </summary>
    Task<bool> LabelExistsAsync(string label, Guid? excludeId = null);
}
