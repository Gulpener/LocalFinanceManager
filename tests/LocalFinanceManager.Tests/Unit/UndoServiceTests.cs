using LocalFinanceManager.Configuration;
using LocalFinanceManager.Data;
using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.Models;
using LocalFinanceManager.Services;
using LocalFinanceManager.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace LocalFinanceManager.Tests.Unit;

/// <summary>
/// Unit tests for UndoService.
/// Tests undo logic, retention window, and concurrency conflict handling.
/// </summary>
[TestFixture]
public class UndoServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ITransactionAuditRepository> _mockAuditRepo;
    private readonly Mock<ILogger<UndoService>> _mockLogger;
    private readonly AutomationOptions _options;
    private readonly UndoService _undoService;
    private readonly Guid _testAccountId;
    private readonly Guid _testBudgetLineId;

    public UndoServiceTests()
    {
        var contextFactory = new TestDbContextFactory();
        _dbContext = contextFactory.CreateContext();

        _mockAuditRepo = new Mock<ITransactionAuditRepository>();
        _mockLogger = new Mock<ILogger<UndoService>>();

        _options = new AutomationOptions
        {
            UndoRetentionDays = 30,
            ConfidenceThreshold = 0.85m
        };

        var optionsMock = Options.Create(_options);
        _undoService = new UndoService(_dbContext, _mockAuditRepo.Object, optionsMock, _mockLogger.Object);

        // Create test account for foreign key constraint
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Label = "Test Account",
            Type = AccountType.Checking,
            Currency = "EUR",
            IBAN = "NL91ABNA0417164300",
            StartingBalance = 1000m,
            IsArchived = false
        };
        _dbContext.Accounts.Add(account);
        _dbContext.SaveChanges();
        _testAccountId = account.Id;

        // Create test budget line for TransactionSplit foreign key
        var budgetPlan = new BudgetPlan
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Year = DateTime.UtcNow.Year,
            Name = "Test Budget",
            IsArchived = false
        };
        _dbContext.BudgetPlans.Add(budgetPlan);
        _dbContext.SaveChanges();

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Test Category",
            Type = CategoryType.Expense,
            BudgetPlanId = budgetPlan.Id,
            IsArchived = false
        };
        _dbContext.Categories.Add(category);
        _dbContext.SaveChanges();

        var budgetLine = new BudgetLine
        {
            Id = Guid.NewGuid(),
            BudgetPlanId = budgetPlan.Id,
            CategoryId = category.Id,
            MonthlyAmountsJson = "[100,100,100,100,100,100,100,100,100,100,100,100]",
            IsArchived = false
        };
        _dbContext.BudgetLines.Add(budgetLine);
        _dbContext.SaveChanges();
        _testBudgetLineId = budgetLine.Id;
    }

    [SetUp]
    public void SetUp()
    {
        // Clear test data before each test to ensure isolation
        _dbContext.TransactionSplits.RemoveRange(_dbContext.TransactionSplits);
        _dbContext.Transactions.RemoveRange(_dbContext.Transactions);
        _dbContext.TransactionAudits.RemoveRange(_dbContext.TransactionAudits);
        _dbContext.SaveChanges();
    }

    [Test]
    public async Task UndoAutoApplyAsync_WithValidAutoApply_SuccessfullyUndoes()
    {
        // Arrange
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = 100m,
            Date = DateTime.UtcNow.AddDays(-1),
            Description = "Test transaction",
            AccountId = _testAccountId,
            IsArchived = false
        };

        var split = new TransactionSplit
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            BudgetLineId = _testBudgetLineId,
            Amount = 100m,
            IsArchived = false
        };

        transaction.AssignedParts = new List<TransactionSplit> { split };

        var autoApplyAudit = new TransactionAudit
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            ActionType = "AutoApply",
            ChangedBy = "AutoApplyService",
            ChangedAt = DateTime.UtcNow.AddMinutes(-10),
            IsAutoApplied = true,
            AutoAppliedBy = "AutoApplyService",
            AutoAppliedAt = DateTime.UtcNow.AddMinutes(-10),
            Confidence = 0.92f,
            ModelVersion = 1,
            IsArchived = false
        };

        _dbContext.Transactions.Add(transaction);
        _dbContext.TransactionSplits.Add(split);
        _dbContext.TransactionAudits.Add(autoApplyAudit);
        await _dbContext.SaveChangesAsync();

        _mockAuditRepo.Setup(x => x.AddAsync(It.IsAny<TransactionAudit>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _undoService.UndoAutoApplyAsync(transaction.Id);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Message, Does.Contain("successfully undone"));

        // Verify split was removed
        var remainingSplits = await _dbContext.TransactionSplits
            .Where(s => s.TransactionId == transaction.Id)
            .ToListAsync();
        Assert.That(remainingSplits, Is.Empty);

        // Verify audit entry was created
        _mockAuditRepo.Verify(x => x.AddAsync(It.Is<TransactionAudit>(
            a => a.ActionType == "Undo" && a.TransactionId == transaction.Id
        )), Times.Once);
    }

    [Test]
    public async Task UndoAutoApplyAsync_OutsideRetentionWindow_ReturnsFalse()
    {
        // Arrange
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = 100m,
            Date = DateTime.UtcNow.AddDays(-1),
            Description = "Test transaction",
            AccountId = _testAccountId,
            IsArchived = false
        };

        var autoApplyAudit = new TransactionAudit
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            ActionType = "AutoApply",
            ChangedBy = "AutoApplyService",
            ChangedAt = DateTime.UtcNow.AddDays(-35), // Outside 30-day window
            IsAutoApplied = true,
            AutoAppliedBy = "AutoApplyService",
            AutoAppliedAt = DateTime.UtcNow.AddDays(-35),
            Confidence = 0.92f,
            ModelVersion = 1,
            IsArchived = false
        };

        _dbContext.Transactions.Add(transaction);
        _dbContext.TransactionAudits.Add(autoApplyAudit);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _undoService.UndoAutoApplyAsync(transaction.Id);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("retention window"));
    }

    [Test]
    public async Task UndoAutoApplyAsync_WithSubsequentChanges_ReturnsConflict()
    {
        // Arrange
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = 100m,
            Date = DateTime.UtcNow.AddDays(-1),
            Description = "Test transaction",
            AccountId = _testAccountId,
            IsArchived = false
        };

        var autoApplyAudit = new TransactionAudit
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            ActionType = "AutoApply",
            IsAutoApplied = true,
            AutoAppliedAt = DateTime.UtcNow.AddMinutes(-20),
            ChangedAt = DateTime.UtcNow.AddMinutes(-20),
            IsArchived = false
        };

        var manualChangeAudit = new TransactionAudit
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            ActionType = "Split",
            IsAutoApplied = false,
            ChangedAt = DateTime.UtcNow.AddMinutes(-5), // After auto-apply
            IsArchived = false
        };

        _dbContext.Transactions.Add(transaction);
        _dbContext.TransactionAudits.Add(autoApplyAudit);
        _dbContext.TransactionAudits.Add(manualChangeAudit);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _undoService.UndoAutoApplyAsync(transaction.Id);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ConflictDetected, Is.True);
        Assert.That(result.Message, Does.Contain("modified after auto-apply"));
    }

    [Test]
    public async Task CanUndoAsync_WithValidAutoApply_ReturnsTrue()
    {
        // Arrange
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = 100m,
            Date = DateTime.UtcNow.AddDays(-1),
            Description = "Test transaction",
            AccountId = _testAccountId,
            IsArchived = false
        };

        var autoApplyAudit = new TransactionAudit
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            IsAutoApplied = true,
            AutoAppliedAt = DateTime.UtcNow.AddMinutes(-10),
            IsArchived = false
        };

        _dbContext.Transactions.Add(transaction);
        _dbContext.TransactionAudits.Add(autoApplyAudit);
        await _dbContext.SaveChangesAsync();

        // Act
        var canUndo = await _undoService.CanUndoAsync(transaction.Id);

        // Assert
        Assert.That(canUndo, Is.True);
    }

    [Test]
    public async Task CanUndoAsync_OutsideRetentionWindow_ReturnsFalse()
    {
        // Arrange
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = 100m,
            Date = DateTime.UtcNow.AddDays(-1),
            Description = "Test transaction",
            AccountId = _testAccountId,
            IsArchived = false
        };

        var autoApplyAudit = new TransactionAudit
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            IsAutoApplied = true,
            AutoAppliedAt = DateTime.UtcNow.AddDays(-35), // Outside window
            IsArchived = false
        };

        _dbContext.Transactions.Add(transaction);
        _dbContext.TransactionAudits.Add(autoApplyAudit);
        await _dbContext.SaveChangesAsync();

        // Act
        var canUndo = await _undoService.CanUndoAsync(transaction.Id);

        // Assert
        Assert.That(canUndo, Is.False);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}
