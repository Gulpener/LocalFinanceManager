# BugReport-13: Profile Picture Upload Not Possible

## Status

- [x] Resolved

## Summary

Users cannot upload a profile picture from the personal account page. The upload flow fails instead of saving the selected image and updating the avatar.

Additional observed symptoms from manual testing:

- Browser console shows `Failed to load resource: the server responded with a status of 404 (Not Found)` on the account page
- The UI shows `Upload failed. Make sure the file is a valid JPEG, PNG or WebP image.`
- The failing test case used a PNG file, so the issue is not limited to unsupported file extensions

## Environment

- Version: latest
- Scope: authenticated user profile page
- Frequency: reproducible

## Steps to Reproduce

1. Sign in with a regular user account.
2. Open the personal profile page.
3. Select a valid JPEG, PNG, or WebP image smaller than 2 MB.
4. Wait for the upload flow to complete.

## Expected Behaviour

The image is uploaded successfully, the avatar preview updates immediately, and the image is visible in the navigation avatar.

## Actual Behaviour

The upload does not complete successfully and the profile picture is not saved.

Observed error details:

- The account page logs a 404 resource error in the browser console
- The page surfaces the generic validation-style upload failure message even when the selected file is a PNG

## Workaround

- No reliable workaround confirmed.

## Impact

- Users cannot personalize their profile with a photo.
- The avatar fallback remains initials-only even after selecting a valid image.
- This blocks a key part of the user profile feature.
- Severity: Medium.

## Related Reports

- None identified.

## Suspected Scope

Likely issue in the profile-picture upload request flow between the Blazor page and protected API endpoint:

- `LocalFinanceManager/Components/Pages/AccountProfile.razor`
- `LocalFinanceManager/Controllers/UserProfileController.cs`
- `LocalFinanceManager/Services/SupabaseStorageService.cs`
- Related authentication or storage integration used by profile uploads

Possible cause: the endpoint itself exists, so the more likely problem is a downstream 404 in the storage flow, such as an incorrect Supabase URL, a missing `profile-pictures` bucket, or a generated image URL that points to a non-existent object. The current UI message may also be masking non-validation failures by showing the same text for any non-success response.

## Tasks

- [x] Reproduce with a valid image and capture the failing HTTP status and response body
- [x] Capture the exact failing request URL from the browser network tab to determine whether the 404 is from `/api/profile/picture` or from the generated image URL
- [x] Verify whether the upload request includes the expected authorization header
- [x] Validate multipart form field naming and content type handling end-to-end
- [x] Verify that the configured Supabase storage bucket `profile-pictures` exists and is reachable
- [x] Confirm Supabase storage upload succeeds and the returned path is persisted in user preferences
- [x] Ensure the UI distinguishes validation failures from storage/configuration failures
- [x] Add or update a regression test for successful authenticated profile picture upload
- [x] Verify avatar refresh in both the profile page and navigation after upload

## Acceptance Criteria

- [x] A valid authenticated upload succeeds from the profile page
- [x] The uploaded image is persisted and returned in the profile response
- [x] The avatar updates immediately after a successful upload
- [x] Regression test added or updated and passing

## Solution

The upload flow failed because `SupabaseStorageService.UploadAsync` used `PUT` against the Supabase object endpoint, which caused a 404 for new files in this flow. The service was updated to use `POST` for object creation and to normalize Supabase base URLs with `TrimEnd('/')` so storage and public URL calls do not generate malformed double-slash URLs.  

Additionally, `AccountProfile.razor` now maps upload failure messages by HTTP status code so storage/configuration failures are no longer shown as invalid image-format errors. Regression coverage was added in `tests/LocalFinanceManager.Tests/Unit/SupabaseStorageServiceTests.cs` to validate request method and URL generation.
