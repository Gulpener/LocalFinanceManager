using LocalFinanceManager.Configuration;
using LocalFinanceManager.Data;
using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.Models;
using LocalFinanceManager.Services;
using LocalFinanceManager.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Moq;

namespace LocalFinanceManager.Tests.Unit;

/// <summary>
/// Unit tests for UndoService.
/// Tests undo logic, retention window, and concurrency conflict handling.
/// </summary>
public class UndoServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ITransactionAuditRepository> _mockAuditRepo;
    private readonly Mock<ILogger<UndoService>> _mockLogger;
    private readonly AutomationOptions _options;
    private readonly UndoService _undoService;

    public UndoServiceTests()
    {
        var contextFactory = new TestDbContextFactory();
        _dbContext = contextFactory.CreateDbContext();

        _mockAuditRepo = new Mock<ITransactionAuditRepository>();
        _mockLogger = new Mock<ILogger<UndoService>>();

        _options = new AutomationOptions
        {
            UndoRetentionDays = 30,
            ConfidenceThreshold = 0.85m
        };

        var optionsMock = Options.Create(_options);
        _undoService = new UndoService(_dbContext, _mockAuditRepo.Object, optionsMock, _mockLogger.Object);
    }

    [Fact]
    public async Task UndoAutoApplyAsync_WithValidAutoApply_SuccessfullyUndoes()
    {
        // Arrange
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = 100m,
            Date = DateTime.UtcNow.AddDays(-1),
            Description = "Test transaction",
            AccountId = Guid.NewGuid(),
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var split = new TransactionSplit
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            BudgetLineId = Guid.NewGuid(),
            Amount = 100m,
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
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
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
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
        Assert.True(result.Success);
        Assert.Contains("successfully undone", result.Message);

        // Verify split was removed
        var remainingSplits = await _dbContext.TransactionSplits
            .Where(s => s.TransactionId == transaction.Id)
            .ToListAsync();
        Assert.Empty(remainingSplits);

        // Verify audit entry was created
        _mockAuditRepo.Verify(x => x.AddAsync(It.Is<TransactionAudit>(
            a => a.ActionType == "Undo" && a.TransactionId == transaction.Id
        )), Times.Once);
    }

    [Fact]
    public async Task UndoAutoApplyAsync_OutsideRetentionWindow_ReturnsFalse()
    {
        // Arrange
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = 100m,
            Date = DateTime.UtcNow.AddDays(-1),
            Description = "Test transaction",
            AccountId = Guid.NewGuid(),
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
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
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Transactions.Add(transaction);
        _dbContext.TransactionAudits.Add(autoApplyAudit);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _undoService.UndoAutoApplyAsync(transaction.Id);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("retention window", result.Message);
    }

    [Fact]
    public async Task UndoAutoApplyAsync_WithSubsequentChanges_ReturnsConflict()
    {
        // Arrange
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = 100m,
            Date = DateTime.UtcNow.AddDays(-1),
            Description = "Test transaction",
            AccountId = Guid.NewGuid(),
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var autoApplyAudit = new TransactionAudit
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            ActionType = "AutoApply",
            IsAutoApplied = true,
            AutoAppliedAt = DateTime.UtcNow.AddMinutes(-20),
            ChangedAt = DateTime.UtcNow.AddMinutes(-20),
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var manualChangeAudit = new TransactionAudit
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            ActionType = "Split",
            IsAutoApplied = false,
            ChangedAt = DateTime.UtcNow.AddMinutes(-5), // After auto-apply
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Transactions.Add(transaction);
        _dbContext.TransactionAudits.Add(autoApplyAudit);
        _dbContext.TransactionAudits.Add(manualChangeAudit);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _undoService.UndoAutoApplyAsync(transaction.Id);

        // Assert
        Assert.False(result.Success);
        Assert.True(result.ConflictDetected);
        Assert.Contains("modified after auto-apply", result.Message);
    }

    [Fact]
    public async Task CanUndoAsync_WithValidAutoApply_ReturnsTrue()
    {
        // Arrange
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = 100m,
            Date = DateTime.UtcNow.AddDays(-1),
            Description = "Test transaction",
            AccountId = Guid.NewGuid(),
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var autoApplyAudit = new TransactionAudit
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            IsAutoApplied = true,
            AutoAppliedAt = DateTime.UtcNow.AddMinutes(-10),
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Transactions.Add(transaction);
        _dbContext.TransactionAudits.Add(autoApplyAudit);
        await _dbContext.SaveChangesAsync();

        // Act
        var canUndo = await _undoService.CanUndoAsync(transaction.Id);

        // Assert
        Assert.True(canUndo);
    }

    [Fact]
    public async Task CanUndoAsync_OutsideRetentionWindow_ReturnsFalse()
    {
        // Arrange
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = 100m,
            Date = DateTime.UtcNow.AddDays(-1),
            Description = "Test transaction",
            AccountId = Guid.NewGuid(),
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var autoApplyAudit = new TransactionAudit
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            IsAutoApplied = true,
            AutoAppliedAt = DateTime.UtcNow.AddDays(-35), // Outside window
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Transactions.Add(transaction);
        _dbContext.TransactionAudits.Add(autoApplyAudit);
        await _dbContext.SaveChangesAsync();

        // Act
        var canUndo = await _undoService.CanUndoAsync(transaction.Id);

        // Assert
        Assert.False(canUndo);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}
