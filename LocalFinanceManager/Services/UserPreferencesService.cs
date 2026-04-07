using LocalFinanceManager.Data;
using LocalFinanceManager.Models;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Services;

public class UserPreferencesService : IUserPreferencesService
{
    private readonly AppDbContext _db;
    private readonly ILogger<UserPreferencesService> _logger;

    public UserPreferencesService(AppDbContext db, ILogger<UserPreferencesService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<UserPreferences?> GetAsync(Guid userId)
    {
        if (userId == Guid.Empty) return null;

        return await _db.UserPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(up => up.UserId == userId);
    }

    public async Task SetThemeAsync(Guid userId, string theme)
    {
        if (userId == Guid.Empty) return;

        var existing = await _db.UserPreferences
            .FirstOrDefaultAsync(up => up.UserId == userId);

        if (existing is null)
        {
            _db.UserPreferences.Add(new UserPreferences
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Theme = theme
            });
        }
        else
        {
            existing.Theme = theme;
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist theme preference for user {UserId}", userId);
        }
    }
}
