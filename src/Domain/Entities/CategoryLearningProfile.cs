namespace LocalFinanceManager.Domain.Entities;

/// <summary>
/// Stores learning profile data for score-based auto-categorization.
/// </summary>
public class CategoryLearningProfile
{
    /// <summary>
    /// Unique identifier for the profile.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the associated category.
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// Frequency of words appearing in transactions for this category.
    /// Key: word, Value: occurrence count.
    /// </summary>
    public Dictionary<string, int> WordFrequency { get; set; } = new();

    /// <summary>
    /// Frequency of IBANs/counter accounts for this category.
    /// Key: IBAN, Value: occurrence count.
    /// </summary>
    public Dictionary<string, int> IbanFrequency { get; set; } = new();

    /// <summary>
    /// Frequency of amount buckets for this category.
    /// Key: bucket range (e.g., "0-10", "10-50"), Value: occurrence count.
    /// </summary>
    public Dictionary<string, int> AmountBucketFrequency { get; set; } = new();

    /// <summary>
    /// Frequency of recurrence patterns for this category.
    /// Key: pattern (e.g., "weekly", "monthly"), Value: occurrence count.
    /// </summary>
    public Dictionary<string, int> RecurrenceFrequency { get; set; } = new();

    /// <summary>
    /// Navigation property to the associated category.
    /// </summary>
    public Category Category { get; set; } = null!;
}
