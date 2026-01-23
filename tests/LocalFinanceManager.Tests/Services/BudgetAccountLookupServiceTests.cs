using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LocalFinanceManager.Configuration;
using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.Models;
using LocalFinanceManager.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace LocalFinanceManager.Tests.Services;

/// <summary>
/// Unit tests for BudgetAccountLookupService caching functionality.
/// </summary>
[TestFixture]
public class BudgetAccountLookupServiceTests
{
    private Mock<IBudgetLineRepository> _budgetLineRepoMock = null!;
    private Mock<ICacheKeyTracker> _cacheKeyTrackerMock = null!;
    private Mock<ILogger<BudgetAccountLookupService>> _loggerMock = null!;
    private IMemoryCache _memoryCache = null!;
    private CacheOptions _cacheOptions = null!;
    private BudgetAccountLookupService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _budgetLineRepoMock = new Mock<IBudgetLineRepository>();
        _cacheKeyTrackerMock = new Mock<ICacheKeyTracker>();
        _loggerMock = new Mock<ILogger<BudgetAccountLookupService>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1000 });
        _cacheOptions = new CacheOptions
        {
            AbsoluteExpirationMinutes = 5,
            SlidingExpirationMinutes = 2,
            SizeLimit = 1000
        };

        var optionsMock = new Mock<IOptions<CacheOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_cacheOptions);

        _service = new BudgetAccountLookupService(
            _memoryCache,
            _cacheKeyTrackerMock.Object,
            _budgetLineRepoMock.Object,
            optionsMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public async Task GetAccountIdForBudgetLineAsync_CacheMiss_QueriesDatabase()
    {
        // Arrange
        var budgetLineId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        _budgetLineRepoMock
            .Setup(x => x.GetAccountIdForBudgetLineAsync(budgetLineId))
            .ReturnsAsync(accountId);

        // Act
        var result = await _service.GetAccountIdForBudgetLineAsync(budgetLineId);

        // Assert
        Assert.That(result, Is.EqualTo(accountId));
        _budgetLineRepoMock.Verify(x => x.GetAccountIdForBudgetLineAsync(budgetLineId), Times.Once);
        _cacheKeyTrackerMock.Verify(x => x.AddKey(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task GetAccountIdForBudgetLineAsync_CacheHit_DoesNotQueryDatabase()
    {
        // Arrange
        var budgetLineId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        _budgetLineRepoMock
            .Setup(x => x.GetAccountIdForBudgetLineAsync(budgetLineId))
            .ReturnsAsync(accountId);

        // First call populates cache
        await _service.GetAccountIdForBudgetLineAsync(budgetLineId);

        // Reset mock to verify second call doesn't hit database
        _budgetLineRepoMock.Reset();

        // Act
        var result = await _service.GetAccountIdForBudgetLineAsync(budgetLineId);

        // Assert
        Assert.That(result, Is.EqualTo(accountId));
        _budgetLineRepoMock.Verify(x => x.GetAccountIdForBudgetLineAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task GetAccountIdsForBudgetLinesAsync_BatchOptimization_SingleDatabaseQuery()
    {
        // Arrange
        var budgetLineIds = Enumerable.Range(0, 50).Select(_ => Guid.NewGuid()).ToList();
        var accountId = Guid.NewGuid();
        var mappings = budgetLineIds.ToDictionary(id => id, _ => accountId);

        _budgetLineRepoMock
            .Setup(x => x.GetAccountMappingsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(mappings);

        // Act
        var result = await _service.GetAccountIdsForBudgetLinesAsync(budgetLineIds);

        // Assert
        Assert.That(result.Count, Is.EqualTo(50));
        _budgetLineRepoMock.Verify(x => x.GetAccountMappingsAsync(It.IsAny<IEnumerable<Guid>>()), Times.Once);
        _cacheKeyTrackerMock.Verify(x => x.AddKey(It.IsAny<string>()), Times.Exactly(50));
    }

    [TearDown]
    public void TearDown()
    {
        _memoryCache?.Dispose();
    }

    [Test]
    public async Task GetAccountIdsForBudgetLinesAsync_PartialCacheHit_OnlyQueriesUncached()
    {
        // Arrange
        var cachedBudgetLineIds = Enumerable.Range(0, 25).Select(_ => Guid.NewGuid()).ToList();
        var uncachedBudgetLineIds = Enumerable.Range(0, 25).Select(_ => Guid.NewGuid()).ToList();
        var allBudgetLineIds = cachedBudgetLineIds.Concat(uncachedBudgetLineIds).ToList();
        var accountId = Guid.NewGuid();

        // Populate cache with first 25
        var cachedMappings = cachedBudgetLineIds.ToDictionary(id => id, _ => accountId);
        _budgetLineRepoMock
            .Setup(x => x.GetAccountMappingsAsync(It.Is<IEnumerable<Guid>>(ids => ids.Count() == 25)))
            .ReturnsAsync(cachedMappings);

        await _service.GetAccountIdsForBudgetLinesAsync(cachedBudgetLineIds);

        // Setup for uncached lookup
        var uncachedMappings = uncachedBudgetLineIds.ToDictionary(id => id, _ => accountId);
        _budgetLineRepoMock
            .Setup(x => x.GetAccountMappingsAsync(It.Is<IEnumerable<Guid>>(ids => ids.Count() == 25 && ids.Any(id => uncachedBudgetLineIds.Contains(id)))))
            .ReturnsAsync(uncachedMappings);

        // Act
        var result = await _service.GetAccountIdsForBudgetLinesAsync(allBudgetLineIds);

        // Assert
        Assert.That(result.Count, Is.EqualTo(50));
        // Should be called twice: once for initial cache population, once for uncached IDs
        _budgetLineRepoMock.Verify(x => x.GetAccountMappingsAsync(It.IsAny<IEnumerable<Guid>>()), Times.Exactly(2));
    }

    [Test]
    public void ClearAllCache_RemovesAllEntries()
    {
        // Arrange
        var budgetLineIds = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToList();
        var cacheKeys = budgetLineIds.Select(id => $"BudgetLineAccount:{id}").ToList();

        _cacheKeyTrackerMock
            .Setup(x => x.GetKeysMatchingPattern("BudgetLineAccount:*"))
            .Returns(cacheKeys);

        // Act
        _service.ClearAllCache();

        // Assert
        _cacheKeyTrackerMock.Verify(x => x.GetKeysMatchingPattern("BudgetLineAccount:*"), Times.Once);
        _cacheKeyTrackerMock.Verify(x => x.RemoveKey(It.IsAny<string>()), Times.Exactly(10));
    }

    [Test]
    public async Task GetAccountIdForBudgetLineAsync_BudgetLineNotFound_ReturnsNull()
    {
        // Arrange
        var budgetLineId = Guid.NewGuid();

        _budgetLineRepoMock
            .Setup(x => x.GetAccountIdForBudgetLineAsync(budgetLineId))
            .ReturnsAsync((Guid?)null);

        // Act
        var result = await _service.GetAccountIdForBudgetLineAsync(budgetLineId);

        // Assert
        Assert.That(result, Is.Null);
        _cacheKeyTrackerMock.Verify(x => x.AddKey(It.IsAny<string>()), Times.Never); // Should not cache null results
    }
}
