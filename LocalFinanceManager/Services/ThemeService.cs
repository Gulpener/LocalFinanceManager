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

        if (userId != Guid.Empty)
        {
            var prefs = await _preferencesService.GetAsync(userId);
            if (prefs is not null)
            {
                theme = prefs.Theme;
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
            await _preferencesService.SetThemeAsync(userId, newTheme);
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
