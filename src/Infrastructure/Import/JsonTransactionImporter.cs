using System.Text.Json;
using LocalFinanceManager.Application.Interfaces;
using LocalFinanceManager.Domain.Entities;

namespace LocalFinanceManager.Infrastructure.Import;

/// <summary>
/// Importer for JSON transaction files.
/// </summary>
public class JsonTransactionImporter : ITransactionImporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public IEnumerable<string> SupportedExtensions => new[] { ".json" };

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
            var jsonTransactions = JsonSerializer.Deserialize<List<JsonTransactionDto>>(content, JsonOptions);

            if (jsonTransactions == null || jsonTransactions.Count == 0)
            {
                result.Errors.Add("No transactions found in JSON file");
                return Task.FromResult(result);
            }

            for (int i = 0; i < jsonTransactions.Count; i++)
            {
                try
                {
                    var dto = jsonTransactions[i];
                    var transaction = new Transaction
                    {
                        AccountId = configuration.AccountId,
                        CategoryId = configuration.DefaultCategoryId,
                        Date = dto.Date,
                        Amount = dto.Amount,
                        Description = dto.Description ?? string.Empty,
                        CounterAccount = dto.CounterAccount ?? string.Empty,
                        OriginalCsv = JsonSerializer.Serialize(dto, JsonOptions)
                    };

                    result.Transactions.Add(transaction);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Transaction {i + 1}: {ex.Message}");
                }
            }
        }
        catch (JsonException ex)
        {
            result.Errors.Add($"Invalid JSON format: {ex.Message}");
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// DTO for JSON transaction import.
    /// </summary>
    private class JsonTransactionDto
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public string? CounterAccount { get; set; }
        public string? AccountNumber { get; set; }
    }
}
