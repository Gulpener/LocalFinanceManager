using Microsoft.AspNetCore.Authorization;

namespace LocalFinanceManager.Services.Authorization;

/// <summary>
/// Authorization requirement that demands the current user has <c>IsAdmin = true</c> in the database.
/// </summary>
public class IsAdminRequirement : IAuthorizationRequirement { }
