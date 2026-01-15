using LocalFinanceManager.Configuration;
using LocalFinanceManager.Data;
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
/// Unit tests for MonitoringService.
/// Tests auto-apply statistics and undo rate threshold detection.
/// </summary>
[TestFixture]
public class MonitoringServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ILogger<MonitoringService>> _mockLogger;
    private readonly AutomationOptions _options;
    private readonly MonitoringService _monitoringService;

    public MonitoringServiceTests()
    {
        var contextFactory = new TestDbContextFactory();
        _dbContext = contextFactory.CreateContext();

        _mockLogger = new Mock<ILogger<MonitoringService>>();

        _options = new AutomationOptions
        {
            UndoRateAlertThreshold = 0.20m // 20%
        };

        var optionsMock = Options.Create(_options);
        _monitoringService = new MonitoringService(_dbContext, optionsMock, _mockLogger.Object);
    }

    [Test]
    public async Task GetAutoApplyStatsAsync_WithNoData_ReturnsZeroStats()
    {
        // Act
        var stats = await _monitoringService.GetAutoApplyStatsAsync(7);

        // Assert
        Assert.That(stats.TotalAutoApplied, Is.EqualTo(0));
        Assert.That(stats.TotalUndone, Is.EqualTo(0));
        Assert.That(stats.UndoRate, Is.EqualTo(0));
        Assert.That(stats.IsUndoRateAboveThreshold, Is.False);
    }

    [Test]
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
        Assert.That(stats.TotalAutoApplied, Is.EqualTo(2));
        Assert.That(stats.TotalUndone, Is.EqualTo(0));
        Assert.That(stats.UndoRate, Is.EqualTo(0));
        Assert.That(stats.AverageConfidence, Is.EqualTo(0.89m)); // (0.90 + 0.88) / 2
        Assert.That(stats.IsUndoRateAboveThreshold, Is.False);
    }

    [Test]
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
        Assert.That(stats.TotalAutoApplied, Is.EqualTo(3));
        Assert.That(stats.TotalUndone, Is.EqualTo(1));
        Assert.That(Math.Round(stats.UndoRate, 2), Is.EqualTo(0.33m)); // 1/3 ≈ 0.33
        Assert.That(stats.IsUndoRateAboveThreshold, Is.True); // 33% > 20% threshold
    }

    [Test]
    public async Task GetAutoApplyStatsAsync_WithExactThreshold_TriggersAlert()
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
        Assert.That(stats.TotalAutoApplied, Is.EqualTo(5));
        Assert.That(stats.TotalUndone, Is.EqualTo(1));
        Assert.That(stats.UndoRate, Is.EqualTo(0.20m)); // Exactly at threshold
        Assert.That(stats.IsUndoRateAboveThreshold, Is.False); // Not above, equal
    }

    [Test]
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
        Assert.That(stats.TotalAutoApplied, Is.EqualTo(1)); // Only recent one
    }

    [Test]
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
        Assert.That(isAboveThreshold, Is.True); // 30% > 20% threshold
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
