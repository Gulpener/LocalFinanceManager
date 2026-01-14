using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LocalFinanceManager.Models;

namespace LocalFinanceManager.Data.Repositories;

/// <summary>
/// Generic repository implementation with soft-delete filtering and concurrency handling.
/// </summary>
/// <typeparam name="T">Entity type that inherits from BaseEntity</typeparam>
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;
    protected readonly ILogger<Repository<T>> _logger;

    public Repository(AppDbContext context, ILogger<Repository<T>> logger)
    {
        _context = context;
        _dbSet = context.Set<T>();
        _logger = logger;
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await _dbSet
            .Where(e => !e.IsArchived && e.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<List<T>> GetActiveAsync()
    {
        return await _dbSet
            .Where(e => !e.IsArchived)
            .ToListAsync();
    }

    public async Task AddAsync(T entity)
    {
        entity.Id = Guid.NewGuid();
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Added new {EntityType} with ID {Id}", typeof(T).Name, entity.Id);
    }

    public async Task UpdateAsync(T entity)
    {
        try
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated {EntityType} with ID {Id}", typeof(T).Name, entity.Id);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict updating {EntityType} with ID {Id}", typeof(T).Name, entity.Id);

            // Last-write-wins: reload current values from database
            var entry = ex.Entries.Single();
            var databaseValues = await entry.GetDatabaseValuesAsync();

            if (databaseValues == null)
            {
                _logger.LogError("Entity {EntityType} with ID {Id} was deleted", typeof(T).Name, entity.Id);
                throw new InvalidOperationException("The entity was deleted by another user.");
            }

            // Set database values as current values
            entry.OriginalValues.SetValues(databaseValues);

            throw; // Re-throw to return HTTP 409 Conflict
        }
    }

    public async Task ArchiveAsync(Guid id)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity != null)
        {
            entity.IsArchived = true;
            _logger.LogInformation("Archived {EntityType} with ID {Id}", typeof(T).Name, id);
        }
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
