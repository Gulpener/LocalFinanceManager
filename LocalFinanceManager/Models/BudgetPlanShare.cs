namespace LocalFinanceManager.Models;

public class BudgetPlanShare : BaseEntity
{
    public Guid BudgetPlanId { get; set; }
    public BudgetPlan BudgetPlan { get; set; } = null!;

    public Guid SharedWithUserId { get; set; }
    public User SharedWithUser { get; set; } = null!;

    public PermissionLevel Permission { get; set; }
    public ShareStatus Status { get; set; } = ShareStatus.Pending;
}
