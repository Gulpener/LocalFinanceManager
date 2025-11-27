using LocalFinanceManager.Application.Interfaces;
using LocalFinanceManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation of the IRuleRepository interface.
/// </summary>
public class EfRuleRepository : IRuleRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfRuleRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public EfRuleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Rule>> GetAllOrderedByPriorityAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Rules
            .Include(r => r.TargetCategory)
            .Include(r => r.TargetEnvelope)
            .OrderByDescending(r => r.Priority)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Rule?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Rules
            .Include(r => r.TargetCategory)
            .Include(r => r.TargetEnvelope)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Rule> AddAsync(Rule rule, CancellationToken cancellationToken = default)
    {
        _context.Rules.Add(rule);
        await _context.SaveChangesAsync(cancellationToken);
        return rule;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Rule rule, CancellationToken cancellationToken = default)
    {
        _context.Rules.Update(rule);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var rule = await _context.Rules.FindAsync([id], cancellationToken);
        if (rule == null)
        {
            return false;
        }

        _context.Rules.Remove(rule);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
