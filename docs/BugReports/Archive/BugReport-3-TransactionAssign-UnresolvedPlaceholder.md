# BugReport-3: Unresolved Placeholder in Transaction Assign Title

## Status

- [x] Resolved

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

- [x] Locate the component/page that renders the assign transaction title
- [x] Fix the string interpolation or variable binding for `TransactionYearSuffix`
- [x] Verify the title renders correctly for a transaction in the current and a past year

## Solution

Root cause: in `TransactionAssignModal.razor` the heading text used `Transactie toewijzen@TransactionYearSuffix` directly in markup. Razor interpreted this sequence as literal text (email-like token) instead of an expression, so `@TransactionYearSuffix` was rendered unresolved.

Implemented fix:
- Replaced inline mixed literal/expression title with a dedicated computed property: `AssignModalTitle`.
- Updated modal header to render `@AssignModalTitle`, which guarantees proper Razor evaluation.
- Added component regression coverage in `TransactionAssignModalFocusTrapTests` with test cases for current and past years (e.g. 2026 and 2024), asserting the title renders as `Transactie toewijzen (YYYY)` and never contains `@TransactionYearSuffix`.

Verification:
- Targeted unit/component tests were executed successfully for the updated test file.
