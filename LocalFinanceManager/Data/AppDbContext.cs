using Microsoft.EntityFrameworkCore;
using LocalFinanceManager.Models;

namespace LocalFinanceManager.Data;

/// <summary>
/// Application database context for LocalFinanceManager.
/// Manages all entity sets and database configuration.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Fixed seed user ID used in Development seeding. Also used by E2E tests via TestUserContext.
    /// </summary>
    public static readonly Guid SeedUserId = new Guid("00000000-0000-0000-0000-000000000002");

    /// <summary>
    /// Email address for the seed user created in Development. Used by E2E test auth claims.
    /// </summary>
    public const string SeedUserEmail = "dev@localfinancemanager.local";

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Entity Sets
    public DbSet<User> Users => Set<User>();
    public DbSet<MLModel> MLModels => Set<MLModel>();
    public DbSet<LabeledExample> LabeledExamples => Set<LabeledExample>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<BudgetPlan> BudgetPlans => Set<BudgetPlan>();
    public DbSet<BudgetLine> BudgetLines => Set<BudgetLine>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TransactionSplit> TransactionSplits => Set<TransactionSplit>();
    public DbSet<TransactionAudit> TransactionAudits => Set<TransactionAudit>();
    public DbSet<AppSettings> AppSettings => Set<AppSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure all entities that inherit from BaseEntity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                // Configure RowVersion for optimistic concurrency.
                // IsRequired(false) allows the column to be null (EF tracks version via token).
                modelBuilder.Entity(entityType.ClrType)
                    .Property("RowVersion")
                    .IsRowVersion()
                    .IsRequired(false);

                // Configure timestamps with default values (PostgreSQL-compatible)
                modelBuilder.Entity(entityType.ClrType)
                    .Property("CreatedAt")
                    .HasDefaultValueSql("now()");

                modelBuilder.Entity(entityType.ClrType)
                    .Property("UpdatedAt")
                    .HasDefaultValueSql("now()");

                // Add index for soft-delete filtering
                modelBuilder.Entity(entityType.ClrType)
                    .HasIndex("IsArchived");
            }
        }

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.SupabaseUserId).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SupabaseUserId).IsRequired().HasMaxLength(36);

            // UserId on User itself is not meaningful (self-reference). Set default null.
            entity.Property(e => e.UserId).HasDefaultValue(null);
            entity.Ignore(e => e.User); // User does not own itself
        });

        // Configure optional FK relationships for user-owned entities
        // Optional (nullable FK) allows system-level entities and test entities without a user
        modelBuilder.Entity<Account>()
            .HasOne(a => a.User)
            .WithMany(u => u.Accounts)
            .HasForeignKey(a => a.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BudgetPlan>()
            .HasOne(bp => bp.User)
            .WithMany(u => u.BudgetPlans)
            .HasForeignKey(bp => bp.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Category>()
            .HasOne(c => c.User)
            .WithMany(u => u.Categories)
            .HasForeignKey(c => c.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.User)
            .WithMany(u => u.Transactions)
            .HasForeignKey(t => t.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        // Entities without direct User ownership: ignore the User navigation property
        modelBuilder.Entity<BudgetLine>().Ignore(e => e.User);
        modelBuilder.Entity<TransactionSplit>().Ignore(e => e.User);
        modelBuilder.Entity<TransactionAudit>().Ignore(e => e.User);
        modelBuilder.Entity<MLModel>().Ignore(e => e.User);
        modelBuilder.Entity<LabeledExample>().Ignore(e => e.User);
        modelBuilder.Entity<AppSettings>().Ignore(e => e.User);

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

            // Configure relationship to current budget plan (optional)
            entity.HasOne(a => a.CurrentBudgetPlan)
                .WithOne()
                .HasForeignKey<Account>(a => a.CurrentBudgetPlanId)
                .OnDelete(DeleteBehavior.SetNull);

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
                .HasColumnType("jsonb");

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
                .HasColumnType("jsonb");

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
                .IsRequired() // BudgetLineId is required
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for queries
            entity.HasIndex(ts => ts.TransactionId);
            entity.HasIndex(ts => ts.BudgetLineId);
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
                .HasColumnType("jsonb");

            entity.Property(ta => ta.AfterState)
                .HasColumnType("jsonb");

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

        // Configure AppSettings entity (per-user settings)
        modelBuilder.Entity<AppSettings>(entity =>
        {
            entity.HasIndex(s => s.UserId).IsUnique();

            entity.Property(s => s.MinimumConfidence)
                .IsRequired();

            entity.Property(s => s.IntervalMinutes)
                .IsRequired();

            entity.Property(s => s.UpdatedBy)
                .HasMaxLength(100);

            entity.Property(s => s.AccountIdsJson)
                .HasColumnType("jsonb");

            entity.Property(s => s.ExcludedCategoryIdsJson)
                .HasColumnType("jsonb");
        });

        // Configure LabeledExample entity
        modelBuilder.Entity<LabeledExample>(entity =>
        {
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

}

