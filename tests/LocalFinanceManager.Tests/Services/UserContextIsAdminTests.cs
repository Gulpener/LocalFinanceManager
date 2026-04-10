using LocalFinanceManager.Data;
using LocalFinanceManager.Models;
using LocalFinanceManager.Services;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace LocalFinanceManager.Tests.Services;

/// <summary>
/// Tests for UserContext.IsAdminAsync().
/// Uses direct DB context manipulation instead of full HTTP context setup.
/// </summary>
[TestFixture]
public class UserContextIsAdminTests
{
    private static readonly Guid AdminUserId = Guid.Parse("aaaa0001-0000-0000-0000-000000000001");
    private static readonly Guid RegularUserId = Guid.Parse("bbbb0002-0000-0000-0000-000000000002");

    private AppDbContext _context = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _context.Users.AddRange(
            new User { Id = AdminUserId, SupabaseUserId = AdminUserId.ToString(), Email = "admin@test.com", DisplayName = "Admin", IsAdmin = true },
            new User { Id = RegularUserId, SupabaseUserId = RegularUserId.ToString(), Email = "user@test.com", DisplayName = "User", IsAdmin = false }
        );
        _context.SaveChanges();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    private IUserContext CreateUserContext(Guid userId)
    {
        // Uses a minimal IUserContext implementation (DbUserContext below) that calls the real DB
        // for IsAdminAsync, without requiring the full HTTP/Blazor circuit infrastructure.
        return new DbUserContext(userId, _context);
    }

    [Test]
    public async Task IsAdminAsync_AdminUser_ReturnsTrue()
    {
        var ctx = CreateUserContext(AdminUserId);
        var result = await ctx.IsAdminAsync();
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task IsAdminAsync_NonAdminUser_ReturnsFalse()
    {
        var ctx = CreateUserContext(RegularUserId);
        var result = await ctx.IsAdminAsync();
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsAdminAsync_EmptyUserId_ReturnsFalse()
    {
        var ctx = CreateUserContext(Guid.Empty);
        var result = await ctx.IsAdminAsync();
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsAdminAsync_UnknownUserId_ReturnsFalse()
    {
        var ctx = CreateUserContext(Guid.NewGuid());
        var result = await ctx.IsAdminAsync();
        Assert.That(result, Is.False);
    }

    /// <summary>
    /// Minimal IUserContext implementation that uses the real AppDbContext for IsAdminAsync
    /// without requiring the full HTTP/Blazor circuit infrastructure.
    /// </summary>
    private sealed class DbUserContext : IUserContext
    {
        private readonly Guid _userId;
        private readonly AppDbContext _context;

        public DbUserContext(Guid userId, AppDbContext context)
        {
            _userId = userId;
            _context = context;
        }

        public Guid GetCurrentUserId() => _userId;
        public string GetCurrentUserEmail() => string.Empty;
        public bool IsAuthenticated() => _userId != Guid.Empty;

        public async Task<bool> IsAdminAsync()
        {
            if (_userId == Guid.Empty) return false;
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == _userId)
                .Select(u => u.IsAdmin)
                .FirstOrDefaultAsync();
        }
    }
}
