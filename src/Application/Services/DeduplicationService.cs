using System.Security.Cryptography;
using System.Text;
using LocalFinanceManager.Application.Interfaces;
using LocalFinanceManager.Domain.Entities;

namespace LocalFinanceManager.Application.Services;

/// <summary>
/// Service for detecting and handling duplicate transactions during import.
/// </summary>
public class DeduplicationService : IDeduplicationService
{
    private readonly ITransactionRepository _transactionRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeduplicationService"/> class.
    /// </summary>
    /// <param name="transactionRepository">The transaction repository.</param>
    public DeduplicationService(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    /// <inheritdoc />
    public async Task<DeduplicationResult> CheckForDuplicatesAsync(
        IEnumerable<Transaction> transactions,
        int accountId,
        CancellationToken cancellationToken = default)
    {
        var result = new DeduplicationResult();
        var transactionList = transactions.ToList();

        foreach (var transaction in transactionList)
        {
            var isDuplicate = await _transactionRepository.ExistsByHashAsync(
                transaction.Date,
                transaction.Amount,
                transaction.Description,
                accountId,
                cancellationToken);

            if (isDuplicate)
            {
                result.Duplicates.Add(new DuplicateInfo
                {
                    NewTransaction = transaction,
                    SimilarityScore = 100 // Exact match
                });
            }
            else
            {
                result.UniqueTransactions.Add(transaction);
            }
        }

        return result;
    }

    /// <inheritdoc />
    public string ComputeHash(Transaction transaction)
    {
        var input = $"{transaction.Date:yyyy-MM-dd}|{transaction.Amount}|{transaction.Description}|{transaction.AccountId}";
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(inputBytes);
        return Convert.ToHexString(hashBytes);
    }
}
