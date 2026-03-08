namespace LocalFinanceManager.Configuration;

/// <summary>
/// Configuration options for Supabase integration (Auth + JWT validation).
/// </summary>
public class SupabaseOptions
{
    public string Url { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string JwtSecret { get; set; } = string.Empty;
}
