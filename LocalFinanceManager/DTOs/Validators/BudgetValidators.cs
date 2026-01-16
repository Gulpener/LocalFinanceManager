using FluentValidation;
using LocalFinanceManager.DTOs;

namespace LocalFinanceManager.DTOs.Validators;

/// <summary>
/// Validator for CreateBudgetPlanDto.
/// </summary>
public class CreateBudgetPlanDtoValidator : AbstractValidator<CreateBudgetPlanDto>
{
    public CreateBudgetPlanDtoValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("Account ID is required.");

        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100)
            .WithMessage("Year must be between 2000 and 2100.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(150)
            .WithMessage("Name cannot exceed 150 characters.");
    }
}

/// <summary>
/// Validator for UpdateBudgetPlanDto.
/// </summary>
public class UpdateBudgetPlanDtoValidator : AbstractValidator<UpdateBudgetPlanDto>
{
    public UpdateBudgetPlanDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(150)
            .WithMessage("Name cannot exceed 150 characters.");

        RuleFor(x => x.RowVersion)
            .NotNull()
            .WithMessage("RowVersion is required for concurrency control.");
    }
}

/// <summary>
/// Validator for CreateBudgetLineDto.
/// </summary>
public class CreateBudgetLineDtoValidator : AbstractValidator<CreateBudgetLineDto>
{
    public CreateBudgetLineDtoValidator()
    {
        RuleFor(x => x.BudgetPlanId)
            .NotEmpty()
            .WithMessage("Budget Plan ID is required.");

        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .WithMessage("Category ID is required.");

        RuleFor(x => x.MonthlyAmounts)
            .NotNull()
            .WithMessage("Monthly amounts are required.")
            .Must(amounts => amounts.Length == 12)
            .WithMessage("Exactly 12 monthly amounts are required (Jan-Dec).")
            .Must(amounts => amounts.All(a => a >= 0))
            .WithMessage("Monthly amounts cannot be negative.");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Notes cannot exceed 500 characters.");
    }
}

/// <summary>
/// Validator for UpdateBudgetLineDto.
/// </summary>
public class UpdateBudgetLineDtoValidator : AbstractValidator<UpdateBudgetLineDto>
{
    public UpdateBudgetLineDtoValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .WithMessage("Category ID is required.");

        RuleFor(x => x.MonthlyAmounts)
            .NotNull()
            .WithMessage("Monthly amounts are required.")
            .Must(amounts => amounts.Length == 12)
            .WithMessage("Exactly 12 monthly amounts are required (Jan-Dec).")
            .Must(amounts => amounts.All(a => a >= 0))
            .WithMessage("Monthly amounts cannot be negative.");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Notes cannot exceed 500 characters.");

        RuleFor(x => x.RowVersion)
            .NotNull()
            .WithMessage("RowVersion is required for concurrency control.");
    }
}

/// <summary>
/// Validator for CreateCategoryDto.
/// </summary>
public class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Naam is verplicht")
            .MaximumLength(100)
            .WithMessage("Naam mag maximaal 100 tekens bevatten");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Type moet Income of Expense zijn");

        RuleFor(x => x.BudgetPlanId)
            .NotEmpty()
            .WithMessage("Budget Plan ID is verplicht");
    }
}

/// <summary>
/// Validator for UpdateCategoryDto.
/// </summary>
public class UpdateCategoryDtoValidator : AbstractValidator<UpdateCategoryDto>
{
    public UpdateCategoryDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Naam is verplicht")
            .MaximumLength(100)
            .WithMessage("Naam mag maximaal 100 tekens bevatten");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Type moet Income of Expense zijn");

        RuleFor(x => x.RowVersion)
            .NotNull()
            .WithMessage("RowVersion is verplicht voor concurrency controle");
    }
}
