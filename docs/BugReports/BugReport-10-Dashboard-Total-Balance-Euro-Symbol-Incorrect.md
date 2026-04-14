# BugReport-10: Dashboard Total Balance Shows Generic Currency Symbol Instead of Euro

## Status

- [ ] Reopened

## Reopen Reason

This bug was previously marked as resolved, but the issue is reproducible again. The dashboard still shows the generic currency symbol instead of the euro sign.

## Regression Update (2026-04-14)

- Newly observed value: `¤0.00`
- Expected value for EUR: `€0.00`
- Reported scope: dashboard total balance path

## Summary

On the dashboard, the "Total balance" amount shows the generic currency symbol `¤` instead of the euro symbol `€`.

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

The total balance is displayed with the generic currency symbol, for example: `¤123.45` or `¤0.00`.

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

- [ ] Reproduce locally and capture exact rendered value and account currency code
- [ ] Reproduce in deployed environment and capture runtime globalization/culture settings
- [ ] Verify dashboard total balance always uses `CurrencyFormatter.Format(...)`
- [ ] Verify no bypass paths in dashboard/account widgets for currency rendering
- [ ] Add or update regression tests to assert EUR shows `€` (including zero values)
- [ ] Verify no regression for other ISO-4217 currencies

## Acceptance Criteria

- [ ] Dashboard "Total balance" renders `€` for EUR amounts, including zero (`€0.00`)
- [ ] Dashboard no longer renders generic `¤` for EUR
- [ ] Regression test added/updated and passing
