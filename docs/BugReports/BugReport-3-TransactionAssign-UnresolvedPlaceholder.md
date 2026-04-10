# BugReport-3: Unresolved Placeholder in Transaction Assign Title

## Status

- [ ] Open

## Summary

When assigning a transaction, the page/dialog title is displayed as `Transactie toewijzen@TransactionYearSuffix` instead of the resolved value (e.g. `Transactie toewijzen 2026`).

## Steps to Reproduce

1. Log in and navigate to the transactions list.
2. Click the assign/toewijzen action on any transaction.
3. Observe the title of the page or dialog.

## Expected Behaviour

The title displays the resolved text, e.g. `Transactie toewijzen 2026`.

## Actual Behaviour

The title displays `Transactie toewijzen@TransactionYearSuffix` — the `@TransactionYearSuffix` variable or localization key is not resolved and is rendered as a literal string.

## Root Cause (suspected)

`@TransactionYearSuffix` is a Razor variable or interpolated string that is either not initialized, out of scope, or incorrectly concatenated with the title string (e.g. using `+` or string interpolation instead of proper Razor syntax).

## Fix

Ensure `TransactionYearSuffix` is correctly defined and in scope in the component, and that the title string uses proper Razor interpolation (e.g. `$"Transactie toewijzen {TransactionYearSuffix}"`).

## Tasks

- [ ] Locate the component/page that renders the assign transaction title
- [ ] Fix the string interpolation or variable binding for `TransactionYearSuffix`
- [ ] Verify the title renders correctly for a transaction in the current and a past year
