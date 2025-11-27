using LocalFinanceManager.Application.Interfaces;
using LocalFinanceManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation of the IEnvelopeRepository interface.
/// </summary>
public class EfEnvelopeRepository : IEnvelopeRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfEnvelopeRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public EfEnvelopeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Envelope?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Envelopes.FindAsync([id], cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Envelope>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Envelopes.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Envelope> AddAsync(Envelope envelope, CancellationToken cancellationToken = default)
    {
        _context.Envelopes.Add(envelope);
        await _context.SaveChangesAsync(cancellationToken);
        return envelope;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Envelope envelope, CancellationToken cancellationToken = default)
    {
        _context.Envelopes.Update(envelope);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var envelope = await _context.Envelopes.FindAsync([id], cancellationToken);
        if (envelope == null)
        {
            return false;
        }

        _context.Envelopes.Remove(envelope);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
