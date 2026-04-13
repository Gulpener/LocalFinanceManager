# BugReport-10: Dashboard Totaal Saldo Shows Generic Currency Symbol Instead of Euro

## Status

- [ ] Open

## Summary

On the dashboard, the "Totaal saldo" amount shows the generic currency symbol `¤` instead of the euro symbol `€`.

## Environment

- Version: latest
- Scope: only observed on the dashboard page

## Steps to Reproduce

1. Log in to the application.
2. Open the dashboard.
3. Check the "Totaal saldo" value.

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

- [ ] Reproduce locally and capture exact rendered value and currency code in component state
- [ ] Verify which formatter/path is used by the dashboard total balance widget
- [ ] Ensure EUR is formatted through the shared currency formatter/culture mapping
- [ ] Add or update regression test that validates `€` rendering for dashboard total balance
- [ ] Verify no regression for other ISO-4217 currencies

## Acceptance Criteria

- [ ] Dashboard "Totaal saldo" renders `€` for EUR amounts
- [ ] Dashboard no longer renders generic `¤` for EUR
- [ ] Regression test added/updated and passing
