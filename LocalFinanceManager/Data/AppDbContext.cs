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
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<BudgetPlan> BudgetPlans => Set<BudgetPlan>();
    public DbSet<BudgetLine> BudgetLines => Set<BudgetLine>();

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

        // Configure Category entity
        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(c => c.Name);
        });

        // Configure BudgetPlan entity
        modelBuilder.Entity<BudgetPlan>(entity =>
        {
            entity.Property(bp => bp.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(bp => bp.Year)
                .IsRequired();

            entity.HasOne(bp => bp.Account)
                .WithMany()
                .HasForeignKey(bp => bp.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(bp => new { bp.AccountId, bp.Year });
        });

        // Configure BudgetLine entity
        modelBuilder.Entity<BudgetLine>(entity =>
        {
            entity.Property(bl => bl.MonthlyAmountsJson)
                .IsRequired()
                .HasColumnType("TEXT");

            entity.Property(bl => bl.Notes)
                .HasMaxLength(500);

            entity.HasOne(bl => bl.BudgetPlan)
                .WithMany(bp => bp.BudgetLines)
                .HasForeignKey(bl => bl.BudgetPlanId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(bl => bl.Category)
                .WithMany()
                .HasForeignKey(bl => bl.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(bl => bl.BudgetPlanId);
            entity.HasIndex(bl => bl.CategoryId);
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

        // Seed categories if none exist
        if (!await Categories.AnyAsync())
        {
            var categories = new[]
            {
                new Category { Id = Guid.NewGuid(), Name = "Huur", IsArchived = false },
                new Category { Id = Guid.NewGuid(), Name = "Boodschappen", IsArchived = false },
                new Category { Id = Guid.NewGuid(), Name = "Transport", IsArchived = false },
                new Category { Id = Guid.NewGuid(), Name = "Utilities", IsArchived = false },
                new Category { Id = Guid.NewGuid(), Name = "Entertainment", IsArchived = false },
                new Category { Id = Guid.NewGuid(), Name = "Healthcare", IsArchived = false },
                new Category { Id = Guid.NewGuid(), Name = "Savings", IsArchived = false },
                new Category { Id = Guid.NewGuid(), Name = "Other", IsArchived = false }
            };

            await Categories.AddRangeAsync(categories);
            await SaveChangesAsync();
        }

        // Seed budget plans if none exist
        if (!await BudgetPlans.AnyAsync())
        {
            var firstAccount = await Accounts.FirstOrDefaultAsync();
            var categoriesList = await Categories.ToListAsync();

            if (firstAccount != null && categoriesList.Any())
            {
                var budgetPlan = new BudgetPlan
                {
                    Id = Guid.NewGuid(),
                    AccountId = firstAccount.Id,
                    Year = DateTime.Now.Year,
                    Name = $"{DateTime.Now.Year} Household Budget",
                    IsArchived = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await BudgetPlans.AddAsync(budgetPlan);
                await SaveChangesAsync();

                // Add budget lines for some categories
                var budgetLines = new List<BudgetLine>();

                var huurCategory = categoriesList.FirstOrDefault(c => c.Name == "Huur");
                if (huurCategory != null)
                {
                    budgetLines.Add(new BudgetLine
                    {
                        Id = Guid.NewGuid(),
                        BudgetPlanId = budgetPlan.Id,
                        CategoryId = huurCategory.Id,
                        MonthlyAmounts = Enumerable.Repeat(850m, 12).ToArray(),
                        Notes = "Monthly rent",
                        IsArchived = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                var boodschappenCategory = categoriesList.FirstOrDefault(c => c.Name == "Boodschappen");
                if (boodschappenCategory != null)
                {
                    budgetLines.Add(new BudgetLine
                    {
                        Id = Guid.NewGuid(),
                        BudgetPlanId = budgetPlan.Id,
                        CategoryId = boodschappenCategory.Id,
                        MonthlyAmounts = Enumerable.Repeat(400m, 12).ToArray(),
                        Notes = "Groceries and household items",
                        IsArchived = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                var transportCategory = categoriesList.FirstOrDefault(c => c.Name == "Transport");
                if (transportCategory != null)
                {
                    budgetLines.Add(new BudgetLine
                    {
                        Id = Guid.NewGuid(),
                        BudgetPlanId = budgetPlan.Id,
                        CategoryId = transportCategory.Id,
                        MonthlyAmounts = Enumerable.Repeat(150m, 12).ToArray(),
                        Notes = "Public transport and fuel",
                        IsArchived = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                var utilitiesCategory = categoriesList.FirstOrDefault(c => c.Name == "Utilities");
                if (utilitiesCategory != null)
                {
                    budgetLines.Add(new BudgetLine
                    {
                        Id = Guid.NewGuid(),
                        BudgetPlanId = budgetPlan.Id,
                        CategoryId = utilitiesCategory.Id,
                        MonthlyAmounts = new decimal[] { 120m, 110m, 100m, 90m, 80m, 70m, 70m, 80m, 90m, 100m, 110m, 120m },
                        Notes = "Gas, electricity, water (seasonal variation)",
                        IsArchived = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                var entertainmentCategory = categoriesList.FirstOrDefault(c => c.Name == "Entertainment");
                if (entertainmentCategory != null)
                {
                    budgetLines.Add(new BudgetLine
                    {
                        Id = Guid.NewGuid(),
                        BudgetPlanId = budgetPlan.Id,
                        CategoryId = entertainmentCategory.Id,
                        MonthlyAmounts = Enumerable.Repeat(200m, 12).ToArray(),
                        Notes = "Movies, dining out, hobbies",
                        IsArchived = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                if (budgetLines.Any())
                {
                    await BudgetLines.AddRangeAsync(budgetLines);
                    await SaveChangesAsync();
                }
            }
        }
    }
}

