namespace LocalFinanceManager.Models;

public class AccountShare : BaseEntity
{
    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;

    public Guid SharedWithUserId { get; set; }
    public User SharedWithUser { get; set; } = null!;

    public PermissionLevel Permission { get; set; }
    public ShareStatus Status { get; set; } = ShareStatus.Pending;
}
