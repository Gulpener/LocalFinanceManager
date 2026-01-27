using LocalFinanceManager.Data;
using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LocalFinanceManager.Tests.Integration;

[TestFixture]
public class AccountRepositoryTests
{
    private AppDbContext _context = null!;
    private AccountRepository _repository = null!;
    private Mock<ILogger<Repository<Account>>> _mockLogger = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _mockLogger = new Mock<ILogger<Repository<Account>>>();
        _repository = new AccountRepository(_context, _mockLogger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    [Test]
    public async Task AddAsync_AddsAccountToDatabase()
    {
        // Arrange
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Label = "Test Account",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = 1000,
            IsArchived = false
        };

        // Act
        await _repository.AddAsync(account);

        // Assert
        var savedAccount = await _context.Accounts.FindAsync(account.Id);
        Assert.That(savedAccount, Is.Not.Null);
        Assert.That(savedAccount!.Label, Is.EqualTo("Test Account"));
    }

    [Test]
    public async Task GetByIdAsync_ExistingAccount_ReturnsAccount()
    {
        // Arrange
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Label = "Test Account",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = 1000,
            IsArchived = false
        };

        await _context.Accounts.AddAsync(account);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(account.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(account.Id));
        Assert.That(result.Label, Is.EqualTo("Test Account"));
    }

    [Test]
    public async Task GetByIdAsync_NonExistingAccount_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetAllActiveAsync_ReturnsOnlyActiveAccounts()
    {
        // Arrange
        var activeAccount1 = new Account
        {
            Id = Guid.NewGuid(),
            Label = "Active Account 1",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = 1000,
            IsArchived = false
        };

        var activeAccount2 = new Account
        {
            Id = Guid.NewGuid(),
            Label = "Active Account 2",
            Type = AccountType.Savings,
            IBAN = "NL20INGB0001234567",
            Currency = "EUR",
            StartingBalance = 2000,
            IsArchived = false
        };

        var archivedAccount = new Account
        {
            Id = Guid.NewGuid(),
            Label = "Archived Account",
            Type = AccountType.Checking,
            IBAN = "NL39RABO0300065264",
            Currency = "EUR",
            StartingBalance = 500,
            IsArchived = true
        };

        await _context.Accounts.AddRangeAsync(activeAccount1, activeAccount2, archivedAccount);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllActiveAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.All(a => !a.IsArchived), Is.True);
        Assert.That(result.Select(a => a.Label), Does.Not.Contain("Archived Account"));
    }

    [Test]
    public async Task UpdateAsync_UpdatesAccount()
    {
        // Arrange
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Label = "Original Label",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = 1000,
            IsArchived = false
        };

        await _context.Accounts.AddAsync(account);
        await _context.SaveChangesAsync();

        // Act
        account.Label = "Updated Label";
        account.StartingBalance = 2000;
        await _repository.UpdateAsync(account);

        // Assert
        var updatedAccount = await _context.Accounts.FindAsync(account.Id);
        Assert.That(updatedAccount, Is.Not.Null);
        Assert.That(updatedAccount!.Label, Is.EqualTo("Updated Label"));
        Assert.That(updatedAccount.StartingBalance, Is.EqualTo(2000));
    }

    [Test]
    public async Task LabelExistsAsync_ExistingLabel_ReturnsTrue()
    {
        // Arrange
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Label = "Existing Account",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = 1000,
            IsArchived = false
        };

        await _context.Accounts.AddAsync(account);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.LabelExistsAsync("Existing Account");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task LabelExistsAsync_NonExistingLabel_ReturnsFalse()
    {
        // Act
        var result = await _repository.LabelExistsAsync("Non-Existing Account");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task LabelExistsAsync_ExcludingId_ReturnsCorrectResult()
    {
        // Arrange
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Label = "Test Account",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = 1000,
            IsArchived = false
        };

        await _context.Accounts.AddAsync(account);
        await _context.SaveChangesAsync();

        // Act - should return false when excluding the same account
        var result = await _repository.LabelExistsAsync("Test Account", account.Id);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task LabelExistsAsync_IgnoresArchivedAccounts()
    {
        // Arrange
        var archivedAccount = new Account
        {
            Id = Guid.NewGuid(),
            Label = "Archived Account",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = 1000,
            IsArchived = true
        };

        await _context.Accounts.AddAsync(archivedAccount);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.LabelExistsAsync("Archived Account");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    [Ignore("SQLite does not automatically generate RowVersion values. Real concurrency testing requires SQL Server or PostgreSQL.")]
    public async Task OptimisticConcurrency_DetectsConflict()
    {
        // Note: SQLite doesn't support automatic RowVersion generation
        // This test is marked as Ignore and documents the limitation
        // Real optimistic concurrency testing should be done with:
        // - SQL Server (with rowversion/timestamp column type)
        // - PostgreSQL (with xmin system column)
        // - Or manual RowVersion management in the application

        await Task.CompletedTask;
    }
}
