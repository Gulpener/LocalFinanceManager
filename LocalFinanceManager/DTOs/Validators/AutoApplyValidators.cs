using FluentValidation;
using LocalFinanceManager.DTOs.ML;

namespace LocalFinanceManager.DTOs.Validators;

/// <summary>
/// Validator for AutoApplySettingsDto.
/// </summary>
public class AutoApplySettingsValidator : AbstractValidator<AutoApplySettingsDto>
{
    public AutoApplySettingsValidator()
    {
        RuleFor(x => x.MinimumConfidence)
            .InclusiveBetween(0.0f, 1.0f)
            .WithMessage("Minimum confidence must be between 0.0 and 1.0");

        RuleFor(x => x.MinimumConfidence)
            .InclusiveBetween(0.6f, 0.95f)
            .WithMessage("Recommended confidence range is 0.6 to 0.95 (60% to 95%)")
            .When(x => x.Enabled)
            .WithSeverity(Severity.Warning);

        RuleFor(x => x.IntervalMinutes)
            .GreaterThan(0)
            .WithMessage("Interval must be greater than 0 minutes");

        RuleFor(x => x.IntervalMinutes)
            .LessThanOrEqualTo(1440) // 24 hours
            .WithMessage("Interval cannot exceed 1440 minutes (24 hours)");

        RuleFor(x => x.AccountIds)
            .NotNull()
            .WithMessage("AccountIds cannot be null");

        RuleFor(x => x.ExcludedCategoryIds)
            .NotNull()
            .WithMessage("ExcludedCategoryIds cannot be null");
    }
}

/// <summary>
/// Validator for SuggestionFeedbackDto.
/// </summary>
public class SuggestionFeedbackValidator : AbstractValidator<SuggestionFeedbackDto>
{
    public SuggestionFeedbackValidator()
    {
        RuleFor(x => x.TransactionId)
            .NotEmpty()
            .WithMessage("Transaction ID is required");

        RuleFor(x => x.FinalCategoryId)
            .NotEmpty()
            .WithMessage("Final category ID is required");

        RuleFor(x => x.SuggestionConfidence)
            .InclusiveBetween(0.0f, 1.0f)
            .When(x => x.SuggestionConfidence.HasValue)
            .WithMessage("Confidence must be between 0.0 and 1.0");
    }
}
