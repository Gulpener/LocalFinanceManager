using LocalFinanceManager.Models;
using LocalFinanceManager.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocalFinanceManager.Data.Repositories;

/// <summary>
/// Repository implementation for Account entity.
/// Encapsulates soft-delete filtering and Account-specific queries.
/// </summary>
public class AccountRepository : Repository<Account>, IAccountRepository
{
    private readonly IUserContext _userContext;

    public AccountRepository(AppDbContext context, ILogger<Repository<Account>> logger, IUserContext userContext)
        : base(context, logger)
    {
        _userContext = userContext;
    }

    public override async Task<Account?> GetByIdAsync(Guid id)
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return null;
        }

        return await _context.Set<Account>()
            .Where(a => !a.IsArchived && a.Id == id && a.UserId == userId)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Get an account by ID that is readable by the current user (owned or accepted shared).
    /// Use for read-only operations. Write operations use GetByIdAsync (owner-only).
    /// </summary>
    public async Task<Account?> GetReadableByIdAsync(Guid id)
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return null;
        }

        return await _context.Set<Account>()
            .Where(a => !a.IsArchived
                && a.Id == id
                && (a.UserId == userId
                    || a.Shares.Any(s => s.SharedWithUserId == userId && s.Status == Models.ShareStatus.Accepted && !s.IsArchived)))
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Get all active (non-archived) accounts for the current user (owned or accepted shared).
    /// </summary>
    public async Task<List<Account>> GetAllActiveAsync()
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return new List<Account>();
        }

        var query = _context.Set<Account>().Where(a => !a.IsArchived
            && (a.UserId == userId
                || a.Shares.Any(s => s.SharedWithUserId == userId && s.Status == Models.ShareStatus.Accepted && !s.IsArchived)));

        return await query
            .Include(a => a.User)
            .Include(a => a.Shares)
            .OrderBy(a => a.Label)
            .ToListAsync();
    }

    /// <summary>
    /// Check if an account with the given label already exists for the current user.
    /// </summary>
    public async Task<bool> LabelExistsAsync(string label, Guid? excludeId = null)
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return false;
        }

        var query = _context.Set<Account>().Where(a => !a.IsArchived && a.Label == label && a.UserId == userId);

        if (excludeId.HasValue)
        {
            query = query.Where(a => a.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    /// <summary>
    /// Returns the count of active (non-archived) accounts accessible by the current user (owned or accepted shared).
    /// </summary>
    public async Task<int> CountActiveAsync()
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return 0;
        }

        return await _context.Set<Account>()
            .CountAsync(a => !a.IsArchived
                && (a.UserId == userId
                    || a.Shares.Any(s => s.SharedWithUserId == userId && s.Status == Models.ShareStatus.Accepted && !s.IsArchived)));
    }
}
