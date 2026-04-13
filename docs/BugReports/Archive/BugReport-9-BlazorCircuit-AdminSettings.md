# BugReport-9: Blazor Circuit Terminated When Opening Admin Settings

## Status

- [x] Resolved

## Summary

Admin Settings navigation is flaky: sometimes the page opens, and sometimes the user is redirected back to Dashboard. This is treated as a regression on the previously resolved Admin Settings flow.

## Regression Report (2026-04-13)

- Reported symptom: Admin Settings sometimes opens and sometimes redirects to Dashboard.
- Frequency: intermittent (non-deterministic).
- Impact: admins cannot reliably access settings.

## Root Cause (confirmed - regression 2026-04-13)

`UserContext.GetCurrentUserId()` could prefer `HttpContext` claims over the Blazor circuit identity.
In interactive Blazor navigation, `HttpContext` identity/claims are not always stable for component flows.
This occasionally resolved to an incorrect or incomplete user identity, making `IsAdminAsync()` return false and triggering an unintended redirect away from Admin Settings.

## Steps to Reproduce

1. Log in with a user that can access admin features.
2. Open Admin Settings from the UI.
3. Navigate away (for example to Dashboard) and return to Admin Settings several times.
4. Observe that behavior is inconsistent: sometimes Admin Settings loads, sometimes it redirects to Dashboard.

## Expected Behaviour

Admin Settings should load consistently for authorized admin users without random redirects.

## Actual Behaviour

Authorized users are intermittently redirected to Dashboard when opening Admin Settings.

## Suspected Area

- Admin authorization/state resolution timing in route guard/layout.
- Inconsistent auth state availability during navigation or first render.

## Error Message

```
blazor.web.b9228eflpl.js:1 [2026-04-12T21:13:54.696Z] Error: There was an unhandled exception on the current circuit, so this circuit will be terminated. For more details turn on detailed exceptions by setting 'DetailedErrors: true' in 'appSettings.Development.json' or set 'CircuitOptions.DetailedErrors'.
```

## Root Cause (confirmed)

Unhandled exceptions during admin page initialization were not consistently caught.
When an exception occurred in Admin Settings initialization or admin authorization checks
(route guard/layout), the exception bubbled up and terminated the Blazor circuit.

## Historical Root Cause and Fix (2026-04-12)

The previous issue was fixed with defensive exception handling in admin entry points. The current report indicates a likely related but different regression (intermittent redirect without guaranteed circuit termination).

## Diagnosis Steps

1. Enable detailed Blazor errors locally (`DetailedErrors: true`) and reproduce.
2. Capture full exception stack trace from browser and server logs.
3. Correlate timestamp with application logs to identify failing component/service.
4. Validate Admin Settings dependencies (options binding, DI registrations, auth state assumptions).

## Tasks

- [x] Reproduce intermittent redirect with deterministic test steps
- [x] Capture server and browser logs for successful vs redirect navigation
- [x] Identify root cause in admin authorization/state timing path
- [x] Implement fix for consistent Admin Settings access
- [x] Add regression test for intermittent redirect scenario
- [x] Verify fix by repeated navigation runs in development and test environments

Verification notes:

- Targeted E2E run passed: 11/11 tests succeeded using filter FullyQualifiedName~AdminSettings|FullyQualifiedName~AdminPanel.
- Dedicated AdminSettings E2E run passed: 2/2 tests succeeded, including AdminSettings_RepeatedNavigation_DoesNotShowCircuitErrorUI.

## Previous Solution (2026-04-12)

Implemented defensive exception handling and logging in all relevant admin entry points:

- Admin Settings page initialization now catches unexpected exceptions, logs the error, and keeps the page alive with safe fallback state.
- Admin route guard now catches failures in admin-resolution logic and redirects safely instead of letting the circuit crash.
- Admin layout now catches failures in both initial and first-render admin checks.

Changed files:

- LocalFinanceManager/Components/Pages/Admin/Settings.razor
- LocalFinanceManager/Components/Shared/AdminRouteGuard.razor
- LocalFinanceManager/Components/Layout/AdminLayout.razor
- tests/LocalFinanceManager.E2E/Admin/AdminSettingsTests.cs

## Solution (2026-04-13)

Implemented identity source stabilization in `UserContext` for Blazor circuits:

- `UserContext.GetCurrentUserId()` now prefers `IBlazorCircuitUser` when initialized.
- This prevents transient `HttpContext` claim inconsistencies from causing false non-admin evaluations during admin page navigation.
- Added focused unit tests to lock the behavior:
  - circuit identity takes precedence when both circuit and HTTP identities exist;
  - circuit identity still wins when HTTP auth is present but `sub` is missing;
  - HTTP `sub` lookup remains the fallback when circuit identity is not initialized.

Changed files:

- LocalFinanceManager/Services/UserContext.cs
- tests/LocalFinanceManager.Tests/Services/UserContextCurrentUserIdTests.cs

Verification notes:

- `dotnet test tests/LocalFinanceManager.Tests/LocalFinanceManager.Tests.csproj --filter "FullyQualifiedName~UserContextCurrentUserIdTests"` passed (3/3).
- `dotnet test tests/LocalFinanceManager.E2E/LocalFinanceManager.E2E.csproj --filter "FullyQualifiedName~AdminSettings_RepeatedNavigation_DoesNotShowCircuitErrorUI"` passed (1/1).

## Follow-up Regression (2026-04-13) — Resolved

> **Note:** This section is part of the completed, archived resolution history for BugReport-9.
> A duplicate active tracking file (`docs/BugReports/BugReport-9-BlazorCircuit-AdminSettings.md`) was removed; this archived record is the canonical source for the full resolution history.

Additional user-reported path still showed intermittent redirect behavior:

- `Beheer -> ML modellen -> Bewaking -> Dashboard` (not deterministic).

Additional hardening implemented:

- `AdminRouteGuard` now uses extended retry stabilization for admin resolution:
  - increased retries (`MaxResolveAttempts`) and short retry delay between attempts;
  - retries on both empty user id and transient `IsAdminAsync == false` before redirect decision;
  - richer debug/warning/error logs with current route and attempt counters.

Additional regression tests added and verified:

- `AdminPanel_MlToMonitoring_DoesNotRedirectToDashboard` (new).
- `AdminPanel_RepeatedCrossTabNavigation_DoesNotRedirectAwayFromAdminPages` (existing broader flow).

Verification notes:

- `dotnet test tests/LocalFinanceManager.E2E/LocalFinanceManager.E2E.csproj -p:UseAppHost=false --filter "FullyQualifiedName~AdminPanel_MlToMonitoring_DoesNotRedirectToDashboard|FullyQualifiedName~AdminPanel_RepeatedCrossTabNavigation_DoesNotRedirectAwayFromAdminPages"` passed (2/2).

Changed files:

- LocalFinanceManager/Components/Shared/AdminRouteGuard.razor
- tests/LocalFinanceManager.E2E/Admin/AdminPanelTests.cs
