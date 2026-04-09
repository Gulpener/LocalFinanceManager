namespace LocalFinanceManager.Models;

/// <summary>
/// Represents an authenticated user with Supabase Auth integration.
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// UUID from Supabase Auth (auth.users.id).
    /// </summary>
    public string SupabaseUserId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; } = false;

    // Navigation properties
    public ICollection<Account> Accounts { get; set; } = new List<Account>();
    public ICollection<BudgetPlan> BudgetPlans { get; set; } = new List<BudgetPlan>();
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    // Shares received by this user
    public ICollection<AccountShare> AccountShares { get; set; } = new List<AccountShare>();
    public ICollection<BudgetPlanShare> BudgetPlanShares { get; set; } = new List<BudgetPlanShare>();
    public ICollection<UserPreferences> UserPreferences { get; set; } = new List<UserPreferences>();
}
