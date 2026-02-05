using LocalFinanceManager.Services;
using LocalFinanceManager.DTOs.ML;
using LocalFinanceManager.Configuration;
using LocalFinanceManager.Data;
using LocalFinanceManager.Models;
using LocalFinanceManager.Tests.Fixtures;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace LocalFinanceManager.Tests.Services;

/// <summary>
/// Unit tests for MonitoringService auto-apply statistics and history.
/// </summary>
[TestFixture]
public class MonitoringServiceTests
{
    private Mock<ILogger<MonitoringService>> _loggerMock;
    private AutomationOptions _options;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<MonitoringService>>();
        _options = new AutomationOptions
        {
            UndoRateAlertThreshold = 0.1m, // 10%
            UndoRetentionDays = 30
        };
    }

    [Test]
    public async Task GetAutoApplyStatsAsync_WithNoData_ReturnsZeroStats()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        using var dbContext = factory.CreateContext();
        var optionsMock = Options.Create(_options);
        var service = new MonitoringService(dbContext, optionsMock, _loggerMock.Object);

        // Act
        var stats = await service.GetAutoApplyStatsAsync(windowDays: 7);

        // Assert
        Assert.That(stats, Is.Not.Null);
        Assert.That(stats.TotalAutoApplied, Is.EqualTo(0));
        Assert.That(stats.UndoCount, Is.EqualTo(0));
        Assert.That(stats.UndoRate, Is.EqualTo(0));
        Assert.That(stats.IsUndoRateAboveThreshold, Is.False);
    }

    [Test]
    public async Task GetAutoApplyStatsAsync_WithAutoAppliedTransactions_CalculatesCorrectly()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        using var dbContext = factory.CreateContext();
        var optionsMock = Options.Create(_options);
        var service = new MonitoringService(dbContext, optionsMock, _loggerMock.Object);

        // Seed test data
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Label = "Test Account",
            IBAN = "NL01TEST0123456789",
            Currency = "EUR"
        };
        dbContext.Accounts.Add(account);

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Description = "Test Transaction",
            Amount = 100m,
            Date = DateTime.Now
        };
        dbContext.Transactions.Add(transaction);

        // Add auto-applied audit entries
        for (int i = 0; i < 10; i++)
        {
            var audit = new TransactionAudit
            {
                Id = Guid.NewGuid(),
                TransactionId = transaction.Id,
                ActionType = "Assign",
                IsAutoApplied = true,
                AutoAppliedAt = DateTime.UtcNow.AddDays(-i),
                ChangedAt = DateTime.UtcNow.AddDays(-i),
                Confidence = 0.85f
            };
            dbContext.TransactionAudits.Add(audit);
        }

        await dbContext.SaveChangesAsync();

        // Act
        var stats = await service.GetAutoApplyStatsAsync(windowDays: 7);

        // Assert
        Assert.That(stats.TotalAutoApplied, Is.EqualTo(7)); // Only last 7 days
        Assert.That(stats.UndoCount, Is.EqualTo(0));
        Assert.That(stats.UndoRate, Is.EqualTo(0));
        Assert.That(stats.IsUndoRateAboveThreshold, Is.False);
        Assert.That(stats.AverageConfidence, Is.GreaterThan(0));
    }

    [Test]
    public async Task GetAutoApplyStatsAsync_WithHighUndoRate_TriggersAlert()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        using var dbContext = factory.CreateContext();
        var optionsMock = Options.Create(_options);
        var service = new MonitoringService(dbContext, optionsMock, _loggerMock.Object);

        // Seed test data
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Label = "Test Account",
            IBAN = "NL01TEST0123456789",
            Currency = "EUR"
        };
        dbContext.Accounts.Add(account);

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Description = "Test Transaction",
            Amount = 100m,
            Date = DateTime.Now
        };
        dbContext.Transactions.Add(transaction);

        // Add 10 auto-applied audit entries
        for (int i = 0; i < 10; i++)
        {
            var audit = new TransactionAudit
            {
                Id = Guid.NewGuid(),
                TransactionId = transaction.Id,
                ActionType = "Assign",
                IsAutoApplied = true,
                AutoAppliedAt = DateTime.UtcNow.AddDays(-i),
                ChangedAt = DateTime.UtcNow.AddDays(-i),
                Confidence = 0.85f
            };
            dbContext.TransactionAudits.Add(audit);
        }

        // Add 3 undo operations (30% undo rate > 10% threshold)
        for (int i = 0; i < 3; i++)
        {
            var undoAudit = new TransactionAudit
            {
                Id = Guid.NewGuid(),
                TransactionId = transaction.Id,
                ActionType = "Undo",
                ChangedAt = DateTime.UtcNow.AddDays(-i),
                Reason = "User undid auto-applied assignment"
            };
            dbContext.TransactionAudits.Add(undoAudit);
        }

        await dbContext.SaveChangesAsync();

        // Act
        var stats = await service.GetAutoApplyStatsAsync(windowDays: 30);

        // Assert
        Assert.That(stats.TotalAutoApplied, Is.EqualTo(10));
        Assert.That(stats.UndoCount, Is.EqualTo(3));
        Assert.That(stats.UndoRate, Is.EqualTo(0.3m)); // 30%
        Assert.That(stats.IsUndoRateAboveThreshold, Is.True); // 30% > 10%
    }

    [Test]
    public async Task EstimateAutoApplyCountAsync_WithHighConfidence_ReturnsLowerEstimate()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        using var dbContext = factory.CreateContext();
        var optionsMock = Options.Create(_options);
        var service = new MonitoringService(dbContext, optionsMock, _loggerMock.Object);

        // Seed 50 unassigned transactions
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Label = "Test Account",
            IBAN = "NL01TEST0123456789",
            Currency = "EUR"
        };
        dbContext.Accounts.Add(account);

        for (int i = 0; i < 50; i++)
        {
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                Description = $"Test Transaction {i}",
                Amount = 100m,
                Date = DateTime.Now.AddDays(-i)
            };
            dbContext.Transactions.Add(transaction);
        }

        await dbContext.SaveChangesAsync();

        // Act
        var estimate90 = await service.EstimateAutoApplyCountAsync(0.9f);
        var estimate80 = await service.EstimateAutoApplyCountAsync(0.8f);
        var estimate70 = await service.EstimateAutoApplyCountAsync(0.7f);

        // Assert
        Assert.That(estimate90, Is.LessThan(estimate80)); // Higher confidence = fewer estimates
        Assert.That(estimate80, Is.LessThan(estimate70));
        Assert.That(estimate90, Is.GreaterThan(0)); // Should have some estimates
    }
}
