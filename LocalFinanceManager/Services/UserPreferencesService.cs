using LocalFinanceManager.Data;
using LocalFinanceManager.DTOs;
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
            .FirstOrDefaultAsync(up => up.UserId == userId && !up.IsArchived);
    }

    public async Task SetThemeAsync(Guid userId, string theme)
    {
        if (userId == Guid.Empty) return;

        var existing = await _db.UserPreferences
            .FirstOrDefaultAsync(up => up.UserId == userId && !up.IsArchived);

        if (existing is null)
        {
            // Check for an archived row — if one exists we must reuse it to avoid violating
            // the unique index on UserId (which covers both active and archived rows).
            var archived = await _db.UserPreferences
                .FirstOrDefaultAsync(up => up.UserId == userId && up.IsArchived);

            if (archived is not null)
            {
                archived.IsArchived = false;
                archived.Theme = theme;
            }
            else
            {
                _db.UserPreferences.Add(new UserPreferences
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Theme = theme
                });
            }
        }
        else
        {
            existing.Theme = theme;
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "A concurrency error occurred while persisting theme preference for user {UserId}", userId);
            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to persist theme preference for user {UserId}", userId);
            throw;
        }
    }

    public async Task<UserProfileResponse> GetProfileAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return new UserProfileResponse();

        await using var db = await _dbFactory.CreateDbContextAsync();
        var prefs = await db.UserPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(up => up.UserId == userId && !up.IsArchived);

        return new UserProfileResponse
        {
            FirstName = prefs?.FirstName,
            LastName = prefs?.LastName,
            ProfileImageUrl = prefs?.ProfileImagePath
        };
    }

    public async Task UpdateProfileAsync(Guid userId, string? firstName, string? lastName)
    {
        if (userId == Guid.Empty) return;

        var existing = await _db.UserPreferences
            .FirstOrDefaultAsync(up => up.UserId == userId && !up.IsArchived);

        if (existing is null)
        {
            var archived = await _db.UserPreferences
                .FirstOrDefaultAsync(up => up.UserId == userId && up.IsArchived);

            if (archived is not null)
            {
                archived.IsArchived = false;
                archived.FirstName = firstName;
                archived.LastName = lastName;
            }
            else
            {
                _db.UserPreferences.Add(new UserPreferences
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    FirstName = firstName,
                    LastName = lastName
                });
            }
        }
        else
        {
            existing.FirstName = firstName;
            existing.LastName = lastName;
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error while updating profile for user {UserId}", userId);
            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to update profile for user {UserId}", userId);
            throw;
        }
    }

    public async Task<string?> GetProfileImagePathAsync(Guid userId)
    {
        if (userId == Guid.Empty) return null;

        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.UserPreferences
            .AsNoTracking()
            .Where(up => up.UserId == userId && !up.IsArchived)
            .Select(up => up.ProfileImagePath)
            .FirstOrDefaultAsync();
    }

    public async Task UpdateProfileImagePathAsync(Guid userId, string? imagePath)
    {
        if (userId == Guid.Empty) return;

        var existing = await _db.UserPreferences
            .FirstOrDefaultAsync(up => up.UserId == userId && !up.IsArchived);

        if (existing is null)
        {
            var archived = await _db.UserPreferences
                .FirstOrDefaultAsync(up => up.UserId == userId && up.IsArchived);

            if (archived is not null)
            {
                archived.IsArchived = false;
                archived.ProfileImagePath = imagePath;
            }
            else
            {
                _db.UserPreferences.Add(new UserPreferences
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ProfileImagePath = imagePath
                });
            }
        }
        else
        {
            existing.ProfileImagePath = imagePath;
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error while updating profile image for user {UserId}", userId);
            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to update profile image for user {UserId}", userId);
            throw;
        }
    }
}
