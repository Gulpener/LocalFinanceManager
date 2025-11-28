using LocalFinanceManager.Application.Interfaces;
using LocalFinanceManager.Domain.Entities;

namespace LocalFinanceManager.Application.Services;

/// <summary>
/// Service implementation for transaction operations.
/// </summary>
public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionService"/> class.
    /// </summary>
    /// <param name="transactionRepository">The transaction repository.</param>
    public TransactionService(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    /// <inheritdoc />
    public async Task<Transaction?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _transactionRepository.GetByIdAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Transaction>> GetByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return await _transactionRepository.GetByAccountIdAsync(accountId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Transaction>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _transactionRepository.GetByDateRangeAsync(startDate, endDate, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        return await _transactionRepository.AddAsync(transaction, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _transactionRepository.UpdateAsync(transaction, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _transactionRepository.DeleteAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TransactionSplit?> GetSplitByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _transactionRepository.GetSplitByIdAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TransactionSplit>> GetSplitsByParentIdAsync(int parentTransactionId, CancellationToken cancellationToken = default)
    {
        return await _transactionRepository.GetSplitsByParentIdAsync(parentTransactionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TransactionSplit> AddSplitAsync(TransactionSplit split, CancellationToken cancellationToken = default)
    {
        return await _transactionRepository.AddSplitAsync(split, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TransactionSplit>> AddSplitsAsync(IEnumerable<TransactionSplit> splits, CancellationToken cancellationToken = default)
    {
        var splitList = splits.ToList();

        if (!splitList.Any())
        {
            return Array.Empty<TransactionSplit>();
        }

        // Ensure all splits reference the same parent and that parent exists
        var parentId = splitList.First().ParentTransactionId;
        var parent = await _transactionRepository.GetByIdAsync(parentId, cancellationToken);
        if (parent == null)
        {
            throw new InvalidOperationException("Parent transaction not found.");
        }

        // Validate sums: sum of splits must equal parent amount
        var total = splitList.Sum(s => s.Amount);
        if (total != parent.Amount)
        {
            throw new InvalidOperationException("Sum of split amounts does not equal parent transaction amount.");
        }

        return await _transactionRepository.AddSplitsAsync(splitList, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateSplitAsync(TransactionSplit split, CancellationToken cancellationToken = default)
    {
        await _transactionRepository.UpdateSplitAsync(split, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteSplitAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _transactionRepository.DeleteSplitAsync(id, cancellationToken);
    }
}
