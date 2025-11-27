using System.Globalization;
using LocalFinanceManager.Application.Interfaces;
using LocalFinanceManager.Domain.Entities;

namespace LocalFinanceManager.Infrastructure.Import;

/// <summary>
/// Importer for CSV and TSV transaction files.
/// </summary>
public class CsvTransactionImporter : ITransactionImporter
{
    /// <inheritdoc />
    public IEnumerable<string> SupportedExtensions => new[] { ".csv", ".tsv" };

    /// <inheritdoc />
    public bool CanHandle(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }

    /// <inheritdoc />
    public async Task<ImportResult> ImportAsync(Stream stream, ImportConfiguration configuration, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync(cancellationToken);
        return await ImportFromStringAsync(content, configuration, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ImportResult> ImportFromStringAsync(string content, ImportConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var result = new ImportResult();
        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length == 0)
        {
            result.Errors.Add("File is empty");
            return Task.FromResult(result);
        }

        var startIndex = configuration.HasHeader ? 1 : 0;
        var headers = configuration.HasHeader ? ParseLine(lines[0], configuration.Delimiter) : null;

        // Set up column mapping if not provided
        var mapping = configuration.ColumnMapping.Count > 0
            ? configuration.ColumnMapping
            : InferColumnMapping(headers);

        for (int i = startIndex; i < lines.Length; i++)
        {
            try
            {
                var originalCsv = lines[i];
                var fields = ParseLine(originalCsv, configuration.Delimiter);

                var transaction = ParseTransaction(fields, mapping, configuration, originalCsv);
                if (transaction != null)
                {
                    result.Transactions.Add(transaction);
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Line {i + 1}: {ex.Message}");
            }
        }

        return Task.FromResult(result);
    }

    private static string[] ParseLine(string line, char delimiter)
    {
        var fields = new List<string>();
        var currentField = new System.Text.StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // Escaped quote
                    currentField.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == delimiter && !inQuotes)
            {
                fields.Add(currentField.ToString().Trim());
                currentField.Clear();
            }
            else
            {
                currentField.Append(c);
            }
        }

        fields.Add(currentField.ToString().Trim());
        return fields.ToArray();
    }

    private static Dictionary<string, int> InferColumnMapping(string[]? headers)
    {
        var mapping = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        if (headers == null)
        {
            // Default positional mapping
            mapping["Date"] = 0;
            mapping["Amount"] = 1;
            mapping["Description"] = 2;
            mapping["CounterAccount"] = 3;
            return mapping;
        }

        var dateAliases = new[] { "date", "datum", "transaction date", "boekingsdatum" };
        var amountAliases = new[] { "amount", "bedrag", "value", "transactionamount" };
        var descriptionAliases = new[] { "description", "omschrijving", "memo", "details" };
        var counterAccountAliases = new[] { "counteraccount", "tegenrekening", "iban", "counterparty" };

        for (int i = 0; i < headers.Length; i++)
        {
            var header = headers[i].ToLowerInvariant().Trim();

            if (dateAliases.Contains(header) && !mapping.ContainsKey("Date"))
                mapping["Date"] = i;
            else if (amountAliases.Contains(header) && !mapping.ContainsKey("Amount"))
                mapping["Amount"] = i;
            else if (descriptionAliases.Contains(header) && !mapping.ContainsKey("Description"))
                mapping["Description"] = i;
            else if (counterAccountAliases.Contains(header) && !mapping.ContainsKey("CounterAccount"))
                mapping["CounterAccount"] = i;
        }

        return mapping;
    }

    private static Transaction? ParseTransaction(string[] fields, Dictionary<string, int> mapping, ImportConfiguration config, string originalCsv)
    {
        if (fields.Length == 0)
            return null;

        var transaction = new Transaction
        {
            AccountId = config.AccountId,
            CategoryId = config.DefaultCategoryId,
            OriginalCsv = originalCsv
        };

        // Parse Date
        if (mapping.TryGetValue("Date", out var dateIndex) && dateIndex < fields.Length)
        {
            if (DateTime.TryParse(fields[dateIndex], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                transaction.Date = date;
            }
            else if (DateTime.TryParseExact(fields[dateIndex], new[] { "yyyy-MM-dd", "dd-MM-yyyy", "MM/dd/yyyy", "dd/MM/yyyy" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                transaction.Date = date;
            }
            else
            {
                throw new FormatException($"Unable to parse date: {fields[dateIndex]}");
            }
        }

        // Parse Amount
        if (mapping.TryGetValue("Amount", out var amountIndex) && amountIndex < fields.Length)
        {
            var amountStr = fields[amountIndex]
                .Replace(",", ".")  // Handle European decimal separator
                .Replace(" ", "");  // Remove spaces

            if (decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
            {
                transaction.Amount = amount;
            }
            else
            {
                throw new FormatException($"Unable to parse amount: {fields[amountIndex]}");
            }
        }

        // Parse Description
        if (mapping.TryGetValue("Description", out var descIndex) && descIndex < fields.Length)
        {
            transaction.Description = fields[descIndex];
        }

        // Parse Counter Account
        if (mapping.TryGetValue("CounterAccount", out var counterIndex) && counterIndex < fields.Length)
        {
            transaction.CounterAccount = fields[counterIndex];
        }

        return transaction;
    }
}
