using FluentValidation.TestHelper;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.DTOs.Validators;
using LocalFinanceManager.Models;

namespace LocalFinanceManager.Tests.Unit;

[TestFixture]
public class BudgetValidatorTests
{
    private CreateBudgetPlanDtoValidator _createPlanValidator = null!;
    private UpdateBudgetPlanDtoValidator _updatePlanValidator = null!;
    private CreateBudgetLineDtoValidator _createLineValidator = null!;
    private UpdateBudgetLineDtoValidator _updateLineValidator = null!;
    private CreateCategoryDtoValidator _createCategoryValidator = null!;

    [SetUp]
    public void Setup()
    {
        _createPlanValidator = new CreateBudgetPlanDtoValidator();
        _updatePlanValidator = new UpdateBudgetPlanDtoValidator();
        _createLineValidator = new CreateBudgetLineDtoValidator();
        _updateLineValidator = new UpdateBudgetLineDtoValidator();
        _createCategoryValidator = new CreateCategoryDtoValidator();
    }

    [Test]
    public async Task CreateBudgetPlanDto_ValidData_PassesValidation()
    {
        // Arrange
        var dto = new CreateBudgetPlanDto
        {
            AccountId = Guid.NewGuid(),
            Year = 2026,
            Name = "Test Budget"
        };

        // Act
        var result = await _createPlanValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task CreateBudgetPlanDto_EmptyAccountId_FailsValidation()
    {
        // Arrange
        var dto = new CreateBudgetPlanDto
        {
            AccountId = Guid.Empty,
            Year = 2026,
            Name = "Test Budget"
        };

        // Act
        var result = await _createPlanValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AccountId);
    }

    [Test]
    public async Task CreateBudgetPlanDto_YearTooLow_FailsValidation()
    {
        // Arrange
        var dto = new CreateBudgetPlanDto
        {
            AccountId = Guid.NewGuid(),
            Year = 1999,
            Name = "Test Budget"
        };

        // Act
        var result = await _createPlanValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Year);
    }

    [Test]
    public async Task CreateBudgetPlanDto_YearTooHigh_FailsValidation()
    {
        // Arrange
        var dto = new CreateBudgetPlanDto
        {
            AccountId = Guid.NewGuid(),
            Year = 2101,
            Name = "Test Budget"
        };

        // Act
        var result = await _createPlanValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Year);
    }

    [Test]
    public async Task CreateBudgetPlanDto_EmptyName_FailsValidation()
    {
        // Arrange
        var dto = new CreateBudgetPlanDto
        {
            AccountId = Guid.NewGuid(),
            Year = 2026,
            Name = ""
        };

        // Act
        var result = await _createPlanValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public async Task CreateBudgetPlanDto_NameTooLong_FailsValidation()
    {
        // Arrange
        var dto = new CreateBudgetPlanDto
        {
            AccountId = Guid.NewGuid(),
            Year = 2026,
            Name = new string('a', 151)
        };

        // Act
        var result = await _createPlanValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public async Task CreateBudgetLineDto_ValidData_PassesValidation()
    {
        // Arrange
        var dto = new CreateBudgetLineDto
        {
            BudgetPlanId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            MonthlyAmounts = Enumerable.Repeat(100m, 12).ToArray(),
            Notes = "Test notes"
        };

        // Act
        var result = await _createLineValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task CreateBudgetLineDto_LessThan12Months_FailsValidation()
    {
        // Arrange
        var dto = new CreateBudgetLineDto
        {
            BudgetPlanId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            MonthlyAmounts = new decimal[] { 100m, 200m, 300m }
        };

        // Act
        var result = await _createLineValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MonthlyAmounts);
    }

    [Test]
    public async Task CreateBudgetLineDto_MoreThan12Months_FailsValidation()
    {
        // Arrange
        var dto = new CreateBudgetLineDto
        {
            BudgetPlanId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            MonthlyAmounts = Enumerable.Repeat(100m, 13).ToArray()
        };

        // Act
        var result = await _createLineValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MonthlyAmounts);
    }

    [Test]
    public async Task CreateBudgetLineDto_NegativeAmount_FailsValidation()
    {
        // Arrange
        var amounts = new decimal[12];
        Array.Fill(amounts, 100m);
        amounts[5] = -50m; // One negative value

        var dto = new CreateBudgetLineDto
        {
            BudgetPlanId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            MonthlyAmounts = amounts
        };

        // Act
        var result = await _createLineValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MonthlyAmounts);
    }

    [Test]
    public async Task CreateBudgetLineDto_NotesTooLong_FailsValidation()
    {
        // Arrange
        var dto = new CreateBudgetLineDto
        {
            BudgetPlanId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            MonthlyAmounts = Enumerable.Repeat(100m, 12).ToArray(),
            Notes = new string('a', 501)
        };

        // Act
        var result = await _createLineValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }

    [Test]
    public async Task UpdateBudgetPlanDto_NoRowVersion_FailsValidation()
    {
        // Arrange
        var dto = new UpdateBudgetPlanDto
        {
            Name = "Updated Budget",
            RowVersion = null
        };

        // Act
        var result = await _updatePlanValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RowVersion);
    }

    [Test]
    public async Task UpdateBudgetLineDto_NoRowVersion_FailsValidation()
    {
        // Arrange
        var dto = new UpdateBudgetLineDto
        {
            CategoryId = Guid.NewGuid(),
            MonthlyAmounts = Enumerable.Repeat(100m, 12).ToArray(),
            RowVersion = null
        };

        // Act
        var result = await _updateLineValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RowVersion);
    }

    [Test]
    public async Task CreateCategoryDto_ValidData_PassesValidation()
    {
        // Arrange
        var dto = new CreateCategoryDto 
        { 
            Name = "Test Category",
            Type = CategoryType.Expense,
            BudgetPlanId = Guid.NewGuid()
        };

        // Act
        var result = await _createCategoryValidator.TestValidateAsync(dto);

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
        var result = await _createCategoryValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public async Task CreateCategoryDto_NameTooLong_FailsValidation()
    {
        // Arrange
        var dto = new CreateCategoryDto { Name = new string('a', 101) };

        // Act
        var result = await _createCategoryValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}
