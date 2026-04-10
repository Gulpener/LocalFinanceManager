# Bug Report 1 - Currency Symbol Incorrect

## Status

- [ ] Open

## Summary

Amounts are displayed with the generic `¤` symbol (e.g. `¤2.60`) instead of the currency symbol matching the account's currency setting (e.g. `€2.60` for EUR, `$2.60` for USD).

## Steps to Reproduce

1. Log in and open any page that displays transaction amounts or account balances.
2. Observe that amounts are shown as `¤2.60` instead of the expected currency symbol.

## Expected Behaviour

The currency symbol corresponds to the ISO-4217 currency code stored on the account (e.g. `EUR` → `€`, `USD` → `$`).

## Actual Behaviour

The generic Unicode currency placeholder `¤` is rendered for all amounts.

## Root Cause (suspected)

`ToString("C")` or the number formatter is called with a `CultureInfo` that does not resolve the correct symbol from the account's currency code.

## Fix

Derive a `CultureInfo` from the ISO-4217 currency code stored on the account and use it when formatting monetary values, so the correct symbol is always shown.

## Tasks

- [ ] Find all call sites of `ToString("C")` and currency formatting helpers
- [ ] Ensure the culture/locale is resolved from the account's ISO-4217 currency code (e.g. `EUR` → `€`)
- [ ] Add unit test: formatting `2.60` with currency `EUR` returns `€2.60`
