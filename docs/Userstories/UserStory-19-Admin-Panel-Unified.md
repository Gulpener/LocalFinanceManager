# UserStory-19: Unified Admin Panel

**As** a user with admin privileges in Local Finance Manager  
**I want** to manage all administrative settings in one consolidated tabbed panel  
**So that** I can maintain the application efficiently without navigating between scattered pages

## Business Value

- **Consolidation:** All admin functions in one place — no scattered navigation paths
- **User management:** Visibility into registered users and their active shared resources
- **Share revocation:** Admin can revoke shared connections without needing the owner to do so
- **Maintainability:** Centralized management reduces cognitive load during application maintenance
- **Security:** Admin section is protected at both the UI and API layer; non-admins cannot access it

## Acceptance Criteria

### Admin Role & Access Control

- [ ] `User` entity has a `bool IsAdmin` property (default `false`); backed by EF Core migration
- [ ] Initial admin bootstrapped on startup: if no admin exists, the user whose email matches `Seed:AdminEmail` in `appsettings.json` gets `IsAdmin = true`
- [ ] Non-admin users visiting any `/admin/*` URL are immediately redirected to `/`
- [ ] The "Admin" nav item in NavMenu is **hidden** for non-admin users
- [ ] All `/api/admin/*` endpoints return **HTTP 403 ProblemDetails** for authenticated non-admins
- [ ] Self-demotion is blocked: admin cannot remove their own `IsAdmin` flag (toggle button disabled in UI; API returns **HTTP 400**)

### Tab Bar Navigation

- [ ] Admin section displays a persistent Bootstrap nav-tabs bar with 4 tabs:
  - **Settings** → `/admin/settings`
  - **ML Models** → `/admin/ml`
  - **Monitoring** → `/admin/monitoring`
  - **Users** → `/admin/users`
- [ ] Active tab is highlighted based on the current URL (`NavigationManager.Uri`)
- [ ] Tab navigation works via clicks and direct URL entry
- [ ] NavMenu consolidated: single "Admin" menu item → `/admin/settings`; separate "Auto-Apply" menu item → `/settings/auto-apply` (visible to all authenticated users)
- [ ] Existing admin routes remain backward-compatible (`/admin/monitoring` etc.)
- [ ] `/admin/autoapply` redirects to `/settings/auto-apply` for backward compatibility

### User Management Tab (`/admin/users`)

- [ ] Displays a table of all registered users with columns:
  - **Email** (sortable)
  - **Display Name**
  - **Admin** (badge shown when `IsAdmin = true`)
  - **Member since** (CreatedAt, relative date + absolute tooltip)
  - **Accounts** (number of accounts owned)
  - **Shares given** (number of active shares the user has given out)
  - **Shares received** (number of active shares the user has received)
  - **Actions** (toggle admin button)
- [ ] **Toggle Admin button** per user row:
  - Calls `POST /api/admin/users/{id}/toggle-admin`
  - Shows confirmation modal: *"Grant admin to {email}?"* or *"Revoke admin from {email}?"*
  - After confirmation, row updates inline (badge appears/disappears)
  - Disabled with tooltip *"Cannot change your own role"* for the currently signed-in user
- [ ] Clicking a user row expands a detail panel showing active shares:
  - Shared accounts: account name, recipient email, permission level, status
  - Shared budget plans: plan name, recipient email, permission level, status
- [ ] **Revoke button** per share entry: calls existing `DELETE /api/shares/accounts/{id}` / `DELETE /api/shares/budgetplans/{id}`
- [ ] Confirmation modal before revoke ("Are you sure you want to revoke this share?")
- [ ] Success message after revoke; share row disappears from expanded panel
- [ ] Empty state: "No users found"
- [ ] Loading spinner during data fetch
- [ ] On failed toggle-admin (network error / HTTP 500): show inline error alert "Failed to update admin role. Please try again." Auto-dismisses after 5 seconds
- [ ] On failed revoke (network error / HTTP 500): show inline error alert "Failed to revoke share. Please try again." Auto-dismisses after 5 seconds

