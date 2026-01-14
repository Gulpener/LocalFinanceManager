namespace LocalFinanceManager.Models;

/// <summary>
/// Abstract base class for all domain entities.
/// Provides common properties for identity, concurrency control, and audit timestamps.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Unique identifier for the entity.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Row version for optimistic concurrency control.
    /// Automatically managed by EF Core.
    /// </summary>
    public byte[]? RowVersion { get; set; }

    /// <summary>
    /// Timestamp when the entity was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the entity was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Indicates whether the entity is archived (soft-deleted).
    /// Archived entities are filtered out from normal queries.
    /// </summary>
    public bool IsArchived { get; set; }
}
