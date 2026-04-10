using LocalFinanceManager.Controllers;
using LocalFinanceManager.Data;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.Models;
using LocalFinanceManager.Services;
using LocalFinanceManager.Tests.Fixtures;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace LocalFinanceManager.Tests.Integration;

/// <summary>
/// Integration tests for AdminController verifying controller and service behaviour.
/// Note: authorization policy ([Authorize(Policy = "AdminPolicy")]) is not executed here because
/// the controller is instantiated directly; policy enforcement is covered by E2E tests.
/// </summary>
[TestFixture]
public class AdminControllerIntegrationTests
{
    private static readonly Guid AdminUserId = Guid.Parse("aaaa0001-0000-0000-0000-000000000001");
    private static readonly Guid SecondAdminUserId = Guid.Parse("cccc0003-0000-0000-0000-000000000003");
    private static readonly Guid RegularUserId = Guid.Parse("bbbb0002-0000-0000-0000-000000000002");

    private AppDbContext _context = null!;
    private AdminService _adminService = null!;
    private AdminController _adminController = null!;
    private AdminController _secondAdminController = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        var serviceLogger = new Mock<ILogger<AdminService>>().Object;
        _adminService = new AdminService(_context, serviceLogger);

        var controllerLogger = new Mock<ILogger<AdminController>>().Object;

        // Admin controller (requesting user is admin)
        _adminController = new AdminController(_adminService, new TestUserContext(AdminUserId, isAdmin: true), controllerLogger);
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Second admin controller (used to test admin-to-admin role changes)
        _secondAdminController = new AdminController(_adminService, new TestUserContext(SecondAdminUserId, isAdmin: true), controllerLogger);
        _secondAdminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

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
            new User { Id = AdminUserId, SupabaseUserId = AdminUserId.ToString(), Email = "admin@test.com", DisplayName = "Admin", IsAdmin = true },
            new User { Id = SecondAdminUserId, SupabaseUserId = SecondAdminUserId.ToString(), Email = "admin2@test.com", DisplayName = "Admin Two", IsAdmin = true },
            new User { Id = RegularUserId, SupabaseUserId = RegularUserId.ToString(), Email = "user@test.com", DisplayName = "User", IsAdmin = false }
        );

        var account = new Account { Id = Guid.NewGuid(), Label = "Account 1", IBAN = "NL91ABNA0417164300", Currency = "EUR", UserId = AdminUserId, Type = AccountType.Checking };
        _context.Accounts.Add(account);

        _context.AccountShares.Add(new AccountShare
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            UserId = AdminUserId,
            SharedWithUserId = RegularUserId,
            Permission = PermissionLevel.Viewer,
            Status = ShareStatus.Accepted
        });

        _context.SaveChanges();
    }

    // GET /api/admin/users

    [Test]
    public async Task GetUsers_ReturnsOkWithUsers()
    {
        var result = await _adminController.GetUsers(CancellationToken.None);
        var ok = result.Result as OkObjectResult;

        Assert.That(ok, Is.Not.Null);
        var users = ok!.Value as List<UserSummaryResponse>;
        Assert.That(users, Is.Not.Null);
        Assert.That(users!.Count, Is.EqualTo(3));
    }

    [Test]
    public async Task GetUsers_ReturnsCorrectIsAdminFlag()
    {
        var result = await _adminController.GetUsers(CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var users = (ok!.Value as List<UserSummaryResponse>)!;

        var admin = users.Single(u => u.Id == AdminUserId);
        var regular = users.Single(u => u.Id == RegularUserId);

        Assert.That(admin.IsAdmin, Is.True);
        Assert.That(regular.IsAdmin, Is.False);
    }

    // GET /api/admin/users/{id}/shares

    [Test]
    public async Task GetUserShares_ReturnsOkWithShares()
    {
        var result = await _adminController.GetUserShares(AdminUserId, CancellationToken.None);
        var ok = result.Result as OkObjectResult;

        Assert.That(ok, Is.Not.Null);
        var shares = ok!.Value as UserSharesResponse;
        Assert.That(shares, Is.Not.Null);
        Assert.That(shares!.AccountShares.Count, Is.EqualTo(1));
        Assert.That(shares.AccountShares[0].SharedWithEmail, Is.EqualTo("user@test.com"));
    }

    [Test]
    public async Task GetUserShares_UserWithNoShares_ReturnsEmptyLists()
    {
        var result = await _adminController.GetUserShares(RegularUserId, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var shares = (ok!.Value as UserSharesResponse)!;

        Assert.That(shares.AccountShares.Count, Is.EqualTo(0));
        Assert.That(shares.BudgetPlanShares.Count, Is.EqualTo(0));
    }

    // POST /api/admin/users/{id}/toggle-admin

    [Test]
    public async Task ToggleAdmin_NonAdminUser_FlipsToAdmin()
    {
        var result = await _adminController.ToggleAdmin(RegularUserId, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
        var user = await _context.Users.FindAsync(RegularUserId);
        Assert.That(user!.IsAdmin, Is.True);
    }

    [Test]
    public async Task ToggleAdmin_AdminUser_RequestedByAnotherAdmin_FlipsToNonAdmin()
    {
        var result = await _secondAdminController.ToggleAdmin(AdminUserId, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
        var user = await _context.Users.FindAsync(AdminUserId);
        Assert.That(user!.IsAdmin, Is.False);
    }

    [Test]
    public async Task ToggleAdmin_SelfDemotion_ReturnsBadRequest()
    {
        var result = await _adminController.ToggleAdmin(AdminUserId, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task ToggleAdmin_UnknownUser_ReturnsNotFound()
    {
        var result = await _adminController.ToggleAdmin(Guid.NewGuid(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }
}
