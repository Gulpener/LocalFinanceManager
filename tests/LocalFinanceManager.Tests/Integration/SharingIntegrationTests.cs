using LocalFinanceManager.Data;
using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.Models;
using LocalFinanceManager.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using LocalFinanceManager.Tests.Fixtures;

namespace LocalFinanceManager.Tests.Integration;

[TestFixture]
public class SharingIntegrationTests
{
    private static readonly Guid OwnerUserId = Guid.Parse("aaaa0000-0000-0000-0000-000000000001");
    private static readonly Guid RecipientUserId = Guid.Parse("bbbb0000-0000-0000-0000-000000000002");

    private AppDbContext _context = null!;
    private SharingService _sharingService = null!;
    private AccountRepository _accountRepo = null!;
    private BudgetPlanRepository _budgetPlanRepo = null!;
    private TransactionRepository _transactionRepo = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        var sharingLogger = new Mock<ILogger<SharingService>>();
        _sharingService = new SharingService(_context, sharingLogger.Object);

        var accountLogger = new Mock<ILogger<Repository<Account>>>();
        var budgetPlanLogger = new Mock<ILogger<Repository<BudgetPlan>>>();
        var transactionLogger = new Mock<ILogger<TransactionRepository>>();

        _accountRepo = new AccountRepository(_context, accountLogger.Object, new TestUserContext(OwnerUserId));
        _budgetPlanRepo = new BudgetPlanRepository(_context, budgetPlanLogger.Object, new TestUserContext(OwnerUserId));
        _transactionRepo = new TransactionRepository(_context, transactionLogger.Object, new TestUserContext(RecipientUserId));

        SeedUsers();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    private void SeedUsers()
    {
        _context.Users.AddRange(
            new User { Id = OwnerUserId, SupabaseUserId = OwnerUserId.ToString(), Email = "owner@test.com", DisplayName = "Owner" },
            new User { Id = RecipientUserId, SupabaseUserId = RecipientUserId.ToString(), Email = "recipient@test.com", DisplayName = "Recipient" }
        );
        _context.SaveChanges();
    }

