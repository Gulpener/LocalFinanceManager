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
        var cut = RenderNavMenu(context, isAuthenticated: false);

        var navLinks = cut.FindAll("a.nav-link");

        Assert.Multiple(() =>
        {
            Assert.That(navLinks.Count, Is.EqualTo(1));
            Assert.That(navLinks[0].TextContent, Does.Contain("Home"));
            Assert.That(cut.Markup, Does.Not.Contain("Rekeningen"));
            Assert.That(cut.Markup, Does.Not.Contain("Transacties"));
            Assert.That(cut.Markup, Does.Not.Contain("Budgetplannen"));
            Assert.That(cut.Markup, Does.Not.Contain("Beheerinstellingen"));
            Assert.That(cut.Markup, Does.Not.Contain("Automatisch toewijzen"));
            Assert.That(cut.Markup, Does.Not.Contain("Bewaking"));
        });
    }

    [Test]
    public void NavMenu_WhenUserIsLoggedIn_ShowsAuthenticatedMenuItems()
    {
        using var context = new BunitContext();
        var cut = RenderNavMenu(context, isAuthenticated: true);

        Assert.Multiple(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Home"));
            Assert.That(cut.Markup, Does.Contain("Rekeningen"));
            Assert.That(cut.Markup, Does.Contain("Transacties"));
            Assert.That(cut.Markup, Does.Contain("Budgetplannen"));
            Assert.That(cut.Markup, Does.Contain("Beheerinstellingen"));
            Assert.That(cut.Markup, Does.Contain("Automatisch toewijzen"));
            Assert.That(cut.Markup, Does.Contain("Bewaking"));
        });
    }

    private static IRenderedComponent<CascadingAuthenticationState> RenderNavMenu(BunitContext context, bool isAuthenticated)
    {
        context.Services.AddAuthorizationCore();
        context.Services.AddSingleton<IAuthorizationService>(new TestAuthorizationService(isAuthenticated));
        context.Services.AddScoped<AuthenticationStateProvider>(_ => new TestAuthenticationStateProvider(isAuthenticated));

        var userContextMock = new Mock<IUserContext>();
        userContextMock.Setup(x => x.GetCurrentUserId()).Returns(Guid.Empty);
        userContextMock.Setup(x => x.IsAuthenticated()).Returns(isAuthenticated);
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