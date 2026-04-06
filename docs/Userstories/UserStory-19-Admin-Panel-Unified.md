# UserStory-19: Unified Admin Panel

**As** an administrator of Local Finance Manager  
**I want** to manage all administrative settings in one consolidated tabbed panel  
**So that** I can maintain the application efficiently without navigating between scattered pages

## Business Value

- **Consolidation:** All admin functions in one place — no scattered navigation paths
- **User management:** Visibility into registered users and their active shared resources
- **Share revocation:** Admin can revoke shared connections without needing the owner to do so
- **Maintainability:** Centralized management reduces cognitive load during application maintenance

## Acceptance Criteria

### Tab Bar Navigation

- [ ] Admin section displays a persistent Bootstrap nav-tabs bar with 5 tabs:
  - **Settings** → `/admin/settings`
  - **Auto-Apply** → `/admin/autoapply`
  - **ML Models** → `/admin/ml`
  - **Monitoring** → `/admin/monitoring`
  - **Users** → `/admin/users`
- [ ] Active tab is highlighted based on the current URL (`NavigationManager.Uri`)
- [ ] Tab navigation works via clicks and direct URL entry
- [ ] NavMenu consolidated to a single "Admin" menu item → navigates to `/admin/settings`
- [ ] Existing admin routes remain backward-compatible (`/admin/autoapply`, `/admin/monitoring` etc.)

### User Management Tab (`/admin/users`)

- [ ] Displays a table of all registered users with columns:
  - **Email** (sortable)
  - **Display Name**
  - **Member since** (CreatedAt, relative date + absolute tooltip)
  - **Accounts** (number of accounts owned)
  - **Shares given** (number of active shares the user has given out)
  - **Shares received** (number of active shares the user has received)
- [ ] Clicking a user row expands a detail panel showing active shares:
  - Shared accounts: account name, recipient email, permission level, status
  - Shared budget plans: plan name, recipient email, permission level, status
- [ ] **Revoke button** per share entry: calls existing `DELETE /api/shares/accounts/{id}` / `DELETE /api/shares/budgetplans/{id}`
- [ ] Confirmation modal before revoke ("Are you sure you want to revoke this share?")
- [ ] Success message after revoke; share row disappears from expanded panel
- [ ] Empty state: "No users found"
- [ ] Loading spinner during data fetch

### Responsiveness & Layout

- [ ] Tab bar is mobile-friendly (horizontal scroll on narrow screens)
- [ ] User table renders correctly on mobile (columns shrink/stack)

## Technical Implementation

### Phase 1: AdminLayout + Tab Bar

**New file:** `LocalFinanceManager/Components/Layout/AdminLayout.razor`

```csharp
// Blazor layout that wraps the existing MainLayout sidebar
// Adds Bootstrap nav-tabs above the page content area
// Detects active tab via NavigationManager.Uri.Contains("/admin/...")
// Tabs: Settings, Auto-Apply, ML Models, Monitoring, Users
```

**Modified files:**

- `Components/Pages/Admin/Settings.razor` — add `@layout AdminLayout`
- `Components/Pages/Admin/AutoApplySettings.razor` — add `@layout AdminLayout`
- `Components/Pages/Admin/ML.razor` — add `@layout AdminLayout`
- `Components/Pages/Admin/Monitoring.razor` — add `@layout AdminLayout`
- `Components/Layout/NavMenu.razor` — replace 4 separate admin links with 1 "Admin" link to `/admin/settings`

### Phase 2: User Management Backend

**New file:** `LocalFinanceManager/DTOs/AdminDTOs.cs`

```csharp
public record UserSummaryResponse(
    Guid Id,
    string Email,
    string DisplayName,
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
}
```

- `GetAllUsersAsync`: query `Users` (`.Where(u => !u.IsArchived)`) with share counts via joins on `AccountShares` and `BudgetPlanShares`
- `GetUserSharesAsync`: load AccountShares + BudgetPlanShares where owner is `userId` and `!IsArchived`, include navigation props for names
- Register as `scoped` via `ServiceCollectionExtensions.cs`

**New file:** `LocalFinanceManager/Controllers/AdminController.cs`

```csharp
[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminController : ControllerBase
{
    // GET /api/admin/users
    // GET /api/admin/users/{id}/shares
}
```

### Phase 3: User Management UI

**New file:** `LocalFinanceManager/Components/Pages/Admin/UserManagement.razor`

```csharp
@page "/admin/users"
@layout AdminLayout
// Table of users with expandable rows for shares
// Revoke button per share via HTTP DELETE to existing endpoints
// Confirmation modal via ConfirmRevoke state
// data-testid attributes on all interactive elements
```

