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
    Task<BulkAssignResultDto> BulkAssignTransactionsAsync(
        BulkAssignTransactionsRequest request,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);
    Task<TransactionDto> UndoAssignmentAsync(UndoAssignmentRequest request);
    Task<List<TransactionAuditDto>> GetTransactionAuditHistoryAsync(Guid transactionId, int page = 1, int pageSize = 50);
    Task<int> GetTransactionAuditCountAsync(Guid transactionId);
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

        try
        {
            await ValidateBudgetLineAssignmentAsync(transaction, request.BudgetLineId);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            await RecordAuditAsync(
                transactionId,
                "ValidationFailed",
                new { request.BudgetLineId },
                new { ErrorCode = "AssignmentValidationFailed", Error = ex.Message });

            throw;
        }

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

        // Reload transaction with splits and account
        transaction = await _transactionRepository.GetByIdWithAccountAsync(transactionId);
        if (transaction == null)
        {
            throw new InvalidOperationException($"Transaction {transactionId} could not be reloaded after assign");
        }

        return MapToDto(transaction);
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

        // Validate account budget-plan ownership and year matching for all unique budget lines
        var budgetLineIds = request.Splits.Select(s => s.BudgetLineId).Distinct().ToList();
        try
        {
            foreach (var budgetLineId in budgetLineIds)
            {
                await ValidateBudgetLineAssignmentAsync(transaction, budgetLineId);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            try
            {
                await RecordAuditAsync(
                    transactionId,
                    "ValidationFailed",
                    new { BudgetLineIds = budgetLineIds },
                    new { ErrorCode = "SplitValidationFailed", Error = ex.Message });
            }
            catch (Exception auditEx)
            {
                _logger.LogError(
                    auditEx,
                    "Failed to record audit for split validation failure on transaction {TransactionId}",
                    transactionId);
            }

            throw;
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

        // Reload transaction with splits and account
        transaction = await _transactionRepository.GetByIdWithAccountAsync(transactionId);
        if (transaction == null)
        {
            throw new InvalidOperationException($"Transaction {transactionId} not found after split operation");
        }

        return MapToDto(transaction);
    }

    public async Task<BulkAssignResultDto> BulkAssignTransactionsAsync(
        BulkAssignTransactionsRequest request,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Bulk assigning {Count} transactions to BudgetLineId={BudgetLineId}",
            request.TransactionIds.Count, request.BudgetLineId);

        var result = new BulkAssignResultDto
        {
            TotalCount = request.TransactionIds.Count
        };

        var assignedCount = 0;
        var failedIds = new List<Guid>();

        for (int i = 0; i < request.TransactionIds.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var transactionId = request.TransactionIds[i];
            try
            {
                var assignRequest = new AssignTransactionRequest
                {
                    BudgetLineId = request.BudgetLineId,
                    Note = request.Note
                };

                await AssignTransactionAsync(transactionId, assignRequest);
                assignedCount++;

                cancellationToken.ThrowIfCancellationRequested();

                // Report progress as percentage (0-100)
                progress?.Report((int)((i + 1) * 100.0 / request.TransactionIds.Count));
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
            throw new InvalidOperationException("No audit entry found to undo");
        }

        if (auditEntry.TransactionId != request.TransactionId)
        {
            _logger.LogWarning(
                "Audit entry {AuditEntryId} does not belong to transaction {TransactionId}",
                auditEntry.Id,
                request.TransactionId);
            throw new InvalidOperationException($"No audit entry found to undo for transaction {request.TransactionId}");
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

        // Reload transaction with account
        transaction = await _transactionRepository.GetByIdWithAccountAsync(request.TransactionId);
        if (transaction == null)
        {
            throw new InvalidOperationException($"Transaction {request.TransactionId} not found after undo operation");
        }

        return MapToDto(transaction);
    }

    public async Task<List<TransactionAuditDto>> GetTransactionAuditHistoryAsync(Guid transactionId, int page = 1, int pageSize = 50)
    {
        var transaction = await _transactionRepository.GetByIdWithAccountAsync(transactionId);
        if (transaction == null)
        {
            _logger.LogWarning(
                "Audit history requested for inaccessible or non-existent transaction {TransactionId}",
                transactionId);
            throw new InvalidOperationException($"Transaction {transactionId} not found");
        }

        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page), page, "Page must be greater than or equal to 1.");
        }

        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "Page size must be greater than or equal to 1.");
        }

        var skip = (page - 1) * pageSize;

        var audits = await _auditRepository.GetPagedByTransactionIdAsync(transactionId, skip, pageSize);
        return audits
            .Select(a => new TransactionAuditDto
            {
                Id = a.Id,
                TransactionId = a.TransactionId,
                ActionType = a.ActionType,
                ChangedBy = a.ChangedBy,
                ChangedAt = a.ChangedAt,
                IsAutoApplied = a.IsAutoApplied,
                Confidence = a.Confidence,
                ModelVersion = a.ModelVersion,
                BeforeState = a.BeforeState,
                AfterState = a.AfterState,
                Reason = a.Reason
            })
            .ToList();
    }

    public async Task<int> GetTransactionAuditCountAsync(Guid transactionId)
    {
        var transaction = await _transactionRepository.GetByIdWithAccountAsync(transactionId);
        if (transaction == null)
        {
            return 0;
        }

        return await _auditRepository.GetCountByTransactionIdAsync(transactionId);
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

    private async Task ValidateBudgetLineAssignmentAsync(Transaction transaction, Guid budgetLineId)
    {
        var budgetLine = await _budgetLineRepository.GetByIdAsync(budgetLineId);
        if (budgetLine == null)
        {
            throw new InvalidOperationException($"Budget line {budgetLineId} not found");
        }

        ValidateBudgetLineBelongsToTransactionAccount(transaction, budgetLine);
        ValidateYearMatch(transaction, budgetLine);
    }

    private static void ValidateYearMatch(Transaction transaction, BudgetLine budgetLine)
    {
        var transactionYear = transaction.Date.Year;

        // Load budget plan to get year
        var budgetPlan = budgetLine.BudgetPlan;
        if (budgetPlan == null)
        {
            throw new InvalidOperationException($"Budget plan not found for budget line {budgetLine.Id}");
        }

        if (budgetPlan.Year != transactionYear)
        {
            throw new InvalidOperationException(
                $"Cannot assign {transactionYear} transaction to {budgetPlan.Year} budget plan. " +
                $"Create a budget plan for {transactionYear} first.");
        }
    }

    private static void ValidateBudgetLineBelongsToTransactionAccount(Transaction transaction, BudgetLine budgetLine)
    {
        var budgetPlan = budgetLine.BudgetPlan;
        if (budgetPlan == null)
        {
            throw new InvalidOperationException($"Budget plan not found for budget line {budgetLine.Id}");
        }

        if (budgetPlan.AccountId != transaction.AccountId)
        {
            throw new ArgumentException(
                $"Budget line belongs to a different account budget plan. Transaction account: {transaction.AccountId}, budget line account: {budgetPlan.AccountId}.");
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
            AccountCurrency = transaction.Account?.Currency,
            ExternalId = transaction.ExternalId,
            SourceFileName = transaction.SourceFileName,
            ImportedAt = transaction.ImportedAt,
            XMin = transaction.XMin
        };
    }
}
