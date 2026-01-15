using LocalFinanceManager.DTOs;
using LocalFinanceManager.Models;

namespace LocalFinanceManager.Services.Import;

/// <summary>
/// Interface for deduplication strategies.
/// </summary>
public interface IDeduplicationStrategy
{
    /// <summary>
    /// Finds potential duplicate transactions in the database.
    /// </summary>
    Task<List<Transaction>> FindDuplicatesAsync(ParsedTransactionDto candidate);

    /// <summary>
    /// Checks if a candidate is a duplicate of an existing transaction.
    /// </summary>
    bool IsDuplicate(ParsedTransactionDto candidate, Transaction existing);
}
