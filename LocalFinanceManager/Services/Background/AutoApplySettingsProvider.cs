using System.Text.Json;
using LocalFinanceManager.Configuration;
using LocalFinanceManager.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace LocalFinanceManager.Services.Background;

public interface IAutoApplySettingsProvider
{
    Task<AutoApplyRuntimeSettings> GetSettingsAsync(CancellationToken cancellationToken = default);

    void Invalidate();
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
    private readonly IDataProtector _accountIdsProtector;

    public AutoApplySettingsProvider(
        IServiceScopeFactory scopeFactory,
        IMemoryCache memoryCache,
        IOptions<AutomationOptions> automationOptions,
        IOptions<CacheOptions> cacheOptions,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<AutoApplySettingsProvider> logger)
    {
        _scopeFactory = scopeFactory;
        _memoryCache = memoryCache;
        _automationOptions = automationOptions.Value;
        _cacheOptions = cacheOptions.Value;
        _accountIdsProtector = dataProtectionProvider.CreateProtector("AutomationSettings.AccountIds");
        _logger = logger;
    }

    public async Task<AutoApplyRuntimeSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        return await _memoryCache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheOptions.AbsoluteExpirationMinutes);
            entry.SlidingExpiration = TimeSpan.FromMinutes(_cacheOptions.SlidingExpirationMinutes);
            entry.Size = 1;

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var persistedSettings = await dbContext.AppSettings
                .AsNoTracking()
                .Where(s => !s.IsArchived)
                .FirstOrDefaultAsync(cancellationToken);

            if (persistedSettings == null)
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

            return new AutoApplyRuntimeSettings
            {
                Enabled = persistedSettings.AutoApplyEnabled,
                MinimumConfidence = persistedSettings.MinimumConfidence,
                IntervalMinutes = persistedSettings.IntervalMinutes,
                AccountIds = DeserializeProtectedGuidList(persistedSettings.AccountIdsJson),
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

    private List<Guid> DeserializeGuidList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<Guid>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(json) ?? new List<Guid>();
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Invalid GUID list JSON detected in AppSettings; falling back to empty list");
            return new List<Guid>();
        }
    }

    private List<Guid> DeserializeProtectedGuidList(string? protectedValue)
    {
        if (string.IsNullOrWhiteSpace(protectedValue))
        {
            return new List<Guid>();
        }

        try
        {
            var unprotectedJson = _accountIdsProtector.Unprotect(protectedValue);
            return DeserializeGuidList(unprotectedJson);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to unprotect auto-apply AccountIds; treating automation settings as invalid.");
            throw;
        }
    }
}
