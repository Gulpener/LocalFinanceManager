# BugReport-5: Dashboard Shows Unresolved Balance Currency Placeholder

## Status

- [x] Resolved

## Summary

The dashboard displays `0.00_balanceCurrency` instead of a properly formatted balance with the correct currency symbol (e.g. `€0.00`).

## Steps to Reproduce

1. Log in and navigate to the dashboard.
2. Observe the "Total balance" widget.

## Expected Behaviour

The total balance is displayed as a formatted amount with the correct currency symbol, e.g.:

```
Total balance
€0.00
1 rekening(en)
```

## Actual Behaviour

The total balance is displayed as:

```
Total balance
0.00_balanceCurrency
1 rekening(en)
```

The `_balanceCurrency` suffix indicates an unresolved string interpolation or localization key.

## Root Cause (suspected)

The balance value and currency are concatenated incorrectly — likely a string like `$"{balance}_{currencyCode}"` or a localization key `balanceCurrency` that is not resolved. The variable or format string is rendered as a literal instead of being interpolated.

## Fix

Locate the dashboard component that renders the total balance and fix the string interpolation or currency formatting so the value is rendered as a properly formatted monetary amount (e.g. using a `CurrencyFormatter` helper or `ToString("C")` with the correct culture derived from the currency code).

## Tasks

- [ ] Locate the dashboard component/razor file rendering "Total balance"
- [ ] Fix the string interpolation or binding for the balance + currency
- [ ] Verify the dashboard displays a correctly formatted balance (e.g. `€0.00`) for all supported currencies

## Solution

**Root cause:** `TotalBalanceKpiWidget.razor` formatted the balance using a raw `N2` number format string with the ISO-4217 currency code appended in a separate `<small>` HTML tag, instead of using the shared `CurrencyFormatter.Format()` helper. This produced output like `0.00 EUR` instead of the expected `€0.00`.

**Files changed:**
- `LocalFinanceManager/Components/Pages/Dashboard/TotalBalanceKpiWidget.razor`

**Fix:** Replaced the manual `@($"{(TotalBalance < 0 ? "-" : "")}{Math.Abs(TotalBalance):N2}")` + `<small> @Currency</small>` rendering with a single call to `@CurrencyFormatter.Format(TotalBalance, Currency)`. Added `@using LocalFinanceManager.Helpers` at the top of the component. `CurrencyFormatter.Format` maps the ISO-4217 currency code to the correct `CultureInfo` via `RegionInfo`, so the output now correctly displays e.g. `€0.00` for EUR.
