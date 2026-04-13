using LocalFinanceManager.Data;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.Models;
using LocalFinanceManager.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace LocalFinanceManager.Tests.Services;

/// <summary>
/// Tests for UserPreferencesService profile methods (GetProfileAsync, UpdateProfileAsync,
/// GetProfileImagePathAsync, UpdateProfileImagePathAsync).
/// </summary>
[TestFixture]
public class UserPreferencesProfileServiceTests
{
    private static readonly Guid UserId = Guid.Parse("aaaabbbb-0000-0000-0000-000000000001");

    private AppDbContext _context = null!;
    private UserPreferencesService _service = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        var factory = new MockDbContextFactory(_context);
        _service = new UserPreferencesService(factory, _context, NullLogger<UserPreferencesService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    // ── GetProfileAsync ──────────────────────────────────────────────────────

    [Test]
    public async Task GetProfileAsync_NoPrefsExist_ReturnsEmptyProfile()
    {
        var result = await _service.GetProfileAsync(UserId);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.FirstName, Is.Null);
        Assert.That(result.LastName, Is.Null);
    }

    [Test]
    public async Task GetProfileAsync_EmptyUserId_ReturnsEmptyProfile()
    {
        var result = await _service.GetProfileAsync(Guid.Empty);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.FirstName, Is.Null);
        Assert.That(result.LastName, Is.Null);
    }

    [Test]
    public async Task GetProfileAsync_PrefsExistWithNames_ReturnsCorrectNames()
    {
        _context.UserPreferences.Add(new UserPreferences
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            FirstName = "Alice",
            LastName = "Smith"
        });
        await _context.SaveChangesAsync();

        var result = await _service.GetProfileAsync(UserId);

        Assert.That(result.FirstName, Is.EqualTo("Alice"));
        Assert.That(result.LastName, Is.EqualTo("Smith"));
    }

    [Test]
    public async Task GetProfileAsync_ArchivedPrefs_ReturnsEmptyProfile()
    {
        _context.UserPreferences.Add(new UserPreferences
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            FirstName = "Ghost",
            IsArchived = true
        });
        await _context.SaveChangesAsync();

        var result = await _service.GetProfileAsync(UserId);

        Assert.That(result.FirstName, Is.Null);
    }

    // ── UpdateProfileAsync ───────────────────────────────────────────────────

    [Test]
    public async Task UpdateProfileAsync_NoExistingPrefs_CreatesNewRecord()
    {
        await _service.UpdateProfileAsync(UserId, "Bob", "Jones");

        var prefs = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == UserId && !p.IsArchived);

        Assert.That(prefs, Is.Not.Null);
        Assert.That(prefs!.FirstName, Is.EqualTo("Bob"));
        Assert.That(prefs.LastName, Is.EqualTo("Jones"));
    }

    [Test]
    public async Task UpdateProfileAsync_ExistingPrefs_UpdatesRecord()
    {
        _context.UserPreferences.Add(new UserPreferences
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            FirstName = "Old",
            LastName = "Name"
        });
        await _context.SaveChangesAsync();

        await _service.UpdateProfileAsync(UserId, "New", "Name2");

        var prefs = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == UserId && !p.IsArchived);

        Assert.That(prefs!.FirstName, Is.EqualTo("New"));
        Assert.That(prefs.LastName, Is.EqualTo("Name2"));
    }

    [Test]
    public async Task UpdateProfileAsync_EmptyUserId_DoesNothing()
    {
        await _service.UpdateProfileAsync(Guid.Empty, "Bob", "Jones");

        var count = await _context.UserPreferences.CountAsync();
        Assert.That(count, Is.EqualTo(0));
    }

    // ── GetProfileImagePathAsync ─────────────────────────────────────────────

    [Test]
    public async Task GetProfileImagePathAsync_NoPrefs_ReturnsNull()
    {
        var result = await _service.GetProfileImagePathAsync(UserId);
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetProfileImagePathAsync_WithImagePath_ReturnsPath()
    {
        _context.UserPreferences.Add(new UserPreferences
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            ProfileImagePath = "user-123/avatar.jpg"
        });
        await _context.SaveChangesAsync();

        var result = await _service.GetProfileImagePathAsync(UserId);
        Assert.That(result, Is.EqualTo("user-123/avatar.jpg"));
    }

    // ── UpdateProfileImagePathAsync ──────────────────────────────────────────

    [Test]
    public async Task UpdateProfileImagePathAsync_NoExistingPrefs_CreatesNewRecord()
    {
        await _service.UpdateProfileImagePathAsync(UserId, "user-123/avatar.png");

        var prefs = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == UserId && !p.IsArchived);

        Assert.That(prefs, Is.Not.Null);
        Assert.That(prefs!.ProfileImagePath, Is.EqualTo("user-123/avatar.png"));
    }

    [Test]
    public async Task UpdateProfileImagePathAsync_ExistingPrefs_UpdatesPath()
    {
        _context.UserPreferences.Add(new UserPreferences
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            ProfileImagePath = "old/path.jpg"
        });
        await _context.SaveChangesAsync();

        await _service.UpdateProfileImagePathAsync(UserId, "new/path.png");

        var prefs = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == UserId && !p.IsArchived);

        Assert.That(prefs!.ProfileImagePath, Is.EqualTo("new/path.png"));
    }

    [Test]
    public async Task UpdateProfileImagePathAsync_SetToNull_ClearsPath()
    {
        _context.UserPreferences.Add(new UserPreferences
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            ProfileImagePath = "some/path.jpg"
        });
        await _context.SaveChangesAsync();

        await _service.UpdateProfileImagePathAsync(UserId, null);

        var prefs = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == UserId && !p.IsArchived);

        Assert.That(prefs!.ProfileImagePath, Is.Null);
    }

    /// <summary>
    /// Wraps a single AppDbContext so it can be used as IDbContextFactory in tests.
    /// </summary>
    private sealed class MockDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly AppDbContext _context;
        public MockDbContextFactory(AppDbContext context) => _context = context;
        public AppDbContext CreateDbContext() => _context;
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken ct = default)
            => Task.FromResult(_context);
    }
}
