namespace LocalFinanceManager.Services;

/// <summary>
/// Abstraction over the Blazor authentication state provider for notifying state changes
/// after login and logout. Injecting this interface instead of the concrete
/// <see cref="CustomAuthenticationStateProvider"/> keeps Razor pages decoupled from the
/// implementation (e.g. the E2E test substitute).
/// </summary>
public interface IAuthStateNotifier
{
    /// <summary>
    /// Stores the JWT and notifies Blazor that the authentication state has changed to authenticated.
    /// </summary>
    Task NotifyUserLoggedInAsync(string token);

    /// <summary>
    /// Clears the JWT and notifies Blazor that the authentication state has changed to anonymous.
    /// </summary>
    Task NotifyUserLoggedOutAsync();
}
