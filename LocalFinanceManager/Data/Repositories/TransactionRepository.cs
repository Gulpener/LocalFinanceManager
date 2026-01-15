using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LocalFinanceManager.Models;

namespace LocalFinanceManager.Data.Repositories;

/// <summary>
/// Repository implementation for Transaction entity.
/// </summary>
public class TransactionRepository : Repository<Transaction>, ITransactionRepository
{
    public TransactionRepository(AppDbContext context, ILogger<TransactionRepository> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// Override GetActiveAsync to include proper ordering by date.
    /// </summary>
    public new async Task<List<Transaction>> GetActiveAsync()
    {
        return await _dbSet
            .Where(t => !t.IsArchived)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Transaction>> GetByAccountIdAsync(Guid accountId)
    {
        return await _dbSet
            .Where(t => !t.IsArchived && t.AccountId == accountId)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Transaction>> FindExactMatchesAsync(DateTime date, decimal amount, string? externalId)
    {
        var query = _dbSet
            .Where(t => !t.IsArchived && t.Date == date && t.Amount == amount);

        if (!string.IsNullOrEmpty(externalId))
        {
            query = query.Where(t => t.ExternalId == externalId);
        }

        return await query.ToListAsync();
    }

    public async Task<List<Transaction>> FindFuzzyMatchCandidatesAsync(DateTime date, decimal amount, int daysTolerance = 2)
    {
        var startDate = date.AddDays(-daysTolerance);
        var endDate = date.AddDays(daysTolerance);

        return await _dbSet
            .Where(t => !t.IsArchived
                && t.Date >= startDate
                && t.Date <= endDate
                && t.Amount == amount)
            .ToListAsync();
    }

    public async Task<List<Transaction>> GetByImportBatchIdAsync(Guid importBatchId)
    {
        return await _dbSet
            .Where(t => !t.IsArchived && t.ImportBatchId == importBatchId)
            .OrderBy(t => t.Date)
            .ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Transaction> transactions)
    {
        foreach (var transaction in transactions)
        {
            transaction.Id = Guid.NewGuid();
        }

        await _dbSet.AddRangeAsync(transactions);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added {Count} transactions in batch", transactions.Count());
    }
}
