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
}
