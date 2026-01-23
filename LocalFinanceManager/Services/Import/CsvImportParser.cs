using System.Globalization;
using System.Text;
using LocalFinanceManager.DTOs;
using Microsoft.Extensions.Logging;

namespace LocalFinanceManager.Services.Import;

/// <summary>
/// CSV file parser with configurable column mapping and auto-detection.
/// </summary>
public class CsvImportParser : IImportParser
{
    private readonly ILogger<CsvImportParser> _logger;

    public CsvImportParser(ILogger<CsvImportParser> logger)
    {
        _logger = logger;
    }

    public async Task<List<ParsedTransactionDto>> ParseAsync(string fileContent, ColumnMappingDto mapping)
    {
        var transactions = new List<ParsedTransactionDto>();
        var lines = await ReadLinesAsync(fileContent);

        if (lines.Count == 0)
        {
            return transactions;
        }

        // Detect delimiter from first line
        char delimiter = DetectDelimiter(lines[0]);

        // First line is header
        var headers = ParseCsvLine(lines[0], delimiter);
        var columnIndices = MapColumns(headers, mapping);

        // Parse data rows
        for (int i = 1; i < lines.Count; i++)
        {
            try
            {
                var values = ParseCsvLine(lines[i], delimiter);
                var transaction = ParseTransaction(values, columnIndices, lines[i], i + 1);
                if (transaction != null)
                {
                    transactions.Add(transaction);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing line {LineNumber}", i + 1);
                // Continue parsing other rows
            }
        }

        return transactions;
    }

    public async Task<List<string>> DetectColumnsAsync(string fileContent)
    {
        var lines = await ReadLinesAsync(fileContent);
        if (lines.Count == 0)
        {
            return new List<string>();
        }

        char delimiter = DetectDelimiter(lines[0]);
        return ParseCsvLine(lines[0], delimiter);
    }

    public async Task<ColumnMappingDto> AutoDetectMappingAsync(string fileContent)
    {
        var columns = await DetectColumnsAsync(fileContent);
        var mapping = new ColumnMappingDto();
        string? debitCreditColumn = null;

        foreach (var column in columns)
        {
            var lowerColumn = column.ToLowerInvariant().Trim();

            // Detect date column
            if (mapping.DateColumn == null &&
                (lowerColumn.Contains("date") || lowerColumn.Contains("datum") ||
                 lowerColumn.Contains("transaction date") || lowerColumn == "transactiedatum"))
            {
                mapping.DateColumn = column;
            }

            // Detect debit/credit indicator column (separate from amount)
            if (debitCreditColumn == null &&
                (lowerColumn == "af bij" || lowerColumn == "debit/credit" ||
                 lowerColumn == "bij af" || lowerColumn == "d/c" || lowerColumn == "dc"))
            {
                debitCreditColumn = column;
            }

            // Detect amount column
            if (mapping.AmountColumn == null &&
                (lowerColumn.Contains("amount") || lowerColumn.Contains("bedrag") ||
                 lowerColumn.Contains("value")))
            {
                mapping.AmountColumn = column;
            }

            // Detect description column
            if (mapping.DescriptionColumn == null &&
                (lowerColumn.Contains("description") || lowerColumn.Contains("omschrijving") ||
                 lowerColumn.Contains("memo") || lowerColumn.Contains("details") ||
                 lowerColumn.Contains("mededelingen")))
            {
                mapping.DescriptionColumn = column;
            }

            // Detect counterparty column
            if (mapping.CounterpartyColumn == null &&
                (lowerColumn.Contains("counterparty") || lowerColumn.Contains("tegenpartij") ||
                 lowerColumn.Contains("naam") || lowerColumn.Contains("merchant") ||
                 lowerColumn.Contains("beneficiary") || lowerColumn == "naam / omschrijving"))
            {
                mapping.CounterpartyColumn = column;
            }

            // Detect external ID column
            if (mapping.ExternalIdColumn == null &&
                (lowerColumn.Contains("reference") || lowerColumn.Contains("referentie") ||
                 lowerColumn.Contains("transaction id") || lowerColumn.Contains("id") ||
                 lowerColumn == "transactiereferentie"))
            {
                mapping.ExternalIdColumn = column;
            }
        }

        // Store debit/credit column as custom property if detected
        // (We'll use ExternalIdColumn temporarily to pass this info)
        if (debitCreditColumn != null && mapping.AmountColumn != null)
        {
            _logger.LogInformation("Detected separate debit/credit column: {DebitCreditColumn}", debitCreditColumn);
        }

        _logger.LogInformation("Auto-detected mapping: Date={DateColumn}, Amount={AmountColumn}, Description={DescriptionColumn}",
            mapping.DateColumn, mapping.AmountColumn, mapping.DescriptionColumn);

        return mapping;
    }

    private async Task<List<string>> ReadLinesAsync(string fileContent)
    {
        var lines = new List<string>();
        using var reader = new StringReader(fileContent);
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                lines.Add(line);
            }
        }
        return lines;
    }

    private char DetectDelimiter(string headerLine)
    {
        // Count occurrences of common delimiters (outside quotes)
        var delimiters = new[] { ',', ';', '\t' };
        var counts = new Dictionary<char, int>();
        bool inQuotes = false;

        foreach (var delimiter in delimiters)
        {
            counts[delimiter] = 0;
        }

        for (int i = 0; i < headerLine.Length; i++)
        {
            char c = headerLine[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < headerLine.Length && headerLine[i + 1] == '"')
                {
                    i++; // Skip escaped quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (!inQuotes && delimiters.Contains(c))
            {
                counts[c]++;
            }
        }

        // Return delimiter with highest count (must be > 0)
        var bestDelimiter = counts.Where(kv => kv.Value > 0)
                                  .OrderByDescending(kv => kv.Value)
                                  .FirstOrDefault();

        char result = bestDelimiter.Key != default ? bestDelimiter.Key : ',';
        _logger.LogInformation("Detected CSV delimiter: '{Delimiter}' (counts: {Counts})",
            result == '\t' ? "\\t" : result.ToString(),
            string.Join(", ", counts.Select(kv => $"{(kv.Key == '\t' ? "\\t" : kv.Key.ToString())}={kv.Value}")));

        return result;
    }

    private List<string> ParseCsvLine(string line, char delimiter = ',')
    {
        var values = new List<string>();
        var currentValue = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // Escaped quote
                    currentValue.Append('"');
                    i++; // Skip next quote
                }
                else
                {
                    // Toggle quote mode
                    inQuotes = !inQuotes;
                }
            }
            else if (c == delimiter && !inQuotes)
            {
                // End of field
                values.Add(currentValue.ToString().Trim());
                currentValue.Clear();
            }
            else
            {
                currentValue.Append(c);
            }
        }

        // Add last field
        values.Add(currentValue.ToString().Trim());

        return values;
    }

    private Dictionary<string, int> MapColumns(List<string> headers, ColumnMappingDto mapping)
    {
        var indices = new Dictionary<string, int>();

        for (int i = 0; i < headers.Count; i++)
        {
            var header = headers[i];
            var lowerHeader = header.ToLowerInvariant().Trim();

            if (header == mapping.DateColumn)
                indices["Date"] = i;
            if (header == mapping.AmountColumn)
                indices["Amount"] = i;
            if (header == mapping.DescriptionColumn)
                indices["Description"] = i;
            if (header == mapping.CounterpartyColumn)
                indices["Counterparty"] = i;
            if (header == mapping.ExternalIdColumn)
                indices["ExternalId"] = i;

            // Auto-detect debit/credit column if present
            if (!indices.ContainsKey("DebitCredit") &&
                (lowerHeader == "af bij" || lowerHeader == "debit/credit" ||
                 lowerHeader == "bij af" || lowerHeader == "d/c" || lowerHeader == "dc"))
            {
                indices["DebitCredit"] = i;
                _logger.LogInformation("Detected debit/credit indicator column at index {Index}: {Header}", i, header);
            }
        }

        return indices;
    }

    private ParsedTransactionDto? ParseTransaction(List<string> values, Dictionary<string, int> columnIndices, string originalLine, int lineNumber)
    {
        try
        {
            var transaction = new ParsedTransactionDto
            {
                LineNumber = lineNumber,
                OriginalImport = originalLine
            };

            // Parse date (required)
            if (columnIndices.TryGetValue("Date", out int dateIndex) && dateIndex < values.Count)
            {
                transaction.Date = ParseDate(values[dateIndex]);
            }
            else
            {
                throw new InvalidOperationException("Date column is required");
            }

            // Parse amount (required)
            if (columnIndices.TryGetValue("Amount", out int amountIndex) && amountIndex < values.Count)
            {
                var amountValue = values[amountIndex];
                transaction.Amount = ParseAmount(amountValue);

                // Check if there's a separate debit/credit indicator column
                if (columnIndices.TryGetValue("DebitCredit", out int dcIndex) && dcIndex < values.Count)
                {
                    var dcValue = values[dcIndex].Trim().ToLowerInvariant();
                    // If amount is positive but indicator says debit ("af"), make it negative
                    if ((dcValue == "af" || dcValue == "debit" || dcValue == "d") && transaction.Amount > 0)
                    {
                        transaction.Amount = -transaction.Amount;
                    }
                    // If amount is negative but indicator says credit ("bij"), make it positive  
                    else if ((dcValue == "bij" || dcValue == "credit" || dcValue == "c") && transaction.Amount < 0)
                    {
                        transaction.Amount = Math.Abs(transaction.Amount);
                    }
                }
            }
            else
            {
                throw new InvalidOperationException("Amount column is required");
            }

            // Parse description (required)
            if (columnIndices.TryGetValue("Description", out int descIndex) && descIndex < values.Count)
            {
                transaction.Description = values[descIndex];
            }
            else
            {
                transaction.Description = "No description";
            }

            // Parse counterparty (optional)
            if (columnIndices.TryGetValue("Counterparty", out int counterpartyIndex) && counterpartyIndex < values.Count)
            {
                transaction.Counterparty = values[counterpartyIndex];
            }

            // Parse external ID (optional)
            if (columnIndices.TryGetValue("ExternalId", out int externalIdIndex) && externalIdIndex < values.Count)
            {
                transaction.ExternalId = values[externalIdIndex];
            }

            return transaction;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse transaction at line {LineNumber}", lineNumber);
            return null;
        }
    }

    private DateTime ParseDate(string value)
    {
        // Remove quotes if present
        value = value.Trim().Trim('"');

        // Try multiple date formats
        var formats = new[]
        {
            "yyyyMMdd",          // ING format: 20230215
            "yyyy-MM-dd",
            "dd-MM-yyyy",
            "MM/dd/yyyy",
            "dd/MM/yyyy",
            "yyyy/MM/dd",
            "dd.MM.yyyy"
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return date;
            }
        }

        // Fallback to general parsing
        if (DateTime.TryParse(value, out var parsedDate))
        {
            return parsedDate;
        }

        throw new FormatException($"Unable to parse date: {value}");
    }

    private decimal ParseAmount(string value)
    {
        // Remove quotes, currency symbols and whitespace
        value = value.Trim().Trim('"').Replace("€", "").Replace("$", "").Replace("£", "").Replace(" ", "");

        // Handle different decimal separators
        // European format: 1.234,56 -> 1234.56
        // US format: 1,234.56 -> 1234.56

        if (value.Contains(",") && value.Contains("."))
        {
            // Determine which is the decimal separator
            int lastComma = value.LastIndexOf(',');
            int lastDot = value.LastIndexOf('.');

            if (lastComma > lastDot)
            {
                // European format
                value = value.Replace(".", "").Replace(",", ".");
            }
            else
            {
                // US format
                value = value.Replace(",", "");
            }
        }
        else if (value.Contains(","))
        {
            // Could be European decimal or thousands separator
            // Assume decimal if only one comma and it's near the end
            int commaPos = value.LastIndexOf(',');
            if (commaPos > value.Length - 4)
            {
                // Likely decimal separator
                value = value.Replace(",", ".");
            }
            else
            {
                // Likely thousands separator
                value = value.Replace(",", "");
            }
        }

        if (decimal.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount))
        {
            return amount;
        }

        throw new FormatException($"Unable to parse amount: {value}");
    }
}
