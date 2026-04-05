# UserStory-15: Application Flow & Onboarding

**As** an authenticated user of Local Finance Manager  
**I want** a proper dashboard, guided onboarding, breadcrumb navigation, and clear permission labels on shared resources  
**So that** I can orient myself quickly, complete initial setup without confusion, and always know where I am and what I can do

---

## Problem

- `Home.razor` is a placeholder ("Hello, world!") — authenticated users have no landing page with useful data.
- New users land on a blank app with no guidance on how to create accounts, budget plans, or categories.
- Most pages lack `[Authorize]` — unauthenticated users can navigate directly to protected routes.
- `Breadcrumb.razor` exists but has to be wired manually per page with static lists; UUID segments are unreadable.
- Shared resources show no permission level or owner label — users cannot tell what they own vs. what was shared with them.

---

## Business Value

- **Retention:** A useful home screen (dashboard) encourages daily use.
- **Time-to-value:** Guided onboarding reduces time from registration to first transaction assignment.
- **Security hygiene:** Proper auth guards prevent data leakage to unauthenticated visitors.
- **Discoverability:** Auto-generated breadcrumbs reduce reliance on the browser back button.
- **Trust:** Clear ownership and permission labels prevent accidental edits on shared resources.

---

## Scope Boundaries

- **No** `HasCompletedOnboarding` DB flag — onboarding trigger is 0 accounts after login (no migration needed).
- **No** `AuthGuard` component — existing `[Authorize]` attribute pattern is sufficient.
- **No** `ResourceCard.razor` generic abstraction — permission badges implemented inline.
- **No** analytics integration.
- `Breadcrumb.razor` (leaf renderer) already exists in `Components/Shared/` — reuse it; do **not** replace it.

---

## Phase 1 — Dashboard (replaces Home.razor)

### Acceptance Criteria

- Unauthenticated visitors see a hero section with **Login** and **Register** CTA buttons; no financial data is exposed.
- Authenticated users see a responsive widget grid (2-column on desktop, 1-column on mobile) and a quick-actions bar.
- Dashboard loads in **< 2 seconds** with all 5 widgets (measured via Playwright `page.waitForLoadState('networkidle')`).

### Widget Specifications

| Widget component | Data source | Content |
|---|---|---|
| `AccountSummaryWidget.razor` | `AccountService` | Total balance across all accounts; month-over-month change (e.g. "+5.3% ↑"); list of top-3 accounts by balance |
| `BudgetStatusWidget.razor` | `BudgetPlanService` + `IBudgetLineRepository` | Current-month budget utilisation percentage; progress bar per category (top 5 by spend); over-budget categories highlighted in red |
| `RecentTransactionsWidget.razor` | `ITransactionRepository` | Last 10 transactions grouped by **Today / Yesterday / This Week**; each row shows an "Assign" quick-action button when the transaction is unassigned |
| `UncategorizedAlertWidget.razor` | `ITransactionRepository` | Count of unassigned transactions (e.g. "⚠ 12 transactions need assignment"); "Assign Now" button navigates to `/transactions?filter=unassigned` |
| `MLSuggestionWidget.razor` | `ISuggestionService` / `/api/suggestions` | Count of pending ML suggestions (e.g. "🤖 5 suggestions available"); "Review Suggestions" button navigates to `/transactions?filter=has-suggestion` |

### Quick-Actions Bar

Four buttons rendered below the widget grid:

- **Create Account** → `/accounts/new`
- **Create Budget Plan** → `/budgets/new`
- **Import Transactions** → `/transactions/import`
- **View All Transactions** → `/transactions`

### Implementation Tasks

