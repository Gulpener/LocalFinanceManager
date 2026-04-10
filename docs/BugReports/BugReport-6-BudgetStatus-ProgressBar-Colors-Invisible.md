# BugReport-6: BudgetStatus Progress Bar Colors Not Visible

## Status

- [ ] Open

## Description

In the **BudgetStatusWidget** on the dashboard, the progress bar colors (`bg-success` / `bg-danger`) are not visible. The bars appear to render without a visible fill, making it impossible to distinguish between on-budget and over-budget categories.

## Steps to Reproduce

1. Log in and navigate to the Dashboard.
2. Ensure at least one budget plan with budget lines exists for the current month.
3. Observe the "Budgetstatus" widget.

## Expected Behaviour

- Progress bars are **green** (`bg-success`) when spending is within budget.
- Progress bars are **red** (`bg-danger`) when spending exceeds budget.

## Actual Behaviour

- Progress bar fills are invisible or not displayed correctly.

## Affected File

- `LocalFinanceManager/Components/Pages/Dashboard/BudgetStatusWidget.razor` — `<div class="progress-bar @(isOver ? "bg-danger" : "bg-success")" ...>`

## Possible Cause

The Bootstrap `progress-bar` color utility classes (`bg-success`, `bg-danger`) may be overridden by a CSS custom property or theme variable (e.g. dark mode), or the `progress` container lacks the required Bootstrap base styles.

## Notes

<!-- Add screenshots or additional context here -->
