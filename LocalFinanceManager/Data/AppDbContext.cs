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
    public DbSet<LabeledExample> LabeledExamples => Set<LabeledExample>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<BudgetPlan> BudgetPlans => Set<BudgetPlan>();
    public DbSet<BudgetLine> BudgetLines => Set<BudgetLine>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TransactionSplit> TransactionSplits => Set<TransactionSplit>();
    public DbSet<TransactionAudit> TransactionAudits => Set<TransactionAudit>();

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

            entity.Property(c => c.Type)
                .HasConversion<int>() // Store enum as int
                .HasDefaultValue(CategoryType.Expense); // Expense = 0

            entity.HasOne(c => c.BudgetPlan)
                .WithMany(bp => bp.Categories)
                .HasForeignKey(c => c.BudgetPlanId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(c => new { c.BudgetPlanId, c.Name });
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
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(bl => bl.BudgetPlanId);
            entity.HasIndex(bl => bl.CategoryId);
        });

        // Configure Transaction entity
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.Property(t => t.Amount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(t => t.Date)
                .IsRequired();

            entity.Property(t => t.Description)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(t => t.Counterparty)
                .HasMaxLength(200);

            entity.Property(t => t.ExternalId)
                .HasMaxLength(100);

            entity.Property(t => t.OriginalImport)
                .HasColumnType("TEXT"); // nvarchar(max) equivalent for SQLite

            entity.Property(t => t.SourceFileName)
                .HasMaxLength(255);

            entity.HasOne(t => t.Account)
                .WithMany()
                .HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for common queries and deduplication
            entity.HasIndex(t => t.AccountId);
            entity.HasIndex(t => t.Date);
            entity.HasIndex(t => new { t.Date, t.Amount, t.ExternalId }); // Exact match deduplication
            entity.HasIndex(t => t.ImportBatchId);
        });

        // Configure TransactionSplit entity
        modelBuilder.Entity<TransactionSplit>(entity =>
        {
            entity.Property(ts => ts.Amount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(ts => ts.Note)
                .HasMaxLength(500);

            entity.HasOne(ts => ts.Transaction)
                .WithMany(t => t.AssignedParts)
                .HasForeignKey(ts => ts.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ts => ts.BudgetLine)
                .WithMany()
                .HasForeignKey(ts => ts.BudgetLineId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ts => ts.Category)
                .WithMany()
                .HasForeignKey(ts => ts.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for queries
            entity.HasIndex(ts => ts.TransactionId);
            entity.HasIndex(ts => ts.BudgetLineId);
            entity.HasIndex(ts => ts.CategoryId);
        });

        // Configure TransactionAudit entity
        modelBuilder.Entity<TransactionAudit>(entity =>
        {
            entity.Property(ta => ta.ActionType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(ta => ta.ChangedBy)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(ta => ta.ChangedAt)
                .IsRequired();

            entity.Property(ta => ta.BeforeState)
                .HasColumnType("TEXT");

            entity.Property(ta => ta.AfterState)
                .HasColumnType("TEXT");

            entity.Property(ta => ta.Reason)
                .HasMaxLength(500);

            entity.HasOne(ta => ta.Transaction)
                .WithMany()
                .HasForeignKey(ta => ta.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for audit queries
            entity.HasIndex(ta => ta.TransactionId);
            entity.HasIndex(ta => ta.ChangedAt);
            entity.HasIndex(ta => ta.ActionType);
        });

        // Configure LabeledExample entity
        modelBuilder.Entity<LabeledExample>(entity =>
        {
            entity.Property(le => le.UserId)
                .HasMaxLength(100);

            entity.HasOne(le => le.Transaction)
                .WithMany()
                .HasForeignKey(le => le.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(le => le.Category)
                .WithMany()
                .HasForeignKey(le => le.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for ML queries (rolling window, category filtering)
            entity.HasIndex(le => le.TransactionId);
            entity.HasIndex(le => le.CategoryId);
            entity.HasIndex(le => le.CreatedAt); // For rolling window queries
            entity.HasIndex(le => new { le.CategoryId, le.CreatedAt }); // Combined for efficient filtering
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
                // Income categories
                new Category { Id = Guid.NewGuid(), Name = "Salaris", Type = CategoryType.Income, IsArchived = false },
                new Category { Id = Guid.NewGuid(), Name = "Freelance", Type = CategoryType.Income, IsArchived = false },
                new Category { Id = Guid.NewGuid(), Name = "Dividend", Type = CategoryType.Income, IsArchived = false },
                // Expense categories
                new Category { Id = Guid.NewGuid(), Name = "Huur", Type = CategoryType.Expense, IsArchived = false },
                new Category { Id = Guid.NewGuid(), Name = "Boodschappen", Type = CategoryType.Expense, IsArchived = false },
                new Category { Id = Guid.NewGuid(), Name = "Transport", Type = CategoryType.Expense, IsArchived = false },
                new Category { Id = Guid.NewGuid(), Name = "Utilities", Type = CategoryType.Expense, IsArchived = false },
                new Category { Id = Guid.NewGuid(), Name = "Entertainment", Type = CategoryType.Expense, IsArchived = false },
                new Category { Id = Guid.NewGuid(), Name = "Healthcare", Type = CategoryType.Expense, IsArchived = false },
                new Category { Id = Guid.NewGuid(), Name = "Savings", Type = CategoryType.Expense, IsArchived = false },
                new Category { Id = Guid.NewGuid(), Name = "Other", Type = CategoryType.Expense, IsArchived = false }
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

        // Seed transactions if none exist
        if (!await Transactions.AnyAsync())
        {
            var firstAccount = await Accounts.FirstOrDefaultAsync();
            if (firstAccount != null)
            {
                var transactions = new[]
                {
                    new Transaction
                    {
                        Id = Guid.NewGuid(),
                        AccountId = firstAccount.Id,
                        Amount = -45.50m,
                        Date = DateTime.UtcNow.AddDays(-5),
                        Description = "Grocery Store Purchase",
                        Counterparty = "Albert Heijn",
                        ExternalId = "TRX001",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsArchived = false
                    },
                    new Transaction
                    {
                        Id = Guid.NewGuid(),
                        AccountId = firstAccount.Id,
                        Amount = -12.30m,
                        Date = DateTime.UtcNow.AddDays(-4),
                        Description = "Coffee Shop",
                        Counterparty = "Starbucks",
                        ExternalId = "TRX002",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsArchived = false
                    },
                    new Transaction
                    {
                        Id = Guid.NewGuid(),
                        AccountId = firstAccount.Id,
                        Amount = 2500.00m,
                        Date = DateTime.UtcNow.AddDays(-3),
                        Description = "Salary Payment",
                        Counterparty = "Employer Corp",
                        ExternalId = "TRX003",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsArchived = false
                    },
                    new Transaction
                    {
                        Id = Guid.NewGuid(),
                        AccountId = firstAccount.Id,
                        Amount = -850.00m,
                        Date = DateTime.UtcNow.AddDays(-2),
                        Description = "Rent Payment",
                        Counterparty = "Landlord",
                        ExternalId = "TRX004",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsArchived = false
                    }
                };

                await Transactions.AddRangeAsync(transactions);
                await SaveChangesAsync();
            }
        }
    }
}

