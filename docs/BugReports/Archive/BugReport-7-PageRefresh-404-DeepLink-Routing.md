# BugReport-7: Page Refresh 404 / Dangerous Site Warning on Deep Links

## Status

- [x] Resolved

## Summary

Refreshing the browser on any deep-link route (e.g. `/transactions`, `/accounts`) on Azure App Service resulted in an HTTP 404 error or a "Dangerous site" browser warning. Fixed by adding a `web.config` with an IIS URL Rewrite rule that falls back all non-API, non-file GET requests to `/`, allowing the Blazor Server app to handle routing.

## Description

Refreshing the browser on any route other than the homepage (`/`) or `/login` results in either:

1. A **"Dangerous site"** warning in Chrome — indicating the browser received an unexpected or invalid response.
2. An **HTTP 404** error from Azure App Service:
  > This page on `localfinancemanager-g8h4euamcfc3ewcs.westeurope-01.azurewebsites.net` could not be found  
  > No webpage was found for the web address: `https://localfinancemanager-g8h4euamcfc3ewcs.westeurope-01.azurewebsites.net/transactions`  
   > HTTP ERROR 404

## Steps to Reproduce

1. Open the application and navigate to any deep route, e.g. `/transactions`, `/accounts`, `/budget`, etc.
2. Press **F5** or use the browser refresh button.
3. Observe the 404 error or dangerous site warning instead of the expected page.

## Expected Behaviour

The application loads correctly on refresh for any valid route. For Blazor Server, the server should rewrite app routes back to `/` so Blazor routing can resolve the original URL.

## Actual Behaviour

Azure App Service returns HTTP 404 on deep links (for example `/transactions`) during refresh because IIS treats the request as a direct server route and finds no matching physical file or endpoint.

## Environment

- **Hosting:** Azure App Service (westeurope)
- **URL:** `https://localfinancemanager-g8h4euamcfc3ewcs.westeurope-01.azurewebsites.net`
- **Framework:** Blazor Server / ASP.NET Core on net10.0
- **Browser:** Google Chrome

## Root Cause (Suspected)

Azure App Service (IIS-based) likely misses fallback handling for deep links. These routes are valid inside the Blazor app, but are unknown to IIS as direct HTTP endpoints unless a fallback/rewrite is configured.

For a Blazor Server app using `MapRazorComponents<App>()`, the server must rewrite non-file, non-API paths back to `/` so the app can bootstrap and handle client-side routing. Without this, refresh/direct navigation on app routes returns 404.

Possible causes:

- Missing `web.config` with IIS URL Rewrite rule for non-file, non-directory requests
- Missing or incorrect fallback mapping in `Program.cs` (e.g. `UseStatusCodePagesWithReExecute("/")`)
- Rewrite rule is too broad and also rewrites API/static files, causing incorrect browser behavior

## Affected Routes

All routes except `/` and `/login`, including but not limited to:

- `/transactions`
- `/accounts`
- `/budget`
- `/dashboard`
- `/account`

## Priority

High — affects all authenticated users who refresh or share a direct link.

## Solution

Implemented an IIS deep-link fallback for Azure App Service by adding a project-level `web.config`.

What was changed:

- Added `LocalFinanceManager/web.config` with URL Rewrite rule:
  - Rewrites only `GET` requests
  - Excludes `/api` and `/api/*` paths
  - Excludes `/health*` paths
  - Excludes `/_blazor` WebSocket connections
  - Excludes any URL with a file extension (e.g. `.css`, `.js`, `.ico`) regardless of whether the file exists
  - Excludes existing files/directories
  - Rewrites all remaining deep links to `/`
- Used publish-time placeholders (`%LAUNCHER_PATH%`, `%LAUNCHER_ARGS%`, `%HOSTING_MODEL%`) in `aspNetCore` element to support both framework-dependent and self-contained deploys without hardcoded assembly names.
- Kept ASP.NET Core handler (`AspNetCoreModuleV2`) configuration intact.

Why this fixes the issue:

- On refresh, IIS previously treated routes such as `/transactions` as direct server paths and returned 404.
- The rewrite now serves the Blazor host response for app routes, allowing client-side Blazor routing to resolve the original URL.

Validation:

- `dotnet build LocalFinanceManager.sln` completed successfully after the change.
