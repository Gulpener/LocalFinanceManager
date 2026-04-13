using System.Security.Claims;
using LocalFinanceManager.Data;
using LocalFinanceManager.Models;
using LocalFinanceManager.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LocalFinanceManager.Tests.Services;

[TestFixture]
public class UserContextCurrentUserIdTests
{
    private AppDbContext _context = null!;
    private ServiceProvider _serviceProvider = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _serviceProvider = new ServiceCollection()
            .AddScoped(_ => _context)
            .BuildServiceProvider();
    }

    [TearDown]
    public void TearDown()
    {
        _serviceProvider.Dispose();
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Test]
    public void GetCurrentUserId_CircuitUserInitialized_PrefersCircuitUserOverHttpContext()
    {
        var circuitUserId = Guid.NewGuid();
        var httpClaimUserId = Guid.NewGuid();

        SeedUser(httpClaimUserId, "supabase-http-user");

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = CreateAuthenticatedPrincipal(subClaim: "supabase-http-user")
            }
        };

        var circuitUser = new BlazorCircuitUser();
        circuitUser.Initialize(circuitUserId, "admin@local.test");

        var sut = new UserContext(
            httpContextAccessor,
            circuitUser,
            _context,
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<UserContext>.Instance);

        var resolvedUserId = sut.GetCurrentUserId();

        Assert.That(resolvedUserId, Is.EqualTo(circuitUserId));
    }

    [Test]
    public void GetCurrentUserId_CircuitUserInitializedAndHttpMissingSub_StillReturnsCircuitUser()
    {
        var circuitUserId = Guid.NewGuid();

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = CreateAuthenticatedPrincipal(subClaim: null)
            }
        };

        var circuitUser = new BlazorCircuitUser();
        circuitUser.Initialize(circuitUserId, "admin@local.test");

        var sut = new UserContext(
            httpContextAccessor,
            circuitUser,
            _context,
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<UserContext>.Instance);

        var resolvedUserId = sut.GetCurrentUserId();

        Assert.That(resolvedUserId, Is.EqualTo(circuitUserId));
    }

    [Test]
    public void GetCurrentUserId_CircuitUserNotInitialized_ResolvesFromHttpSubClaim()
    {
        var expectedUserId = Guid.NewGuid();
        const string supabaseUserId = "supabase-user-id";

        SeedUser(expectedUserId, supabaseUserId);

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = CreateAuthenticatedPrincipal(subClaim: supabaseUserId)
            }
        };

        var circuitUserMock = new Mock<IBlazorCircuitUser>();
        circuitUserMock.SetupGet(x => x.IsInitialized).Returns(false);
        circuitUserMock.SetupGet(x => x.UserId).Returns(Guid.Empty);

        var sut = new UserContext(
            httpContextAccessor,
            circuitUserMock.Object,
            _context,
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<UserContext>.Instance);

        var resolvedUserId = sut.GetCurrentUserId();

        Assert.That(resolvedUserId, Is.EqualTo(expectedUserId));
    }

    private void SeedUser(Guid id, string supabaseUserId)
    {
        _context.Users.Add(new User
        {
            Id = id,
            SupabaseUserId = supabaseUserId,
            Email = $"{supabaseUserId}@local.test",
            DisplayName = "Test User",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsArchived = false
        });

        _context.SaveChanges();
    }

    private static ClaimsPrincipal CreateAuthenticatedPrincipal(string? subClaim)
    {
        var claims = new List<Claim>();

        if (!string.IsNullOrWhiteSpace(subClaim))
        {
            claims.Add(new Claim("sub", subClaim));
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
        return new ClaimsPrincipal(identity);
    }
}
