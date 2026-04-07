# UserStory-16: Design Overhaul

**As** a user of Local Finance Manager  
**I want** a modern, professional, and consistent UI  
**So that** I can use the application comfortably and confidently on any device, in both light and dark mode

## Problem

The current design is the default Blazor/Bootstrap output without a custom visual identity. It looks basic, lacks consistency, and is not optimised for mobile use.

## Business Value

- **User experience:** A professional look increases trust in financial data
- **Consistency:** Uniform components reduce the learning curve and minimise errors
- **Accessibility:** Mobile-first layout makes the app usable on any device
- **Retention:** An attractive UI encourages daily use

---

## Design System — Finance Blue

### Colour Palette (CSS custom properties)

```css
:root {
  /* Brand */
  --color-primary: #1e3a5f; /* Sidebar, headers, primary buttons */
  --color-accent: #3b82f6; /* Links, highlights, focus states */

  /* Status */
  --color-success: #10b981; /* Positive balances, confirmations */
  --color-danger: #ef4444; /* Negative balances, errors */
  --color-warning: #f59e0b; /* Warnings, alerts */

  /* Surface */
  --color-bg: #f8fafc; /* Page background */
  --color-surface: #ffffff; /* Cards, modals, dropdowns */
  --color-border: #e2e8f0; /* Borders, dividers */

  /* Typography */
  --color-text: #1e293b; /* Main text */
  --color-muted: #64748b; /* Subtext, labels, placeholders */

  /* Dark mode (via [data-theme="dark"] or prefers-color-scheme) */
  --color-bg-dark: #0f172a;
  --color-surface-dark: #1e293b;
  --color-border-dark: #334155;
  --color-text-dark: #f1f5f9;
  --color-muted-dark: #94a3b8;
}
```

### Typography

- **Font stack:** `Inter, 'Segoe UI', system-ui, sans-serif`
- **Headings:** semibold, `--color-primary`
- **Body:** regular, `--color-text`, line-height 1.6
- **Monospace (amounts):** `'JetBrains Mono', monospace`
- **Loading:** Google Fonts CDN — add to `<head>` in `App.razor`:
  ```html
  <link rel="preconnect" href="https://fonts.googleapis.com" />
  <link
    href="https://fonts.googleapis.com/css2?family=Inter:wght@400;600&family=JetBrains+Mono:wght@400&display=swap"
    rel="stylesheet"
  />
  ```

---

## ThemeService Specification

Theme preference is stored **per user in the database** via a `UserPreferences` entity. JS interop is used only to apply the `[data-theme]` attribute to the DOM.

### `UserPreferences` Entity

```csharp
public class UserPreferences : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Theme { get; set; } = "light"; // "light" | "dark"
}
```

EF Core: configure `UserId` as unique index. Migration auto-applied on startup.

### `IUserPreferencesService`

```csharp
public interface IUserPreferencesService
{
    Task<UserPreferences?> GetAsync(string userId);
    Task SetThemeAsync(string userId, string theme);
}
```

Registered as Scoped in `ServiceCollectionExtensions.cs`.

### `IThemeService`

```csharp
public interface IThemeService
{
    string CurrentTheme { get; }              // "light" | "dark"
    Task InitialiseAsync(string? userId);     // Load from DB if authenticated, else OS preference
    Task ToggleAsync(string userId);          // Persist to DB + apply to DOM
    event Action? ThemeChanged;              // Blazor components subscribe to re-render
}
```

**`InitialiseAsync` logic:**

1. If `userId` is null → call `window.theme.getOsPreference()`
2. Else → call `IUserPreferencesService.GetAsync(userId)`
3. If no record found → call `window.theme.getOsPreference()`
4. Apply via `window.theme.set(resolvedTheme)`, set `CurrentTheme`, raise `ThemeChanged`

**`ToggleAsync` logic:**

1. Flip `CurrentTheme` (light ↔ dark)
2. Call `IUserPreferencesService.SetThemeAsync(userId, newTheme)`
3. Call `window.theme.set(newTheme)` to update DOM
4. Raise `ThemeChanged`

### JS Interop — `wwwroot/js/theme.js`

DOM-only (no localStorage):

```js
window.theme = {
  set: (value) => document.documentElement.setAttribute("data-theme", value),
  getOsPreference: () =>
    window.matchMedia("(prefers-color-scheme: dark)").matches
      ? "dark"
      : "light",
};
```

Add `<script src="js/theme.js"></script>` to `App.razor`.

---

## Pages in Scope (All 18 Pages)

**Auth:** `Login.razor`, `Register.razor`, `Logout.razor`, `PasswordReset.razor`  
**Accounts:** `Accounts.razor`, `AccountCreate.razor`, `AccountEdit.razor`  
**Transactions:** `Transactions.razor`, `TransactionImport.razor`  
**Budget:** `BudgetPlans.razor`, `BudgetPlanCreate.razor`, `BudgetPlanEdit.razor`  
**Categories:** `CategoryCreate.razor`, `CategoryEdit.razor`  
**Dashboard:** `Home.razor`  
**System:** `Error.razor`, `NotFound.razor`  
**Admin/Sharing:** all pages under `Admin/` and `Sharing/` subdirectories

---

## Implementation Phases

### Phase A — CSS Foundation & Layout _(prerequisite for Phase B)_

#### A1. Theme & CSS Variables

- [x] Define all CSS custom properties in `wwwroot/app.css`
- [x] Add `[data-theme="dark"]` variants for all surface/text tokens in `app.css`
- [x] Override Bootstrap 5 variables (`$primary`, `$body-bg`, etc.) via CSS overrides
- [x] Add Google Fonts CDN link to `App.razor` (Inter + JetBrains Mono)

