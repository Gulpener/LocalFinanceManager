namespace LocalFinanceManager.Services;

/// <summary>
/// Result of an authentication operation.
/// </summary>
public class AuthResponse
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public string? ErrorMessage { get; set; }
    /// <summary>
    /// Whether the user's email is verified. Null when email verification status is unknown.
    /// </summary>
    public bool? EmailVerified { get; set; }
}

/// <summary>
/// Provides authentication operations via Supabase Auth.
/// </summary>
public interface IAuthService
{
    Task<AuthResponse> SignUpAsync(string email, string password);
    Task<AuthResponse> SignInAsync(string email, string password);
    Task SignOutAsync();
    Task<AuthResponse> SendPasswordResetEmailAsync(string email);
    Task<AuthResponse> ResendVerificationEmailAsync(string email);
}
