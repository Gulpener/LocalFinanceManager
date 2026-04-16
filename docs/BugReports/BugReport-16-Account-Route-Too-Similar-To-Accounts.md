# BugReport-16: Account Route Too Similar To Accounts

## Status

- [ ] Open

## Summary

The personal profile route uses `/account`, which is too similar to the existing `/accounts` route for financial accounts. This creates navigation ambiguity and increases the chance of users opening the wrong page.

## Environment

- Version: latest
- Scope: profile navigation and routing
- Frequency: always present

## Steps to Reproduce

1. Sign in to the application.
2. Open the profile dropdown from the navigation bar.
3. Observe that `My account` points to `/account`.
4. Compare this with the existing financial accounts page route `/accounts`.

## Expected Behaviour

The personal profile page uses a route that is clearly distinct from the financial accounts area, for example `/useraccount`.

## Actual Behaviour

The profile page uses `/account`, which is visually and semantically too close to `/accounts`.

## Workaround

- Users can still reach the correct page if they choose the exact dropdown item or type the exact route.

## Impact

- Creates avoidable confusion between user profile settings and financial account management.
- Increases the risk of incorrect navigation, especially for less frequent users.
- Requires extra cognitive load for a core navigation path.
- Severity: Low.

## Related Reports

- None identified.

## Suspected Scope

Likely route naming mismatch across profile page, navigation, tests, and documentation:

- `LocalFinanceManager/Components/Pages/AccountProfile.razor`
- `LocalFinanceManager/Components/Shared/ProfileDropdown.razor`
- `LocalFinanceManager/Components/Layout/NavMenu.razor`
- Related tests and docs that reference `/account`

Possible cause: the route was introduced as part of the profile feature without considering the existing `/accounts` domain route, resulting in an ambiguous URL pair.

## Tasks

- [ ] Confirm the replacement route name for the user profile page
- [ ] Update navigation links and route attributes to use the new route
- [ ] Add a redirect or compatibility path if needed for existing bookmarks
- [ ] Update tests and documentation referencing `/account`
- [ ] Verify no conflicts remain with `/accounts`

## Acceptance Criteria

- [ ] The user profile page uses a route clearly distinct from `/accounts`
- [ ] Navigation and deep links open the intended page consistently
- [ ] Existing tests and relevant documentation are updated
- [ ] Regression test added or updated and passing
