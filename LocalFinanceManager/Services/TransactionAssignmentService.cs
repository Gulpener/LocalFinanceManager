using System.Text.Json;
using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.Models;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Services;

/// <summary>
/// Service for handling transaction assignment operations.
/// </summary>
public interface ITransactionAssignmentService
{
    Task<TransactionDto> AssignTransactionAsync(Guid transactionId, AssignTransactionRequest request);
    Task<TransactionDto> SplitTransactionAsync(Guid transactionId, SplitTransactionRequest request);
    Task<BulkAssignResultDto> BulkAssignTransactionsAsync(BulkAssignTransactionsRequest request);
    Task<TransactionDto> UndoAssignmentAsync(UndoAssignmentRequest request);
    Task<List<TransactionAuditDto>> GetTransactionAuditHistoryAsync(Guid transactionId);
}

/// <summary>
/// Implementation of transaction assignment service.
/// </summary>
public class TransactionAssignmentService : ITransactionAssignmentService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransactionSplitRepository _splitRepository;
    private readonly ITransactionAuditRepository _auditRepository;
    private readonly IBudgetLineRepository _budgetLineRepository;
    private readonly ILogger<TransactionAssignmentService> _logger;
    private const decimal RoundingTolerance = 0.01m;

    public TransactionAssignmentService(
        ITransactionRepository transactionRepository,
        ITransactionSplitRepository splitRepository,
        ITransactionAuditRepository auditRepository,
        IBudgetLineRepository budgetLineRepository,
        ILogger<TransactionAssignmentService> logger)
    {
        _transactionRepository = transactionRepository;
        _splitRepository = splitRepository;
        _auditRepository = auditRepository;
        _budgetLineRepository = budgetLineRepository;
        _logger = logger;
    }

    public async Task<TransactionDto> AssignTransactionAsync(Guid transactionId, AssignTransactionRequest request)
    {
        _logger.LogInformation("Assigning transaction {TransactionId} to BudgetLineId={BudgetLineId}",
            transactionId, request.BudgetLineId);

        var transaction = await _transactionRepository.GetByIdAsync(transactionId);
        if (transaction == null)
        {
            throw new InvalidOperationException($"Transaction {transactionId} not found");
        }

        // Validate year matching
        await ValidateYearMatchAsync(transaction, request.BudgetLineId);

        // Clear existing splits
        await _splitRepository.DeleteByTransactionIdAsync(transactionId);

        // Create single split representing full assignment
        var split = new TransactionSplit
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            BudgetLineId = request.BudgetLineId,
            Amount = Math.Abs(transaction.Amount), // Use absolute value for split amount
            Note = request.Note
        };

        await _splitRepository.AddAsync(split);

        // Record audit trail
        await RecordAuditAsync(transactionId, "Assign", null, new { request.BudgetLineId, request.Note });

        // Reload transaction with splits
        transaction = await _transactionRepository.GetByIdAsync(transactionId);
        return MapToDto(transaction!);
    }

    public async Task<TransactionDto> SplitTransactionAsync(Guid transactionId, SplitTransactionRequest request)
    {
        _logger.LogInformation("Splitting transaction {TransactionId} into {SplitCount} parts",
            transactionId, request.Splits.Count);

        var transaction = await _transactionRepository.GetByIdAsync(transactionId);
        if (transaction == null)
        {
            throw new InvalidOperationException($"Transaction {transactionId} not found");
        }

        // Validate year matching for all splits
        foreach (var split in request.Splits)
        {
            await ValidateYearMatchAsync(transaction, split.BudgetLineId);
        }

        // Validate split sum
        var totalSplitAmount = request.Splits.Sum(s => s.Amount);
        var transactionAbsAmount = Math.Abs(transaction.Amount);

        if (Math.Abs(totalSplitAmount - transactionAbsAmount) > RoundingTolerance)
        {
            throw new InvalidOperationException(
                $"Split amounts ({totalSplitAmount}) must equal transaction amount ({transactionAbsAmount}) within tolerance ({RoundingTolerance})");
        }

        // Clear existing splits
        await _splitRepository.DeleteByTransactionIdAsync(transactionId);

        // Create new splits
        var splits = request.Splits.Select(s => new TransactionSplit
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            BudgetLineId = s.BudgetLineId,
            Amount = s.Amount,
            Note = s.Note
        }).ToList();

        await _splitRepository.AddRangeAsync(splits);

        // Record audit trail
        await RecordAuditAsync(transactionId, "Split", null, new { Splits = request.Splits });

        // Reload transaction with splits
        transaction = await _transactionRepository.GetByIdAsync(transactionId);
        return MapToDto(transaction!);
    }

    public async Task<BulkAssignResultDto> BulkAssignTransactionsAsync(BulkAssignTransactionsRequest request)
    {
        _logger.LogInformation("Bulk assigning {Count} transactions to BudgetLineId={BudgetLineId}",
            request.TransactionIds.Count, request.BudgetLineId);

        var result = new BulkAssignResultDto
        {
            TotalCount = request.TransactionIds.Count
        };

        var assignedCount = 0;
        var failedIds = new List<Guid>();

        foreach (var transactionId in request.TransactionIds)
        {
            try
            {
                var assignRequest = new AssignTransactionRequest
                {
                    BudgetLineId = request.BudgetLineId,
                    Note = request.Note
                };

                await AssignTransactionAsync(transactionId, assignRequest);
                assignedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to assign transaction {TransactionId}", transactionId);
                failedIds.Add(transactionId);
            }
        }

        result.AssignedCount = assignedCount;
        result.FailedCount = failedIds.Count;
        result.FailedTransactionIds = failedIds;
        result.Success = failedIds.Count == 0;
        result.Message = result.Success
            ? $"Successfully assigned {assignedCount} transactions"
            : $"Assigned {assignedCount} transactions, {failedIds.Count} failed";

        return result;
    }

    public async Task<TransactionDto> UndoAssignmentAsync(UndoAssignmentRequest request)
    {
        _logger.LogInformation("Undoing assignment for transaction {TransactionId}", request.TransactionId);

        var transaction = await _transactionRepository.GetByIdAsync(request.TransactionId);
        if (transaction == null)
        {
            throw new InvalidOperationException($"Transaction {request.TransactionId} not found");
        }

        // Get the audit entry to undo
        var auditEntry = request.AuditEntryId.HasValue
            ? await _auditRepository.GetByIdAsync(request.AuditEntryId.Value)
            : await _auditRepository.GetLatestByTransactionIdAsync(request.TransactionId);

        if (auditEntry == null)
        {
            throw new InvalidOperationException($"No audit entry found to undo");
        }

        // Clear current splits
        await _splitRepository.DeleteByTransactionIdAsync(request.TransactionId);

        // If there was a before state, restore it
        if (!string.IsNullOrEmpty(auditEntry.BeforeState))
        {
            try
            {
                // Parse before state and restore splits
                var beforeState = JsonSerializer.Deserialize<Dictionary<string, object>>(auditEntry.BeforeState);
                // For MVP, we'll implement simple undo by just clearing splits
                // Full restore would deserialize and recreate splits from beforeState
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse before state for undo");
            }
        }

        // Record undo in audit trail
        await RecordAuditAsync(request.TransactionId, "Undo", auditEntry.AfterState, null);

        // Reload transaction
        transaction = await _transactionRepository.GetByIdAsync(request.TransactionId);
        return MapToDto(transaction!);
    }

    public async Task<List<TransactionAuditDto>> GetTransactionAuditHistoryAsync(Guid transactionId)
    {
        var audits = await _auditRepository.GetByTransactionIdAsync(transactionId);
        return audits.Select(a => new TransactionAuditDto
        {
            Id = a.Id,
            TransactionId = a.TransactionId,
            ActionType = a.ActionType,
            ChangedBy = a.ChangedBy,
            ChangedAt = a.ChangedAt,
            BeforeState = a.BeforeState,
            AfterState = a.AfterState,
            Reason = a.Reason
        }).ToList();
    }

    private async Task RecordAuditAsync(Guid transactionId, string actionType, object? beforeState, object? afterState)
    {
        var audit = new TransactionAudit
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            ActionType = actionType,
            ChangedBy = "System", // TODO: Get from current user context
            ChangedAt = DateTime.UtcNow,
            BeforeState = beforeState != null ? JsonSerializer.Serialize(beforeState) : null,
            AfterState = afterState != null ? JsonSerializer.Serialize(afterState) : null
        };

        await _auditRepository.AddAsync(audit);
    }

    private async Task ValidateYearMatchAsync(Transaction transaction, Guid budgetLineId)
    {
        var budgetLine = await _budgetLineRepository.GetByIdAsync(budgetLineId);
        if (budgetLine == null)
        {
            throw new InvalidOperationException($"Budget line {budgetLineId} not found");
        }

        var transactionYear = transaction.Date.Year;

        // Load budget plan to get year
        var budgetPlan = budgetLine.BudgetPlan;
        if (budgetPlan == null)
        {
            throw new InvalidOperationException($"Budget plan not found for budget line {budgetLineId}");
        }

        if (budgetPlan.Year != transactionYear)
        {
            throw new InvalidOperationException(
                $"Cannot assign {transactionYear} transaction to {budgetPlan.Year} budget plan. " +
                $"Create a budget plan for {transactionYear} first.");
        }
    }

    private TransactionDto MapToDto(Transaction transaction)
    {
        return new TransactionDto
        {
            Id = transaction.Id,
            Amount = transaction.Amount,
            Date = transaction.Date,
            Description = transaction.Description,
            Counterparty = transaction.Counterparty,
            AccountId = transaction.AccountId,
            ExternalId = transaction.ExternalId,
            SourceFileName = transaction.SourceFileName,
            ImportedAt = transaction.ImportedAt,
            RowVersion = transaction.RowVersion
        };
    }
}
