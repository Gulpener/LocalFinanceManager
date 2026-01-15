namespace LocalFinanceManager.Models;

/// <summary>
/// Represents a single budget line with monthly allocations for a specific category.
/// </summary>
public class BudgetLine : BaseEntity
{
    /// <summary>
    /// Foreign key to the BudgetPlan this line belongs to.
    /// </summary>
    public Guid BudgetPlanId { get; set; }

    /// <summary>
    /// Navigation property to the BudgetPlan.
    /// </summary>
    public BudgetPlan BudgetPlan { get; set; } = null!;

    /// <summary>
    /// Foreign key to the Category.
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Navigation property to the Category.
    /// </summary>
    public Category Category { get; set; } = null!;

    /// <summary>
    /// Monthly amounts (Jan-Dec) stored as JSON array of 12 decimal values.
    /// </summary>
    public string MonthlyAmountsJson { get; set; } = "[]";

    /// <summary>
    /// Optional notes for this budget line.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Computed property: Year total (sum of all 12 months).
    /// </summary>
    public decimal YearTotal => MonthlyAmounts.Sum();

    /// <summary>
    /// Gets or sets the monthly amounts as a decimal array.
    /// </summary>
    public decimal[] MonthlyAmounts
    {
        get => System.Text.Json.JsonSerializer.Deserialize<decimal[]>(MonthlyAmountsJson) ?? new decimal[12];
        set => MonthlyAmountsJson = System.Text.Json.JsonSerializer.Serialize(value);
    }
}
