namespace LocalFinanceManager.Models;

/// <summary>
/// Category type: Income or Expense.
/// </summary>
public enum CategoryType
{
    Expense = 0,
    Income = 1
}

/// <summary>
/// Represents a budget category for transaction classification.
/// </summary>
public class Category : BaseEntity
{
    /// <summary>
    /// Category name (e.g., "Groceries", "Rent", "Salary").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category type (Income or Expense).
    /// </summary>
    public CategoryType Type { get; set; }

    /// <summary>
    /// Foreign key to the BudgetPlan this category belongs to.
    /// </summary>
    public Guid BudgetPlanId { get; set; }

    /// <summary>
    /// Navigation property to the BudgetPlan.
    /// </summary>
    public BudgetPlan BudgetPlan { get; set; } = null!;
}
