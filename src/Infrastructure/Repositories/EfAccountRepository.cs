using LocalFinanceManager.Application.Interfaces;
using LocalFinanceManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation of the IAccountRepository interface.
/// </summary>
public class EfAccountRepository : IAccountRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfAccountRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public EfAccountRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Account?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts.FindAsync([id], cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Accounts.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Account>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Accounts
            .Where(a => a.IsActive)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Account> AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync(cancellationToken);
        return account;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Account account, CancellationToken cancellationToken = default)
    {
        _context.Accounts.Update(account);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var account = await _context.Accounts.FindAsync([id], cancellationToken);
        if (account == null)
        {
            return false;
        }

        _context.Accounts.Remove(account);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
