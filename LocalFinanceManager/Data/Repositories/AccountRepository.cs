using LocalFinanceManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocalFinanceManager.Data.Repositories;

/// <summary>
/// Repository implementation for Account entity.
/// Encapsulates soft-delete filtering and Account-specific queries.
/// </summary>
public class AccountRepository : Repository<Account>, IAccountRepository
{
    public AccountRepository(AppDbContext context, ILogger<Repository<Account>> logger) : base(context, logger)
    {
    }

    /// <summary>
    /// Get all active (non-archived) accounts.
    /// </summary>
    public async Task<List<Account>> GetAllActiveAsync()
    {
        return await _context.Set<Account>()
            .Where(a => !a.IsArchived)
            .OrderBy(a => a.Label)
            .ToListAsync();
    }

    /// <summary>
    /// Check if an account with the given label already exists.
    /// </summary>
    public async Task<bool> LabelExistsAsync(string label, Guid? excludeId = null)
    {
        var query = _context.Set<Account>()
            .Where(a => !a.IsArchived && a.Label == label);

        if (excludeId.HasValue)
        {
            query = query.Where(a => a.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }
}