    private Account SeedAccount()
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Label = "Test Account",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = 1000,
            UserId = OwnerUserId
        };
        _context.Accounts.Add(account);
        _context.SaveChanges();
        return account;
    }

    private BudgetPlan SeedBudgetPlan(Guid accountId)
    {
        var plan = new BudgetPlan
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Name = "Test Plan",
            Year = 2026,
            UserId = OwnerUserId
        };
        _context.BudgetPlans.Add(plan);
        _context.SaveChanges();
        return plan;
    }

    private Transaction SeedTransaction(Guid accountId)
    {
        var tx = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Amount = -50,
            Date = DateTime.UtcNow,
            Description = "Test",
            UserId = OwnerUserId
        };
        _context.Transactions.Add(tx);
        _context.SaveChanges();
        return tx;
    }

    // --- Pending share does NOT grant repository access ---

    [Test]
    public async Task AccountRepository_PendingShare_DoesNotGrantAccess()
    {
        var account = SeedAccount();
        await _sharingService.ShareAccountAsync(account.Id, "recipient@test.com", PermissionLevel.Viewer, OwnerUserId);

        // Recipient repo
        var recipientRepo = new AccountRepository(_context,
            new Mock<ILogger<Repository<Account>>>().Object,
            new TestUserContext(RecipientUserId));

        var result = await recipientRepo.GetReadableByIdAsync(account.Id);

        Assert.That(result, Is.Null);
    }

    // --- Accepted share DOES grant repository access ---

    [Test]
    public async Task AccountRepository_AcceptedShare_GrantsAccess()
    {
        var account = SeedAccount();
        var share = await _sharingService.ShareAccountAsync(account.Id, "recipient@test.com", PermissionLevel.Viewer, OwnerUserId);
        await _sharingService.AcceptAccountShareAsync(share.Id, RecipientUserId);

        var recipientRepo = new AccountRepository(_context,
            new Mock<ILogger<Repository<Account>>>().Object,
            new TestUserContext(RecipientUserId));

        var result = await recipientRepo.GetReadableByIdAsync(account.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(account.Id));
    }

    // --- Declined share never grants access ---

    [Test]
    public async Task AccountRepository_DeclinedShare_DoesNotGrantAccess()
    {
        var account = SeedAccount();
        var share = await _sharingService.ShareAccountAsync(account.Id, "recipient@test.com", PermissionLevel.Viewer, OwnerUserId);
        await _sharingService.DeclineAccountShareAsync(share.Id, RecipientUserId);

        var recipientRepo = new AccountRepository(_context,
            new Mock<ILogger<Repository<Account>>>().Object,
            new TestUserContext(RecipientUserId));

        var result = await recipientRepo.GetReadableByIdAsync(account.Id);

        Assert.That(result, Is.Null);
    }

    // --- Revocation immediately removes access ---

    [Test]
    public async Task AccountRepository_RevokedShare_RemovesAccess()
    {
        var account = SeedAccount();
        var share = await _sharingService.ShareAccountAsync(account.Id, "recipient@test.com", PermissionLevel.Viewer, OwnerUserId);
        await _sharingService.AcceptAccountShareAsync(share.Id, RecipientUserId);

        var recipientRepo = new AccountRepository(_context,
            new Mock<ILogger<Repository<Account>>>().Object,
            new TestUserContext(RecipientUserId));

        // Verify access before revocation
        var before = await recipientRepo.GetReadableByIdAsync(account.Id);
        Assert.That(before, Is.Not.Null);

        // Revoke
        await _sharingService.RevokeAccountShareAsync(share.Id, OwnerUserId);

        var after = await recipientRepo.GetReadableByIdAsync(account.Id);
        Assert.That(after, Is.Null);
    }

    // --- Viewer cannot mutate (enforced at service/controller level via CanEdit check) ---

    [Test]
    public async Task SharingService_ViewerPermission_CanViewButNotEdit()
    {
        var account = SeedAccount();
        var share = await _sharingService.ShareAccountAsync(account.Id, "recipient@test.com", PermissionLevel.Viewer, OwnerUserId);
        await _sharingService.AcceptAccountShareAsync(share.Id, RecipientUserId);

        var canView = await _sharingService.CanViewAccountAsync(account.Id, RecipientUserId);
        var canEdit = await _sharingService.CanEditAccountAsync(account.Id, RecipientUserId);

        Assert.That(canView, Is.True);
        Assert.That(canEdit, Is.False);
    }

    // --- Cascade: accepted Account share grants access to its Transactions ---

    [Test]
    public async Task TransactionRepository_AcceptedAccountShare_GrantsTransactionAccess()
    {
        var account = SeedAccount();
        var tx = SeedTransaction(account.Id);

        var share = await _sharingService.ShareAccountAsync(account.Id, "recipient@test.com", PermissionLevel.Viewer, OwnerUserId);
        await _sharingService.AcceptAccountShareAsync(share.Id, RecipientUserId);

        var txs = await _transactionRepo.GetByAccountIdAsync(account.Id);

        Assert.That(txs, Has.Count.EqualTo(1));
        Assert.That(txs[0].Id, Is.EqualTo(tx.Id));
    }

    [Test]
    public async Task TransactionRepository_PendingAccountShare_DoesNotGrantTransactionAccess()
    {
        var account = SeedAccount();
        SeedTransaction(account.Id);

        await _sharingService.ShareAccountAsync(account.Id, "recipient@test.com", PermissionLevel.Viewer, OwnerUserId);

        var txs = await _transactionRepo.GetByAccountIdAsync(account.Id);

        Assert.That(txs, Is.Empty);
    }

    // --- Cascade: accepted BudgetPlan share grants access to Transactions of the linked Account ---

    [Test]
    public async Task TransactionRepository_AcceptedBudgetPlanShare_GrantsTransactionAccess()
    {
        var account = SeedAccount();
        var plan = SeedBudgetPlan(account.Id);

        // Wire up CurrentBudgetPlanId so the repository's BudgetPlanShare lookup can match
        account.CurrentBudgetPlanId = plan.Id;
        _context.SaveChanges();

        var tx = SeedTransaction(account.Id);

        var share = await _sharingService.ShareBudgetPlanAsync(plan.Id, "recipient@test.com", PermissionLevel.Viewer, OwnerUserId);
        await _sharingService.AcceptBudgetPlanShareAsync(share.Id, RecipientUserId);

        var txs = await _transactionRepo.GetByAccountIdAsync(account.Id);

        Assert.That(txs, Has.Count.EqualTo(1));
        Assert.That(txs[0].Id, Is.EqualTo(tx.Id));
    }

    [Test]
    public async Task TransactionRepository_PendingBudgetPlanShare_DoesNotGrantTransactionAccess()
    {
        var account = SeedAccount();
        var plan = SeedBudgetPlan(account.Id);

        account.CurrentBudgetPlanId = plan.Id;
        _context.SaveChanges();

        SeedTransaction(account.Id);

        await _sharingService.ShareBudgetPlanAsync(plan.Id, "recipient@test.com", PermissionLevel.Viewer, OwnerUserId);

        var txs = await _transactionRepo.GetByAccountIdAsync(account.Id);

        Assert.That(txs, Is.Empty);
    }

    // --- BudgetPlan sharing ---

    [Test]
    public async Task BudgetPlanRepository_AcceptedShare_GrantsAccess()
    {
        var account = SeedAccount();
        var plan = SeedBudgetPlan(account.Id);

        var share = await _sharingService.ShareBudgetPlanAsync(plan.Id, "recipient@test.com", PermissionLevel.Viewer, OwnerUserId);
        await _sharingService.AcceptBudgetPlanShareAsync(share.Id, RecipientUserId);

        var recipientRepo = new BudgetPlanRepository(_context,
            new Mock<ILogger<Repository<BudgetPlan>>>().Object,
            new TestUserContext(RecipientUserId));

        var result = await recipientRepo.GetByIdWithLinesAsync(plan.Id);

        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task BudgetPlanRepository_PendingShare_DoesNotGrantAccess()
    {
        var account = SeedAccount();
        var plan = SeedBudgetPlan(account.Id);

        await _sharingService.ShareBudgetPlanAsync(plan.Id, "recipient@test.com", PermissionLevel.Viewer, OwnerUserId);

        var recipientRepo = new BudgetPlanRepository(_context,
            new Mock<ILogger<Repository<BudgetPlan>>>().Object,
            new TestUserContext(RecipientUserId));

        var result = await recipientRepo.GetByIdWithLinesAsync(plan.Id);

        Assert.That(result, Is.Null);
    }
}
