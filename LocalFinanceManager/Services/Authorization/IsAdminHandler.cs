using LocalFinanceManager.Services;
using Microsoft.AspNetCore.Authorization;

namespace LocalFinanceManager.Services.Authorization;

/// <summary>
/// Handles the <see cref="IsAdminRequirement"/> by checking whether the current authenticated
/// user has <c>IsAdmin = true</c> in the database.
/// Calling <c>context.Fail()</c> explicitly ensures the framework returns HTTP 403 (not 401)
/// when the requirement is not met for an authenticated user.
/// </summary>
public class IsAdminHandler : AuthorizationHandler<IsAdminRequirement>
{
    private readonly IUserContext _userContext;

    public IsAdminHandler(IUserContext userContext)
    {
        _userContext = userContext;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        IsAdminRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            // Let the default 401 challenge flow handle unauthenticated users.
            return;
        }

        var isAdmin = await _userContext.IsAdminAsync();
        if (isAdmin)
            context.Succeed(requirement);
        else
            context.Fail();
    }
}
