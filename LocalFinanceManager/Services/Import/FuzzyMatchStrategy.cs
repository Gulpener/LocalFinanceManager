using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LocalFinanceManager.Services.Import;

/// <summary>
/// Fuzzy match deduplication strategy.
/// Uses Jaccard similarity on description + counterparty matching + amount matching.
/// </summary>
public class FuzzyMatchStrategy : IDeduplicationStrategy
{
    private readonly ITransactionRepository _repository;
    private readonly ILogger<FuzzyMatchStrategy> _logger;
    private readonly decimal _threshold;

    public FuzzyMatchStrategy(
        ITransactionRepository repository,
        ILogger<FuzzyMatchStrategy> logger,
        IOptions<ImportOptions> options)
    {
        _repository = repository;
        _logger = logger;
        _threshold = options.Value.FuzzyMatchThreshold;
    }

    public async Task<List<Transaction>> FindDuplicatesAsync(ParsedTransactionDto candidate)
    {
        // Find candidates with same amount within date tolerance
        var candidates = await _repository.FindFuzzyMatchCandidatesAsync(
            candidate.Date,
            candidate.Amount,
            daysTolerance: 2
        );

        var matches = new List<Transaction>();

        foreach (var existing in candidates)
        {
            if (IsDuplicate(candidate, existing))
            {
                matches.Add(existing);
            }
        }

        if (matches.Any())
        {
            _logger.LogDebug("Found {Count} fuzzy matches for transaction on {Date}",
                matches.Count, candidate.Date);
        }

        return matches;
    }

    public bool IsDuplicate(ParsedTransactionDto candidate, Transaction existing)
    {
        // Amount must match exactly
        if (existing.Amount != candidate.Amount)
        {
            return false;
        }

        // Date must be within 2 days
        var daysDifference = Math.Abs((existing.Date.Date - candidate.Date.Date).TotalDays);
        if (daysDifference > 2)
        {
            return false;
        }

        // Calculate description similarity
        var descriptionSimilarity = CalculateJaccardSimilarity(
            candidate.Description ?? "",
            existing.Description ?? ""
        );

        // Calculate counterparty similarity (if both present)
        var counterpartySimilarity = 0.0;
        if (!string.IsNullOrEmpty(candidate.Counterparty) && !string.IsNullOrEmpty(existing.Counterparty))
        {
            counterpartySimilarity = CalculateJaccardSimilarity(
                candidate.Counterparty,
                existing.Counterparty ?? ""
            );
        }

        // Combined similarity score
        // Weight: 70% description + 30% counterparty (if available)
        double similarity;
        if (counterpartySimilarity > 0)
        {
            similarity = (0.7 * descriptionSimilarity) + (0.3 * counterpartySimilarity);
        }
        else
        {
            similarity = descriptionSimilarity;
        }

        var isDuplicate = similarity >= (double)_threshold;

        if (isDuplicate)
        {
            _logger.LogDebug("Fuzzy match found with similarity {Similarity:F2} (threshold: {Threshold})",
                similarity, _threshold);
        }

        return isDuplicate;
    }

    /// <summary>
    /// Calculates Jaccard similarity between two strings.
    /// Returns a value between 0 (completely different) and 1 (identical).
    /// </summary>
    private double CalculateJaccardSimilarity(string text1, string text2)
    {
        if (string.IsNullOrWhiteSpace(text1) || string.IsNullOrWhiteSpace(text2))
        {
            return 0.0;
        }

        // Tokenize and normalize
        var tokens1 = Tokenize(text1);
        var tokens2 = Tokenize(text2);

        if (tokens1.Count == 0 || tokens2.Count == 0)
        {
            return 0.0;
        }

        // Calculate Jaccard similarity: |intersection| / |union|
        var intersection = tokens1.Intersect(tokens2).Count();
        var union = tokens1.Union(tokens2).Count();

        return union > 0 ? (double)intersection / union : 0.0;
    }

    /// <summary>
    /// Tokenizes text into normalized words.
    /// </summary>
    private HashSet<string> Tokenize(string text)
    {
        // Convert to lowercase, split by whitespace and punctuation
        var tokens = text.ToLowerInvariant()
            .Split(new[] { ' ', ',', '.', ':', ';', '-', '_', '/', '\\', '(', ')', '[', ']' },
                StringSplitOptions.RemoveEmptyEntries)
            .Where(token => token.Length > 2) // Filter out very short tokens
            .Select(token => token.Trim())
            .Where(token => !string.IsNullOrEmpty(token));

        return new HashSet<string>(tokens);
    }
}
