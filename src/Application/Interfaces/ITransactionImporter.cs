using LocalFinanceManager.Domain.Entities;

namespace LocalFinanceManager.Application.Interfaces;

/// <summary>
/// Result of an import operation.
/// </summary>
public class ImportResult
{
    /// <summary>
    /// List of successfully parsed transactions.
    /// </summary>
    public List<Transaction> Transactions { get; set; } = new();

    /// <summary>
    /// List of errors encountered during import.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Whether the import was successful (no errors).
    /// </summary>
    public bool IsSuccess => Errors.Count == 0;

    /// <summary>
    /// Number of transactions successfully parsed.
    /// </summary>
    public int SuccessCount => Transactions.Count;
}

/// <summary>
/// Configuration for transaction import.
/// </summary>
public class ImportConfiguration
{
    /// <summary>
    /// The target account ID for imported transactions.
    /// </summary>
    public int AccountId { get; set; }

    /// <summary>
    /// Default category ID to assign to imported transactions.
    /// </summary>
    public int DefaultCategoryId { get; set; }

    /// <summary>
    /// Delimiter character for CSV/TSV files.
    /// </summary>
    public char Delimiter { get; set; } = ',';

    /// <summary>
    /// Whether the file has a header row.
    /// </summary>
    public bool HasHeader { get; set; } = true;

    /// <summary>
    /// Column mapping for CSV files.
    /// </summary>
    public Dictionary<string, int> ColumnMapping { get; set; } = new();
}

/// <summary>
/// Interface for transaction importers.
/// </summary>
public interface ITransactionImporter
{
    /// <summary>
    /// Gets the supported file extensions for this importer.
    /// </summary>
    IEnumerable<string> SupportedExtensions { get; }

    /// <summary>
    /// Determines if this importer can handle the specified file.
    /// </summary>
    /// <param name="fileName">The file name or path.</param>
    /// <returns>True if this importer can handle the file.</returns>
    bool CanHandle(string fileName);

    /// <summary>
    /// Imports transactions from a stream.
    /// </summary>
    /// <param name="stream">The input stream.</param>
    /// <param name="configuration">Import configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The import result.</returns>
    Task<ImportResult> ImportAsync(Stream stream, ImportConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports transactions from file content.
    /// </summary>
    /// <param name="content">The file content as string.</param>
    /// <param name="configuration">Import configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The import result.</returns>
    Task<ImportResult> ImportFromStringAsync(string content, ImportConfiguration configuration, CancellationToken cancellationToken = default);
}
