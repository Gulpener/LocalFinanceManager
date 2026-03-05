using LocalFinanceManager.Configuration;
using LocalFinanceManager.Data;
using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.ML;
using LocalFinanceManager.Models;
using LocalFinanceManager.Services.Background;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace LocalFinanceManager.Services;

/// <summary>
/// Result of a single auto-apply job execution.
/// </summary>
public record AutoApplyJobResult(int AppliedCount, int SkippedCount, float AverageConfidence);

/// <summary>
/// Executes the auto-apply job: predicts categories for unassigned transactions
/// that meet the confidence threshold and records audit entries.
/// </summary>
public interface IAutoApplyJobService
{
    /// <summary>
    /// Runs the auto-apply job with the supplied runtime settings.
    /// </summary>
    Task<AutoApplyJobResult> ExecuteJobAsync(AutoApplyRuntimeSettings settings, CancellationToken cancellationToken = default);
}

/// <summary>
/// Scoped implementation of <see cref="IAutoApplyJobService"/>.
/// Uses the injected <see cref="AppDbContext"/> and <see cref="IMLService"/> directly.
/// </summary>
public class AutoApplyJobService : IAutoApplyJobService
{
    private readonly AppDbContext _dbContext;
    private readonly IMLService _mlService;
    private readonly ITransactionAuditRepository _auditRepo;
    private readonly AutomationOptions _options;
    private readonly ILogger<AutoApplyJobService> _logger;

    public AutoApplyJobService(
        AppDbContext dbContext,
        IMLService mlService,
        ITransactionAuditRepository auditRepo,
        IOptions<AutomationOptions> options,
        ILogger<AutoApplyJobService> logger)
    {
        _dbContext = dbContext;
        _mlService = mlService;
        _auditRepo = auditRepo;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AutoApplyJobResult> ExecuteJobAsync(AutoApplyRuntimeSettings settings, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting auto-apply job (confidence threshold: {Threshold})", settings.MinimumConfidence);

        var activeModel = await _mlService.GetActiveModelAsync();
        if (activeModel == null)
        {
            _logger.LogWarning("Auto-apply skipped: No active ML model available");
            return new AutoApplyJobResult(0, 0, 0f);
        }

        var query = _dbContext.Transactions
            .Where(t => !t.IsArchived)
            .Where(t => t.AssignedParts == null || !t.AssignedParts.Any());

        if (settings.AccountIds.Any())
        {
            query = query.Where(t => settings.AccountIds.Contains(t.AccountId));
        }

        var unassignedTransactions = await query
            .OrderBy(t => t.Date)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        if (unassignedTransactions.Count == 0)
        {
            _logger.LogInformation("Auto-apply completed: No unassigned transactions found");
            return new AutoApplyJobResult(0, 0, 0f);
        }

        int appliedCount = 0;
        int skippedCount = 0;
        var confidenceScores = new List<float>();

        foreach (var transaction in unassignedTransactions)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var confidence = await TryApplyAsync(transaction, activeModel.Version, settings, cancellationToken);
            if (confidence.HasValue)
            {
                appliedCount++;
                confidenceScores.Add(confidence.Value);
            }
            else
            {
                skippedCount++;
            }
        }

        var avgConfidence = confidenceScores.Any() ? confidenceScores.Average() : 0f;

        _logger.LogInformation(
            "Auto-apply job completed: {Applied} applied (avg confidence: {AvgConfidence:F4}), {Skipped} skipped",
            appliedCount, avgConfidence, skippedCount);

        return new AutoApplyJobResult(appliedCount, skippedCount, avgConfidence);
    }

    private async Task<float?> TryApplyAsync(
        Transaction transaction,
        int modelVersion,
        AutoApplyRuntimeSettings settings,
        CancellationToken cancellationToken)
    {
        var prediction = await _mlService.PredictCategoryAsync(transaction.Id);

        if (prediction == null)
            return null;

        if (prediction.Confidence < settings.MinimumConfidence)
            return null;

        if (settings.ExcludedCategoryIds.Contains(prediction.CategoryId))
            return null;

        var beforeState = JsonSerializer.Serialize(new { transaction.AssignedParts });

        // TODO: This uses CategoryId as BudgetLineId which is a temporary workaround.
        // In production, we need to resolve the proper BudgetLine for the category within the relevant BudgetPlan.
        // This requires determining which BudgetPlan is active for the transaction's account and date,
        // then finding the BudgetLine that matches the predicted CategoryId.
        var split = new TransactionSplit
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            BudgetLineId = prediction.CategoryId, // Using CategoryId as BudgetLineId proxy for MVP
            Amount = transaction.Amount,
            Note = $"Auto-applied by ML (confidence: {prediction.Confidence:F4})",
            IsArchived = false
        };

        _dbContext.TransactionSplits.Add(split);

        var afterState = JsonSerializer.Serialize(new { AssignedParts = new[] { split } });

        var audit = new TransactionAudit
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            ActionType = "AutoApply",
            ChangedBy = "AutoApplyService",
            ChangedAt = DateTime.UtcNow,
            BeforeState = beforeState,
            AfterState = afterState,
            Reason = $"Auto-applied ML suggestion (model v{modelVersion})",
            IsAutoApplied = true,
            AutoAppliedBy = "AutoApplyService",
            AutoAppliedAt = DateTime.UtcNow,
            Confidence = prediction.Confidence,
            ModelVersion = modelVersion,
            IsArchived = false
        };

        await _auditRepo.AddAsync(audit);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Auto-applied category {CategoryId} to transaction {TransactionId} (confidence: {Confidence:F4})",
            prediction.CategoryId, transaction.Id, prediction.Confidence);

        return prediction.Confidence;
    }
}