- [ ] Replace `Components/Pages/Home.razor` body with the dashboard layout
- [ ] Add `<AuthorizeView>` split: `<Authorized>` = widgets + quick-actions; `<NotAuthorized>` = hero with CTA buttons
- [ ] Create `Components/Pages/Dashboard/AccountSummaryWidget.razor`
- [ ] Create `Components/Pages/Dashboard/BudgetStatusWidget.razor`
- [ ] Create `Components/Pages/Dashboard/RecentTransactionsWidget.razor`
- [ ] Create `Components/Pages/Dashboard/UncategorizedAlertWidget.razor`
- [ ] Create `Components/Pages/Dashboard/MLSuggestionWidget.razor`
- [ ] Implement CSS Grid layout (2-col desktop / 1-col mobile) in `Home.razor`

---

## Phase 2 — Authentication Guards

### Acceptance Criteria

- Every protected page redirects an unauthenticated visitor to `/login?returnUrl=<intended-path>`.
- After successful login, the user is sent to the intended page (already handled by `RedirectToLogin.razor` + `Login.razor`).

### Pages Requiring `[Authorize]`

The following pages currently **lack** the `[Authorize]` attribute:

| Page | File |
|---|---|
| Accounts list | `Components/Pages/Accounts.razor` |
| Create Account | `Components/Pages/AccountCreate.razor` |
| Edit Account | `Components/Pages/AccountEdit.razor` |
| Transactions | `Components/Pages/Transactions.razor` |
| Import Transactions | `Components/Pages/TransactionImport.razor` |
| Budget Plans | `Components/Pages/BudgetPlans.razor` |
| Create Budget Plan | `Components/Pages/BudgetPlanCreate.razor` |
| Edit Budget Plan | `Components/Pages/BudgetPlanEdit.razor` |
| Create Category | `Components/Pages/CategoryCreate.razor` |
| Edit Category | `Components/Pages/CategoryEdit.razor` |
| Admin Settings | `Components/Pages/Admin/Settings.razor` |
| Auto-Apply Settings | `Components/Pages/Admin/AutoApplySettings.razor` |
| Monitoring | `Components/Pages/Admin/Monitoring.razor` |
| ML Info | `Components/Pages/Admin/ML.razor` |

> `Backup.razor` and `SharedWithMe.razor` already carry `[Authorize]` — do not duplicate.

### Implementation Tasks

- [ ] Add `@attribute [Authorize]` to each page listed above
- [ ] Verify `<NotAuthorized><RedirectToLogin /></NotAuthorized>` is present in `Routes.razor` or each page's `<AuthorizeView>` block as a fallback

---

## Phase 3 — Auto-Generated Breadcrumbs

### Design

`Breadcrumb.razor` (leaf renderer, already exists) is untouched. A new scoped service parses the current URL and notifies `MainLayout.razor` to re-render.

```
IBreadcrumbService
  ├── IReadOnlyList<BreadcrumbItem> Items  (current trail)
  ├── event Action? OnChange               (fires on navigation or manual override)
  └── void SetSectionTitle(string segmentOrId, string title)
       // Pages call this to name UUID or custom segments
       // e.g. BreadcrumbService.SetSectionTitle(Id.ToString(), budgetPlan.Name)
```

### URL → Breadcrumb Mapping (static dictionary)

| URL segment | Breadcrumb label |
|---|---|
| *(root `/`)* | Home |
| `accounts` | Accounts |
| `budgets` | Budget Plans |
| `transactions` | Transactions |
| `admin` | Admin |
| `sharing` | Sharing |
| `backup` | Backup & Restore |
| `onboarding` | Onboarding |
| `new` | New |
| `edit` | Edit |
| `import` | Import |
| `shared-with-me` | Shared with Me |
| *(GUID)* | title registered via `SetSectionTitle`, else "Details" |

### Example Trails

- `/accounts` → `Home / Accounts`
- `/accounts/{guid}/edit` → `Home / Accounts / {AccountName} / Edit`
- `/budgets/{guid}/categories/new` → `Home / Budget Plans / {PlanName} / Categories / New`
- `/admin/monitoring` → `Home / Admin / Monitoring`

### Implementation Tasks