### Responsiveness & Layout

- [ ] Tab bar is mobile-friendly (horizontal scroll on narrow screens)
- [ ] User table renders correctly on mobile (columns shrink/stack)

## Technical Implementation

### Phase 0: Admin Role Model & Authorization

**Modified file:** `LocalFinanceManager/Models/User.cs`

```csharp
public bool IsAdmin { get; set; } = false;
```

**New EF Core migration** adds `IsAdmin` column (`bool NOT NULL DEFAULT 0`).

**Seed logic** in `Program.cs` (Development + Production, runs after `MigrateAsync`):

```csharp
// If no admin exists, promote the configured email to admin
var adminEmail = config["Seed:AdminEmail"];
if (!string.IsNullOrEmpty(adminEmail) && !await db.Users.AnyAsync(u => u.IsAdmin))
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
    if (user != null) { user.IsAdmin = true; await db.SaveChangesAsync(); }
}
```

**Authorization policy** in `Program.cs`:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy =>
        policy.Requirements.Add(new IsAdminRequirement()));
});
// Register IAuthorizationHandler: IsAdminHandler resolves current User from DB and checks IsAdmin
```

**Modified file:** `Services/IUserContext.cs` — add method:

```csharp
/// <summary>
/// Returns true when the current user has IsAdmin = true in the database.
/// Returns false for unauthenticated users (GetCurrentUserId() == Guid.Empty) or when user is not found.
/// </summary>
Task<bool> IsAdminAsync();
```

**Modified file:** `Services/UserContext.cs` — implement `IsAdminAsync()`:

```csharp
public async Task<bool> IsAdminAsync()
{
    var userId = GetCurrentUserId();
    if (userId == Guid.Empty) return false;
    return await _context.Users
        .AsNoTracking()
        .Where(u => u.Id == userId)
        .Select(u => u.IsAdmin)
        .FirstOrDefaultAsync();
}
```

**New files:** `Services/Authorization/IsAdminRequirement.cs` + `IsAdminHandler.cs`

```csharp
public class IsAdminRequirement : IAuthorizationRequirement { }

public class IsAdminHandler : AuthorizationHandler<IsAdminRequirement>
{
    // Resolves Supabase 'sub' claim → User.IsAdmin via IUserContext
    // context.Fail() → results in HTTP 403 (not 401)
}
```

**New file:** `Components/Shared/AdminRouteGuard.razor`

```csharp
// Checks IUserContext.IsAdminAsync() on OnInitializedAsync
// If false → NavigationManager.NavigateTo("/", forceLoad: false)
// If true → renders @ChildContent
// All /admin/* pages wrap their body: <AdminRouteGuard>...</AdminRouteGuard>
```

**Modified:** `Components/Layout/NavMenu.razor` — "Admin" link rendered only when `await userContext.IsAdminAsync() == true`

**Modified:** `Components/Layout/AdminLayout.razor` — tab bar rendered only when admin (guard already redirects, but prevents flash)

### Phase 1: AdminLayout + Tab Bar

**New file:** `LocalFinanceManager/Components/Layout/AdminLayout.razor`

```csharp
// Blazor layout that wraps the existing MainLayout sidebar
// Adds Bootstrap nav-tabs above the page content area
// Detects active tab via NavigationManager.Uri.Contains("/admin/...")
// Tabs: Settings, ML Models, Monitoring, Users
// Tab bar only rendered when current user IsAdmin
```

**Modified files:**

- `Components/Pages/Admin/Settings.razor` — add `@layout AdminLayout`, wrap in `<AdminRouteGuard>`
- `Components/Pages/Admin/ML.razor` — add `@layout AdminLayout`, wrap in `<AdminRouteGuard>`
- `Components/Pages/Admin/Monitoring.razor` — add `@layout AdminLayout`, wrap in `<AdminRouteGuard>`
- `Components/Pages/Admin/AutoApplySettings.razor` — remove `/admin/autoapply` route alias; keep only `/settings/auto-apply`; no admin guard (accessible to all authenticated users)
- `Components/Layout/NavMenu.razor` — replace admin links with 1 "Admin" link to `/admin/settings` (hidden for non-admins) + keep "Auto-Apply" link to `/settings/auto-apply` (visible to all)

### Phase 2: User Management Backend

**New file:** `LocalFinanceManager/DTOs/AdminDTOs.cs`

```csharp
public record UserSummaryResponse(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsAdmin,
    DateTime CreatedAt,
    int AccountCount,
    int SharesGiven,
    int SharesReceived
);

