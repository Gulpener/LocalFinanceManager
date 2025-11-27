using LocalFinanceManager.Domain.Entities;

namespace LocalFinanceManager.Application.Interfaces;

/// <summary>
/// Repository interface for Account entity operations.
/// </summary>
public interface IAccountRepository
{
    /// <summary>
    /// Gets an account by its ID.
    /// </summary>
    /// <param name="id">The account ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The account if found; otherwise null.</returns>
    Task<Account?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all accounts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all accounts.</returns>
    Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active accounts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of active accounts.</returns>
    Task<IReadOnlyList<Account>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new account.
    /// </summary>
    /// <param name="account">The account to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added account with generated ID.</returns>
    Task<Account> AddAsync(Account account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing account.
    /// </summary>
    /// <param name="account">The account to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(Account account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an account by ID.
    /// </summary>
    /// <param name="id">The account ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted; false if not found.</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