## Implementation Tasks

### Phase 1: AdminLayout + Tab Bar

- [ ] Create `Components/Layout/AdminLayout.razor` with Bootstrap nav-tabs
- [ ] Add active tab detection via `NavigationManager.Uri`
- [ ] Add `@layout AdminLayout` to Settings.razor
- [ ] Add `@layout AdminLayout` to AutoApplySettings.razor
- [ ] Add `@layout AdminLayout` to ML.razor
- [ ] Add `@layout AdminLayout` to Monitoring.razor
- [ ] Update NavMenu.razor: replace separate admin links with 1 "Admin" link

### Phase 2: Backend

- [ ] Create `DTOs/AdminDTOs.cs` with `UserSummaryResponse`, `UserSharesResponse`, `AccountShareDetail`, `BudgetPlanShareDetail`
- [ ] Create `Services/IAdminService.cs`
- [ ] Create `Services/AdminService.cs` with `GetAllUsersAsync` and `GetUserSharesAsync`
- [ ] Register `AdminService` as scoped service in `ServiceCollectionExtensions.cs`
- [ ] Create `Controllers/AdminController.cs` with `GET /api/admin/users` and `GET /api/admin/users/{id}/shares`

### Phase 3: User Management UI

- [ ] Create `Components/Pages/Admin/UserManagement.razor`
- [ ] Implement user table with all columns
- [ ] Implement expandable row for shares
- [ ] Implement Revoke button + confirmation modal
- [ ] Add empty state and loading spinner
- [ ] Add `data-testid` attributes to all interactive elements

### Phase 4: Tests

- [ ] Unit tests for `AdminService.GetAllUsersAsync()` — in-memory SQLite, various user/share combinations
- [ ] Unit tests for `AdminService.GetUserSharesAsync()` — verify correct joins and filtering
- [ ] Integration tests for `GET /api/admin/users` — seed users + shares, verify response structure
- [ ] E2E test: navigate to `/admin/settings` → tab bar visible, Settings tab active
- [ ] E2E test: click each tab → correct route, correct tab highlighted
- [ ] E2E test: `/admin/users` → user table loads, expand row shows shares
- [ ] E2E test: Revoke button → confirmation → share disappears from list

## Testing

### Unit Tests (`LocalFinanceManager.Tests/Services/AdminServiceTests.cs`)

- Seed 3 users with varying account and share counts
- Verify `GetAllUsersAsync()` returns correct counts
- Verify `GetUserSharesAsync()` filters by correct userId and `!IsArchived`
- Use in-memory SQLite via `TestDbContextFactory`

### Integration Tests

- `GET /api/admin/users` with seeded data → HTTP 200 + correct JSON structure
- `GET /api/admin/users/{id}/shares` → HTTP 200 + shares grouped by type
- `GET /api/admin/users/{unknown-id}/shares` → HTTP 404

### E2E Tests (`LocalFinanceManager.E2E/Tests/AdminPanelTests.cs`)

```csharp
// AdminPanelTests.cs
// - Navigate_ToAdminSettings_ShowsTabBar
// - ClickAutoApplyTab_NavigatesToCorrectRoute
// - ClickUsersTab_LoadsUserTable
// - ExpandUserRow_ShowsActiveShares
// - RevokeShare_ConfirmDialog_ShareDisappears
```

**data-testid attributes:**

- `data-testid="admin-tab-settings"`, `"admin-tab-autoapply"`, `"admin-tab-ml"`, `"admin-tab-monitoring"`, `"admin-tab-users"`
- `data-testid="user-row-{userId}"`
- `data-testid="expand-user-{userId}"`
- `data-testid="revoke-share-{shareId}"`
- `data-testid="confirm-revoke-button"`

## Success Criteria

- [ ] Navigating to `/admin/settings` shows tab bar with 5 tabs; Settings tab is active
- [ ] Directly navigating to `/admin/autoapply` correctly activates the Auto-Apply tab
- [ ] `/admin/users` loads all users with correct share counts
- [ ] Revoking a share via the UI is persisted to the database
- [ ] NavMenu has one "Admin" link instead of multiple separate admin links
- [ ] All existing admin routes still work without any behavior changes
- [ ] Unit + integration tests pass for AdminService
- [ ] E2E tests pass for tab navigation and user management flow

## Scope Exclusions

- No role-based access control for the admin section (future story)
- No user deletion — share revocation only
- No editable user profiles
- Backup (`/backup`) remains its own nav link; not included in admin tabs
