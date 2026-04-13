using LocalFinanceManager.DTOs;
using LocalFinanceManager.Models;

namespace LocalFinanceManager.Services;

public interface IUserPreferencesService
{
    Task<UserPreferences?> GetAsync(Guid userId);
    Task SetThemeAsync(Guid userId, string theme);
    Task<UserProfileResponse> GetProfileAsync(Guid userId);
    Task UpdateProfileAsync(Guid userId, string? firstName, string? lastName);
    Task<string?> GetProfileImagePathAsync(Guid userId);
    Task UpdateProfileImagePathAsync(Guid userId, string? imagePath);
}
