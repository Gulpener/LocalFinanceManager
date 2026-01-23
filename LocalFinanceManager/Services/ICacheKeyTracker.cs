namespace LocalFinanceManager.Services;

/// <summary>
/// Tracks cache keys to enable pattern-based removal.
/// IMemoryCache doesn't support native pattern matching, so we track keys externally.
/// </summary>
public interface ICacheKeyTracker
{
    /// <summary>
    /// Adds a cache key to the tracker.
    /// </summary>
    void AddKey(string key);

    /// <summary>
    /// Removes a cache key from the tracker.
    /// </summary>
    void RemoveKey(string key);

    /// <summary>
    /// Gets all keys matching a wildcard pattern.
    /// Pattern uses * as wildcard (e.g., "BudgetPlanValidation:account-id:*").
    /// </summary>
    IEnumerable<string> GetKeysMatchingPattern(string pattern);

    /// <summary>
    /// Clears all tracked keys.
    /// </summary>
    void Clear();
}
