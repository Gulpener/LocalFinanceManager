namespace LocalFinanceManager.Models;

/// <summary>
/// Represents a labeled transaction used for ML model training.
/// Stores user corrections and assignments as training data.
/// </summary>
public class LabeledExample : BaseEntity
{
    /// <summary>
    /// Foreign key to the Transaction.
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Navigation property to Transaction.
    /// </summary>
    public Transaction? Transaction { get; set; }

    /// <summary>
    /// Foreign key to the Category assigned by the user.
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Navigation property to Category.
    /// </summary>
    public Category? Category { get; set; }

    /// <summary>
    /// User identifier who made the assignment (for multi-user support later).
    /// Nullable for now (single-user mode).
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Indicates whether this label was auto-applied by ML or manually assigned.
    /// Used for tracking acceptance rate of ML suggestions.
    /// </summary>
    public bool WasAutoApplied { get; set; }

    /// <summary>
    /// Indicates whether the user accepted the ML suggestion (true) or overrode it (false).
    /// Null if no suggestion was provided (manual assignment).
    /// </summary>
    public bool? AcceptedSuggestion { get; set; }

    /// <summary>
    /// The confidence score of the ML suggestion if one was provided.
    /// Null if no suggestion was provided.
    /// </summary>
    public float? SuggestionConfidence { get; set; }

    /// <summary>
    /// Model version that generated the suggestion (if applicable).
    /// </summary>
    public int? ModelVersion { get; set; }
}
