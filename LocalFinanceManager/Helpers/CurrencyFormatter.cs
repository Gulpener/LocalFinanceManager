using System.Globalization;

namespace LocalFinanceManager.Helpers;

/// <summary>
/// Formats monetary amounts using the symbol that corresponds to an ISO-4217 currency code.
/// </summary>
public static class CurrencyFormatter
{
    private static readonly Dictionary<string, CultureInfo> _cultureCache = BuildCurrencyMap();

    /// <summary>
    /// Formats <paramref name="amount"/> with the currency symbol that matches
    /// <paramref name="currencyCode"/> (ISO-4217, e.g. "EUR", "USD").
    /// Falls back to <see cref="CultureInfo.CurrentCulture"/> formatting when the code is
    /// null, empty, or not recognised.
    /// </summary>
    public static string Format(decimal amount, string? currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            return amount.ToString("C2", CultureInfo.CurrentCulture);
        }

        var culture = GetCulture(currencyCode);
        return amount.ToString("C2", culture);
    }

    /// <summary>
    /// Returns the <see cref="CultureInfo"/> whose currency symbol matches the supplied
    /// ISO-4217 currency code, or <see cref="CultureInfo.CurrentCulture"/> as a fallback.
    /// </summary>
    public static CultureInfo GetCulture(string currencyCode)
    {
        if (_cultureCache.TryGetValue(currencyCode.ToUpperInvariant(), out var culture))
        {
            return culture;
        }

        return CultureInfo.CurrentCulture;
    }

    private static Dictionary<string, CultureInfo> BuildCurrencyMap()
    {
        var map = new Dictionary<string, CultureInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
        {
            try
            {
                var region = new RegionInfo(culture.Name);
                var code = region.ISOCurrencySymbol;

                // Keep the first match per currency code (deterministic ordering)
                if (!map.ContainsKey(code))
                {
                    map[code] = culture;
                }
            }
            catch (ArgumentException)
            {
                // Some culture names are not valid for RegionInfo – skip them.
            }
        }

        return map;
    }
}
