using FluentValidation;
using LocalFinanceManager.DTOs;

namespace LocalFinanceManager.DTOs.Validators;

/// <summary>
/// Validator for UpdateProfileRequest. Both fields are optional but have max-length constraints.
/// </summary>
public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters")
            .When(x => x.FirstName is not null);

        RuleFor(x => x.LastName)
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters")
            .When(x => x.LastName is not null);
    }
}
