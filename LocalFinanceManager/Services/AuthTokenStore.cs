namespace LocalFinanceManager.Services;

/// <summary>
/// Scoped in-memory store for the current circuit's auth token.
/// </summary>
public class AuthTokenStore
{
    public string? AccessToken { get; set; }
}
