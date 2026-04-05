using LocalFinanceManager.Data;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.Models;
using LocalFinanceManager.Services;
using LocalFinanceManager.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LocalFinanceManager.Tests.Unit;

[TestFixture]
public class BackupServiceTests
{
    private static readonly Guid TestUserId = TestUserContext.DefaultUserId;
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private TestDbContextFactory _factory = null!;
    private AppDbContext _context = null!;
    private BackupService _service = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new TestDbContextFactory();
        _context = _factory.CreateContext();

        // Seed test user
        if (!_context.Users.Any(u => u.Id == TestUserId))
        {
            _context.Users.Add(new User
            {
                Id = TestUserId,
                SupabaseUserId = TestUserId.ToString(),
                Email = "test@localfinancemanager.local",
                DisplayName = "Test User",
                EmailConfirmed = true,
                IsArchived = false
            });
            _context.SaveChanges();
        }

        var userContext = new TestUserContext(TestUserId);
        var logger = new Mock<ILogger<BackupService>>().Object;
        _service = new BackupService(_context, userContext, logger);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
        _factory.Dispose();
    }

    // ---- Helpers ----

    private Account AddAccount(Guid? userId = null, bool archived = false, string iban = "NL91ABNA0417164300")
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Label = "Test Account",
            Type = AccountType.Checking,
            Currency = "EUR",
            IBAN = iban,
            StartingBalance = 100m,
            UserId = userId ?? TestUserId,
            IsArchived = archived,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.Accounts.Add(account);
        _context.SaveChanges();
        return account;
    }

    private Transaction AddTransaction(Guid accountId, Guid? userId = null, bool archived = false)
    {
        var t = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = -50m,
            Date = DateTime.UtcNow.Date,
            Description = "Test",
            AccountId = accountId,
            UserId = userId ?? TestUserId,
            IsArchived = archived,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.Transactions.Add(t);
        _context.SaveChanges();
        return t;
    }

    private BudgetPlan AddBudgetPlan(Guid accountId, Guid? userId = null, bool archived = false)
    {
        var bp = new BudgetPlan
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Year = 2026,
            Name = "Test Plan",
            UserId = userId ?? TestUserId,
            IsArchived = archived,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.BudgetPlans.Add(bp);
        _context.SaveChanges();
        return bp;
    }

    private Category AddCategory(Guid budgetPlanId, Guid? userId = null, bool archived = false)
    {
        var cat = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Groceries",
            Type = CategoryType.Expense,
            BudgetPlanId = budgetPlanId,
            UserId = userId ?? TestUserId,
            IsArchived = archived,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.Categories.Add(cat);
        _context.SaveChanges();
        return cat;
    }

    private BudgetLine AddBudgetLine(Guid budgetPlanId, Guid categoryId, bool archived = false)
    {
        var bl = new BudgetLine
        {
            Id = Guid.NewGuid(),
            BudgetPlanId = budgetPlanId,
            CategoryId = categoryId,
            MonthlyAmountsJson = "[100,100,100,100,100,100,100,100,100,100,100,100]",
            IsArchived = archived,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.BudgetLines.Add(bl);
        _context.SaveChanges();
        return bl;
    }

    // ---- CreateBackupAsync ----

    [Test]
    public async Task CreateBackupAsync_ExcludesArchivedEntities()
    {
        var account = AddAccount(archived: false);
        AddAccount(archived: true); // archived — should not appear
        AddTransaction(account.Id, archived: false);
        AddTransaction(account.Id, archived: true); // archived

        var backup = await _service.CreateBackupAsync(TestUserId);

        Assert.That(backup.Accounts, Has.Count.EqualTo(1));
        Assert.That(backup.Transactions, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task CreateBackupAsync_OnlyReturnsTenantData()
    {
        // Ensure other user exists for FK validity
        if (!_context.Users.Any(u => u.Id == OtherUserId))
        {
            _context.Users.Add(new User
            {
                Id = OtherUserId,
                SupabaseUserId = OtherUserId.ToString(),
                Email = "other@test.local",
                DisplayName = "Other",
                EmailConfirmed = true,
                IsArchived = false
            });
            _context.SaveChanges();
        }

        AddAccount(userId: TestUserId, iban: "NL91ABNA0417164300");
        AddAccount(userId: OtherUserId, iban: "NL20INGB0001234567"); // other user

        var backup = await _service.CreateBackupAsync(TestUserId);

        Assert.That(backup.Accounts.All(a => a.IBAN == "NL91ABNA0417164300"), Is.True);
        Assert.That(backup.Accounts, Has.Count.EqualTo(1));
    }

    // ---- ValidateBackupAsync ----

    [Test]
    public async Task ValidateBackupAsync_RejectsIncompatibleVersion()
    {
        var backup = new BackupData { Version = "2.0" };

        var result = await _service.ValidateBackupAsync(backup, TestUserId);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Count.GreaterThan(0));
        Assert.That(result.Errors[0], Does.Contain("version"));
    }

    [Test]
    public async Task ValidateBackupAsync_DetectsIbanConflict()
    {
        // Existing local account with IBAN
        AddAccount(iban: "NL91ABNA0417164300");

        // Backup contains a DIFFERENT account ID claiming the same IBAN
        var backup = new BackupData
        {
            Version = "1.0",
            Accounts =
            [
                new BackupAccountDto
                {
                    Id = Guid.NewGuid(), // different id
                    Label = "Other",
                    AccountType = "Checking",
                    Currency = "EUR",
                    IBAN = "NL91ABNA0417164300", // same IBAN
                    StartingBalance = 0
                }
            ]
        };

        var result = await _service.ValidateBackupAsync(backup, TestUserId);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.Contains("IBAN")), Is.True);
    }

    [Test]
    public async Task ValidateBackupAsync_DetectsBrokenInternalReference()
    {
        var orphanBudgetPlanId = Guid.NewGuid();
        var backup = new BackupData
        {
            Version = "1.0",
            BudgetPlans =
            [
                new BackupBudgetPlanDto
                {
                    Id = orphanBudgetPlanId,
                    AccountId = Guid.NewGuid(), // not in backup.Accounts
                    Year = 2026,
                    Name = "Orphan Plan"
                }
            ]
        };

        var result = await _service.ValidateBackupAsync(backup, TestUserId);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.Contains("BudgetPlan")), Is.True);
    }

    // ---- RestoreBackupAsync — Merge ----

    [Test]
    public async Task RestoreBackupAsync_Merge_InsertsNewEntities()
    {
        var accountId = Guid.NewGuid();
        var backup = new BackupData
        {
            Version = "1.0",
            Accounts =
            [
                new BackupAccountDto
                {
                    Id = accountId,
                    Label = "Imported Account",
                    AccountType = "Checking",
                    Currency = "EUR",
                    IBAN = "NL91ABNA0417164300",
                    StartingBalance = 0,
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                }
            ]
        };

        var result = await _service.RestoreBackupAsync(TestUserId, backup, ConflictResolutionStrategy.Merge);

        Assert.That(result.Success, Is.True);
        Assert.That(result.AccountsImported, Is.EqualTo(1));
        Assert.That(await _context.Accounts.AnyAsync(a => a.Id == accountId), Is.True);
    }

    [Test]
    public async Task RestoreBackupAsync_Merge_UpdatesWhenBackupNewer()
    {
        var existing = AddAccount(iban: "NL91ABNA0417164300");
        var newerUpdatedAt = existing.UpdatedAt.AddDays(1);

        var backup = new BackupData
        {
            Version = "1.0",
            Accounts =
            [
                new BackupAccountDto
                {
                    Id = existing.Id,
                    Label = "Updated Label",
                    AccountType = "Savings",
                    Currency = "EUR",
                    IBAN = "NL91ABNA0417164300",
                    StartingBalance = 999m,
                    CreatedAt = existing.CreatedAt,
                    UpdatedAt = newerUpdatedAt
                }
            ]
        };

        var result = await _service.RestoreBackupAsync(TestUserId, backup, ConflictResolutionStrategy.Merge);

        Assert.That(result.Success, Is.True);
        Assert.That(result.AccountsUpdated, Is.EqualTo(1));

        _context.ChangeTracker.Clear();
        var updated = await _context.Accounts.FindAsync(existing.Id);
        Assert.That(updated!.Label, Is.EqualTo("Updated Label"));
    }

    [Test]
    public async Task RestoreBackupAsync_Merge_SkipsWhenLocalNewer()
    {
        var existing = AddAccount(iban: "NL91ABNA0417164300");
        var olderUpdatedAt = existing.UpdatedAt.AddDays(-2);

        var backup = new BackupData
        {
            Version = "1.0",
            Accounts =
            [
                new BackupAccountDto
                {
                    Id = existing.Id,
                    Label = "Should Not Apply",
                    AccountType = "Checking",
                    Currency = "EUR",
                    IBAN = "NL91ABNA0417164300",
                    StartingBalance = 0,
                    CreatedAt = existing.CreatedAt,
                    UpdatedAt = olderUpdatedAt
                }
            ]
        };

        var result = await _service.RestoreBackupAsync(TestUserId, backup, ConflictResolutionStrategy.Merge);

        Assert.That(result.Success, Is.True);
        Assert.That(result.AccountsSkipped, Is.EqualTo(1));

        _context.ChangeTracker.Clear();
        var unchanged = await _context.Accounts.FindAsync(existing.Id);
        Assert.That(unchanged!.Label, Is.Not.EqualTo("Should Not Apply"));
    }

    [Test]
    public async Task RestoreBackupAsync_Merge_AbortsOnIbanConflict()
    {
        AddAccount(iban: "NL91ABNA0417164300");

        var backup = new BackupData
        {
            Version = "1.0",
            Accounts =
            [
                new BackupAccountDto
                {
                    Id = Guid.NewGuid(), // different id
                    Label = "Conflict Account",
                    AccountType = "Checking",
                    Currency = "EUR",
                    IBAN = "NL91ABNA0417164300",
                    StartingBalance = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            ]
        };

        var result = await _service.RestoreBackupAsync(TestUserId, backup, ConflictResolutionStrategy.Merge);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Errors.Any(e => e.Contains("IBAN")), Is.True);
    }

    // ---- RestoreBackupAsync — Skip ----

    [Test]
    public async Task RestoreBackupAsync_Skip_DoesNotOverwriteExisting()
    {
        var existing = AddAccount(iban: "NL91ABNA0417164300");

        var backup = new BackupData
        {
            Version = "1.0",
            Accounts =
            [
                new BackupAccountDto
                {
                    Id = existing.Id,
                    Label = "Should Be Skipped",
                    AccountType = "Savings",
                    Currency = "USD",
                    IBAN = "NL91ABNA0417164300",
                    StartingBalance = 9999m,
                    CreatedAt = existing.CreatedAt,
                    UpdatedAt = DateTime.UtcNow.AddDays(1) // even if newer, skip ignores it
                }
            ]
        };

        var result = await _service.RestoreBackupAsync(TestUserId, backup, ConflictResolutionStrategy.Skip);

        Assert.That(result.Success, Is.True);
        Assert.That(result.AccountsSkipped, Is.EqualTo(1));

        _context.ChangeTracker.Clear();
        var unchanged = await _context.Accounts.FindAsync(existing.Id);
        Assert.That(unchanged!.Label, Is.Not.EqualTo("Should Be Skipped"));
    }

    // ---- RestoreBackupAsync — Overwrite ----

    [Test]
    public async Task RestoreBackupAsync_Overwrite_ReplacesAllData()
    {
        // Existing data
        AddAccount(iban: "NL91ABNA0417164300");
        var accountCountBefore = await _context.Accounts
            .CountAsync(a => a.UserId == TestUserId && !a.IsArchived);
        Assert.That(accountCountBefore, Is.EqualTo(1));

        var newAccountId = Guid.NewGuid();
        var backup = new BackupData
        {
            Version = "1.0",
            Accounts =
            [
                new BackupAccountDto
                {
                    Id = newAccountId,
                    Label = "Replacement Account",
                    AccountType = "Savings",
                    Currency = "EUR",
                    IBAN = "NL20INGB0001234567",
                    StartingBalance = 500m,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            ]
        };

        var result = await _service.RestoreBackupAsync(TestUserId, backup, ConflictResolutionStrategy.Overwrite);

        Assert.That(result.Success, Is.True);
        Assert.That(result.AccountsImported, Is.EqualTo(1));

        _context.ChangeTracker.Clear();
        var accounts = await _context.Accounts
            .Where(a => a.UserId == TestUserId)
            .ToListAsync();

        Assert.That(accounts, Has.Count.EqualTo(1));
        Assert.That(accounts[0].Id, Is.EqualTo(newAccountId));
    }
}