public record UserSharesResponse(
    List<AccountShareDetail> AccountShares,
    List<BudgetPlanShareDetail> BudgetPlanShares
);

public record AccountShareDetail(Guid ShareId, string AccountName, string SharedWithEmail, string Permission, string Status);
public record BudgetPlanShareDetail(Guid ShareId, string PlanName, string SharedWithEmail, string Permission, string Status);
```

**New files:** `LocalFinanceManager/Services/IAdminService.cs` + `AdminService.cs`

```csharp
public interface IAdminService
{
    Task<List<UserSummaryResponse>> GetAllUsersAsync(CancellationToken ct = default);
    Task<UserSharesResponse> GetUserSharesAsync(Guid userId, CancellationToken ct = default);
    Task ToggleAdminAsync(Guid targetUserId, Guid requestingUserId, CancellationToken ct = default);
    // Throws InvalidOperationException when targetUserId == requestingUserId (self-demotion)
}
```

- `GetAllUsersAsync`: query `Users` (`.Where(u => !u.IsArchived)`) with share counts via joins on `AccountShares` and `BudgetPlanShares`
  > **Note:** `User` inherits `BaseEntity` which includes `IsArchived`; this filter is intentional and correct.
- `GetUserSharesAsync`: load AccountShares + BudgetPlanShares where owner is `userId` and `!IsArchived`, include navigation props for names
- `ToggleAdminAsync`: flip `User.IsAdmin`; throw if `targetUserId == requestingUserId`
- Register as `scoped` via `ServiceCollectionExtensions.cs`

**New file:** `LocalFinanceManager/Controllers/AdminController.cs`

```csharp
[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminPolicy")]   // ← 403 for non-admins
public class AdminController : ControllerBase
{
    // GET  /api/admin/users
    // GET  /api/admin/users/{id}/shares
    // POST /api/admin/users/{id}/toggle-admin  → 400 on self-demotion
}
```

### Phase 3: User Management UI

**New file:** `LocalFinanceManager/Components/Pages/Admin/UserManagement.razor`

```csharp
@page "/admin/users"
@layout AdminLayout
// Wrapped in <AdminRouteGuard>
// Table of users with IsAdmin badge and Toggle Admin button per row
// Toggle Admin: confirmation modal, disabled for current user
// Expandable rows for shares
// Revoke button per share via HTTP DELETE to existing endpoints
// Confirmation modal via ConfirmRevoke state
// data-testid attributes on all interactive elements
```

## Implementation Tasks

### Phase 0: Admin Role Model & Authorization

- [ ] Add `bool IsAdmin` property to `Models/User.cs`
- [ ] Add EF Core migration for `IsAdmin` column
- [ ] Add `Seed:AdminEmail` config key to `appsettings.json` + `appsettings.Development.json`
- [ ] Add startup seed logic in `Program.cs` to promote first admin
- [ ] Add `Task<bool> IsAdminAsync()` to `Services/IUserContext.cs`
- [ ] Implement `IsAdminAsync()` in `Services/UserContext.cs` (DB lookup; returns `false` for `Guid.Empty` or unknown user)
- [ ] Create `Services/Authorization/IsAdminRequirement.cs`
- [ ] Create `Services/Authorization/IsAdminHandler.cs` (resolves `IsAdmin` from DB; `context.Fail()` → 403)
- [ ] Register `IsAdminHandler` and `"AdminPolicy"` in `Program.cs`
- [ ] Create `Components/Shared/AdminRouteGuard.razor` (calls `IsAdminAsync()`; redirects non-admins to `/`)
- [ ] Update `NavMenu.razor` to hide "Admin" link via `IsAdminAsync()`

### Phase 1: AdminLayout + Tab Bar

- [ ] Create `Components/Layout/AdminLayout.razor` with Bootstrap nav-tabs (conditional on admin)
- [ ] Add active tab detection via `NavigationManager.Uri`
- [ ] Add `@layout AdminLayout` + `<AdminRouteGuard>` to Settings.razor
- [ ] Add `@layout AdminLayout` + `<AdminRouteGuard>` to ML.razor
- [ ] Add `@layout AdminLayout` + `<AdminRouteGuard>` to Monitoring.razor
- [ ] Remove `/admin/autoapply` route from AutoApplySettings.razor; keep only `/settings/auto-apply`
- [ ] Create `Components/Pages/Admin/AutoApplyRedirect.razor` — `@page "/admin/autoapply"`, calls `NavigationManager.NavigateTo("/settings/auto-apply", replace: true)` in `OnInitializedAsync`; renders no UI
- [ ] Update NavMenu.razor: 1 "Admin" link (hidden for non-admins) + "Auto-Apply" link for all users

### Phase 2: Backend

- [ ] Create `DTOs/AdminDTOs.cs` with `UserSummaryResponse` (incl. `IsAdmin`), `UserSharesResponse`, `AccountShareDetail`, `BudgetPlanShareDetail`
- [ ] Create `Services/IAdminService.cs`
- [ ] Create `Services/AdminService.cs` with `GetAllUsersAsync`, `GetUserSharesAsync`, `ToggleAdminAsync`
- [ ] Register `AdminService` as scoped service in `ServiceCollectionExtensions.cs`
- [ ] Create `Controllers/AdminController.cs` with `[Authorize(Policy = "AdminPolicy")]`:
  - `GET /api/admin/users`
  - `GET /api/admin/users/{id}/shares`
  - `POST /api/admin/users/{id}/toggle-admin` (400 on self-demotion)

### Phase 3: User Management UI

- [ ] Create `Components/Pages/Admin/UserManagement.razor` (wrapped in `<AdminRouteGuard>`)
- [ ] Implement user table with all columns including IsAdmin badge
- [ ] Implement Toggle Admin button (disabled for current user) + confirmation modal
- [ ] Implement expandable row for shares
- [ ] Implement Revoke button + confirmation modal
- [ ] Implement inline error alerts for failed toggle-admin and failed revoke (auto-dismiss 5s)
- [ ] Add empty state and loading spinner
- [ ] Add `data-testid` attributes to all interactive elements

### Phase 4: Tests

- [ ] Unit tests for `AdminService.GetAllUsersAsync()` — in-memory SQLite, various user/share combinations
- [ ] Unit tests for `AdminService.GetUserSharesAsync()` — verify correct joins and filtering
- [ ] Unit tests for `AdminService.ToggleAdminAsync()` — verify toggle and self-demotion guard
- [ ] Unit tests for `IsAdminHandler` — admin and non-admin claims scenarios
- [ ] Integration tests for `GET /api/admin/users` — non-admin request → 403; admin → 200
- [ ] Integration tests for `POST /api/admin/users/{id}/toggle-admin` — self-demotion → 400
- [ ] E2E test: non-admin visits `/admin/settings` → redirected to `/`
- [ ] E2E test: admin visits `/admin/settings` → tab bar visible with 4 tabs, Settings tab active
- [ ] E2E test: click each admin tab → correct route, correct tab highlighted
- [ ] E2E test: non-admin visits `/settings/auto-apply` → page loads (no redirect)
- [ ] E2E test: `/admin/autoapply` redirects to `/settings/auto-apply`
- [ ] E2E test: `/admin/users` → user table loads, IsAdmin badge visible, expand row shows shares
- [ ] E2E test: Toggle Admin → confirmation → badge updates inline
- [ ] E2E test: Revoke button → confirmation → share disappears from list

## Testing

### Unit Tests (`LocalFinanceManager.Tests/Services/AdminServiceTests.cs`)

- Seed 3 users with varying account and share counts
- Verify `GetAllUsersAsync()` returns correct counts and `IsAdmin` flag
- Verify `GetUserSharesAsync()` filters by correct userId and `!IsArchived`
- Verify `ToggleAdminAsync()` flips `IsAdmin` correctly
- Verify `ToggleAdminAsync()` throws when `targetUserId == requestingUserId`
- Use in-memory SQLite via `TestDbContextFactory`

### Unit Tests (`LocalFinanceManager.Tests/Authorization/IsAdminHandlerTests.cs`)

- Admin user → `context.Succeed()` called
- Non-admin user → `context.Fail()` called
- Unauthenticated (no sub claim) → `context.Fail()` called

### Unit Tests (`LocalFinanceManager.Tests/Services/UserContextIsAdminTests.cs`)

- Admin user in DB → `IsAdminAsync()` returns `true`
- Non-admin user in DB → `IsAdminAsync()` returns `false`
- `GetCurrentUserId()` returns `Guid.Empty` (unauthenticated) → `IsAdminAsync()` returns `false`

### Integration Tests

- `GET /api/admin/users` as non-admin → HTTP 403
- `GET /api/admin/users` as admin with seeded data → HTTP 200 + correct JSON structure
- `GET /api/admin/users/{id}/shares` → HTTP 200 + shares grouped by type
- `GET /api/admin/users/{unknown-id}/shares` → HTTP 404
- `POST /api/admin/users/{id}/toggle-admin` with self → HTTP 400
- `POST /api/admin/users/{id}/toggle-admin` with other user → HTTP 200 + IsAdmin flipped

### E2E Tests (`LocalFinanceManager.E2E/Tests/AdminPanelTests.cs`)

```csharp
// AdminPanelTests.cs
// - NonAdmin_VisitAdminUrl_RedirectsToHome
// - Navigate_ToAdminSettings_ShowsTabBar  (4 tabs)
// - ClickEachTab_NavigatesToCorrectRoute
// - ClickUsersTab_LoadsUserTable
// - ToggleAdmin_ConfirmDialog_BadgeUpdates
// - ToggleAdmin_SelfButton_IsDisabled
// - ExpandUserRow_ShowsActiveShares
// - RevokeShare_ConfirmDialog_ShareDisappears
// - NonAdmin_VisitAutoApply_PageLoads
// - LegacyAdminAutoApplyRoute_RedirectsToSettings
```

**data-testid attributes:**

- `data-testid="admin-tab-settings"`, `"admin-tab-ml"`, `"admin-tab-monitoring"`, `"admin-tab-users"`
- `data-testid="user-row-{userId}"`
- `data-testid="admin-badge-{userId}"`
- `data-testid="toggle-admin-{userId}"`
- `data-testid="confirm-toggle-admin-button"`
- `data-testid="expand-user-{userId}"`
- `data-testid="revoke-share-{shareId}"`
- `data-testid="confirm-revoke-button"`

## Success Criteria

- [ ] Non-admin visiting any `/admin/*` URL is redirected to `/`
- [ ] Admin nav item hidden from NavMenu for non-admin users
- [ ] All `/api/admin/*` endpoints return HTTP 403 for non-admins
- [ ] Navigating to `/admin/settings` as admin shows tab bar with 4 tabs; Settings tab is active
- [ ] `/admin/autoapply` redirects to `/settings/auto-apply` (backward compat)
- [ ] Non-admin can access `/settings/auto-apply` without redirect
- [ ] `/admin/users` loads all users with `IsAdmin` badge and correct share counts
- [ ] Admin can toggle another user's admin status; row updates inline
- [ ] Self-demotion toggle button is disabled in UI; API returns HTTP 400
- [ ] Revoking a share via the UI is persisted to the database
- [ ] NavMenu has one "Admin" link instead of multiple separate admin links
- [ ] All existing admin routes still work without any behavior changes
- [ ] Unit + integration + E2E tests pass for all authorization and admin management scenarios

## Scope Exclusions

- No user deletion — share revocation only
- No editable user profiles
- Backup (`/backup`) remains its own nav link; not included in admin tabs
