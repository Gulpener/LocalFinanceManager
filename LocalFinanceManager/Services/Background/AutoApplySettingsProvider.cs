using System.Text.Json;
using LocalFinanceManager.Configuration;
using LocalFinanceManager.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace LocalFinanceManager.Services.Background;

public interface IAutoApplySettingsProvider
{
    Task<AutoApplyRuntimeSettings> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task<AutoApplyRuntimeSettings> GetSettingsAsync(Guid userId, CancellationToken cancellationToken = default);

    void Invalidate();
    void Invalidate(Guid userId);
}

/// <summary>
/// Loads auto-apply settings from AppSettings with in-memory caching and configuration fallback.
/// </summary>
public sealed class AutoApplySettingsProvider : IAutoApplySettingsProvider
{
    private const string CacheKey = "AutoApplyRuntimeSettings";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _memoryCache;
    private readonly AutomationOptions _automationOptions;
    private readonly CacheOptions _cacheOptions;
    private readonly ILogger<AutoApplySettingsProvider> _logger;

    public AutoApplySettingsProvider(
        IServiceScopeFactory scopeFactory,
        IMemoryCache memoryCache,
        IOptions<AutomationOptions> automationOptions,
        IOptions<CacheOptions> cacheOptions,
        ILogger<AutoApplySettingsProvider> logger)
    {
        _scopeFactory = scopeFactory;
        _memoryCache = memoryCache;
        _automationOptions = automationOptions.Value;
        _cacheOptions = cacheOptions.Value;
        _logger = logger;
    }

    public async Task<AutoApplyRuntimeSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        return await GetSettingsCoreAsync(CacheKey, null, cancellationToken);
    }

    public async Task<AutoApplyRuntimeSettings> GetSettingsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return GetDefaultSettings();
        }

        return await GetSettingsCoreAsync(GetUserCacheKey(userId), userId, cancellationToken);
    }

    private async Task<AutoApplyRuntimeSettings> GetSettingsCoreAsync(string cacheKey, Guid? userId, CancellationToken cancellationToken)
    {
        return await _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheOptions.AbsoluteExpirationMinutes);
            entry.SlidingExpiration = TimeSpan.FromMinutes(_cacheOptions.SlidingExpirationMinutes);
            entry.Size = 1;

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var query = dbContext.AppSettings
                .AsNoTracking()
                .Where(s => !s.IsArchived);

            if (userId.HasValue)
            {
                query = query.Where(s => s.UserId == userId.Value);
            }

            var persistedSettings = await query.FirstOrDefaultAsync(cancellationToken);

            if (persistedSettings == null)
            {
                return GetDefaultSettings();
            }

            return new AutoApplyRuntimeSettings
            {
                Enabled = persistedSettings.AutoApplyEnabled,
                MinimumConfidence = persistedSettings.MinimumConfidence,
                IntervalMinutes = persistedSettings.IntervalMinutes,
                AccountIds = DeserializeGuidList(persistedSettings.AccountIdsJson),
                ExcludedCategoryIds = DeserializeGuidList(persistedSettings.ExcludedCategoryIdsJson).ToHashSet()
            };
        }) ?? new AutoApplyRuntimeSettings
        {
            Enabled = _automationOptions.AutoApplyEnabled,
            MinimumConfidence = (float)_automationOptions.ConfidenceThreshold,
            IntervalMinutes = 15
        };
    }

    public void Invalidate()
    {
        _memoryCache.Remove(CacheKey);
    }

    public void Invalidate(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return;
        }

        _memoryCache.Remove(GetUserCacheKey(userId));
    }

    private static string GetUserCacheKey(Guid userId)
    {
        return $"{CacheKey}:{userId}";
    }

    private AutoApplyRuntimeSettings GetDefaultSettings()
    {
        return new AutoApplyRuntimeSettings
        {
            Enabled = _automationOptions.AutoApplyEnabled,
            MinimumConfidence = (float)_automationOptions.ConfidenceThreshold,
            IntervalMinutes = 15,
            AccountIds = new List<Guid>(),
            ExcludedCategoryIds = new HashSet<Guid>()
        };
    }

    internal static List<Guid> DeserializeGuidList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<Guid>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(json) ?? new List<Guid>();
        }
        catch (JsonException)
        {
            return new List<Guid>();
        }
    }
}
