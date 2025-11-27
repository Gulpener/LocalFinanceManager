namespace LocalFinanceManager.Domain.Entities;

/// <summary>
/// Represents a financial transaction.
/// </summary>
public class Transaction
{
    /// <summary>
    /// Unique identifier for the transaction.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the associated account.
    /// </summary>
    public int AccountId { get; set; }

    /// <summary>
    /// Date of the transaction.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Amount of the transaction (positive for income, negative for expenses).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Description or memo for the transaction.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to the category.
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// Optional foreign key to an envelope/pot.
    /// </summary>
    public int? EnvelopeId { get; set; }

    /// <summary>
    /// Tags associated with this transaction.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Original CSV string from import (for audit/debugging).
    /// </summary>
    public string OriginalCsv { get; set; } = string.Empty;

    /// <summary>
    /// Counter account (IBAN) for the transaction.
    /// </summary>
    public string CounterAccount { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to the associated account.
    /// </summary>
    public Account Account { get; set; } = null!;

    /// <summary>
    /// Navigation property to the associated category.
    /// </summary>
    public Category Category { get; set; } = null!;

    /// <summary>
    /// Navigation property to the associated envelope (optional).
    /// </summary>
    public Envelope? Envelope { get; set; }
}
