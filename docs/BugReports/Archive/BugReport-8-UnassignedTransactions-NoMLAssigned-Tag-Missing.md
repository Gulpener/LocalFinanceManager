# BugReport-8: Unassigned Transactions Missing "No ML Assigned" Tag

## Status

- [x] Resolved

## Summary

On the Transactions page, not all unassigned transactions show the expected "No ML Assigned" indicator (no-model badge). This makes ML assignment state unclear and appears to contribute to ML-related E2E test failures.

## Scope

- Reproduces on all branches (user confirmed).

## Steps to Reproduce

1. Log in and open the Transactions page.
2. Select an account with unassigned transactions.
3. Optionally apply the unassigned filter.
4. Inspect visible unassigned transaction rows.
5. Observe that some rows do not show either ML suggestion badge or "No ML Assigned" badge.

## Expected Behaviour

Every visible unassigned transaction row shows exactly one ML state badge:
- ML suggestion badge (`data-testid='ml-suggestion-badge'`), or
- No-model badge / "No ML Assigned" (`data-testid='no-model-badge'`).

## Actual Behaviour

Some unassigned transaction rows show no ML state badge at all.

## E2E Impact (Confirmed CI Evidence)

GitHub Actions Phase 4 (filter: `MLSuggestionTests|AutoApplyTests|MonitoringDashboardTests`) reports 3 failures caused by timeout waiting for:

`[data-testid='ml-suggestion-badge'], [data-testid='no-model-badge']`

Failing tests:

1. `MLSuggestionBadge_AcceptButton_RecordsFeedbackAndHidesBadge`
2. `MLSuggestionBadge_DisplayedOnUnassignedTransactions`
3. `MLSuggestionBadge_ShowsFeatureImportanceTooltip`

Common failure path:
- `WaitForFirstSuggestionBadgeAsync()` in `tests/LocalFinanceManager.E2E/ML/MLSuggestionTests.cs`
- timeout while waiting for any badge variant to become visible.

## Environment Context

CI debug output also showed these secrets as null:
- `SUPABASE_TEST_URL`
- `SUPABASE_TEST_KEY`
- `SUPABASE_TEST_JWT_SECRET`

This is currently treated as context, not confirmed root cause, because the direct failures are selector visibility timeouts and most tests in the same phase still pass.

## Root Cause (suspected)

One or more of the following:

- Conditional rendering branch does not always emit `no-model-badge` when no suggestion is available.
- Async badge hydration race condition (row renders before badge state resolves, then badge never re-renders for some rows).
- Filter/sort interaction removes or skips badge component rendering on a subset of unassigned rows.
- Per-row API/state mismatch for ML status resulting in no fallback badge.

## Tasks

- [ ] Reproduce locally with deterministic seed data and capture affected row IDs.
- [ ] Trace badge rendering path for unassigned rows and verify fallback to `no-model-badge` is unconditional.
- [ ] Add guard so each unassigned row always renders one of the two badge test IDs.
- [ ] Add/adjust component tests to assert one-badge-per-unassigned-row invariant.
- [ ] Stabilize E2E waits only after rendering bug is fixed (avoid masking product issue with longer timeouts).
- [ ] Re-run Phase 4 E2E set and attach green run evidence.

## Solution

Root cause was twofold:

1. `MLSuggestionBadge` could render no badge at all when the suggestions API returned a non-success status (for example 401/403/500) or a success response with an empty payload.
2. E2E timing on Transactions and ML admin pages was too strict in CI, causing false timeouts before the UI was fully interactive.

Implemented fix:

- Updated `LocalFinanceManager/Components/Shared/MLSuggestionBadge.razor`:
	- Reset suggestion state before each load.
	- Force fallback `no-model-badge` for non-success responses.
	- Force fallback `no-model-badge` when success payload is null.
	- Added warning logging for fallback path on non-success status codes.
- Added regression component test `tests/LocalFinanceManager.Tests/Components/MLSuggestionBadgeTests.cs`:
	- Verifies HTTP 500 from suggestions API still renders `no-model-badge`.
- Hardened E2E waits in `tests/LocalFinanceManager.E2E/Pages/TransactionsPageModel.cs`:
	- Added explicit transactions-page readiness wait on account filter visibility.
	- Added retry for account filter select during Blazor re-render detach/attach races.
	- Increased account-load completion wait timeout using `data-loaded-account` synchronization.
- Relaxed ML admin info waits in `tests/LocalFinanceManager.E2E/ML/MLSuggestionTests.cs`:
	- Increased model-info selector timeouts from 5s to 30s.

Verification:

- Unit/component validation passed:
	- `dotnet test tests/LocalFinanceManager.Tests/LocalFinanceManager.Tests.csproj --configuration Release --filter "FullyQualifiedName~MLSuggestionBadgeTests"`
- Phase 4 E2E validation passed after rebuild:
	- `dotnet test tests/LocalFinanceManager.E2E/ --configuration Release --logger "console;verbosity=minimal" --collect:"XPlat Code Coverage" --filter "FullyQualifiedName~MLSuggestionTests|FullyQualifiedName~AutoApplyTests|FullyQualifiedName~MonitoringDashboardTests"`
	- Result: 28 total, 27 passed, 0 failed, 1 skipped.
