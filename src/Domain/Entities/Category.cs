namespace LocalFinanceManager.Domain.Entities;

/// <summary>
/// Represents a transaction category.
/// </summary>
public class Category
{
    /// <summary>
    /// Unique identifier for the category.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Name of the category.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional parent category for hierarchical categorization.
    /// </summary>
    public int? ParentCategoryId { get; set; }

    /// <summary>
    /// Monthly budget amount for this category.
    /// </summary>
    public decimal MonthlyBudget { get; set; }

    /// <summary>
    /// Navigation property to the parent category.
    /// </summary>
    public Category? ParentCategory { get; set; }

    /// <summary>
    /// Navigation property for child categories.
    /// </summary>
    public ICollection<Category> ChildCategories { get; set; } = new List<Category>();

    /// <summary>
    /// Navigation property for transactions in this category.
    /// </summary>
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
