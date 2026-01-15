using LocalFinanceManager.Configuration;
using LocalFinanceManager.Data;
using LocalFinanceManager.Models;
using LocalFinanceManager.Services;
using LocalFinanceManager.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Moq;

namespace LocalFinanceManager.Tests.Unit;

/// <summary>
/// Unit tests for MonitoringService.
/// Tests auto-apply statistics and undo rate threshold detection.
/// </summary>
public class MonitoringServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ILogger<MonitoringService>> _mockLogger;
    private readonly AutomationOptions _options;
    private readonly MonitoringService _monitoringService;

    public MonitoringServiceTests()
    {
        var contextFactory = new TestDbContextFactory();
        _dbContext = contextFactory.CreateDbContext();

        _mockLogger = new Mock<ILogger<MonitoringService>>();

        _options = new AutomationOptions
        {
            UndoRateAlertThreshold = 0.20m // 20%
        };

        var optionsMock = Options.Create(_options);
        _monitoringService = new MonitoringService(_dbContext, optionsMock, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAutoApplyStatsAsync_WithNoData_ReturnsZeroStats()
    {
        // Act
        var stats = await _monitoringService.GetAutoApplyStatsAsync(7);

        // Assert
        Assert.Equal(0, stats.TotalAutoApplied);
        Assert.Equal(0, stats.TotalUndone);
        Assert.Equal(0, stats.UndoRate);
        Assert.False(stats.IsUndoRateAboveThreshold);
    }

    [Fact]
    public async Task GetAutoApplyStatsAsync_WithAutoAppliedOnly_ReturnsCorrectStats()
    {
        // Arrange
        var transaction1 = CreateTransaction();
        var transaction2 = CreateTransaction();

        var audit1 = CreateAutoApplyAudit(transaction1.Id, 0.90f, daysAgo: 1);
        var audit2 = CreateAutoApplyAudit(transaction2.Id, 0.88f, daysAgo: 2);

        _dbContext.Transactions.AddRange(transaction1, transaction2);
        _dbContext.TransactionAudits.AddRange(audit1, audit2);
        await _dbContext.SaveChangesAsync();

        // Act
        var stats = await _monitoringService.GetAutoApplyStatsAsync(7);

        // Assert
        Assert.Equal(2, stats.TotalAutoApplied);
        Assert.Equal(0, stats.TotalUndone);
        Assert.Equal(0, stats.UndoRate);
        Assert.Equal(0.89m, stats.AverageConfidence); // (0.90 + 0.88) / 2
        Assert.False(stats.IsUndoRateAboveThreshold);
    }

    [Fact]
    public async Task GetAutoApplyStatsAsync_WithUndos_CalculatesUndoRate()
    {
        // Arrange
        var transaction1 = CreateTransaction();
        var transaction2 = CreateTransaction();
        var transaction3 = CreateTransaction();

        var autoApply1 = CreateAutoApplyAudit(transaction1.Id, 0.90f, daysAgo: 1);
        var autoApply2 = CreateAutoApplyAudit(transaction2.Id, 0.88f, daysAgo: 2);
        var autoApply3 = CreateAutoApplyAudit(transaction3.Id, 0.85f, daysAgo: 3);

        var undo1 = CreateUndoAudit(transaction1.Id, daysAgo: 0);

        _dbContext.Transactions.AddRange(transaction1, transaction2, transaction3);
        _dbContext.TransactionAudits.AddRange(autoApply1, autoApply2, autoApply3, undo1);
        await _dbContext.SaveChangesAsync();

        // Act
        var stats = await _monitoringService.GetAutoApplyStatsAsync(7);

        // Assert
        Assert.Equal(3, stats.TotalAutoApplied);
        Assert.Equal(1, stats.TotalUndone);
        Assert.Equal(0.33m, Math.Round(stats.UndoRate, 2)); // 1/3 ≈ 0.33
        Assert.True(stats.IsUndoRateAboveThreshold); // 33% > 20% threshold
    }

    [Fact]
    public async Task GetAutoApplyStatsAsync_WithExactThreshold_TriggesAlert()
    {
        // Arrange
        var transactions = Enumerable.Range(0, 5).Select(_ => CreateTransaction()).ToList();
        var autoApplies = transactions.Select(t => CreateAutoApplyAudit(t.Id, 0.90f, daysAgo: 1)).ToList();
        var undo = CreateUndoAudit(transactions[0].Id, daysAgo: 0); // 1/5 = 20%

        _dbContext.Transactions.AddRange(transactions);
        _dbContext.TransactionAudits.AddRange(autoApplies);
        _dbContext.TransactionAudits.Add(undo);
        await _dbContext.SaveChangesAsync();

        // Act
        var stats = await _monitoringService.GetAutoApplyStatsAsync(7);

        // Assert
        Assert.Equal(5, stats.TotalAutoApplied);
        Assert.Equal(1, stats.TotalUndone);
        Assert.Equal(0.20m, stats.UndoRate); // Exactly at threshold
        Assert.False(stats.IsUndoRateAboveThreshold); // Not above, equal
    }

    [Fact]
    public async Task GetAutoApplyStatsAsync_OutsideWindow_NotIncluded()
    {
        // Arrange
        var transaction1 = CreateTransaction();
        var transaction2 = CreateTransaction();

        var recentAudit = CreateAutoApplyAudit(transaction1.Id, 0.90f, daysAgo: 3);
        var oldAudit = CreateAutoApplyAudit(transaction2.Id, 0.88f, daysAgo: 10); // Outside 7-day window

        _dbContext.Transactions.AddRange(transaction1, transaction2);
        _dbContext.TransactionAudits.AddRange(recentAudit, oldAudit);
        await _dbContext.SaveChangesAsync();

        // Act
        var stats = await _monitoringService.GetAutoApplyStatsAsync(7);

        // Assert
        Assert.Equal(1, stats.TotalAutoApplied); // Only recent one
    }

    [Fact]
    public async Task IsUndoRateAboveThresholdAsync_AboveThreshold_ReturnsTrue()
    {
        // Arrange
        var transactions = Enumerable.Range(0, 10).Select(_ => CreateTransaction()).ToList();
        var autoApplies = transactions.Select(t => CreateAutoApplyAudit(t.Id, 0.90f, daysAgo: 1)).ToList();
        var undos = transactions.Take(3).Select(t => CreateUndoAudit(t.Id, daysAgo: 0)).ToList(); // 30% undo rate

        _dbContext.Transactions.AddRange(transactions);
        _dbContext.TransactionAudits.AddRange(autoApplies);
        _dbContext.TransactionAudits.AddRange(undos);
        await _dbContext.SaveChangesAsync();

        // Act
        var isAboveThreshold = await _monitoringService.IsUndoRateAboveThresholdAsync(7);

        // Assert
        Assert.True(isAboveThreshold); // 30% > 20% threshold
    }

    private Transaction CreateTransaction()
    {
        return new Transaction
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
    }

    private TransactionAudit CreateAutoApplyAudit(Guid transactionId, float confidence, int daysAgo)
    {
        var timestamp = DateTime.UtcNow.AddDays(-daysAgo);
        return new TransactionAudit
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            ActionType = "AutoApply",
            ChangedBy = "AutoApplyService",
            ChangedAt = timestamp,
            IsAutoApplied = true,
            AutoAppliedBy = "AutoApplyService",
            AutoAppliedAt = timestamp,
            Confidence = confidence,
            ModelVersion = 1,
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private TransactionAudit CreateUndoAudit(Guid transactionId, int daysAgo)
    {
        return new TransactionAudit
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            ActionType = "Undo",
            ChangedBy = "UndoService",
            ChangedAt = DateTime.UtcNow.AddDays(-daysAgo),
            Reason = "Reverted auto-applied assignment",
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}
