using FluentValidation;
using LocalFinanceManager.DTOs;

namespace LocalFinanceManager.DTOs.Validators;

/// <summary>
/// Validator for AssignTransactionRequest.
/// </summary>
public class AssignTransactionRequestValidator : AbstractValidator<AssignTransactionRequest>
{
    public AssignTransactionRequestValidator()
    {
        // At least one of BudgetLineId or CategoryId must be provided
        RuleFor(x => x)
            .Must(x => x.BudgetLineId.HasValue || x.CategoryId.HasValue)
            .WithMessage("Either BudgetLineId or CategoryId must be provided");

        // Note length validation
        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Note))
            .WithMessage("Note cannot exceed 500 characters");
    }
}

/// <summary>
/// Validator for SplitTransactionRequest.
/// </summary>
public class SplitTransactionRequestValidator : AbstractValidator<SplitTransactionRequest>
{
    public SplitTransactionRequestValidator()
    {
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
}

/// <summary>
/// Validator for SplitAllocationDto.
/// </summary>
public class SplitAllocationDtoValidator : AbstractValidator<SplitAllocationDto>
{
    public SplitAllocationDtoValidator()
    {
        // At least one of BudgetLineId or CategoryId must be provided
        RuleFor(x => x)
            .Must(x => x.BudgetLineId.HasValue || x.CategoryId.HasValue)
            .WithMessage("Either BudgetLineId or CategoryId must be provided");

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

        // At least one of BudgetLineId or CategoryId must be provided
        RuleFor(x => x)
            .Must(x => x.BudgetLineId.HasValue || x.CategoryId.HasValue)
            .WithMessage("Either BudgetLineId or CategoryId must be provided");

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
