# User Story 21 - User Profile Page

## Story

As a user I want a profile page where I can set my name and profile picture, so that the application feels more personal and I can quickly access my account settings from the navigation bar.

## Acceptance Criteria

### Profile Avatar in Navigation Bar

- [ ] When logged in: the navigation bar shows a circular profile picture instead of a username + separate buttons
- [ ] When no profile picture is set: the avatar shows the initials of first and last name (e.g. "JD" for John Doe)
- [ ] When no name is set: the avatar shows the first two characters of the email address
- [ ] The initials avatar has a background color via CSS custom properties (works in both light and dark mode)
- [ ] Not logged in: the navigation bar remains unchanged (login/register links + theme toggle)

### Dropdown Menu

- [ ] Clicking the avatar opens a dropdown with three options: "My account", theme toggle (sun/moon), "Logout"
- [ ] "My account" navigates to `/account`
- [ ] Theme toggle switches between light/dark and persists the preference (same behaviour as current)
- [ ] "Logout" navigates to `/logout`
- [ ] Clicking outside the dropdown closes it

### Profile Page (`/account`)

- [ ] The page is only accessible to authenticated users (`[Authorize]`)
- [ ] The page shows a large avatar preview (96px)
- [ ] The user can upload a profile picture (max 2 MB, accepted formats: JPEG, PNG, WebP)
- [ ] A file that is too large (>2 MB) shows an error message
- [ ] An invalid file type shows an error message
- [ ] The user can remove the profile picture (button only visible when a picture exists)
- [ ] The user can fill in First Name and Last Name and save them
- [ ] After saving a success notification (toast/alert) is shown
- [ ] Breadcrumb shows "My account"

### Storage

- [ ] Profile pictures are stored in Supabase Storage bucket `profile-pictures`
- [ ] `UserPreferences` contains `FirstName`, `LastName`, and `ProfileImagePath` (path within the bucket)
- [ ] EF Core migration adds the three new columns to the `UserPreferences` table
- [ ] Migration is applied automatically on app startup

## Technical Notes

- Extend `SupabaseOptions` with a `StorageBucket` property
- New `ISupabaseStorageService` for upload/delete/public URL via Supabase Storage REST API (forwarding user JWT)
- New `IUserProfileService` (Blazor-side) for HTTP calls to `/api/users/me/*`
- New `UserProfileController` with: `GET`, `PUT`, `POST /profile-picture`, `DELETE /profile-picture`
- Shared `ProfileAvatar.razor` component (reusable: small in top bar, large on profile page)
- `ProfileDropdown.razor` shared component injects `IUserProfileService`, `IThemeService`, `IUserContext`
- Initials fallback: pure CSS, no JavaScript

## Tasks

### 👤 User tasks (manual steps required)

- [ ] Create Supabase Storage bucket `profile-pictures` in the Supabase dashboard
- [ ] Set bucket visibility to **public** (so profile picture URLs are publicly accessible without auth)
- [ ] Add RLS policy on the bucket: users can only upload/delete their own files (path starts with their `user_id`)
- [ ] Add `Supabase:StorageBucket` = `profile-pictures` to environment variables / Azure App Service configuration

### 🤖 Copilot tasks (implemented in code)

- [ ] Extend SupabaseOptions with StorageBucket; update appsettings.json and appsettings.Development.json
- [ ] Extend UserPreferences model (FirstName, LastName, ProfileImagePath)
- [ ] Create and apply EF Core migration
- [ ] Implement ISupabaseStorageService + SupabaseStorageService
- [ ] Create UserProfileDTOs + FluentValidation validator
- [ ] Implement UserProfileController (GET/PUT profile, POST/DELETE profile picture)
- [ ] Implement IUserProfileService + UserProfileService (Blazor HTTP client)
- [ ] Register services in ServiceCollectionExtensions
- [ ] Create ProfileAvatar.razor shared component (picture + initials fallback, CSS)
- [ ] Create ProfileDropdown.razor shared component (avatar trigger + dropdown menu)
- [ ] Replace MainLayout.razor Authorized section with ProfileDropdown
- [ ] Add "My account" nav item to NavMenu.razor (bi-person-circle, authenticated only)
- [ ] Create AccountProfile.razor page (/account, [Authorize])
- [ ] Add CSS for avatar-circle and avatar-initials in app.css
- [ ] Unit tests: UserProfileService (mock HTTP)
- [ ] Integration tests: UserProfileController validation (>2 MB, wrong type -> 400)
