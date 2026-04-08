# Plan: Migrate from Blazor Server to ASP.NET Core Web API + Angular SPA

**Date:** April 9, 2026  
**Status:** Proposal — not yet approved  
**Driver:** Blazor Server SignalR flakiness, desire for API-first architecture, Angular ecosystem benefits  
**Goal:** Replace the Blazor Server UI with an Angular SPA served from the same ASP.NET Core host; keep all existing API controllers, services, and data layer intact

---

## Why migrating makes sense here

The existing codebase already has a clean API-first split: all 9 domain controllers
(`AccountsController`, `TransactionsController`, `BudgetPlansController`, etc.) are
fully functional REST endpoints independently of Blazor. The Blazor UI is the UI
shell that calls those endpoints. Replacing the shell leaves the business logic
untouched.

Additional drivers:

- SignalR race conditions are structural and affect Playwright E2E reliability regardless of workarounds
- Angular's change detection is client-side and deterministic — Playwright tests work as expected
- An Angular SPA enables future native mobile clients against the same API with no backend changes
- Angular CLI ecosystem (Jest, Angular Testing Library, Cypress/Playwright without SignalR noise) is widely understood

---

## Architecture Overview

```
LocalFinanceManager/                 (ASP.NET Core — keep, shrink)
├── Controllers/                     unchanged — all 9 controllers
├── Services/                        keep domain services, remove ~7 Blazor-UI services
├── Data/                            unchanged
├── Models/                          unchanged
├── DTOs/                            unchanged
├── Migrations/                      unchanged
├── ClientApp/                       NEW — Angular 18+ CLI project
│   ├── src/app/
│   │   ├── auth/                    login, register, password-reset, auth guard, interceptor
│   │   ├── accounts/
│   │   ├── transactions/            most complex — assign modal, bulk, split, filters
│   │   ├── budget-plans/
│   │   ├── categories/
│   │   ├── dashboard/               8 widget components
│   │   ├── sharing/
│   │   ├── backup/
│   │   └── admin/                   settings, monitoring, auto-apply, ML
│   └── (ng build → ../wwwroot)
├── wwwroot/                         Angular build output (gitignored)
└── Program.cs                       remove Blazor DI; add MapFallbackToFile("index.html")

tests/
├── LocalFinanceManager.Tests/       unchanged — unit + integration tests still pass
└── LocalFinanceManager.E2E/         keep infrastructure, update tests for Angular DOM
```

**Serving model:** Angular is built into `wwwroot/`, served by Kestrel as static files. No
CORS configuration required. A `MapFallbackToFile("index.html")` in `Program.cs` handles
deep-linked Angular routes.

---

## What stays, what goes

### Keep (no changes required)

- All API controllers
- All domain services: `AccountService`, `CategoryService`, `BudgetPlanService`, `TransactionAssignmentService`, `SharingService`, `BackupService`, `MonitoringService`, `AutoApplyJobService`, `MLService`, import pipeline
- All background services: `AutoApplyBackgroundService`, `MLRetrainingBackgroundService`
- All repositories and `AppDbContext`
- All models, DTOs, FluentValidation validators
- JWT Bearer auth middleware (Supabase RS256/HS256)
- `DevSmoke` auth handler (re-usable by E2E tests)
- `LocalFinanceManager.ML` class library (unchanged)

### Remove (Blazor-specific, no equivalent in Angular)

- `Components/` directory (all `.razor` files)
- `CustomAuthenticationStateProvider`
- `AuthTokenStore` (circuit-scoped JWT holder)
- `BlazorCircuitUser` / `BlazorCircuitUserAccessor`
- `IBreadcrumbService` / `BreadcrumbService` (becomes Angular router breadcrumbs)
- `IThemeService` / `ThemeService` (becomes Angular service + localStorage)
- `IUndoService` / `UndoService` (becomes Angular state pattern)
- `IFilterStateService` / `FilterStateService` (becomes Angular component state)
- `IRecentCategoriesService` / `RecentCategoriesService` (becomes Angular service)
- `IDeviceDetectionService` / `DeviceDetectionService` (becomes Angular CDK)
- Blazor NuGet references in `LocalFinanceManager.csproj`

