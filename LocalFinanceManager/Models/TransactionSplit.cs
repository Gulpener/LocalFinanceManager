namespace LocalFinanceManager.Models;

/// <summary>
/// Represents a split part of a transaction assigned to a specific budget line or category.
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
    /// Nullable to support direct category assignment without budget line.
    /// </summary>
    public Guid? BudgetLineId { get; set; }

    /// <summary>
    /// Navigation property to the BudgetLine.
    /// </summary>
    public BudgetLine? BudgetLine { get; set; }

    /// <summary>
    /// Foreign key to the Category this split is assigned to.
    /// Used when assigning directly to category without specific budget line.
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Navigation property to the Category.
    /// </summary>
    public Category? Category { get; set; }

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
