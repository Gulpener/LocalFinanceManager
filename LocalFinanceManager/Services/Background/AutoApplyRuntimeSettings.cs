namespace LocalFinanceManager.Services.Background;

/// <summary>
/// Runtime auto-apply settings used by background processing.
/// Loaded from persisted AppSettings with configuration fallback.
/// </summary>
public sealed class AutoApplyRuntimeSettings
{
    public bool Enabled { get; init; }

    public float MinimumConfidence { get; init; }

    public int IntervalMinutes { get; init; }

    public List<Guid> AccountIds { get; init; } = new();

    public HashSet<Guid> ExcludedCategoryIds { get; init; } = new();
}
