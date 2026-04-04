using LocalFinanceManager.Models;

namespace LocalFinanceManager.DTOs;

// --- Requests ---

public class ShareResourceRequest
{
    public string Email { get; set; } = string.Empty;
    public PermissionLevel Permission { get; set; } = PermissionLevel.Viewer;
}

// --- Responses ---

public class AccountShareResponse
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string AccountLabel { get; set; } = string.Empty;
    public Guid SharedWithUserId { get; set; }
    public string SharedWithEmail { get; set; } = string.Empty;
    public string SharedWithDisplayName { get; set; } = string.Empty;
    public PermissionLevel Permission { get; set; }
    public ShareStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    public static AccountShareResponse FromEntity(AccountShare share)
    {
        return new AccountShareResponse
        {
            Id = share.Id,
            AccountId = share.AccountId,
            AccountLabel = share.Account?.Label ?? string.Empty,
            SharedWithUserId = share.SharedWithUserId,
            SharedWithEmail = share.SharedWithUser?.Email ?? string.Empty,
            SharedWithDisplayName = share.SharedWithUser?.DisplayName ?? string.Empty,
            Permission = share.Permission,
            Status = share.Status,
            CreatedAt = share.CreatedAt
        };
    }
}

public class BudgetPlanShareResponse
{
    public Guid Id { get; set; }
    public Guid BudgetPlanId { get; set; }
    public string BudgetPlanName { get; set; } = string.Empty;
    public Guid SharedWithUserId { get; set; }
    public string SharedWithEmail { get; set; } = string.Empty;
    public string SharedWithDisplayName { get; set; } = string.Empty;
    public PermissionLevel Permission { get; set; }
    public ShareStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    public static BudgetPlanShareResponse FromEntity(BudgetPlanShare share)
    {
        return new BudgetPlanShareResponse
        {
            Id = share.Id,
            BudgetPlanId = share.BudgetPlanId,
            BudgetPlanName = share.BudgetPlan?.Name ?? string.Empty,
            SharedWithUserId = share.SharedWithUserId,
            SharedWithEmail = share.SharedWithUser?.Email ?? string.Empty,
            SharedWithDisplayName = share.SharedWithUser?.DisplayName ?? string.Empty,
            Permission = share.Permission,
            Status = share.Status,
            CreatedAt = share.CreatedAt
        };
    }
}

public class PendingSharesResponse
{
    public List<AccountShareResponse> AccountShares { get; set; } = new();
    public List<BudgetPlanShareResponse> BudgetPlanShares { get; set; } = new();
    public int TotalCount => AccountShares.Count + BudgetPlanShares.Count;
}
