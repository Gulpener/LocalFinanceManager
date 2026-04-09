# UserStory-20: Stitch Visual Redesign

**As** a user of Local Finance Manager  
**I want** a modern, premium UI inspired by the Google Stitch "WealthCurator" design  
**So that** the application feels polished, trustworthy, and pleasant to use every day

## Problem

The current UI uses the default Bootstrap 5 output with the Finance Blue design system. While functional, it lacks the premium feel of modern financial applications. The Stitch export demonstrates a significantly higher visual quality: a light sidebar, Material Design 3 colour tokens, Manrope headline typography, a bento-grid dashboard, and grouped transaction rows with icon circles.

## Business Value

- **Trust:** A premium aesthetic increases confidence in financial data
- **Engagement:** Users are more likely to open the app daily when it looks great
- **Brand identity:** Shifts the product from "developer tool" to "personal finance product"

---

## Design Reference

Source: Google Stitch export — WealthCurator Personal Finance Dashboard

### Colour Palette (updated CSS custom properties)

```css
:root {
  /* Brand */
  --color-primary: #0056d2;
  --color-primary-dark: #0040a1;

  /* Status */
  --color-success: #1b6d24;
  --color-danger: #ba1a1a;
  --color-warning: #f59e0b;

  /* Surfaces — Material Design 3 tier system */
  --color-bg: #f9f9fd;
  --color-surface: #ffffff;
  --color-surface-container-low: #f3f3f7;
  --color-surface-container: #edeef1;
  --color-surface-container-high: #e7e8eb;
  --color-surface-container-highest: #e2e2e6;

  /* Typography */
  --color-text: #191c1e;
  --color-muted: #424654;

  /* Borders */
  --color-border: #c3c6d6;
  --color-border-strong: #737785;

  /* Sidebar — light */
  --color-sidebar-bg: #f8fafc;
  --color-sidebar-text: #1e3a5f;
  --color-sidebar-active-bg: rgba(0, 64, 161, 0.08);
  --color-sidebar-active: #0040a1;
  --color-sidebar-hover: #f1f5f9;

  /* Layout */
  --font-headline: "Manrope", "Inter", sans-serif;
  --border-radius-card: 0.75rem;
  --color-card-shadow:
    0 1px 3px rgba(0, 0, 0, 0.06), 0 1px 2px rgba(0, 0, 0, 0.04);
}

[data-theme="dark"] {
  --color-bg: #0f172a;
  --color-surface: #1e293b;
  --color-surface-container-low: #1e293b;
  --color-surface-container: #243044;
  --color-surface-container-high: #2a3650;
  --color-surface-container-highest: #334155;
  --color-text: #f1f5f9;
  --color-muted: #94a3b8;
  --color-border: #334155;
  --color-border-strong: #475569;
  --color-sidebar-bg: #0f172a;
  --color-sidebar-text: #e2e8f0;
  --color-sidebar-active-bg: rgba(178, 197, 255, 0.12);
  --color-sidebar-active: #b2c5ff;
  --color-sidebar-hover: rgba(255, 255, 255, 0.06);
}
```

### Typography

- **Headline font:** `Manrope` (weights 600, 700, 800) — page titles, card headings, balance figures
- **Body font:** `Inter` (existing) — body text, labels, navigation
- **Monospace (amounts):** `JetBrains Mono` (existing)
- **Google Fonts CDN** — add to `App.razor`:
  ```html
  <link
    href="https://fonts.googleapis.com/css2?family=Manrope:wght@600;700;800&display=swap"
    rel="stylesheet"
  />
  <link
    href="https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined:opsz,wght,FILL,GRAD@20..48,100..700,0..1,-50..200&display=swap"
    rel="stylesheet"
  />
  ```

---

## Scope

All pages receive updated CSS tokens automatically. The following pages get structural HTML changes:

| Page                                   | Change                                                                  |
| -------------------------------------- | ----------------------------------------------------------------------- |
| `NavMenu.razor` + `.css`               | Light sidebar, rounded active states, "Transactie toevoegen" CTA button |
| `MainLayout.razor` + `.css`            | Frosted-glass top bar, user initials avatar                             |
| `Home.razor`                           | Bento-grid layout (2×2 rows)                                            |
| `TotalBalanceKpiWidget.razor`          | Hero card with large balance, trend badge, decorative blur              |
| `RecentTransactionsWidget.razor`       | Grouped date headers, category icon circles                             |
| `BudgetStatusWidget.razor`             | Coloured progress bars, insight callout card                            |
| `UncategorizedKpiWidget.razor`         | Compact stat card                                                       |
| `ActiveBudgetsKpiWidget.razor`         | Compact stat card                                                       |
| `TransactionsThisMonthKpiWidget.razor` | Compact stat card                                                       |
| `Transactions.razor`                   | Grouped-by-date rows, category icon circles, colour-coded amounts       |
| `Accounts.razor`                       | Card-grid layout (3-col desktop, 2-col tablet, 1-col mobile)            |
| `BudgetPlans.razor`                    | Card-grid with utilisation progress bar per plan                        |
| `BudgetPlanEdit.razor`                 | Sectioned form layout with card containers                              |

---

## Implementation Phases

