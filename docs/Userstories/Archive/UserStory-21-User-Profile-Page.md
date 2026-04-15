# UserStory-21: User Profile Page

## Story

As a user I want a profile page where I can set my name and profile picture, so that the application feels more personal and I can quickly access my account settings from the navigation bar.

## Acceptance Criteria

### Profile Avatar in Navigation Bar

- [x] When logged in: the navigation bar shows a circular profile picture instead of a username + separate buttons
- [x] When no profile picture is set: the avatar shows the initials of first and last name (e.g. "JD" for John Doe)
- [x] When no name is set: the avatar shows the first two characters of the email address
- [x] The initials avatar has a background color via CSS custom properties (works in both light and dark mode)
- [x] Not logged in: the navigation bar remains unchanged (login/register links + theme toggle)

### Dropdown Menu

- [x] Clicking the avatar opens a dropdown with three options: "My account", theme toggle (sun/moon), "Logout"
- [x] "My account" navigates to `/account`
- [x] Theme toggle switches between light/dark and persists the preference (same behaviour as current)
- [x] "Logout" navigates to `/logout`
- [x] Clicking outside the dropdown closes it

### Profile Page (`/account`)

- [x] The page is only accessible to authenticated users (`[Authorize]`)
- [x] The page shows a large avatar preview (96px)
- [x] The user can upload a profile picture (max 2 MB, accepted formats: JPEG, PNG, WebP)
- [x] A file that is too large (>2 MB) shows an error message
- [x] An invalid file type shows an error message
- [x] The user can remove the profile picture (button only visible when a picture exists)
- [x] The user can fill in First Name and Last Name and save them
- [x] After saving a success notification (toast/alert) is shown
- [x] Breadcrumb shows "My account"

### Storage

- [x] Profile pictures are stored in Supabase Storage bucket `profile-pictures`
- [x] `UserPreferences` contains `FirstName`, `LastName`, and `ProfileImagePath` (path within the bucket)
- [x] EF Core migration adds the three new columns to the `UserPreferences` table
- [x] Migration is applied automatically on app startup

## Technical Notes

- Extend `SupabaseOptions` with a `StorageBucket` property
- New `ISupabaseStorageService` for upload/delete/public URL via Supabase Storage REST API (forwarding user JWT)
- **Extend existing `IUserPreferencesService`** with profile methods: `GetProfileAsync`, `UpdateProfileAsync`, `GetProfileImageUrlAsync`, `UpdateProfileImagePathAsync` — consistent with how `ThemeService` uses it; no separate Blazor-side HTTP client service needed
- New `UserProfileController` with: `GET`, `PUT`, `POST /profile-picture`, `DELETE /profile-picture`
  - Apply `[RequestSizeLimit(2_097_152)]` + `[RequestFormLimits(MultipartBodyLengthLimit = 2_097_152)]` on the `POST /profile-picture` action to reject oversized requests before reading the body
  - Server-side MIME validation: read the first 8 bytes of the uploaded stream (magic bytes) to verify JPEG (`FF D8`), PNG (`89 50 4E 47`), or WebP (`52 49 46 46 ... 57 45 42 50`). Do NOT rely on the `Content-Type` header alone
  - `POST /profile-picture`: if `ProfileImagePath` is already set, delete the existing Supabase Storage file before uploading the new one
- Storage path format: `{userId}/{Guid.NewGuid()}.{ext}` — used for both the upload path and the Supabase RLS policy enforcement
- Shared `ProfileAvatar.razor` component (reusable: small in top bar, large on profile page)
- `ProfileDropdown.razor` shared component injects `IUserPreferencesService`, `IThemeService`, `IUserContext` — `IThemeService` and `IUserContext` already exist and are registered
- Initials fallback: pure CSS, no JavaScript
- Dropdown close-on-outside-click: requires a JS interop helper (`clickOutsideHandler`) or `ElementRef` + `@onfocusout` approach — must be explicitly implemented, not assumed

## Tasks

### 👤 User tasks (manual steps required)

- [x] Create Supabase Storage bucket `profile-pictures` in the Supabase dashboard
- [x] Set bucket visibility to **public** (so profile picture URLs are publicly accessible without auth)
- [x] Add RLS policy on the bucket: users can only upload/delete their own files (path starts with their `user_id`)
- [x] Add `Supabase__StorageBucket` = `profile-pictures` to environment variables / Azure App Service configuration

### 🤖 Copilot tasks (implemented in code)

- [x] Extend SupabaseOptions with StorageBucket; update appsettings.json and appsettings.Development.json
- [x] Extend UserPreferences model (FirstName, LastName, ProfileImagePath)
- [x] Create and apply EF Core migration
- [x] Implement ISupabaseStorageService + SupabaseStorageService
- [x] Create UserProfileDTOs + FluentValidation validator
- [x] Implement UserProfileController (GET/PUT profile, POST/DELETE profile picture) — with `[RequestSizeLimit]`, magic-byte MIME validation, and auto-delete of old picture on re-upload
- [x] Extend IUserPreferencesService + UserPreferencesService with profile methods (GetProfileAsync, UpdateProfileAsync, GetProfileImageUrlAsync, UpdateProfileImagePathAsync)
- [x] Register ISupabaseStorageService in ServiceCollectionExtensions
- [x] Create ProfileAvatar.razor shared component (picture + initials fallback, CSS)
- [x] Create ProfileDropdown.razor shared component (avatar trigger + dropdown menu + JS interop for outside-click)
- [x] Replace MainLayout.razor Authorized section with ProfileDropdown
- [x] Create AccountProfile.razor page (/account, [Authorize])
- [x] Add CSS for avatar-circle and avatar-initials in app.css
- [x] Unit tests: IUserPreferencesService profile methods (mock DbContext)
- [x] Integration tests: UserProfileController validation (>2 MB → 400, wrong type → 400, missing auth → 401)
- [x] E2E tests:
  - [x] Upload profile picture → avatar in nav bar shows photo (not initials)
  - [x] Remove profile picture → nav bar falls back to initials
  - [x] Navigate to `/account` unauthenticated → redirect to `/login`
