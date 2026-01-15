using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LocalFinanceManager.Services.Import;

/// <summary>
/// Service for importing transactions from CSV/JSON files.
/// </summary>
public class ImportService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILogger<ImportService> _logger;
    private readonly ImportOptions _options;
    private readonly Dictionary<string, IImportParser> _parsers;
    private readonly Dictionary<string, IDeduplicationStrategy> _deduplicationStrategies;

    public ImportService(
        ITransactionRepository transactionRepository,
        ILogger<ImportService> logger,
        IOptions<ImportOptions> options,
        CsvImportParser csvParser,
        JsonImportParser jsonParser,
        ExactMatchStrategy exactMatchStrategy,
        FuzzyMatchStrategy fuzzyMatchStrategy)
    {
        _transactionRepository = transactionRepository;
        _logger = logger;
        _options = options.Value;

        _parsers = new Dictionary<string, IImportParser>(StringComparer.OrdinalIgnoreCase)
        {
            { "csv", csvParser },
            { "json", jsonParser }
        };

        _deduplicationStrategies = new Dictionary<string, IDeduplicationStrategy>(StringComparer.OrdinalIgnoreCase)
        {
            { "exact", exactMatchStrategy },
            { "fuzzy", fuzzyMatchStrategy },
            { "none", new NoDeduplicationStrategy() }
        };
    }

    /// <summary>
    /// Generates a preview of the import file with suggested column mappings.
    /// </summary>
    public async Task<ImportPreviewDto> PreviewImportAsync(string fileContent, string fileFormat)
    {
        _logger.LogInformation("Starting import preview for format: {FileFormat}", fileFormat);

        var preview = new ImportPreviewDto();

        try
        {
            // Get parser
            if (!_parsers.TryGetValue(fileFormat, out var parser))
            {
                throw new InvalidOperationException($"Unsupported file format: {fileFormat}");
            }

            // Detect columns
            preview.AvailableColumns = await parser.DetectColumnsAsync(fileContent);

            // Auto-detect mapping
            preview.SuggestedMapping = await parser.AutoDetectMappingAsync(fileContent);

            // Parse sample transactions (first 10 rows)
            var allTransactions = await parser.ParseAsync(fileContent, preview.SuggestedMapping);
            preview.SampleTransactions = allTransactions.Take(10).ToList();
            preview.TotalRows = allTransactions.Count;

            _logger.LogInformation("Preview completed: {TotalRows} rows detected", preview.TotalRows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to preview import");
            preview.Errors.Add(new ImportRowError
            {
                LineNumber = 0,
                FieldName = "File",
                ErrorMessage = ex.Message
            });
        }

        return preview;
    }

    /// <summary>
    /// Imports transactions from file content.
    /// </summary>
    public async Task<ImportResultDto> ImportTransactionsAsync(ImportTransactionsRequest request, Guid accountId)
    {
        var importBatchId = Guid.NewGuid();
        var result = new ImportResultDto
        {
            ImportBatchId = importBatchId
        };

        _logger.LogInformation("Starting import batch {ImportBatchId} for account {AccountId}", importBatchId, accountId);

        try
        {
            // Validate file size (Base64 encoded, so decode to get actual size)
            var fileBytes = Convert.FromBase64String(request.FileContent);
            var fileSizeMB = fileBytes.Length / (1024.0 * 1024.0);
            if (fileSizeMB > _options.MaxFileSizeMB)
            {
                throw new InvalidOperationException($"File size ({fileSizeMB:F2} MB) exceeds maximum allowed ({_options.MaxFileSizeMB} MB)");
            }

            // Decode file content
            var fileContent = System.Text.Encoding.UTF8.GetString(fileBytes);

            // Get parser
            if (!_parsers.TryGetValue(request.FileFormat, out var parser))
            {
                throw new InvalidOperationException($"Unsupported file format: {request.FileFormat}");
            }

            // Get deduplication strategy
            if (!_deduplicationStrategies.TryGetValue(request.DeduplicationStrategy, out var deduplicationStrategy))
            {
                throw new InvalidOperationException($"Unknown deduplication strategy: {request.DeduplicationStrategy}");
            }

            // Parse transactions
            _logger.LogInformation("Parsing file with format: {FileFormat}", request.FileFormat);
            var parsedTransactions = await parser.ParseAsync(fileContent, request.ColumnMapping);
            result.TotalRows = parsedTransactions.Count;

            // Process in batches
            var transactionsToImport = new List<Transaction>();
            var batchSize = _options.BatchSize;

            for (int i = 0; i < parsedTransactions.Count; i += batchSize)
            {
                var batch = parsedTransactions.Skip(i).Take(batchSize).ToList();
                _logger.LogInformation("Processing batch {BatchNumber} ({Start}-{End} of {Total})",
                    i / batchSize + 1, i + 1, Math.Min(i + batchSize, parsedTransactions.Count), parsedTransactions.Count);

                foreach (var parsed in batch)
                {
                    try
                    {
                        // Check for duplicates
                        var duplicates = await deduplicationStrategy.FindDuplicatesAsync(parsed);
                        if (duplicates.Any())
                        {
                            var isDuplicate = duplicates.Any(dup => deduplicationStrategy.IsDuplicate(parsed, dup));
                            if (isDuplicate)
                            {
                                _logger.LogDebug("Skipping duplicate transaction at line {LineNumber}", parsed.LineNumber);
                                result.DuplicateCount++;
                                continue;
                            }
                        }

                        // Create transaction
                        var transaction = new Transaction
                        {
                            Id = Guid.NewGuid(),
                            AccountId = accountId,
                            Amount = parsed.Amount,
                            Date = parsed.Date,
                            Description = parsed.Description,
                            Counterparty = parsed.Counterparty,
                            ExternalId = parsed.ExternalId,
                            OriginalImport = parsed.OriginalImport,
                            ImportBatchId = importBatchId,
                            SourceFileName = request.SourceFileName,
                            ImportedAt = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            IsArchived = false
                        };

                        transactionsToImport.Add(transaction);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing transaction at line {LineNumber}", parsed.LineNumber);
                        
                        var error = new ImportRowError
                        {
                            LineNumber = parsed.LineNumber,
                            FieldName = "Transaction",
                            ErrorMessage = ex.Message
                        };
                        result.Errors.Add(error);
                        result.SkippedCount++;

                        if (!request.SkipErrors)
                        {
                            // All-or-nothing mode: fail entire import
                            throw new InvalidOperationException($"Import failed at line {parsed.LineNumber}: {ex.Message}", ex);
                        }
                    }
                }
            }

            // Import transactions in batches
            if (transactionsToImport.Any())
            {
                for (int i = 0; i < transactionsToImport.Count; i += batchSize)
                {
                    var batch = transactionsToImport.Skip(i).Take(batchSize).ToList();
                    await _transactionRepository.AddRangeAsync(batch);
                    _logger.LogInformation("Saved batch {BatchNumber} with {Count} transactions",
                        i / batchSize + 1, batch.Count);
                }

                result.ImportedCount = transactionsToImport.Count;
            }

            result.Success = true;
            result.Message = $"Successfully imported {result.ImportedCount} transactions. Skipped {result.DuplicateCount} duplicates.";

            _logger.LogInformation("Import batch {ImportBatchId} completed: {ImportedCount} imported, {DuplicateCount} duplicates, {SkippedCount} errors",
                importBatchId, result.ImportedCount, result.DuplicateCount, result.SkippedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import batch {ImportBatchId} failed", importBatchId);
            result.Success = false;
            result.Message = $"Import failed: {ex.Message}";

            if (!result.Errors.Any())
            {
                result.Errors.Add(new ImportRowError
                {
                    LineNumber = 0,
                    FieldName = "Import",
                    ErrorMessage = ex.Message
                });
            }
        }

        return result;
    }

    /// <summary>
    /// No-op deduplication strategy that doesn't filter any transactions.
    /// </summary>
    private class NoDeduplicationStrategy : IDeduplicationStrategy
    {
        public Task<List<Transaction>> FindDuplicatesAsync(ParsedTransactionDto candidate)
        {
            return Task.FromResult(new List<Transaction>());
        }

        public bool IsDuplicate(ParsedTransactionDto candidate, Transaction existing)
        {
            return false;
        }
    }
}
