namespace LocalFinanceManager.Services;

public interface IThemeService
{
    string CurrentTheme { get; }
    Task InitialiseAsync(Guid userId = default);
    Task ToggleAsync(Guid userId = default);
    event Action? ThemeChanged;
}
