namespace LocalFinanceManager.Models;

/// <summary>
/// Represents a budget category for transaction classification.
/// </summary>
public class Category : BaseEntity
{
    /// <summary>
    /// Category name (e.g., "Groceries", "Rent", "Transport").
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
