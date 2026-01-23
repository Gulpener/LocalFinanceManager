using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace LocalFinanceManager.Services;

/// <summary>
/// Thread-safe cache key tracker using ConcurrentDictionary.
/// Enables pattern-based cache invalidation.
/// </summary>
public class CacheKeyTracker : ICacheKeyTracker
{
    private readonly ConcurrentDictionary<string, byte> _keys = new();

    /// <inheritdoc />
    public void AddKey(string key) => _keys.TryAdd(key, 0);

    /// <inheritdoc />
    public void RemoveKey(string key) => _keys.TryRemove(key, out _);

    /// <inheritdoc />
    public IEnumerable<string> GetKeysMatchingPattern(string pattern)
    {
        // Convert wildcard pattern to regex: "BudgetPlanValidation:*:category-id" -> "^BudgetPlanValidation:.*:category-id$"
        var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        var regex = new Regex(regexPattern, RegexOptions.Compiled);

        return _keys.Keys.Where(k => regex.IsMatch(k));
    }

    /// <inheritdoc />
    public void Clear() => _keys.Clear();
}
