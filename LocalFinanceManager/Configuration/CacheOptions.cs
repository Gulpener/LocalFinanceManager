namespace LocalFinanceManager.Configuration;

/// <summary>
/// Configuration options for caching behavior.
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// Absolute expiration time in minutes (how long cache entry lives from creation).
    /// Default: 5 minutes.
    /// </summary>
    public int AbsoluteExpirationMinutes { get; set; } = 5;

    /// <summary>
    /// Sliding expiration time in minutes (resets on access).
    /// Default: 2 minutes.
    /// </summary>
    public int SlidingExpirationMinutes { get; set; } = 2;

    /// <summary>
    /// Maximum number of cache entries before eviction.
    /// Default: 1000 entries.
    /// </summary>
    public int SizeLimit { get; set; } = 1000;
}
