using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LocalFinanceManager.Models;
using LocalFinanceManager.Services;

namespace LocalFinanceManager.Data.Repositories;

/// <summary>
/// Repository implementation for Transaction entity.
/// </summary>
public class TransactionRepository : Repository<Transaction>, ITransactionRepository
{
    private readonly IUserContext _userContext;

    public TransactionRepository(AppDbContext context, ILogger<TransactionRepository> logger, IUserContext userContext)
        : base(context, logger)
    {
        _userContext = userContext;
    }

    /// <summary>
    /// Returns account IDs accessible to the user via direct AccountShare or BudgetPlanShare.
    /// Combines both paths with a SQL UNION so the set is computed once and used as an IN-list,
    /// avoiding correlated subqueries that re-evaluate per transaction row.
    /// </summary>
    private IQueryable<Guid> GetSharedAccessibleAccountIdsQuery(Guid userId)
    {
        var directShares = _context.AccountShares
            .Where(s => s.SharedWithUserId == userId && s.Status == ShareStatus.Accepted && !s.IsArchived)
            .Select(s => s.AccountId);

        var budgetPlanSharedAccounts = _context.Accounts
            .Where(a => !a.IsArchived
                && a.CurrentBudgetPlanId != null
                && _context.BudgetPlanShares.Any(s =>
                    s.BudgetPlanId == a.CurrentBudgetPlanId
                    && s.SharedWithUserId == userId
                    && s.Status == ShareStatus.Accepted
                    && !s.IsArchived))
            .Select(a => a.Id);

        return directShares.Union(budgetPlanSharedAccounts);
    }

    /// <summary>
    /// Override GetActiveAsync to include proper ordering by date and user filtering.
    /// Includes transactions for accounts shared with the current user (cascade from AccountShare or BudgetPlanShare).
    /// </summary>
    public new async Task<List<Transaction>> GetActiveAsync()
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return new List<Transaction>();
        }

        var accessibleAccountIds = GetSharedAccessibleAccountIdsQuery(userId);
        var query = _dbSet.Where(t => !t.IsArchived
            && (t.UserId == userId || accessibleAccountIds.Contains(t.AccountId)));

        return await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Transaction>> GetByAccountIdAsync(Guid accountId)
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return new List<Transaction>();
        }

        var accessibleAccountIds = GetSharedAccessibleAccountIdsQuery(userId);
        var query = _dbSet.Where(t => !t.IsArchived && t.AccountId == accountId
            && (t.UserId == userId || accessibleAccountIds.Contains(t.AccountId)));

        return await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Transaction>> FindExactMatchesAsync(DateTime date, decimal amount, string? externalId)
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return new List<Transaction>();
        }

        var query = _dbSet.Where(t => !t.IsArchived && t.Date == date && t.Amount == amount && t.UserId == userId);

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
        var userId = _userContext.GetCurrentUserId();

        if (userId == Guid.Empty)
        {
            return new List<Transaction>();
        }

        var query = _dbSet.Where(t => !t.IsArchived
            && t.Date >= startDate
            && t.Date <= endDate
            && t.Amount == amount
            && t.UserId == userId);

        return await query.ToListAsync();
    }

    public async Task<List<Transaction>> GetByImportBatchIdAsync(Guid importBatchId)
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return new List<Transaction>();
        }

        var query = _dbSet.Where(t => !t.IsArchived && t.ImportBatchId == importBatchId && t.UserId == userId);

        return await query.OrderBy(t => t.Date).ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Transaction> transactions)
    {
        var userId = _userContext.GetCurrentUserId();

        if (userId == Guid.Empty)
        {
            throw new InvalidOperationException("Cannot add transactions without an authenticated user context.");
        }

        foreach (var transaction in transactions)
        {
            transaction.Id = Guid.NewGuid();
            transaction.UserId = userId;
        }

        await _dbSet.AddRangeAsync(transactions);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added {Count} transactions in batch", transactions.Count());
    }

    public async Task<Transaction?> GetByIdWithAccountAsync(Guid id)
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return null;
        }

        var accessibleAccountIds = GetSharedAccessibleAccountIdsQuery(userId);
        var query = _dbSet.Include(t => t.Account).Where(t => t.Id == id && !t.IsArchived
            && (t.UserId == userId || accessibleAccountIds.Contains(t.AccountId)));

        return await query.FirstOrDefaultAsync();
    }

    public async Task<List<Transaction>> GetAllWithSplitsAsync()
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return new List<Transaction>();
        }

        var accessibleAccountIds = GetSharedAccessibleAccountIdsQuery(userId);
        var query = _dbSet.Where(t => !t.IsArchived
            && (t.UserId == userId || accessibleAccountIds.Contains(t.AccountId)));

        return await query
            .Include(t => t.Account)
            .Include(t => t.AssignedParts!)
                .ThenInclude(s => s.BudgetLine)
                    .ThenInclude(bl => bl.Category)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Transaction>> GetByAccountIdWithSplitsAsync(Guid accountId)
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return new List<Transaction>();
        }

        var accessibleAccountIds = GetSharedAccessibleAccountIdsQuery(userId);
        var query = _dbSet.Where(t => !t.IsArchived && t.AccountId == accountId
            && (t.UserId == userId || accessibleAccountIds.Contains(t.AccountId)));

        return await query
            .Include(t => t.Account)
            .Include(t => t.AssignedParts!)
                .ThenInclude(s => s.BudgetLine)
                    .ThenInclude(bl => bl.Category)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Transaction>> GetRecentWithSplitsAsync(int count)
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return new List<Transaction>();
        }

        var accessibleAccountIds = GetSharedAccessibleAccountIdsQuery(userId);
        var query = _dbSet.Where(t => !t.IsArchived
            && (t.UserId == userId || accessibleAccountIds.Contains(t.AccountId)));

        return await query
            .Include(t => t.Account)
            .Include(t => t.AssignedParts!)
                .ThenInclude(s => s.BudgetLine)
                    .ThenInclude(bl => bl.Category)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<int> CountUnassignedAsync()
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return 0;
        }

        var accessibleAccountIds = GetSharedAccessibleAccountIdsQuery(userId);
        return await _dbSet
            .CountAsync(t => !t.IsArchived
                && (t.UserId == userId || accessibleAccountIds.Contains(t.AccountId))
                && !t.AssignedParts!.Any());
    }
}
