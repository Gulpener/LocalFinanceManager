namespace LocalFinanceManager.DTOs;

public record UserSummaryResponse(
    Guid Id,
    string Email,
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? ProfileImageUrl,
    bool IsAdmin,
    DateTime CreatedAt,
    int AccountCount,
    int SharesGiven,
    int SharesReceived
);

public record UserSharesResponse(
    List<AccountShareDetail> AccountShares,
    List<BudgetPlanShareDetail> BudgetPlanShares
);

public record AccountShareDetail(
    Guid ShareId,
    string AccountName,
    string SharedWithEmail,
    string Permission,
    string Status
);

public record BudgetPlanShareDetail(
    Guid ShareId,
    string PlanName,
    string SharedWithEmail,
    string Permission,
    string Status
);
