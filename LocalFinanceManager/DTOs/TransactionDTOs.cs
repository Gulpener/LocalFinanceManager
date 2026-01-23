namespace LocalFinanceManager.DTOs;

/// <summary>
/// DTO for transaction data.
/// </summary>
public class TransactionDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Counterparty { get; set; }
    public Guid AccountId { get; set; }
    public string? ExternalId { get; set; }
    public string? SourceFileName { get; set; }
    public DateTime? ImportedAt { get; set; }
    public byte[]? RowVersion { get; set; }
}

/// <summary>
/// Column mapping configuration for import.
/// </summary>
public class ColumnMappingDto
{
    /// <summary>
    /// Source column name for Date field.
    /// </summary>
    public string? DateColumn { get; set; }

    /// <summary>
    /// Source column name for Amount field.
    /// </summary>
    public string? AmountColumn { get; set; }

    /// <summary>
    /// Source column name for Description field.
    /// </summary>
    public string? DescriptionColumn { get; set; }

    /// <summary>
    /// Source column name for Counterparty field.
    /// </summary>
    public string? CounterpartyColumn { get; set; }

    /// <summary>
    /// Source column name for ExternalId field.
    /// </summary>
    public string? ExternalIdColumn { get; set; }
}

/// <summary>
/// Request for importing transactions.
/// </summary>
public class ImportTransactionsRequest
{
    /// <summary>
    /// Account ID to associate transactions with.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// File format (csv or json).
    /// </summary>
    public string FileFormat { get; set; } = "csv";

    /// <summary>
    /// Column mapping configuration.
    /// </summary>
    public ColumnMappingDto ColumnMapping { get; set; } = new();

    /// <summary>
    /// Whether to skip rows with errors (partial import) or fail entire import.
    /// </summary>
    public bool SkipErrors { get; set; } = false;

    /// <summary>
    /// Deduplication strategy: exact, fuzzy, or none.
    /// </summary>
    public string DeduplicationStrategy { get; set; } = "exact";

    /// <summary>
    /// Source file name.
    /// </summary>
    public string? SourceFileName { get; set; }

    /// <summary>
    /// File content (Base64 encoded for API transport).
    /// </summary>
    public string FileContent { get; set; } = string.Empty;
}

/// <summary>
/// Per-row error information.
/// </summary>
public class ImportRowError
{
    public int LineNumber { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Result of import operation.
/// </summary>
public class ImportResultDto
{
    /// <summary>
    /// Import batch ID.
    /// </summary>
    public Guid ImportBatchId { get; set; }

    /// <summary>
    /// Number of transactions successfully imported.
    /// </summary>
    public int ImportedCount { get; set; }

    /// <summary>
    /// Number of transactions skipped due to errors.
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// Number of transactions skipped due to deduplication.
    /// </summary>
    public int DuplicateCount { get; set; }

    /// <summary>
    /// Total number of rows processed.
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// Per-row errors.
    /// </summary>
    public List<ImportRowError> Errors { get; set; } = new();

    /// <summary>
    /// Whether import completed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Optional message.
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
/// Parsed transaction data from import.
/// </summary>
public class ParsedTransactionDto
{
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Counterparty { get; set; }
    public string? ExternalId { get; set; }
    public string OriginalImport { get; set; } = string.Empty;
    public int LineNumber { get; set; }
}

/// <summary>
/// Preview result showing parsed data and suggested mappings.
/// </summary>
public class ImportPreviewDto
{
    /// <summary>
    /// Detected columns from source file.
    /// </summary>
    public List<string> AvailableColumns { get; set; } = new();

    /// <summary>
    /// Suggested column mappings (auto-detected).
    /// </summary>
    public ColumnMappingDto SuggestedMapping { get; set; } = new();

    /// <summary>
    /// Sample transactions (first 10 rows).
    /// </summary>
    public List<ParsedTransactionDto> SampleTransactions { get; set; } = new();

    /// <summary>
    /// Total row count in file.
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// Any parsing errors encountered.
    /// </summary>
    public List<ImportRowError> Errors { get; set; } = new();
}

/// <summary>
/// DTO for transaction split data.
/// </summary>
public class TransactionSplitDto
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public Guid BudgetLineId { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public byte[]? RowVersion { get; set; }
}

/// <summary>
/// Request to assign a transaction to a budget line.
/// </summary>
public class AssignTransactionRequest
{
    /// <summary>
    /// Budget line ID to assign to (required).
    /// </summary>
    public Guid BudgetLineId { get; set; }

    /// <summary>
    /// Optional note for this assignment.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Row version for optimistic concurrency control.
    /// </summary>
    public byte[]? RowVersion { get; set; }
}

/// <summary>
/// Request to split a transaction across multiple budget lines or categories.
/// </summary>
public class SplitTransactionRequest
{
    /// <summary>
    /// Split allocations - must sum to transaction amount (±0.01 tolerance).
    /// </summary>
    public List<SplitAllocationDto> Splits { get; set; } = new();

    /// <summary>
    /// Row version for optimistic concurrency control.
    /// </summary>
    public byte[]? RowVersion { get; set; }
}

/// <summary>
/// Individual split allocation within a transaction split.
/// </summary>
public class SplitAllocationDto
{
    /// <summary>
    /// Budget line ID to assign to (required).
    /// </summary>
    public Guid BudgetLineId { get; set; }

    /// <summary>
    /// Amount for this split part.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Optional note explaining this split.
    /// </summary>
    public string? Note { get; set; }
}

/// <summary>
/// Request to bulk assign multiple transactions to same budget line.
/// </summary>
public class BulkAssignTransactionsRequest
{
    /// <summary>
    /// Transaction IDs to assign.
    /// </summary>
    public List<Guid> TransactionIds { get; set; } = new();

    /// <summary>
    /// Budget line ID to assign all transactions to (required).
    /// </summary>
    public Guid BudgetLineId { get; set; }

    /// <summary>
    /// Optional note for the bulk assignment.
    /// </summary>
    public string? Note { get; set; }
}

/// <summary>
/// Result of bulk assignment operation.
/// </summary>
public class BulkAssignResultDto
{
    /// <summary>
    /// Number of transactions successfully assigned.
    /// </summary>
    public int AssignedCount { get; set; }

    /// <summary>
    /// Number of transactions that failed.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Total transactions attempted.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Whether operation completed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Optional message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// IDs of failed transactions.
    /// </summary>
    public List<Guid> FailedTransactionIds { get; set; } = new();
}

/// <summary>
/// Request to undo last assignment action on a transaction.
/// </summary>
public class UndoAssignmentRequest
{
    /// <summary>
    /// Transaction ID to undo.
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Audit entry ID to undo (most recent if not specified).
    /// </summary>
    public Guid? AuditEntryId { get; set; }
}

/// <summary>
/// DTO for transaction audit entry.
/// </summary>
public class TransactionAuditDto
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? BeforeState { get; set; }
    public string? AfterState { get; set; }
    public string? Reason { get; set; }
}

