using LocalFinanceManager.Data;
using LocalFinanceManager.Services;

namespace LocalFinanceManager.Tests.Fixtures;

/// <summary>
/// Test implementation of IUserContext that returns a configurable user ID.
/// Used in unit and integration tests to simulate an authenticated user without HTTP context.
/// </summary>
public sealed class TestUserContext : IUserContext
{
    /// <summary>
    /// The fixed user ID returned by this test context.
    /// Defaults to Guid.Empty (no user filtering) for isolated unit/integration tests.
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// Creates a TestUserContext with Guid.Empty (bypass user filtering).
    /// </summary>
    public TestUserContext() : this(Guid.Empty)
    {
    }

    /// <summary>
    /// Creates a TestUserContext with a specific user ID.
    /// </summary>
    public TestUserContext(Guid userId)
    {
        UserId = userId;
    }

    public Guid GetCurrentUserId() => UserId;
    public string GetCurrentUserEmail() => "test@localfinancemanager.local";
    public bool IsAuthenticated() => UserId != Guid.Empty;
}
