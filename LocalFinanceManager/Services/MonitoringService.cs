using LocalFinanceManager.Configuration;
using LocalFinanceManager.Data;
using LocalFinanceManager.DTOs.ML;
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

    /// <summary>
    /// Gets auto-apply history.
    /// </summary>
    Task<List<AutoApplyHistoryDto>> GetAutoApplyHistoryAsync(int limit = 50);

    /// <summary>
    /// Estimates number of auto-applies based on confidence threshold.
    /// </summary>
    Task<int> EstimateAutoApplyCountAsync(float confidenceThreshold);
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
            UndoCount = totalUndone,
            UndoRate = undoRate,
            AverageConfidence = (decimal)avgConfidence,
            IsUndoRateAboveThreshold = undoRate > _options.UndoRateAlertThreshold,
            LastRunTimestamp = autoAppliedAudits.Any() 
                ? autoAppliedAudits.Max(a => a.AutoAppliedAt ?? a.ChangedAt) 
                : null
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

    public async Task<List<AutoApplyHistoryDto>> GetAutoApplyHistoryAsync(int limit = 50)
    {
        var autoAppliedAudits = await _dbContext.TransactionAudits
            .Where(a => !a.IsArchived)
            .Where(a => a.IsAutoApplied)
            .Include(a => a.Transaction)
            .OrderByDescending(a => a.AutoAppliedAt)
            .Take(limit)
            .ToListAsync();

        var history = new List<AutoApplyHistoryDto>();

        foreach (var audit in autoAppliedAudits)
        {
            if (audit.Transaction == null) continue;

            // Check if this auto-apply was undone
            var wasUndone = await _dbContext.TransactionAudits
                .AnyAsync(a => !a.IsArchived
                    && a.TransactionId == audit.TransactionId
                    && a.ActionType == "Undo"
                    && a.ChangedAt > audit.AutoAppliedAt);

            // Check if can undo (within retention window and not already undone)
            var retentionCutoff = DateTime.UtcNow.AddDays(-_options.UndoRetentionDays);
            var canUndo = !wasUndone
                && audit.AutoAppliedAt.HasValue
                && audit.AutoAppliedAt.Value >= retentionCutoff;

            // Get category name - would need to be stored in a separate field or retrieved from transaction
            var categoryName = "Category"; // Placeholder - in production, would get from related category

            history.Add(new AutoApplyHistoryDto
            {
                TransactionId = audit.TransactionId,
                Description = audit.Transaction.Description,
                Amount = audit.Transaction.Amount,
                CategoryName = categoryName,
                ConfidenceScore = audit.Confidence ?? 0.0f,
                AutoAppliedAt = audit.AutoAppliedAt ?? audit.ChangedAt,
                Status = wasUndone ? "Undone" : "Accepted",
                CanUndo = canUndo
            });
        }

        return history;
    }

    public async Task<int> EstimateAutoApplyCountAsync(float confidenceThreshold)
    {
        // Estimate based on last 100 unassigned transactions
        // This is a simplified estimation - in production, would use ML service to get actual predictions
        
        var recentUnassignedCount = await _dbContext.Transactions
            .Where(t => !t.IsArchived)
            .Where(t => t.AssignedParts == null || !t.AssignedParts.Any())
            .Take(100)
            .CountAsync();

        // Rough estimation: assume 60% of unassigned have suggestions above threshold
        var estimationFactor = confidenceThreshold switch
        {
            >= 0.9f => 0.3m,
            >= 0.8f => 0.5m,
            >= 0.7f => 0.65m,
            >= 0.6f => 0.75m,
            _ => 0.8m
        };

        return (int)(recentUnassignedCount * estimationFactor);
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
    public int UndoCount { get; set; }
    public decimal UndoRate { get; set; }
    public decimal AverageConfidence { get; set; }
    public bool IsUndoRateAboveThreshold { get; set; }
    public DateTime? LastRunTimestamp { get; set; }
}
