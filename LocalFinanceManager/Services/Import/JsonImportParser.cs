using System.Text.Json;
using LocalFinanceManager.DTOs;
using Microsoft.Extensions.Logging;

namespace LocalFinanceManager.Services.Import;

/// <summary>
/// JSON file parser for transaction imports.
/// </summary>
public class JsonImportParser : IImportParser
{
    private readonly ILogger<JsonImportParser> _logger;

    public JsonImportParser(ILogger<JsonImportParser> logger)
    {
        _logger = logger;
    }

    public async Task<List<ParsedTransactionDto>> ParseAsync(string fileContent, ColumnMappingDto mapping)
    {
        var transactions = new List<ParsedTransactionDto>();

        try
        {
            var jsonArray = JsonDocument.Parse(fileContent);
            
            if (jsonArray.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("JSON root must be an array of transaction objects");
            }

            int lineNumber = 1;
            foreach (var element in jsonArray.RootElement.EnumerateArray())
            {
                try
                {
                    var transaction = ParseTransaction(element, mapping, lineNumber);
                    if (transaction != null)
                    {
                        transactions.Add(transaction);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error parsing JSON element at index {LineNumber}", lineNumber);
                }
                lineNumber++;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON content");
            throw new InvalidOperationException("Invalid JSON format", ex);
        }

        return await Task.FromResult(transactions);
    }

    public async Task<List<string>> DetectColumnsAsync(string fileContent)
    {
        var columns = new HashSet<string>();

        try
        {
            var jsonArray = JsonDocument.Parse(fileContent);
            
            if (jsonArray.RootElement.ValueKind != JsonValueKind.Array)
            {
                return new List<string>();
            }

            // Get all property names from first few elements
            int sampleSize = Math.Min(10, jsonArray.RootElement.GetArrayLength());
            for (int i = 0; i < sampleSize; i++)
            {
                var element = jsonArray.RootElement[i];
                foreach (var property in element.EnumerateObject())
                {
                    columns.Add(property.Name);
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to detect columns from JSON");
        }

        return await Task.FromResult(columns.ToList());
    }

    public async Task<ColumnMappingDto> AutoDetectMappingAsync(string fileContent)
    {
        var columns = await DetectColumnsAsync(fileContent);
        var mapping = new ColumnMappingDto();

        foreach (var column in columns)
        {
            var lowerColumn = column.ToLowerInvariant();

            // Detect date column
            if (mapping.DateColumn == null && 
                (lowerColumn == "date" || lowerColumn == "transactiondate" || 
                 lowerColumn == "datum" || lowerColumn == "transactiedatum"))
            {
                mapping.DateColumn = column;
            }

            // Detect amount column
            if (mapping.AmountColumn == null && 
                (lowerColumn == "amount" || lowerColumn == "bedrag" || 
                 lowerColumn == "value" || lowerColumn == "debitcredit"))
            {
                mapping.AmountColumn = column;
            }

            // Detect description column
            if (mapping.DescriptionColumn == null && 
                (lowerColumn == "description" || lowerColumn == "omschrijving" || 
                 lowerColumn == "memo" || lowerColumn == "details"))
            {
                mapping.DescriptionColumn = column;
            }

            // Detect counterparty column
            if (mapping.CounterpartyColumn == null && 
                (lowerColumn == "counterparty" || lowerColumn == "tegenpartij" || 
                 lowerColumn == "naam" || lowerColumn == "merchant"))
            {
                mapping.CounterpartyColumn = column;
            }

            // Detect external ID column
            if (mapping.ExternalIdColumn == null && 
                (lowerColumn == "reference" || lowerColumn == "referentie" || 
                 lowerColumn == "transactionid" || lowerColumn == "id" || lowerColumn == "externalid"))
            {
                mapping.ExternalIdColumn = column;
            }
        }

        return mapping;
    }

    private ParsedTransactionDto? ParseTransaction(JsonElement element, ColumnMappingDto mapping, int lineNumber)
    {
        try
        {
            var transaction = new ParsedTransactionDto
            {
                LineNumber = lineNumber,
                OriginalImport = element.GetRawText()
            };

            // Parse date (required)
            if (!string.IsNullOrEmpty(mapping.DateColumn) && element.TryGetProperty(mapping.DateColumn, out var dateElement))
            {
                if (dateElement.ValueKind == JsonValueKind.String)
                {
                    transaction.Date = DateTime.Parse(dateElement.GetString()!);
                }
                else
                {
                    throw new InvalidOperationException("Date must be a string");
                }
            }
            else
            {
                throw new InvalidOperationException("Date property is required");
            }

            // Parse amount (required)
            if (!string.IsNullOrEmpty(mapping.AmountColumn) && element.TryGetProperty(mapping.AmountColumn, out var amountElement))
            {
                if (amountElement.ValueKind == JsonValueKind.Number)
                {
                    transaction.Amount = amountElement.GetDecimal();
                }
                else if (amountElement.ValueKind == JsonValueKind.String)
                {
                    transaction.Amount = decimal.Parse(amountElement.GetString()!);
                }
                else
                {
                    throw new InvalidOperationException("Amount must be a number or string");
                }
            }
            else
            {
                throw new InvalidOperationException("Amount property is required");
            }

            // Parse description (required)
            if (!string.IsNullOrEmpty(mapping.DescriptionColumn) && element.TryGetProperty(mapping.DescriptionColumn, out var descElement))
            {
                transaction.Description = descElement.GetString() ?? "No description";
            }
            else
            {
                transaction.Description = "No description";
            }

            // Parse counterparty (optional)
            if (!string.IsNullOrEmpty(mapping.CounterpartyColumn) && element.TryGetProperty(mapping.CounterpartyColumn, out var counterpartyElement))
            {
                transaction.Counterparty = counterpartyElement.GetString();
            }

            // Parse external ID (optional)
            if (!string.IsNullOrEmpty(mapping.ExternalIdColumn) && element.TryGetProperty(mapping.ExternalIdColumn, out var externalIdElement))
            {
                transaction.ExternalId = externalIdElement.GetString();
            }

            return transaction;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON transaction at line {LineNumber}", lineNumber);
            return null;
        }
    }
}
