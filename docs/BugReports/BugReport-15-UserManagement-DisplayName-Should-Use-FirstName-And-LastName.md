# BugReport-15: UserManagement Display Name Should Use First Name And Last Name

## Status

- [ ] Open

## Summary

The display name shown in the admin user management page should prefer a user's first name and last name when those profile fields are present, instead of only showing the stored display name value.

## Environment

- Version: latest
- Scope: admin user management page
- Frequency: always reproducible when profile names are set

## Steps to Reproduce

1. Sign in as a regular user and set first name and last name on the personal profile page.
2. Sign in as an admin user.
3. Open the user management page.
4. Review the value shown in the `Weergavenaam` column for that user.

## Expected Behaviour

If first name and last name are available, the admin overview shows `FirstName LastName`. If one of the two is missing, it uses the best available non-empty name value. If neither is available, it falls back to the existing stored display name.

## Actual Behaviour

The admin overview continues to show the generic stored display name instead of the more specific profile name fields.

## Workaround

- Admins can infer identity from email, but the intended display name is not reflected.

## Impact

- The admin overview shows lower-quality identity information than the profile feature already captures.
- Makes user lists less readable and less personal.
- Can create confusion when the stored display name differs from the user's actual profile name.
- Severity: Low.

## Related Reports

- None identified.

## Suspected Scope

Likely missing composition logic between core user records and profile preferences:

- `LocalFinanceManager/Components/Pages/Admin/UserManagement.razor`
- `LocalFinanceManager/DTOs/AdminDTOs.cs`
- `LocalFinanceManager/Services/AdminService.cs`
- `LocalFinanceManager/Services/UserPreferencesService.cs`

Possible cause: admin user summaries are built only from `Users.DisplayName`, while first name and last name are stored in `UserPreferences` and are never joined into the admin projection.

## Tasks

- [ ] Define display-name precedence for admin views
- [ ] Join or project profile preference names into the admin user summary
- [ ] Format the display name as `FirstName LastName` when available
- [ ] Preserve a sensible fallback when one or both profile name fields are missing
- [ ] Add or update regression tests for all name-combination scenarios
- [ ] Verify sorting behavior remains predictable after the display-name change

## Acceptance Criteria

- [ ] User management shows `FirstName LastName` when both values are present
- [ ] User management shows the best available non-empty name when only one value is present
- [ ] User management falls back to stored display name when no profile names exist
- [ ] Regression test added or updated and passing
