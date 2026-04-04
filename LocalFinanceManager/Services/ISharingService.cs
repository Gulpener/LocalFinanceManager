using LocalFinanceManager.Models;

namespace LocalFinanceManager.Services;

public interface ISharingService
{
    // Share operations
    Task<AccountShare> ShareAccountAsync(Guid accountId, string targetEmail, PermissionLevel permission, Guid currentUserId);
    Task<BudgetPlanShare> ShareBudgetPlanAsync(Guid planId, string targetEmail, PermissionLevel permission, Guid currentUserId);

    // Accept / Decline
    Task AcceptAccountShareAsync(Guid shareId, Guid currentUserId);
    Task DeclineAccountShareAsync(Guid shareId, Guid currentUserId);
    Task AcceptBudgetPlanShareAsync(Guid shareId, Guid currentUserId);
    Task DeclineBudgetPlanShareAsync(Guid shareId, Guid currentUserId);

    // Revoke (owner only, any status)
    Task RevokeAccountShareAsync(Guid shareId, Guid currentUserId);
    Task RevokeBudgetPlanShareAsync(Guid shareId, Guid currentUserId);

    // Queries
    Task<List<AccountShare>> GetAccountSharesAsync(Guid accountId, Guid currentUserId);
    Task<List<BudgetPlanShare>> GetBudgetPlanSharesAsync(Guid planId, Guid currentUserId);
    Task<(List<AccountShare> AccountShares, List<BudgetPlanShare> BudgetPlanShares)> GetPendingSharesForUserAsync(Guid userId);
    Task<(List<AccountShare> AccountShares, List<BudgetPlanShare> BudgetPlanShares)> GetAcceptedSharesForUserAsync(Guid userId);

    // Access checks (Accepted only)
    Task<PermissionLevel?> GetUserAccountAccessLevelAsync(Guid accountId, Guid userId);
    Task<PermissionLevel?> GetUserBudgetPlanAccessLevelAsync(Guid planId, Guid userId);
    Task<bool> CanViewAccountAsync(Guid accountId, Guid userId);
    Task<bool> CanEditAccountAsync(Guid accountId, Guid userId);
    Task<bool> IsAccountOwnerAsync(Guid accountId, Guid userId);
    Task<bool> CanViewBudgetPlanAsync(Guid planId, Guid userId);
    Task<bool> CanEditBudgetPlanAsync(Guid planId, Guid userId);
    Task<bool> IsBudgetPlanOwnerAsync(Guid planId, Guid userId);

    // Count of pending shares for badge
    Task<int> GetPendingShareCountAsync(Guid userId);
}
