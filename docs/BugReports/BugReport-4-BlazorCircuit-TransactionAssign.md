# Bug Report 4 - Blazor Circuit Terminated When Assigning Transaction to Budget Line

## Status

- [ ] Open

## Summary

When assigning a transaction to a budget line, an unhandled exception occasionally occurs that terminates the Blazor circuit, crashing the current page session.

## Error Message

```
blazor.web.js [2026-04-10T18:22:23.918Z] Error: There was an unhandled exception on the current circuit,
so this circuit will be terminated. For more details turn on detailed exceptions by setting
'DetailedErrors: true' in 'appSettings.Development.json' or set 'CircuitOptions.DetailedErrors'.
```

## Steps to Reproduce

1. Log in and navigate to the transactions list.
2. Click the assign action on a transaction.
3. Select a budget line and confirm the assignment.
4. Observe that occasionally the circuit is terminated and the page becomes unresponsive.

## Expected Behaviour

The transaction is assigned to the budget line without errors. No circuit termination occurs.

## Actual Behaviour

An unhandled exception terminates the Blazor circuit. The exact cause is hidden unless `DetailedErrors` is enabled.

## Diagnosis Steps

1. Set `"DetailedErrors": true` in `appsettings.Development.json` under `CircuitOptions` or at the root.
2. Reproduce the issue and capture the full exception message and stack trace.
3. Check server-side logs in `logs/` for the corresponding error.

## Tasks

- [ ] Enable `DetailedErrors: true` locally and reproduce to capture the full stack trace
- [ ] Identify the root cause (null reference, concurrency conflict, missing budget line, etc.)
- [ ] Fix the underlying exception
- [ ] Add a try/catch or error boundary around the assign action to prevent full circuit termination
- [ ] Verify the fix by re-running the assign flow multiple times
