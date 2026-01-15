using FluentValidation;
using IbanNet;
using LocalFinanceManager.Data.Repositories;

namespace LocalFinanceManager.DTOs.Validators;

/// <summary>
/// Validator for CreateAccountRequest.
/// </summary>
public class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IIbanValidator _ibanValidator;

    public CreateAccountRequestValidator(IAccountRepository accountRepository, IIbanValidator ibanValidator)
    {
        _accountRepository = accountRepository;
        _ibanValidator = ibanValidator;

        RuleFor(x => x.Label)
            .NotEmpty().WithMessage("Label is required")
            .MaximumLength(100).WithMessage("Label cannot exceed 100 characters")
            .MustAsync(async (label, cancellationToken) => !await _accountRepository.LabelExistsAsync(label))
            .WithMessage("An account with this label already exists");

        RuleFor(x => x.IBAN)
            .NotEmpty().WithMessage("IBAN is required")
            .MaximumLength(34).WithMessage("IBAN cannot exceed 34 characters")
            .Must(BeValidIban).WithMessage("Invalid IBAN format");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(3).WithMessage("Currency must be a 3-letter ISO-4217 code")
            .Matches("^[A-Z]{3}$").WithMessage("Currency must be uppercase ISO-4217 code (e.g., EUR, USD)");

        RuleFor(x => x.StartingBalance)
            .GreaterThanOrEqualTo(0).WithMessage("Starting balance cannot be negative for Checking or Savings accounts")
            .When(x => x.Type == Models.AccountType.Checking || x.Type == Models.AccountType.Savings);
    }

    private bool BeValidIban(string iban)
    {
        if (string.IsNullOrWhiteSpace(iban))
            return false;

        // Remove spaces for validation
        var normalizedIban = iban.Replace(" ", "");
        var result = _ibanValidator.Validate(normalizedIban);
        return result.IsValid;
    }
}

/// <summary>
/// Validator for UpdateAccountRequest.
/// </summary>
public class UpdateAccountRequestValidator : AbstractValidator<UpdateAccountRequest>
{
    private readonly IIbanValidator _ibanValidator;

    public UpdateAccountRequestValidator(IIbanValidator ibanValidator)
    {
        _ibanValidator = ibanValidator;

        RuleFor(x => x.Label)
            .NotEmpty().WithMessage("Label is required")
            .MaximumLength(100).WithMessage("Label cannot exceed 100 characters");

        RuleFor(x => x.IBAN)
            .NotEmpty().WithMessage("IBAN is required")
            .MaximumLength(34).WithMessage("IBAN cannot exceed 34 characters")
            .Must(BeValidIban).WithMessage("Invalid IBAN format");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(3).WithMessage("Currency must be a 3-letter ISO-4217 code")
            .Matches("^[A-Z]{3}$").WithMessage("Currency must be uppercase ISO-4217 code (e.g., EUR, USD)");

        RuleFor(x => x.StartingBalance)
            .GreaterThanOrEqualTo(0).WithMessage("Starting balance cannot be negative for Checking or Savings accounts")
            .When(x => x.Type == Models.AccountType.Checking || x.Type == Models.AccountType.Savings);
    }

    private bool BeValidIban(string iban)
    {
        if (string.IsNullOrWhiteSpace(iban))
            return false;

        var normalizedIban = iban.Replace(" ", "");
        var result = _ibanValidator.Validate(normalizedIban);
        return result.IsValid;
    }
}