- [ ] Create `Services/IBreadcrumbService.cs`
- [ ] Create `Services/BreadcrumbService.cs` (scoped):
  - Subscribe to `NavigationManager.LocationChanged` in constructor; reset registered titles on navigation; parse segments; raise `OnChange`
- [ ] Register `BreadcrumbService` as `Scoped` in `Extensions/ServiceCollectionExtensions.cs`
- [ ] Update `Components/Layout/MainLayout.razor`: inject `IBreadcrumbService`, subscribe to `OnChange`, render `<Breadcrumb Items="@_breadcrumbs" />` in the top-row
- [ ] Migrate `Components/Pages/CategoryCreate.razor`: remove manual `<Breadcrumb Items=...>`, inject `IBreadcrumbService`, call `BreadcrumbService.SetSectionTitle(budgetPlanId.ToString(), budgetPlan.Name)` in `OnInitializedAsync`
- [ ] Migrate `Components/Pages/CategoryEdit.razor`: same as above
- [ ] Migrate `Components/Pages/Admin/Monitoring.razor`: remove manual `<Breadcrumb>` tag

---

## Phase 4 — Onboarding Wizard

### Trigger

In `Login.razor`, after successful sign-in, check account count:

```csharp
var count = await AccountService.GetActiveAccountCountAsync(userId);
Navigation.NavigateTo(count == 0 ? "/onboarding" : destination);
```

On `Onboarding.razor` load, if user already has accounts → `Navigation.NavigateTo("/")`.

### Wizard Steps

| # | Title | Key fields / actions |
|---|---|---|
| 1 | Welcome | Headline "Welcome to Local Finance Manager!"; "Get Started" → Step 2 |
| 2 | Create First Account | Account Name, IBAN (validated), Currency (ISO-4217), Initial Balance; "Next" → Step 3; "Skip" → `/` |
| 3 | Create First Budget Plan | Budget Plan Name, Start Date (today), End Date (1 year from today); "Next" → Step 4 |
| 4 | Add Categories from Template | Category template multi-select grouped by Income/Expense; all selected by default; "Create Categories" → Step 5 |
| 5 | Import Transactions (optional) | CSV upload; preview first 5 rows; "Import" → Step 6; "Skip" → Step 6 |
| 6 | Completion | Summary card: "X account, Y budget plan, Z categories created"; "Go to Dashboard" → `/` |

- Progress indicator: `Step X of 6` shown above form.
- Each step that creates data calls the appropriate service directly (no external API round-trips from wizard).
- Form validation mirrors the standalone Create pages (IBAN format, ISO-4217, required fields).

### Implementation Tasks

- [ ] Create `Components/Pages/Onboarding.razor` with `@attribute [Authorize]`
- [ ] Implement wizard step state machine in `@code` block (no session storage needed)
- [ ] Add step-guard on `OnInitializedAsync`: redirect to `/` if account count > 0
- [ ] Update `Login.razor` post-sign-in: inject `AccountService`; check count → redirect to `/onboarding` or destination
- [ ] Add `GetActiveAccountCountAsync(Guid userId)` overload to `AccountService` if not present

---

## Phase 5 — Multi-Tenant UX

### Acceptance Criteria

- Every resource in a list shows a **permission badge** (Owner / Editor / Viewer) with Bootstrap badge colours (primary / warning / secondary).
- Non-owned items show **"Shared by [email]"** in muted text below the resource name.
- Each list page has a **filter dropdown**: All / Owned by Me / Shared with Me; default = All.
- Edit and delete/archive actions are hidden for resources where the current user is not the owner or does not have Editor permission.

### Pages to Update

#### `Accounts.razor`

- [ ] Add `OwnerEmail` property to `AccountDto` (populated from `Account.User.Email`)
- [ ] Render permission badge next to account label
- [ ] Render "Shared by [email]" for non-owned accounts
- [ ] Add filter dropdown (All / Owned by Me / Shared with Me) above the table
- [ ] Hide Edit / Share / Archive buttons when permission level < Editor

