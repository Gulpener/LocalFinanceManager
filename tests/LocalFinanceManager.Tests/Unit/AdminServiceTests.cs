using LocalFinanceManager.Data;
using LocalFinanceManager.Configuration;
using LocalFinanceManager.Models;
using LocalFinanceManager.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace LocalFinanceManager.Tests.Unit;

[TestFixture]
public class AdminServiceTests
{
    private static readonly Guid User1Id = Guid.Parse("aaaa0001-0000-0000-0000-000000000001");
    private static readonly Guid User2Id = Guid.Parse("bbbb0002-0000-0000-0000-000000000002");
    private static readonly Guid User3Id = Guid.Parse("cccc0003-0000-0000-0000-000000000003");

    private AppDbContext _context = null!;
    private AdminService _service = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        var logger = new Mock<ILogger<AdminService>>().Object;
        var storageServiceMock = new Mock<ISupabaseStorageService>();
        storageServiceMock
            .Setup(s => s.GetPublicUrl(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string bucket, string path) => $"https://cdn.example/{bucket}/{path}");
        var supabaseOptions = Options.Create(new SupabaseOptions { StorageBucket = "profile-pictures" });
        _service = new AdminService(_context, logger, storageServiceMock.Object, supabaseOptions);

        SeedTestData();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    private void SeedTestData()
    {
        _context.Users.AddRange(
            new User { Id = User1Id, SupabaseUserId = User1Id.ToString(), Email = "user1@test.com", DisplayName = "User 1", IsAdmin = true },
            new User { Id = User2Id, SupabaseUserId = User2Id.ToString(), Email = "user2@test.com", DisplayName = "User 2", IsAdmin = false },
            new User { Id = User3Id, SupabaseUserId = User3Id.ToString(), Email = "user3@test.com", DisplayName = "User 3", IsAdmin = false }
        );

        // User1 owns account1
        var account1 = new Account { Id = Guid.NewGuid(), Label = "Account 1", IBAN = "NL91ABNA0417164300", Currency = "EUR", UserId = User1Id, Type = AccountType.Checking };
        var account2 = new Account { Id = Guid.NewGuid(), Label = "Account 2", IBAN = "NL20INGB0001234567", Currency = "EUR", UserId = User2Id, Type = AccountType.Checking };
        _context.Accounts.AddRange(account1, account2);

        // User1 shares account1 with User2
        _context.AccountShares.Add(new AccountShare
        {
            Id = Guid.NewGuid(),
            AccountId = account1.Id,
            UserId = User1Id,
            SharedWithUserId = User2Id,
            Permission = PermissionLevel.Viewer,
            Status = ShareStatus.Accepted
        });

        // User2 shares account2 with User3
        _context.AccountShares.Add(new AccountShare
        {
            Id = Guid.NewGuid(),
            AccountId = account2.Id,
            UserId = User2Id,
            SharedWithUserId = User3Id,
            Permission = PermissionLevel.Viewer,
            Status = ShareStatus.Accepted
        });

        _context.SaveChanges();
    }

    // GetAllUsersAsync

    [Test]
    public async Task GetAllUsersAsync_ReturnsAllNonArchivedUsers()
    {
        var result = await _service.GetAllUsersAsync();

        Assert.That(result.Count, Is.EqualTo(3));
    }

    [Test]
    public async Task GetAllUsersAsync_ExcludesArchivedUsers()
    {
        var archivedUser = new User
        {
            Id = Guid.NewGuid(),
            SupabaseUserId = "archived-user",
            Email = "archived@test.com",
            DisplayName = "Archived",
            IsArchived = true
        };
        _context.Users.Add(archivedUser);
        _context.SaveChanges();

        var result = await _service.GetAllUsersAsync();

        Assert.That(result.Count, Is.EqualTo(3));
        Assert.That(result.Any(u => u.Id == archivedUser.Id), Is.False);
    }

