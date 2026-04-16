using LocalFinanceManager.Data;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.Models;
using LocalFinanceManager.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LocalFinanceManager.Services;

/// <summary>
/// Service for admin-only operations: listing users, viewing shares, and toggling admin roles.
/// </summary>
public class AdminService : IAdminService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminService> _logger;
    private readonly ISupabaseStorageService _storageService;
    private readonly SupabaseOptions _supabaseOptions;

    public AdminService(
        AppDbContext context,
        ILogger<AdminService> logger,
        ISupabaseStorageService storageService,
        IOptions<SupabaseOptions> supabaseOptions)
    {
        _context = context;
        _logger = logger;
        _storageService = storageService;
        _supabaseOptions = supabaseOptions.Value;
    }

    /// <inheritdoc />
    public async Task<List<UserSummaryResponse>> GetAllUsersAsync(CancellationToken ct = default)
    {
        // Use grouped aggregates (one query per count type) instead of correlated subqueries
        // to avoid N scalar subqueries per user row as the user table grows.
        var ownedAccountCounts = await _context.Accounts
            .AsNoTracking()
            .Where(a => !a.IsArchived && a.UserId != null)
            .GroupBy(a => a.UserId!.Value)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, ct);

        var outgoingAccountShareCounts = await _context.AccountShares
            .AsNoTracking()
            .Where(s => !s.IsArchived && s.Status == ShareStatus.Accepted && !s.Account.IsArchived && s.UserId != null)
            .GroupBy(s => s.UserId!.Value)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, ct);

        var outgoingBudgetPlanShareCounts = await _context.BudgetPlanShares
            .AsNoTracking()
            .Where(s => !s.IsArchived && s.Status == ShareStatus.Accepted && !s.BudgetPlan.IsArchived && s.UserId != null)
            .GroupBy(s => s.UserId!.Value)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, ct);

        var incomingAccountShareCounts = await _context.AccountShares
            .AsNoTracking()
            .Where(s => !s.IsArchived && s.Status == ShareStatus.Accepted && !s.Account.IsArchived)
            .GroupBy(s => s.SharedWithUserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, ct);

        var incomingBudgetPlanShareCounts = await _context.BudgetPlanShares
            .AsNoTracking()
            .Where(s => !s.IsArchived && s.Status == ShareStatus.Accepted && !s.BudgetPlan.IsArchived)
            .GroupBy(s => s.SharedWithUserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, ct);

        var profilePreferences = await _context.UserPreferences
            .AsNoTracking()
            .Where(p => !p.IsArchived && p.UserId != null)
            .Select(p => new
            {
                UserId = p.UserId!.Value,
                p.FirstName,
                p.LastName,
                p.ProfileImagePath
            })
            .ToDictionaryAsync(p => p.UserId, ct);

        return (await _context.Users
                .AsNoTracking()
                .Where(u => !u.IsArchived)
                .OrderBy(u => u.Email)
                .Select(u => new { u.Id, u.Email, u.DisplayName, u.IsAdmin, u.CreatedAt })
                .ToListAsync(ct))
            .Select(u => new UserSummaryResponse(
                u.Id,
                u.Email,
                BuildDisplayName(u.DisplayName, profilePreferences.GetValueOrDefault(u.Id)?.FirstName, profilePreferences.GetValueOrDefault(u.Id)?.LastName),
                profilePreferences.GetValueOrDefault(u.Id)?.FirstName,
                profilePreferences.GetValueOrDefault(u.Id)?.LastName,
                BuildProfileImageUrl(profilePreferences.GetValueOrDefault(u.Id)?.ProfileImagePath),
                u.IsAdmin,
                u.CreatedAt,
                ownedAccountCounts.GetValueOrDefault(u.Id),
                outgoingAccountShareCounts.GetValueOrDefault(u.Id) + outgoingBudgetPlanShareCounts.GetValueOrDefault(u.Id),
                incomingAccountShareCounts.GetValueOrDefault(u.Id) + incomingBudgetPlanShareCounts.GetValueOrDefault(u.Id)
            )).ToList();
    }

    private string BuildDisplayName(string fallbackDisplayName, string? firstName, string? lastName)
    {
        var trimmedFirstName = firstName?.Trim();
        var trimmedLastName = lastName?.Trim();

        if (!string.IsNullOrWhiteSpace(trimmedFirstName) && !string.IsNullOrWhiteSpace(trimmedLastName))
        {
            return $"{trimmedFirstName} {trimmedLastName}";
        }

        if (!string.IsNullOrWhiteSpace(trimmedFirstName))
        {
            return trimmedFirstName;
        }

        if (!string.IsNullOrWhiteSpace(trimmedLastName))
        {
            return trimmedLastName;
        }

        return fallbackDisplayName;
    }

    private string? BuildProfileImageUrl(string? profileImagePath)
    {
        if (string.IsNullOrWhiteSpace(profileImagePath))
        {
            return null;
        }

        return _storageService.GetPublicUrl(_supabaseOptions.StorageBucket, profileImagePath);
    }

    /// <inheritdoc />
    public async Task<UserSharesResponse> GetUserSharesAsync(Guid userId, CancellationToken ct = default)
    {
        var accountShares = await _context.AccountShares
            .AsNoTracking()
            .Where(s => s.UserId == userId && !s.IsArchived && !s.Account.IsArchived && s.Status != ShareStatus.Declined)
            .Select(s => new AccountShareDetail(
                s.Id,
                s.Account.Label,
                s.SharedWithUser.Email,
                s.Permission.ToString(),
                s.Status.ToString()
            ))
            .ToListAsync(ct);

        var budgetPlanShares = await _context.BudgetPlanShares
            .AsNoTracking()
            .Where(s => s.UserId == userId && !s.IsArchived && !s.BudgetPlan.IsArchived && s.Status != ShareStatus.Declined)
            .Select(s => new BudgetPlanShareDetail(
                s.Id,
                s.BudgetPlan.Name,
                s.SharedWithUser.Email,
                s.Permission.ToString(),
                s.Status.ToString()
            ))
            .ToListAsync(ct);

        return new UserSharesResponse(accountShares, budgetPlanShares);
    }

    /// <inheritdoc />
    public async Task ToggleAdminAsync(Guid targetUserId, Guid requestingUserId, CancellationToken ct = default)
    {
        if (targetUserId == requestingUserId)
            throw new InvalidOperationException("Cannot change your own admin role.");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == targetUserId && !u.IsArchived, ct)
            ?? throw new KeyNotFoundException($"User {targetUserId} not found.");

        user.IsAdmin = !user.IsAdmin;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Admin role for user {TargetUserId} set to {IsAdmin} by {RequestingUserId}",
            targetUserId, user.IsAdmin, requestingUserId);
    }
}