### New (Angular)

- Angular 18+ standalone components for every page / widget
- `AuthService` using `@supabase/supabase-js` for login, register, session management
- `AuthInterceptor` (`HttpInterceptor`) that attaches `Authorization: Bearer <token>`
- `AuthGuard` (`CanActivateFn`) replacing `RedirectToLogin` component
- Angular `Router` configuration replacing Blazor route attributes
- Jest (`jest-preset-angular`) for Angular unit + component tests

---

## Not Yet Decided — Risks and Unknowns

Before committing, the following must be investigated:

1. **Dashboard API gap**  
   No `DashboardController` exists. The 8 dashboard widgets currently pull data
   server-side in Blazor. In Angular they will each make separate API calls.
   Worth adding a single `GET /api/dashboard/summary` endpoint to avoid 8 concurrent
   requests on load. **Must confirm in Phase 0.**

2. **`IUserContext` scoping**  
   `IUserContext` / `UserContext` is currently circuit-scoped. Check if it reads
   `UserId` from `IHttpContextAccessor` (request-scoped, works in API controllers)
   or from Blazor circuit state. If the latter, a small refactor is needed.

3. **JWT storage and page-reload**  
   Memory-only token storage means users must re-login after F5. Supabase JS SDK
   uses `localStorage` by default (PKCE flow). Decide: accept `localStorage` (Supabase
   default) or add a short-lived HttpOnly cookie backed by a `/api/auth/refresh` endpoint.

4. **Admin panel API coverage**  
   `AutomationController` covers auto-apply settings. Confirm AppSettings (DB key/value)
   and ML model management are fully exposed via existing API routes, or list gaps.

5. **`TransactionImport` file parsing**  
   CSV/JSON import currently POSTs a file to `TransactionsController`. Confirm the
   endpoint accepts `multipart/form-data`. If it does, the Angular component is
   a straightforward `<input type="file">` with `FormData`.

---

## Migration Sequence

### Phase 0 — Backend Audit (1–2 days, read-only)

- [ ] Map each Blazor page to the API routes it calls; surface any page that calls no
      API and does server-side work inline
- [ ] Verify `IUserContext` reads from `IHttpContextAccessor` (not circuit state)
- [ ] Confirm Dashboard data sources — document which existing API routes cover each widget
- [ ] Confirm TransactionImport controller accepts `multipart/form-data`
- [ ] List any missing API endpoints (expected: `/api/dashboard/summary`, possibly others)

### Phase 1 — Backend Cleanup (half day, after Phase 0)

```powershell
# Remove Blazor packages
dotnet remove LocalFinanceManager package Microsoft.AspNetCore.Components.Web
# (remove any other Blazor/Razor package references)
```

- [ ] Delete `LocalFinanceManager/Components/` directory
- [ ] Remove Blazor-specific services from `Services/` and their DI registrations in `Program.cs`
- [ ] Add `app.UseStaticFiles()` and `app.MapFallbackToFile("index.html")` in `Program.cs`
- [ ] Remove `data-loaded-*` HTML attributes from any remaining server-rendered markup
- [ ] Verify `dotnet build` passes with no Blazor compile errors

### Phase 2 — Angular Scaffold + Auth (1 day, parallel with Phase 1)

```powershell
# Scaffold Angular app inside the main project
cd LocalFinanceManager
npx @angular/cli@latest new ClientApp --routing --style=scss --standalone --ssr=false

# Configure ClientApp output path
# In angular.json: set "outputPath": "../wwwroot"

# Install runtime dependencies
cd ClientApp
npm install @supabase/supabase-js

# Install test dependencies
npm install --save-dev jest jest-preset-angular @types/jest
```

