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
    private IBudgetLineRepository _budgetLineRepository = null!;
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
        var budgetLineLogger = new Mock<ILogger<Repository<BudgetLine>>>();
        _budgetLineRepository = new BudgetLineRepository(_context, budgetLineLogger.Object);

        var assignmentLogger = new Mock<ILogger<TransactionAssignmentService>>();
        _assignmentService = new TransactionAssignmentService(
            _transactionRepository,
            _splitRepository,
            _auditRepository,
            _budgetLineRepository,
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

        var budgetPlan = new BudgetPlan
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Year = DateTime.UtcNow.Year,
            Name = "Test Budget",
            IsArchived = false
        };
        await _context.BudgetPlans.AddAsync(budgetPlan);

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
            Type = CategoryType.Expense,
            BudgetPlanId = budgetPlan.Id
        };
        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();

        var budgetLine = new BudgetLine
        {
            Id = Guid.NewGuid(),
            BudgetPlanId = budgetPlan.Id,
            CategoryId = category.Id,
            MonthlyAmountsJson = System.Text.Json.JsonSerializer.Serialize(new decimal[12])
        };
        await _context.BudgetLines.AddAsync(budgetLine);
        await _context.SaveChangesAsync();

        var request = new AssignTransactionRequest
        {
            BudgetLineId = budgetLine.Id,
            Note = "Test assignment"
        };

        // Act
        var result = await _assignmentService.AssignTransactionAsync(transaction.Id, request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(transaction.Id));

        var splits = await _splitRepository.GetByTransactionIdAsync(transaction.Id);
        Assert.That(splits.Count, Is.EqualTo(1));
        Assert.That(splits[0].BudgetLineId, Is.EqualTo(budgetLine.Id));
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

        var budgetPlan = new BudgetPlan
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Year = DateTime.UtcNow.Year,
            Name = "Test Budget",
            IsArchived = false
        };
        await _context.BudgetPlans.AddAsync(budgetPlan);

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

        var category1 = new Category { Id = Guid.NewGuid(), Name = "Category 1", Type = CategoryType.Expense, BudgetPlanId = budgetPlan.Id };
        var category2 = new Category { Id = Guid.NewGuid(), Name = "Category 2", Type = CategoryType.Expense, BudgetPlanId = budgetPlan.Id };
        await _context.Categories.AddRangeAsync(category1, category2);
        await _context.SaveChangesAsync();

        var budgetLine1 = new BudgetLine
        {
            Id = Guid.NewGuid(),
            BudgetPlanId = budgetPlan.Id,
            CategoryId = category1.Id,
            MonthlyAmountsJson = System.Text.Json.JsonSerializer.Serialize(new decimal[12])
        };
        var budgetLine2 = new BudgetLine
        {
            Id = Guid.NewGuid(),
            BudgetPlanId = budgetPlan.Id,
            CategoryId = category2.Id,
            MonthlyAmountsJson = System.Text.Json.JsonSerializer.Serialize(new decimal[12])
        };
        await _context.BudgetLines.AddRangeAsync(budgetLine1, budgetLine2);
        await _context.SaveChangesAsync();

        var request = new SplitTransactionRequest
        {
            Splits = new List<SplitAllocationDto>
            {
                new SplitAllocationDto { BudgetLineId = budgetLine1.Id, Amount = 60.00m, Note = "Part 1" },
                new SplitAllocationDto { BudgetLineId = budgetLine2.Id, Amount = 40.00m, Note = "Part 2" }
            }
        };

        // Act
        var result = await _assignmentService.SplitTransactionAsync(transaction.Id, request);

        // Assert
        Assert.That(result, Is.Not.Null);

        var splits = await _splitRepository.GetByTransactionIdAsync(transaction.Id);
        Assert.That(splits.Count, Is.EqualTo(2));
        Assert.That(splits.Any(s => s.BudgetLineId == budgetLine1.Id && s.Amount == 60.00m), Is.True);
        Assert.That(splits.Any(s => s.BudgetLineId == budgetLine2.Id && s.Amount == 40.00m), Is.True);
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

        var budgetPlan = new BudgetPlan
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Year = DateTime.UtcNow.Year,
            Name = "Test Budget",
            IsArchived = false
        };
        await _context.BudgetPlans.AddAsync(budgetPlan);

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

        var category = new Category { Id = Guid.NewGuid(), Name = "Category", Type = CategoryType.Expense, BudgetPlanId = budgetPlan.Id };
        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();

        var budgetLine = new BudgetLine
        {
            Id = Guid.NewGuid(),
            BudgetPlanId = budgetPlan.Id,
            CategoryId = category.Id,
            MonthlyAmountsJson = System.Text.Json.JsonSerializer.Serialize(new decimal[12])
        };
        await _context.BudgetLines.AddAsync(budgetLine);
        await _context.SaveChangesAsync();

        var request = new SplitTransactionRequest
        {
            Splits = new List<SplitAllocationDto>
            {
                new SplitAllocationDto { BudgetLineId = budgetLine.Id, Amount = 50.00m },
                new SplitAllocationDto { BudgetLineId = budgetLine.Id, Amount = 40.00m } // Sum = 90, not 100
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

        var budgetPlan = new BudgetPlan
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Year = DateTime.UtcNow.Year,
            Name = "Test Budget",
            IsArchived = false
        };
        await _context.BudgetPlans.AddAsync(budgetPlan);

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

        var category = new Category { Id = Guid.NewGuid(), Name = "Category", Type = CategoryType.Expense, BudgetPlanId = budgetPlan.Id };
        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();

        var budgetLine = new BudgetLine
        {
            Id = Guid.NewGuid(),
            BudgetPlanId = budgetPlan.Id,
            CategoryId = category.Id,
            MonthlyAmountsJson = System.Text.Json.JsonSerializer.Serialize(new decimal[12])
        };
        await _context.BudgetLines.AddAsync(budgetLine);
        await _context.SaveChangesAsync();

        var request = new SplitTransactionRequest
        {
            Splits = new List<SplitAllocationDto>
            {
                new SplitAllocationDto { BudgetLineId = budgetLine.Id, Amount = 50.00m },
                new SplitAllocationDto { BudgetLineId = budgetLine.Id, Amount = 50.005m } // Sum = 100.005 (within 0.01 tolerance)
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

        var budgetPlan = new BudgetPlan
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Year = DateTime.UtcNow.Year,
            Name = "Test Budget",
            IsArchived = false
        };
        await _context.BudgetPlans.AddAsync(budgetPlan);

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

        var category = new Category { Id = Guid.NewGuid(), Name = "Category", Type = CategoryType.Expense, BudgetPlanId = budgetPlan.Id };
        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();

        var budgetLine = new BudgetLine
        {
            Id = Guid.NewGuid(),
            BudgetPlanId = budgetPlan.Id,
            CategoryId = category.Id,
            MonthlyAmountsJson = System.Text.Json.JsonSerializer.Serialize(new decimal[12])
        };
        await _context.BudgetLines.AddAsync(budgetLine);
        await _context.SaveChangesAsync();

        var request = new BulkAssignTransactionsRequest
        {
            TransactionIds = new List<Guid> { transaction1.Id, transaction2.Id },
            BudgetLineId = budgetLine.Id,
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
        Assert.That(splits1[0].BudgetLineId, Is.EqualTo(budgetLine.Id));
        Assert.That(splits2[0].BudgetLineId, Is.EqualTo(budgetLine.Id));
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

        var budgetPlan = new BudgetPlan
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Year = DateTime.UtcNow.Year,
            Name = "Test Budget",
            IsArchived = false
        };
        await _context.BudgetPlans.AddAsync(budgetPlan);

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

        var category = new Category { Id = Guid.NewGuid(), Name = "Category", Type = CategoryType.Expense, BudgetPlanId = budgetPlan.Id };
        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();

        var budgetLine = new BudgetLine
        {
            Id = Guid.NewGuid(),
            BudgetPlanId = budgetPlan.Id,
            CategoryId = category.Id,
            MonthlyAmountsJson = System.Text.Json.JsonSerializer.Serialize(new decimal[12])
        };
        await _context.BudgetLines.AddAsync(budgetLine);
        await _context.SaveChangesAsync();

        var request = new AssignTransactionRequest
        {
            BudgetLineId = budgetLine.Id,
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

        var budgetPlan = new BudgetPlan
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Year = DateTime.UtcNow.Year,
            Name = "Test Budget",
            IsArchived = false
        };
        await _context.BudgetPlans.AddAsync(budgetPlan);

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

        var category = new Category { Id = Guid.NewGuid(), Name = "Category", Type = CategoryType.Expense, BudgetPlanId = budgetPlan.Id };
        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();

        var budgetLine = new BudgetLine
        {
            Id = Guid.NewGuid(),
            BudgetPlanId = budgetPlan.Id,
            CategoryId = category.Id,
            MonthlyAmountsJson = System.Text.Json.JsonSerializer.Serialize(new decimal[12])
        };
        await _context.BudgetLines.AddAsync(budgetLine);
        await _context.SaveChangesAsync();

        var assignRequest = new AssignTransactionRequest
        {
            BudgetLineId = budgetLine.Id,
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

    [Test]
    public async Task AssignTransaction_CrossYearAssignment_ShouldThrowException()
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

        var budgetPlan2025 = new BudgetPlan
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Year = 2025,
            Name = "Budget 2025",
            IsArchived = false
        };
        await _context.BudgetPlans.AddAsync(budgetPlan2025);

        // Transaction from 2026
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Amount = -100.00m,
            Date = new DateTime(2026, 1, 15),
            Description = "Transaction from 2026"
        };
        await _context.Transactions.AddAsync(transaction);
        await _context.SaveChangesAsync();

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Test Category",
            Type = CategoryType.Expense,
            BudgetPlanId = budgetPlan2025.Id
        };
        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();

        var budgetLine = new BudgetLine
        {
            Id = Guid.NewGuid(),
            BudgetPlanId = budgetPlan2025.Id,
            CategoryId = category.Id,
            MonthlyAmountsJson = System.Text.Json.JsonSerializer.Serialize(new decimal[12])
        };
        await _context.BudgetLines.AddAsync(budgetLine);
        await _context.SaveChangesAsync();

        var request = new AssignTransactionRequest
        {
            BudgetLineId = budgetLine.Id,
            Note = "Cross-year assignment"
        };

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _assignmentService.AssignTransactionAsync(transaction.Id, request));

        Assert.That(exception!.Message, Does.Contain("Cannot assign 2026 transaction to 2025 budget plan"));
    }

    [Test]
    public async Task AssignTransaction_SameYearAssignment_ShouldSucceed()
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

        var budgetPlan2025 = new BudgetPlan
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Year = 2025,
            Name = "Budget 2025",
            IsArchived = false
        };
        await _context.BudgetPlans.AddAsync(budgetPlan2025);

        // Transaction from 2025
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Amount = -100.00m,
            Date = new DateTime(2025, 12, 31),
            Description = "Transaction from 2025"
        };
        await _context.Transactions.AddAsync(transaction);
        await _context.SaveChangesAsync();

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Test Category",
            Type = CategoryType.Expense,
            BudgetPlanId = budgetPlan2025.Id
        };
        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();

        var budgetLine = new BudgetLine
        {
            Id = Guid.NewGuid(),
            BudgetPlanId = budgetPlan2025.Id,
            CategoryId = category.Id,
            MonthlyAmountsJson = System.Text.Json.JsonSerializer.Serialize(new decimal[12])
        };
        await _context.BudgetLines.AddAsync(budgetLine);
        await _context.SaveChangesAsync();

        var request = new AssignTransactionRequest
        {
            BudgetLineId = budgetLine.Id,
            Note = "Same year assignment"
        };

        // Act
        var result = await _assignmentService.AssignTransactionAsync(transaction.Id, request);

        // Assert
        Assert.That(result, Is.Not.Null);
        var splits = await _splitRepository.GetByTransactionIdAsync(transaction.Id);
        Assert.That(splits.Count, Is.EqualTo(1));
        Assert.That(splits[0].BudgetLineId, Is.EqualTo(budgetLine.Id));
    }

    [Test]
    public async Task SplitTransaction_CrossYearSplit_ShouldThrowException()
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

        var budgetPlan2025 = new BudgetPlan
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Year = 2025,
            Name = "Budget 2025",
            IsArchived = false
        };
        var budgetPlan2026 = new BudgetPlan
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Year = 2026,
            Name = "Budget 2026",
            IsArchived = false
        };
        await _context.BudgetPlans.AddRangeAsync(budgetPlan2025, budgetPlan2026);

        // Transaction from 2026
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Amount = -100.00m,
            Date = new DateTime(2026, 1, 15),
            Description = "Transaction from 2026"
        };
        await _context.Transactions.AddAsync(transaction);
        await _context.SaveChangesAsync();

        var category2025 = new Category { Id = Guid.NewGuid(), Name = "Category 2025", Type = CategoryType.Expense, BudgetPlanId = budgetPlan2025.Id };
        var category2026 = new Category { Id = Guid.NewGuid(), Name = "Category 2026", Type = CategoryType.Expense, BudgetPlanId = budgetPlan2026.Id };
        await _context.Categories.AddRangeAsync(category2025, category2026);
        await _context.SaveChangesAsync();

        var budgetLine2025 = new BudgetLine
        {
            Id = Guid.NewGuid(),
            BudgetPlanId = budgetPlan2025.Id,
            CategoryId = category2025.Id,
            MonthlyAmountsJson = System.Text.Json.JsonSerializer.Serialize(new decimal[12])
        };
        var budgetLine2026 = new BudgetLine
        {
            Id = Guid.NewGuid(),
            BudgetPlanId = budgetPlan2026.Id,
            CategoryId = category2026.Id,
            MonthlyAmountsJson = System.Text.Json.JsonSerializer.Serialize(new decimal[12])
        };
        await _context.BudgetLines.AddRangeAsync(budgetLine2025, budgetLine2026);
        await _context.SaveChangesAsync();

        var request = new SplitTransactionRequest
        {
            Splits = new List<SplitAllocationDto>
            {
                new SplitAllocationDto { BudgetLineId = budgetLine2025.Id, Amount = 60.00m, Note = "Part 2025" },
                new SplitAllocationDto { BudgetLineId = budgetLine2026.Id, Amount = 40.00m, Note = "Part 2026" }
            }
        };

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _assignmentService.SplitTransactionAsync(transaction.Id, request));

        Assert.That(exception!.Message, Does.Contain("Cannot assign 2026 transaction to 2025 budget plan"));
    }
}
