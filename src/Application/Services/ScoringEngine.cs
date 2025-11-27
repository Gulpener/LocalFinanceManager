using System.Text.RegularExpressions;
using LocalFinanceManager.Application.Interfaces;
using LocalFinanceManager.Domain.Entities;

namespace LocalFinanceManager.Application.Services;

/// <summary>
/// Scoring engine for automatic transaction categorization.
/// Uses learned patterns from word frequency, IBAN, amount buckets, and recurrence.
/// </summary>
public partial class ScoringEngine : IScoringEngine
{
    private readonly ICategoryLearningProfileRepository _profileRepository;
    private readonly ICategoryRepository _categoryRepository;

    // Weight factors for different scoring components
    private const float WordWeight = 0.4f;
    private const float IbanWeight = 0.3f;
    private const float AmountWeight = 0.2f;
    private const float RecurrenceWeight = 0.1f;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScoringEngine"/> class.
    /// </summary>
    /// <param name="profileRepository">The profile repository.</param>
    /// <param name="categoryRepository">The category repository.</param>
    public ScoringEngine(
        ICategoryLearningProfileRepository profileRepository,
        ICategoryRepository categoryRepository)
    {
        _profileRepository = profileRepository;
        _categoryRepository = categoryRepository;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategorySuggestion>> GetSuggestionsAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default)
    {
        var profiles = await _profileRepository.GetAllAsync(cancellationToken);
        var categories = await _categoryRepository.GetAllAsync(cancellationToken);
        var categoryDict = categories.ToDictionary(c => c.Id, c => c.Name);

        var suggestions = new List<CategorySuggestion>();

        foreach (var profile in profiles)
        {
            var breakdown = new ScoreBreakdown
            {
                WordScore = CalculateWordScore(transaction.Description, profile.WordFrequency),
                IbanScore = CalculateIbanScore(transaction.CounterAccount, profile.IbanFrequency),
                AmountScore = CalculateAmountScore(transaction.Amount, profile.AmountBucketFrequency),
                RecurrenceScore = 0 // Recurrence requires historical data
            };

            var totalScore =
                breakdown.WordScore * WordWeight +
                breakdown.IbanScore * IbanWeight +
                breakdown.AmountScore * AmountWeight +
                breakdown.RecurrenceScore * RecurrenceWeight;

            if (totalScore > 0)
            {
                suggestions.Add(new CategorySuggestion
                {
                    CategoryId = profile.CategoryId,
                    CategoryName = categoryDict.GetValueOrDefault(profile.CategoryId, "Unknown"),
                    Score = totalScore,
                    Breakdown = breakdown
                });
            }
        }

        // If no matches, return all categories with zero score
        if (suggestions.Count == 0)
        {
            foreach (var category in categories)
            {
                suggestions.Add(new CategorySuggestion
                {
                    CategoryId = category.Id,
                    CategoryName = category.Name,
                    Score = 0,
                    Breakdown = new ScoreBreakdown()
                });
            }
        }

        return suggestions.OrderByDescending(s => s.Score).ToList();
    }

    /// <inheritdoc />
    public async Task<CategorySuggestion?> GetBestSuggestionAsync(
        Transaction transaction,
        float uncertaintyThreshold,
        CancellationToken cancellationToken = default)
    {
        var suggestions = await GetSuggestionsAsync(transaction, cancellationToken);
        var best = suggestions.FirstOrDefault();

        if (best != null && best.Score >= uncertaintyThreshold)
        {
            return best;
        }

        return null;
    }

    private static float CalculateWordScore(string description, Dictionary<string, int> wordFrequency)
    {
        if (string.IsNullOrWhiteSpace(description) || wordFrequency.Count == 0)
            return 0;

        var words = ExtractWords(description.ToLowerInvariant());
        var totalFrequency = wordFrequency.Values.Sum();

        if (totalFrequency == 0)
            return 0;

        var matchedFrequency = words
            .Where(w => wordFrequency.ContainsKey(w))
            .Sum(w => wordFrequency[w]);

        // Normalize to 0-1 range
        return Math.Min(1.0f, (float)matchedFrequency / totalFrequency);
    }

    private static float CalculateIbanScore(string counterAccount, Dictionary<string, int> ibanFrequency)
    {
        if (string.IsNullOrWhiteSpace(counterAccount) || ibanFrequency.Count == 0)
            return 0;

        var normalizedIban = counterAccount.ToUpperInvariant().Replace(" ", "");

        if (ibanFrequency.TryGetValue(normalizedIban, out var frequency))
        {
            var totalFrequency = ibanFrequency.Values.Sum();
            return totalFrequency > 0 ? (float)frequency / totalFrequency : 0;
        }

        return 0;
    }

    private static float CalculateAmountScore(decimal amount, Dictionary<string, int> amountBucketFrequency)
    {
        if (amountBucketFrequency.Count == 0)
            return 0;

        var bucket = GetAmountBucket(Math.Abs(amount));

        if (amountBucketFrequency.TryGetValue(bucket, out var frequency))
        {
            var totalFrequency = amountBucketFrequency.Values.Sum();
            return totalFrequency > 0 ? (float)frequency / totalFrequency : 0;
        }

        return 0;
    }

    /// <summary>
    /// Gets the amount bucket for a given amount.
    /// </summary>
    public static string GetAmountBucket(decimal amount)
    {
        var absAmount = Math.Abs(amount);
        return absAmount switch
        {
            < 10 => "0-10",
            < 25 => "10-25",
            < 50 => "25-50",
            < 100 => "50-100",
            < 250 => "100-250",
            < 500 => "250-500",
            < 1000 => "500-1000",
            _ => "1000+"
        };
    }

    private static IEnumerable<string> ExtractWords(string text)
    {
        return WordRegex().Matches(text)
            .Select(m => m.Value)
            .Where(w => w.Length >= 3); // Minimum word length
    }

    [GeneratedRegex(@"\b[a-z]+\b", RegexOptions.Compiled)]
    private static partial Regex WordRegex();
}