### Phase 1 — Fonts & Design Tokens _(prerequisite for all other phases)_

- [ ] **`App.razor`** — add Manrope + Material Symbols CDN links
- [ ] **`wwwroot/app.css`** — replace all CSS custom properties with Stitch palette; add `.font-headline`, `.bento-grid`, updated card/button/badge/table/form global styles; add `[data-theme="dark"]` overrides for all new sidebar tokens

### Phase 2 — Layout: Sidebar + Header _(depends on Phase 1)_

- [ ] **`NavMenu.razor.css`** — rewrite for light sidebar: `--color-sidebar-bg` background, `border-right: 1px solid --color-border`, nav items `border-radius: 0.5rem`, active state uses `--color-sidebar-active-bg` + `--color-sidebar-active` text
- [ ] **`NavMenu.razor`** — wrap brand text in `font-headline`; keep all `NavLink` hrefs and `data-testid` unchanged; add "Transactie toevoegen" primary pill button at bottom (inside `<Authorized>`, links to `/transactions/create`)
- [ ] **`MainLayout.razor.css`** — `.top-row` becomes `position: sticky; top: 0; backdrop-filter: blur(20px); background: rgba(var(--color-surface-rgb), 0.85)`; add `--color-surface-rgb` helper token
- [ ] **`MainLayout.razor`** — replace plain username text with `<div class="user-avatar">` showing initials; all `data-testid` attributes preserved

### Phase 3 — Dashboard _(depends on Phase 1 & 2)_

- [ ] **`Home.razor`** — replace `.kpi-grid` + separate sections with `.bento-grid`:
  - Row 1: `TotalBalanceKpiWidget` (col-span 8) + quick-actions card (col-span 4)
  - Row 2: net-worth chart placeholder (col-span 8) + `BudgetStatusWidget` (col-span 4)
  - Row 3: `RecentTransactionsWidget` (col-span 8) + stacked KPI mini-cards (col-span 4)
- [ ] **`TotalBalanceKpiWidget.razor`** — hero layout: `font-headline` balance figure, trend percentage badge, "Importeren" + "Rekeningen" action buttons, decorative `blur-circle` div
- [ ] **`RecentTransactionsWidget.razor`** — grouped date headers (`<div class="tx-group-header">`), each row gets a category icon circle, amount colour-coded; "Toewijzen" badge preserved
- [ ] **`BudgetStatusWidget.razor`** — `<div class="budget-bar">` progress bars with fill colours (primary/danger/success); insight callout card at bottom with lightbulb icon
- [ ] **`UncategorizedKpiWidget.razor`** — compact stat card (icon circle + label + value + optional badge)
- [ ] **`ActiveBudgetsKpiWidget.razor`** — compact stat card
- [ ] **`TransactionsThisMonthKpiWidget.razor`** — compact stat card

### Phase 4 — Transactions Page _(depends on Phase 1)_

- [ ] **`Transactions.razor`** — transactions grouped by date with sticky group headers; each row: category icon circle + description/category text + colour-coded amount; keep all filters, `data-testid` attributes, and pagination unchanged

### Phase 5 — Accounts Page _(depends on Phase 1)_

- [ ] **`Accounts.razor`** — switch from table/list to Bootstrap card-columns grid; each card: account name (headline font), masked IBAN, large balance, action buttons; keep `data-testid` attributes

### Phase 6 — Budget Pages _(depends on Phase 1)_

- [ ] **`BudgetPlans.razor`** — card-grid layout; each plan card shows period, total allocated, utilisation progress bar; keep `data-testid` attributes
- [ ] **`BudgetPlanEdit.razor`** — wrap form sections in `.card-form-section` cards

---

## Constraints

- **Bootstrap 5 stays** — no Tailwind, no removal of Bootstrap classes
- **No framework migration** — only CSS tokens + component markup changes
- **All `data-testid` attributes are never touched** — E2E test safety
- **Dark mode preserved** — every new CSS variable has a `[data-theme="dark"]` counterpart
- **Dutch navigation labels preserved** — Rekeningen, Transacties, Budgetplannen, etc.
- **Material Symbols added alongside Bootstrap Icons** — existing `bi-*` icons are not removed

---

## Acceptance Criteria

- [ ] Light sidebar visible on desktop; hamburger overlay still works on mobile
- [ ] Manrope font renders for all headings and balance figures
- [ ] Dashboard shows bento-grid layout with hero balance card, budget bars, and grouped transactions
- [ ] Transactions page groups rows by date with category icon circles
- [ ] Accounts page renders as a card-grid (not a table)
- [ ] Budget plans page renders as a card-grid with progress bars
- [ ] Dark mode toggle switches all surfaces including sidebar correctly
- [ ] All existing E2E tests pass without changes to selectors
- [ ] `dotnet build` produces zero errors
- [ ] Lighthouse Accessibility score ≥ 90 maintained (contrast ratios checked for new palette)

## Definition of Done

- All Phase 1–6 tasks checked off
- `dotnet build` clean
- `dotnet test` (unit + integration) green
- E2E test suite green — all `data-testid` selectors resolve
- Visual inspection: desktop light, desktop dark, mobile (375px), tablet (768px)
- No `bin/`, `obj/`, or `.db` files committed
