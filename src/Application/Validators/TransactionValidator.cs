using FluentValidation;
using LocalFinanceManager.Domain.Entities;

namespace LocalFinanceManager.Application.Validators;

/// <summary>
/// Validator for Transaction entity.
/// </summary>
public class TransactionValidator : AbstractValidator<Transaction>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionValidator"/> class.
    /// </summary>
    public TransactionValidator()
    {
        RuleFor(t => t.AccountId)
            .GreaterThan(0)
            .WithMessage("Account is required");

        RuleFor(t => t.Date)
            .NotEmpty()
            .WithMessage("Date is required")
            .LessThanOrEqualTo(DateTime.Today.AddDays(1))
            .WithMessage("Date cannot be in the future");

        RuleFor(t => t.Amount)
            .NotEqual(0)
            .WithMessage("Amount cannot be zero");

        RuleFor(t => t.Description)
            .NotEmpty()
            .WithMessage("Description is required")
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters");

        RuleFor(t => t.CategoryId)
            .GreaterThan(0)
            .WithMessage("Category is required");

        RuleFor(t => t.CounterAccount)
            .MaximumLength(50)
            .WithMessage("Counter account cannot exceed 50 characters");
    }
}
