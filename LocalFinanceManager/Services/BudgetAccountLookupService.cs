using LocalFinanceManager.Configuration;
using LocalFinanceManager.Data.Repositories;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace LocalFinanceManager.Services;

/// <summary>
/// Cached lookup service implementation for budget line to account validation.
/// Uses IMemoryCache with configurable TTL and ICacheKeyTracker for pattern-based invalidation.
/// </summary>
public class BudgetAccountLookupService : IBudgetAccountLookupService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ICacheKeyTracker _cacheKeyTracker;
    private readonly IBudgetLineRepository _budgetLineRepository;
    private readonly CacheOptions _cacheOptions;
    private readonly ILogger<BudgetAccountLookupService> _logger;
    private readonly IDataProtector _accountIdProtector;

    public BudgetAccountLookupService(
        IMemoryCache memoryCache,
        ICacheKeyTracker cacheKeyTracker,
        IBudgetLineRepository budgetLineRepository,
        IOptions<CacheOptions> cacheOptions,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<BudgetAccountLookupService> logger)
    {
        _memoryCache = memoryCache;
        _cacheKeyTracker = cacheKeyTracker;
        _budgetLineRepository = budgetLineRepository;
        _cacheOptions = cacheOptions.Value;
        _logger = logger;
        _accountIdProtector = dataProtectionProvider.CreateProtector("BudgetAccountLookupService.AccountIdCache");
    }

    /// <inheritdoc />
    public async Task<Guid?> GetAccountIdForBudgetLineAsync(Guid budgetLineId)
    {
        // Try cache first - we cache by budget line ID directly
        var cacheKey = $"BudgetLineAccount:{budgetLineId}";

        if (_memoryCache.TryGetValue<string>(cacheKey, out var protectedAccountId))
        {
            if (TryUnprotectGuid(protectedAccountId, out var cachedAccountId))
            {
                _logger.LogDebug("Cache hit for budget line {BudgetLineId}", budgetLineId);
                return cachedAccountId;
            }

            _logger.LogWarning(
                "Failed to unprotect cached account id for budget line {BudgetLineId}. Removing corrupted cache entry.",
                budgetLineId);

            _memoryCache.Remove(cacheKey);
        }

        _logger.LogDebug("Cache miss for budget line {BudgetLineId}, querying database", budgetLineId);

        // Cache miss - query database
        var accountId = await _budgetLineRepository.GetAccountIdForBudgetLineAsync(budgetLineId);

        if (accountId.HasValue)
        {
            // Cache the result
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheOptions.AbsoluteExpirationMinutes),
                SlidingExpiration = TimeSpan.FromMinutes(_cacheOptions.SlidingExpirationMinutes),
                Size = 1,
                Priority = CacheItemPriority.Normal
            };

            _memoryCache.Set(cacheKey, ProtectGuid(accountId.Value), cacheEntryOptions);
            _cacheKeyTracker.AddKey(cacheKey);

            _logger.LogDebug("Cached budget line-account mapping for budget line {BudgetLineId}", budgetLineId);
        }

        return accountId;
    }

    /// <inheritdoc />
    public async Task<Dictionary<Guid, Guid>> GetAccountIdsForBudgetLinesAsync(IEnumerable<Guid> budgetLineIds)
    {
        var budgetLineIdList = budgetLineIds.ToList();
        var result = new Dictionary<Guid, Guid>();
        var uncachedIds = new List<Guid>();

        // Check cache for each budget line
        foreach (var budgetLineId in budgetLineIdList)
        {
            var cacheKey = $"BudgetLineAccount:{budgetLineId}";

            if (_memoryCache.TryGetValue<string>(cacheKey, out var protectedAccountId) &&
                TryUnprotectGuid(protectedAccountId, out var cachedAccountId))
            {
                result[budgetLineId] = cachedAccountId;
            }
            else
            {
                uncachedIds.Add(budgetLineId);
            }
        }

        _logger.LogDebug("Batch lookup: {CacheHits} cache hits, {CacheMisses} cache misses",
            result.Count, uncachedIds.Count);

        // Fetch uncached entries in single database query
        if (uncachedIds.Any())
        {
            var accountMappings = await _budgetLineRepository.GetAccountMappingsAsync(uncachedIds);

            // Cache the results and add to return dictionary
            foreach (var (budgetLineId, accountId) in accountMappings)
            {
                var cacheKey = $"BudgetLineAccount:{budgetLineId}";
                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheOptions.AbsoluteExpirationMinutes),
                    SlidingExpiration = TimeSpan.FromMinutes(_cacheOptions.SlidingExpirationMinutes),
                    Size = 1,
                    Priority = CacheItemPriority.Normal
                };

                _memoryCache.Set(cacheKey, ProtectGuid(accountId), cacheEntryOptions);
                _cacheKeyTracker.AddKey(cacheKey);
                result[budgetLineId] = accountId;
            }
        }

        return result;
    }

    /// <inheritdoc />
    public void InvalidateAccountCache(Guid accountId)
    {
        // Pattern: all budget lines for this account's budget plan
        // Since we cache by BudgetLineId, we need to remove entries that belong to this account
        // This requires a more complex invalidation - we'll clear all cache for safety
        // In a production system, you might maintain a reverse index
        _logger.LogInformation("Invalidating cache for account");

        // For now, clear all cache when account changes
        // Future optimization: maintain AccountId -> BudgetLineIds mapping
        ClearAllCache();
    }

    /// <inheritdoc />
    public void InvalidateCategoryCache(Guid categoryId)
    {
        // When a category's budget plan changes, we need to invalidate all budget lines using that category
        // Since we cache by BudgetLineId, we'll clear all cache for safety
        _logger.LogInformation("Invalidating cache for category {CategoryId}", categoryId);

        // For now, clear all cache when category changes
        // Future optimization: maintain CategoryId -> BudgetLineIds mapping
        ClearAllCache();
    }

    /// <inheritdoc />
    public void ClearAllCache()
    {
        var pattern = "BudgetLineAccount:*";
        var keysToRemove = _cacheKeyTracker.GetKeysMatchingPattern(pattern).ToList();

        _logger.LogInformation("Clearing {Count} cache entries", keysToRemove.Count);

        foreach (var key in keysToRemove)
        {
            _memoryCache.Remove(key);
            _cacheKeyTracker.RemoveKey(key);
        }
    }

    private string ProtectGuid(Guid value)
    {
        return _accountIdProtector.Protect(value.ToString("N"));
    }

    private bool TryUnprotectGuid(string? protectedValue, out Guid value)
    {
        value = Guid.Empty;

        if (string.IsNullOrWhiteSpace(protectedValue))
        {
            return false;
        }

        try
        {
            var unprotected = _accountIdProtector.Unprotect(protectedValue);
            return Guid.TryParseExact(unprotected, "N", out value);
        }
        catch
        {
            _logger.LogWarning("Unable to unprotect cached account mapping entry; cache entry will be ignored.");
            return false;
        }
    }
}
