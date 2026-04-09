using LocalFinanceManager.Data;
using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.Models;
using LocalFinanceManager.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;

namespace LocalFinanceManager.Tests.Integration;

/// <summary>
/// Integration tests for <see cref="TransactionRepository.CountThisMonthAsync"/>.
/// Verifies month boundary logic, soft-delete filtering, and shared-account access.
/// </summary>
[TestFixture]
public class TransactionRepositoryCountTests
{
    private static readonly Guid TestUserId = TestUserContext.DefaultUserId;
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private TestDbContextFactory _factory = null!;
    private AppDbContext _context = null!;
    private TransactionRepository _repository = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new TestDbContextFactory();
        _context = _factory.CreateContext();

        _context.ChangeTracker.Tracked += (_, args) => TestEntityOwnershipHelper.Apply(args.Entry);
        _context.ChangeTracker.StateChanged += (_, args) => TestEntityOwnershipHelper.Apply(args.Entry);

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

        if (!_context.Users.Any(u => u.Id == OtherUserId))
        {
            _context.Users.Add(new User
            {
                Id = OtherUserId,
                SupabaseUserId = OtherUserId.ToString(),
                Email = "other@localfinancemanager.local",
                DisplayName = "Other User",
                EmailConfirmed = true,
                IsArchived = false
            });
            _context.SaveChanges();
        }

        _repository = new TransactionRepository(_context, NullLogger<TransactionRepository>.Instance, new TestUserContext(TestUserId));
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
        _factory?.Dispose();
    }

    private Account CreateAccount(Guid? ownerId = null)
    {
        return new Account
        {
            Id = Guid.NewGuid(),
            Label = "Test Account",
            Type = AccountType.Checking,
            IBAN = $"NL{Guid.NewGuid().ToString("N")[..16]}",
            Currency = "EUR",
            StartingBalance = 1000m,
            UserId = ownerId ?? TestUserId
        };
    }

    private Transaction CreateTransaction(Guid accountId, DateTime date, Guid? ownerId = null)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Date = date,
            Description = "Test",
            Amount = 10m,
            UserId = ownerId ?? TestUserId
        };
    }

    [Test]
    public async Task CountThisMonthAsync_ReturnsOnlyCurrentMonthTransactions()
    {
        // Arrange
        var account = CreateAccount();
        await _context.Accounts.AddAsync(account);

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = startOfMonth.AddMonths(1);

        var inMonth1 = CreateTransaction(account.Id, startOfMonth.AddHours(1));
        var inMonth2 = CreateTransaction(account.Id, endOfMonth.AddSeconds(-1));
        var beforeMonth = CreateTransaction(account.Id, startOfMonth.AddSeconds(-1));
        var afterMonth = CreateTransaction(account.Id, endOfMonth);

        await _context.Transactions.AddRangeAsync(inMonth1, inMonth2, beforeMonth, afterMonth);
        await _context.SaveChangesAsync();

        // Act
        var count = await _repository.CountThisMonthAsync();

        // Assert
        Assert.That(count, Is.EqualTo(2), "Only transactions within the current month boundary should be counted");
    }

    [Test]
    public async Task CountThisMonthAsync_ExcludesArchivedTransactions()
    {
        // Arrange
        var account = CreateAccount();
        await _context.Accounts.AddAsync(account);

        var now = DateTime.UtcNow;

        var active = CreateTransaction(account.Id, now);
        var archived = CreateTransaction(account.Id, now);
        archived.IsArchived = true;

        await _context.Transactions.AddRangeAsync(active, archived);
        await _context.SaveChangesAsync();

        // Act
        var count = await _repository.CountThisMonthAsync();

        // Assert
        Assert.That(count, Is.EqualTo(1), "Archived transactions must not be counted");
    }

    [Test]
    public async Task CountThisMonthAsync_ExcludesOtherUsersTransactions()
    {
        // Arrange — two accounts, each owned by a different user
        var ownAccount = CreateAccount(TestUserId);
        var otherAccount = CreateAccount(OtherUserId);
        await _context.Accounts.AddRangeAsync(ownAccount, otherAccount);

        var now = DateTime.UtcNow;
        var ownTx = CreateTransaction(ownAccount.Id, now, TestUserId);
        var otherTx = CreateTransaction(otherAccount.Id, now, OtherUserId);

        await _context.Transactions.AddRangeAsync(ownTx, otherTx);
        await _context.SaveChangesAsync();

        // Act
        var count = await _repository.CountThisMonthAsync();

        // Assert
        Assert.That(count, Is.EqualTo(1), "Transactions belonging to another user should not be counted");
    }

    [Test]
    public async Task CountThisMonthAsync_IncludesSharedAccountTransactions()
    {
        // Arrange — account owned by OtherUser, shared with TestUser via AccountShare
        var sharedAccount = CreateAccount(OtherUserId);
        await _context.Accounts.AddAsync(sharedAccount);

        var now = DateTime.UtcNow;
        var sharedTx = CreateTransaction(sharedAccount.Id, now, OtherUserId);
        await _context.Transactions.AddAsync(sharedTx);

        var share = new AccountShare
        {
            Id = Guid.NewGuid(),
            AccountId = sharedAccount.Id,
            UserId = OtherUserId,
            SharedWithUserId = TestUserId,
            Permission = PermissionLevel.Viewer,
            Status = ShareStatus.Accepted
        };
        await _context.AccountShares.AddAsync(share);
        await _context.SaveChangesAsync();

        // Act
        var count = await _repository.CountThisMonthAsync();

        // Assert
        Assert.That(count, Is.EqualTo(1), "Transactions on accounts shared with the current user should be counted");
    }

    [Test]
    public async Task CountThisMonthAsync_ReturnsZero_WhenNoTransactionsThisMonth()
    {
        // Arrange — only a transaction from last month
        var account = CreateAccount();
        await _context.Accounts.AddAsync(account);

        var now = DateTime.UtcNow;
        var startOfCurrentMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonth = startOfCurrentMonth.AddMonths(-1);
        var oldTx = CreateTransaction(account.Id, lastMonth);
        await _context.Transactions.AddAsync(oldTx);
        await _context.SaveChangesAsync();

        // Act
        var count = await _repository.CountThisMonthAsync();

        // Assert
        Assert.That(count, Is.EqualTo(0));
    }
}
