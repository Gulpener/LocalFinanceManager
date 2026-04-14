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
    /// Pre-built <see cref="NumberFormatInfo"/> instances for each entry in <see cref="_knownSymbols"/>,
    /// so the fallback path in <see cref="Format(decimal,string?)"/> never allocates a new clone
    /// on every call.
    /// </summary>
    private static readonly Dictionary<string, NumberFormatInfo> _fallbackFormats =
        BuildFallbackFormats();

    private static readonly Dictionary<string, string> _symbolToCode = BuildSymbolToCodeMap();

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

        // Normalize once so lookups are stable even when callers pass whitespace,
        // mixed casing, or a currency symbol (e.g. "€") instead of ISO code ("EUR").
        var normalizedCode = NormalizeCurrencyCode(currencyCode);
        var culture = GetCulture(normalizedCode);
        return FormatWithCulture(amount, normalizedCode, culture);
    }

    /// <summary>
    /// Formats <paramref name="amount"/> using the supplied <paramref name="culture"/>, falling
    /// back to a hardcoded symbol from <see cref="_knownSymbols"/> when <paramref name="culture"/>
    /// still reports the generic <c>¤</c> placeholder (e.g. in invariant-globalization mode).
    /// </summary>
    /// <param name="amount">The monetary value to format.</param>
    /// <param name="normalizedCode">Upper-cased, trimmed ISO-4217 currency code.</param>
    /// <param name="culture">Resolved <see cref="CultureInfo"/> to use for formatting.</param>
    /// <remarks>
    /// Exposed as <c>internal</c> so that unit tests can inject a culture with <c>¤</c> to
    /// exercise the fallback branch directly.
    /// </remarks>
    internal static string FormatWithCulture(decimal amount, string normalizedCode, CultureInfo culture)
    {
        // If we know the intended symbol for this code, prefer that symbol whenever the
        // resolved culture does not match (including invariant-globalization "¤" cases).
        if (_fallbackFormats.TryGetValue(normalizedCode, out var fallbackNfi) &&
            !string.Equals(culture.NumberFormat.CurrencySymbol, fallbackNfi.CurrencySymbol, StringComparison.Ordinal))
        {
            return amount.ToString("C2", fallbackNfi);
        }

        return amount.ToString("C2", culture);
    }

    /// <summary>
    /// Returns the <see cref="CultureInfo"/> whose currency symbol matches the supplied
    /// ISO-4217 currency code, or <see cref="CultureInfo.CurrentCulture"/> as a fallback.
    /// </summary>
    public static CultureInfo GetCulture(string currencyCode)
    {
        var normalizedCode = NormalizeCurrencyCode(currencyCode);
        if (_cultureCache.TryGetValue(normalizedCode, out var culture))
        {
            return culture;
        }

        return CultureInfo.CurrentCulture;
    }

    private static Dictionary<string, NumberFormatInfo> BuildFallbackFormats()
    {
        // Base all fallback formats on the invariant culture's number format, overriding only
        // the currency symbol. The invariant format uses "." for decimals and "," for grouping,
        // which is a safe default when no locale information is available at all.
        var baseNfi = CultureInfo.InvariantCulture.NumberFormat;
        var result = new Dictionary<string, NumberFormatInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var (code, symbol) in _knownSymbols)
        {
            var nfi = (NumberFormatInfo)baseNfi.Clone();
            nfi.CurrencySymbol = symbol;
            result[code] = nfi;
        }
        return result;
    }

    private static Dictionary<string, string> BuildSymbolToCodeMap()
    {
        // Count how many ISO codes share each symbol.
        var symbolCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (_, symbol) in _knownSymbols)
        {
            symbolCounts[symbol] = symbolCounts.GetValueOrDefault(symbol) + 1;
        }

        // Only map symbols that are unambiguous (used by exactly one currency code).
        // Ambiguous symbols like "¥" (JPY/CNY) and "kr" (SEK/NOK) are intentionally excluded
        // to prevent silently normalizing to the wrong ISO code.
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (code, symbol) in _knownSymbols)
        {
            if (symbolCounts[symbol] == 1)
            {
                result[symbol] = code;
            }
        }

        return result;
    }

    private static string NormalizeCurrencyCode(string currencyCode)
    {
        var trimmed = currencyCode.Trim();
        if (_symbolToCode.TryGetValue(trimmed, out var codeFromSymbol))
        {
            return codeFromSymbol;
        }

        return trimmed.ToUpperInvariant();
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
