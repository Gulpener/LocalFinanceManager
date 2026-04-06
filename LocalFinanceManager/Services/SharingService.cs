using LocalFinanceManager.Data;
using LocalFinanceManager.Models;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Services;

public class SharingService : ISharingService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SharingService> _logger;

    public SharingService(AppDbContext context, ILogger<SharingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AccountShare> ShareAccountAsync(Guid accountId, string targetEmail, PermissionLevel permission, Guid currentUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetEmail);
        var normalizedTargetEmail = targetEmail.Trim().ToUpperInvariant();

        var account = await _context.Accounts
            .Where(a => !a.IsArchived && a.Id == accountId && a.UserId == currentUserId)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Account {accountId} not found or you are not the owner.");

        var targetUser = await _context.Users
            .Where(u => !u.IsArchived && u.Email != null && u.Email.Trim().ToUpper() == normalizedTargetEmail)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("No user found with the specified email address.");

        if (targetUser.Id == currentUserId)
            throw new InvalidOperationException("You cannot share an account with yourself.");

        var existing = await _context.AccountShares
            .Where(s => !s.IsArchived && s.AccountId == accountId && s.SharedWithUserId == targetUser.Id
                        && (s.Status == ShareStatus.Pending || s.Status == ShareStatus.Accepted))
            .FirstOrDefaultAsync();
        if (existing != null)
            throw new InvalidOperationException("A pending or accepted share already exists for this user.");

        var share = new AccountShare
        {
            AccountId = accountId,
            SharedWithUserId = targetUser.Id,
            Permission = permission,
            Status = ShareStatus.Pending,
            UserId = currentUserId
        };
        share.Id = Guid.NewGuid();
        _context.AccountShares.Add(share);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Account share created by user {UserId}", currentUserId);
        return share;
    }

    public async Task<BudgetPlanShare> ShareBudgetPlanAsync(Guid planId, string targetEmail, PermissionLevel permission, Guid currentUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetEmail);
        var normalizedTargetEmail = targetEmail.Trim().ToUpperInvariant();

        var plan = await _context.BudgetPlans
            .Where(bp => !bp.IsArchived && bp.Id == planId && bp.UserId == currentUserId)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Budget plan {planId} not found or you are not the owner.");

        var targetUser = await _context.Users
            .Where(u => !u.IsArchived && u.Email != null && u.Email.Trim().ToUpper() == normalizedTargetEmail)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("No user found with the specified email address.");

        if (targetUser.Id == currentUserId)
            throw new InvalidOperationException("You cannot share a budget plan with yourself.");

        var existing = await _context.BudgetPlanShares
            .Where(s => !s.IsArchived && s.BudgetPlanId == planId && s.SharedWithUserId == targetUser.Id
                        && (s.Status == ShareStatus.Pending || s.Status == ShareStatus.Accepted))
            .FirstOrDefaultAsync();
        if (existing != null)
            throw new InvalidOperationException("A pending or accepted share already exists for this user.");

        var share = new BudgetPlanShare
        {
            BudgetPlanId = planId,
            SharedWithUserId = targetUser.Id,
            Permission = permission,
            Status = ShareStatus.Pending,
            UserId = currentUserId
        };
        share.Id = Guid.NewGuid();
        _context.BudgetPlanShares.Add(share);
        await _context.SaveChangesAsync();

        _logger.LogInformation("BudgetPlan share created by user {UserId}", currentUserId);
        return share;
    }

    public async Task AcceptAccountShareAsync(Guid shareId, Guid currentUserId)
    {
        var share = await GetAccountShareForRecipientAsync(shareId, currentUserId);
        if (share.Status != ShareStatus.Pending)
            throw new InvalidOperationException("Only pending shares can be accepted.");
        share.Status = ShareStatus.Accepted;
        await _context.SaveChangesAsync();
        _logger.LogInformation("AccountShare accepted by user {UserId}", currentUserId);
    }

    public async Task DeclineAccountShareAsync(Guid shareId, Guid currentUserId)
    {
        var share = await GetAccountShareForRecipientAsync(shareId, currentUserId);
        if (share.Status != ShareStatus.Pending)
            throw new InvalidOperationException("Only pending shares can be declined.");
        share.Status = ShareStatus.Declined;
        await _context.SaveChangesAsync();
        _logger.LogInformation("AccountShare declined by user {UserId}", currentUserId);
    }

    public async Task AcceptBudgetPlanShareAsync(Guid shareId, Guid currentUserId)
    {
        var share = await GetBudgetPlanShareForRecipientAsync(shareId, currentUserId);
        if (share.Status != ShareStatus.Pending)
            throw new InvalidOperationException("Only pending shares can be accepted.");
        share.Status = ShareStatus.Accepted;
        await _context.SaveChangesAsync();
        _logger.LogInformation("BudgetPlanShare accepted by user {UserId}", currentUserId);
    }

    public async Task DeclineBudgetPlanShareAsync(Guid shareId, Guid currentUserId)
    {
        var share = await GetBudgetPlanShareForRecipientAsync(shareId, currentUserId);
        if (share.Status != ShareStatus.Pending)
            throw new InvalidOperationException("Only pending shares can be declined.");
        share.Status = ShareStatus.Declined;
        await _context.SaveChangesAsync();
        _logger.LogInformation("BudgetPlanShare declined by user {UserId}", currentUserId);
    }

    public async Task RevokeAccountShareAsync(Guid shareId, Guid currentUserId)
    {
        var share = await _context.AccountShares
            .Where(s => !s.IsArchived && s.Id == shareId && s.UserId == currentUserId)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Share {shareId} not found or you are not the owner.");
        share.IsArchived = true;
        await _context.SaveChangesAsync();
        _logger.LogInformation("AccountShare revoked by owner {UserId}", currentUserId);
    }

    public async Task RevokeBudgetPlanShareAsync(Guid shareId, Guid currentUserId)
    {
        var share = await _context.BudgetPlanShares
            .Where(s => !s.IsArchived && s.Id == shareId && s.UserId == currentUserId)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Share {shareId} not found or you are not the owner.");
        share.IsArchived = true;
        await _context.SaveChangesAsync();
        _logger.LogInformation("BudgetPlanShare revoked by owner {UserId}", currentUserId);
    }

    public async Task<List<AccountShare>> GetAccountSharesAsync(Guid accountId, Guid currentUserId)
    {
        var isOwner = await _context.Accounts
            .AnyAsync(a => a.Id == accountId && a.UserId == currentUserId && !a.IsArchived);
        if (!isOwner)
            throw new UnauthorizedAccessException("Only the account owner can view shares.");

        return await _context.AccountShares
            .Include(s => s.SharedWithUser)
            .Where(s => !s.IsArchived && s.AccountId == accountId)
            .ToListAsync();
    }

    public async Task<List<BudgetPlanShare>> GetBudgetPlanSharesAsync(Guid planId, Guid currentUserId)
    {
        var isOwner = await _context.BudgetPlans
            .AnyAsync(bp => bp.Id == planId && bp.UserId == currentUserId && !bp.IsArchived);
        if (!isOwner)
            throw new UnauthorizedAccessException("Only the budget plan owner can view shares.");

        return await _context.BudgetPlanShares
            .Include(s => s.SharedWithUser)
            .Where(s => !s.IsArchived && s.BudgetPlanId == planId)
            .ToListAsync();
    }

    public async Task<(List<AccountShare> AccountShares, List<BudgetPlanShare> BudgetPlanShares)> GetPendingSharesForUserAsync(Guid userId)
    {
        var accountShares = await _context.AccountShares
            .Include(s => s.Account)
            .ThenInclude(a => a.User)
            .Where(s => !s.IsArchived && !s.Account.IsArchived && s.SharedWithUserId == userId && s.Status == ShareStatus.Pending)
            .ToListAsync();

        var budgetPlanShares = await _context.BudgetPlanShares
            .Include(s => s.BudgetPlan)
            .ThenInclude(bp => bp.User)
            .Where(s => !s.IsArchived && !s.BudgetPlan.IsArchived && s.SharedWithUserId == userId && s.Status == ShareStatus.Pending)
            .ToListAsync();

        return (accountShares, budgetPlanShares);
    }

    public async Task<(List<AccountShare> AccountShares, List<BudgetPlanShare> BudgetPlanShares)> GetAcceptedSharesForUserAsync(Guid userId)
    {
        var accountShares = await _context.AccountShares
            .Include(s => s.Account)
            .ThenInclude(a => a.User)
            .Where(s => !s.IsArchived && !s.Account.IsArchived && s.SharedWithUserId == userId && s.Status == ShareStatus.Accepted)
            .ToListAsync();

        var budgetPlanShares = await _context.BudgetPlanShares
            .Include(s => s.BudgetPlan)
            .ThenInclude(bp => bp.User)
            .Where(s => !s.IsArchived && !s.BudgetPlan.IsArchived && s.SharedWithUserId == userId && s.Status == ShareStatus.Accepted)
            .ToListAsync();

        return (accountShares, budgetPlanShares);
    }

    public async Task<int> GetPendingShareCountAsync(Guid userId)
    {
        var accountCount = await _context.AccountShares
            .CountAsync(s => !s.IsArchived && !s.Account.IsArchived && s.SharedWithUserId == userId && s.Status == ShareStatus.Pending);
        var budgetPlanCount = await _context.BudgetPlanShares
            .CountAsync(s => !s.IsArchived && !s.BudgetPlan.IsArchived && s.SharedWithUserId == userId && s.Status == ShareStatus.Pending);
        return accountCount + budgetPlanCount;
    }

    public async Task<PermissionLevel?> GetUserAccountAccessLevelAsync(Guid accountId, Guid userId)
    {
        var isOwner = await _context.Accounts
            .AnyAsync(a => a.Id == accountId && a.UserId == userId && !a.IsArchived);
        if (isOwner) return PermissionLevel.Owner;

        var share = await _context.AccountShares
            .Where(s => !s.IsArchived && s.AccountId == accountId && s.SharedWithUserId == userId && s.Status == ShareStatus.Accepted)
            .FirstOrDefaultAsync();
        return share?.Permission;
    }

    public async Task<PermissionLevel?> GetUserBudgetPlanAccessLevelAsync(Guid planId, Guid userId)
    {
        var isOwner = await _context.BudgetPlans
            .AnyAsync(bp => bp.Id == planId && bp.UserId == userId && !bp.IsArchived);
        if (isOwner) return PermissionLevel.Owner;

        var share = await _context.BudgetPlanShares
            .Where(s => !s.IsArchived && s.BudgetPlanId == planId && s.SharedWithUserId == userId && s.Status == ShareStatus.Accepted)
            .FirstOrDefaultAsync();
        return share?.Permission;
    }

    public async Task<bool> CanViewAccountAsync(Guid accountId, Guid userId)
    {
        var level = await GetUserAccountAccessLevelAsync(accountId, userId);
        return level != null;
    }

    public async Task<bool> CanEditAccountAsync(Guid accountId, Guid userId)
    {
        var level = await GetUserAccountAccessLevelAsync(accountId, userId);
        return level is PermissionLevel.Owner or PermissionLevel.Editor;
    }

    public async Task<bool> IsAccountOwnerAsync(Guid accountId, Guid userId)
    {
        return await _context.Accounts
            .AnyAsync(a => a.Id == accountId && a.UserId == userId && !a.IsArchived);
    }

    public async Task<bool> CanViewBudgetPlanAsync(Guid planId, Guid userId)
    {
        var level = await GetUserBudgetPlanAccessLevelAsync(planId, userId);
        return level != null;
    }

    public async Task<bool> CanEditBudgetPlanAsync(Guid planId, Guid userId)
    {
        var level = await GetUserBudgetPlanAccessLevelAsync(planId, userId);
        return level is PermissionLevel.Owner or PermissionLevel.Editor;
    }

    public async Task<bool> IsBudgetPlanOwnerAsync(Guid planId, Guid userId)
    {
        return await _context.BudgetPlans
            .AnyAsync(bp => bp.Id == planId && bp.UserId == userId && !bp.IsArchived);
    }

    // --- Private helpers ---
    private async Task<AccountShare> GetAccountShareForRecipientAsync(Guid shareId, Guid currentUserId)
    {
        return await _context.AccountShares
            .Where(s => !s.IsArchived && s.Id == shareId && s.SharedWithUserId == currentUserId)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Share {shareId} not found or you are not the recipient.");
    }

    private async Task<BudgetPlanShare> GetBudgetPlanShareForRecipientAsync(Guid shareId, Guid currentUserId)
    {
        return await _context.BudgetPlanShares
            .Where(s => !s.IsArchived && s.Id == shareId && s.SharedWithUserId == currentUserId)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Share {shareId} not found or you are not the recipient.");
    }
}
