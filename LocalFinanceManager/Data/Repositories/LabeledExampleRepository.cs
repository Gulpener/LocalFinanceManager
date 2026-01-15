using LocalFinanceManager.Models;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Data.Repositories;

/// <summary>
/// Repository interface for LabeledExample operations.
/// </summary>
public interface ILabeledExampleRepository
{
    /// <summary>
    /// Gets labeled examples within a rolling window for training data.
    /// </summary>
    /// <param name="windowDays">Number of days to look back</param>
    /// <returns>List of labeled examples within the window</returns>
    Task<List<LabeledExample>> GetTrainingDataAsync(int windowDays);

    /// <summary>
    /// Gets labeled examples for a specific category within a rolling window.
    /// </summary>
    /// <param name="categoryId">Category ID to filter by</param>
    /// <param name="windowDays">Number of days to look back</param>
    /// <returns>List of labeled examples for the category</returns>
    Task<List<LabeledExample>> GetByCategoryAsync(Guid categoryId, int windowDays);

    /// <summary>
    /// Gets count of labeled examples per category within a rolling window.
    /// </summary>
    /// <param name="windowDays">Number of days to look back</param>
    /// <returns>Dictionary mapping category ID to count</returns>
    Task<Dictionary<Guid, int>> GetCountPerCategoryAsync(int windowDays);

    /// <summary>
    /// Adds a new labeled example (user assignment/correction).
    /// </summary>
    Task AddAsync(LabeledExample example);

    /// <summary>
    /// Gets labeled example for a specific transaction (most recent).
    /// </summary>
    Task<LabeledExample?> GetByTransactionIdAsync(Guid transactionId);

    /// <summary>
    /// Gets acceptance rate statistics for ML suggestions.
    /// </summary>
    /// <param name="windowDays">Number of days to look back</param>
    /// <returns>Tuple of (accepted count, total suggestions)</returns>
    Task<(int AcceptedCount, int TotalSuggestions)> GetAcceptanceRateAsync(int windowDays);
}

/// <summary>
/// Repository for LabeledExample entity operations.
/// </summary>
public class LabeledExampleRepository : ILabeledExampleRepository
{
    private readonly AppDbContext _context;

    public LabeledExampleRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<List<LabeledExample>> GetTrainingDataAsync(int windowDays)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-windowDays);

        return await _context.LabeledExamples
            .Include(le => le.Transaction)
            .Include(le => le.Category)
            .Where(le => !le.IsArchived)
            .Where(le => le.CreatedAt >= cutoffDate)
            .OrderByDescending(le => le.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<List<LabeledExample>> GetByCategoryAsync(Guid categoryId, int windowDays)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-windowDays);

        return await _context.LabeledExamples
            .Include(le => le.Transaction)
            .Where(le => !le.IsArchived)
            .Where(le => le.CategoryId == categoryId)
            .Where(le => le.CreatedAt >= cutoffDate)
            .OrderByDescending(le => le.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<Dictionary<Guid, int>> GetCountPerCategoryAsync(int windowDays)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-windowDays);

        return await _context.LabeledExamples
            .Where(le => !le.IsArchived)
            .Where(le => le.CreatedAt >= cutoffDate)
            .GroupBy(le => le.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count);
    }

    /// <inheritdoc/>
    public async Task AddAsync(LabeledExample example)
    {
        await _context.LabeledExamples.AddAsync(example);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<LabeledExample?> GetByTransactionIdAsync(Guid transactionId)
    {
        return await _context.LabeledExamples
            .Where(le => !le.IsArchived)
            .Where(le => le.TransactionId == transactionId)
            .OrderByDescending(le => le.CreatedAt)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    public async Task<(int AcceptedCount, int TotalSuggestions)> GetAcceptanceRateAsync(int windowDays)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-windowDays);

        var withSuggestions = await _context.LabeledExamples
            .Where(le => !le.IsArchived)
            .Where(le => le.CreatedAt >= cutoffDate)
            .Where(le => le.AcceptedSuggestion != null)
            .ToListAsync();

        var totalSuggestions = withSuggestions.Count;
        var acceptedCount = withSuggestions.Count(le => le.AcceptedSuggestion == true);

        return (acceptedCount, totalSuggestions);
    }
}
