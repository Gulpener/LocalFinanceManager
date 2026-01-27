using LocalFinanceManager.Configuration;
using LocalFinanceManager.Data;
using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.ML;
using LocalFinanceManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace LocalFinanceManager.Services.Background;

/// <summary>
/// Background service for automatically applying ML category suggestions to transactions
/// when confidence exceeds the configured threshold.
/// Runs daily (default: 6 AM UTC) to process unassigned transactions.
/// </summary>
public class AutoApplyBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutoApplyBackgroundService> _logger;
    private readonly AutomationOptions _options;

    public AutoApplyBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<AutoApplyBackgroundService> logger,
        IOptions<AutomationOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Auto-Apply Background Service started with schedule: {Schedule} (Enabled: {Enabled})",
            _options.AutoApplyScheduleCron,
            _options.AutoApplyEnabled);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var nextRun = CronParser.GetNextOccurrence(_options.AutoApplyScheduleCron, now);
                var delay = nextRun - now;

                _logger.LogInformation("Next auto-apply job scheduled at {NextRun} UTC (in {Delay})",
                    nextRun, delay);

                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                if (_options.AutoApplyEnabled)
                {
                    await ExecuteAutoApplyJobAsync(stoppingToken);
                }
                else
                {
                    _logger.LogInformation("Auto-apply job skipped: AutoApplyEnabled is false");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Auto-Apply Background Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in auto-apply scheduler loop");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Auto-Apply Background Service stopped");
    }

    private async Task ExecuteAutoApplyJobAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting auto-apply job (confidence threshold: {Threshold})", _options.ConfidenceThreshold);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var mlService = scope.ServiceProvider.GetRequiredService<IMLService>();
            var transactionAuditRepo = scope.ServiceProvider.GetRequiredService<ITransactionAuditRepository>();

            // Get active model info
            var activeModel = await mlService.GetActiveModelAsync();
            if (activeModel == null)
            {
                _logger.LogWarning("Auto-apply skipped: No active ML model available");
                return;
            }

            // Get unassigned transactions (no splits and not archived)
            var unassignedTransactions = await dbContext.Transactions
                .Where(t => !t.IsArchived)
                .Where(t => t.AssignedParts == null || !t.AssignedParts.Any())
                .OrderBy(t => t.Date)
                .Take(_options.BatchSize)
                .ToListAsync(cancellationToken);

            if (unassignedTransactions.Count == 0)
            {
                _logger.LogInformation("Auto-apply completed: No unassigned transactions found");
                return;
            }

            _logger.LogInformation("Processing {Count} unassigned transactions", unassignedTransactions.Count);

            int appliedCount = 0;
            int skippedCount = 0;
            var confidenceScores = new List<float>();

            foreach (var transaction in unassignedTransactions)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var applied = await TryAutoApplyWithRetryAsync(
                    transaction,
                    activeModel.Version,
                    mlService,
                    dbContext,
                    transactionAuditRepo,
                    cancellationToken);

                if (applied.HasValue)
                {
                    appliedCount++;
                    confidenceScores.Add(applied.Value);
                }
                else
                {
                    skippedCount++;
                }
            }

            var avgConfidence = confidenceScores.Any() ? confidenceScores.Average() : 0;

            _logger.LogInformation(
                "Auto-apply job completed: {Applied} applied (avg confidence: {AvgConfidence:F4}), {Skipped} skipped",
                appliedCount,
                avgConfidence,
                skippedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-apply job failed");
        }
    }

    private async Task<float?> TryAutoApplyWithRetryAsync(
        Transaction transaction,
        int modelVersion,
        IMLService mlService,
        AppDbContext dbContext,
        ITransactionAuditRepository auditRepo,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (int attempt = 0; attempt <= _options.MaxRetries; attempt++)
        {
            try
            {
                // Get ML prediction
                var prediction = await mlService.PredictCategoryAsync(transaction.Id);

                if (prediction == null)
                {
                    _logger.LogDebug("Transaction {TransactionId}: No prediction available", transaction.Id);
                    return null;
                }

                if (prediction.Confidence < (float)_options.ConfidenceThreshold)
                {
                    _logger.LogDebug(
                        "Transaction {TransactionId}: Confidence {Confidence:F4} below threshold {Threshold}",
                        transaction.Id,
                        prediction.Confidence,
                        _options.ConfidenceThreshold);
                    return null;
                }

                // Apply the assignment
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

                dbContext.TransactionSplits.Add(split);

                var afterState = JsonSerializer.Serialize(new { AssignedParts = new[] { split } });

                // Create audit record
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

                await auditRepo.AddAsync(audit);
                await dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Auto-applied category {CategoryId} to transaction {TransactionId} with confidence {Confidence:F4}",
                    prediction.CategoryId,
                    transaction.Id,
                    prediction.Confidence);

                return prediction.Confidence;
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (attempt < _options.MaxRetries)
                {
                    var delaySeconds = Math.Pow(2, attempt); // Exponential backoff: 1s, 2s, 4s, 8s, 16s
                    _logger.LogWarning(
                        ex,
                        "Auto-apply failed for transaction {TransactionId} (attempt {Attempt}/{MaxRetries}). Retrying in {Delay}s",
                        transaction.Id,
                        attempt + 1,
                        _options.MaxRetries,
                        delaySeconds);

                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                }
            }
        }

        _logger.LogError(
            lastException,
            "Auto-apply failed for transaction {TransactionId} after {MaxRetries} retries. Skipping.",
            transaction.Id,
            _options.MaxRetries);

        return null;
    }
}
