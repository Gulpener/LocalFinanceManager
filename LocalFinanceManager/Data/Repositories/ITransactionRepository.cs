using LocalFinanceManager.Models;

namespace LocalFinanceManager.Data.Repositories;

/// <summary>
/// Repository interface for Transaction entity operations.
/// </summary>
public interface ITransactionRepository : IRepository<Transaction>
{
    /// <summary>
    /// Gets all transactions for a specific account, excluding archived transactions.
    /// </summary>
    Task<List<Transaction>> GetByAccountIdAsync(Guid accountId);

    /// <summary>
    /// Finds potential duplicate transactions using exact match strategy.
    /// Matches on Date, Amount, and ExternalId.
    /// </summary>
    Task<List<Transaction>> FindExactMatchesAsync(DateTime date, decimal amount, string? externalId);

    /// <summary>
    /// Finds potential duplicate transactions for fuzzy matching.
    /// Returns transactions with matching amount within date range.
    /// </summary>
    Task<List<Transaction>> FindFuzzyMatchCandidatesAsync(DateTime date, decimal amount, int daysTolerance = 2);

    /// <summary>
    /// Gets all transactions in a specific import batch.
    /// </summary>
    Task<List<Transaction>> GetByImportBatchIdAsync(Guid importBatchId);

    /// <summary>
    /// Adds multiple transactions in a single batch operation.
    /// </summary>
    Task AddRangeAsync(IEnumerable<Transaction> transactions);

    /// <summary>
    /// Gets a transaction by ID with Account eager-loaded.
    /// Used for validation that requires account information.
    /// </summary>
    Task<Transaction?> GetByIdWithAccountAsync(Guid id);

    /// <summary>
    /// Gets all transactions with their splits and related budget line/category data.
    /// </summary>
    Task<List<Transaction>> GetAllWithSplitsAsync();

    /// <summary>
    /// Gets transactions for a specific account with their splits and related data.
    /// </summary>
    Task<List<Transaction>> GetByAccountIdWithSplitsAsync(Guid accountId);
}
