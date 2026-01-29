using Microsoft.JSInterop;
using System.Text.Json;

namespace LocalFinanceManager.Services;

/// <summary>
/// Service for tracking recent category usage and favorites using browser localStorage.
/// </summary>
public interface IRecentCategoriesService
{
    Task TrackCategoryUsageAsync(Guid categoryId);
    Task<List<Guid>> GetRecentCategoriesAsync(int count = 5);
    Task ToggleFavoriteAsync(Guid categoryId);
    Task<List<Guid>> GetFavoriteCategoriesAsync();
}

public class RecentCategoriesService : IRecentCategoriesService
{
    private const string UsageStorageKey = "recentCategories";
    private const string FavoritesStorageKey = "favoriteCategories";
    private const int MaxTrackedCategories = 20;

    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<RecentCategoriesService> _logger;

    public RecentCategoriesService(IJSRuntime jsRuntime, ILogger<RecentCategoriesService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task TrackCategoryUsageAsync(Guid categoryId)
    {
        try
        {
            var usageData = await GetUsageDataAsync();

            if (usageData.ContainsKey(categoryId))
            {
                usageData[categoryId]++;
            }
            else
            {
                usageData[categoryId] = 1;
            }

            // Trim to top 20 categories if exceeded
            if (usageData.Count > MaxTrackedCategories)
            {
                var sortedEntries = usageData.OrderByDescending(kvp => kvp.Value).Take(MaxTrackedCategories);
                usageData = sortedEntries.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            await SaveUsageDataAsync(usageData);
        }
        catch (JSDisconnectedException ex)
        {
            _logger.LogWarning(ex, "Client disconnected while tracking category usage for categoryId: {CategoryId}", categoryId);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "JSInterop call invalid while tracking category usage for categoryId: {CategoryId}", categoryId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track category usage for categoryId: {CategoryId}", categoryId);
        }
    }

    public async Task<List<Guid>> GetRecentCategoriesAsync(int count = 5)
    {
        try
        {
            var usageData = await GetUsageDataAsync();
            return usageData
                .OrderByDescending(kvp => kvp.Value)
                .Take(count)
                .Select(kvp => kvp.Key)
                .ToList();
        }
        catch (JSDisconnectedException ex)
        {
            _logger.LogWarning(ex, "Client disconnected while getting recent categories");
            return new List<Guid>();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "JSInterop call invalid while getting recent categories");
            return new List<Guid>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent categories");
            return new List<Guid>();
        }
    }

    public async Task ToggleFavoriteAsync(Guid categoryId)
    {
        try
        {
            var favorites = await GetFavoriteCategoriesAsync();

            if (favorites.Contains(categoryId))
            {
                favorites.Remove(categoryId);
            }
            else
            {
                favorites.Add(categoryId);
            }

            await SaveFavoritesAsync(favorites);
        }
        catch (JSDisconnectedException ex)
        {
            _logger.LogWarning(ex, "Client disconnected while toggling favorite for categoryId: {CategoryId}", categoryId);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "JSInterop call invalid while toggling favorite for categoryId: {CategoryId}", categoryId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle favorite for categoryId: {CategoryId}", categoryId);
        }
    }

    public async Task<List<Guid>> GetFavoriteCategoriesAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", FavoritesStorageKey);

            if (string.IsNullOrEmpty(json))
            {
                return new List<Guid>();
            }

            return JsonSerializer.Deserialize<List<Guid>>(json) ?? new List<Guid>();
        }
        catch (JSDisconnectedException ex)
        {
            _logger.LogWarning(ex, "Client disconnected while getting favorite categories");
            return new List<Guid>();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "JSInterop call invalid while getting favorite categories");
            return new List<Guid>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get favorite categories");
            return new List<Guid>();
        }
    }

    private async Task<Dictionary<Guid, int>> GetUsageDataAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", UsageStorageKey);

            if (string.IsNullOrEmpty(json))
            {
                return new Dictionary<Guid, int>();
            }

            return JsonSerializer.Deserialize<Dictionary<Guid, int>>(json) ?? new Dictionary<Guid, int>();
        }
        catch (JSDisconnectedException ex)
        {
            _logger.LogWarning(ex, "Client disconnected while getting usage data from localStorage");
            return new Dictionary<Guid, int>();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "JSInterop call invalid while getting usage data from localStorage");
            return new Dictionary<Guid, int>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get usage data from localStorage");
            return new Dictionary<Guid, int>();
        }
    }

    private async Task SaveUsageDataAsync(Dictionary<Guid, int> usageData)
    {
        try
        {
            var json = JsonSerializer.Serialize(usageData);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", UsageStorageKey, json);
        }
        catch (JSDisconnectedException ex)
        {
            _logger.LogWarning(ex, "Client disconnected while saving usage data");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "JSInterop call invalid while saving usage data");
        }
    }

    private async Task SaveFavoritesAsync(List<Guid> favorites)
    {
        try
        {
            var json = JsonSerializer.Serialize(favorites);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", FavoritesStorageKey, json);
        }
        catch (JSDisconnectedException ex)
        {
            _logger.LogWarning(ex, "Client disconnected while saving favorites");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "JSInterop call invalid while saving favorites");
        }
    }
}
