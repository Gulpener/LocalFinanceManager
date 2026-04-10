# BugReport-5: Dashboard Shows Unresolved Balance Currency Placeholder

## Status

- [ ] Open

## Summary

The dashboard displays `0.00_balanceCurrency` instead of a properly formatted balance with the correct currency symbol (e.g. `€0.00`).

## Steps to Reproduce

1. Log in and navigate to the dashboard.
2. Observe the "Totaal saldo" widget.

## Expected Behaviour

The total balance is displayed as a formatted amount with the correct currency symbol, e.g.:

```
Totaal saldo
€0.00
1 rekening(en)
```

## Actual Behaviour

The total balance is displayed as:

```
Totaal saldo
0.00_balanceCurrency
1 rekening(en)
```

The `_balanceCurrency` suffix indicates an unresolved string interpolation or localization key.

## Root Cause (suspected)

The balance value and currency are concatenated incorrectly — likely a string like `$"{balance}_{currencyCode}"` or a localization key `balanceCurrency` that is not resolved. The variable or format string is rendered as a literal instead of being interpolated.

## Fix

Locate the dashboard component that renders the total balance and fix the string interpolation or currency formatting so the value is rendered as a properly formatted monetary amount (e.g. using a `CurrencyFormatter` helper or `ToString("C")` with the correct culture derived from the currency code).

## Tasks

- [ ] Locate the dashboard component/razor file rendering "Totaal saldo"
- [ ] Fix the string interpolation or binding for the balance + currency
- [ ] Verify the dashboard displays a correctly formatted balance (e.g. `€0.00`) for all supported currencies
