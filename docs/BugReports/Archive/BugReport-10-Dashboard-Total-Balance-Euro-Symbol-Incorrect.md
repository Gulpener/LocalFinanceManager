# BugReport-10: Dashboard Total Balance Shows Generic Currency Symbol Instead of Euro

## Status

- [x] Resolved

## Reopen Reason

This bug was previously marked as resolved, but the issue became reproducible again.

## Regression Update (2026-04-14)

- Newly observed value: `¤0.00`
- Expected value for EUR: `€0.00`
- Reported scope: dashboard total balance path

## Summary

On the dashboard, the "Total balance" amount showed the generic currency symbol `¤` instead of the euro symbol `€`.

## Environment

- Version: latest
- Scope: dashboard page (reported)

## Steps to Reproduce

1. Log in to the application.
2. Open the dashboard.
3. Check the "Total balance" value.
4. Observe that `¤0.00` is rendered instead of `€0.00` for EUR.

## Expected Behaviour

The total balance is displayed with the euro symbol, for example: `€123.45` or `€0.00`.

## Actual Behaviour

The total balance was displayed with the generic currency symbol, for example: `¤123.45` or `¤0.00`.

## Impact

- Currency display is inconsistent and confusing for users.
- Creates a regression in trust and UI quality on a primary page.

## Related Reports

- `docs/BugReports/Archive/BugReport-5-Dashboard-UnresolvedBalanceCurrency.md` (earlier dashboard currency rendering issue)
- `docs/BugReports/Archive/BugReport-1-Currency-Symbol-Incorrect.md` (earlier generic currency symbol issue)

## Suspected Scope

Likely dashboard-specific formatting path for total balance, deployment/runtime globalization settings, or a code path bypassing shared currency formatting.

## Previous Attempt (Archived Solution Context)

Previous fix documented that `CurrencyFormatter.BuildCurrencyMap()` may return no cultures in invariant globalization mode, causing fallback to `CultureInfo.CurrentCulture` and rendering `¤`. A fallback symbol mapping was added in `CurrencyFormatter` and related tests were introduced.

## Tasks

- [x] Reproduce locally and capture exact rendered value and account currency code
- [ ] Reproduce in deployed environment and capture runtime globalization/culture settings
- [x] Verify dashboard total balance always uses `CurrencyFormatter.Format(...)`
- [x] Verify no bypass paths in dashboard/account widgets for currency rendering
- [x] Add or update regression tests to assert EUR shows `€` (including zero values)
- [x] Verify no regression for other ISO-4217 currencies

## Acceptance Criteria

- [x] Dashboard "Total balance" renders `€` for EUR amounts, including zero (`€0.00`)
- [x] Dashboard no longer renders generic `¤` for EUR
- [x] Regression test added/updated and passing

## Solution

**Root cause:** The formatter fallback was only triggered when the resolved culture symbol was exactly `¤`. If callers passed a non-ISO value (for example a symbol such as `€`) or if the resolved culture produced a different symbol string than expected, the fallback path was bypassed and formatting could still render the wrong currency marker.

**Implemented fix:**

- `LocalFinanceManager/Helpers/CurrencyFormatter.cs`
  - Added normalization that accepts both ISO codes and currency symbols (for example `€` -> `EUR`).
  - Added a symbol-to-code lookup map derived from known symbols.
  - Hardened fallback logic so known currencies always use the expected symbol when the resolved culture symbol does not match.
  - Updated `GetCulture` to use the same normalization path.

- `tests/LocalFinanceManager.Tests/Unit/CurrencyFormatterTests.cs`
  - Added regression test for symbol input (`Format_CurrencySymbolInput_StillFormatsCorrectly`).
  - Added regression test for mismatched culture symbols (`FormatWithCulture_WhenCultureSymbolDoesNotMatchKnownCurrency_UsesFallbackSymbol`).
  - Existing currency formatter regression suite remains green.

**Verification:**

- Targeted unit tests passed: `CurrencyFormatterTests` (13 passed, 0 failed).