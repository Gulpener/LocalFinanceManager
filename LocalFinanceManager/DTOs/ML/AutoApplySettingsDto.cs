namespace LocalFinanceManager.DTOs.ML;

/// <summary>
/// DTO for auto-apply configuration settings.
/// </summary>
public class AutoApplySettingsDto
{
    /// <summary>
    /// Enable/disable auto-apply feature.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Minimum confidence threshold for auto-apply (0.0 to 1.0).
    /// Recommended range: 0.6 to 0.95.
    /// </summary>
    public float MinimumConfidence { get; set; } = 0.8f;

    /// <summary>
    /// Account IDs to include in auto-apply (empty = all accounts).
    /// </summary>
    public List<Guid> AccountIds { get; set; } = new();

    /// <summary>
    /// Category IDs to exclude from auto-apply (e.g., sensitive categories).
    /// </summary>
    public List<Guid> ExcludedCategoryIds { get; set; } = new();

    /// <summary>
    /// Optional: default category for low-confidence transactions.
    /// </summary>
    public Guid? DefaultCategoryId { get; set; }

    /// <summary>
    /// Run interval in minutes (default: 15).
    /// </summary>
    public int IntervalMinutes { get; set; } = 15;
}

/// <summary>
/// DTO for auto-apply statistics.
/// </summary>
public class AutoApplyStatsDto
{
    /// <summary>
    /// Total number of auto-applied transactions in the window.
    /// </summary>
    public int TotalAutoApplied { get; set; }

    /// <summary>
    /// Number of auto-applied assignments that were accepted (not undone).
    /// </summary>
    public int AcceptedCount { get; set; }

    /// <summary>
    /// Number of auto-applied assignments that were undone.
    /// </summary>
    public int UndoCount { get; set; }

    /// <summary>
    /// Undo rate as a percentage (UndoCount / TotalAutoApplied).
    /// </summary>
    public decimal UndoRate { get; set; }

    /// <summary>
    /// Average confidence score of auto-applied assignments.
    /// </summary>
    public decimal AverageConfidence { get; set; }

    /// <summary>
    /// Timestamp of the last auto-apply run.
    /// </summary>
    public DateTime? LastRunTimestamp { get; set; }

    /// <summary>
    /// Number of days in the statistics window.
    /// </summary>
    public int WindowDays { get; set; } = 7;

    /// <summary>
    /// Is the undo rate above the alert threshold?
    /// </summary>
    public bool IsUndoRateAboveThreshold { get; set; }
}

/// <summary>
/// DTO for auto-apply alert.
/// </summary>
public class AutoApplyAlertDto
{
    /// <summary>
    /// Alert type.
    /// </summary>
    public AutoApplyAlertType AlertType { get; set; }

    /// <summary>
    /// Alert message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Alert threshold value.
    /// </summary>
    public decimal Threshold { get; set; }

    /// <summary>
    /// Current value that triggered the alert.
    /// </summary>
    public decimal CurrentValue { get; set; }

    /// <summary>
    /// Severity level.
    /// </summary>
    public AlertSeverity Severity { get; set; }
}

/// <summary>
/// Auto-apply alert types.
/// </summary>
public enum AutoApplyAlertType
{
    UndoRateHigh,
    ModelStale,
    ConfidenceDrift,
    LowAutoApplyRate
}

/// <summary>
/// Alert severity levels.
/// </summary>
public enum AlertSeverity
{
    Info,
    Warning,
    Error
}

/// <summary>
/// DTO for auto-apply history entry.
/// </summary>
public class AutoApplyHistoryDto
{
    /// <summary>
    /// Transaction ID.
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Transaction description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Transaction amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Category name assigned.
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score of the auto-applied suggestion.
    /// </summary>
    public float ConfidenceScore { get; set; }

    /// <summary>
    /// Timestamp when auto-applied.
    /// </summary>
    public DateTime AutoAppliedAt { get; set; }

    /// <summary>
    /// Status: Accepted or Undone.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Can this auto-apply be undone?
    /// </summary>
    public bool CanUndo { get; set; }
}
