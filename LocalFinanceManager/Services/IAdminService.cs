using LocalFinanceManager.DTOs;

namespace LocalFinanceManager.Services;

/// <summary>
/// Service for admin-only operations: listing users, viewing shares, and toggling admin roles.
/// </summary>
public interface IAdminService
{
    /// <summary>
    /// Returns all non-archived users with account and share counts.
    /// </summary>
    Task<List<UserSummaryResponse>> GetAllUsersAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns all active shares owned or received by the specified user.
    /// </summary>
    Task<UserSharesResponse> GetUserSharesAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Toggles the <c>IsAdmin</c> flag of <paramref name="targetUserId"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="targetUserId"/> equals <paramref name="requestingUserId"/> (self-demotion).</exception>
    /// <exception cref="KeyNotFoundException">Thrown when <paramref name="targetUserId"/> is not found.</exception>
    Task ToggleAdminAsync(Guid targetUserId, Guid requestingUserId, CancellationToken ct = default);
}
