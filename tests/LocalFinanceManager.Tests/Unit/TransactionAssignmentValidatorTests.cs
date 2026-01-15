using FluentValidation.TestHelper;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.DTOs.Validators;

namespace LocalFinanceManager.Tests.Unit;

/// <summary>
/// Unit tests for transaction assignment validators.
/// </summary>
[TestFixture]
public class TransactionAssignmentValidatorTests
{
    [Test]
    public void AssignTransactionRequest_ValidWithBudgetLineId_ShouldPassValidation()
    {
        // Arrange
        var validator = new AssignTransactionRequestValidator();
        var request = new AssignTransactionRequest
        {
            BudgetLineId = Guid.NewGuid(),
            Note = "Test assignment"
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void AssignTransactionRequest_ValidWithCategoryId_ShouldPassValidation()
    {
        // Arrange
        var validator = new AssignTransactionRequestValidator();
        var request = new AssignTransactionRequest
        {
            CategoryId = Guid.NewGuid(),
            Note = "Test assignment"
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void AssignTransactionRequest_NoBudgetLineOrCategoryId_ShouldFailValidation()
    {
        // Arrange
        var validator = new AssignTransactionRequestValidator();
        var request = new AssignTransactionRequest
        {
            Note = "Test assignment"
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x);
    }

    [Test]
    public void AssignTransactionRequest_NoteTooLong_ShouldFailValidation()
    {
        // Arrange
        var validator = new AssignTransactionRequestValidator();
        var request = new AssignTransactionRequest
        {
            BudgetLineId = Guid.NewGuid(),
            Note = new string('a', 501)
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Note);
    }

    [Test]
    public void SplitTransactionRequest_ValidSplits_ShouldPassValidation()
    {
        // Arrange
        var validator = new SplitTransactionRequestValidator();
        var request = new SplitTransactionRequest
        {
            Splits = new List<SplitAllocationDto>
            {
                new SplitAllocationDto { BudgetLineId = Guid.NewGuid(), Amount = 50.00m },
                new SplitAllocationDto { CategoryId = Guid.NewGuid(), Amount = 50.00m }
            }
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void SplitTransactionRequest_EmptySplits_ShouldFailValidation()
    {
        // Arrange
        var validator = new SplitTransactionRequestValidator();
        var request = new SplitTransactionRequest
        {
            Splits = new List<SplitAllocationDto>()
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Splits);
    }

    [Test]
    public void SplitTransactionRequest_OnlyOneSplit_ShouldFailValidation()
    {
        // Arrange
        var validator = new SplitTransactionRequestValidator();
        var request = new SplitTransactionRequest
        {
            Splits = new List<SplitAllocationDto>
            {
                new SplitAllocationDto { BudgetLineId = Guid.NewGuid(), Amount = 100.00m }
            }
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Splits);
    }

    [Test]
    public void SplitAllocation_NegativeAmount_ShouldFailValidation()
    {
        // Arrange
        var validator = new SplitAllocationDtoValidator();
        var allocation = new SplitAllocationDto
        {
            BudgetLineId = Guid.NewGuid(),
            Amount = -10.00m
        };

        // Act
        var result = validator.TestValidate(allocation);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Test]
    public void SplitAllocation_ZeroAmount_ShouldFailValidation()
    {
        // Arrange
        var validator = new SplitAllocationDtoValidator();
        var allocation = new SplitAllocationDto
        {
            BudgetLineId = Guid.NewGuid(),
            Amount = 0m
        };

        // Act
        var result = validator.TestValidate(allocation);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Test]
    public void BulkAssignTransactionsRequest_ValidRequest_ShouldPassValidation()
    {
        // Arrange
        var validator = new BulkAssignTransactionsRequestValidator();
        var request = new BulkAssignTransactionsRequest
        {
            TransactionIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            BudgetLineId = Guid.NewGuid()
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void BulkAssignTransactionsRequest_EmptyTransactionIds_ShouldFailValidation()
    {
        // Arrange
        var validator = new BulkAssignTransactionsRequestValidator();
        var request = new BulkAssignTransactionsRequest
        {
            TransactionIds = new List<Guid>(),
            BudgetLineId = Guid.NewGuid()
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TransactionIds);
    }

    [Test]
    public void BulkAssignTransactionsRequest_NoBudgetLineOrCategory_ShouldFailValidation()
    {
        // Arrange
        var validator = new BulkAssignTransactionsRequestValidator();
        var request = new BulkAssignTransactionsRequest
        {
            TransactionIds = new List<Guid> { Guid.NewGuid() }
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x);
    }
}
