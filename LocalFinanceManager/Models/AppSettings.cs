namespace LocalFinanceManager.Models;

/// <summary>
/// Application settings stored in database for runtime configuration.
/// User-scoped entity - one active record per user.
/// </summary>
public class AppSettings
    : BaseEntity
{
    /// <summary>
    /// Auto-apply enabled flag.
    /// </summary>
    public bool AutoApplyEnabled { get; set; }

    /// <summary>
    /// Minimum confidence threshold for auto-apply (0.0-1.0).
    /// </summary>
    public float MinimumConfidence { get; set; } = 0.85f;

    /// <summary>
    /// Auto-apply interval in minutes.
    /// </summary>
    public int IntervalMinutes { get; set; } = 15;

    /// <summary>
    /// JSON serialized list of account IDs (empty = all accounts).
    /// </summary>
    public string? AccountIdsJson { get; set; }

    /// <summary>
    /// JSON serialized list of excluded category IDs.
    /// </summary>
    public string? ExcludedCategoryIdsJson { get; set; }

    /// <summary>
    /// Updated by user identifier (for audit trail).
    /// </summary>
    public string? UpdatedBy { get; set; }
}
