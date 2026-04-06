using LocalFinanceManager.Data;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

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
    private readonly IBlazorCircuitUser _circuitUser;
    private readonly AuthTokenStore _tokenStore;
    private readonly AppDbContext _context;
    private readonly ILogger<UserContext> _logger;

    public UserContext(IHttpContextAccessor httpContextAccessor, IBlazorCircuitUser circuitUser, AuthTokenStore tokenStore, AppDbContext context, ILogger<UserContext> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _circuitUser = circuitUser;
        _tokenStore = tokenStore;
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public Guid GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        // In Blazor Server the HttpContext belongs to the SignalR hub connection and its
        // User is anonymous (auth is done via sessionStorage JWT, not HTTP headers).
        // Fall back to the circuit-scoped user that Routes.razor populates at circuit start.
        var user = httpContext?.User;
        if (user == null || !(user.Identity?.IsAuthenticated ?? false))
        {
            if (_circuitUser.IsInitialized)
                return _circuitUser.UserId;

            // Circuit user not yet initialized (race condition: login navigation fired before
            // OnAuthenticationStateChanged DB lookup completed). Resolve from the token store
            // so write operations don't silently persist Guid.Empty as the owner.
            return ResolveUserIdFromTokenStore();
        }

        // API/HTTP path: check cache first to avoid repeated DB lookups per request
        if (httpContext!.Items.TryGetValue(CacheKey, out var cached) && cached is Guid cachedId)
        {
            return cachedId;
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
        if (user != null && (user.Identity?.IsAuthenticated ?? false))
            return user.FindFirst("email")?.Value ?? string.Empty;
        return _circuitUser.Email;
    }

    /// <inheritdoc />
    public bool IsAuthenticated()
    {
        if (_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false)
            return true;
        return _circuitUser.IsInitialized && _circuitUser.UserId != Guid.Empty;
    }

    /// <summary>
    /// Fallback path for when the circuit user hasn't been initialized yet.
    /// Parses the stored JWT, resolves the local user from the DB, and eagerly
    /// initializes the circuit user to avoid repeated DB lookups.
    /// </summary>
    private Guid ResolveUserIdFromTokenStore()
    {
        var token = _tokenStore.AccessToken;
        if (string.IsNullOrEmpty(token))
            return Guid.Empty;

        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(token))
            return Guid.Empty;

        var jwtToken = handler.ReadJwtToken(token);
        var sub = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        if (string.IsNullOrEmpty(sub))
            return Guid.Empty;

        var localUser = _context.Users
            .AsNoTracking()
            .Where(u => u.SupabaseUserId == sub)
            .Select(u => new { u.Id, u.Email })
            .FirstOrDefault();

        if (localUser == null)
        {
            _logger.LogWarning("Token store fallback: no local user found for Supabase UUID {Sub}", sub);
            return Guid.Empty;
        }

        var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? localUser.Email;
        _circuitUser.Initialize(localUser.Id, email);

        _logger.LogDebug("Token store fallback resolved user {UserId}; circuit user eagerly initialized", localUser.Id);
        return localUser.Id;
    }
}
