namespace LocalFinanceManager.Models;

/// <summary>
/// Category type: Income or Expense.
/// </summary>
public enum CategoryType
{
    Income,
    Expense
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
}
