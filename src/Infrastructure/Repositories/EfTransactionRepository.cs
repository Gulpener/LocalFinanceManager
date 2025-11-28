using LocalFinanceManager.Application.Interfaces;
using LocalFinanceManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation of the ITransactionRepository interface.
/// </summary>
public class EfTransactionRepository : ITransactionRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfTransactionRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public EfTransactionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Transaction?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Include(t => t.Envelope)
            .Include(t => t.Splits)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Transaction>> GetByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.Envelope)
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.Date)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Transaction>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Include(t => t.Envelope)
            .Where(t => t.Date >= startDate && t.Date <= endDate)
            .OrderByDescending(t => t.Date)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Transaction>> GetByCategoryIdAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.Envelope)
            .Where(t => t.CategoryId == categoryId)
            .OrderByDescending(t => t.Date)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);
        return transaction;
    }

    /// <inheritdoc />
    public async Task<TransactionSplit?> GetSplitByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.TransactionSplits
            .Include(s => s.Category)
            .Include(s => s.Envelope)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TransactionSplit>> GetSplitsByParentIdAsync(int parentTransactionId, CancellationToken cancellationToken = default)
    {
        return await _context.TransactionSplits
            .Include(s => s.Category)
            .Include(s => s.Envelope)
            .Where(s => s.ParentTransactionId == parentTransactionId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Transaction>> AddRangeAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default)
    {
        var transactionList = transactions.ToList();
        _context.Transactions.AddRange(transactionList);
        await _context.SaveChangesAsync(cancellationToken);
        return transactionList;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        _context.Transactions.Update(transaction);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TransactionSplit> AddSplitAsync(TransactionSplit split, CancellationToken cancellationToken = default)
    {
        _context.TransactionSplits.Add(split);
        await _context.SaveChangesAsync(cancellationToken);
        return split;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TransactionSplit>> AddSplitsAsync(IEnumerable<TransactionSplit> splits, CancellationToken cancellationToken = default)
    {
        var list = splits.ToList();
        _context.TransactionSplits.AddRange(list);
        await _context.SaveChangesAsync(cancellationToken);
        return list;
    }

    /// <inheritdoc />
    public async Task UpdateSplitAsync(TransactionSplit split, CancellationToken cancellationToken = default)
    {
        _context.TransactionSplits.Update(split);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Transactions.FindAsync([id], cancellationToken);
        if (transaction == null)
        {
            return false;
        }

        _context.Transactions.Remove(transaction);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByHashAsync(DateTime date, decimal amount, string description, int accountId, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .AnyAsync(t =>
                t.Date == date &&
                t.Amount == amount &&
                t.Description == description &&
                t.AccountId == accountId,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteSplitAsync(int id, CancellationToken cancellationToken = default)
    {
        var split = await _context.TransactionSplits.FindAsync([id], cancellationToken);
        if (split == null)
        {
            return false;
        }

        _context.TransactionSplits.Remove(split);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
