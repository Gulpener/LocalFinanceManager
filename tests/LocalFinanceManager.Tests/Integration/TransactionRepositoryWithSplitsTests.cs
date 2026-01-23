using LocalFinanceManager.Data;
using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.Models;
using LocalFinanceManager.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LocalFinanceManager.Tests.Integration;

/// <summary>
/// Integration tests for TransactionRepository methods with splits.
/// </summary>
[TestFixture]
public class TransactionRepositoryWithSplitsTests
{
    private TestDbContextFactory _factory = null!;
    private AppDbContext _context = null!;
    private TransactionRepository _repository = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new TestDbContextFactory();
        _context = _factory.CreateContext();
        var logger = NullLogger<TransactionRepository>.Instance;
        _repository = new TransactionRepository(_context, logger);
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task GetAllWithSplitsAsync_LoadsTransactionsWithSplitDetails()
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
            StartingBalance = 1000m
        };
        await _context.Accounts.AddAsync(account);

        var budgetPlan = new BudgetPlan
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Year = 2026,
            Name = "Budget 2026"
        };
        await _context.BudgetPlans.AddAsync(budgetPlan);

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Groceries",
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
            MonthlyAmountsJson = "[300,300,300,300,300,300,300,300,300,300,300,300]"
        };
        await _context.BudgetLines.AddAsync(budgetLine);
        await _context.SaveChangesAsync();

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Date = DateTime.UtcNow,
            Description = "Test Transaction",
            Amount = 50m
        };
        await _context.Transactions.AddAsync(transaction);

        var assignedPart = new TransactionSplit
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            BudgetLineId = budgetLine.Id,
            Amount = 50m,
            Note = "Test assignment"
        };
        await _context.TransactionSplits.AddAsync(assignedPart);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllWithSplitsAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        var txn = result.First();
        Assert.That(txn.AssignedParts, Is.Not.Null);
        Assert.That(txn.AssignedParts, Has.Count.EqualTo(1));

        var split = txn.AssignedParts.First();
        Assert.That(split.BudgetLine, Is.Not.Null);
        Assert.That(split.BudgetLine.Category, Is.Not.Null);
        Assert.That(split.BudgetLine.Category.Name, Is.EqualTo("Groceries"));
    }

    [Test]
    public async Task GetByAccountIdWithSplitsAsync_FiltersTransactionsByAccount()
    {
        // Arrange
        var account1Id = Guid.NewGuid();
        var account2Id = Guid.NewGuid();

        var account1 = new Account
        {
            Id = account1Id,
            Label = "Account 1",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = 1000m
        };

        var account2 = new Account
        {
            Id = account2Id,
            Label = "Account 2",
            Type = AccountType.Savings,
            IBAN = "NL92ABNA0417164301",
            Currency = "EUR",
            StartingBalance = 2000m
        };

        await _context.Accounts.AddRangeAsync(account1, account2);

        var txn1 = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = account1Id,
            Date = DateTime.UtcNow,
            Description = "Transaction 1",
            Amount = 50m
        };

        var txn2 = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = account2Id,
            Date = DateTime.UtcNow,
            Description = "Transaction 2",
            Amount = 75m
        };

        await _context.Transactions.AddRangeAsync(txn1, txn2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByAccountIdWithSplitsAsync(account1Id);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result.First().AccountId, Is.EqualTo(account1Id));
        Assert.That(result.First().Description, Is.EqualTo("Transaction 1"));
    }

    [Test]
    public async Task GetAllWithSplitsAsync_HandlesUnassignedTransactions()
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
            StartingBalance = 1000m
        };
        await _context.Accounts.AddAsync(account);

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Date = DateTime.UtcNow,
            Description = "Unassigned Transaction",
            Amount = 50m
        };
        await _context.Transactions.AddAsync(transaction);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllWithSplitsAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        var txn = result.First();
        Assert.That(txn.AssignedParts, Is.Empty);
    }

    [Test]
    public async Task GetAllWithSplitsAsync_LoadsMultipleSplitsPerTransaction()
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
            StartingBalance = 1000m
        };
        await _context.Accounts.AddAsync(account);

        var budgetPlan = new BudgetPlan
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Year = 2026,
            Name = "Budget 2026"
        };
        await _context.BudgetPlans.AddAsync(budgetPlan);

        var category1 = new Category { Id = Guid.NewGuid(), Name = "Groceries", Type = CategoryType.Expense, BudgetPlanId = budgetPlan.Id };
        var category2 = new Category { Id = Guid.NewGuid(), Name = "Transport", Type = CategoryType.Expense, BudgetPlanId = budgetPlan.Id };
        await _context.Categories.AddRangeAsync(category1, category2);
        await _context.SaveChangesAsync();

        var budgetLine1 = new BudgetLine
        {
            Id = Guid.NewGuid(),
            BudgetPlanId = budgetPlan.Id,
            CategoryId = category1.Id,
            MonthlyAmountsJson = "[300,300,300,300,300,300,300,300,300,300,300,300]"
        };

        var budgetLine2 = new BudgetLine
        {
            Id = Guid.NewGuid(),
            BudgetPlanId = budgetPlan.Id,
            CategoryId = category2.Id,
            MonthlyAmountsJson = "[100,100,100,100,100,100,100,100,100,100,100,100]"
        };
        await _context.BudgetLines.AddRangeAsync(budgetLine1, budgetLine2);
        await _context.SaveChangesAsync();

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Date = DateTime.UtcNow,
            Description = "Split Transaction",
            Amount = 100m
        };
        await _context.Transactions.AddAsync(transaction);

        var assignedPart1 = new TransactionSplit
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            BudgetLineId = budgetLine1.Id,
            Amount = 60m
        };

        var assignedPart2 = new TransactionSplit
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            BudgetLineId = budgetLine2.Id,
            Amount = 40m
        };
        await _context.TransactionSplits.AddRangeAsync(assignedPart1, assignedPart2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllWithSplitsAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        var txn = result.First();
        Assert.That(txn.AssignedParts, Has.Count.EqualTo(2));

        var categories = txn.AssignedParts
            .Select(ap => ap.BudgetLine.Category.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.That(categories, Is.EqualTo(new[] { "Groceries", "Transport" }));
    }
}
