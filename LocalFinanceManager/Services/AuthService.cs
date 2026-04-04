using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LocalFinanceManager.Configuration;
using LocalFinanceManager.Data;
using LocalFinanceManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LocalFinanceManager.Services;

/// <summary>
/// Authentication service that calls the Supabase Auth REST API.
/// Syncs the Supabase user with the local database User entity on successful login/registration.
/// </summary>
public class AuthService : IAuthService
{
    private readonly SupabaseOptions _options;
    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWebHostEnvironment _environment;
    private readonly IDevelopmentUserSeedService _developmentUserSeedService;
    private readonly ILogger<AuthService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AuthService(
        IOptions<SupabaseOptions> options,
        AppDbContext context,
        IHttpClientFactory httpClientFactory,
        IWebHostEnvironment environment,
        IDevelopmentUserSeedService developmentUserSeedService,
        ILogger<AuthService> logger)
    {
        _options = options.Value;
        _context = context;
        _httpClientFactory = httpClientFactory;
        _environment = environment;
        _developmentUserSeedService = developmentUserSeedService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AuthResponse> SignUpAsync(string email, string password)
    {
        try
        {
            var body = JsonSerializer.Serialize(new { email, password });
            var response = await PostAsync("/auth/v1/signup", body);

            if (!response.IsSuccessStatusCode)
            {
                var error = await ReadErrorAsync(response);
                return new AuthResponse { Success = false, ErrorMessage = error };
            }

            _logger.LogInformation("User registered.");
            return new AuthResponse { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sign-up failed.");
            return new AuthResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <inheritdoc />
    public async Task<AuthResponse> SignInAsync(string email, string password)
    {
        try
        {
            var body = JsonSerializer.Serialize(new { email, password });
            var response = await PostAsync("/auth/v1/token?grant_type=password", body);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var error = await ReadErrorAsync(response, content);

                if (content.Contains("Email not confirmed", StringComparison.OrdinalIgnoreCase) ||
                    content.Contains("email_not_confirmed", StringComparison.OrdinalIgnoreCase))
                {
                    return new AuthResponse
                    {
                        Success = false,
                        ErrorMessage = "Email not verified.",
                        EmailVerified = false
                    };
                }

                return new AuthResponse { Success = false, ErrorMessage = error };
            }

            var session = JsonSerializer.Deserialize<SupabaseSession>(content, JsonOptions);
            if (session?.AccessToken == null || session.User == null)
            {
                return new AuthResponse { Success = false, ErrorMessage = "Login failed." };
            }

            var emailVerified = session.User.EmailConfirmedAt.HasValue;
            if (!emailVerified)
            {
                return new AuthResponse
                {
                    Success = false,
                    ErrorMessage = "Email not verified.",
                    EmailVerified = false
                };
            }

            await SyncUserAsync(session.User, email);

            return new AuthResponse
            {
                Success = true,
                AccessToken = session.AccessToken,
                EmailVerified = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sign-in failed.");
            return new AuthResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <inheritdoc />
    public async Task SignOutAsync()
    {
        await Task.CompletedTask;
        _logger.LogDebug("Sign-out called (JWT cleared client-side)");
    }

    /// <inheritdoc />
    public async Task<AuthResponse> SendPasswordResetEmailAsync(string email)
    {
        try
        {
            var body = JsonSerializer.Serialize(new { email });
            var response = await PostAsync("/auth/v1/recover", body);
            if (!response.IsSuccessStatusCode)
            {
                var error = await ReadErrorAsync(response);
                _logger.LogWarning("Password reset request failed: {Error}", error);
                return new AuthResponse { Success = false, ErrorMessage = ToUserFriendlyError(error, response.StatusCode) };
            }

            _logger.LogInformation("Password reset email requested.");
            return new AuthResponse { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Password reset request failed.");
            return new AuthResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <inheritdoc />
    public async Task<AuthResponse> ResendVerificationEmailAsync(string email)
    {
        try
        {
            var body = JsonSerializer.Serialize(new { type = "signup", email });
            var response = await PostAsync("/auth/v1/resend", body);
            if (!response.IsSuccessStatusCode)
            {
                var error = await ReadErrorAsync(response);
                _logger.LogWarning("Resend verification failed: {Error}", error);
                return new AuthResponse { Success = false, ErrorMessage = ToUserFriendlyError(error, response.StatusCode) };
            }

            _logger.LogInformation("Verification email resent.");
            return new AuthResponse { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Resend verification failed.");
            return new AuthResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    private static string ToUserFriendlyError(string rawError, System.Net.HttpStatusCode statusCode)
    {
        if (statusCode == System.Net.HttpStatusCode.TooManyRequests ||
            rawError.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
            rawError.Contains("over_email_send_rate_limit", StringComparison.OrdinalIgnoreCase))
        {
            return "Too many emails sent recently. Please wait a few minutes before trying again.";
        }

        return rawError;
    }

    private async Task<HttpResponseMessage> PostAsync(string path, string jsonBody)
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_options.Url);
        client.DefaultRequestHeaders.Add("apikey", _options.Key);

        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        return await client.PostAsync(path, content);
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, string? content = null)
    {
        content ??= await response.Content.ReadAsStringAsync();
        try
        {
            var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("msg", out var msg))
                return msg.GetString() ?? "Unknown error";
            if (doc.RootElement.TryGetProperty("message", out var message))
                return message.GetString() ?? "Unknown error";
            if (doc.RootElement.TryGetProperty("error_description", out var desc))
                return desc.GetString() ?? "Unknown error";
        }
        catch
        {
        }

        return $"Request failed ({response.StatusCode})";
    }

    private async Task SyncUserAsync(SupabaseSessionUser supabaseUser, string email)
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.SupabaseUserId == supabaseUser.Id);

        if (existingUser == null)
        {
            var newUser = new User
            {
                SupabaseUserId = supabaseUser.Id ?? string.Empty,
                Email = email,
                DisplayName = email.Split('@')[0],
                EmailConfirmed = supabaseUser.EmailConfirmedAt.HasValue
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            if (_environment.IsDevelopment())
            {
                await _developmentUserSeedService.SeedForUserAsync(newUser.Id);
            }

            _logger.LogInformation("Created local user record.");
        }
        else
        {
            existingUser.EmailConfirmed = supabaseUser.EmailConfirmedAt.HasValue;
            existingUser.Email = email;

            await _context.SaveChangesAsync();
        }
    }

    private sealed class SupabaseSession
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("user")]
        public SupabaseSessionUser? User { get; set; }
    }

    private sealed class SupabaseSessionUser
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("email_confirmed_at")]
        public DateTime? EmailConfirmedAt { get; set; }
    }
}
