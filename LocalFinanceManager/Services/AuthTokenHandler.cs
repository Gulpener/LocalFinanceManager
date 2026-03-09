using System.Net.Http.Headers;
using Microsoft.JSInterop;

namespace LocalFinanceManager.Services;

/// <summary>
/// Adds the JWT from sessionStorage to outgoing same-origin API requests.
/// </summary>
public class AuthTokenHandler : DelegatingHandler
{
    private const string SessionStorageKey = "auth_token";

    private readonly IJSRuntime _jsRuntime;
    private readonly AuthTokenStore _tokenStore;
    private readonly ILogger<AuthTokenHandler> _logger;

    public AuthTokenHandler(IJSRuntime jsRuntime, AuthTokenStore tokenStore, ILogger<AuthTokenHandler> logger)
    {
        _jsRuntime = jsRuntime;
        _tokenStore = tokenStore;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _tokenStore.AccessToken;

        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                token = await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", cancellationToken, SessionStorageKey);
                if (!string.IsNullOrWhiteSpace(token))
                {
                    _tokenStore.AccessToken = token;
                }
            }

            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch (Exception ex)
        {
            // JS interop can be unavailable during prerendering or outside an interactive circuit.
            _logger.LogDebug(ex, "Could not attach auth token to outgoing API request");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
