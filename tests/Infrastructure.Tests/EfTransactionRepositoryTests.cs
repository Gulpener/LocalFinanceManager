using LocalFinanceManager.Domain.Entities;
using LocalFinanceManager.Infrastructure;
using LocalFinanceManager.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests;

public class EfTransactionRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly EfTransactionRepository _repository;
    private readonly Account _testAccount;
    private readonly Category _testCategory;

    public EfTransactionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new EfTransactionRepository(_context);

        // Seed required entities
        _testAccount = new Account { Name = "Test Account", AccountType = "bank", InitialBalance = 1000m, IsActive = true };
        _testCategory = new Category { Name = "Test Category", MonthlyBudget = 500m };
        _context.Accounts.Add(_testAccount);
        _context.Categories.Add(_testCategory);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task AddAsync_ShouldAddTransaction()
    {
        // Arrange
        var transaction = new Transaction
        {
            AccountId = _testAccount.Id,
            CategoryId = _testCategory.Id,
            Date = DateTime.Today,
            Amount = 100.50m,
            Description = "Test Transaction",
            CounterAccount = "NL01TEST0000000001",
            OriginalCsv = "2025-01-15,100.50,Test Transaction"
        };

        // Act
        var result = await _repository.AddAsync(transaction);

        // Assert
        Assert.NotEqual(0, result.Id);
        Assert.Equal(100.50m, result.Amount);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnTransaction_WhenExists()
    {
        // Arrange
        var transaction = new Transaction
        {
            AccountId = _testAccount.Id,
            CategoryId = _testCategory.Id,
            Date = DateTime.Today,
            Amount = 100.50m,
            Description = "Test Transaction",
            CounterAccount = "NL01TEST0000000001"
        };
        await _repository.AddAsync(transaction);

        // Act
        var result = await _repository.GetByIdAsync(transaction.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Transaction", result.Description);
        Assert.NotNull(result.Account);
        Assert.NotNull(result.Category);
    }

    [Fact]
    public async Task GetByAccountIdAsync_ShouldReturnTransactionsForAccount()
    {
        // Arrange
        await _repository.AddAsync(new Transaction
        {
            AccountId = _testAccount.Id,
            CategoryId = _testCategory.Id,
            Date = DateTime.Today,
            Amount = 100m,
            Description = "Transaction 1",
            CounterAccount = "NL01TEST0000000001"
        });
        await _repository.AddAsync(new Transaction
        {
            AccountId = _testAccount.Id,
            CategoryId = _testCategory.Id,
            Date = DateTime.Today.AddDays(-1),
            Amount = 200m,
            Description = "Transaction 2",
            CounterAccount = "NL01TEST0000000002"
        });

        // Act
        var result = await _repository.GetByAccountIdAsync(_testAccount.Id);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByDateRangeAsync_ShouldReturnTransactionsInRange()
    {
        // Arrange
        var today = DateTime.Today;
        await _repository.AddAsync(new Transaction
        {
            AccountId = _testAccount.Id,
            CategoryId = _testCategory.Id,
            Date = today,
            Amount = 100m,
            Description = "Today",
            CounterAccount = "NL01TEST0000000001"
        });
        await _repository.AddAsync(new Transaction
        {
            AccountId = _testAccount.Id,
            CategoryId = _testCategory.Id,
            Date = today.AddDays(-10),
            Amount = 200m,
            Description = "10 days ago",
            CounterAccount = "NL01TEST0000000002"
        });

        // Act
        var result = await _repository.GetByDateRangeAsync(today.AddDays(-5), today);

        // Assert
        Assert.Single(result);
        Assert.Equal("Today", result[0].Description);
    }

    [Fact]
    public async Task AddRangeAsync_ShouldAddMultipleTransactions()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            new Transaction
            {
                AccountId = _testAccount.Id,
                CategoryId = _testCategory.Id,
                Date = DateTime.Today,
                Amount = 100m,
                Description = "Transaction 1",
                CounterAccount = "NL01TEST0000000001"
            },
            new Transaction
            {
                AccountId = _testAccount.Id,
                CategoryId = _testCategory.Id,
                Date = DateTime.Today,
                Amount = 200m,
                Description = "Transaction 2",
                CounterAccount = "NL01TEST0000000002"
            }
        };

        // Act
        var result = await _repository.AddRangeAsync(transactions);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.NotEqual(0, t.Id));
    }

    [Fact]
    public async Task ExistsByHashAsync_ShouldReturnTrue_WhenDuplicateExists()
    {
        // Arrange
        var date = DateTime.Today;
        var amount = 100.50m;
        var description = "Test Transaction";

        await _repository.AddAsync(new Transaction
        {
            AccountId = _testAccount.Id,
            CategoryId = _testCategory.Id,
            Date = date,
            Amount = amount,
            Description = description,
            CounterAccount = "NL01TEST0000000001"
        });

        // Act
        var result = await _repository.ExistsByHashAsync(date, amount, description, _testAccount.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsByHashAsync_ShouldReturnFalse_WhenNoDuplicate()
    {
        // Act
        var result = await _repository.ExistsByHashAsync(DateTime.Today, 100m, "New Transaction", _testAccount.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateTransaction()
    {
        // Arrange
        var transaction = new Transaction
        {
            AccountId = _testAccount.Id,
            CategoryId = _testCategory.Id,
            Date = DateTime.Today,
            Amount = 100m,
            Description = "Original",
            CounterAccount = "NL01TEST0000000001"
        };
        await _repository.AddAsync(transaction);

        // Act
        transaction.Description = "Updated";
        await _repository.UpdateAsync(transaction);

        // Assert
        var result = await _repository.GetByIdAsync(transaction.Id);
        Assert.Equal("Updated", result!.Description);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteTransaction()
    {
        // Arrange
        var transaction = new Transaction
        {
            AccountId = _testAccount.Id,
            CategoryId = _testCategory.Id,
            Date = DateTime.Today,
            Amount = 100m,
            Description = "To Delete",
            CounterAccount = "NL01TEST0000000001"
        };
        await _repository.AddAsync(transaction);

        // Act
        var result = await _repository.DeleteAsync(transaction.Id);

        // Assert
        Assert.True(result);
        Assert.Null(await _repository.GetByIdAsync(transaction.Id));
    }
}
