# BugReport-7 - Page Refresh 404 / Dangerous Site Warning on Deep Links

## Description

Refreshing the browser on any route other than the homepage (`/`) or `/login` results in either:

1. A **"Gevaarlijke site" (Dangerous site)** warning in Chrome — indicating the browser received an unexpected or invalid response.
2. An **HTTP 404** error from Azure App Service:
   > Deze pagina op `localfinancemanager-g8h4euamcfc3ewcs.westeurope-01.azurewebsites.net` kan niet worden gevonden  
   > Er is geen webpagina gevonden voor het webadres: `https://localfinancemanager-g8h4euamcfc3ewcs.westeurope-01.azurewebsites.net/transactions`  
   > HTTP ERROR 404

## Steps to Reproduce

1. Open the application and navigate to any deep route, e.g. `/transactions`, `/accounts`, `/budget`, etc.
2. Press **F5** or use the browser refresh button.
3. Observe the 404 error or dangerous site warning instead of the expected page.

## Expected Behaviour

The application loads correctly on refresh for any valid route. The server should fall back to serving the Blazor app shell (index page), which then handles client-side routing.

## Actual Behaviour

Azure App Service returns HTTP 404 because it looks for a physical file or endpoint matching `/transactions` and finds none.

## Environment

- **Hosting:** Azure App Service (westeurope)
- **URL:** `https://localfinancemanager-g8h4euamcfc3ewcs.westeurope-01.azurewebsites.net`
- **Framework:** Blazor Server / ASP.NET Core on net10.0
- **Browser:** Google Chrome

## Root Cause (Suspected)

Azure App Service (IIS-based) does not have a URL rewrite rule to redirect all unknown paths back to the Blazor entry point. For a Blazor Server app, the server must handle all paths and serve the app shell; missing `web.config` rewrite rules or missing `UseStaticFiles` + fallback routing in `Program.cs` cause 404s on direct navigation/refresh.

Possible causes:
- Missing `web.config` with IIS URL Rewrite rule (`<rule name="SPA Fallback">`)
- `app.MapFallbackToPage("/_Host")` or `app.MapRazorComponents` fallback not configured in `Program.cs`
- Static file middleware intercepting requests before Blazor routing

## Affected Routes

All routes except `/` and `/login`, including but not limited to:
- `/transactions`
- `/accounts`
- `/budget`
- `/dashboard`
- `/account`

## Priority

High — affects all authenticated users who refresh or share a direct link.
