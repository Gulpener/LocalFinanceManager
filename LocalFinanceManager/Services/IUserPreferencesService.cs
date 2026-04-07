using LocalFinanceManager.Models;

namespace LocalFinanceManager.Services;

public interface IUserPreferencesService
{
    Task<UserPreferences?> GetAsync(Guid userId);
    Task SetThemeAsync(Guid userId, string theme);
}
