# BugReport-10: Dashboard Total Balance Shows Generic Currency Symbol Instead of Euro

## Status

- [x] Resolved

## Summary

On the dashboard, the "Total balance" amount shows the generic currency symbol `¤` instead of the euro symbol `€`.

## Environment

- Version: latest
- Scope: only observed on the dashboard page

## Steps to Reproduce

1. Log in to the application.
2. Open the dashboard.
3. Check the "Total balance" value.

## Expected Behaviour

The total balance is displayed with the euro symbol, for example: `€123.45`.

## Actual Behaviour

The total balance is displayed with the generic currency symbol, for example: `¤123.45`.

## Impact

- Currency display is inconsistent and confusing for users.
- Creates a regression in trust and UI quality on a primary page.

## Related Reports

- `docs/BugReports/Archive/BugReport-5-Dashboard-UnresolvedBalanceCurrency.md` (earlier dashboard currency rendering issue)
- `docs/BugReports/Archive/BugReport-1-Currency-Symbol-Incorrect.md` (earlier generic currency symbol issue)

## Suspected Scope

Likely dashboard-specific formatting path for total balance that bypasses shared currency formatting or applies an incorrect culture/currency mapping.

## Tasks

- [x] Reproduce locally and capture exact rendered value and currency code in component state
- [x] Verify which formatter/path is used by the dashboard total balance widget
- [x] Ensure EUR is formatted through the shared currency formatter/culture mapping
- [x] Add or update regression test that validates `€` rendering for dashboard total balance
- [x] Verify no regression for other ISO-4217 currencies

## Acceptance Criteria

- [x] Dashboard "Total balance" renders `€` for EUR amounts
- [x] Dashboard no longer renders generic `¤` for EUR
- [x] Regression test added/updated and passing

## Solution

**Root cause:** `CurrencyFormatter.BuildCurrencyMap()` relies on `CultureInfo.GetCultures(CultureTypes.SpecificCultures)` to build its ISO-4217 → `CultureInfo` lookup. On Linux deployments running with invariant globalization mode (or without ICU data), `GetCultures()` returns an empty collection. This caused `GetCulture("EUR")` to fall back to `CultureInfo.CurrentCulture`, which is the invariant culture with the generic `¤` placeholder as its currency symbol. Because `TotalBalanceKpiWidget` is the only dashboard widget that renders a full `CurrencyFormatter.Format(...)` call with the currency symbol, the `¤` was observed exclusively on the "Total balance" card.

**Files changed:**

- `LocalFinanceManager/Helpers/CurrencyFormatter.cs`  
  Added a `_knownSymbols` dictionary that maps common ISO-4217 codes (EUR, USD, GBP, JPY, etc.) to their symbols. `Format()` now checks whether the resolved `CultureInfo` still carries the generic `¤` symbol and, if so, creates a cloned `NumberFormatInfo` with the correct hardcoded symbol. This ensures EUR (and other common currencies) always render the correct symbol regardless of the server's globalization configuration.

- `tests/LocalFinanceManager.Tests/Unit/CurrencyFormatterTests.cs`  
  Added `Format_EUR_NeverShowsGenericCurrencyPlaceholder` (regression test for this exact bug) and `Format_CommonCurrencies_NeverShowsGenericPlaceholder` (parameterised test for USD, GBP, JPY, CHF). Both assert that the `¤` placeholder is never produced. Tests were added and are passing.
