# BugReport-14: UserManagement Profile Photo Missing

## Status

- [x] Resolved

## Summary

The admin user management page does not show users' profile photos. This makes the overview less recognizable and inconsistent with the profile feature already available elsewhere in the application.

## Environment

- Version: latest
- Scope: admin user management page
- Frequency: always reproducible

## Steps to Reproduce

1. Sign in as an admin user.
2. Ensure one or more users have a profile picture configured.
3. Open the user management page.
4. Review the user overview table.

## Expected Behaviour

Each user row shows the user's profile photo when available, with the existing avatar fallback used when no photo is set.

## Actual Behaviour

The user management overview does not include any profile photo or avatar column for users.

## Workaround

- Admins can only identify users by textual fields such as email or display name.

## Impact

- Reduces scanability in the admin overview.
- Makes the profile feature feel incomplete from an administration perspective.
- Increases the chance of selecting the wrong user in larger user lists.
- Severity: Low to Medium.

## Related Reports

- None identified.

## Suspected Scope

Likely missing data projection and UI rendering support in admin user management:

- `LocalFinanceManager/Components/Pages/Admin/UserManagement.razor`
- `LocalFinanceManager/DTOs/AdminDTOs.cs`
- `LocalFinanceManager/Services/AdminService.cs`
- `LocalFinanceManager/Components/Shared/ProfileAvatar.razor`

Possible cause: admin user summaries only expose `DisplayName` and email, while profile image data lives in `UserPreferences` and is not joined or rendered for the admin overview.

## Tasks

- [x] Confirm the intended admin UX for showing avatars in the user list
- [x] Extend the admin user summary data to include profile image information
- [x] Render a profile photo or avatar fallback in the user management overview
- [x] Ensure the list remains readable and responsive on smaller screens
- [x] Add or update UI tests covering users with and without profile pictures
- [x] Verify no regression in admin page loading performance

## Acceptance Criteria

- [x] User management shows a profile photo when one exists for a user
- [x] Users without a photo show a consistent avatar fallback
- [x] The user overview remains usable on desktop and mobile widths
- [x] Regression test added or updated and passing

## Solution

Updated admin user projection and UI rendering so the user management list now includes avatar data from `UserPreferences` and displays `ProfileAvatar` per row. `AdminService` now resolves profile image public URLs and returns profile name/image fields in `UserSummaryResponse`, and `UserManagement.razor` renders photo-or-initials fallback via `ProfileAvatar`. Regression coverage was added/updated in `tests/LocalFinanceManager.Tests/Integration/AdminControllerIntegrationTests.cs` and `tests/LocalFinanceManager.E2E/Admin/AdminPanelTests.cs` for users with and without profile photos.
