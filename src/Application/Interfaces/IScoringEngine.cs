using LocalFinanceManager.Domain.Entities;

namespace LocalFinanceManager.Application.Interfaces;

/// <summary>
/// Result of category scoring for a transaction.
/// </summary>
public class CategorySuggestion
{
    /// <summary>
    /// The suggested category ID.
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// Name of the category.
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score (0.0 to 1.0).
    /// </summary>
    public float Score { get; set; }

    /// <summary>
    /// Breakdown of score components.
    /// </summary>
    public ScoreBreakdown Breakdown { get; set; } = new();
}

/// <summary>
/// Breakdown of scoring components.
/// </summary>
public class ScoreBreakdown
{
    /// <summary>
    /// Score from word matching.
    /// </summary>
    public float WordScore { get; set; }

    /// <summary>
    /// Score from IBAN/counter account matching.
    /// </summary>
    public float IbanScore { get; set; }

    /// <summary>
    /// Score from amount bucket matching.
    /// </summary>
    public float AmountScore { get; set; }

    /// <summary>
    /// Score from recurrence pattern matching.
    /// </summary>
    public float RecurrenceScore { get; set; }
}

/// <summary>
/// Interface for the category scoring engine.
/// </summary>
public interface IScoringEngine
{
    /// <summary>
    /// Computes category suggestions for a transaction.
    /// </summary>
    /// <param name="transaction">The transaction to categorize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of category suggestions ordered by score.</returns>
    Task<IReadOnlyList<CategorySuggestion>> GetSuggestionsAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the best category suggestion if score exceeds threshold.
    /// </summary>
    /// <param name="transaction">The transaction to categorize.</param>
    /// <param name="uncertaintyThreshold">Minimum score threshold.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The best suggestion if above threshold; otherwise null.</returns>
    Task<CategorySuggestion?> GetBestSuggestionAsync(Transaction transaction, float uncertaintyThreshold, CancellationToken cancellationToken = default);
}
