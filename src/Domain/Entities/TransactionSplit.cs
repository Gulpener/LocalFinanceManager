namespace LocalFinanceManager.Domain.Entities;

/// <summary>
/// Represents a part of a split transaction.
/// </summary>
public class TransactionSplit
{
    /// <summary>
    /// Unique identifier for the split part.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the parent transaction.
    /// </summary>
    public int ParentTransactionId { get; set; }

    /// <summary>
    /// Amount for this split part. Signed decimal (positive/negative) as per transaction conventions.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Optional category assigned to this split part.
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Optional envelope assigned to this split part.
    /// </summary>
    public int? EnvelopeId { get; set; }

    /// <summary>
    /// Notes for this split part.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Navigation to the parent transaction.
    /// </summary>
    public Transaction ParentTransaction { get; set; } = null!;

    /// <summary>
    /// Navigation to the category (optional).
    /// </summary>
    public Category? Category { get; set; }

    /// <summary>
    /// Navigation to the envelope (optional).
    /// </summary>
    public Envelope? Envelope { get; set; }
}
