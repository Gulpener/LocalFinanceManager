namespace LocalFinanceManager.Domain.Entities;

/// <summary>
/// Represents a budget for tracking planned vs. actual spending.
/// Can be scoped to a category, envelope, or account.
/// </summary>
public class Budget
{
    /// <summary>
    /// Unique identifier for the budget.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Optional category this budget applies to.
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Optional envelope this budget applies to.
    /// </summary>
    public int? EnvelopeId { get; set; }

    /// <summary>
    /// Optional account this budget applies to.
    /// </summary>
    public int? AccountId { get; set; }

    /// <summary>
    /// The month this budget applies to (first day of the month).
    /// </summary>
    public DateTime Month { get; set; }

    /// <summary>
    /// Planned/budgeted amount for the period.
    /// </summary>
    public decimal PlannedAmount { get; set; }

    /// <summary>
    /// Actual amount spent/received in the period.
    /// </summary>
    public decimal ActualAmount { get; set; }

    /// <summary>
    /// Navigation property to the associated category.
    /// </summary>
    public Category? Category { get; set; }

    /// <summary>
    /// Navigation property to the associated envelope.
    /// </summary>
    public Envelope? Envelope { get; set; }

    /// <summary>
    /// Navigation property to the associated account.
    /// </summary>
    public Account? Account { get; set; }
}
