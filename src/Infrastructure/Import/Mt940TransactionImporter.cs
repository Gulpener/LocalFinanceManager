using LocalFinanceManager.Application.Interfaces;
using LocalFinanceManager.Domain.Entities;

namespace LocalFinanceManager.Infrastructure.Import;

/// <summary>
/// Importer for MT940 bank statement files (SWIFT standard).
/// Note: This is a skeleton implementation. Full MT940 parsing is complex and may require
/// a dedicated library for production use.
/// </summary>
public class Mt940TransactionImporter : ITransactionImporter
{
    /// <inheritdoc />
    public IEnumerable<string> SupportedExtensions => new[] { ".mt940", ".sta" };

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

        try
        {
            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            Transaction? currentTransaction = null;
            string? accountNumber = null;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

                // Skip empty lines
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Account identification
                if (line.StartsWith(":25:"))
                {
                    accountNumber = line[4..];
                }
                // Statement line - transaction
                else if (line.StartsWith(":61:"))
                {
                    // Save previous transaction if exists
                    if (currentTransaction != null)
                    {
                        result.Transactions.Add(currentTransaction);
                    }

                    currentTransaction = ParseStatementLine(line, configuration, accountNumber);
                }
                // Transaction details
                else if (line.StartsWith(":86:") && currentTransaction != null)
                {
                    var details = line[4..];

                    // Try to extract counter account from details
                    if (details.Contains("COUNTER ACCOUNT:"))
                    {
                        var counterStart = details.IndexOf("COUNTER ACCOUNT:") + 16;
                        var counterEnd = details.IndexOf(' ', counterStart);
                        if (counterEnd == -1) counterEnd = details.Length;
                        currentTransaction.CounterAccount = details[counterStart..counterEnd].Trim();
                    }

                    // Append to description
                    currentTransaction.Description += " " + details;
                    currentTransaction.Description = currentTransaction.Description.Trim();
                }
            }

            // Add last transaction
            if (currentTransaction != null)
            {
                result.Transactions.Add(currentTransaction);
            }

            if (result.Transactions.Count == 0)
            {
                result.Errors.Add("No transactions found in MT940 file. This may be due to unsupported format variation.");
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Error parsing MT940 file: {ex.Message}");
        }

        return Task.FromResult(result);
    }

    private static Transaction? ParseStatementLine(string line, ImportConfiguration config, string? accountNumber)
    {
        // MT940 :61: format: YYMMDDYYMMDD[CD]amount[N]reference//description
        // This is a simplified parser - real MT940 has more complex formats

        try
        {
            var content = line[4..]; // Remove ":61:"

            // Parse date (YYMMDD format)
            var dateStr = content[..6];
            var year = 2000 + int.Parse(dateStr[..2]);
            var month = int.Parse(dateStr[2..4]);
            var day = int.Parse(dateStr[4..6]);
            var date = new DateTime(year, month, day);

            // Skip second date if present (YYMMDD)
            var pos = 6;
            if (content.Length > 12 && char.IsDigit(content[6]))
            {
                pos = 12;
            }

            // Parse credit/debit indicator
            var creditDebit = content[pos];
            pos++;

            // Handle reversal indicator
            if (content[pos] == 'R')
            {
                pos++;
            }

            // Parse amount (ends at N or non-numeric)
            var amountStart = pos;
            while (pos < content.Length && (char.IsDigit(content[pos]) || content[pos] == ',' || content[pos] == '.'))
            {
                pos++;
            }

            var amountStr = content[amountStart..pos].Replace(',', '.');
            var amount = decimal.Parse(amountStr, System.Globalization.CultureInfo.InvariantCulture);

            // Apply credit/debit sign
            amount = creditDebit == 'D' ? -Math.Abs(amount) : Math.Abs(amount);

            // Extract description from remainder
            var description = pos < content.Length ? content[pos..] : string.Empty;
            if (description.Contains("//"))
            {
                description = description[(description.IndexOf("//") + 2)..];
            }

            return new Transaction
            {
                AccountId = config.AccountId,
                CategoryId = config.DefaultCategoryId,
                Date = date,
                Amount = amount,
                Description = description.Trim(),
                CounterAccount = string.Empty,
                OriginalCsv = line
            };
        }
        catch
        {
            return null;
        }
    }
}
