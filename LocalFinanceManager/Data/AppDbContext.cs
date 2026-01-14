using Microsoft.EntityFrameworkCore;
using LocalFinanceManager.Models;

namespace LocalFinanceManager.Data;

/// <summary>
/// Application database context for LocalFinanceManager.
/// Manages all entity sets and database configuration.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Entity Sets
    public DbSet<MLModel> MLModels => Set<MLModel>();
    public DbSet<Account> Accounts => Set<Account>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure all entities that inherit from BaseEntity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                // Configure RowVersion for optimistic concurrency (nullable for SQLite compatibility)
                modelBuilder.Entity(entityType.ClrType)
                    .Property("RowVersion")
                    .IsRowVersion()
                    .IsRequired(false); // SQLite doesn't auto-generate RowVersion like SQL Server

                // Configure timestamps with default values
                modelBuilder.Entity(entityType.ClrType)
                    .Property("CreatedAt")
                    .HasDefaultValueSql("datetime('now')");

                modelBuilder.Entity(entityType.ClrType)
                    .Property("UpdatedAt")
                    .HasDefaultValueSql("datetime('now')");

                // Add index for soft-delete filtering
                modelBuilder.Entity(entityType.ClrType)
                    .HasIndex("IsArchived");
            }
        }

        // Configure Account entity
        modelBuilder.Entity<Account>(entity =>
        {
            entity.Property(a => a.Label)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(a => a.IBAN)
                .IsRequired()
                .HasMaxLength(34);

            entity.Property(a => a.Currency)
                .IsRequired()
                .HasMaxLength(3);

            entity.Property(a => a.StartingBalance)
                .HasColumnType("decimal(18,2)");

            // Index for common queries
            entity.HasIndex(a => a.Label);
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Updates CreatedAt and UpdatedAt timestamps for tracked entities.
    /// </summary>
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }

    /// <summary>
    /// Seeds the database with initial data for development environments.
    /// Checks for existing data to prevent duplicate seeding.
    /// </summary>
    public async Task SeedAsync()
    {
        // Seed accounts if none exist
        if (!await Accounts.AnyAsync())
        {
            var accounts = new[]
            {
                new Account
                {
                    Id = Guid.NewGuid(),
                    Label = "Betaalrekening",
                    Type = AccountType.Checking,
                    IBAN = "NL91ABNA0417164300",
                    Currency = "EUR",
                    StartingBalance = 1000.00m,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsArchived = false
                },
                new Account
                {
                    Id = Guid.NewGuid(),
                    Label = "Spaarrekening",
                    Type = AccountType.Savings,
                    IBAN = "NL20INGB0001234567",
                    Currency = "EUR",
                    StartingBalance = 2500.00m,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsArchived = false
                },
                new Account
                {
                    Id = Guid.NewGuid(),
                    Label = "Credit Card",
                    Type = AccountType.Credit,
                    IBAN = "NL39RABO0300065264",
                    Currency = "EUR",
                    StartingBalance = 0.00m,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsArchived = false
                }
            };

            await Accounts.AddRangeAsync(accounts);
            await SaveChangesAsync();
        }
    }
}
