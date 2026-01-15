namespace LocalFinanceManager.Models;

/// <summary>
/// Represents an audit log entry for transaction assignment changes.
/// Tracks who changed what, when, and provides undo capability.
/// </summary>
public class TransactionAudit : BaseEntity
{
    /// <summary>
    /// Foreign key to the Transaction that was modified.
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Navigation property to the Transaction.
    /// </summary>
    public Transaction Transaction { get; set; } = null!;

    /// <summary>
    /// Type of action performed (Assign, Split, Undo, BulkAssign).
    /// </summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// User identifier who performed the action.
    /// For MVP, can be system user; for multi-user later.
    /// </summary>
    public string ChangedBy { get; set; } = "System";

    /// <summary>
    /// Timestamp when the action was performed.
    /// </summary>
    public DateTime ChangedAt { get; set; }

    /// <summary>
    /// JSON representation of the state before the change.
    /// Enables undo functionality.
    /// </summary>
    public string? BeforeState { get; set; }

    /// <summary>
    /// JSON representation of the state after the change.
    /// Provides complete audit trail.
    /// </summary>
    public string? AfterState { get; set; }

    /// <summary>
    /// Optional reason or note for the change.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Indicates whether this assignment was automatically applied by the ML system.
    /// </summary>
    public bool IsAutoApplied { get; set; }

    /// <summary>
    /// Identifier of the automated system that applied this change.
    /// Typically "AutoApplyService" for auto-applied assignments.
    /// </summary>
    public string? AutoAppliedBy { get; set; }

    /// <summary>
    /// Timestamp when the auto-apply occurred.
    /// </summary>
    public DateTime? AutoAppliedAt { get; set; }

    /// <summary>
    /// ML model confidence score for auto-applied assignments.
    /// Range: 0.0 to 1.0
    /// </summary>
    public float? Confidence { get; set; }

    /// <summary>
    /// Version of the ML model that generated the auto-applied suggestion.
    /// </summary>
    public int? ModelVersion { get; set; }
}
