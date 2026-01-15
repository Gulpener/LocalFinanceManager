namespace LocalFinanceManager.Models;

/// <summary>
/// Configuration options for transaction import operations.
/// </summary>
public class ImportOptions
{
    /// <summary>
    /// Number of transactions to process per batch.
    /// </summary>
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Maximum file size in megabytes.
    /// </summary>
    public int MaxFileSizeMB { get; set; } = 100;

    /// <summary>
    /// Timeout in seconds per batch processing.
    /// </summary>
    public int TimeoutSecondsPerBatch { get; set; } = 60;

    /// <summary>
    /// Jaccard similarity threshold for fuzzy matching (0.0 to 1.0).
    /// </summary>
    public decimal FuzzyMatchThreshold { get; set; } = 0.65m;
}
