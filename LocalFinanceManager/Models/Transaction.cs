namespace LocalFinanceManager.Models;

/// <summary>
/// Represents a financial transaction imported from CSV/JSON or manually created.
/// </summary>
public class Transaction : BaseEntity
{
    /// <summary>
    /// Transaction amount (positive for income, negative for expense).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Transaction date.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Transaction description/memo.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Counterparty name (e.g., merchant, employer).
    /// </summary>
    public string? Counterparty { get; set; }

    /// <summary>
    /// Account ID (foreign key).
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Navigation property to Account.
    /// </summary>
    public Account? Account { get; set; }

    /// <summary>
    /// External ID from source system (bank reference, etc.) for deduplication.
    /// </summary>
    public string? ExternalId { get; set; }

    /// <summary>
    /// Original import string (CSV/JSON) for audit trail.
    /// </summary>
    public string? OriginalImport { get; set; }

    /// <summary>
    /// Import batch ID for grouping imported transactions.
    /// </summary>
    public Guid? ImportBatchId { get; set; }

    /// <summary>
    /// Source file name from import.
    /// </summary>
    public string? SourceFileName { get; set; }

    /// <summary>
    /// Timestamp when transaction was imported.
    /// </summary>
    public DateTime? ImportedAt { get; set; }

    /// <summary>
    /// Collection of transaction splits when transaction is divided across multiple categories.
    /// Null or empty for unsplit transactions.
    /// </summary>
    public ICollection<TransactionSplit>? AssignedParts { get; set; }

    /// <summary>
    /// Computed property: Indicates whether transaction is split across multiple categories.
    /// </summary>
    public bool IsSplit => AssignedParts != null && AssignedParts.Any();

    /// <summary>
    /// Computed property: Returns the effective amount (sum of splits or base amount).
    /// Used for validation and reporting.
    /// </summary>
    public decimal EffectiveAmount => IsSplit ? AssignedParts!.Sum(s => s.Amount) : Amount;
}
