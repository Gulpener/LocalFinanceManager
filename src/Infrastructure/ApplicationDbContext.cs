using System.Text.Json;
using LocalFinanceManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LocalFinanceManager.Infrastructure;

/// <summary>
/// Entity Framework Core database context for LocalFinanceManager.
/// </summary>
public class ApplicationDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
    /// </summary>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the accounts.
    /// </summary>
    public DbSet<Account> Accounts => Set<Account>();

    /// <summary>
    /// Gets or sets the transactions.
    /// </summary>
    public DbSet<Transaction> Transactions => Set<Transaction>();

    /// <summary>
    /// Gets or sets the categories.
    /// </summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>
    /// Gets or sets the envelopes.
    /// </summary>
    public DbSet<Envelope> Envelopes => Set<Envelope>();

    /// <summary>
    /// Gets or sets the rules.
    /// </summary>
    public DbSet<Rule> Rules => Set<Rule>();

    /// <summary>
    /// Gets or sets the category learning profiles.
    /// </summary>
    public DbSet<CategoryLearningProfile> CategoryLearningProfiles => Set<CategoryLearningProfile>();

    /// <summary>
    /// Gets or sets the budgets.
    /// </summary>
    public DbSet<Budget> Budgets => Set<Budget>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Account configuration
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.AccountType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.InitialBalance).HasPrecision(18, 2);
        });

        // Transaction configuration
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CounterAccount).HasMaxLength(50);

            // Store Tags as JSON
            entity.Property(e => e.Tags)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .HasColumnType("TEXT");

            // Configure comparer for List<string>
            entity.Property(e => e.Tags).Metadata.SetValueComparer(
                new ValueComparer<List<string>>(
                    (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));

            entity.HasOne(e => e.Account)
                .WithMany(a => a.Transactions)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Transactions)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Envelope)
                .WithMany(e => e.Transactions)
                .HasForeignKey(e => e.EnvelopeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Category configuration
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.MonthlyBudget).HasPrecision(18, 2);

            entity.HasOne(e => e.ParentCategory)
                .WithMany(e => e.ChildCategories)
                .HasForeignKey(e => e.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Envelope configuration
        modelBuilder.Entity<Envelope>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Balance).HasPrecision(18, 2);
            entity.Property(e => e.MonthlyAllocation).HasPrecision(18, 2);
        });

        // Rule configuration
        modelBuilder.Entity<Rule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MatchType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Pattern).IsRequired().HasMaxLength(500);

            // Store AddLabels as JSON
            entity.Property(e => e.AddLabels)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .HasColumnType("TEXT");

            // Configure comparer for List<string>
            entity.Property(e => e.AddLabels).Metadata.SetValueComparer(
                new ValueComparer<List<string>>(
                    (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));

            entity.HasOne(e => e.TargetCategory)
                .WithMany()
                .HasForeignKey(e => e.TargetCategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.TargetEnvelope)
                .WithMany()
                .HasForeignKey(e => e.TargetEnvelopeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // CategoryLearningProfile configuration
        modelBuilder.Entity<CategoryLearningProfile>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Store Dictionary<string, int> properties as JSON
            entity.Property(e => e.WordFrequency)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, int>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, int>())
                .HasColumnType("TEXT");

            entity.Property(e => e.WordFrequency).Metadata.SetValueComparer(
                new ValueComparer<Dictionary<string, int>>(
                    (c1, c2) => c1 != null && c2 != null && c1.Count == c2.Count && !c1.Except(c2).Any(),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value)),
                    c => c.ToDictionary(e => e.Key, e => e.Value)));

            entity.Property(e => e.IbanFrequency)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, int>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, int>())
                .HasColumnType("TEXT");

            entity.Property(e => e.IbanFrequency).Metadata.SetValueComparer(
                new ValueComparer<Dictionary<string, int>>(
                    (c1, c2) => c1 != null && c2 != null && c1.Count == c2.Count && !c1.Except(c2).Any(),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value)),
                    c => c.ToDictionary(e => e.Key, e => e.Value)));

            entity.Property(e => e.AmountBucketFrequency)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, int>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, int>())
                .HasColumnType("TEXT");

            entity.Property(e => e.AmountBucketFrequency).Metadata.SetValueComparer(
                new ValueComparer<Dictionary<string, int>>(
                    (c1, c2) => c1 != null && c2 != null && c1.Count == c2.Count && !c1.Except(c2).Any(),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value)),
                    c => c.ToDictionary(e => e.Key, e => e.Value)));

            entity.Property(e => e.RecurrenceFrequency)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, int>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, int>())
                .HasColumnType("TEXT");

            entity.Property(e => e.RecurrenceFrequency).Metadata.SetValueComparer(
                new ValueComparer<Dictionary<string, int>>(
                    (c1, c2) => c1 != null && c2 != null && c1.Count == c2.Count && !c1.Except(c2).Any(),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value)),
                    c => c.ToDictionary(e => e.Key, e => e.Value)));

            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Budget configuration
        modelBuilder.Entity<Budget>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PlannedAmount).HasPrecision(18, 2);
            entity.Property(e => e.ActualAmount).HasPrecision(18, 2);

            // Index for AccountId + Month uniqueness
            entity.HasIndex(e => new { e.AccountId, e.Month })
                .HasDatabaseName("IX_Budget_AccountId_Month");

            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Envelope)
                .WithMany()
                .HasForeignKey(e => e.EnvelopeId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Account)
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
