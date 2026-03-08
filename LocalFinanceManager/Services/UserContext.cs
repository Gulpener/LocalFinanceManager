using LocalFinanceManager.Data;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Services;

/// <summary>
/// Resolves the current authenticated user's local database ID from the JWT 'sub' claim (Supabase UUID).
/// Returns Guid.Empty when there is no authenticated HTTP context (e.g., background services).
/// </summary>
public class UserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UserContext> _logger;

    public UserContext(IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider, ILogger<UserContext> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public Guid GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null || !(user.Identity?.IsAuthenticated ?? false))
        {
            return Guid.Empty;
        }

        var subClaim = user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(subClaim))
        {
            _logger.LogWarning("Authenticated user is missing the 'sub' claim");
            return Guid.Empty;
        }

        // Resolve local DB user ID from Supabase UUID
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var localUser = context.Users
            .AsNoTracking()
            .FirstOrDefault(u => u.SupabaseUserId == subClaim);

        if (localUser == null)
        {
            _logger.LogWarning("No local user found for Supabase UUID {SubClaim}", subClaim);
            return Guid.Empty;
        }

        return localUser.Id;
    }

    /// <inheritdoc />
    public string GetCurrentUserEmail()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.FindFirst("email")?.Value ?? string.Empty;
    }

    /// <inheritdoc />
    public bool IsAuthenticated()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    }
}
