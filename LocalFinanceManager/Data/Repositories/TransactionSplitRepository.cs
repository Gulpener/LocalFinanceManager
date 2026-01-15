using LocalFinanceManager.Models;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Data.Repositories;

/// <summary>
/// Repository interface for TransactionSplit entity.
/// </summary>
public interface ITransactionSplitRepository
{
    Task<TransactionSplit?> GetByIdAsync(Guid id);
    Task<List<TransactionSplit>> GetByTransactionIdAsync(Guid transactionId);
    Task AddAsync(TransactionSplit split);
    Task AddRangeAsync(IEnumerable<TransactionSplit> splits);
    Task UpdateAsync(TransactionSplit split);
    Task DeleteAsync(Guid id);
    Task DeleteByTransactionIdAsync(Guid transactionId);
}

/// <summary>
/// Repository implementation for TransactionSplit entity.
/// </summary>
public class TransactionSplitRepository : ITransactionSplitRepository
{
    private readonly AppDbContext _context;

    public TransactionSplitRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TransactionSplit?> GetByIdAsync(Guid id)
    {
        return await _context.TransactionSplits
            .Include(ts => ts.BudgetLine)
            .Include(ts => ts.Category)
            .Where(ts => !ts.IsArchived)
            .FirstOrDefaultAsync(ts => ts.Id == id);
    }

    public async Task<List<TransactionSplit>> GetByTransactionIdAsync(Guid transactionId)
    {
        return await _context.TransactionSplits
            .Include(ts => ts.BudgetLine)
            .Include(ts => ts.Category)
            .Where(ts => ts.TransactionId == transactionId && !ts.IsArchived)
            .ToListAsync();
    }

    public async Task AddAsync(TransactionSplit split)
    {
        await _context.TransactionSplits.AddAsync(split);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<TransactionSplit> splits)
    {
        await _context.TransactionSplits.AddRangeAsync(splits);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(TransactionSplit split)
    {
        _context.TransactionSplits.Update(split);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var split = await _context.TransactionSplits.FindAsync(id);
        if (split != null)
        {
            split.IsArchived = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteByTransactionIdAsync(Guid transactionId)
    {
        var splits = await _context.TransactionSplits
            .Where(ts => ts.TransactionId == transactionId && !ts.IsArchived)
            .ToListAsync();

        foreach (var split in splits)
        {
            split.IsArchived = true;
        }

        if (splits.Any())
        {
            await _context.SaveChangesAsync();
        }
    }
}
