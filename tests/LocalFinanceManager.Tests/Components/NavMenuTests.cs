using Bunit;
using LocalFinanceManager.Components.Layout;
using LocalFinanceManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;

namespace LocalFinanceManager.Tests.Components;

[TestFixture]
public class NavMenuTests
{
    [Test]
    public void NavMenu_WhenUserIsNotLoggedIn_ShowsOnlyHomeMenuItem()
    {
        using var context = new BunitContext();
        var cut = RenderNavMenu(context, isAuthenticated: false, isAdmin: false);

        var navLinks = cut.FindAll("a.nav-link");

        Assert.Multiple(() =>
        {
            Assert.That(navLinks.Count, Is.EqualTo(1));
            Assert.That(navLinks[0].TextContent, Does.Contain("Home"));
            Assert.That(cut.Markup, Does.Not.Contain("Rekeningen"));
            Assert.That(cut.Markup, Does.Not.Contain("Transacties"));
            Assert.That(cut.Markup, Does.Not.Contain("Budgetplannen"));
            Assert.That(cut.Markup, Does.Not.Contain("Beheer"));
            Assert.That(cut.Markup, Does.Not.Contain("Automatisch toewijzen"));
        });
    }

    [Test]
    public void NavMenu_WhenUserIsLoggedIn_ShowsAuthenticatedMenuItems()
    {
        using var context = new BunitContext();
        var cut = RenderNavMenu(context, isAuthenticated: true, isAdmin: false);

        Assert.Multiple(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Dashboard"));
            Assert.That(cut.Markup, Does.Contain("Rekeningen"));
            Assert.That(cut.Markup, Does.Contain("Transacties"));
            Assert.That(cut.Markup, Does.Contain("Budgetplannen"));
            // Allow for whitespace/newlines between 'Automatisch' and 'toewijzen'
            Assert.That(cut.Markup, Does.Match("Automatisch\\s*toewijzen"));
            // Admin link is not shown for non-admin users
            Assert.That(cut.Markup, Does.Not.Contain("Beheer"));
        });
    }

    // Admin link visibility is verified in E2E AdminPanelTests.NavMenu_AdminLink_VisibleForAdminUser.
    // In these unit tests GetCurrentUserId() is mocked to return Guid.Empty, so
    // RefreshPendingCountAsync returns early before the DB scope is opened or IsAdminAsync is called.
    [Test]
    public void NavMenu_WhenAdminUserIsLoggedIn_RenderWithoutErrors()
    {
        using var context = new BunitContext();
        // Simply confirm the component renders without exceptions when user is admin.
        var cut = RenderNavMenu(context, isAuthenticated: true, isAdmin: true);
        Assert.That(cut.Markup, Does.Contain("Dashboard"));
    }

    private static IRenderedComponent<CascadingAuthenticationState> RenderNavMenu(BunitContext context, bool isAuthenticated, bool isAdmin)
    {
        context.Services.AddAuthorizationCore();
        context.Services.AddSingleton<IAuthorizationService>(new TestAuthorizationService(isAuthenticated));
        context.Services.AddScoped<AuthenticationStateProvider>(_ => new TestAuthenticationStateProvider(isAuthenticated));

        var userContextMock = new Mock<IUserContext>();
        userContextMock.Setup(x => x.GetCurrentUserId()).Returns(Guid.Empty);
        userContextMock.Setup(x => x.IsAuthenticated()).Returns(isAuthenticated);
        userContextMock.Setup(x => x.IsAdminAsync()).ReturnsAsync(isAdmin);
        context.Services.AddSingleton(userContextMock.Object);

        var sharingServiceMock = new Mock<ISharingService>();
        sharingServiceMock.Setup(x => x.GetPendingShareCountAsync(It.IsAny<Guid>())).ReturnsAsync(0);
        context.Services.AddSingleton(sharingServiceMock.Object);

        return context.Render<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent<NavMenu>());
    }

    private sealed class TestAuthenticationStateProvider(bool isAuthenticated) : AuthenticationStateProvider
    {
        private readonly AuthenticationState _authenticationState = new(
            new ClaimsPrincipal(
                isAuthenticated
                    ? new ClaimsIdentity([new Claim(ClaimTypes.Name, "test-user")], authenticationType: "TestAuth")
                    : new ClaimsIdentity()));

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            return Task.FromResult(_authenticationState);
        }
    }

    private sealed class TestAuthorizationService(bool isAuthenticated) : IAuthorizationService
    {
        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
        {
            return Task.FromResult(isAuthenticated ? AuthorizationResult.Success() : AuthorizationResult.Failed());
        }

        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName)
        {
            return Task.FromResult(isAuthenticated ? AuthorizationResult.Success() : AuthorizationResult.Failed());
        }
    }
}
