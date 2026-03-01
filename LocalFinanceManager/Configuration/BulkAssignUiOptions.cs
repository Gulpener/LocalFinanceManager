namespace LocalFinanceManager.Configuration;

/// <summary>
/// UI timing options for bulk assignment modal behavior.
/// Configured via appsettings.json BulkAssignUiOptions section.
/// </summary>
public class BulkAssignUiOptions
{
    /// <summary>
    /// Optional minimum time to keep the processing state visible.
    /// Only applied when ApplyMinimumVisibleDurationOnlyInDevelopment is false,
    /// or when running in Development environment.
    /// Default: 0 (disabled in production by default).
    /// </summary>
    public int MinimumProcessingVisibleMilliseconds { get; set; } = 0;

    /// <summary>
    /// Keep the "completed" state briefly visible after progress reaches 100%.
    /// Default: 150 milliseconds.
    /// </summary>
    public int CompletionMessageVisibleMilliseconds { get; set; } = 150;

    /// <summary>
    /// When true, MinimumProcessingVisibleMilliseconds is only enforced in Development.
    /// Default: true.
    /// </summary>
    public bool ApplyMinimumVisibleDurationOnlyInDevelopment { get; set; } = true;
}