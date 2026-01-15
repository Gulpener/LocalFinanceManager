using LocalFinanceManager.Configuration;
using LocalFinanceManager.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LocalFinanceManager.Services;

/// <summary>
/// Service for monitoring auto-apply metrics and generating alerts.
/// Tracks auto-apply rate, undo rate, confidence drift.
/// </summary>
public interface IMonitoringService
{
    /// <summary>
    /// Gets current auto-apply statistics.
    /// </summary>
    Task<AutoApplyStats> GetAutoApplyStatsAsync(int windowDays = 7);

    /// <summary>
    /// Checks if undo rate exceeds alert threshold.
    /// </summary>
    Task<bool> IsUndoRateAboveThresholdAsync(int windowDays = 7);
}

public class MonitoringService : IMonitoringService
{
    private readonly AppDbContext _dbContext;
    private readonly AutomationOptions _options;
    private readonly ILogger<MonitoringService> _logger;

    public MonitoringService(
        AppDbContext dbContext,
        IOptions<AutomationOptions> options,
        ILogger<MonitoringService> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AutoApplyStats> GetAutoApplyStatsAsync(int windowDays = 7)
    {
        var startDate = DateTime.UtcNow.AddDays(-windowDays);

        // Get auto-applied assignments
        var autoAppliedAudits = await _dbContext.TransactionAudits
            .Where(a => !a.IsArchived)
            .Where(a => a.IsAutoApplied)
            .Where(a => a.ChangedAt >= startDate)
            .ToListAsync();

        // Get undo operations for auto-applied assignments
        var undoAudits = await _dbContext.TransactionAudits
            .Where(a => !a.IsArchived)
            .Where(a => a.ActionType == "Undo")
            .Where(a => a.ChangedAt >= startDate)
            .Where(a => a.Reason != null && a.Reason.Contains("auto-applied"))
            .ToListAsync();

        var totalAutoApplied = autoAppliedAudits.Count;
        var totalUndone = undoAudits.Count;
        var undoRate = totalAutoApplied > 0 ? (decimal)totalUndone / totalAutoApplied : 0;
        var avgConfidence = autoAppliedAudits.Any() && autoAppliedAudits.Any(a => a.Confidence.HasValue)
            ? autoAppliedAudits.Where(a => a.Confidence.HasValue).Average(a => a.Confidence!.Value)
            : 0;

        var stats = new AutoApplyStats
        {
            WindowDays = windowDays,
            TotalAutoApplied = totalAutoApplied,
            TotalUndone = totalUndone,
            UndoRate = undoRate,
            AverageConfidence = (decimal)avgConfidence,
            IsUndoRateAboveThreshold = undoRate > _options.UndoRateAlertThreshold
        };

        if (stats.IsUndoRateAboveThreshold)
        {
            _logger.LogWarning(
                "Undo rate alert: {UndoRate:P2} exceeds threshold {Threshold:P2} in {WindowDays}-day window. " +
                "({Undone}/{Total} auto-applied assignments were undone)",
                undoRate,
                _options.UndoRateAlertThreshold,
                windowDays,
                totalUndone,
                totalAutoApplied);
        }

        return stats;
    }

    public async Task<bool> IsUndoRateAboveThresholdAsync(int windowDays = 7)
    {
        var stats = await GetAutoApplyStatsAsync(windowDays);
        return stats.IsUndoRateAboveThreshold;
    }
}

/// <summary>
/// Auto-apply statistics for monitoring dashboard.
/// </summary>
public class AutoApplyStats
{
    public int WindowDays { get; set; }
    public int TotalAutoApplied { get; set; }
    public int TotalUndone { get; set; }
    public decimal UndoRate { get; set; }
    public decimal AverageConfidence { get; set; }
    public bool IsUndoRateAboveThreshold { get; set; }
}
