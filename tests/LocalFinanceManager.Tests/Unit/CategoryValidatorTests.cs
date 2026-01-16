using FluentValidation.TestHelper;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.DTOs.Validators;
using LocalFinanceManager.Models;

namespace LocalFinanceManager.Tests.Unit;

[TestFixture]
public class CategoryValidatorTests
{
    private CreateCategoryDtoValidator _createValidator = null!;
    private UpdateCategoryDtoValidator _updateValidator = null!;

    [SetUp]
    public void Setup()
    {
        _createValidator = new CreateCategoryDtoValidator();
        _updateValidator = new UpdateCategoryDtoValidator();
    }

    #region CreateCategoryDto Tests

    [Test]
    public async Task CreateCategoryDto_ValidData_PassesValidation()
    {
        // Arrange
        var dto = new CreateCategoryDto
        {
            Name = "Boodschappen",
            Type = CategoryType.Expense,
            BudgetPlanId = Guid.NewGuid()
        };

        // Act
        var result = await _createValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task CreateCategoryDto_EmptyName_FailsValidation()
    {
        // Arrange
        var dto = new CreateCategoryDto
        {
            Name = "",
            Type = CategoryType.Expense,
            BudgetPlanId = Guid.NewGuid()
        };

        // Act
        var result = await _createValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Naam is verplicht");
    }

    [Test]
    public async Task CreateCategoryDto_NameTooLong_FailsValidation()
    {
        // Arrange
        var dto = new CreateCategoryDto
        {
            Name = new string('A', 101), // 101 characters
            Type = CategoryType.Expense,
            BudgetPlanId = Guid.NewGuid()
        };

        // Act
        var result = await _createValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Naam mag maximaal 100 tekens bevatten");
    }

    [Test]
    public async Task CreateCategoryDto_NameExactly100Chars_PassesValidation()
    {
        // Arrange
        var dto = new CreateCategoryDto
        {
            Name = new string('A', 100), // Exactly 100 characters
            Type = CategoryType.Income,
            BudgetPlanId = Guid.NewGuid()
        };

        // Act
        var result = await _createValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task CreateCategoryDto_ValidIncomeType_PassesValidation()
    {
        // Arrange
        var dto = new CreateCategoryDto
        {
            Name = "Salaris",
            Type = CategoryType.Income,
            BudgetPlanId = Guid.NewGuid()
        };

        // Act
        var result = await _createValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task CreateCategoryDto_ValidExpenseType_PassesValidation()
    {
        // Arrange
        var dto = new CreateCategoryDto
        {
            Name = "Huur",
            Type = CategoryType.Expense,
            BudgetPlanId = Guid.NewGuid()
        };

        // Act
        var result = await _createValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task CreateCategoryDto_InvalidEnumValue_FailsValidation()
    {
        // Arrange
        var dto = new CreateCategoryDto
        {
            Name = "Test",
            Type = (CategoryType)99, // Invalid enum value
            BudgetPlanId = Guid.NewGuid()
        };

        // Act
        var result = await _createValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Type)
            .WithErrorMessage("Type moet Income of Expense zijn");
    }

    [Test]
    public async Task CreateCategoryDto_EmptyBudgetPlanId_FailsValidation()
    {
        // Arrange
        var dto = new CreateCategoryDto
        {
            Name = "Test Category",
            Type = CategoryType.Expense,
            BudgetPlanId = Guid.Empty
        };

        // Act
        var result = await _createValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BudgetPlanId)
            .WithErrorMessage("Budget Plan ID is verplicht");
    }

    #endregion

    #region UpdateCategoryDto Tests

    [Test]
    public async Task UpdateCategoryDto_ValidData_PassesValidation()
    {
        // Arrange
        var dto = new UpdateCategoryDto
        {
            Name = "Transport",
            Type = CategoryType.Expense,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        // Act
        var result = await _updateValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task UpdateCategoryDto_EmptyName_FailsValidation()
    {
        // Arrange
        var dto = new UpdateCategoryDto
        {
            Name = "",
            Type = CategoryType.Expense,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        // Act
        var result = await _updateValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Naam is verplicht");
    }

    [Test]
    public async Task UpdateCategoryDto_NameTooLong_FailsValidation()
    {
        // Arrange
        var dto = new UpdateCategoryDto
        {
            Name = new string('B', 101), // 101 characters
            Type = CategoryType.Income,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        // Act
        var result = await _updateValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Naam mag maximaal 100 tekens bevatten");
    }

    [Test]
    public async Task UpdateCategoryDto_NullRowVersion_FailsValidation()
    {
        // Arrange
        var dto = new UpdateCategoryDto
        {
            Name = "Valid Name",
            Type = CategoryType.Expense,
            RowVersion = null
        };

        // Act
        var result = await _updateValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RowVersion)
            .WithErrorMessage("RowVersion is verplicht voor concurrency controle");
    }

    [Test]
    public async Task UpdateCategoryDto_NameExactly100Chars_PassesValidation()
    {
        // Arrange
        var dto = new UpdateCategoryDto
        {
            Name = new string('C', 100), // Exactly 100 characters
            Type = CategoryType.Income,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        // Act
        var result = await _updateValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task UpdateCategoryDto_ValidIncomeType_PassesValidation()
    {
        // Arrange
        var dto = new UpdateCategoryDto
        {
            Name = "Freelance",
            Type = CategoryType.Income,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        // Act
        var result = await _updateValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task UpdateCategoryDto_ValidExpenseType_PassesValidation()
    {
        // Arrange
        var dto = new UpdateCategoryDto
        {
            Name = "Utilities",
            Type = CategoryType.Expense,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        // Act
        var result = await _updateValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task UpdateCategoryDto_InvalidEnumValue_FailsValidation()
    {
        // Arrange
        var dto = new UpdateCategoryDto
        {
            Name = "Test",
            Type = (CategoryType)99, // Invalid enum value
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        // Act
        var result = await _updateValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Type)
            .WithErrorMessage("Type moet Income of Expense zijn");
    }

    [Test]
    public async Task UpdateCategoryDto_ChangeTypeFromExpenseToIncome_PassesValidation()
    {
        // Arrange
        var dto = new UpdateCategoryDto
        {
            Name = "Converted Category",
            Type = CategoryType.Income,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        // Act
        var result = await _updateValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
