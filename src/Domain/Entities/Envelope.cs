namespace LocalFinanceManager.Domain.Entities;

/// <summary>
/// Represents an envelope/pot for budgeting (e.g., "Groceries", "Vacation").
/// </summary>
public class Envelope
{
    /// <summary>
    /// Unique identifier for the envelope.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Name of the envelope.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Current balance in the envelope.
    /// </summary>
    public decimal Balance { get; set; }

    /// <summary>
    /// Monthly allocation amount for automatic funding.
    /// </summary>
    public decimal MonthlyAllocation { get; set; }

    /// <summary>
    /// Navigation property for transactions linked to this envelope.
    /// </summary>
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
