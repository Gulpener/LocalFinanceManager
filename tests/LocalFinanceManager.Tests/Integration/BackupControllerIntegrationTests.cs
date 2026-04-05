using LocalFinanceManager.Data;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.Models;
using LocalFinanceManager.Services;
using LocalFinanceManager.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LocalFinanceManager.Tests.Integration;

/// <summary>
/// Integration tests for BackupController / BackupService against in-memory SQLite.
/// Tests verify full service behaviour including database interactions.
/// </summary>
[TestFixture]
public class BackupControllerIntegrationTests
{
    private static readonly Guid UserId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private AppDbContext _context = null!;
    private BackupService _service = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        // Seed test user
        _context.Users.Add(new User
        {
            Id = UserId,
            SupabaseUserId = UserId.ToString(),
            Email = "backup-test@localfinancemanager.local",
            DisplayName = "Backup Test User",
            EmailConfirmed = true,
            IsArchived = false
        });
        _context.SaveChanges();

        var userContext = new TestUserContext(UserId);
        var logger = new Mock<ILogger<BackupService>>().Object;
        _service = new BackupService(_context, userContext, logger);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    private Account SeedAccount(string iban = "NL91ABNA0417164300")
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Label = "Test Account",
            Type = AccountType.Checking,
            Currency = "EUR",
            IBAN = iban,
            StartingBalance = 1000m,
            UserId = UserId,
            IsArchived = false,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.Accounts.Add(account);
        _context.SaveChanges();
        return account;
    }

    private BudgetPlan SeedBudgetPlan(Guid accountId)
    {
        var bp = new BudgetPlan
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Year = 2026,
            Name = "2026 Budget",
            UserId = UserId,
            IsArchived = false,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.BudgetPlans.Add(bp);
        _context.SaveChanges();
        return bp;
    }

    private Category SeedCategory(Guid budgetPlanId)
    {
        var cat = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Groceries",
            Type = CategoryType.Expense,
            BudgetPlanId = budgetPlanId,
            UserId = UserId,
            IsArchived = false,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.Categories.Add(cat);
        _context.SaveChanges();
        return cat;
    }

    private BudgetLine SeedBudgetLine(Guid budgetPlanId, Guid categoryId)
    {
        var bl = new BudgetLine
        {
            Id = Guid.NewGuid(),
            BudgetPlanId = budgetPlanId,
            CategoryId = categoryId,
            MonthlyAmountsJson = "[200,200,200,200,200,200,200,200,200,200,200,200]",
            IsArchived = false,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.BudgetLines.Add(bl);
        _context.SaveChanges();
        return bl;
    }

    private Transaction SeedTransaction(Guid accountId)
    {
        var tx = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = -45.50m,
            Date = DateTime.UtcNow.Date,
            Description = "Albert Heijn",
            Counterparty = "AH",
            AccountId = accountId,
            UserId = UserId,
            IsArchived = false,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.Transactions.Add(tx);
        _context.SaveChanges();
        return tx;
    }

    // ---- Export ----

    [Test]
    public async Task Export_ReturnsValidJsonWithAllEntityTypes()
    {
        var account = SeedAccount();
        var budgetPlan = SeedBudgetPlan(account.Id);
        var category = SeedCategory(budgetPlan.Id);
        SeedBudgetLine(budgetPlan.Id, category.Id);
        SeedTransaction(account.Id);

        var backup = await _service.CreateBackupAsync(UserId);

        Assert.That(backup.Version, Is.EqualTo("1.0"));
        Assert.That(backup.Accounts, Has.Count.EqualTo(1));
        Assert.That(backup.BudgetPlans, Has.Count.EqualTo(1));
        Assert.That(backup.Categories, Has.Count.EqualTo(1));
        Assert.That(backup.BudgetLines, Has.Count.EqualTo(1));
        Assert.That(backup.Transactions, Has.Count.EqualTo(1));
    }

    // ---- Validate ----

    [Test]
    public async Task Validate_ReturnsIsValid_ForConsistentBackup()
    {
        var accountId = Guid.NewGuid();
        var budgetPlanId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var budgetLineId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();

        var backup = new BackupData
        {
            Version = "1.0",
            Accounts = [new BackupAccountDto { Id = accountId, Label = "A", AccountType = "Checking", Currency = "EUR", IBAN = "NL20INGB0001234567" }],
            BudgetPlans = [new BackupBudgetPlanDto { Id = budgetPlanId, AccountId = accountId, Year = 2026, Name = "Plan" }],
            Categories = [new BackupCategoryDto { Id = categoryId, Name = "Food", CategoryType = "Expense", BudgetPlanId = budgetPlanId }],
            BudgetLines = [new BackupBudgetLineDto { Id = budgetLineId, BudgetPlanId = budgetPlanId, CategoryId = categoryId, MonthlyAmountsJson = "[]" }],
            Transactions = [new BackupTransactionDto { Id = transactionId, Amount = -10m, Date = DateTime.UtcNow, Description = "Test", AccountId = accountId }],
            TransactionSplits = [new BackupTransactionSplitDto { Id = Guid.NewGuid(), TransactionId = transactionId, BudgetLineId = budgetLineId, Amount = -10m }]
        };

        var result = await _service.ValidateBackupAsync(backup, UserId);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public async Task Validate_ReturnsErrors_ForIbanConflict()
    {
        SeedAccount(iban: "NL91ABNA0417164300");

        var backup = new BackupData
        {
            Version = "1.0",
            Accounts =
            [
                new BackupAccountDto
                {
                    Id = Guid.NewGuid(),
                    Label = "Conflict",
                    AccountType = "Checking",
                    Currency = "EUR",
                    IBAN = "NL91ABNA0417164300" // same IBAN, different Id
                }
            ]
        };

        var result = await _service.ValidateBackupAsync(backup, UserId);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.Contains("IBAN")), Is.True);
    }

    // ---- Restore ----

    [Test]
    public async Task Restore_Overwrite_ReplacesAllExistingData()
    {
        SeedAccount();
        var existingCount = await _context.Accounts.CountAsync(a => a.UserId == UserId);
        Assert.That(existingCount, Is.EqualTo(1));

        var newId = Guid.NewGuid();
        var backup = new BackupData
        {
            Version = "1.0",
            Accounts =
            [
                new BackupAccountDto
                {
                    Id = newId,
                    Label = "New Account",
                    AccountType = "Savings",
                    Currency = "EUR",
                    IBAN = "NL20INGB0001234567",
                    StartingBalance = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            ]
        };

        var result = await _service.RestoreBackupAsync(UserId, backup, ConflictResolutionStrategy.Overwrite);

        Assert.That(result.Success, Is.True);
        _context.ChangeTracker.Clear();
        var accounts = await _context.Accounts.Where(a => a.UserId == UserId).ToListAsync();
        Assert.That(accounts, Has.Count.EqualTo(1));
        Assert.That(accounts[0].Id, Is.EqualTo(newId));
    }

    [Test]
    public async Task Restore_IntoEmptyDatabase_ImportsAllEntities()
    {
        var accountId = Guid.NewGuid();
        var budgetPlanId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var budgetLineId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var splitId = Guid.NewGuid();

        var backup = new BackupData
        {
            Version = "1.0",
            Accounts = [new BackupAccountDto { Id = accountId, Label = "A", AccountType = "Checking", Currency = "EUR", IBAN = "NL91ABNA0417164300", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }],
            BudgetPlans = [new BackupBudgetPlanDto { Id = budgetPlanId, AccountId = accountId, Year = 2026, Name = "Plan", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }],
            Categories = [new BackupCategoryDto { Id = categoryId, Name = "Food", CategoryType = "Expense", BudgetPlanId = budgetPlanId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }],
            BudgetLines = [new BackupBudgetLineDto { Id = budgetLineId, BudgetPlanId = budgetPlanId, CategoryId = categoryId, MonthlyAmountsJson = "[]", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }],
            Transactions = [new BackupTransactionDto { Id = transactionId, Amount = -10m, Date = DateTime.UtcNow, Description = "Test", AccountId = accountId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }],
            TransactionSplits = [new BackupTransactionSplitDto { Id = splitId, TransactionId = transactionId, BudgetLineId = budgetLineId, Amount = -10m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }]
        };

        var result = await _service.RestoreBackupAsync(UserId, backup, ConflictResolutionStrategy.Merge);

        Assert.That(result.Success, Is.True);
        Assert.That(result.AccountsImported, Is.EqualTo(1));
        Assert.That(result.BudgetPlansImported, Is.EqualTo(1));
        Assert.That(result.CategoriesImported, Is.EqualTo(1));
        Assert.That(result.BudgetLinesImported, Is.EqualTo(1));
        Assert.That(result.TransactionsImported, Is.EqualTo(1));
        Assert.That(result.TransactionSplitsImported, Is.EqualTo(1));
    }

    [Test]
    public async Task Restore_Merge_ReturnsCorrectCounts()
    {
        var existingAccount = SeedAccount();
        var newAccountId = Guid.NewGuid();

        var backup = new BackupData
        {
            Version = "1.0",
            Accounts =
            [
                // Existing but older → skip
                new BackupAccountDto
                {
                    Id = existingAccount.Id,
                    Label = "Old Label",
                    AccountType = "Checking",
                    Currency = "EUR",
                    IBAN = "NL91ABNA0417164300",
                    StartingBalance = 0,
                    CreatedAt = existingAccount.CreatedAt,
                    UpdatedAt = existingAccount.UpdatedAt.AddDays(-1) // older → skip
                },
                // New → import
                new BackupAccountDto
                {
                    Id = newAccountId,
                    Label = "Brand New",
                    AccountType = "Savings",
                    Currency = "EUR",
                    IBAN = "NL20INGB0001234567",
                    StartingBalance = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            ]
        };

        var result = await _service.RestoreBackupAsync(UserId, backup, ConflictResolutionStrategy.Merge);

        Assert.That(result.Success, Is.True);
        Assert.That(result.AccountsImported, Is.EqualTo(1));
        Assert.That(result.AccountsSkipped, Is.EqualTo(1));
    }
}