#### `BudgetPlans.razor`

- [ ] Same badge, label, filter, and button-visibility treatment as `Accounts.razor`
- [ ] Add `OwnerEmail` + `PermissionLevel` to `BudgetPlanDto`

#### `SharedWithMe.razor`

- [ ] Add "Shared by [email]" label on each resource row
- [ ] Display permission level badge next to each shared item

### Implementation Tasks (shared)

- [ ] Extend `AccountDto` + `BudgetPlanDto` with `OwnerEmail` and `PermissionLevel` fields (populated in service layer)
- [ ] Update `AccountService.GetAccountsForUserAsync` to include shared accounts with permission info
- [ ] Update `BudgetPlanService` equivalently

---

## New Files

| File | Purpose |
|---|---|
| `Services/IBreadcrumbService.cs` | Interface |
| `Services/BreadcrumbService.cs` | Implementation (scoped) |
| `Components/Pages/Onboarding.razor` | 6-step wizard |
| `Components/Pages/Dashboard/AccountSummaryWidget.razor` | Dashboard widget |
| `Components/Pages/Dashboard/BudgetStatusWidget.razor` | Dashboard widget |
| `Components/Pages/Dashboard/RecentTransactionsWidget.razor` | Dashboard widget |
| `Components/Pages/Dashboard/UncategorizedAlertWidget.razor` | Dashboard widget |
| `Components/Pages/Dashboard/MLSuggestionWidget.razor` | Dashboard widget |

---

## Tests

### E2E (NUnit + Playwright)

- [ ] New user registers → logs in → redirected to `/onboarding` (0 accounts)
- [ ] Completes all 6 wizard steps → dashboard shows created account, budget plan, categories
- [ ] Clicks "Skip" on Step 2 → redirects to `/` with empty-state widgets
- [ ] Returning user with accounts → login → redirected to `/` (not `/onboarding`)
- [ ] Unauthenticated `GET /accounts` → redirected to `/login?returnUrl=%2Faccounts`
- [ ] Unauthenticated `GET /budgets` → redirected to `/login?returnUrl=%2Fbudgets`
- [ ] Navigate to `/budgets/{id}/categories/new` → breadcrumb shows "Home / Budget Plans / {PlanName} / Categories / New"
- [ ] Navigate to `/admin/monitoring` → breadcrumb shows "Home / Admin / Monitoring"
- [ ] Dashboard loads with all 5 widgets in < 2 s (`page.waitForLoadState('networkidle')`)
- [ ] Accounts list: shared account shows permission badge and "Shared by" label; filter dropdown filters correctly

### Unit

- [ ] `BreadcrumbService`: static segment mapping for all known routes
- [ ] `BreadcrumbService`: UUID segment shows registered title after `SetSectionTitle` call
- [ ] `BreadcrumbService`: trail resets on navigation event
- [ ] `BreadcrumbService`: unknown segment falls back to "Details"

---

## Success Criteria

- [ ] All existing unit + integration tests remain green after changes
- [ ] Unauthenticated users cannot reach any protected page without being redirected to `/login`
- [ ] New users complete onboarding wizard and land on a populated dashboard
- [ ] Breadcrumbs auto-render on every page without manual wiring per page (except `SetSectionTitle` calls for entity names)
- [ ] Dashboard loads in < 2 seconds with all 5 widgets
- [ ] Shared resources show permission badge and owner label on `Accounts` and `BudgetPlans` list pages

---

## Estimated Effort

**4–5 days** (after refinement)
- Dashboard provides clear overview of user's data
- Uncategorized transaction warnings are visible
- Onboarding wizard guides new users through setup
- Breadcrumbs help users navigate complex hierarchies
- Multi-tenant UI clearly shows ownership and permissions
- User experience is intuitive and polished