    [Test]
    public async Task GetAllUsersAsync_ReturnsCorrectAccountCount()
    {
        var result = await _service.GetAllUsersAsync();

        var user1 = result.Single(u => u.Id == User1Id);
        var user2 = result.Single(u => u.Id == User2Id);
        var user3 = result.Single(u => u.Id == User3Id);

        Assert.That(user1.AccountCount, Is.EqualTo(1));
        Assert.That(user2.AccountCount, Is.EqualTo(1));
        Assert.That(user3.AccountCount, Is.EqualTo(0));
    }

    [Test]
    public async Task GetAllUsersAsync_ReturnsCorrectShareCounts()
    {
        var result = await _service.GetAllUsersAsync();

        var user1 = result.Single(u => u.Id == User1Id);
        var user2 = result.Single(u => u.Id == User2Id);
        var user3 = result.Single(u => u.Id == User3Id);

        Assert.That(user1.SharesGiven, Is.EqualTo(1));
        Assert.That(user1.SharesReceived, Is.EqualTo(0));

        Assert.That(user2.SharesGiven, Is.EqualTo(1));
        Assert.That(user2.SharesReceived, Is.EqualTo(1));

        Assert.That(user3.SharesGiven, Is.EqualTo(0));
        Assert.That(user3.SharesReceived, Is.EqualTo(1));
    }

    [Test]
    public async Task GetAllUsersAsync_ReturnsCorrectIsAdminFlag()
    {
        var result = await _service.GetAllUsersAsync();

        var user1 = result.Single(u => u.Id == User1Id);
        var user2 = result.Single(u => u.Id == User2Id);

        Assert.That(user1.IsAdmin, Is.True);
        Assert.That(user2.IsAdmin, Is.False);
    }

    // GetUserSharesAsync

    [Test]
    public async Task GetUserSharesAsync_ReturnsSharesForUser()
    {
        var result = await _service.GetUserSharesAsync(User1Id);

        Assert.That(result.AccountShares.Count, Is.EqualTo(1));
        Assert.That(result.AccountShares[0].AccountName, Is.EqualTo("Account 1"));
        Assert.That(result.AccountShares[0].SharedWithEmail, Is.EqualTo("user2@test.com"));
    }

    [Test]
    public async Task GetUserSharesAsync_ExcludesArchivedShares()
    {
        // Archive the share
        var share = _context.AccountShares.First(s => s.UserId == User1Id);
        share.IsArchived = true;
        _context.SaveChanges();

        var result = await _service.GetUserSharesAsync(User1Id);

        Assert.That(result.AccountShares.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task GetUserSharesAsync_ReturnsEmptyForUserWithNoShares()
    {
        var result = await _service.GetUserSharesAsync(User3Id);

        Assert.That(result.AccountShares.Count, Is.EqualTo(0));
        Assert.That(result.BudgetPlanShares.Count, Is.EqualTo(0));
    }

    // ToggleAdminAsync

    [Test]
    public async Task ToggleAdminAsync_FlipsIsAdminFromFalseToTrue()
    {
        await _service.ToggleAdminAsync(User2Id, User1Id);

        var user = await _context.Users.FindAsync(User2Id);
        Assert.That(user!.IsAdmin, Is.True);
    }

    [Test]
    public async Task ToggleAdminAsync_FlipsIsAdminFromTrueToFalse()
    {
        await _service.ToggleAdminAsync(User1Id, User2Id);

        var user = await _context.Users.FindAsync(User1Id);
        Assert.That(user!.IsAdmin, Is.False);
    }

    [Test]
    public void ToggleAdminAsync_ThrowsWhenSelfDemotion()
    {
        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ToggleAdminAsync(User1Id, User1Id));

        Assert.That(ex!.Message, Does.Contain("own admin role"));
    }

    [Test]
    public void ToggleAdminAsync_ThrowsWhenUserNotFound()
    {
        var unknownId = Guid.NewGuid();

        Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.ToggleAdminAsync(unknownId, User1Id));
    }
}
