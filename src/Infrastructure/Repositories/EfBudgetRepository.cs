using LocalFinanceManager.Application.Interfaces;
using LocalFinanceManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation of the IBudgetRepository interface.
/// </summary>
public class EfBudgetRepository : IBudgetRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfBudgetRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public EfBudgetRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Budget>> GetByMonthAsync(DateTime month, CancellationToken cancellationToken = default)
    {
        var monthStart = new DateTime(month.Year, month.Month, 1);
        return await _context.Budgets
            .Include(b => b.Category)
            .Include(b => b.Envelope)
            .Include(b => b.Account)
            .Where(b => b.Month == monthStart)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Budget?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Budgets
            .Include(b => b.Category)
            .Include(b => b.Envelope)
            .Include(b => b.Account)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Budget> AddAsync(Budget budget, CancellationToken cancellationToken = default)
    {
        _context.Budgets.Add(budget);
        await _context.SaveChangesAsync(cancellationToken);
        return budget;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Budget budget, CancellationToken cancellationToken = default)
    {
        _context.Budgets.Update(budget);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var budget = await _context.Budgets.FindAsync([id], cancellationToken);
        if (budget == null)
        {
            return false;
        }

        _context.Budgets.Remove(budget);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
