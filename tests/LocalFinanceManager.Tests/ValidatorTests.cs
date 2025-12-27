using FluentValidation.TestHelper;
using LocalFinanceManager.Api.DTOs;
using LocalFinanceManager.Api.Validators;
using Xunit;

namespace LocalFinanceManager.Tests
{
    public class ValidatorTests
    {
        [Fact]
        public void CreateAccountDtoValidator_Fails_On_Invalid_Data()
        {
            var validator = new CreateAccountDtoValidator();
            var dto = new CreateAccountDto { Label = "", IBAN = "", Currency = "X" };
            var result = validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Label);
            result.ShouldHaveValidationErrorFor(x => x.IBAN);
            result.ShouldHaveValidationErrorFor(x => x.Currency);
        }
    }
}
