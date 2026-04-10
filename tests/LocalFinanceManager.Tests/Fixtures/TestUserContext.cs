using LocalFinanceManager.Data;
using LocalFinanceManager.Services;

namespace LocalFinanceManager.Tests.Fixtures;

/// <summary>
/// Test implementation of IUserContext that returns a configurable user ID.
/// Used in unit and integration tests to simulate an authenticated user without HTTP context.
/// </summary>
public sealed class TestUserContext : IUserContext
{
    public static readonly Guid DefaultUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>
    /// The fixed user ID returned by this test context.
    /// Defaults to a deterministic non-empty test user ID.
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// Creates a TestUserContext with a deterministic non-empty test user ID.
    /// </summary>
    public TestUserContext() : this(DefaultUserId)
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
    public Task<bool> IsAdminAsync() => Task.FromResult(false);
}
