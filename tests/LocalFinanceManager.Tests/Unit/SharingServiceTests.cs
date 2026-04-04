using LocalFinanceManager.Data;
using LocalFinanceManager.Models;
using LocalFinanceManager.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using LocalFinanceManager.Tests.Fixtures;

namespace LocalFinanceManager.Tests.Unit;

[TestFixture]
public class SharingServiceTests
{
    private static readonly Guid OwnerUserId = Guid.Parse("aaaa0000-0000-0000-0000-000000000001");
    private static readonly Guid RecipientUserId = Guid.Parse("bbbb0000-0000-0000-0000-000000000002");
    private static readonly Guid ThirdUserId = Guid.Parse("cccc0000-0000-0000-0000-000000000003");

    private AppDbContext _context = null!;
    private SharingService _service = null!;
    private Mock<ILogger<SharingService>> _mockLogger = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _mockLogger = new Mock<ILogger<SharingService>>();
        _service = new SharingService(_context, _mockLogger.Object);

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
            new User { Id = RecipientUserId, SupabaseUserId = RecipientUserId.ToString(), Email = "recipient@test.com", DisplayName = "Recipient" },
            new User { Id = ThirdUserId, SupabaseUserId = ThirdUserId.ToString(), Email = "third@test.com", DisplayName = "Third" }
        );
        _context.SaveChanges();
    }

    private Account SeedAccount(Guid ownerId)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Label = "Test Account",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = 1000,
            UserId = ownerId
        };
        _context.Accounts.Add(account);
        _context.SaveChanges();
        return account;
    }

    private BudgetPlan SeedBudgetPlan(Guid accountId, Guid ownerId)
    {
        var plan = new BudgetPlan
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Name = "Test Plan",
            Year = 2026,
            UserId = ownerId
        };
        _context.BudgetPlans.Add(plan);
        _context.SaveChanges();
        return plan;
    }

    // --- ShareAccount ---

    [Test]
    public async Task ShareAccountAsync_ValidRequest_CreatesPendingShare()
    {
        var account = SeedAccount(OwnerUserId);

        var share = await _service.ShareAccountAsync(account.Id, "recipient@test.com", PermissionLevel.Viewer, OwnerUserId);

        Assert.That(share, Is.Not.Null);
        Assert.That(share.Status, Is.EqualTo(ShareStatus.Pending));
        Assert.That(share.Permission, Is.EqualTo(PermissionLevel.Viewer));
        Assert.That(share.SharedWithUserId, Is.EqualTo(RecipientUserId));
    }

    [Test]
    public async Task ShareAccountAsync_NonOwner_ThrowsKeyNotFoundException()
    {
        var account = SeedAccount(OwnerUserId);

        Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.ShareAccountAsync(account.Id, "recipient@test.com", PermissionLevel.Viewer, RecipientUserId));
    }

    [Test]
    public async Task ShareAccountAsync_UnknownEmail_ThrowsKeyNotFoundException()
    {
        var account = SeedAccount(OwnerUserId);

        Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.ShareAccountAsync(account.Id, "notexist@test.com", PermissionLevel.Viewer, OwnerUserId));
    }

    [Test]
    public async Task ShareAccountAsync_SelfShare_ThrowsInvalidOperationException()
    {
        var account = SeedAccount(OwnerUserId);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ShareAccountAsync(account.Id, "owner@test.com", PermissionLevel.Viewer, OwnerUserId));
    }

    [Test]
    public async Task ShareAccountAsync_DuplicatePendingShare_ThrowsInvalidOperationException()
    {
        var account = SeedAccount(OwnerUserId);
        await _service.ShareAccountAsync(account.Id, "recipient@test.com", PermissionLevel.Viewer, OwnerUserId);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ShareAccountAsync(account.Id, "recipient@test.com", PermissionLevel.Editor, OwnerUserId));
    }

    // --- Accept / Decline / Revoke ---

    [Test]
    public async Task AcceptAccountShareAsync_Recipient_SetsStatusAccepted()
    {
        var account = SeedAccount(OwnerUserId);
        var share = await _service.ShareAccountAsync(account.Id, "recipient@test.com", PermissionLevel.Viewer, OwnerUserId);

        await _service.AcceptAccountShareAsync(share.Id, RecipientUserId);

        var updated = await _context.AccountShares.FindAsync(share.Id);
        Assert.That(updated!.Status, Is.EqualTo(ShareStatus.Accepted));
    }

    [Test]
    public async Task AcceptAccountShareAsync_NonRecipient_ThrowsKeyNotFoundException()
    {
        var account = SeedAccount(OwnerUserId);
        var share = await _service.ShareAccountAsync(account.Id, "recipient@test.com", PermissionLevel.Viewer, OwnerUserId);

        Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.AcceptAccountShareAsync(share.Id, ThirdUserId));
    }

    [Test]
    public async Task DeclineAccountShareAsync_Recipient_SetsStatusDeclined()
    {
        var account = SeedAccount(OwnerUserId);
        var share = await _service.ShareAccountAsync(account.Id, "recipient@test.com", PermissionLevel.Viewer, OwnerUserId);

        await _service.DeclineAccountShareAsync(share.Id, RecipientUserId);

        var updated = await _context.AccountShares.FindAsync(share.Id);
        Assert.That(updated!.Status, Is.EqualTo(ShareStatus.Declined));
    }

    [Test]
    public async Task DeclineAccountShareAsync_AlreadyAccepted_ThrowsInvalidOperationException()
    {
        var account = SeedAccount(OwnerUserId);
        var share = await _service.ShareAccountAsync(account.Id, "recipient@test.com", PermissionLevel.Viewer, OwnerUserId);
        await _service.AcceptAccountShareAsync(share.Id, RecipientUserId);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeclineAccountShareAsync(share.Id, RecipientUserId));
    }

    [Test]
    public async Task RevokeAccountShareAsync_Owner_RemovesShare()
    {
        var account = SeedAccount(OwnerUserId);
        var share = await _service.ShareAccountAsync(account.Id, "recipient@test.com", PermissionLevel.Viewer, OwnerUserId);

        await _service.RevokeAccountShareAsync(share.Id, OwnerUserId);

        var deleted = await _context.AccountShares.FindAsync(share.Id);
        Assert.That(deleted, Is.Null);
    }

    [Test]
    public async Task RevokeAccountShareAsync_NonOwner_ThrowsKeyNotFoundException()
    {
        var account = SeedAccount(OwnerUserId);
        var share = await _service.ShareAccountAsync(account.Id, "recipient@test.com", PermissionLevel.Viewer, OwnerUserId);

        Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.RevokeAccountShareAsync(share.Id, RecipientUserId));
    }

    // --- Permission level checks ---

    [Test]
    public async Task GetUserAccountAccessLevelAsync_Owner_ReturnsOwner()
    {
        var account = SeedAccount(OwnerUserId);

        var level = await _service.GetUserAccountAccessLevelAsync(account.Id, OwnerUserId);

        Assert.That(level, Is.EqualTo(PermissionLevel.Owner));
    }

    [Test]
    public async Task GetUserAccountAccessLevelAsync_AcceptedViewer_ReturnsViewer()
    {
        var account = SeedAccount(OwnerUserId);
        var share = await _service.ShareAccountAsync(account.Id, "recipient@test.com", PermissionLevel.Viewer, OwnerUserId);
        await _service.AcceptAccountShareAsync(share.Id, RecipientUserId);

        var level = await _service.GetUserAccountAccessLevelAsync(account.Id, RecipientUserId);

        Assert.That(level, Is.EqualTo(PermissionLevel.Viewer));
    }

    [Test]
    public async Task GetUserAccountAccessLevelAsync_PendingShare_ReturnsNull()
    {
        var account = SeedAccount(OwnerUserId);
        await _service.ShareAccountAsync(account.Id, "recipient@test.com", PermissionLevel.Viewer, OwnerUserId);

        var level = await _service.GetUserAccountAccessLevelAsync(account.Id, RecipientUserId);

        Assert.That(level, Is.Null);
    }

    [Test]
    public async Task GetUserAccountAccessLevelAsync_DeclinedShare_ReturnsNull()
    {
        var account = SeedAccount(OwnerUserId);
        var share = await _service.ShareAccountAsync(account.Id, "recipient@test.com", PermissionLevel.Viewer, OwnerUserId);
        await _service.DeclineAccountShareAsync(share.Id, RecipientUserId);

        var level = await _service.GetUserAccountAccessLevelAsync(account.Id, RecipientUserId);

        Assert.That(level, Is.Null);
    }

    [Test]
    public async Task CanEditAccountAsync_Viewer_ReturnsFalse()
    {
        var account = SeedAccount(OwnerUserId);
        var share = await _service.ShareAccountAsync(account.Id, "recipient@test.com", PermissionLevel.Viewer, OwnerUserId);
        await _service.AcceptAccountShareAsync(share.Id, RecipientUserId);

        var canEdit = await _service.CanEditAccountAsync(account.Id, RecipientUserId);

        Assert.That(canEdit, Is.False);
    }

    [Test]
    public async Task CanEditAccountAsync_Editor_ReturnsTrue()
    {
        var account = SeedAccount(OwnerUserId);
        var share = await _service.ShareAccountAsync(account.Id, "recipient@test.com", PermissionLevel.Editor, OwnerUserId);
        await _service.AcceptAccountShareAsync(share.Id, RecipientUserId);

        var canEdit = await _service.CanEditAccountAsync(account.Id, RecipientUserId);

        Assert.That(canEdit, Is.True);
    }

    // --- BudgetPlan sharing ---

    [Test]
    public async Task ShareBudgetPlanAsync_ValidRequest_CreatesPendingShare()
    {
        var account = SeedAccount(OwnerUserId);
        var plan = SeedBudgetPlan(account.Id, OwnerUserId);

        var share = await _service.ShareBudgetPlanAsync(plan.Id, "recipient@test.com", PermissionLevel.Editor, OwnerUserId);

        Assert.That(share.Status, Is.EqualTo(ShareStatus.Pending));
        Assert.That(share.Permission, Is.EqualTo(PermissionLevel.Editor));
    }

    [Test]
    public async Task GetPendingSharesForUserAsync_ReturnsPendingOnly()
    {
        var account = SeedAccount(OwnerUserId);
        await _service.ShareAccountAsync(account.Id, "recipient@test.com", PermissionLevel.Viewer, OwnerUserId);
        var share2account = SeedAccount(OwnerUserId);
        var s2 = await _service.ShareAccountAsync(share2account.Id, "recipient@test.com", PermissionLevel.Editor, OwnerUserId);
        await _service.AcceptAccountShareAsync(s2.Id, RecipientUserId);

        var (pendingAccounts, _) = await _service.GetPendingSharesForUserAsync(RecipientUserId);

        Assert.That(pendingAccounts, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetPendingShareCountAsync_CountsAllPendingAcrossTypes()
    {
        var account = SeedAccount(OwnerUserId);
        var plan = SeedBudgetPlan(account.Id, OwnerUserId);

        await _service.ShareAccountAsync(account.Id, "recipient@test.com", PermissionLevel.Viewer, OwnerUserId);
        await _service.ShareBudgetPlanAsync(plan.Id, "recipient@test.com", PermissionLevel.Viewer, OwnerUserId);

        var count = await _service.GetPendingShareCountAsync(RecipientUserId);

        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public async Task RevokeAccountShareAsync_WorksOnAnyStatus()
    {
        var account = SeedAccount(OwnerUserId);
        var share = await _service.ShareAccountAsync(account.Id, "recipient@test.com", PermissionLevel.Viewer, OwnerUserId);
        await _service.AcceptAccountShareAsync(share.Id, RecipientUserId);

        // Revoke accepted share
        await _service.RevokeAccountShareAsync(share.Id, OwnerUserId);

        var deleted = await _context.AccountShares.FindAsync(share.Id);
        Assert.That(deleted, Is.Null);
    }
}
