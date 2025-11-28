using LocalFinanceManager.Domain.Entities;
using LocalFinanceManager.Infrastructure;
using LocalFinanceManager.Infrastructure.Repositories;
using LocalFinanceManager.Application.Services;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests;

public class TransactionSplitTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly EfTransactionRepository _repository;
    private readonly TransactionService _service;
    private readonly Account _testAccount;
    private readonly Category _testCategory;

    public TransactionSplitTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new EfTransactionRepository(_context);
        _service = new TransactionService(_repository);

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
    public async Task AddSplits_ShouldPersistAndAttachToParent()
    {
        // Arrange: create parent transaction
        var parent = new Transaction
        {
            AccountId = _testAccount.Id,
            CategoryId = _testCategory.Id,
            Date = DateTime.Today,
            Amount = 300m,
            Description = "Parent",
            CounterAccount = "NL01TEST0000000001"
        };
        await _repository.AddAsync(parent);

        var splits = new List<TransactionSplit>
        {
            new TransactionSplit { ParentTransactionId = parent.Id, Amount = 100m, CategoryId = _testCategory.Id },
            new TransactionSplit { ParentTransactionId = parent.Id, Amount = 200m, CategoryId = _testCategory.Id }
        };

        // Act
        var added = await _service.AddSplitsAsync(splits);

        // Assert
        Assert.Equal(2, added.Count);

        var loadedParent = await _repository.GetByIdAsync(parent.Id);
        Assert.NotNull(loadedParent);
        Assert.Equal(2, loadedParent!.Splits.Count);
        Assert.Equal(300m, loadedParent.Splits.Sum(s => s.Amount));
    }

    [Fact]
    public async Task AddSplits_ShouldThrow_WhenSumMismatch()
    {
        // Arrange: create parent transaction
        var parent = new Transaction
        {
            AccountId = _testAccount.Id,
            CategoryId = _testCategory.Id,
            Date = DateTime.Today,
            Amount = 300m,
            Description = "Parent",
            CounterAccount = "NL01TEST0000000001"
        };
        await _repository.AddAsync(parent);

        var splits = new List<TransactionSplit>
        {
            new TransactionSplit { ParentTransactionId = parent.Id, Amount = 100m, CategoryId = _testCategory.Id },
            new TransactionSplit { ParentTransactionId = parent.Id, Amount = 50m, CategoryId = _testCategory.Id }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AddSplitsAsync(splits));
    }
}
