using LocalFinanceManager.Data;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.Models;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Services;

/// <summary>
/// Service for admin-only operations: listing users, viewing shares, and toggling admin roles.
/// </summary>
public class AdminService : IAdminService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminService> _logger;

    public AdminService(AppDbContext context, ILogger<AdminService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<UserSummaryResponse>> GetAllUsersAsync(CancellationToken ct = default)
    {
        var users = await _context.Users
            .AsNoTracking()
            .Where(u => !u.IsArchived)
            .Select(u => new UserSummaryResponse(
                u.Id,
                u.Email,
                u.DisplayName,
                u.IsAdmin,
                u.CreatedAt,
                _context.Accounts.Count(a => a.UserId == u.Id && !a.IsArchived),
                _context.AccountShares.Count(s =>
                    s.UserId == u.Id
                    && !s.IsArchived
                    && s.Status == ShareStatus.Accepted
                    && !s.Account.IsArchived)
                + _context.BudgetPlanShares.Count(s =>
                    s.UserId == u.Id
                    && !s.IsArchived
                    && s.Status == ShareStatus.Accepted
                    && !s.BudgetPlan.IsArchived),
                _context.AccountShares.Count(s =>
                    s.SharedWithUserId == u.Id
                    && !s.IsArchived
                    && s.Status == ShareStatus.Accepted
                    && !s.Account.IsArchived)
                + _context.BudgetPlanShares.Count(s =>
                    s.SharedWithUserId == u.Id
                    && !s.IsArchived
                    && s.Status == ShareStatus.Accepted
                    && !s.BudgetPlan.IsArchived)
            ))
            .ToListAsync(ct);

        return users;
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
