using LocalFinanceManager.Services;
using LocalFinanceManager.Services.Authorization;
using Microsoft.AspNetCore.Authorization;
using Moq;
using NUnit.Framework;
using System.Security.Claims;

namespace LocalFinanceManager.Tests.Authorization;

[TestFixture]
public class IsAdminHandlerTests
{
    private Mock<IUserContext> _userContextMock = null!;
    private IsAdminHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _userContextMock = new Mock<IUserContext>();
        _handler = new IsAdminHandler(_userContextMock.Object);
    }

    private static AuthorizationHandlerContext CreateContext(bool isAuthenticated)
    {
        ClaimsIdentity identity;
        if (isAuthenticated)
        {
            identity = new ClaimsIdentity(new[]
            {
                new Claim("sub", "test-sub"),
            }, "TestAuth");
        }
        else
        {
            identity = new ClaimsIdentity();
        }

        var principal = new ClaimsPrincipal(identity);
        var requirement = new IsAdminRequirement();
        return new AuthorizationHandlerContext(new[] { requirement }, principal, null);
    }

    [Test]
    public async Task HandleRequirementAsync_AdminUser_Succeeds()
    {
        _userContextMock.Setup(u => u.IsAdminAsync()).ReturnsAsync(true);
        var context = CreateContext(isAuthenticated: true);

        await _handler.HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.True);
    }

    [Test]
    public async Task HandleRequirementAsync_NonAdminUser_Fails()
    {
        _userContextMock.Setup(u => u.IsAdminAsync()).ReturnsAsync(false);
        var context = CreateContext(isAuthenticated: true);

        await _handler.HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.False);
        Assert.That(context.HasFailed, Is.True);
    }

    [Test]
    public async Task HandleRequirementAsync_UnauthenticatedUser_Fails()
    {
        _userContextMock.Setup(u => u.IsAdminAsync()).ReturnsAsync(false);
        var context = CreateContext(isAuthenticated: false);

        await _handler.HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.False);
        Assert.That(context.HasFailed, Is.True);
    }

    [Test]
    public async Task HandleRequirementAsync_UnauthenticatedUser_DoesNotCallIsAdminAsync()
    {
        _userContextMock.Setup(u => u.IsAdminAsync()).ReturnsAsync(false);
        var context = CreateContext(isAuthenticated: false);

        await _handler.HandleAsync(context);

        _userContextMock.Verify(u => u.IsAdminAsync(), Times.Never);
    }
}
