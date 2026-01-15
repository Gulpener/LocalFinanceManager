namespace LocalFinanceManager.Models;

/// <summary>
/// Represents an annual budget plan for a specific account.
/// </summary>
public class BudgetPlan : BaseEntity
{
    /// <summary>
    /// Foreign key to the Account this budget plan belongs to.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Navigation property to the Account.
    /// </summary>
    public Account Account { get; set; } = null!;

    /// <summary>
    /// Calendar year (e.g., 2026 = January–December 2026).
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Plan name (e.g., "2026 Household Budget").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Collection of budget lines for this plan.
    /// </summary>
    public ICollection<BudgetLine> BudgetLines { get; set; } = new List<BudgetLine>();
}
