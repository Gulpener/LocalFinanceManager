using LocalFinanceManager.Data;
using LocalFinanceManager.Models;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Services;

public class UserPreferencesService : IUserPreferencesService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly AppDbContext _db;
    private readonly ILogger<UserPreferencesService> _logger;

    public UserPreferencesService(IDbContextFactory<AppDbContext> dbFactory, AppDbContext db, ILogger<UserPreferencesService> logger)
    {
        _dbFactory = dbFactory;
        _db = db;
        _logger = logger;
    }

    public async Task<UserPreferences?> GetAsync(Guid userId)
    {
        if (userId == Guid.Empty) return null;

        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.UserPreferences
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
