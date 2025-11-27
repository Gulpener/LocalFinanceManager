using LocalFinanceManager.Domain.Entities;

namespace LocalFinanceManager.Application.Interfaces;

/// <summary>
/// Repository interface for Transaction entity operations.
/// </summary>
public interface ITransactionRepository
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
    /// Gets transactions by category.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of transactions in the category.</returns>
    Task<IReadOnlyList<Transaction>> GetByCategoryIdAsync(int categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new transaction.
    /// </summary>
    /// <param name="transaction">The transaction to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added transaction with generated ID.</returns>
    Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple transactions.
    /// </summary>
    /// <param name="transactions">The transactions to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added transactions with generated IDs.</returns>
    Task<IReadOnlyList<Transaction>> AddRangeAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default);

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
    /// Checks if a transaction with the same hash already exists (for deduplication).
    /// </summary>
    /// <param name="date">Transaction date.</param>
    /// <param name="amount">Transaction amount.</param>
    /// <param name="description">Transaction description.</param>
    /// <param name="accountId">Account ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if duplicate exists; otherwise false.</returns>
    Task<bool> ExistsByHashAsync(DateTime date, decimal amount, string description, int accountId, CancellationToken cancellationToken = default);
}
