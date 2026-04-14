using System.Globalization;
using LocalFinanceManager.Helpers;
using NUnit.Framework;

namespace LocalFinanceManager.Tests.Unit;

[TestFixture]
public class CurrencyFormatterTests
{
    [TestCase("EUR", "€")]
    [TestCase("USD", "$")]
    [TestCase("GBP", "£")]
    public void Format_KnownCurrencyCode_ContainsExpectedSymbol(string currencyCode, string expectedSymbol)
    {
        var result = CurrencyFormatter.Format(2.60m, currencyCode);

        Assert.That(result, Does.Contain(expectedSymbol),
            $"Expected '{expectedSymbol}' in formatted output for currency code '{currencyCode}', but got '{result}'.");
    }

    [Test]
    public void Format_EUR_FormatsCorrectly()
    {
        var result = CurrencyFormatter.Format(2.60m, "EUR");

        Assert.That(result, Does.Contain("2"), "Formatted value should contain the numeric part.");
        Assert.That(result, Does.Contain("€"), "Formatted value should contain the euro symbol.");
    }

    [Test]
    public void Format_NullCurrencyCode_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => CurrencyFormatter.Format(2.60m, null));
    }

    [Test]
    public void Format_EmptyCurrencyCode_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => CurrencyFormatter.Format(2.60m, ""));
    }

    [Test]
    public void Format_UnknownCurrencyCode_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => CurrencyFormatter.Format(2.60m, "XYZ"));
    }

    [Test]
    public void Format_NegativeAmount_ContainsDigits()
    {
        var result = CurrencyFormatter.Format(-15.50m, "EUR");

        Assert.That(result, Does.Contain("15"), "Formatted negative value should contain the numeric part.");
    }

    [TestCase("eur")]
    [TestCase("Eur")]
    [TestCase("EUR")]
    public void Format_CurrencyCodeCaseInsensitive_ContainsEuroSymbol(string currencyCode)
    {
        var result = CurrencyFormatter.Format(1.00m, currencyCode);

        Assert.That(result, Does.Contain("€"),
            $"Expected '€' for currency code '{currencyCode}'.");
    }

    [Test]
    public void GetCulture_EUR_ReturnsEuroSymbol()
    {
        var culture = CurrencyFormatter.GetCulture("EUR");

        Assert.That(culture.NumberFormat.CurrencySymbol, Is.EqualTo("€"));
    }

    [Test]
    public void GetCulture_UnknownCode_ReturnsCurrentCulture()
    {
        var culture = CurrencyFormatter.GetCulture("XYZ");

        Assert.That(culture, Is.EqualTo(CultureInfo.CurrentCulture));
        Assert.That(culture.NumberFormat.CurrencySymbol,
            Is.EqualTo(CultureInfo.CurrentCulture.NumberFormat.CurrencySymbol));
    }

    /// <summary>
    /// Regression test for BugReport-10: formatting EUR must render the euro symbol
    /// and must not show the generic currency placeholder.
    /// </summary>
    [Test]
    public void Format_EUR_UsesEuroSymbolInsteadOfGenericCurrencyPlaceholder()
    {
        var result = CurrencyFormatter.Format(123.45m, "EUR");

        Assert.That(result, Does.Not.Contain("¤"),
            $"EUR must never render the generic ¤ placeholder. Got: '{result}'.");
        Assert.That(result, Does.Contain("€"),
            $"EUR must always render the € symbol. Got: '{result}'.");
    }

    [TestCase("USD")]
    [TestCase("GBP")]
    [TestCase("JPY")]
    [TestCase("CHF")]
    public void Format_CommonCurrencies_NeverShowsGenericPlaceholder(string currencyCode)
    {
        var result = CurrencyFormatter.Format(100m, currencyCode);

        Assert.That(result, Does.Not.Contain("¤"),
            $"{currencyCode} must never render the generic ¤ placeholder. Got: '{result}'.");
    }

    /// <summary>
    /// True regression test for the invariant-globalization fallback path (BugReport-10).
    /// Injects a culture whose CurrencySymbol is "¤" to exercise FormatWithCulture's
    /// symbol-replacement branch directly, regardless of whether ICU data is available.
    /// </summary>
    [TestCase("EUR", "€")]
    [TestCase("USD", "$")]
    [TestCase("GBP", "£")]
    public void FormatWithCulture_WhenCultureHasGenericPlaceholder_UsesFallbackSymbol(
        string currencyCode, string expectedSymbol)
    {
        // Arrange: build a culture whose CurrencySymbol is the generic ¤ placeholder,
        // simulating what GetCulture() returns in invariant-globalization mode.
        var testCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        testCulture.NumberFormat.CurrencySymbol = "¤"; // Explicitly set to trigger the fallback

        // Act
        var result = CurrencyFormatter.FormatWithCulture(123.45m, currencyCode, testCulture);

        // Assert
        Assert.That(result, Does.Not.Contain("¤"),
            $"FormatWithCulture must replace ¤ with the known symbol for '{currencyCode}'. Got: '{result}'.");
        Assert.That(result, Does.Contain(expectedSymbol),
            $"FormatWithCulture must use '{expectedSymbol}' for '{currencyCode}'. Got: '{result}'.");
    }

    [Test]
    public void Format_CurrencyCodeWithWhitespace_StillFormatsCorrectly()
    {
        // Verifies the normalization path (Trim) in Format()
        var result = CurrencyFormatter.Format(50m, "  EUR  ");

        Assert.That(result, Does.Contain("€"),
            $"EUR with surrounding whitespace must still render €. Got: '{result}'.");
        Assert.That(result, Does.Not.Contain("¤"),
            $"EUR with surrounding whitespace must not render ¤. Got: '{result}'.");
    }

    [Test]
    public void Format_CurrencySymbolInput_StillFormatsCorrectly()
    {
        var result = CurrencyFormatter.Format(50m, "€");

        Assert.That(result, Does.Contain("€"),
            $"Currency symbol input must render €. Got: '{result}'.");
        Assert.That(result, Does.Not.Contain("¤"),
            $"Currency symbol input must not render ¤. Got: '{result}'.");
    }

    [Test]
    public void FormatWithCulture_WhenCultureSymbolDoesNotMatchKnownCurrency_UsesFallbackSymbol()
    {
        var testCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        testCulture.NumberFormat.CurrencySymbol = "EUR";

        var result = CurrencyFormatter.FormatWithCulture(123.45m, "EUR", testCulture);

        Assert.That(result, Does.Contain("€"),
            $"Expected EUR to render €. Got: '{result}'.");
        Assert.That(result, Does.Not.Contain("¤"),
            $"Expected EUR to never render ¤. Got: '{result}'.");
    }
}
