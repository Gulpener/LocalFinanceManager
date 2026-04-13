# BugReport-6: BudgetStatus Progress Bar Colors Not Visible

## Status

- [x] Resolved

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

## Solution

**Root cause:** Bootstrap's `bg-success` and `bg-danger` utility classes apply background color via `rgba(var(--bs-success-rgb), ...)`, which conflicts with the app's custom CSS design system that uses different custom properties (`--color-success`, `--color-danger`). The Bootstrap variables don't match the app's color palette and could render incorrectly or be invisible.

**Fix applied in:** `LocalFinanceManager/Components/Pages/Dashboard/BudgetStatusWidget.razor`

Replaced Bootstrap utility classes on `.progress-bar` with an inline `style` attribute that references the app's own CSS custom properties directly:
```html
<div class="progress-bar" role="progressbar"
     style="width: @pct%; background-color: @(isOver ? "var(--color-danger)" : "var(--color-success)");"
     aria-valuenow="@pct" aria-valuemin="0" aria-valuemax="100"></div>
```
This ensures the fill color always uses the design-system colours (`#10b981` green / `#ef4444` red) in both light and dark mode. Added `role="progressbar"` and ARIA attributes for accessibility.
