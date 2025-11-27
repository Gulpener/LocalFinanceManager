using LocalFinanceManager.Domain.Entities;

namespace LocalFinanceManager.Application.Interfaces;

/// <summary>
/// Result of deduplication check.
/// </summary>
public class DeduplicationResult
{
    /// <summary>
    /// Transactions that are unique (not duplicates).
    /// </summary>
    public List<Transaction> UniqueTransactions { get; set; } = new();

    /// <summary>
    /// Transactions that appear to be duplicates.
    /// </summary>
    public List<DuplicateInfo> Duplicates { get; set; } = new();
}

/// <summary>
/// Information about a potential duplicate.
/// </summary>
public class DuplicateInfo
{
    /// <summary>
    /// The new transaction being imported.
    /// </summary>
    public Transaction NewTransaction { get; set; } = null!;

    /// <summary>
    /// The existing transaction that matches.
    /// </summary>
    public Transaction? ExistingTransaction { get; set; }

    /// <summary>
    /// Similarity score (0-100).
    /// </summary>
    public int SimilarityScore { get; set; }
}

/// <summary>
/// Service interface for transaction deduplication.
/// </summary>
public interface IDeduplicationService
{
    /// <summary>
    /// Checks a list of transactions for duplicates against existing data.
    /// </summary>
    /// <param name="transactions">Transactions to check.</param>
    /// <param name="accountId">Account ID to check against.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deduplication result with unique and duplicate transactions.</returns>
    Task<DeduplicationResult> CheckForDuplicatesAsync(
        IEnumerable<Transaction> transactions,
        int accountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes a hash for a transaction for duplicate detection.
    /// </summary>
    /// <param name="transaction">The transaction.</param>
    /// <returns>Hash string.</returns>
    string ComputeHash(Transaction transaction);
}
