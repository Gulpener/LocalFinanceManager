using FluentValidation;
using LocalFinanceManager.Api.DTOs;

namespace LocalFinanceManager.Api.Validators
{
    public class CreateAccountDtoValidator : AbstractValidator<CreateAccountDto>
    {
        public CreateAccountDtoValidator()
        {
            RuleFor(x => x.Label).NotEmpty().MaximumLength(100);
            RuleFor(x => x.IBAN).NotEmpty().MaximumLength(34);
            RuleFor(x => x.Currency).NotEmpty().Length(3);
            RuleFor(x => x.StartingBalance).GreaterThanOrEqualTo(decimal.MinValue);
        }
    }

    public class UpdateAccountDtoValidator : AbstractValidator<UpdateAccountDto>
    {
        public UpdateAccountDtoValidator()
        {
            RuleFor(x => x.Label).NotEmpty().MaximumLength(100);
            RuleFor(x => x.IBAN).NotEmpty().MaximumLength(34);
            RuleFor(x => x.Currency).NotEmpty().Length(3);
            RuleFor(x => x.StartingBalance).GreaterThanOrEqualTo(decimal.MinValue);
            RuleFor(x => x.RowVersion).NotNull();
        }
    }
}
