using LocalFinanceManager.Data;
using LocalFinanceManager.Models;
using LocalFinanceManager.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace LocalFinanceManager.Tests.Services;

[TestFixture]
public class UserContextAdminCacheTests
{
    private AppDbContext _context = null!;
    private BlazorCircuitUser _circuitUser = null!;
    private Guid _userId;

    [SetUp]
    public void SetUp()
    {
        _userId = Guid.NewGuid();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _context.Users.Add(new User
        {
            Id = _userId,
            SupabaseUserId = "cache-test-user",
            Email = "cache-test@local.test",
            DisplayName = "Cache Test",
            IsAdmin = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsArchived = false
        });
        _context.SaveChanges();

        _circuitUser = new BlazorCircuitUser();
        _circuitUser.Initialize(_userId, "cache-test@local.test");
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Test]
    public async Task IsAdminAsync_UsesCachedValue_UntilInvalidated()
    {
        var sut = CreateUserContext();

        var initialResult = await sut.IsAdminAsync();
        Assert.That(initialResult, Is.True);

        var user = await _context.Users.SingleAsync(u => u.Id == _userId);
        user.IsAdmin = false;
        await _context.SaveChangesAsync();

        var cachedResult = await sut.IsAdminAsync();
        Assert.That(cachedResult, Is.True);

        sut.InvalidateAdminState();

        var refreshedResult = await sut.IsAdminAsync();
        Assert.That(refreshedResult, Is.False);
    }

    [Test]
    public async Task IsAdminAsync_EmptyCurrentUser_ResetsCachedAdminState()
    {
        var sut = CreateUserContext();

        Assert.That(await sut.IsAdminAsync(), Is.True);

        _circuitUser.Reset();

        var result = await sut.IsAdminAsync();

        Assert.That(result, Is.False);
    }

    private UserContext CreateUserContext()
    {
        return new UserContext(
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
            _circuitUser,
            _context,
            new TestScopeFactory(_context),
            NullLogger<UserContext>.Instance);
    }

    private sealed class TestScopeFactory(AppDbContext context) : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new TestScope(context);
    }

    private sealed class TestScope(AppDbContext context) : IServiceScope, IAsyncDisposable
    {
        public IServiceProvider ServiceProvider { get; } = new TestServiceProvider(context);

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class TestServiceProvider(AppDbContext context) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            return serviceType == typeof(AppDbContext) ? context : null;
        }
    }
}