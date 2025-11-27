namespace LocalFinanceManager.Domain.Entities;

/// <summary>
/// Represents an auto-categorization rule.
/// </summary>
public class Rule
{
    /// <summary>
    /// Unique identifier for the rule.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Type of match (e.g., "contains", "regex", "iban").
    /// </summary>
    public string MatchType { get; set; } = string.Empty;

    /// <summary>
    /// Pattern to match against (text pattern or regex).
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Target category to assign when rule matches.
    /// </summary>
    public int? TargetCategoryId { get; set; }

    /// <summary>
    /// Target envelope to assign when rule matches.
    /// </summary>
    public int? TargetEnvelopeId { get; set; }

    /// <summary>
    /// Labels to add when rule matches.
    /// </summary>
    public List<string> AddLabels { get; set; } = new();

    /// <summary>
    /// Priority of the rule (higher priority evaluated first).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Navigation property to the target category.
    /// </summary>
    public Category? TargetCategory { get; set; }

    /// <summary>
    /// Navigation property to the target envelope.
    /// </summary>
    public Envelope? TargetEnvelope { get; set; }
}
