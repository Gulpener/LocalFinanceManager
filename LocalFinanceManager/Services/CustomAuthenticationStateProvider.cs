using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace LocalFinanceManager.Services;

/// <summary>
/// Blazor authentication state provider that reads the JWT from sessionStorage.
/// The JWT is stored by the Login page via JavaScript interop after successful sign-in.
/// </summary>
public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private const string SessionStorageKey = "auth_token";

    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<CustomAuthenticationStateProvider> _logger;

    private static readonly AuthenticationState AnonymousState =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public CustomAuthenticationStateProvider(IJSRuntime jsRuntime, ILogger<CustomAuthenticationStateProvider> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", SessionStorageKey);
            if (string.IsNullOrEmpty(token))
            {
                return AnonymousState;
            }

            var principal = BuildClaimsPrincipal(token);
            if (principal == null)
            {
                return AnonymousState;
            }

            return new AuthenticationState(principal);
        }
        catch (Exception ex)
        {
            // JS interop can fail during server-side pre-rendering
            _logger.LogDebug(ex, "Could not read auth token from sessionStorage (likely during SSR pre-render)");
            return AnonymousState;
        }
    }

    /// <summary>
    /// Called after a successful login to store the JWT and notify Blazor.
    /// </summary>
    public async Task NotifyUserLoggedInAsync(string token)
    {
        await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", SessionStorageKey, token);

        var principal = BuildClaimsPrincipal(token);
        var state = principal != null
            ? new AuthenticationState(principal)
            : AnonymousState;

        NotifyAuthenticationStateChanged(Task.FromResult(state));
    }

    /// <summary>
    /// Called on logout to clear the JWT and notify Blazor.
    /// </summary>
    public async Task NotifyUserLoggedOutAsync()
    {
        await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", SessionStorageKey);
        NotifyAuthenticationStateChanged(Task.FromResult(AnonymousState));
    }

    private ClaimsPrincipal? BuildClaimsPrincipal(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
            {
                return null;
            }

            var jwtToken = handler.ReadJwtToken(token);

            // Check expiry without full validation (validation happens server-side on API calls)
            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                _logger.LogDebug("JWT token has expired");
                return null;
            }

            var claims = jwtToken.Claims.ToList();

            // Add Name claim from email for display in AuthorizeView
            var emailClaim = claims.FirstOrDefault(c => c.Type == "email");
            if (emailClaim != null)
            {
                claims.Add(new Claim(ClaimTypes.Name, emailClaim.Value));
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            return new ClaimsPrincipal(identity);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse JWT token");
            return null;
        }
    }
}
