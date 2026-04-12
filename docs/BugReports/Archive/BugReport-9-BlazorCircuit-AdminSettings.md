# BugReport-9: Blazor Circuit Terminated When Opening Admin Settings

## Status

- [x] Resolved

## Summary

When opening the Admin Settings page, an unhandled exception occurs that terminates the Blazor circuit, crashing the current page session.

## Error Message

```
blazor.web.b9228eflpl.js:1 [2026-04-12T21:13:54.696Z] Error: There was an unhandled exception on the current circuit, so this circuit will be terminated. For more details turn on detailed exceptions by setting 'DetailedErrors: true' in 'appSettings.Development.json' or set 'CircuitOptions.DetailedErrors'.
```

## Steps to Reproduce

1. Log in with a user that can access admin features.
2. Navigate to Admin Settings.
3. Observe the unhandled exception and terminated circuit.

## Expected Behaviour

Admin Settings should load successfully without circuit termination.

## Actual Behaviour

An unhandled exception terminates the Blazor circuit. The exact cause is not visible without detailed errors enabled.

## Root Cause (confirmed)

Unhandled exceptions during admin page initialization were not consistently caught.
When an exception occurred in Admin Settings initialization or admin authorization checks
(route guard/layout), the exception bubbled up and terminated the Blazor circuit.

## Diagnosis Steps

1. Enable detailed Blazor errors locally (`DetailedErrors: true`) and reproduce.
2. Capture full exception stack trace from browser and server logs.
3. Correlate timestamp with application logs to identify failing component/service.
4. Validate Admin Settings dependencies (options binding, DI registrations, auth state assumptions).

## Tasks

- [x] Enable detailed errors and capture complete stack trace
- [x] Identify and document root cause in the failing Admin Settings flow
- [x] Implement fix in the failing component/service
- [x] Add regression test for opening Admin Settings without circuit termination
- [x] Verify fix by opening Admin Settings repeatedly in development and test environments

Verification notes:

- Targeted E2E run passed: 11/11 tests succeeded using filter FullyQualifiedName~AdminSettings|FullyQualifiedName~AdminPanel.
- Dedicated AdminSettings E2E run passed: 2/2 tests succeeded, including AdminSettings_RepeatedNavigation_DoesNotShowCircuitErrorUI.

## Solution

Implemented defensive exception handling and logging in all relevant admin entry points:

- Admin Settings page initialization now catches unexpected exceptions, logs the error, and keeps the page alive with safe fallback state.
- Admin route guard now catches failures in admin-resolution logic and redirects safely instead of letting the circuit crash.
- Admin layout now catches failures in both initial and first-render admin checks.

Changed files:

- LocalFinanceManager/Components/Pages/Admin/Settings.razor
- LocalFinanceManager/Components/Shared/AdminRouteGuard.razor
- LocalFinanceManager/Components/Layout/AdminLayout.razor
- tests/LocalFinanceManager.E2E/Admin/AdminSettingsTests.cs
