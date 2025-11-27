using System.Text.RegularExpressions;
using LocalFinanceManager.Application.Interfaces;
using LocalFinanceManager.Domain.Entities;

namespace LocalFinanceManager.Application.Services;

/// <summary>
/// Service for updating category learning profiles based on user actions.
/// </summary>
public partial class LearningService : ILearningService
{
    private readonly ICategoryLearningProfileRepository _profileRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="LearningService"/> class.
    /// </summary>
    /// <param name="profileRepository">The profile repository.</param>
    public LearningService(ICategoryLearningProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    /// <inheritdoc />
    public async Task LearnFromTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        var profile = await _profileRepository.GetOrCreateAsync(transaction.CategoryId, cancellationToken);
        UpdateProfile(profile, transaction, 1);
        await _profileRepository.UpdateAsync(profile, cancellationToken);
    }

    /// <inheritdoc />
    public async Task LearnFromCorrectionAsync(Transaction transaction, int oldCategoryId, CancellationToken cancellationToken = default)
    {
        // Decrease weight in old category
        var oldProfile = await _profileRepository.GetByCategoryIdAsync(oldCategoryId, cancellationToken);
        if (oldProfile != null)
        {
            UpdateProfile(oldProfile, transaction, -1);
            await _profileRepository.UpdateAsync(oldProfile, cancellationToken);
        }

        // Increase weight in new category
        var newProfile = await _profileRepository.GetOrCreateAsync(transaction.CategoryId, cancellationToken);
        UpdateProfile(newProfile, transaction, 2); // Double weight for corrections
        await _profileRepository.UpdateAsync(newProfile, cancellationToken);
    }

    private static void UpdateProfile(CategoryLearningProfile profile, Transaction transaction, int delta)
    {
        // Update word frequencies
        var words = ExtractWords(transaction.Description?.ToLowerInvariant() ?? string.Empty);
        foreach (var word in words)
        {
            if (profile.WordFrequency.TryGetValue(word, out var currentWordFreq))
            {
                profile.WordFrequency[word] = Math.Max(0, currentWordFreq + delta);
            }
            else if (delta > 0)
            {
                profile.WordFrequency[word] = delta;
            }
        }

        // Update IBAN frequency
        if (!string.IsNullOrWhiteSpace(transaction.CounterAccount))
        {
            var normalizedIban = transaction.CounterAccount.ToUpperInvariant().Replace(" ", "");
            if (profile.IbanFrequency.TryGetValue(normalizedIban, out var currentIbanFreq))
            {
                profile.IbanFrequency[normalizedIban] = Math.Max(0, currentIbanFreq + delta);
            }
            else if (delta > 0)
            {
                profile.IbanFrequency[normalizedIban] = delta;
            }
        }

        // Update amount bucket frequency
        var bucket = ScoringEngine.GetAmountBucket(transaction.Amount);
        if (profile.AmountBucketFrequency.TryGetValue(bucket, out var currentBucketFreq))
        {
            profile.AmountBucketFrequency[bucket] = Math.Max(0, currentBucketFreq + delta);
        }
        else if (delta > 0)
        {
            profile.AmountBucketFrequency[bucket] = delta;
        }
    }

    private static IEnumerable<string> ExtractWords(string text)
    {
        return WordRegex().Matches(text)
            .Select(m => m.Value)
            .Where(w => w.Length >= 3);
    }

    [GeneratedRegex(@"\b[a-z]+\b", RegexOptions.Compiled)]
    private static partial Regex WordRegex();
}
