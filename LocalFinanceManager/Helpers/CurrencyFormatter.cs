using System.Globalization;

namespace LocalFinanceManager.Helpers;

/// <summary>
/// Formats monetary amounts using the symbol that corresponds to an ISO-4217 currency code.
/// </summary>
public static class CurrencyFormatter
{
    private static readonly Dictionary<string, CultureInfo> _cultureCache = BuildCurrencyMap();

    /// <summary>
    /// Hardcoded symbol fallbacks for common ISO-4217 currency codes.
    /// Used when <see cref="CultureInfo.GetCultures"/> returns an empty set (e.g. invariant
    /// globalization mode on Linux) so that well-known currencies always render their correct
    /// symbol instead of the generic <c>¤</c> placeholder.
    /// </summary>
    private static readonly Dictionary<string, string> _knownSymbols = new(StringComparer.OrdinalIgnoreCase)
    {
        ["EUR"] = "€",
        ["USD"] = "$",
        ["GBP"] = "£",
        ["JPY"] = "¥",
        ["CHF"] = "Fr",
        ["CAD"] = "CA$",
        ["AUD"] = "A$",
        ["SEK"] = "kr",
        ["NOK"] = "kr",
        ["DKK"] = "kr.",
        ["PLN"] = "zł",
        ["CZK"] = "Kč",
        ["HUF"] = "Ft",
        ["RON"] = "lei",
        ["BGN"] = "лв.",
        ["HRK"] = "kn",
        ["NZD"] = "NZ$",
        ["SGD"] = "S$",
        ["HKD"] = "HK$",
        ["MXN"] = "MX$",
        ["BRL"] = "R$",
        ["ZAR"] = "R",
        ["INR"] = "₹",
        ["CNY"] = "¥",
        ["KRW"] = "₩",
        ["TRY"] = "₺",
    };

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

        // If the resolved culture still uses the generic placeholder (e.g. in invariant
        // globalization mode) and we have a hardcoded symbol, build a custom NumberFormatInfo
        // so the correct symbol is always displayed.
        if (culture.NumberFormat.CurrencySymbol == "¤" &&
            _knownSymbols.TryGetValue(currencyCode, out var knownSymbol))
        {
            var nfi = (NumberFormatInfo)culture.NumberFormat.Clone();
            nfi.CurrencySymbol = knownSymbol;
            return amount.ToString("C2", nfi);
        }

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

        foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                     .OrderBy(c => c.Name, StringComparer.Ordinal))
        {
            try
            {
                var region = new RegionInfo(culture.Name);
                var code = region.ISOCurrencySymbol;

                // Keep the first match per currency code (ordered by culture Name for determinism)
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