- [ ] Add MSBuild target to `LocalFinanceManager.csproj` that runs `npm run build` before publish
- [ ] Add `ClientApp/node_modules/` and `wwwroot/browser/` to `.gitignore`
- [ ] Implement `AuthService` (Supabase PKCE + session management)
- [ ] Implement `AuthInterceptor` (attach Bearer token to all `/api/*` requests)
- [ ] Implement `AuthGuard`
- [ ] Implement Login, Register, Logout, PasswordReset pages
- [ ] Verify: login → JWT obtained → `GET /api/accounts` returns 200

### Phase 3 — Feature Parity (order by priority)

- [ ] Accounts: list, create, edit
- [ ] Transactions list + QuickFilters
- [ ] TransactionAssignModal (category assign, ML suggestion badge)
- [ ] BulkAssignModal
- [ ] SplitEditor
- [ ] Budget Plans: list, create, edit
- [ ] Categories: create, edit
- [ ] Dashboard (8 widgets, pending `/api/dashboard/summary` from Phase 0)
- [ ] Onboarding wizard
- [ ] Sharing: SharedWithMe page, ShareModal
- [ ] Backup: export (file download) + import (file upload)
- [ ] Admin: Settings, AutoApply config, Monitoring, ML model management

### Phase 4 — Tests

- [ ] Jest unit tests for Angular services (`AuthService`, API service wrappers)
- [ ] Angular component tests for critical components (TransactionAssignModal, BulkAssignModal, QuickFilters)
- [ ] Update `LocalFinanceManager.E2E/` Page Object Models for Angular DOM structure
- [ ] Remove Playwright workarounds (`data-loaded-*` waits, `NetworkIdle` hacks) — no longer needed
- [ ] Confirm all 9 existing API integration tests still pass (`dotnet test tests/LocalFinanceManager.Tests`)

---

## Test Strategy Recommendation

| Layer | Tool | Scope |
| --- | --- | --- |
| Angular unit/component | Jest + Angular Testing Library | Services, components with mocked HTTP |
| .NET API integration | xUnit/NUnit + in-memory SQLite | All existing integration tests, unchanged |
| E2E | Playwright (keep existing infra) | Reliable now — no SignalR races from client-side Angular |

E2E reliability improves structurally: Playwright no longer needs to race Blazor's
SignalR re-render cycle. `WaitForSelector` and `WaitForLoadStateAsync("networkidle")`
become accurate again because DOM updates trigger synchronously from Angular zone.

---

## Decision

This migration is worth doing if:

- Phase 0 audit shows the Dashboard API gap can be closed with one new endpoint
- `IUserContext` is confirmed to be `IHttpContextAccessor`-based (no circuit coupling)
- Phase 1 compiles cleanly with Blazor removed

It is **not** worth doing if:

- Multiple pages do significant server-side work with no API equivalent
  (would require unplanned new API endpoints extending timelines)
- JWT session-on-reload behavior via Supabase PKCE is unacceptable to users
- Timeline pressure makes a 2–3 week migration unacceptable right now

---

## Comparison

| Aspect | Current (Blazor Server) | Proposed (Angular + Web API) |
| --- | --- | --- |
| E2E flakiness | Structural — SignalR races | Near-zero — deterministic DOM updates |
| E2E run time | ~5–10 min | ~2–3 min |
| Frontend testability | bUnit (in-process) | Jest + Angular Testing Library |
| Real-time capability | Built-in (SignalR) | Not included (add later if needed) |
| API-first / mobile-ready | Partial | Yes — full decoupling |
| Offline / PWA potential | No | Yes (Angular Service Worker) |
| Developer tooling | Blazor-specific | Universal Angular CLI ecosystem |
| Backend rewrite required | — | No — all controllers and services reused |
| Estimated migration effort | — | ~2–3 weeks (Phase 0–4) |
