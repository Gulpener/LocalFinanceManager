# BugReport-11: Login Admin Button Not Shown

## Status

- [ ] Open

## Summary

After logging in, the "Admin" button is sometimes not shown in the navigation, even for users with admin rights.

## Environment

- Version: latest
- Scope: production
- Frequency: intermittent (race condition)

## Steps to Reproduce

1. Deploy a new version of the application.
2. Log in with an admin account.
3. Check the navigation bar immediately after login.

## Expected Behaviour

The "Admin" button is immediately visible for admin users after a successful login.

## Actual Behaviour

The "Admin" button is sometimes missing immediately after login.

## Workaround

- A browser refresh usually shows the button afterwards.

## Impact

- Admin functionality is less discoverable in production.
- Causes confusion about permissions and login status.
- Increases support and triage effort due to intermittent behavior.

## Related Reports

- `docs/BugReports/Archive/BugReport-9-BlazorCircuit-AdminSettings.md` (related auth/circuit timing context, but not a duplicate)

## Suspected Scope

Likely a timing/race condition in the initialization of admin state for the navigation:

- `LocalFinanceManager/Components/Layout/NavMenu.razor`
- `LocalFinanceManager/Services/IUserContext.cs`
- `LocalFinanceManager/Services/UserContext.cs`
- `LocalFinanceManager/Components/Shared/AdminRouteGuard.razor`

Possible cause: admin status is evaluated too early as false during startup/warmup, after which the UI state is not immediately restored without a refresh.

## Tasks

- [ ] Reproduce in a production-like environment with a cold start after deployment
- [ ] Add temporary logging around user context and admin checks during nav initialization
- [ ] Validate the timing of login state versus navigation rendering
- [ ] Ensure that admin state is re-evaluated once user context is available
- [ ] Add a regression test for admin button visibility immediately after login (without refresh)
- [ ] Verify that route access and button visibility remain consistent for admin/non-admin users

## Acceptance Criteria

- [ ] "Admin" is immediately visible after login for admin users, including after a fresh deploy/cold start
- [ ] No manual refresh is required to make the button visible
- [ ] Non-admin users do not see the button
- [ ] Regression test added/updated and passing