#### A2. Dark Mode — Data Layer

- [x] Add `UserPreferences` entity inheriting `BaseEntity` with `UserId` and `Theme` properties
- [x] Configure EF Core: unique index on `UserId`, in `AppDbContext`
- [x] Add EF Core migration (auto-applied on startup)
- [x] Implement `IUserPreferencesService` / `UserPreferencesService` (Scoped)
- [x] Register `UserPreferencesService` in `ServiceCollectionExtensions.cs`

#### A2. Dark Mode — UI Layer

- [x] Create `wwwroot/js/theme.js` with `window.theme.set` and `window.theme.getOsPreference`
- [x] Add `<script src="js/theme.js"></script>` to `App.razor`
- [x] Implement `IThemeService` / `ThemeService` per spec above (Scoped)
- [x] Register `ThemeService` in `ServiceCollectionExtensions.cs`
- [x] Call `ThemeService.InitialiseAsync(userId)` in `MainLayout.razor` `OnAfterRenderAsync`

#### A3. Sidebar & Navigation

- [x] Restyle sidebar to `--color-primary` (#1E3A5F) with white icons and text (`NavMenu.razor.css`)
- [x] Active nav item: accent-colour background + bold text
- [x] Responsive: hamburger button on mobile (< 768px), overlay sidebar that closes after navigation
- [ ] Persist sidebar collapse state via `localStorage`
- [x] Update `MainLayout.razor`: top bar with username, dark mode toggle (`bi-sun`/`bi-moon`), and logout button

---

### Phase B — Pages & Components _(depends on Phase A)_

#### B4. Dashboard (Home.razor)

- [x] Replace current content with a KPI card row:
  - Total balance (all accounts, colour-coded positive/negative)
  - Uncategorised transactions (warning-coloured badge when > 0)
  - Active budget plans
  - Transactions this month
- [x] Add "Quick Actions" section: Add Account, Import Transactions, Create Budget Plan
- [ ] Add "Recent Activity" widget (last 5 transactions)
- [ ] Responsive 2-column layout on tablet, 1-column on mobile

#### B5. Consistent Component Patterns

- [x] **Cards:** All sections in a card with `border-radius: 12px`, subtle shadow, `--color-surface` background
- [x] **Tables:** Remove default Bootstrap striped; use hover highlight, sticky header
- [x] **Buttons:** Primary button in `--color-primary`, accent hover; danger in `--color-danger`
- [x] **Forms:** Styled labels with focus ring in `--color-accent`
- [x] **Badges/chips:** Category labels as coloured pill badges
- [x] **Alerts:** Custom style for success/warning/danger with icon on the left
- [x] **Empty states:** Illustration + description + action button for empty lists

#### B6. Transactions & Accounts Pages

- [x] Transaction amounts: `font-family: monospace`, green/red for positive/negative
- [x] Account cards on `Accounts.razor`: card grid instead of table
- [x] Filters and search bar: styled pill buttons for quick filters

#### B7. Mobile-First Responsiveness

- [x] All tables: horizontally scrollable or converted to card list on small screens
- [x] Minimum touch target: 44×44px for all interactive elements
- [x] Test at viewports 375px (iPhone SE), 768px (iPad), 1280px (desktop)

#### B8. Maintain Accessibility

- [x] Focus-visible ring remains visible (2px `--color-accent` outline)
- [x] Colour contrast ratio for primary text ≥ 4.5:1 (WCAG AA)
- [x] Dark mode contrast ≥ 4.5:1
- [x] No information conveyed by colour alone (icon or text always present)

---

## E2E Test Scenarios

New scenarios to add in `LocalFinanceManager.E2E/`:

**Dark Mode**

1. First visit (no DB preference), OS dark → app loads in dark mode
2. First visit (no DB preference), OS light → app loads in light mode
3. Authenticated user toggles dark mode → page switches theme
4. Authenticated user toggles dark mode → refreshes page → toggled theme persists (from DB)
5. Toggle twice → returns to original theme

**Responsive Navigation**

1. Viewport 375px → hamburger button visible, sidebar hidden
2. Click hamburger → sidebar overlay opens
3. Click a nav item → sidebar closes, correct page loads
4. Viewport 1280px → sidebar always visible, hamburger not present

---

## Acceptance Criteria

- [ ] All 18 pages use the Finance Blue colour palette
- [x] Sidebar is dark blue (#1E3A5F) with white text
- [x] Dark mode preference is stored in `UserPreferences` table in the database
- [x] Unauthenticated users see OS preference (`prefers-color-scheme`)
- [x] Dark mode toggle persists after page refresh (loaded from DB)
- [x] `IThemeService` implemented with `InitialiseAsync`, `ToggleAsync`, `ThemeChanged` per spec
- [x] `wwwroot/js/theme.js` exists with `window.theme.set` and `window.theme.getOsPreference`
- [x] Dashboard shows at least 4 KPI cards
- [x] Tables and cards are readable at 375px viewport
- [x] Hamburger nav functional at < 768px
- [ ] Lighthouse Accessibility score ≥ 90 maintained
- [x] All 9 new e2e test scenarios pass
- [ ] Existing e2e tests pass without regressions
- [x] No `bin/`, `obj/`, or other build artifacts committed

## Definition of Done

- All Phase A and Phase B tasks checked off
- `UserPreferences` entity + migration in place
- `IUserPreferencesService` and `IThemeService` implemented per spec
- `wwwroot/js/theme.js` created with specified functions
- Code review approved
- E2E tests green (existing + 9 new scenarios)
- Visual inspection on desktop (light + dark), tablet, and mobile
