namespace LocalFinanceManager.Domain.Entities;

/// <summary>
/// Represents a financial account (bank, savings, credit card, cash).
/// </summary>
public class Account
{
    /// <summary>
    /// Unique identifier for the account.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Name of the account.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of account (e.g., "bank", "savings", "creditcard", "cash").
    /// </summary>
    public string AccountType { get; set; } = string.Empty;

    /// <summary>
    /// Initial balance when the account was created.
    /// </summary>
    public decimal InitialBalance { get; set; }

    /// <summary>
    /// Whether the account is currently active (not archived).
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Navigation property for transactions associated with this account.
    /// </summary>
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
