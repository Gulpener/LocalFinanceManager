using FluentValidation;
using IbanNet;
using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.DTOs.Validators;
using LocalFinanceManager.Models;
using Moq;

namespace LocalFinanceManager.Tests.Unit;

[TestFixture]
public class AccountValidatorTests
{
    private Mock<IAccountRepository> _mockRepository = null!;
    private IIbanValidator _ibanValidator = null!;
    private CreateAccountRequestValidator _createValidator = null!;
    private UpdateAccountRequestValidator _updateValidator = null!;

    [SetUp]
    public void Setup()
    {
        _mockRepository = new Mock<IAccountRepository>();
        _ibanValidator = new IbanValidator();
        _createValidator = new CreateAccountRequestValidator(_mockRepository.Object, _ibanValidator);
        _updateValidator = new UpdateAccountRequestValidator(_ibanValidator);
    }

    [Test]
    public async Task CreateAccountValidator_ValidRequest_Passes()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Label = "Test Account",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = 1000
        };

        _mockRepository.Setup(r => r.LabelExistsAsync("Test Account", null)).ReturnsAsync(false);

        // Act
        var result = await _createValidator.ValidateAsync(request);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task CreateAccountValidator_EmptyLabel_Fails()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Label = "",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = 1000
        };

        // Act
        var result = await _createValidator.ValidateAsync(request);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == "Label"), Is.True);
    }

    [Test]
    public async Task CreateAccountValidator_LabelTooLong_Fails()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Label = new string('A', 101),
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = 1000
        };

        // Act
        var result = await _createValidator.ValidateAsync(request);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == "Label"), Is.True);
    }

    [Test]
    public async Task CreateAccountValidator_InvalidIBAN_Fails()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Label = "Test Account",
            Type = AccountType.Checking,
            IBAN = "INVALID",
            Currency = "EUR",
            StartingBalance = 1000
        };

        _mockRepository.Setup(r => r.LabelExistsAsync("Test Account", null)).ReturnsAsync(false);

        // Act
        var result = await _createValidator.ValidateAsync(request);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == "IBAN"), Is.True);
    }

    [Test]
    public async Task CreateAccountValidator_EmptyIBAN_Fails()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Label = "Test Account",
            Type = AccountType.Checking,
            IBAN = "",
            Currency = "EUR",
            StartingBalance = 1000
        };

        // Act
        var result = await _createValidator.ValidateAsync(request);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == "IBAN"), Is.True);
    }

    [Test]
    public async Task CreateAccountValidator_InvalidCurrency_Fails()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Label = "Test Account",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EU",
            StartingBalance = 1000
        };

        _mockRepository.Setup(r => r.LabelExistsAsync("Test Account", null)).ReturnsAsync(false);

        // Act
        var result = await _createValidator.ValidateAsync(request);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == "Currency"), Is.True);
    }

    [Test]
    public async Task CreateAccountValidator_LowercaseCurrency_Fails()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Label = "Test Account",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "eur",
            StartingBalance = 1000
        };

        _mockRepository.Setup(r => r.LabelExistsAsync("Test Account", null)).ReturnsAsync(false);

        // Act
        var result = await _createValidator.ValidateAsync(request);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == "Currency"), Is.True);
    }

    [Test]
    public async Task CreateAccountValidator_DuplicateLabel_Fails()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Label = "Existing Account",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = 1000
        };

        _mockRepository.Setup(r => r.LabelExistsAsync("Existing Account", null)).ReturnsAsync(true);

        // Act
        var result = await _createValidator.ValidateAsync(request);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == "Label"), Is.True);
    }

    [Test]
    public async Task CreateAccountValidator_IBANWithSpaces_Passes()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Label = "Test Account",
            Type = AccountType.Checking,
            IBAN = "NL91 ABNA 0417 1643 00",
            Currency = "EUR",
            StartingBalance = 1000
        };

        _mockRepository.Setup(r => r.LabelExistsAsync("Test Account", null)).ReturnsAsync(false);

        // Act
        var result = await _createValidator.ValidateAsync(request);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task CreateAccountValidator_NegativeBalanceForChecking_Fails()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Label = "Test Account",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = -100
        };

        _mockRepository.Setup(r => r.LabelExistsAsync("Test Account", null)).ReturnsAsync(false);

        // Act
        var result = await _createValidator.ValidateAsync(request);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == "StartingBalance"), Is.True);
    }

    [Test]
    public async Task CreateAccountValidator_NegativeBalanceForCredit_Passes()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Label = "Test Credit",
            Type = AccountType.Credit,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = -500
        };

        _mockRepository.Setup(r => r.LabelExistsAsync("Test Credit", null)).ReturnsAsync(false);

        // Act
        var result = await _createValidator.ValidateAsync(request);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }
}
