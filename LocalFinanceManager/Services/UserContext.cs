using LocalFinanceManager.Data;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Services;

/// <summary>
/// Resolves the current authenticated user's local database ID from the JWT 'sub' claim (Supabase UUID).
/// Returns Guid.Empty when there is no authenticated HTTP context (e.g., background services).
/// Caches the resolved user ID in HttpContext.Items for the lifetime of the request.
/// </summary>
public class UserContext : IUserContext
{
    private const string CacheKey = "UserContext:UserId";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _context;
    private readonly ILogger<UserContext> _logger;

    public UserContext(IHttpContextAccessor httpContextAccessor, AppDbContext context, ILogger<UserContext> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public Guid GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return Guid.Empty;
        }

        // Check cache first to avoid repeated DB lookups per request
        if (httpContext.Items.TryGetValue(CacheKey, out var cached) && cached is Guid cachedId)
        {
            return cachedId;
        }

        var user = httpContext.User;
        if (user == null || !(user.Identity?.IsAuthenticated ?? false))
        {
            httpContext.Items[CacheKey] = Guid.Empty;
            return Guid.Empty;
        }

        var subClaim = user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(subClaim))
        {
            _logger.LogWarning("Authenticated user is missing the 'sub' claim");
            httpContext.Items[CacheKey] = Guid.Empty;
            return Guid.Empty;
        }

        // Resolve local DB user ID from Supabase UUID
        var localUser = _context.Users
            .AsNoTracking()
            .FirstOrDefault(u => u.SupabaseUserId == subClaim);

        var resolvedId = localUser?.Id ?? Guid.Empty;
        if (localUser == null)
        {
            _logger.LogWarning("No local user found for Supabase UUID {SubClaim}", subClaim);

            var nameIdentifier = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(nameIdentifier, out var fallbackUserId) && fallbackUserId != Guid.Empty)
            {
                httpContext.Items[CacheKey] = fallbackUserId;
                return fallbackUserId;
            }
        }

        httpContext.Items[CacheKey] = resolvedId;
        return resolvedId;
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
