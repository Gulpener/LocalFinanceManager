using LocalFinanceManager.Data;
using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.Models;
using LocalFinanceManager.Services;
using LocalFinanceManager.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using Moq;

namespace LocalFinanceManager.Tests.Integration;

/// <summary>
/// Integration tests for transaction assignment operations.
/// </summary>
[TestFixture]
public class TransactionAssignmentIntegrationTests
{
    private TestDbContextFactory _factory = null!;
    private AppDbContext _context = null!;
    private ITransactionRepository _transactionRepository = null!;
    private ITransactionSplitRepository _splitRepository = null!;
    private ITransactionAuditRepository _auditRepository = null!;
    private ITransactionAssignmentService _assignmentService = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new TestDbContextFactory();
        _context = _factory.CreateContext();
        
        var transactionLogger = new Mock<ILogger<TransactionRepository>>();
        _transactionRepository = new TransactionRepository(_context, transactionLogger.Object);
        _splitRepository = new TransactionSplitRepository(_context);
        _auditRepository = new TransactionAuditRepository(_context);
        
        var assignmentLogger = new Mock<ILogger<TransactionAssignmentService>>();
        _assignmentService = new TransactionAssignmentService(
            _transactionRepository,
            _splitRepository,
            _auditRepository,
            assignmentLogger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task AssignTransaction_ValidRequest_ShouldCreateSplit()
    {
        // Arrange
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Label = "Test Account",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = 1000m
        };
        await _context.Accounts.AddAsync(account);

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Amount = -100.00m,
            Date = DateTime.UtcNow,
            Description = "Test Transaction"
        };
        await _context.Transactions.AddAsync(transaction);
        await _context.SaveChangesAsync();

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Test Category",
            Type = CategoryType.Expense
        };
        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();

        var request = new AssignTransactionRequest
        {
            CategoryId = category.Id,
            Note = "Test assignment"
        };

        // Act
        var result = await _assignmentService.AssignTransactionAsync(transaction.Id, request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(transaction.Id));

        var splits = await _splitRepository.GetByTransactionIdAsync(transaction.Id);
        Assert.That(splits.Count, Is.EqualTo(1));
        Assert.That(splits[0].CategoryId, Is.EqualTo(category.Id));
        Assert.That(splits[0].Amount, Is.EqualTo(100.00m)); // Absolute value
        Assert.That(splits[0].Note, Is.EqualTo("Test assignment"));
    }

    [Test]
    public async Task SplitTransaction_ValidSplits_ShouldCreateMultipleSplits()
    {
        // Arrange
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Label = "Test Account",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = 1000m
        };
        await _context.Accounts.AddAsync(account);

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Amount = -100.00m,
            Date = DateTime.UtcNow,
            Description = "Test Transaction"
        };
        await _context.Transactions.AddAsync(transaction);
        await _context.SaveChangesAsync();

        var category1 = new Category { Id = Guid.NewGuid(), Name = "Category 1", Type = CategoryType.Expense };
        var category2 = new Category { Id = Guid.NewGuid(), Name = "Category 2", Type = CategoryType.Expense };
        await _context.Categories.AddRangeAsync(category1, category2);
        await _context.SaveChangesAsync();

        var request = new SplitTransactionRequest
        {
            Splits = new List<SplitAllocationDto>
            {
                new SplitAllocationDto { CategoryId = category1.Id, Amount = 60.00m, Note = "Part 1" },
                new SplitAllocationDto { CategoryId = category2.Id, Amount = 40.00m, Note = "Part 2" }
            }
        };

        // Act
        var result = await _assignmentService.SplitTransactionAsync(transaction.Id, request);

        // Assert
        Assert.That(result, Is.Not.Null);

        var splits = await _splitRepository.GetByTransactionIdAsync(transaction.Id);
        Assert.That(splits.Count, Is.EqualTo(2));
        Assert.That(splits.Any(s => s.CategoryId == category1.Id && s.Amount == 60.00m), Is.True);
        Assert.That(splits.Any(s => s.CategoryId == category2.Id && s.Amount == 40.00m), Is.True);
    }

    [Test]
    public async Task SplitTransaction_InvalidSum_ShouldThrowException()
    {
        // Arrange
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Label = "Test Account",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = 1000m
        };
        await _context.Accounts.AddAsync(account);

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Amount = -100.00m,
            Date = DateTime.UtcNow,
            Description = "Test Transaction"
        };
        await _context.Transactions.AddAsync(transaction);
        await _context.SaveChangesAsync();

        var category = new Category { Id = Guid.NewGuid(), Name = "Category", Type = CategoryType.Expense };
        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();

        var request = new SplitTransactionRequest
        {
            Splits = new List<SplitAllocationDto>
            {
                new SplitAllocationDto { CategoryId = category.Id, Amount = 50.00m },
                new SplitAllocationDto { CategoryId = category.Id, Amount = 40.00m } // Sum = 90, not 100
            }
        };

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _assignmentService.SplitTransactionAsync(transaction.Id, request));
        
        Assert.That(exception!.Message, Does.Contain("must equal transaction amount"));
    }

    [Test]
    public async Task SplitTransaction_WithinRoundingTolerance_ShouldSucceed()
    {
        // Arrange
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Label = "Test Account",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = 1000m
        };
        await _context.Accounts.AddAsync(account);

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Amount = -100.00m,
            Date = DateTime.UtcNow,
            Description = "Test Transaction"
        };
        await _context.Transactions.AddAsync(transaction);
        await _context.SaveChangesAsync();

        var category = new Category { Id = Guid.NewGuid(), Name = "Category", Type = CategoryType.Expense };
        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();

        var request = new SplitTransactionRequest
        {
            Splits = new List<SplitAllocationDto>
            {
                new SplitAllocationDto { CategoryId = category.Id, Amount = 50.00m },
                new SplitAllocationDto { CategoryId = category.Id, Amount = 50.005m } // Sum = 100.005 (within 0.01 tolerance)
            }
        };

        // Act
        var result = await _assignmentService.SplitTransactionAsync(transaction.Id, request);

        // Assert
        Assert.That(result, Is.Not.Null);
        var splits = await _splitRepository.GetByTransactionIdAsync(transaction.Id);
        Assert.That(splits.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task BulkAssignTransactions_ValidRequest_ShouldAssignAll()
    {
        // Arrange
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Label = "Test Account",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = 1000m
        };
        await _context.Accounts.AddAsync(account);

        var transaction1 = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Amount = -50.00m,
            Date = DateTime.UtcNow,
            Description = "Transaction 1"
        };
        var transaction2 = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Amount = -75.00m,
            Date = DateTime.UtcNow,
            Description = "Transaction 2"
        };
        await _context.Transactions.AddRangeAsync(transaction1, transaction2);
        await _context.SaveChangesAsync();

        var category = new Category { Id = Guid.NewGuid(), Name = "Category", Type = CategoryType.Expense };
        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();

        var request = new BulkAssignTransactionsRequest
        {
            TransactionIds = new List<Guid> { transaction1.Id, transaction2.Id },
            CategoryId = category.Id,
            Note = "Bulk assignment"
        };

        // Act
        var result = await _assignmentService.BulkAssignTransactionsAsync(request);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.AssignedCount, Is.EqualTo(2));
        Assert.That(result.FailedCount, Is.EqualTo(0));

        var splits1 = await _splitRepository.GetByTransactionIdAsync(transaction1.Id);
        var splits2 = await _splitRepository.GetByTransactionIdAsync(transaction2.Id);
        Assert.That(splits1.Count, Is.EqualTo(1));
        Assert.That(splits2.Count, Is.EqualTo(1));
        Assert.That(splits1[0].CategoryId, Is.EqualTo(category.Id));
        Assert.That(splits2[0].CategoryId, Is.EqualTo(category.Id));
    }

    [Test]
    public async Task AssignTransaction_ShouldCreateAuditEntry()
    {
        // Arrange
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Label = "Test Account",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = 1000m
        };
        await _context.Accounts.AddAsync(account);

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Amount = -100.00m,
            Date = DateTime.UtcNow,
            Description = "Test Transaction"
        };
        await _context.Transactions.AddAsync(transaction);
        await _context.SaveChangesAsync();

        var category = new Category { Id = Guid.NewGuid(), Name = "Category", Type = CategoryType.Expense };
        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();

        var request = new AssignTransactionRequest
        {
            CategoryId = category.Id,
            Note = "Test assignment"
        };

        // Act
        await _assignmentService.AssignTransactionAsync(transaction.Id, request);

        // Assert
        var audits = await _auditRepository.GetByTransactionIdAsync(transaction.Id);
        Assert.That(audits.Count, Is.EqualTo(1));
        Assert.That(audits[0].ActionType, Is.EqualTo("Assign"));
        Assert.That(audits[0].TransactionId, Is.EqualTo(transaction.Id));
        Assert.That(audits[0].AfterState, Is.Not.Null);
    }

    [Test]
    public async Task UndoAssignment_ShouldClearSplits()
    {
        // Arrange
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Label = "Test Account",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = 1000m
        };
        await _context.Accounts.AddAsync(account);

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Amount = -100.00m,
            Date = DateTime.UtcNow,
            Description = "Test Transaction"
        };
        await _context.Transactions.AddAsync(transaction);
        await _context.SaveChangesAsync();

        var category = new Category { Id = Guid.NewGuid(), Name = "Category", Type = CategoryType.Expense };
        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();

        var assignRequest = new AssignTransactionRequest
        {
            CategoryId = category.Id,
            Note = "Test assignment"
        };
        await _assignmentService.AssignTransactionAsync(transaction.Id, assignRequest);

        var undoRequest = new UndoAssignmentRequest
        {
            TransactionId = transaction.Id
        };

        // Act
        await _assignmentService.UndoAssignmentAsync(undoRequest);

        // Assert
        var splits = await _splitRepository.GetByTransactionIdAsync(transaction.Id);
        Assert.That(splits.Count, Is.EqualTo(0));

        var audits = await _auditRepository.GetByTransactionIdAsync(transaction.Id);
        Assert.That(audits.Count, Is.EqualTo(2)); // Assign + Undo
        Assert.That(audits[0].ActionType, Is.EqualTo("Undo")); // Most recent first
    }
}
