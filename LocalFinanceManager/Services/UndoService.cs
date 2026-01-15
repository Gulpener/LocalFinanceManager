using LocalFinanceManager.Configuration;
using LocalFinanceManager.Data;
using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace LocalFinanceManager.Services;

/// <summary>
/// Service for undoing auto-applied transaction assignments.
/// Respects retention window and handles concurrency conflicts.
/// </summary>
public interface IUndoService
{
    /// <summary>
    /// Undoes an auto-applied assignment for a transaction.
    /// </summary>
    /// <param name="transactionId">Transaction ID to undo</param>
    /// <returns>True if undo successful, false otherwise</returns>
    Task<UndoResult> UndoAutoApplyAsync(Guid transactionId);

    /// <summary>
    /// Checks if a transaction has an auto-applied assignment that can be undone.
    /// </summary>
    Task<bool> CanUndoAsync(Guid transactionId);
}

public class UndoService : IUndoService
{
    private readonly AppDbContext _dbContext;
    private readonly ITransactionAuditRepository _auditRepo;
    private readonly AutomationOptions _options;
    private readonly ILogger<UndoService> _logger;

    public UndoService(
        AppDbContext dbContext,
        ITransactionAuditRepository auditRepo,
        IOptions<AutomationOptions> options,
        ILogger<UndoService> logger)
    {
        _dbContext = dbContext;
        _auditRepo = auditRepo;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<UndoResult> UndoAutoApplyAsync(Guid transactionId)
    {
        try
        {
            // Find the most recent auto-apply audit entry for this transaction
            var retentionCutoff = DateTime.UtcNow.AddDays(-_options.UndoRetentionDays);

            var autoApplyAudit = await _dbContext.TransactionAudits
                .Where(a => !a.IsArchived)
                .Where(a => a.TransactionId == transactionId)
                .Where(a => a.IsAutoApplied)
                .Where(a => a.AutoAppliedAt >= retentionCutoff)
                .OrderByDescending(a => a.AutoAppliedAt)
                .FirstOrDefaultAsync();

            if (autoApplyAudit == null)
            {
                return new UndoResult
                {
                    Success = false,
                    Message = "No auto-applied assignment found within retention window"
                };
            }

            // Get transaction with its splits
            var transaction = await _dbContext.Transactions
                .Include(t => t.AssignedParts)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                return new UndoResult
                {
                    Success = false,
                    Message = "Transaction not found"
                };
            }

            // Check if transaction was modified after auto-apply
            var hasSubsequentChanges = await _dbContext.TransactionAudits
                .AnyAsync(a => !a.IsArchived
                    && a.TransactionId == transactionId
                    && a.ChangedAt > autoApplyAudit.AutoAppliedAt
                    && !a.IsAutoApplied);

            if (hasSubsequentChanges)
            {
                return new UndoResult
                {
                    Success = false,
                    Message = "Transaction was modified after auto-apply. Manual review required.",
                    ConflictDetected = true
                };
            }

            // Record state before undo
            var beforeState = JsonSerializer.Serialize(new
            {
                AssignedParts = transaction.AssignedParts?.Select(s => new
                {
                    s.Id,
                    s.BudgetLineId,
                    s.Amount,
                    s.Note
                }).ToList()
            });

            // Remove the auto-applied splits
            if (transaction.AssignedParts != null && transaction.AssignedParts.Any())
            {
                foreach (var split in transaction.AssignedParts.ToList())
                {
                    _dbContext.TransactionSplits.Remove(split);
                }
            }

            var afterState = JsonSerializer.Serialize(new { AssignedParts = (object?)null });

            // Create undo audit entry
            var undoAudit = new TransactionAudit
            {
                Id = Guid.NewGuid(),
                TransactionId = transactionId,
                ActionType = "Undo",
                ChangedBy = "UndoService",
                ChangedAt = DateTime.UtcNow,
                BeforeState = beforeState,
                AfterState = afterState,
                Reason = $"Reverted auto-applied assignment (model v{autoApplyAudit.ModelVersion}, confidence: {autoApplyAudit.Confidence:F4})",
                IsArchived = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _auditRepo.AddAsync(undoAudit);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Undid auto-applied assignment for transaction {TransactionId} (model v{ModelVersion}, confidence: {Confidence:F4})",
                transactionId,
                autoApplyAudit.ModelVersion,
                autoApplyAudit.Confidence);

            return new UndoResult
            {
                Success = true,
                Message = "Auto-applied assignment successfully undone"
            };
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict when undoing transaction {TransactionId}", transactionId);
            return new UndoResult
            {
                Success = false,
                Message = "Transaction was modified by another request. Please reload and try again.",
                ConflictDetected = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error undoing auto-apply for transaction {TransactionId}", transactionId);
            return new UndoResult
            {
                Success = false,
                Message = "An error occurred while undoing the assignment"
            };
        }
    }

    public async Task<bool> CanUndoAsync(Guid transactionId)
    {
        var retentionCutoff = DateTime.UtcNow.AddDays(-_options.UndoRetentionDays);

        var hasAutoApply = await _dbContext.TransactionAudits
            .AnyAsync(a => !a.IsArchived
                && a.TransactionId == transactionId
                && a.IsAutoApplied
                && a.AutoAppliedAt >= retentionCutoff);

        return hasAutoApply;
    }
}

/// <summary>
/// Result of an undo operation.
/// </summary>
public class UndoResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool ConflictDetected { get; set; }
}
