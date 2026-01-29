using Microsoft.JSInterop;
using System.Text.Json;

namespace LocalFinanceManager.Services;

/// <summary>
/// Model for transaction filter state persistence.
/// </summary>
public class FilterState
{
    public string AssignmentStatus { get; set; } = "all";
    public string SuggestionStatus { get; set; } = "all";
    public string DateRange { get; set; } = "all";
    public DateTime? CustomStartDate { get; set; }
    public DateTime? CustomEndDate { get; set; }
    public List<Guid> SelectedCategories { get; set; } = new();
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public Guid? SelectedAccountId { get; set; }
}

/// <summary>
/// Service for persisting transaction filter state in browser localStorage.
/// </summary>
public interface IFilterStateService
{
    Task SaveFiltersAsync(FilterState filters);
    Task<FilterState?> LoadFiltersAsync();
    Task ClearFiltersAsync();
}

public class FilterStateService : IFilterStateService
{
    private const string StorageKey = "transactionFilters";

    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<FilterStateService> _logger;

    public FilterStateService(IJSRuntime jsRuntime, ILogger<FilterStateService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task SaveFiltersAsync(FilterState filters)
    {
        try
        {
            var json = JsonSerializer.Serialize(filters);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save filter state to localStorage");
        }
    }

    public async Task<FilterState?> LoadFiltersAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", StorageKey);

            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<FilterState>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load filter state from localStorage");
            return null;
        }
    }

    public async Task ClearFiltersAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear filter state from localStorage");
        }
    }
}
