using LocalFinanceManager.Domain.Entities;

namespace LocalFinanceManager.Application.Interfaces;

/// <summary>
/// Service interface for transaction operations.
/// </summary>
public interface ITransactionService
{
    /// <summary>
    /// Gets a transaction by its ID.
    /// </summary>
    /// <param name="id">The transaction ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transaction if found; otherwise null.</returns>
    Task<Transaction?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all transactions for a specific account.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of transactions for the account.</returns>
    Task<IReadOnlyList<Transaction>> GetByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets transactions within a date range.
    /// </summary>
    /// <param name="startDate">Start date (inclusive).</param>
    /// <param name="endDate">End date (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of transactions within the date range.</returns>
    Task<IReadOnlyList<Transaction>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new transaction.
    /// </summary>
    /// <param name="transaction">The transaction to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added transaction with generated ID.</returns>
    Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing transaction.
    /// </summary>
    /// <param name="transaction">The transaction to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a transaction by ID.
    /// </summary>
    /// <param name="id">The transaction ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted; false if not found.</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a split part by its ID.
    /// </summary>
    Task<TransactionSplit?> GetSplitByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all split parts for a parent transaction.
    /// </summary>
    Task<IReadOnlyList<TransactionSplit>> GetSplitsByParentIdAsync(int parentTransactionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a split part.
    /// </summary>
    Task<TransactionSplit> AddSplitAsync(TransactionSplit split, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple split parts.
    /// </summary>
    Task<IReadOnlyList<TransactionSplit>> AddSplitsAsync(IEnumerable<TransactionSplit> splits, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing split part.
    /// </summary>
    Task UpdateSplitAsync(TransactionSplit split, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a split part by ID.
    /// </summary>
    Task<bool> DeleteSplitAsync(int id, CancellationToken cancellationToken = default);
}
