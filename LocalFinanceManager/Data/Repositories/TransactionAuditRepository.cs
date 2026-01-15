using LocalFinanceManager.Models;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Data.Repositories;

/// <summary>
/// Repository interface for TransactionAudit entity.
/// </summary>
public interface ITransactionAuditRepository
{
    Task<TransactionAudit?> GetByIdAsync(Guid id);
    Task<List<TransactionAudit>> GetByTransactionIdAsync(Guid transactionId, int count = 10);
    Task<TransactionAudit?> GetLatestByTransactionIdAsync(Guid transactionId);
    Task AddAsync(TransactionAudit audit);
    Task<List<TransactionAudit>> GetRecentAuditsAsync(int days = 30);
}

/// <summary>
/// Repository implementation for TransactionAudit entity.
/// </summary>
public class TransactionAuditRepository : ITransactionAuditRepository
{
    private readonly AppDbContext _context;

    public TransactionAuditRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TransactionAudit?> GetByIdAsync(Guid id)
    {
        return await _context.TransactionAudits
            .Where(ta => !ta.IsArchived)
            .FirstOrDefaultAsync(ta => ta.Id == id);
    }

    public async Task<List<TransactionAudit>> GetByTransactionIdAsync(Guid transactionId, int count = 10)
    {
        return await _context.TransactionAudits
            .Where(ta => ta.TransactionId == transactionId && !ta.IsArchived)
            .OrderByDescending(ta => ta.ChangedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<TransactionAudit?> GetLatestByTransactionIdAsync(Guid transactionId)
    {
        return await _context.TransactionAudits
            .Where(ta => ta.TransactionId == transactionId && !ta.IsArchived)
            .OrderByDescending(ta => ta.ChangedAt)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(TransactionAudit audit)
    {
        await _context.TransactionAudits.AddAsync(audit);
        await _context.SaveChangesAsync();
    }

    public async Task<List<TransactionAudit>> GetRecentAuditsAsync(int days = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        return await _context.TransactionAudits
            .Where(ta => ta.ChangedAt >= cutoffDate && !ta.IsArchived)
            .OrderByDescending(ta => ta.ChangedAt)
            .ToListAsync();
    }
}
