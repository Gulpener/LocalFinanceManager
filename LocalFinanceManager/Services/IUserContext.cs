namespace LocalFinanceManager.Services;

/// <summary>
/// Provides access to the current authenticated user's identity.
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// Gets the current user's ID (from the 'sub' JWT claim mapped to the local User.Id).
    /// Returns Guid.Empty when the user is not authenticated (e.g., background services).
    /// </summary>
    Guid GetCurrentUserId();

    /// <summary>
    /// Gets the current user's email address.
    /// </summary>
    string GetCurrentUserEmail();

    /// <summary>
    /// Indicates whether the current request is authenticated.
    /// </summary>
    bool IsAuthenticated();
}
