namespace LocalFinanceManager.Configuration;

/// <summary>
/// Configuration options for automated ML retraining and auto-apply functionality.
/// Configured via appsettings.json AutomationOptions section.
/// </summary>
public class AutomationOptions
{
    /// <summary>
    /// Minimum confidence threshold for auto-applying category suggestions.
    /// Default: 0.85 (85% confidence required).
    /// </summary>
    public decimal ConfidenceThreshold { get; set; } = 0.85m;

    /// <summary>
    /// Cron expression for scheduled retraining job.
    /// Default: "0 2 * * 0" (Sunday 2 AM UTC weekly).
    /// Format: "minute hour day-of-month month day-of-week"
    /// </summary>
    public string RetrainingScheduleCron { get; set; } = "0 2 * * 0";

    /// <summary>
    /// Cron expression for scheduled auto-apply job.
    /// Default: "0 6 * * *" (daily 6 AM UTC).
    /// </summary>
    public string AutoApplyScheduleCron { get; set; } = "0 6 * * *";

    /// <summary>
    /// Number of days within which auto-applied assignments can be undone.
    /// Default: 30 days.
    /// </summary>
    public int UndoRetentionDays { get; set; } = 30;

    /// <summary>
    /// Undo rate threshold that triggers quality warning alerts.
    /// Default: 0.20 (20% undo rate in 7-day window).
    /// </summary>
    public decimal UndoRateAlertThreshold { get; set; } = 0.20m;

    /// <summary>
    /// Maximum number of retries for auto-apply operations with exponential backoff.
    /// Default: 5 retries (1s, 2s, 4s, 8s, 16s).
    /// </summary>
    public int MaxRetries { get; set; } = 5;

    /// <summary>
    /// Enable or disable auto-apply functionality globally.
    /// Default: false (disabled for safety).
    /// </summary>
    public bool AutoApplyEnabled { get; set; } = false;

    /// <summary>
    /// Number of transactions to process in each auto-apply batch.
    /// Default: 100 transactions per batch.
    /// </summary>
    public int BatchSize { get; set; } = 100;
}
