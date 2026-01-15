using FluentValidation.TestHelper;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.DTOs.Validators;

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
            Name = "Boodschappen"
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
            Name = ""
        };

        // Act
        var result = await _createValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Category name is required.");
    }

    [Test]
    public async Task CreateCategoryDto_NameTooLong_FailsValidation()
    {
        // Arrange
        var dto = new CreateCategoryDto
        {
            Name = new string('A', 101) // 101 characters
        };

        // Act
        var result = await _createValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Category name cannot exceed 100 characters.");
    }

    [Test]
    public async Task CreateCategoryDto_NameExactly100Chars_PassesValidation()
    {
        // Arrange
        var dto = new CreateCategoryDto
        {
            Name = new string('A', 100) // Exactly 100 characters
        };

        // Act
        var result = await _createValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
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
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        // Act
        var result = await _updateValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
