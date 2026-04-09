using Microsoft.JSInterop;

namespace LocalFinanceManager.Services;

public class ThemeService : IThemeService
{
    private readonly IUserPreferencesService _preferencesService;
    private readonly IJSRuntime _js;
    private readonly ILogger<ThemeService> _logger;

    private string _currentTheme = "light";

    public string CurrentTheme => _currentTheme;
    public event Action? ThemeChanged;

    public ThemeService(
        IUserPreferencesService preferencesService,
        IJSRuntime js,
        ILogger<ThemeService> logger)
    {
        _preferencesService = preferencesService;
        _js = js;
        _logger = logger;
    }

    public async Task InitialiseAsync(Guid userId = default)
    {
        string theme;

        try
        {
            if (userId != Guid.Empty)
            {
                var prefs = await _preferencesService.GetAsync(userId);
                if (prefs is not null)
                {
                    // Normalise to a known value; ignore any legacy/unexpected stored strings.
                    theme = prefs.Theme is "light" or "dark" ? prefs.Theme : "light";
                }
                else
                {
                    // No DB preference — fall back to OS preference
                    theme = await GetOsPreferenceAsync();
                }
            }
            else
            {
                // Unauthenticated: use OS preference
                theme = await GetOsPreferenceAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read theme preference; defaulting to light");
            theme = "light";
        }

        _currentTheme = theme;
        await ApplyThemeAsync(theme);
    }

    public async Task ToggleAsync(Guid userId = default)
    {
        var newTheme = _currentTheme == "dark" ? "light" : "dark";
        _currentTheme = newTheme;

        await ApplyThemeAsync(newTheme);

        if (userId != Guid.Empty)
        {
            try
            {
                await _preferencesService.SetThemeAsync(userId, newTheme);
            }
            catch (Exception ex)
            {
                // Persistence failure must not crash the circuit; the theme is still applied locally.
                _logger.LogError(ex, "Failed to persist toggled theme for user {UserId}; change applied locally only", userId);
            }
        }

        ThemeChanged?.Invoke();
    }

    private async Task ApplyThemeAsync(string theme)
    {
        try
        {
            await _js.InvokeVoidAsync("window.theme.set", theme);
        }
        catch (Exception ex)
        {
            // JS interop may fail during pre-render or before the DOM is ready
            _logger.LogDebug(ex, "Could not apply theme via JS interop; will be applied on next render");
        }
    }

    private async Task<string> GetOsPreferenceAsync()
    {
        try
        {
            return await _js.InvokeAsync<string>("window.theme.getOsPreference");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not read OS theme preference; defaulting to light");
            return "light";
        }
    }
}
