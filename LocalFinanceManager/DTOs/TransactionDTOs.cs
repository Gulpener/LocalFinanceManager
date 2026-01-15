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
