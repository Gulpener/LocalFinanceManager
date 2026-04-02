using System.Net.Http.Headers;

namespace LocalFinanceManager.Services;

/// <summary>
/// Adds the JWT Bearer token to outgoing same-origin API requests.
/// The token is read exclusively from <see cref="AuthTokenStore"/>, which is populated
/// by <see cref="CustomAuthenticationStateProvider.GetAuthenticationStateAsync"/> before
/// any child component's OnInitializedAsync runs (via CascadingAuthenticationState).
/// No JS interop is performed here to avoid circuit timing issues.
/// </summary>
public class AuthTokenHandler : DelegatingHandler
{
    private readonly AuthTokenStore _tokenStore;

    public AuthTokenHandler(AuthTokenStore tokenStore)
    {
        _tokenStore = tokenStore;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _tokenStore.AccessToken;

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
