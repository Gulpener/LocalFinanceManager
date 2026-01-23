namespace LocalFinanceManager.Models;

/// <summary>
/// Represents a financial account (bank account, savings account, credit account, etc.).
/// </summary>
public class Account : BaseEntity
{
    /// <summary>
    /// User-friendly label for the account (e.g., "Main Checking Account").
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Type of account.
    /// </summary>
    public AccountType Type { get; set; }

    /// <summary>
    /// ISO-4217 currency code (e.g., "EUR", "USD").
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// International Bank Account Number (IBAN) stored without spaces.
    /// </summary>
    public string IBAN { get; set; } = string.Empty;

    /// <summary>
    /// Initial balance when the account was created.
    /// </summary>
    public decimal StartingBalance { get; set; }

    /// <summary>
    /// Foreign key to the current active budget plan for this account.
    /// Null if no budget plan is active.
    /// </summary>
    public Guid? CurrentBudgetPlanId { get; set; }

    /// <summary>
    /// Navigation property to the current budget plan.
    /// </summary>
    public BudgetPlan? CurrentBudgetPlan { get; set; }

    /// <summary>
    /// Computed current balance (for MVP-1, equals StartingBalance; will include transactions in later MVPs).
    /// </summary>
    public decimal CurrentBalance => StartingBalance; // Will be updated in MVP-3
}

/// <summary>
/// Types of financial accounts.
/// </summary>
public enum AccountType
{
    Checking,
    Savings,
    Credit,
    Other
}
