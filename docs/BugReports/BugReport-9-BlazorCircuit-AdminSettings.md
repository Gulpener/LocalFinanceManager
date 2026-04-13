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
