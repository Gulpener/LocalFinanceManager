namespace LocalFinanceManager.Models;

/// <summary>
/// Represents a split part of a transaction assigned to a specific budget line.
/// Enables transactions to be divided across multiple budget categories.
/// </summary>
public class TransactionSplit : BaseEntity
{
    /// <summary>
    /// Foreign key to the Transaction this split belongs to.
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Navigation property to the Transaction.
    /// </summary>
    public Transaction Transaction { get; set; } = null!;

    /// <summary>
    /// Foreign key to the BudgetLine this split is assigned to.
    /// Required - all splits must reference a budget line.
    /// Category is accessed via BudgetLine.Category navigation property.
    /// </summary>
    public Guid BudgetLineId { get; set; }

    /// <summary>
    /// Navigation property to the BudgetLine.
    /// </summary>
    public BudgetLine BudgetLine { get; set; } = null!;

    /// <summary>
    /// Amount allocated to this split.
    /// Must be positive and sum of all splits must equal transaction amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Optional note explaining this split allocation.
    /// </summary>
    public string? Note { get; set; }
}
