using FluentValidation;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.Services;

namespace LocalFinanceManager.DTOs.Validators;

/// <summary>
/// Result of account validation.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public List<string> ErrorMessages { get; set; } = new();
}

/// <summary>
/// Validator for AssignTransactionRequest.
/// </summary>
public class AssignTransactionRequestValidator : AbstractValidator<AssignTransactionRequest>
{
    private readonly IBudgetAccountLookupService _lookupService;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IBudgetLineRepository _budgetLineRepository;

    public AssignTransactionRequestValidator(
        IBudgetAccountLookupService lookupService,
        ITransactionRepository transactionRepository,
        IBudgetLineRepository budgetLineRepository)
    {
        _lookupService = lookupService;
        _transactionRepository = transactionRepository;
        _budgetLineRepository = budgetLineRepository;

        // BudgetLineId is required
        RuleFor(x => x.BudgetLineId)
            .NotEmpty()
            .WithMessage("BudgetLineId is required");

        // Note length validation
        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Note))
            .WithMessage("Note cannot exceed 500 characters");
    }

    /// <summary>
    /// Validates that budget line belongs to transaction's account budget plan.
    /// Call this from controller/service with transaction ID context.
    /// </summary>
    public async Task<ValidationResult> ValidateAccountMatchAsync(Guid transactionId, Guid budgetLineId)
    {
        var result = new ValidationResult { IsValid = true };

        var transaction = await _transactionRepository.GetByIdWithAccountAsync(transactionId);
        if (transaction == null)
        {
            result.IsValid = false;
            result.ErrorMessage = "Transaction not found";
            return result;
        }

        var budgetLineAccountId = await _lookupService.GetAccountIdForBudgetLineAsync(budgetLineId);
        if (!budgetLineAccountId.HasValue)
        {
            result.IsValid = false;
            result.ErrorMessage = "Budget line not found";
            return result;
        }

        if (budgetLineAccountId.Value != transaction.AccountId)
        {
            result.IsValid = false;
            result.ErrorMessage = $"Budget line belongs to a different account's budget plan. " +
                          $"Transaction is for account '{transaction.Account.Label}', " +
                          $"but budget line belongs to account with ID '{budgetLineAccountId.Value}'";
            return result;
        }

        return result;
    }
}

/// <summary>
/// Validator for SplitTransactionRequest.
/// </summary>
public class SplitTransactionRequestValidator : AbstractValidator<SplitTransactionRequest>
{
    private readonly IBudgetAccountLookupService _lookupService;
    private readonly ITransactionRepository _transactionRepository;

    public SplitTransactionRequestValidator(
        IBudgetAccountLookupService lookupService,
        ITransactionRepository transactionRepository)
    {
        _lookupService = lookupService;
        _transactionRepository = transactionRepository;

        // Must have at least 2 splits
        RuleFor(x => x.Splits)
            .NotEmpty()
            .WithMessage("At least one split is required")
            .Must(splits => splits.Count >= 2)
            .WithMessage("At least 2 splits are required for a split transaction");

        // Each split must have valid data
        RuleForEach(x => x.Splits)
            .SetValidator(new SplitAllocationDtoValidator());
    }

    /// <summary>
    /// Validates that all budget lines belong to transaction's account budget plan.
    /// Call this from controller/service with transaction ID context.
    /// </summary>
    public async Task<ValidationResult> ValidateAccountMatchAsync(Guid transactionId, List<SplitAllocationDto> splits)
    {
        var result = new ValidationResult { IsValid = true, ErrorMessages = new List<string>() };

        var transaction = await _transactionRepository.GetByIdWithAccountAsync(transactionId);
        if (transaction == null)
        {
            result.IsValid = false;
            result.ErrorMessages.Add("Transaction not found");
            return result;
        }

        // Batch lookup for all budget line IDs
        var budgetLineIds = splits.Select(s => s.BudgetLineId).ToList();
        var accountMappings = await _lookupService.GetAccountIdsForBudgetLinesAsync(budgetLineIds);

        // Check each split
        for (int i = 0; i < splits.Count; i++)
        {
            var split = splits[i];

            if (!accountMappings.TryGetValue(split.BudgetLineId, out var budgetLineAccountId))
            {
                result.ErrorMessages.Add($"Split {i + 1}: Budget line not found");
                continue;
            }

            if (budgetLineAccountId != transaction.AccountId)
            {
                result.ErrorMessages.Add($"Split {i + 1}: Budget line belongs to a different account's budget plan. " +
                                $"Transaction is for account '{transaction.Account.Label}', " +
                                $"but budget line belongs to account with ID '{budgetLineAccountId}'");
            }
        }

        result.IsValid = result.ErrorMessages.Count == 0;
        return result;
    }
}

/// <summary>
/// Validator for SplitAllocationDto.
/// </summary>
public class SplitAllocationDtoValidator : AbstractValidator<SplitAllocationDto>
{
    public SplitAllocationDtoValidator()
    {
        // BudgetLineId is required
        RuleFor(x => x.BudgetLineId)
            .NotEmpty()
            .WithMessage("BudgetLineId is required");

        // Amount must be positive
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Split amount must be greater than zero");

        // Note length validation
        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Note))
            .WithMessage("Note cannot exceed 500 characters");
    }
}

/// <summary>
/// Validator for BulkAssignTransactionsRequest.
/// </summary>
public class BulkAssignTransactionsRequestValidator : AbstractValidator<BulkAssignTransactionsRequest>
{
    public BulkAssignTransactionsRequestValidator()
    {
        // Must have at least one transaction ID
        RuleFor(x => x.TransactionIds)
            .NotEmpty()
            .WithMessage("At least one transaction ID is required");

        // BudgetLineId is required
        RuleFor(x => x.BudgetLineId)
            .NotEmpty()
            .WithMessage("BudgetLineId is required");

        // Note length validation
        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Note))
            .WithMessage("Note cannot exceed 500 characters");
    }
}

/// <summary>
/// Validator for UndoAssignmentRequest.
/// </summary>
public class UndoAssignmentRequestValidator : AbstractValidator<UndoAssignmentRequest>
{
    public UndoAssignmentRequestValidator()
    {
        // Transaction ID is required
        RuleFor(x => x.TransactionId)
            .NotEmpty()
            .WithMessage("Transaction ID is required");
    }
}
