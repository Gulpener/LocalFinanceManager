using LocalFinanceManager.DTOs;

namespace LocalFinanceManager.Services.Import;

/// <summary>
/// Interface for parsing import files into transaction data.
/// </summary>
public interface IImportParser
{
    /// <summary>
    /// Parses the file content and returns parsed transactions.
    /// </summary>
    Task<List<ParsedTransactionDto>> ParseAsync(string fileContent, ColumnMappingDto mapping);

    /// <summary>
    /// Detects available columns in the file.
    /// </summary>
    Task<List<string>> DetectColumnsAsync(string fileContent);

    /// <summary>
    /// Auto-detects column mappings based on common patterns.
    /// </summary>
    Task<ColumnMappingDto> AutoDetectMappingAsync(string fileContent);
}
