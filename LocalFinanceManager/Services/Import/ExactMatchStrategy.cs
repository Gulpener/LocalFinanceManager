using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.Models;
using Microsoft.Extensions.Logging;

namespace LocalFinanceManager.Services.Import;

/// <summary>
/// Exact match deduplication strategy.
/// Matches on Date + Amount + ExternalId.
/// </summary>
public class ExactMatchStrategy : IDeduplicationStrategy
{
    private readonly ITransactionRepository _repository;
    private readonly ILogger<ExactMatchStrategy> _logger;

    public ExactMatchStrategy(ITransactionRepository repository, ILogger<ExactMatchStrategy> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<Transaction>> FindDuplicatesAsync(ParsedTransactionDto candidate)
    {
        var matches = await _repository.FindExactMatchesAsync(
            candidate.Date,
            candidate.Amount,
            candidate.ExternalId
        );

        if (matches.Any())
        {
            _logger.LogDebug("Found {Count} exact matches for transaction on {Date} with amount {Amount}",
                matches.Count, candidate.Date, candidate.Amount);
        }

        return matches;
    }

    public bool IsDuplicate(ParsedTransactionDto candidate, Transaction existing)
    {
        // Exact match on date and amount
        if (existing.Date.Date != candidate.Date.Date || existing.Amount != candidate.Amount)
        {
            return false;
        }

        // If both have ExternalId, they must match
        if (!string.IsNullOrEmpty(candidate.ExternalId) && !string.IsNullOrEmpty(existing.ExternalId))
        {
            return candidate.ExternalId.Equals(existing.ExternalId, StringComparison.OrdinalIgnoreCase);
        }

        // If only one has ExternalId, not a duplicate (different sources)
        if (!string.IsNullOrEmpty(candidate.ExternalId) != !string.IsNullOrEmpty(existing.ExternalId))
        {
            return false;
        }

        // Both have no ExternalId - match on date and amount
        return true;
    }
}
