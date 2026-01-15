using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.DTOs.Validators;
using LocalFinanceManager.Models;
using LocalFinanceManager.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LocalFinanceManager.Tests.Unit;

[TestFixture]
public class AccountServiceTests
{
    private Mock<IAccountRepository> _mockRepository = null!;
    private Mock<ILogger<AccountService>> _mockLogger = null!;
    private AccountService _service = null!;

    [SetUp]
    public void Setup()
    {
        _mockRepository = new Mock<IAccountRepository>();
        _mockLogger = new Mock<ILogger<AccountService>>();
        _service = new AccountService(_mockRepository.Object, _mockLogger.Object);
    }

    [Test]
    public async Task GetAllActiveAsync_ReturnsAllActiveAccounts()
    {
        // Arrange
        var accounts = new List<Account>
        {
            new Account { Id = Guid.NewGuid(), Label = "Account 1", Type = AccountType.Checking, IBAN = "NL91ABNA0417164300", Currency = "EUR", StartingBalance = 1000 },
            new Account { Id = Guid.NewGuid(), Label = "Account 2", Type = AccountType.Savings, IBAN = "NL20INGB0001234567", Currency = "EUR", StartingBalance = 2000 }
        };
        _mockRepository.Setup(r => r.GetAllActiveAsync()).ReturnsAsync(accounts);

        // Act
        var result = await _service.GetAllActiveAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Label, Is.EqualTo("Account 1"));
        Assert.That(result[1].Label, Is.EqualTo("Account 2"));
    }

    [Test]
    public async Task GetByIdAsync_ExistingAccount_ReturnsAccount()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            Label = "Test Account",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = 1000
        };
        _mockRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

        // Act
        var result = await _service.GetByIdAsync(accountId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(accountId));
        Assert.That(result.Label, Is.EqualTo("Test Account"));
    }

    [Test]
    public async Task GetByIdAsync_NonExistingAccount_ReturnsNull()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

        // Act
        var result = await _service.GetByIdAsync(accountId);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_CreatesAccount()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Label = "New Account",
            Type = AccountType.Checking,
            IBAN = "NL91 ABNA 0417 1643 00",
            Currency = "eur",
            StartingBalance = 1000
        };

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Label, Is.EqualTo("New Account"));
        Assert.That(result.IBAN, Is.EqualTo("NL91ABNA0417164300")); // Spaces removed
        Assert.That(result.Currency, Is.EqualTo("EUR")); // Uppercase
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_ExistingAccount_UpdatesAccount()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var existingAccount = new Account
        {
            Id = accountId,
            Label = "Old Label",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = 1000
        };

        var request = new UpdateAccountRequest
        {
            Label = "New Label",
            Type = AccountType.Savings,
            IBAN = "NL20INGB0001234567",
            Currency = "USD",
            StartingBalance = 2000
        };

        _mockRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(existingAccount);
        _mockRepository.Setup(r => r.LabelExistsAsync("New Label", accountId)).ReturnsAsync(false);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateAsync(accountId, request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Label, Is.EqualTo("New Label"));
        Assert.That(result.Type, Is.EqualTo(AccountType.Savings));
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Account>()), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_NonExistingAccount_ReturnsNull()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var request = new UpdateAccountRequest
        {
            Label = "New Label",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = 1000
        };

        _mockRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

        // Act
        var result = await _service.UpdateAsync(accountId, request);

        // Assert
        Assert.That(result, Is.Null);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Account>()), Times.Never);
    }

    [Test]
    public async Task ArchiveAsync_ExistingAccount_ArchivesAccount()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            Label = "Test Account",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = 1000,
            IsArchived = false
        };

        _mockRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.ArchiveAsync(accountId);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(account.IsArchived, Is.True);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Account>()), Times.Once);
    }

    [Test]
    public async Task ArchiveAsync_NonExistingAccount_ReturnsFalse()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

        // Act
        var result = await _service.ArchiveAsync(accountId);

        // Assert
        Assert.That(result, Is.False);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Account>()), Times.Never);
    }
}
