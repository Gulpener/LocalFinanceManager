namespace LocalFinanceManager.Configuration;

/// <summary>
/// Configuration options for Supabase integration (Auth + JWT validation + Storage).
/// </summary>
public class SupabaseOptions
{
    public string Url { get; set; } = string.Empty;
    public string AnonKey { get; set; } = string.Empty;

    [Obsolete("Use AnonKey instead. This alias exists for backward-compatible configuration binding.")]
    public string Key
    {
        get => AnonKey;
        set => AnonKey = value;
    }
    public string JwtSecret { get; set; } = string.Empty;

    /// <summary>Name of the Supabase Storage bucket for profile pictures.</summary>
    public string StorageBucket { get; set; } = "profile-pictures";
}
