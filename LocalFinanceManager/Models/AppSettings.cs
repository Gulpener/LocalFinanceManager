namespace LocalFinanceManager.Models;

/// <summary>
/// Application settings stored in database for runtime configuration.
/// Singleton entity - only one record should exist.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Single instance ID (always 1).
    /// </summary>
    public int Id { get; set; } = 1;

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
    /// Last updated timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Updated by user identifier (for audit trail).
    /// </summary>
    public string? UpdatedBy { get; set; }
}
